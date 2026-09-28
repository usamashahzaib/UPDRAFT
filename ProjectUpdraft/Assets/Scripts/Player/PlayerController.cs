using UnityEngine;
using UnityEngine.InputSystem;
using Updraft.Config;
using Updraft.Movement;
using Updraft.Pads;
using Updraft.Weapons;
using Updraft.Input;
using Updraft.Camera;

namespace Updraft.Player
{
    /// <summary>
    /// MonoBehaviour wrapper for player movement, health, weapons, and pads.
    /// Bridges the pure C# simulation logic (PlayerMovementSimulator, PlayerHealthController,
    /// PlayerPadManager, ShotResolver) with Unity's physics/rendering.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class PlayerController : MonoBehaviour, Updraft.Match.IMatchParticipant
    {
        [Header("Config")]
        [SerializeField] MovementConfig movementConfig;
        [SerializeField] WeaponDefinition[] weaponDefinitions; // ARC, ZIP, BRICK
        [SerializeField] PadConfig padConfig;
        [SerializeField] MapConfig mapConfig;

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
        int currentAmmo;
        float reloadTimer;
        bool isReloading;
        bool wasGrounded;
        float timeSinceLastFootstep;
        float fireTimer;
        int teamId = 0; // 0 = TeamA (friendly), 1 = TeamB (enemy)
        int playerId;
        bool movementAllowed = true;
        bool combatAllowed = true;
        bool padPlacementAllowed = true;
        bool weaponSelectionAllowed = true;
        bool isSpawnProtected;
        float spawnProtectionRemaining;
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
        void OnDisable() => All.Remove(this);

        public static PlayerController Find(int id)
        {
            for (int i = 0; i < All.Count; i++) if (All[i].playerId == id) return All[i];
            return null;
        }

        int Updraft.Match.IMatchParticipant.ParticipantId => playerId;
        public float HealthFraction => healthController.CurrentHealth / movementConfig.maxHealth;
        public Vector3 Position => transform.position;
        public string DisplayName { get; set; } = "Player";
        public bool IsBot { get; set; }

        // Per-match stats for the results screen and personal bests.
        public int Eliminations { get; private set; }
        public int Deaths { get; private set; }
        public int PadLaunches { get; private set; }
        public float ObjectiveSeconds { get; set; }
        public float RespawnRemaining => IsAlive ? 0f : Mathf.Max(0f, respawnAt - Time.time);
        public int LastKillerId { get; private set; } = -1;
        public float LastShotTime { get; private set; } = -100f;
        float respawnAt;
        float nextLowHealthCue;

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
        public System.Action<PlayerController> OnDeath;
        public System.Action<PlayerController> OnRespawn;
        public System.Action<float> OnHealthChanged; // 0-1
        public System.Action<int, int> OnAmmoChanged; // current, max
        public System.Action<LaunchPad> OnPadPlaced;
        public System.Action<LaunchPad> OnPadActivated;
        public System.Action OnPadChargeReady;
        public System.Action OnShotBlocked;
        public System.Action OnHitConfirmed;
        public System.Action<int, WeaponId> OnEliminated;

        public bool IsAlive => healthController.IsAlive;
        public bool IsGrounded => movementSim.IsGrounded;
        public float CurrentHealth => healthController.CurrentHealth;
        public float MaxHealth => movementConfig.maxHealth;
        public int CurrentAmmo => currentAmmo;
        public int MaxAmmo => currentWeapon?.magazineSize ?? 0;
        public bool IsReloading => isReloading;
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
            charController = GetComponent<CharacterController>();
            audioSource = GetComponent<AudioSource>();
            audioSource.priority = 20; // threats (shots, footsteps, damage) outrank everything else
            audioSource.spatialBlend = 1f; // stereo position + distance attenuation
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 3f;
            audioSource.maxDistance = 50f;
            audioSource.dopplerLevel = 0f;
            if (footstepClips == null || footstepClips.Length == 0) footstepClips = new[] { Updraft.Core.OriginalGameAudio.Get(Updraft.Core.GameCue.Step) };
            if (landClip == null) landClip = Updraft.Core.OriginalGameAudio.Get(Updraft.Core.GameCue.Land);
            if (jumpClip == null) jumpClip = Updraft.Core.OriginalGameAudio.Get(Updraft.Core.GameCue.Jump);
            if (damageClip == null) damageClip = Updraft.Core.OriginalGameAudio.Get(Updraft.Core.GameCue.Hurt);
            if (deathClip == null) deathClip = Updraft.Core.OriginalGameAudio.Get(Updraft.Core.GameCue.Elimination);

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
                currentAmmo = currentWeapon.magazineSize;
            }

