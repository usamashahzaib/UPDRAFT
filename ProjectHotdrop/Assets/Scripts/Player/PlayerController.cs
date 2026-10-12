using UnityEngine;
using UnityEngine.InputSystem;
using Hotdrop.Config;
using Hotdrop.Movement;
using Hotdrop.Pads;
using Hotdrop.Weapons;
using Hotdrop.Input;
using Hotdrop.Camera;

namespace Hotdrop.Player
{
    /// <summary>
    /// MonoBehaviour wrapper for player movement, health, weapons, and pads.
    /// Bridges the pure C# simulation logic (PlayerMovementSimulator, PlayerHealthController,
    /// PlayerPadManager, ShotResolver) with Unity's physics/rendering.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class PlayerController : MonoBehaviour, Hotdrop.Match.IMatchParticipant
    {
        [Header("Config")]
        [SerializeField] MovementConfig movementConfig;
        [SerializeField] WeaponDefinition[] weaponDefinitions; // ARC, ZIP, BRICK
        [SerializeField] PadConfig padConfig;
        [SerializeField] MapConfig mapConfig;
        MatchRulesConfig matchRules;

        [Header("References")]
        // Public so MatchController/TutorialController can wire these at spawn time.
        public ThirdPersonCamera playerCamera;
        public TouchInputManager touchInput;
        [SerializeField] Transform muzzleTransform;
        [SerializeField] Transform cameraPivot; // upper-torso pivot for camera

        [Header("Visuals")]
        [SerializeField] GameObject bodyMesh;
        [SerializeField] GameObject[] weaponMeshes; // indexed by WeaponId
        [SerializeField] Renderer bodyRenderer;
        [SerializeField] Material friendlyMaterial;
        [SerializeField] Material enemyMaterial;

        [Header("Audio")]
        [SerializeField] AudioClip[] footstepClips;
        [SerializeField] AudioClip landClip;
        [SerializeField] AudioClip jumpClip;
        [SerializeField] AudioClip damageClip;
        [SerializeField] AudioClip deathClip;

        // Simulation components
        PlayerMovementSimulator movementSim;
        PlayerHealthController healthController;
        PlayerPadManager padManager;

        // Runtime state
        CharacterController charController;
        AudioSource audioSource;
        WeaponDefinition currentWeapon;
        int currentWeaponIndex = 0;
        readonly WeaponState weaponState = new WeaponState();
        readonly PelletResult[] pelletBuffer = new PelletResult[16];
        bool wasGrounded;
        float timeSinceLastFootstep;
        int teamId = 0; // 0 = TeamA (friendly), 1 = TeamB (enemy)
        SportAvatarAnimation avatarMotion;
        int playerId;
        string displayName = "Player";
        bool movementAllowed = true;
        bool combatAllowed = true;
        bool padPlacementAllowed = true;
        bool weaponSelectionAllowed = true;
        bool isSpawnProtected;
        float spawnProtectionRemaining;
        float weaponLockoutUntil;
        bool hadPadCharge = true;

        // External (bot) input override. Humans drive via touchInput; bots drive
        // via SetExternalInput so BotController's decisions actually reach fire/
        // reload/jump instead of silently doing nothing (spec section 12: bots
        // are driven through the same input interface as humans).
        bool useExternalInput;
        Vector2 externalMoveAxis;
        bool externalJumpPressed;
        bool externalFireHeld;
        bool externalReloadPressed;

        bool hasExternalAim;
        Vector3 externalAimDirection;

        public void SetExternalInput(Vector2 moveAxis, bool jumpPressed, bool fireHeld, bool reloadPressed)
        {
            useExternalInput = true;
            externalMoveAxis = moveAxis;
            externalJumpPressed = jumpPressed;
            externalFireHeld = fireHeld;
            externalReloadPressed = reloadPressed;
        }

        /// <summary>Camera-less (bot) aim direction; humans aim through the camera.</summary>
        public void SetExternalAim(Vector3 direction)
        {
            hasExternalAim = direction.sqrMagnitude > 0.0001f;
            externalAimDirection = direction.normalized;
        }

        // Every live player, maintained without per-frame scene searches.
        public static readonly System.Collections.Generic.List<PlayerController> All = new System.Collections.Generic.List<PlayerController>();
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable()
        {
            All.Remove(this);
            if (!IsBot) Hotdrop.Core.GameAudio.ClearFlightMix();
        }

        public static PlayerController Find(int id)
        {
            for (int i = 0; i < All.Count; i++) if (All[i].playerId == id) return All[i];
            return null;
        }

        int Hotdrop.Match.IMatchParticipant.ParticipantId => playerId;
        public float HealthFraction => healthController.CurrentHealth / movementConfig.maxHealth;
        public Vector3 Position => transform.position;
        public string DisplayName
        {
            get => Hotdrop.UI.StreamSafePresentation.PlayerName(displayName, playerId);
            set => displayName = value;
        }
        public bool IsBot { get; set; }
        public bool IsConnected => true;
        public Vector3 FacingForward => GetFacingForward();

        // Per-match stats for the results screen and personal bests.
        public int Eliminations { get; private set; }
        public int Deaths { get; private set; }
        public int PadLaunches { get; private set; }
        public float ObjectiveSeconds { get; set; }
        public float RespawnRemaining => IsAlive ? 0f : Mathf.Max(0f, respawnAt - Time.time);
        public int LastKillerId { get; private set; } = -1;
        public float LastShotTime { get; private set; } = -100f;
        /// <summary>Where the last shot was fired from: the audible origin bots may react to.</summary>
        public Vector3 LastShotPosition { get; private set; }
        float respawnAt;
        float nextLowHealthCue;
        bool tookDamageThisLife;

        public void ResetStats()
        {
            Eliminations = Deaths = PadLaunches = 0;
            ObjectiveSeconds = 0f;
        }

        // Aim assist (spec section 8: slowdown only, ≤3° cone, ≤25m, sensitivity drops to 70%)
        [Header("Aim Assist")]
        [SerializeField] bool aimAssistEnabled = true;
        readonly Collider[] aimAssistCandidates = new Collider[32];
        readonly RaycastHit[] shotHits = new RaycastHit[64];

        // Events
        public System.Action<int, Vector3, Vector3> OnFire; // weaponIndex, muzzlePos, aimDir
        public System.Action OnShotStarted; // committed ammo/cadence, before local hit callbacks
        public System.Action<PlayerController> OnDeath;
        public System.Action<PlayerController> OnRespawn;
        public System.Action<float> OnHealthChanged; // 0-1
        /// <summary>Local presentation signal: planar speed in m/s and current airborne state.</summary>
        public System.Action<float, bool> OnFlightMotion;
        /// <summary>World-space direction from this player toward the attacker, raised once per damaging hit.</summary>
        public System.Action<Vector3> OnDamagedFrom;
        public System.Action<int, int> OnAmmoChanged; // current, max
        public System.Action<LaunchPad> OnPadPlaced;
        public System.Action<LaunchPad> OnPadActivated;
        public System.Action OnPadChargeReady;
        public System.Action OnShotBlocked;
        public System.Action<bool> OnHitConfirmed;
        public System.Action<int, WeaponId> OnEliminated;

        public bool IsAlive => healthController.IsAlive;
        public bool IsGrounded => movementSim.IsGrounded;
        public float CurrentHealth => healthController.CurrentHealth;
        public float MaxHealth => movementConfig.maxHealth;
        public int CurrentAmmo => weaponState.Ammo;
        public int MaxAmmo => currentWeapon?.magazineSize ?? 0;
        public bool IsReloading => weaponState.IsReloading;
        public string LastAttackDirection { get; private set; }
        /// <summary>QA-031: 0..1 progress of the current reload (0 when not reloading).</summary>
        public float ReloadProgress => weaponState.IsReloading && currentWeapon != null && currentWeapon.reloadSeconds > 0f
            ? Mathf.Clamp01(1f - weaponState.ReloadRemaining / currentWeapon.reloadSeconds) : 0f;
        public WeaponDefinition CurrentWeapon => currentWeapon;
        public PlayerPadManager PadManager => padManager;
        public bool HasPadCharge => padManager.HasCharge;
        public float PadRechargeRemaining => padManager.RechargeRemaining;
        public float PadRechargeDuration => padConfig.rechargeSeconds;
        public bool CanActivatePad => padManager.CanActivate;
        public bool IsSpawnProtected => isSpawnProtected;
        public int TeamId => teamId;
        public int PlayerId => playerId;

        void Awake()
        {
            matchRules = Resources.Load<MatchRulesConfig>("Config/MatchRulesConfig");
            charController = GetComponent<CharacterController>();
            audioSource = GetComponent<AudioSource>();
            audioSource.priority = 20; // threats (shots, footsteps, damage) outrank everything else
            audioSource.spatialBlend = 1f; // stereo position + distance attenuation
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 3f;
            audioSource.maxDistance = 50f;
            audioSource.dopplerLevel = 0f;
            if (footstepClips == null || footstepClips.Length == 0) footstepClips = new[] { Hotdrop.Core.OriginalGameAudio.Get(Hotdrop.Core.GameCue.Step) };
            if (landClip == null) landClip = Hotdrop.Core.OriginalGameAudio.Get(Hotdrop.Core.GameCue.Land);
            if (jumpClip == null) jumpClip = Hotdrop.Core.OriginalGameAudio.Get(Hotdrop.Core.GameCue.Jump);
            if (damageClip == null) damageClip = Hotdrop.Core.OriginalGameAudio.Get(Hotdrop.Core.GameCue.Hurt);
            if (deathClip == null) deathClip = Hotdrop.Core.OriginalGameAudio.Get(Hotdrop.Core.GameCue.Elimination);

            // Setup CharacterController from config
            charController.height = movementConfig.capsuleHeight;
            charController.radius = movementConfig.capsuleRadius;
            charController.stepOffset = movementConfig.stepHeight;
            charController.center = new Vector3(0, movementConfig.capsuleHeight * 0.5f, 0);

            // Initialize simulation
            movementSim = new PlayerMovementSimulator(movementConfig);
            healthController = new PlayerHealthController(movementConfig);
            padManager = new PlayerPadManager(padConfig);

            // Setup weapon
            if (weaponDefinitions.Length > 0)
            {
                currentWeapon = weaponDefinitions[0]; // ARC default per spec
                weaponState.Equip(currentWeapon);
            }

            // Setup visuals
            UpdateWeaponVisual();
            SetTeamMaterial(teamId);
            avatarMotion = bodyMesh != null ? bodyMesh.GetComponentInChildren<SportAvatarAnimation>(true) : null;

            BindTouchInput();
        }

        public void SetMatchRulesConfig(MatchRulesConfig rules)
        {
            if (rules != null) matchRules = rules;
        }

        void Start()
        {
            // Load aim assist setting
            aimAssistEnabled = PlayerPrefs.GetInt("AimAssistEnabled", 1) == 1;
            // Rebuild all runners so bots and humans use the same current presentation.
            ApplyStoredBodyRig();
        }

        /// <summary>
        /// Rebuilds this avatar's humanoid in the locally chosen body rig (spec 15 v2.1: identical
        /// capsule, hitboxes and animation set). Appearance-only; safe to call again when the
        /// player changes the choice in Appearance.
        /// </summary>
        public void ApplyStoredBodyRig()
        {
            if (bodyMesh == null) return;
            bool friendly = LaunchPadBehaviour.IsFriendlyToViewer(teamId);
            var parts = AvatarRigVisual.Rebuild(bodyMesh, IsBot ? BodyRigs.Default : BodyRigStore.Current,
                friendly ? friendlyMaterial : enemyMaterial,
                Resources.Load<Material>("Materials/Equipment"), includeGun: AvatarRigVisual.UsesOperator);
            // The skinned operator holds its weapon in hand; the legacy floating weapon meshes
            // stay for weapon logic and wraps but no longer draw beside the body.
            if (AvatarRigVisual.UsesOperator && weaponMeshes != null)
                foreach (var weapon in weaponMeshes)
                    if (weapon != null)
                        foreach (var r in weapon.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            avatarMotion = bodyMesh.GetComponentInChildren<SportAvatarAnimation>(true);
            if (avatarMotion == null) avatarMotion = bodyMesh.AddComponent<SportAvatarAnimation>();
            AvatarRigVisual.Wire(avatarMotion, parts);
            AvatarRigVisual.SetPersistentDamage(bodyMesh, tookDamageThisLife);
            var renderer = parts.Torso.GetComponent<Renderer>();
            if (renderer != null) bodyRenderer = renderer;
            SetTeamMaterial(teamId); // re-colours the new torso and re-applies team cue + cosmetic
            UpdateWeaponVisual();    // the rebuilt operator starts with the default weapon in hand
        }

        public void Initialize(int playerId, int teamId, WeaponDefinition startingWeapon = null)
        {
            this.playerId = playerId;
            this.teamId = teamId;
            Hotdrop.Map.SpawnExitBarrier.ConfigurePlayer(this);

            if (startingWeapon != null)
            {
                currentWeapon = startingWeapon;
                currentWeaponIndex = System.Array.IndexOf(weaponDefinitions, currentWeapon);
                if (currentWeaponIndex < 0) currentWeaponIndex = 0;
            }

            weaponState.Equip(currentWeapon);
            SetTeamMaterial(teamId);
            UpdateWeaponVisual();
            BindTouchInput();
        }

        void BindTouchInput()
        {
            if (touchInput == null) return;
            touchInput.OnPingWorldPositionRequested -= HandlePingRequest;
            touchInput.OnPingWorldPositionRequested += HandlePingRequest;
        }

        void OnDestroy()
        {
            padPreview.Destroy();
            if (touchInput != null)
                touchInput.OnPingWorldPositionRequested -= HandlePingRequest;
        }

        public void SetWeapon(int weaponIndex)
        {
            if (weaponIndex < 0 || weaponIndex >= weaponDefinitions.Length) return;
            if (healthController.IsAlive && !weaponSelectionAllowed) return;

            currentWeaponIndex = weaponIndex;
            currentWeapon = weaponDefinitions[weaponIndex];
            weaponState.Equip(currentWeapon);
            UpdateWeaponVisual();
            OnAmmoChanged?.Invoke(weaponState.Ammo, currentWeapon.magazineSize);
        }

        /// <summary>Used by tutorial scripting to force a specific ammo count (e.g. the reload step).</summary>
        public void SetAmmo(int amount)
        {
            weaponState.SetAmmo(amount);
            OnAmmoChanged?.Invoke(weaponState.Ammo, currentWeapon.magazineSize);
        }

        public void WarpForTraining(Vector3 position, Quaternion rotation)
        {
            charController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            movementSim.Reset();
            weaponLockoutUntil = 0f;
            charController.enabled = true;
            playerCamera?.SetTarget(transform);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            // A paused local world must preserve contact/state. CharacterController.Move(0)
            // otherwise clears CollidedBelow and turns a grounded checkpoint into airborne.
            if (dt <= 0) return;
            padManager.Tick(dt);
            if (!hadPadCharge && padManager.HasCharge)
                OnPadChargeReady?.Invoke();
            hadPadCharge = padManager.HasCharge;
            if (touchInput != null)
            {
                touchInput.PadAvailable = padManager.HasCharge;
                touchInput.PadCooldown = padManager.RechargeRemaining;
                touchInput.LaunchAvailable = false;
                if (movementSim.IsGrounded && padManager.CanActivate &&
                    Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out var padHit, 1f, LayerMask.GetMask("Pad"), QueryTriggerInteraction.Ignore))
                    touchInput.LaunchAvailable = padHit.collider.GetComponentInParent<LaunchPadBehaviour>()?.PadData?.CanBeUsed == true;
            }

            if (!healthController.IsAlive) return;

            if (transform.position.y < mapConfig.groundLevelY - 10f ||
                transform.position.x < mapConfig.boundsMin.x - 5f || transform.position.x > mapConfig.boundsMax.x + 5f ||
                transform.position.z < mapConfig.boundsMin.y - 5f || transform.position.z > mapConfig.boundsMax.y + 5f)
            {
                healthController.ApplyDamage(movementConfig.maxHealth);
                Die(-1, currentWeapon.id);
                return;
            }

            float healthBeforeTick = healthController.CurrentHealth;
            healthController.Tick(dt);
            if (touchInput != null && matchRules != null &&
                healthController.CurrentHealth <= movementConfig.maxHealth * matchRules.lowHealthThresholdFraction &&
                Time.time >= nextLowHealthCue)
            {
                nextLowHealthCue = Time.time + matchRules.lowHealthAudioRepeatIntervalSeconds;
                Hotdrop.Core.GameAudio.Play2D(Hotdrop.Core.GameCue.LowHealth, 0.7f);
            }
            if (!Mathf.Approximately(healthBeforeTick, healthController.CurrentHealth))
                OnHealthChanged?.Invoke(healthController.CurrentHealth / movementConfig.maxHealth);
            UpdateSpawnProtection(dt);
            UpdateCameraInput();
            if (playerCamera != null)
                transform.rotation = Quaternion.Euler(0, playerCamera.transform.eulerAngles.y, 0);

            MovementInput movementInput = GetMovementInput();
            if (movementAllowed)
            {
                HandlePadInteraction();
                movementSim.Tick(movementInput, GetFacingForward(), GetFacingRight(), dt);
                if (movementSim.IsLaunched && movementInput.moveAxis.sqrMagnitude > 0.01f)
                {
                    Vector3 steerDirection = GetFacingForward() * movementInput.moveAxis.y + GetFacingRight() * movementInput.moveAxis.x;
                    movementSim.ApplyAirSteer(steerDirection, padConfig.airSteerDegreesPerSecond, dt);
                }
            }

            // Apply movement via CharacterController
            if (movementAllowed)
            {
                  Vector3 displacement = movementSim.Velocity * dt;
                  // CharacterController.Move(0) does not retain CollidedBelow.
                  // Keep a small downward contact step while grounded so jump
                  // and launch do not randomly fail on alternating frames.
                  if (movementSim.IsGrounded && displacement.y <= 0f)
                      displacement.y = -2f * dt;
                  CollisionFlags flags = charController.Move(displacement);
                movementSim.IsGrounded = (flags & CollisionFlags.CollidedBelow) != 0;
            }

            // Handle landing
            if (!wasGrounded && movementSim.IsGrounded)
            {
                PlaySound(landClip);
                timeSinceLastFootstep = 0f;
            }
            wasGrounded = movementSim.IsGrounded;
            if (!IsBot)
            {
                float planarSpeed = new Vector2(movementSim.Velocity.x, movementSim.Velocity.z).magnitude;
                bool airborne = !movementSim.IsGrounded && healthController.IsAlive;
                Hotdrop.Core.GameAudio.SetFlightMix(planarSpeed, movementSim.Velocity.y, airborne);
                OnFlightMotion?.Invoke(planarSpeed, airborne);
            }

            // Footsteps
            HandleFootsteps(dt);

            // Handle pad activation (jump on armed pad = launch)

            UpdatePadPreview();
            if (touchInput?.PadPressed == true)
                TryPlacePad();

            // Handle shooting
            HandleShooting(dt);

            // Handle reload
            HandleReload(dt);

            // Amendment 2: the avatar's combat pose follows the real weapon state.
            if (avatarMotion != null)
                avatarMotion.SetActionState(weaponState.IsReloading, movementSim.IsGrounded);

            // Update camera pivot position (upper torso)
            if (cameraPivot != null)
            {
                cameraPivot.position = transform.position + Vector3.up * movementConfig.capsuleHeight * 0.8f;
            }
        }

        void UpdateCameraInput()
        {
            if (playerCamera == null) return;
            if (touchInput != null && touchInput.EditingLayout)
            {
                // Layout editing owns every touch until SAVE/CANCEL. Guard at the camera sink as
                // well as in TouchInputManager so a stale aim delta or focus state cannot turn
                // the gameplay camera during a UI gesture, regardless of component update order.
                playerCamera.SetFocusAimHeld(false);
                return;
            }
            bool focusAim = touchInput != null && touchInput.FocusAimHeld;
            playerCamera.SetFocusAimHeld(focusAim);
            if (touchInput == null) return;

            float focusScale = focusAim ? touchInput.FocusAimSensitivityScale : 1f;
            touchInput.AimSensitivityScale = AimAssistRules.SensitivityScale(IsAimAssistRegionActive()) * focusScale;

            Vector2 aim = touchInput.AimDelta;
            if (aim.sqrMagnitude > 0f)
                playerCamera.AddYawPitch(aim.x, -aim.y);
        }

        void UpdateSpawnProtection(float dt)
        {
            if (!isSpawnProtected) return;

            spawnProtectionRemaining -= dt;
            if (spawnProtectionRemaining <= 0f || FireHeld || !IsInsideOwnSpawnRoom())
                isSpawnProtected = false;
        }

        // QA-007: one rule for offline and online. The old inline version widened the Vector2 room
        // size to a Vector3 (z = 0), so only the room's centre line counted as "inside".
        bool IsInsideOwnSpawnRoom() => mapConfig.IsInsideSpawnRoom(teamId, transform.position);

        MovementInput GetMovementInput()
        {
            if (useExternalInput)
            {
                return new MovementInput
                {
                    moveAxis = externalMoveAxis,
                    jumpOrLaunchPressed = externalJumpPressed,
                    isFiring = externalFireHeld && weaponState.CanFire
                };
            }

            var input = new MovementInput
            {
                moveAxis = touchInput?.MoveInput ?? Vector2.zero,
                jumpOrLaunchPressed = touchInput?.JumpPressed ?? false,
                isFiring = (touchInput?.FireHeld ?? false) && weaponState.CanFire
            };
            return input;
        }

        bool FireHeld => useExternalInput ? externalFireHeld : (touchInput?.FireHeld ?? false);
        bool JumpPressed => useExternalInput ? externalJumpPressed : (touchInput?.JumpPressed ?? false);
        bool ReloadPressed => useExternalInput ? externalReloadPressed : (touchInput?.ReloadPressed ?? false);

        Vector3 GetFacingForward()
        {
            if (playerCamera != null)
            {
                Vector3 forward = playerCamera.transform.forward;
                forward.y = 0;
                return forward.normalized;
            }
            return transform.forward;
        }

        Vector3 GetFacingRight()
        {
            if (playerCamera != null)
            {
                Vector3 right = playerCamera.transform.right;
                right.y = 0;
                return right.normalized;
            }
            return transform.right;
        }

        void HandleFootsteps(float dt)
        {
            if (!movementSim.IsGrounded) return;
            if (footstepClips == null || footstepClips.Length == 0) return;

            float speed = new Vector3(movementSim.Velocity.x, 0, movementSim.Velocity.z).magnitude;
            if (speed > 1f)
            {
                timeSinceLastFootstep += dt;
                float stepInterval = 0.4f; // adjust based on speed
                if (timeSinceLastFootstep >= stepInterval)
                {
                    PlaySound(footstepClips[Random.Range(0, footstepClips.Length)], 0.5f);
                    timeSinceLastFootstep = 0f;
                }
            }
        }

        void HandlePadInteraction()
        {
            // Check if standing on an armed pad
            if (movementSim.IsGrounded && JumpPressed)
            {
                // Raycast down to find pad
                Ray ray = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, 1f, LayerMask.GetMask("Pad"), QueryTriggerInteraction.Ignore))
                {
                    var pad = hit.collider.GetComponentInParent<LaunchPadBehaviour>();
                      if (pad != null && pad.PadData.CanBeUsed && padManager.CanActivate &&
                          Vector2.Distance(new Vector2(transform.position.x, transform.position.z),
                              new Vector2(pad.PadData.Position.x, pad.PadData.Position.z)) <= padConfig.diameter * 0.5f)
                    {
                        // Launch!
                        Vector3 launchVel = pad.PadData.GetLaunchVelocity();
                        movementSim.ApplyLaunchVelocity(launchVel);
                        weaponLockoutUntil = Time.time + padConfig.launchWeaponLockoutSeconds;
                        pad.NotifyActivated();
                        padManager.NotifyActivated();
                        PadLaunches++;
                        if (!IsBot)
                        {
                            Hotdrop.Core.HapticFeedback.Pulse();
                            playerCamera?.TriggerPadLaunchFeedback();
                        }
                        OnPadActivated?.Invoke(pad.PadData);
                        PlaySound(jumpClip, 1f); // launch sound
                        return;
                    }
                }
            }

