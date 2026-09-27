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
    public class PlayerController : MonoBehaviour
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
        [SerializeField] SkinnedMeshRenderer bodyRenderer;
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

        // External (bot) input override. Humans drive via touchInput; bots drive
        // via SetExternalInput so BotController's decisions actually reach fire/
        // reload/jump instead of silently doing nothing (spec section 12: bots
        // are driven through the same input interface as humans).
        bool useExternalInput;
        Vector2 externalMoveAxis;
        bool externalJumpPressed;
        bool externalFireHeld;
        bool externalReloadPressed;

        public void SetExternalInput(Vector2 moveAxis, bool jumpPressed, bool fireHeld, bool reloadPressed)
        {
            useExternalInput = true;
            externalMoveAxis = moveAxis;
            externalJumpPressed = jumpPressed;
            externalFireHeld = fireHeld;
            externalReloadPressed = reloadPressed;
        }

        // Aim assist (spec section 8: slowdown only, ≤3° cone, ≤25m, sensitivity drops to 70%)
        [Header("Aim Assist")]
        [SerializeField] bool aimAssistEnabled = true;
        [SerializeField] float aimAssistConeDegrees = 3f;
        [SerializeField] float aimAssistMaxDistance = 25f;
        [SerializeField] float aimAssistSensitivityMultiplier = 0.7f;

        // Events
        public System.Action<int, Vector3, Vector3> OnFire; // weaponIndex, muzzlePos, aimDir
        public System.Action<PlayerController> OnDeath;
        public System.Action<PlayerController> OnRespawn;
        public System.Action<float> OnHealthChanged; // 0-1
        public System.Action<int, int> OnAmmoChanged; // current, max
        public System.Action<LaunchPad> OnPadPlaced;
        public System.Action<LaunchPad> OnPadActivated;
        public System.Action OnPadChargeReady;

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
        public bool CanActivatePad => padManager.CanActivate;
        public int TeamId => teamId;
        public int PlayerId => playerId;

        void Awake()
        {
            charController = GetComponent<CharacterController>();
            audioSource = GetComponent<AudioSource>();

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

            // Subscribe to input
            if (touchInput != null)
            {
                touchInput.OnPingWorldPositionRequested += HandlePingRequest;
            }
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

            if (startingWeapon != null)
            {
                currentWeapon = startingWeapon;
                currentWeaponIndex = System.Array.IndexOf(weaponDefinitions, currentWeapon);
                if (currentWeaponIndex < 0) currentWeaponIndex = 0;
            }

            currentAmmo = currentWeapon.magazineSize;
            SetTeamMaterial(teamId);
            UpdateWeaponVisual();
        }

        public void SetWeapon(int weaponIndex)
        {
            if (weaponIndex < 0 || weaponIndex >= weaponDefinitions.Length) return;
            if (!healthController.IsAlive) return; // no switching while alive per spec

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

        void Update()
        {
            if (!healthController.IsAlive) return;

            // Tick simulation
            float dt = Time.deltaTime;
            movementSim.Tick(GetMovementInput(), GetFacingForward(), GetFacingRight(), dt);
            healthController.Tick(dt);
            padManager.Tick(dt);

            // Apply movement via CharacterController
            Vector3 displacement = movementSim.Velocity * dt;
            CollisionFlags flags = charController.Move(displacement);
            movementSim.IsGrounded = (flags & CollisionFlags.CollidedBelow) != 0;

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
            HandlePadInteraction();

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
                Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, 0.5f))
                {
                    var pad = hit.collider.GetComponent<LaunchPadBehaviour>();
                    if (pad != null && pad.PadData.CanBeUsed)
                    {
                        // Launch!
                        Vector3 launchVel = pad.PadData.GetLaunchVelocity();
                        movementSim.ApplyLaunchVelocity(launchVel);
                        pad.NotifyActivated();
                        padManager.NotifyActivated();
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
            if (currentWeapon == null) return;

            bool fireHeld = FireHeld;

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
            OnAmmoChanged?.Invoke(currentAmmo, currentWeapon.magazineSize);

            // Determine movement state for spread
            MovementState moveState;
            if (!movementSim.IsGrounded) moveState = MovementState.Airborne;
            else if (new Vector3(movementSim.Velocity.x, 0, movementSim.Velocity.z).magnitude > 0.5f) moveState = MovementState.MovingGrounded;
            else moveState = MovementState.StationaryGrounded;

            // Get aim direction with aim assist
            Vector3 aimDirection = GetAimDirectionWithAssist();

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

        Vector3 GetAimDirectionWithAssist()
        {
            Vector3 aimDir = playerCamera != null ? playerCamera.transform.forward : transform.forward;

            if (!aimAssistEnabled || playerCamera == null) return aimDir;

            // Check for enemies in aim assist cone
            Ray ray = new Ray(playerCamera.transform.position, aimDir);
            if (Physics.SphereCast(ray, Mathf.Tan(aimAssistConeDegrees * Mathf.Deg2Rad) * aimAssistMaxDistance, out RaycastHit hit, aimAssistMaxDistance, LayerMask.GetMask("Player")))
            {
                var target = hit.collider.GetComponent<PlayerController>();
                if (target != null && target.TeamId != this.teamId && target.IsAlive)
                {
                    // Slow down sensitivity when near target
                    Vector3 toTarget = (target.transform.position + Vector3.up * 1.5f) - playerCamera.transform.position;
                    float angle = Vector3.Angle(aimDir, toTarget.normalized);
                    if (angle <= aimAssistConeDegrees)
                    {
                        // Return direction toward target (slowdown effect handled by input sensitivity)
                        return toTarget.normalized;
                    }
                }
            }

            return aimDir;
        }

        void ProcessPellet(Vector3 direction)
        {
            // Raycast for hit
            Ray ray = new Ray(muzzleTransform.position, direction);
            float maxRange = currentWeapon.maxRange;

            if (Physics.Raycast(ray, out RaycastHit hit, maxRange, ~0, QueryTriggerInteraction.Ignore))
            {
                var target = hit.collider.GetComponent<PlayerController>();
                if (target != null && target.IsAlive && target.TeamId != this.teamId)
                {
                    // Check headshot
                    bool isHeadshot = hit.collider.CompareTag("Head");

                    float damage = ShotResolver.GetDamage(currentWeapon, hit.distance, isHeadshot);
                    target.ApplyDamage(damage, this.playerId, currentWeapon.id);

                    // Hit confirmation feedback
                    // TODO: hit marker, sound
                }
                else if (hit.collider.GetComponent<LaunchPadBehaviour>() != null)
                {
                    var padBehaviour = hit.collider.GetComponent<LaunchPadBehaviour>();
                    // Damage pad (enemies can destroy, friendly fire can't per spec)
                    if (padBehaviour.PadData.TeamId != this.teamId)
                    {
                        padBehaviour.PadData.ApplyDamage(currentWeapon.bodyDamage); // simplified
                    }
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
            isReloading = true;
            reloadTimer = currentWeapon.reloadSeconds;
            // Play reload sound
        }

        public void ApplyDamage(float amount, int attackerId, WeaponId weaponId)
        {
            if (!healthController.IsAlive) return;

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
            OnDeath?.Invoke(this);

            // Disable input, start respawn timer
            enabled = false;
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
            currentAmmo = currentWeapon.magazineSize;
            isReloading = false;
            OnAmmoChanged?.Invoke(currentAmmo, currentWeapon.magazineSize);
            OnHealthChanged?.Invoke(1f);

            // Spawn protection
            StartCoroutine(SpawnProtectionRoutine());

            enabled = true;
            OnRespawn?.Invoke(this);
        }

        System.Collections.IEnumerator SpawnProtectionRoutine()
        {
            // TODO: Implement spawn protection visual feedback
            yield return new WaitForSeconds(movementConfig.respawnProtectionSeconds);
        }

        public void TryPlacePad()
        {
            if (!padManager.HasCharge) return;

            // Calculate placement position (~1.6m ahead facing camera horizontal)
            Vector3 forward = GetFacingForward();
            Vector3 position = transform.position + forward * padConfig.placementForwardOffset;
            position.y = mapConfig.groundLevelY + 0.05f; // slight offset from ground

            // Validate placement (simplified - real version uses PadPlacementValidator with raycasts)
            if (CanPlacePadAt(position, forward))
            {
                var pad = padManager.PlaceNew(playerId, teamId, position, forward);
                if (pad != null)
                {
                    // Spawn pad visual
                    SpawnPadVisual(pad);
                    OnPadPlaced?.Invoke(pad);
                }
            }
        }

        bool CanPlacePadAt(Vector3 position, Vector3 forward)
        {
            // Check surface tilt
            if (Physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 1f))
            {
                float tilt = Vector3.Angle(hit.normal, Vector3.up);
                if (tilt > padConfig.maxSurfaceTiltDegrees) return false;
            }

            // Check spawn exclusion
            Vector3 spawnCenter = teamId == 0 ? mapConfig.northSpawnCenter : mapConfig.southSpawnCenter;
            if (Vector3.Distance(position, spawnCenter) < mapConfig.spawnRoomSize.magnitude * 0.5f + padConfig.noPlacementNearSpawnExitRadius)
                return false;

            // Check capture circle exclusion
            Vector3[] zones = { mapConfig.centerZone, mapConfig.westZone, mapConfig.eastZone };
            foreach (var zone in zones)
            {
                if (Vector3.Distance(new Vector2(position.x, position.z), new Vector2(zone.x, zone.z)) < mapConfig.zoneRadius + padConfig.noPlacementInsideCaptureRadius)
                    return false;
            }

            // Check overhead obstruction
            if (Physics.Raycast(position + Vector3.up * 0.1f, Vector3.up, out _, padConfig.launchUpwardVelocity * 0.5f))
                return false;

            return true;
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

            var padBehaviour = padObj.AddComponent<LaunchPadBehaviour>();
            padBehaviour.Initialize(pad);

            // Set material by team
            var renderer = padObj.GetComponent<Renderer>();
            renderer.material = teamId == 0 ? friendlyMaterial : enemyMaterial;
        }

        void HandlePingRequest(Vector2 screenPos)
        {
            // Convert screen position to world ray
            if (playerCamera != null)
            {
                Ray ray = playerCamera.GetComponent<UnityEngine.Camera>().ScreenPointToRay(screenPos);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    // TODO: Send ping to teammates
                }
            }
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
                bodyRenderer.material = team == 0 ? friendlyMaterial : enemyMaterial;
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
            // TODO: Play weapon-specific fire sound
        }

        public void SetAimAssistEnabled(bool enabled)
        {
            aimAssistEnabled = enabled;
            PlayerPrefs.SetInt("AimAssistEnabled", enabled ? 1 : 0);
        }
    }
}