            // Setup visuals
            UpdateWeaponVisual();
            SetTeamMaterial(teamId);

            BindTouchInput();
        }

        void Start()
        {
            // Load aim assist setting
            aimAssistEnabled = PlayerPrefs.GetInt("AimAssistEnabled", 1) == 1;
        }

        public void Initialize(int playerId, int teamId, WeaponDefinition startingWeapon = null)
        {
            this.playerId = playerId;
            this.teamId = teamId;
            Updraft.Map.SpawnExitBarrier.ConfigurePlayer(this);

            if (startingWeapon != null)
            {
                currentWeapon = startingWeapon;
                currentWeaponIndex = System.Array.IndexOf(weaponDefinitions, currentWeapon);
                if (currentWeaponIndex < 0) currentWeaponIndex = 0;
            }

            currentAmmo = currentWeapon.magazineSize;
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
            if (padPreview != null) Destroy(padPreview.gameObject);
            if (touchInput != null)
                touchInput.OnPingWorldPositionRequested -= HandlePingRequest;
        }

        public void SetWeapon(int weaponIndex)
        {
            if (weaponIndex < 0 || weaponIndex >= weaponDefinitions.Length) return;
            if (healthController.IsAlive && !weaponSelectionAllowed) return;

            currentWeaponIndex = weaponIndex;
            currentWeapon = weaponDefinitions[weaponIndex];
            currentAmmo = currentWeapon.magazineSize;
            isReloading = false;
            reloadTimer = 0f;
            UpdateWeaponVisual();
            OnAmmoChanged?.Invoke(currentAmmo, currentWeapon.magazineSize);
        }

        /// <summary>Used by tutorial scripting to force a specific ammo count (e.g. the reload step).</summary>
        public void SetAmmo(int amount)
        {
            currentAmmo = Mathf.Clamp(amount, 0, currentWeapon.magazineSize);
            OnAmmoChanged?.Invoke(currentAmmo, currentWeapon.magazineSize);
        }

        public void WarpForTraining(Vector3 position, Quaternion rotation)
        {
            charController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            movementSim.Reset();
            charController.enabled = true;
            playerCamera?.SetTarget(transform);
        }

        void Update()
        {
            float dt = Time.deltaTime;
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
            if (touchInput != null && healthController.CurrentHealth <= movementConfig.maxHealth * 0.3f && Time.time >= nextLowHealthCue)
            {
                nextLowHealthCue = Time.time + 1.2f;
                Updraft.Core.GameAudio.Play2D(Updraft.Core.GameCue.LowHealth, 0.7f);
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

            // Update camera pivot position (upper torso)
            if (cameraPivot != null)
            {
                cameraPivot.position = transform.position + Vector3.up * movementConfig.capsuleHeight * 0.8f;
            }
        }

        void UpdateCameraInput()
        {
            if (touchInput == null || playerCamera == null) return;

            touchInput.AimSensitivityScale = IsAimAssistRegionActive()
                ? AimAssistRules.SensitivityMultiplier
                : 1f;

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

        bool IsInsideOwnSpawnRoom()
        {
            Vector3 center = teamId == 0 ? mapConfig.northSpawnCenter : mapConfig.southSpawnCenter;
            Vector3 half = mapConfig.spawnRoomSize * 0.5f;
            Vector3 offset = transform.position - center;
            return Mathf.Abs(offset.x) <= half.x && Mathf.Abs(offset.z) <= half.z;
        }

        MovementInput GetMovementInput()
        {
            if (useExternalInput)
            {
                return new MovementInput
                {
                    moveAxis = externalMoveAxis,
                    jumpOrLaunchPressed = externalJumpPressed,
                    isFiring = externalFireHeld
                };
            }

            var input = new MovementInput
            {
                moveAxis = touchInput?.MoveInput ?? Vector2.zero,
                jumpOrLaunchPressed = touchInput?.JumpPressed ?? false,
                isFiring = touchInput?.FireHeld ?? false
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
                        pad.NotifyActivated();
                        padManager.NotifyActivated();
                        PadLaunches++;
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

            bool fireHeld = FireHeld && !IsInsideOwnSpawnRoom();

            if (isReloading)
            {
                reloadTimer -= dt;
                if (reloadTimer <= 0f)
                {
                    isReloading = false;
                    currentAmmo = currentWeapon.magazineSize;
                    OnAmmoChanged?.Invoke(currentAmmo, currentWeapon.magazineSize);
                }
                return;
            }

            if (currentAmmo <= 0)
            {
                // Auto-reload (spec: auto-reload default)
                StartReload();
                return;
            }

            if (fireHeld)
            {
                float timeBetweenShots = currentWeapon.SecondsPerShot;
                fireTimer += dt;

                if (fireTimer >= timeBetweenShots)
                {
                    fireTimer = 0f;
                    FireShot();
                }
            }
        }

        void FireShot()
        {
            if (currentWeapon == null || currentAmmo <= 0) return;

            currentAmmo--;
            LastShotTime = Time.time;
            OnAmmoChanged?.Invoke(currentAmmo, currentWeapon.magazineSize);

            // Determine movement state for spread
            MovementState moveState;
            if (!movementSim.IsGrounded) moveState = MovementState.Airborne;
            else if (new Vector3(movementSim.Velocity.x, 0, movementSim.Velocity.z).magnitude > 0.5f) moveState = MovementState.MovingGrounded;
            else moveState = MovementState.StationaryGrounded;

            // Camera chooses the aim point; the real shot is traced from the muzzle
            // so nearby walls still block a camera-visible target.
            Vector3 aimDirection = GetMuzzleAimDirection();

            // Generate shot seed (in real networked game, server provides this)
            uint shotSeed = (uint)Random.Range(0, int.MaxValue);

            // Resolve pellets
            var pellets = ShotResolver.ResolvePellets(currentWeapon, aimDirection, shotSeed, moveState);

            // Fire each pellet (in offline mode, we process locally)
            foreach (var pellet in pellets)
            {
                ProcessPellet(pellet.direction);
            }

            // Recoil
            ApplyRecoil(currentWeapon.verticalRecoilDegreesPerShot);

            // Muzzle flash, sound, etc.
            PlayWeaponFireSound();
            OnFire?.Invoke(currentWeaponIndex, muzzleTransform.position, aimDirection);
        }

        Vector3 GetMuzzleAimDirection()
        {
            if (playerCamera == null || muzzleTransform == null) return hasExternalAim ? externalAimDirection : transform.forward;
            Vector3 aimPoint = playerCamera.GetAimPoint(currentWeapon.maxRange, ~0);
            Vector3 direction = aimPoint - muzzleTransform.position;
            float distance = direction.magnitude;
            if (distance > 0.01f)
            {
                var hits = Physics.RaycastAll(muzzleTransform.position, direction.normalized, distance, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    if (hit.collider.GetComponentInParent<PlayerController>() == this) continue;
                    if (hit.distance < distance - 0.1f) OnShotBlocked?.Invoke();
                    break;
                }
            }
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : playerCamera.transform.forward;
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
                if (Vector3.Angle(playerCamera.transform.forward, toTarget) > AimAssistRules.ConeDegrees) continue;

                if (Physics.Raycast(playerCamera.transform.position, toTarget.normalized, out RaycastHit hit,
                    toTarget.magnitude, ~0, QueryTriggerInteraction.Ignore) &&
                    hit.collider.GetComponentInParent<PlayerController>() != target)
                    continue;

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
                    OnHitConfirmed?.Invoke();
                    if (touchInput != null)
                        Updraft.Core.GameAudio.Play2D(target.IsAlive ? Updraft.Core.GameCue.Hit : Updraft.Core.GameCue.EliminationConfirm, 0.8f);
                }
                else if (hit.collider.GetComponentInParent<LaunchPadBehaviour>() != null)
                {
                    var padBehaviour = hit.collider.GetComponentInParent<LaunchPadBehaviour>();
                    // Damage pad (enemies can destroy, friendly fire can't per spec)
                    if (padBehaviour.PadData.TeamId != this.teamId)
                    {
                        padBehaviour.ApplyDamage(ShotResolver.GetDamage(currentWeapon, hit.distance, false));
                    }
                }
                else if (hit.collider.GetComponentInParent<Updraft.Tutorial.TutorialTarget>() is Updraft.Tutorial.TutorialTarget trainingTarget)
                {
                    trainingTarget.TakeDamage(ShotResolver.GetDamage(currentWeapon, hit.distance, false));
                    OnHitConfirmed?.Invoke();
                }
            }
        }

        void ApplyRecoil(float verticalDegrees)
        {
            if (playerCamera != null)
            {
                playerCamera.AddYawPitch(0, -verticalDegrees);
            }
        }

        void HandleReload(float dt)
        {
            if (ReloadPressed && !isReloading && currentAmmo < currentWeapon.magazineSize)
            {
                StartReload();
            }
        }

        void StartReload()
        {
            if (currentAmmo == 0) PlaySound(Updraft.Core.OriginalGameAudio.Get(Updraft.Core.GameCue.Empty), 0.6f);
            isReloading = true;
            reloadTimer = currentWeapon.reloadSeconds;
            PlaySound(Updraft.Core.OriginalGameAudio.Get(Updraft.Core.GameCue.Reload), 0.55f);
        }

        public void ApplyDamage(float amount, int attackerId, WeaponId weaponId)
        {
            if (!healthController.IsAlive || !combatAllowed || isSpawnProtected) return;

            healthController.ApplyDamage(amount);
            OnHealthChanged?.Invoke(healthController.CurrentHealth / movementConfig.maxHealth);

            PlaySound(damageClip);

            if (!healthController.IsAlive)
            {
                Die(attackerId, weaponId);
            }
        }

        void Die(int killerId, WeaponId weaponId)
        {
            PlaySound(deathClip);
            Deaths++;
            LastKillerId = killerId;
            var killer = Find(killerId);
            if (killer != null && killer != this) killer.Eliminations++;
            OnEliminated?.Invoke(killerId, weaponId);
            OnDeath?.Invoke(this);

            charController.enabled = false;
            if (bodyMesh != null) bodyMesh.SetActive(false);
            respawnAt = Time.time + movementConfig.respawnDelaySeconds;
            Invoke(nameof(Respawn), movementConfig.respawnDelaySeconds);
        }

        void Respawn()
        {
            // Find spawn point based on team
            Vector3 spawnPos = teamId == 0 ? mapConfig.northSpawnCenter : mapConfig.southSpawnCenter;
            spawnPos.y = mapConfig.groundLevelY + 2f; // spawn slightly above ground

            transform.position = spawnPos;
            charController.enabled = false;
            transform.position = spawnPos;
            charController.enabled = true;

            healthController.ResetForRespawn();
            movementSim.Reset();
            currentAmmo = currentWeapon.magazineSize;
            isReloading = false;
            OnAmmoChanged?.Invoke(currentAmmo, currentWeapon.magazineSize);
            OnHealthChanged?.Invoke(1f);

            if (bodyMesh != null) bodyMesh.SetActive(true);
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
            if (!Updraft.Core.StudySettings.PadsEnabled) return "PADS DISABLED IN THIS VERSION";
            if (!padPlacementAllowed) return "NOT DURING THIS PHASE";
            if (!padManager.HasCharge) return "RECHARGING";
            if (!movementSim.IsGrounded) return "MUST BE GROUNDED";
            var physics = Physics.defaultPhysicsScene;
            string reason = PadPlacementRules.ResolvePosition(physics, transform.position, forward, padConfig, out position);
            if (reason != null) return reason;
            int activeOther = 0;
            foreach (var existing in FindObjectsByType<LaunchPadBehaviour>(FindObjectsSortMode.None))
                if (existing.PadData != null && existing.PadData.OwnerId != playerId &&
                    (existing.PadData.State == PadState.Arming || existing.PadData.State == PadState.Armed)) activeOther++;
            return PadPlacementRules.Validate(physics, position, forward, activeOther, padConfig, mapConfig, movementConfig);
        }

        LineRenderer padPreview;
        readonly Vector3[] previewPoints = new Vector3[32];

        // Hold-to-preview: draw the launch arc from the would-be pad, green if valid, red if not.
        void UpdatePadPreview()
        {
            bool show = touchInput != null && touchInput.PadPreviewActive && IsAlive;
            if (!show) { if (padPreview != null && padPreview.enabled) padPreview.enabled = false; return; }
            if (padPreview == null)
            {
                padPreview = new GameObject("Pad preview").AddComponent<LineRenderer>();
                padPreview.useWorldSpace = true;
                padPreview.startWidth = 0.12f; padPreview.endWidth = 0.05f;
                padPreview.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                padPreview.sharedMaterial = Resources.Load<Material>("Materials/Objective");
                padPreview.material = new Material(padPreview.sharedMaterial);
            }
            string reason = ResolvePadPlacement(out Vector3 position, out Vector3 forward);
            Vector3 velocity = forward * padConfig.launchHorizontalVelocity + Vector3.up * padConfig.launchUpwardVelocity;
            Vector3 point = position + Vector3.up * 0.1f;
            int count = 0;
            for (; count < previewPoints.Length; count++)
            {
                previewPoints[count] = point;
                velocity += Vector3.down * movementConfig.gravity * 0.06f;
                point += velocity * 0.06f;
                if (point.y < position.y - 0.5f) { count++; break; }
            }
            padPreview.positionCount = count;
            padPreview.SetPositions(previewPoints);
            padPreview.material.color = reason == null ? new Color(0.4f, 1f, 0.55f) : new Color(1f, 0.39f, 0.46f);
            padPreview.enabled = true;
            touchInput.PadPreviewReason = reason;
        }

        void SpawnPadVisual(LaunchPad pad)
        {
            // TODO: Instantiate pad prefab at pad.Position with pad.ForwardDirection
            // For now, create a simple cylinder
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
            if (playerCamera == null || Updraft.UI.PingSystem.Instance == null) return;
            Ray ray = playerCamera.GetComponent<UnityEngine.Camera>().ScreenPointToRay(screenPos);
            if (!Updraft.UI.PingSystem.Instance.RequestPing(this, ray))
                Updraft.Core.GameAudio.Play2D(Updraft.Core.GameCue.UiTap, 0.4f); // rate-limited
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
        }

        void SetTeamMaterial(int team)
        {
            if (bodyRenderer != null)
            {
                bool friendly = LaunchPadBehaviour.IsFriendlyToViewer(team);
                var material = friendly ? friendlyMaterial : enemyMaterial;
                if (material != null) bodyRenderer.sharedMaterial = material;
                else bodyRenderer.material.color = friendly
                    ? new Color(0.188f, 0.835f, 0.91f) : new Color(1f, 0.388f, 0.463f);
            }
        }

        void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }

        void PlayWeaponFireSound()
        {
            PlaySound(Updraft.Core.OriginalGameAudio.Get((Updraft.Core.GameCue)currentWeaponIndex), 0.85f);
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
            healthController.ResetForRespawn();
            movementSim.Reset();
            padManager.ResetForMatchStart();
            hadPadCharge = true;
            currentAmmo = currentWeapon.magazineSize;
            isReloading = false;
            fireTimer = currentWeapon.SecondsPerShot;
            isSpawnProtected = false;
            if (!charController.enabled) charController.enabled = true;
            if (bodyMesh != null) bodyMesh.SetActive(true);
            OnHealthChanged?.Invoke(1f);
            OnAmmoChanged?.Invoke(currentAmmo, currentWeapon.magazineSize);
        }
    }
}