            // Normal jump
            if (JumpPressed && movementSim.IsGrounded)
            {
                PlaySound(jumpClip);
            }
        }

        void HandleShooting(float dt)
        {
            if (currentWeapon == null || !combatAllowed) return;

            // Spawn protection: no firing out of your own spawn room.
            bool trigger = FireHeld && !IsInsideOwnSpawnRoom() && Time.time >= weaponLockoutUntil;
            bool wasReloading = weaponState.IsReloading;
            int shots = weaponState.Tick(dt, trigger);
            for (int i = 0; i < shots; i++)
                FireShot();

            if (!wasReloading && weaponState.IsReloading)
                PlayReloadCues(emptied: true); // auto-reload on an empty magazine
            else if (wasReloading && !weaponState.IsReloading)
                OnAmmoChanged?.Invoke(weaponState.Ammo, currentWeapon.magazineSize);
        }

        void FireShot()
        {
            if (currentWeapon == null) return;

            LastShotTime = Time.time;
            LastShotPosition = transform.position;
            OnAmmoChanged?.Invoke(weaponState.Ammo, currentWeapon.magazineSize);

            // Determine movement state for spread
            MovementState moveState;
            if (!movementSim.IsGrounded) moveState = MovementState.Airborne;
            else if (new Vector3(movementSim.Velocity.x, 0, movementSim.Velocity.z).magnitude > movementConfig.weaponMovingGroundSpeedThreshold) moveState = MovementState.MovingGrounded;
            else moveState = MovementState.StationaryGrounded;

            // Camera chooses the aim point; the real shot is traced from the muzzle
            // so nearby walls still block a camera-visible target.
            Vector3 aimDirection = GetMuzzleAimDirection();

            // Generate shot seed (in real networked game, server provides this)
            uint shotSeed = (uint)Random.Range(0, int.MaxValue);

            // Resolve pellets
            int pelletCount = ShotResolver.ResolvePellets(currentWeapon, aimDirection, shotSeed, moveState, pelletBuffer);
            OnShotStarted?.Invoke();
            touchInput?.PulseFireFeedback();

            // QA-012: a muzzle pushed into or through thin cover fires into that cover.
            if (IsMuzzleBuried(out Vector3 buriedAt))
            {
                ShotTracers.Show(muzzleTransform.position, buriedAt, LaunchPadBehaviour.IsFriendlyToViewer(teamId));
                OnShotBlocked?.Invoke();
            }
            else
            {
                // Fire each pellet (in offline mode, we process locally)
                for (int i = 0; i < pelletCount; i++)
                {
                    ProcessPellet(pelletBuffer[i].direction);
                }
            }

            // Recoil
            int shotInMagazine = currentWeapon.magazineSize - weaponState.Ammo;
            ApplyRecoil(currentWeapon.GetVerticalRecoilDegrees(shotInMagazine));

            // Muzzle flash, sound, etc.
            PlayWeaponFireSound();
            avatarMotion?.PulseFire();
            OnFire?.Invoke(currentWeaponIndex, muzzleTransform.position, aimDirection);
        }

        Vector3 GetMuzzleAimDirection()
        {
            if (playerCamera == null || muzzleTransform == null) return hasExternalAim ? externalAimDirection : transform.forward;
            Vector3 aimPoint = playerCamera.GetAimPoint(currentWeapon.maxRange, ~0);
            Vector3 direction = aimPoint - muzzleTransform.position;
            float distance = direction.magnitude;
            if (distance > 0.01f && !IsMuzzleBuried(out _)) // a buried muzzle is reported by the fire path
            {
                // QA-012: the same pass-through rule as ProcessPellet — the shooter, teammates and
                // the dead never block, so a teammate in the way no longer shows a false warning.
                int count = Physics.RaycastNonAlloc(muzzleTransform.position, direction / distance, shotHits, distance, ~0, QueryTriggerInteraction.Ignore);
                float nearest = distance;
                bool blocked = false;
                for (int i = 0; i < count; i++)
                {
                    if (shotHits[i].distance >= nearest) continue;
                    var player = shotHits[i].collider.GetComponentInParent<PlayerController>();
                    if (player != null && (player == this || player.TeamId == teamId || !player.IsAlive)) continue;
                    nearest = shotHits[i].distance;
                    blocked = player == null && shotHits[i].distance < distance - 0.1f;
                }
                if (blocked) OnShotBlocked?.Invoke();
            }
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : playerCamera.transform.forward;
        }

        bool IsMuzzleBuried(out Vector3 blockPoint)
        {
            blockPoint = muzzleTransform != null ? muzzleTransform.position : transform.position;
            if (muzzleTransform == null) return false;
            Vector3 muzzle = muzzleTransform.position;
            Vector3 body = new Vector3(transform.position.x, muzzle.y, transform.position.z);
            return MuzzleObstruction.IsBuried(Physics.defaultPhysicsScene, body, muzzle, MuzzleObstruction.GeometryMask, out blockPoint);
        }

        bool IsAimAssistRegionActive()
        {
            if (!aimAssistEnabled || playerCamera == null) return false;

            int candidateCount = Physics.OverlapSphereNonAlloc(playerCamera.transform.position, AimAssistRules.MaxDistance,
                aimAssistCandidates, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < candidateCount; i++)
            {
                var candidate = aimAssistCandidates[i];
                var target = candidate.GetComponentInParent<PlayerController>();
                if (target == null || target == this || !target.IsAlive || target.TeamId == teamId) continue;

                Vector3 targetPoint = target.transform.position + Vector3.up * 1.4f;
                Vector3 toTarget = targetPoint - playerCamera.transform.position;
                // Cover blocks assist outright (spec 8: no tracking through cover).
                bool blocked = Physics.Raycast(playerCamera.transform.position, toTarget.normalized, out RaycastHit hit,
                    toTarget.magnitude, ~0, QueryTriggerInteraction.Ignore) &&
                    hit.collider.GetComponentInParent<PlayerController>() != target;

                if (AimAssistRules.Qualifies(playerCamera.transform.position, playerCamera.transform.forward, targetPoint, !blocked))
                    return true;
            }

            return false;
        }

        void ProcessPellet(Vector3 direction)
        {
            // Raycast for hit
            Ray ray = new Ray(muzzleTransform.position, direction);
            float maxRange = currentWeapon.maxRange;

            int hitCount = Physics.RaycastNonAlloc(ray, shotHits, maxRange, ~0, QueryTriggerInteraction.Ignore);
            RaycastHit hit = default;
            bool found = false;
            float nearest = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                var player = shotHits[i].collider.GetComponentInParent<PlayerController>();
                if (player != null && (player == this || player.TeamId == teamId || !player.IsAlive)) continue;
                if (shotHits[i].distance >= nearest) continue;
                hit = shotHits[i]; nearest = hit.distance; found = true;
            }
            ShotTracers.Show(muzzleTransform.position, found ? hit.point : ray.GetPoint(maxRange),
                LaunchPadBehaviour.IsFriendlyToViewer(teamId));
            if (found)
            {
                var target = hit.collider.GetComponentInParent<PlayerController>();
                if (target != null && target.IsAlive && target.TeamId != this.teamId)
                {
                    // Check headshot
                    bool isHeadshot = hit.point.y - target.transform.position.y >= movementConfig.capsuleHeight * 0.8f;

                    float damage = ShotResolver.GetDamage(currentWeapon, hit.distance, isHeadshot);
                    target.ApplyDamage(damage, this.playerId, currentWeapon.id);
                    if (touchInput != null) Hotdrop.Core.HapticFeedback.Pulse(0.6f);
                    OnHitConfirmed?.Invoke(isHeadshot);
                    if (touchInput != null)
                        Hotdrop.Core.GameAudio.Play2D(target.IsAlive ? Hotdrop.Core.GameCue.Hit : Hotdrop.Core.GameCue.EliminationConfirm, 0.8f);
                }
                else if (hit.collider.GetComponentInParent<LaunchPadBehaviour>() != null)
                {
                    var padBehaviour = hit.collider.GetComponentInParent<LaunchPadBehaviour>();
                    // Damage pad (enemies can destroy, friendly fire can't per spec)
                    if (padBehaviour.PadData.TeamId != this.teamId)
                    {
                        Hotdrop.Core.ShotImpactVfx.TryShow(hit.collider, hit.point, hit.normal);
                        padBehaviour.ApplyDamage(ShotResolver.GetDamage(currentWeapon, hit.distance, false));
                    }
                }
                else if (hit.collider.GetComponentInParent<Hotdrop.Tutorial.TutorialTarget>() is Hotdrop.Tutorial.TutorialTarget trainingTarget)
                {
                    trainingTarget.TakeDamage(ShotResolver.GetDamage(currentWeapon, hit.distance, false));
                    OnHitConfirmed?.Invoke(false);
                }
                else
                {
                    Hotdrop.Core.ShotImpactVfx.TryShow(hit.collider, hit.point, hit.normal);
                }
            }
        }

        void ApplyRecoil(float verticalDegrees)
        {
            if (playerCamera != null)
            {
                playerCamera.AddRecoilPitch(-verticalDegrees);
            }
        }

        void HandleReload(float dt)
        {
            if (ReloadPressed && combatAllowed && weaponState.RequestReload())
                PlayReloadCues(emptied: false);
        }

        void PlayReloadCues(bool emptied)
        {
            if (emptied) PlaySound(Hotdrop.Core.OriginalGameAudio.Get(Hotdrop.Core.GameCue.Empty), 0.6f);
            PlaySound(Hotdrop.Core.OriginalGameAudio.Get(Hotdrop.Core.GameCue.Reload), 0.55f);
        }

        public void ApplyDamage(float amount, int attackerId, WeaponId weaponId)
        {
            if (!healthController.IsAlive || !combatAllowed || isSpawnProtected) return;

            float healthBeforeDamage = healthController.CurrentHealth;
            healthController.ApplyDamage(amount);
            OnHealthChanged?.Invoke(healthController.CurrentHealth / movementConfig.maxHealth);
            if (healthController.CurrentHealth < healthBeforeDamage)
            {
                tookDamageThisLife = true;
                AvatarRigVisual.SetPersistentDamage(bodyMesh, true);
            }

            var damageSource = Find(attackerId);
            if (damageSource != null && damageSource != this)
                OnDamagedFrom?.Invoke(damageSource.transform.position - transform.position);

            PlaySound(damageClip);
            if (touchInput != null)
                Hotdrop.Core.GameAudio.ReportLocalCue(Hotdrop.Core.GameCue.Hurt);

            if (!healthController.IsAlive)
            {
                Die(attackerId, weaponId);
            }
        }

        void Die(int killerId, WeaponId weaponId)
        {
            if (!IsBot) Hotdrop.Core.GameAudio.ClearFlightMix();
            // The local human hears only the gentle own-death drone; bots keep the spatial death clip.
            if (IsBot) PlaySound(deathClip);
            else Hotdrop.Core.GameAudio.Play2D(Hotdrop.Core.GameCue.OwnDeath);
            Deaths++;
            LastKillerId = killerId;
            var killer = Find(killerId);
            LastAttackDirection = killer == null || killer == this ? "" : DirectionLabel(killer.transform.position - transform.position);
            if (killer != null && killer != this) killer.Eliminations++;
            OnEliminated?.Invoke(killerId, weaponId);
            OnDeath?.Invoke(this);

            charController.enabled = false;
            if (bodyMesh != null) bodyMesh.SetActive(false);
            respawnAt = Time.time + movementConfig.respawnDelaySeconds;
            Invoke(nameof(Respawn), movementConfig.respawnDelaySeconds);
        }

        string DirectionLabel(Vector3 from)
        {
            float angle = Vector3.SignedAngle(transform.forward, from, Vector3.up);
            if (Mathf.Abs(angle) < 45f) return "FRONT";
            if (Mathf.Abs(angle) > 135f) return "BEHIND";
            return angle > 0f ? "RIGHT" : "LEFT";
        }

        void Respawn()
        {
            // Mirrored spawn exits (MapConfig.SpawnExit): the same layout for both teams.
            Pose spawn = mapConfig.SpawnExit(teamId, Random.Range(0, mapConfig.exitsPerSpawn));
            charController.enabled = false;
            transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            charController.enabled = true;

            healthController.ResetForRespawn();
            tookDamageThisLife = false;
            movementSim.Reset();
            weaponState.Equip(currentWeapon); // full magazine; the weapon may have changed while down
            OnAmmoChanged?.Invoke(weaponState.Ammo, currentWeapon.magazineSize);
            OnHealthChanged?.Invoke(1f);

            if (bodyMesh != null) bodyMesh.SetActive(true);
            AvatarRigVisual.SetPersistentDamage(bodyMesh, false);
            isSpawnProtected = true;
            spawnProtectionRemaining = movementConfig.respawnProtectionSeconds;

            OnRespawn?.Invoke(this);
        }

        public System.Action<string> OnPadRejected;

        /// <summary>Places a pad ~1.6 m ahead. Returns null on success, otherwise the reason; an invalid placement costs nothing.</summary>
        public string TryPlacePad()
        {
            string reason = ResolvePadPlacement(out Vector3 position, out Vector3 forward);
            if (reason == null)
            {
                var pad = padManager.PlaceNew(playerId, teamId, position, forward);
                if (pad == null) reason = "RECHARGING";
                else
                {
                    SpawnPadVisual(pad);
                    OnPadPlaced?.Invoke(pad);
                    return null;
                }
            }
            OnPadRejected?.Invoke(reason);
            return reason;
        }

        string ResolvePadPlacement(out Vector3 position, out Vector3 forward)
        {
            forward = GetFacingForward();
            position = transform.position + forward * padConfig.placementForwardOffset;
            if (!Hotdrop.Core.StudySettings.PadsEnabled) return "PADS DISABLED IN THIS VERSION";
            if (!padPlacementAllowed) return "NOT DURING THIS PHASE";
            if (!padManager.HasCharge) return "RECHARGING";
            if (!movementSim.IsGrounded) return "MUST BE GROUNDED";
            var physics = Physics.defaultPhysicsScene;
            string reason = PadPlacementRules.ResolvePosition(physics, transform.position, forward, padConfig, out position);
            if (reason != null) return reason;
            int activeOther = LaunchPadBehaviour.ActiveCountExcludingOwner(playerId);
            return PadPlacementRules.Validate(physics, position, forward, activeOther, padConfig, mapConfig, movementConfig);
        }

        readonly PadPreviewArc padPreview = new PadPreviewArc();

        // Hold-to-preview: draw the launch arc from the would-be pad, green if valid, red if not.
        void UpdatePadPreview()
        {
            bool show = touchInput != null && touchInput.PadPreviewActive && IsAlive;
            if (!show) { padPreview.Hide(); return; }
            string reason = ResolvePadPlacement(out Vector3 position, out Vector3 forward);
            padPreview.Show(position, forward, reason == null, padConfig, movementConfig.gravity);
            touchInput.PadPreviewReason = reason;
        }

        void SpawnPadVisual(LaunchPad pad)
        {
            // Primitive supplies the existing gameplay collider; Initialize replaces only its render mesh.
            GameObject padObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            padObj.transform.position = pad.Position;
            padObj.transform.rotation = Quaternion.LookRotation(pad.ForwardDirection);
            padObj.transform.localScale = new Vector3(padConfig.diameter, 0.1f, padConfig.diameter);
            padObj.layer = LayerMask.NameToLayer("Pad");

            // Initialize applies viewer-relative team colour and symbol.
            padObj.AddComponent<LaunchPadBehaviour>().Initialize(pad, true);
        }

        void HandlePingRequest(Vector2 screenPos)
        {
            if (playerCamera == null || Hotdrop.UI.PingSystem.Instance == null) return;
            Ray ray = playerCamera.GetComponent<UnityEngine.Camera>().ScreenPointToRay(screenPos);
            if (!Hotdrop.UI.PingSystem.Instance.RequestPing(this, ray))
                Hotdrop.Core.GameAudio.Play2D(Hotdrop.Core.GameCue.UiNegative, 0.4f); // rate-limited
        }

        void UpdateWeaponVisual()
        {
            if (weaponMeshes != null)
            {
                for (int i = 0; i < weaponMeshes.Length; i++)
                {
                    weaponMeshes[i].SetActive(i == currentWeaponIndex);
                }
            }
            // The skinned operator carries the equipped weapon's model in hand.
            if (currentWeapon != null && bodyMesh != null)
            {
                var held = bodyMesh.GetComponentInChildren<OperatorWeaponFollow>(true);
                if (held != null) held.SetWeapon(currentWeapon.id.ToString().ToUpperInvariant());
            }
        }

        void SetTeamMaterial(int team)
        {
            bool friendly = LaunchPadBehaviour.IsFriendlyToViewer(team);
            if (bodyRenderer != null)
            {
                var material = friendly ? friendlyMaterial : enemyMaterial;
                if (material != null) bodyRenderer.sharedMaterial = AvatarRigVisual.WithRim(material);
                else bodyRenderer.material.color = friendly
                    ? new Color(0.188f, 0.835f, 0.91f) : new Color(1f, 0.388f, 0.463f);
            }
            // QA-023: shape cue (ally orb + chest band / enemy cross + shoulder spikes) so team
            // never relies on colour alone. Parented to the body so it hides with it on death.
            var teamCue = TeamCueVisual.Apply(bodyMesh != null ? bodyMesh.transform : transform, friendly, friendly ? friendlyMaterial : enemyMaterial);
            // The overhead identifier is useful on other players, but blocks our own aim.
            if (!IsBot && touchInput != null)
            {
                var marker = teamCue.Find(friendly ? TeamCueVisual.AllyMarkerName : TeamCueVisual.EnemyMarkerName);
                if (marker != null) marker.gameObject.SetActive(false);
            }
            // QA-024/Amendment 3: the human player's equipped look (all slots) is visible on
            // their own avatar in play.
            if (touchInput != null) ApplyCosmetic(Hotdrop.UI.CosmeticLook.LocalComposite);
        }

        Material[] defaultWeaponMaterials;

        public void ApplyCosmetic(string cosmeticId)
        {
            Hotdrop.UI.CosmeticLook.Apply(bodyMesh != null ? bodyMesh.transform : transform, cosmeticId);
            if (weaponMeshes == null) return;
            if (defaultWeaponMaterials == null)
            {
                defaultWeaponMaterials = new Material[weaponMeshes.Length];
                for (int i = 0; i < weaponMeshes.Length; i++)
                    if (weaponMeshes[i] != null && weaponMeshes[i].GetComponent<Renderer>() is Renderer r) defaultWeaponMaterials[i] = r.sharedMaterial;
            }
            var wrap = Hotdrop.UI.CosmeticLook.WrapMaterialFor(cosmeticId);
            for (int i = 0; i < weaponMeshes.Length; i++)
                if (weaponMeshes[i] != null && weaponMeshes[i].GetComponent<Renderer>() is Renderer r)
                    r.sharedMaterial = wrap != null ? wrap : defaultWeaponMaterials[i];
        }

        // QA-022: the direct AudioSource path honours the same SFX volume setting as GameAudio.
        static float EffectiveSfxVolume(float baseVolume) => baseVolume * Hotdrop.Core.GameAudio.SfxVolume;

        void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, EffectiveSfxVolume(volume));
            }
        }

        void PlayWeaponFireSound()
        {
            var cue = (Hotdrop.Core.GameCue)currentWeaponIndex;
            // Keep offline weapon reports on the same spatial, layered mixer as
            // networked shots so both paths include mechanism and environment tail.
            Hotdrop.Core.GameAudio.Play(cue, muzzleTransform.position, 0.85f);
        }

        public void SetAimAssistEnabled(bool enabled)
        {
            aimAssistEnabled = enabled;
            PlayerPrefs.SetInt("AimAssistEnabled", enabled ? 1 : 0);
        }

        public void SetMatchPermissions(bool allowMovement, bool allowCombat, bool allowPadPlacement, bool allowWeaponSelection)
        {
            movementAllowed = allowMovement;
            combatAllowed = allowCombat;
            padPlacementAllowed = allowPadPlacement;
            weaponSelectionAllowed = allowWeaponSelection;
        }

        public void ResetForMatchStart()
        {
            CancelInvoke(nameof(Respawn)); // a death in an earlier phase must not warp this player later
            healthController.ResetForRespawn();
            tookDamageThisLife = false;
            movementSim.Reset();
            padManager.ResetForMatchStart();
            hadPadCharge = true;
            weaponState.Equip(currentWeapon);
            isSpawnProtected = false;
            if (!charController.enabled) charController.enabled = true;
            if (bodyMesh != null) bodyMesh.SetActive(true);
            AvatarRigVisual.SetPersistentDamage(bodyMesh, false);
            OnHealthChanged?.Invoke(1f);
            OnAmmoChanged?.Invoke(weaponState.Ammo, currentWeapon.magazineSize);
        }
    }
}



