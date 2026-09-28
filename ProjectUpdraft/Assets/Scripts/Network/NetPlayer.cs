using System.Collections.Generic;
using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;
using Updraft.Config;
using Updraft.Match;
using Updraft.Movement;
using Updraft.UI;
using Updraft.Weapons;

namespace Updraft.Network
{
    /// <summary>
    /// Server-authoritative avatar (spec sections 5-7, 10, 21). Movement runs the
    /// same PlayerMovementSimulator as offline play; SimpleKCC only resolves
    /// collision. The input-authority client predicts its own movement and
    /// weapon cadence; health, damage, pads, scores and eliminations are decided
    /// by the server alone.
    /// </summary>
    [RequireComponent(typeof(SimpleKCC))]
    public class NetPlayer : NetworkBehaviour, IMatchParticipant
    {
        public static readonly List<NetPlayer> All = new List<NetPlayer>();

        [Networked] public int Slot { get; set; }
        [Networked] public int Team { get; set; }
        [Networked] public NetworkString<_32> Alias { get; set; }
        [Networked] public NetworkString<_64> AccountId { get; set; }
        [Networked] public NetworkBool BotControlled { get; set; }
        [Networked] public NetworkBool HadBotSubstitution { get; set; }
        [Networked] public NetworkBool Connected { get; set; }
        [Networked] public TickTimer DisconnectGrace { get; set; }

        [Networked] public float Health { get; set; }
        [Networked] public TickTimer RegenDelay { get; set; }
        [Networked] public NetworkBool Dead { get; set; }
        [Networked] public TickTimer RespawnTimer { get; set; }
        [Networked] public TickTimer Protection { get; set; }

        [Networked] public int WeaponIndex { get; set; }
        [Networked] public int PendingWeapon { get; set; }
        [Networked] public int Ammo { get; set; }
        [Networked] public TickTimer ReloadTimer { get; set; }
        [Networked] public TickTimer FireCooldown { get; set; }
        [Networked] public int ShotCounter { get; set; }

        [Networked] public Vector3 Velocity { get; set; }
        [Networked] public NetworkBool Launched { get; set; }
        [Networked] public NetworkBool HasPadCharge { get; set; }
        [Networked] public TickTimer PadRecharge { get; set; }
        [Networked] public TickTimer ActivationLockout { get; set; }
        [Networked] public TickTimer PingCooldown { get; set; }
        [Networked] public int PadPlacements { get; set; }
        [Networked] public NetworkButtons PreviousButtons { get; set; }

        [Networked] public int EliminationCount { get; set; }
        [Networked] public int DeathCount { get; set; }
        [Networked] public int PadLaunchCount { get; set; }
        [Networked] public float ZoneSeconds { get; set; }
        [Networked] public int LastKillerSlot { get; set; }
        [Networked] public int LastKillerWeapon { get; set; }
        [Networked] public int PresentTicks { get; set; }
        [Networked] public int LastActiveTick { get; set; }
        [Networked] public NetworkBool AfkWarned { get; set; }

        public SimpleKCC Kcc { get; private set; }
        public NetInput LastInput { get; private set; }
        public System.Action<Vector3, Vector3> OnLocalShot;      // predicted FX: muzzle, hit point
        public System.Action OnLocalDamaged;

        PlayerMovementSimulator sim;
        static readonly RaycastHit[] fxHits = new RaycastHit[16];
        static readonly RaycastHit[] pingHits = new RaycastHit[16];

        MovementConfig M => NetConfigs.Movement;
        PadConfig P => NetConfigs.Pad;
        MapConfig Map => NetConfigs.Map;
        public WeaponDefinition Weapon => NetConfigs.Weapons[Mathf.Clamp(WeaponIndex, 0, 2)];
        NetMatch Match => NetMatch.For(Runner);

        // ---- IMatchParticipant ----
        public int ParticipantId => Slot;
        public string DisplayName => Alias.ToString();
        public int TeamId => Team;
        public bool IsBot => BotControlled;
        public bool IsAlive => !Dead;
        public float HealthFraction => Health / M.maxHealth;
        public float RespawnRemaining => Dead ? RespawnTimer.RemainingTime(Runner) ?? 0f : 0f;
        public int Eliminations => EliminationCount;
        public int Deaths => DeathCount;
        public int PadLaunches => PadLaunchCount;
        public float ObjectiveSeconds => ZoneSeconds;
        public Vector3 Position => transform.position;
        public Vector3 GetMuzzlePosition(float yaw) => transform.position +
            Quaternion.Euler(0f, yaw, 0f) * NetAvatarVisual.MuzzleLocalPosition;
        public bool IsSpawnProtected => Protection.IsRunning && !Protection.Expired(Runner);

        public override void Spawned()
        {
            Kcc = GetComponent<SimpleKCC>();
            sim = new PlayerMovementSimulator(M);
            All.Add(this);
            if (HasStateAuthority)
            {
                Health = M.maxHealth;
                Ammo = Weapon.magazineSize;
                HasPadCharge = true;
                PendingWeapon = -1;
                LastKillerSlot = -1;
                LastActiveTick = Runner.Tick;
            }
            Kcc.SetGravity(0f);
            NetAvatarVisual.Attach(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState) => All.Remove(this);

        public override void FixedUpdateNetwork()
        {
            var match = Match;
            if (match == null) return;

            NetInput input = default;
            bool hasInput = GetInput(out input);
            if (HasStateAuthority && BotControlled) { input = NetBotDriver.Decide(this, match); hasInput = true; }
            if (!hasInput)
            {
                // No input this tick (packet loss or disconnected): stand still, keep looking the same way.
                input.Yaw = transform.eulerAngles.y;
                input.Pitch = LastInput.Pitch;
            }
            input.Sanitize();
            LastInput = input;

            if (HasStateAuthority && hasInput && !BotControlled && IsActivity(input)) { LastActiveTick = Runner.Tick; AfkWarned = false; }

            bool live = match.IsLive;
            if (input.WeaponChoice > 0 && (Dead || match.Phase == MatchPhase.Preparation))
                PendingWeapon = input.WeaponChoice - 1;
            if (match.Phase == MatchPhase.Preparation && PendingWeapon >= 0 && PendingWeapon != WeaponIndex)
                EquipWeapon(PendingWeapon);

            TickPad();
            if (Dead) { PreviousButtons = input.Buttons; return; }

            Kcc.SetLookRotation(input.Pitch, input.Yaw);
            if (live) Move(input);
            else { Kcc.ResetVelocity(); Kcc.Move(Vector3.zero, 0f); Velocity = Vector3.zero; }

            if (HasStateAuthority)
            {
                if (transform.position.y < Map.groundLevelY - 10f || !InsideBounds(transform.position))
                    match.Eliminate(this, -1, WeaponIndex);
                if (IsSpawnProtected && !Map.IsInsideSpawnRoom(Team, transform.position)) Protection = TickTimer.None;
                RegenerateHealth();
            }

            if (live) Weapons(input, match);
            if (live && HasStateAuthority && input.Buttons.WasPressed(PreviousButtons, (int)NetButton.Pad)) TryPlacePad(match);
            if (live && HasStateAuthority && input.Buttons.WasPressed(PreviousButtons, (int)NetButton.Ping)) TrySendPing(match, input);
            PreviousButtons = input.Buttons;
        }

        void TrySendPing(NetMatch match, NetInput input)
        {
            if (PingCooldown.IsRunning && !PingCooldown.Expired(Runner)) return;

            Vector3 origin = transform.position + Vector3.up * (M.capsuleHeight * 0.8f);
            Vector3 direction = Quaternion.Euler(input.Pitch, input.Yaw, 0f) * Vector3.forward;
            PingSystem.Classify(this, new Ray(origin, direction), Runner.GetPhysicsScene(), pingHits,
                out PingKind kind, out Vector3 position);

            PingCooldown = TickTimer.CreateFromSeconds(Runner, PingSystem.CooldownSeconds);
            match.BroadcastPing(Slot, Team, kind, position);
        }

        static bool IsActivity(NetInput i) => i.Move.sqrMagnitude > 0.01f || i.Buttons.Bits != 0;

        bool InsideBounds(Vector3 p) =>
            p.x >= Map.boundsMin.x - 5f && p.x <= Map.boundsMax.x + 5f && p.z >= Map.boundsMin.y - 5f && p.z <= Map.boundsMax.y + 5f;

        void Move(NetInput input)
        {
            float dt = Runner.DeltaTime;
            Vector3 forward = Quaternion.Euler(0, input.Yaw, 0) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0, input.Yaw, 0) * Vector3.right;
            bool grounded = Kcc.IsGrounded;
            sim.Restore(Velocity, grounded, Launched && !grounded);

            bool jump = input.Buttons.WasPressed(PreviousButtons, (int)NetButton.Jump);
            if (jump && grounded && !ActivationLockoutRunning())
            {
                var pad = NetPad.FindUsableUnder(Runner, transform.position, P);
                if (pad != null)
                {
                    // Spec 6: launch replaces velocity (never stacks); anyone can use any pad.
                    sim.ApplyLaunchVelocity(pad.LaunchVelocity);
                    ActivationLockout = TickTimer.CreateFromSeconds(Runner, P.perPlayerActivationLockoutSeconds);
                    if (HasStateAuthority) { PadLaunchCount++; pad.NotifyUsed(this); }
                    jump = false;
                }
            }

            var movementInput = new MovementInput
            {
                moveAxis = input.Move,
                jumpOrLaunchPressed = jump,
                isFiring = input.IsDown(NetButton.Fire),
            };
            sim.Tick(movementInput, forward, right, dt);
            if (sim.IsLaunched && input.Move.sqrMagnitude > 0.01f)
                sim.ApplyAirSteer(forward * input.Move.y + right * input.Move.x, P.airSteerDegreesPerSecond, dt);

            Vector3 v = sim.Velocity;
            // SimpleKCC resolves collision only: vertical motion comes from the shared simulator.
            // With zero KCC gravity, reset + impulse sets the dynamic vertical velocity to exactly v.y.
            Kcc.ResetVelocity();
            Kcc.Move(new Vector3(v.x, 0f, v.z), v.y);

            bool nowGrounded = Kcc.IsGrounded;
            float vy = v.y;
            if (nowGrounded && vy <= 0f) vy = 0f;
            else if (vy > 0f && Kcc.RealVelocity.y < vy * 0.5f) vy = 0f; // hit a ceiling
            Velocity = new Vector3(v.x, vy, v.z);
            Launched = sim.IsLaunched && !(nowGrounded && vy <= 0f);
        }

        bool ActivationLockoutRunning() => ActivationLockout.IsRunning && !ActivationLockout.Expired(Runner);

        void Weapons(NetInput input, NetMatch match)
        {
            var weapon = Weapon;
            if (ReloadTimer.IsRunning)
            {
                if (!ReloadTimer.Expired(Runner)) return;
                ReloadTimer = TickTimer.None;
                Ammo = weapon.magazineSize;
            }
            bool reload = input.Buttons.WasPressed(PreviousButtons, (int)NetButton.Reload);
            if (Ammo <= 0 || (reload && Ammo < weapon.magazineSize))
            {
                ReloadTimer = TickTimer.CreateFromSeconds(Runner, weapon.reloadSeconds);
                return;
            }

            bool firing = input.IsDown(NetButton.Fire) && !Map.IsInsideSpawnRoom(Team, transform.position);
            if (!firing || (FireCooldown.IsRunning && !FireCooldown.Expired(Runner))) return;

            FireCooldown = TickTimer.CreateFromSeconds(Runner, weapon.SecondsPerShot);
            Ammo--;
            ShotCounter++;
            Protection = TickTimer.None; // firing ends spawn protection

            Vector3 eye = transform.position + Vector3.up * (M.capsuleHeight * 0.8f);
            Vector3 muzzle = GetMuzzlePosition(input.Yaw);
            Vector3 cameraAimPoint = input.AimPoint;
            if (cameraAimPoint == Vector3.zero)
                cameraAimPoint = eye + Quaternion.Euler(input.Pitch, input.Yaw, 0f) * Vector3.forward * weapon.maxRange;
            Vector3 aim = (cameraAimPoint - muzzle).sqrMagnitude > 0.0001f
                ? (cameraAimPoint - muzzle).normalized
                : Quaternion.Euler(input.Pitch, input.Yaw, 0f) * Vector3.forward;
            var state = !Kcc.IsGrounded ? MovementState.Airborne
                : new Vector2(Velocity.x, Velocity.z).sqrMagnitude > 0.25f ? MovementState.MovingGrounded : MovementState.StationaryGrounded;
            // Server-seeded spread: the seed is derived from shared tick/shot data, so client
            // prediction and the server roll the same pellets (spec section 7).
            uint seed = (uint)(Runner.Tick.Raw * 2654435761u) ^ (uint)(Slot * 40503 + ShotCounter * 97);

            if (HasStateAuthority) match.QueueShot(this, muzzle, aim, seed, state);
            if (HasInputAuthority && Runner.IsForward)
            {
                // Predicted presentation only; the server decides every hit.
                var physics = Runner.GetPhysicsScene();
                Vector3 end = muzzle + aim * weapon.maxRange;
                int count = physics.Raycast(muzzle, aim, fxHits, weapon.maxRange, ~0, QueryTriggerInteraction.Ignore);
                float nearest = weapon.maxRange;
                for (int i = 0; i < count; i++)
                    if (fxHits[i].distance < nearest && fxHits[i].collider.GetComponentInParent<NetPlayer>() != this)
                    { nearest = fxHits[i].distance; end = fxHits[i].point; }
                OnLocalShot?.Invoke(muzzle, end);
            }
        }

        void EquipWeapon(int index)
        {
            WeaponIndex = Mathf.Clamp(index, 0, 2);
            Ammo = Weapon.magazineSize;
            ReloadTimer = TickTimer.None;
        }

        void RegenerateHealth()
        {
            if (Health >= M.maxHealth || (RegenDelay.IsRunning && !RegenDelay.Expired(Runner))) return;
            Health = Mathf.Min(M.maxHealth, Health + M.recoveryPerSecond * Runner.DeltaTime);
        }

        void TickPad()
        {
            if (!HasStateAuthority || HasPadCharge) return;
            if (PadRecharge.Expired(Runner)) { HasPadCharge = true; PadRecharge = TickTimer.None; }
        }

        void TryPlacePad(NetMatch match)
        {
            if (!Updraft.Core.StudySettings.PadsEnabled || !HasPadCharge || !Kcc.IsGrounded) return;
            Vector3 forward = Quaternion.Euler(0, LastInput.Yaw, 0) * Vector3.forward;
            var physics = Runner.GetPhysicsScene();
            if (Updraft.Pads.PadPlacementRules.ResolvePosition(physics, transform.position, forward, P, out var position) != null) return;
            if (Updraft.Pads.PadPlacementRules.Validate(physics, position, forward, NetPad.ActiveCountExcludingOwner(Runner, Slot), P, Map, M) != null) return;
            if (PadPlacements >= P.maxActivePadsPerMatch * 10) return; // sanity bound against a modified client
            match.SpawnPad(this, position, forward);
            HasPadCharge = false;
            PadPlacements++;
            // Death never refreshes the cooldown: the timer runs from placement.
            PadRecharge = TickTimer.CreateFromSeconds(Runner, P.rechargeSeconds);
        }

        // ---- Server-only state changes (called by NetMatch) ----

        public void ServerDamage(float amount, int attackerSlot, int weapon)
        {
            if (Dead || IsSpawnProtected) return;
            Health -= amount;
            RegenDelay = TickTimer.CreateFromSeconds(Runner, M.recoveryDelayAfterDamage);
            if (Health <= 0f) Match.Eliminate(this, attackerSlot, weapon);
        }

        public void ServerKill(int killerSlot, int weapon)
        {
            Dead = true;
            Health = 0f;
            DeathCount++;
            LastKillerSlot = killerSlot;
            LastKillerWeapon = weapon;
            Velocity = Vector3.zero;
            Launched = false;
            RespawnTimer = TickTimer.CreateFromSeconds(Runner, M.respawnDelaySeconds);
        }

        public void ServerRespawn(Pose spawn, bool matchStart)
        {
            if (PendingWeapon >= 0) EquipWeapon(PendingWeapon);
            PendingWeapon = -1;
            Kcc.SetPosition(spawn.position);
            Kcc.SetLookRotation(0f, spawn.rotation.eulerAngles.y);
            Dead = false;
            Health = M.maxHealth;
            Ammo = Weapon.magazineSize;
            ReloadTimer = TickTimer.None;
            Velocity = Vector3.zero;
            Launched = false;
            RespawnTimer = TickTimer.None;
            Protection = matchStart ? TickTimer.None : TickTimer.CreateFromSeconds(Runner, M.respawnProtectionSeconds);
            if (matchStart)
            {
                HasPadCharge = true;
                PadRecharge = TickTimer.None;
                ActivationLockout = TickTimer.None;
                EliminationCount = DeathCount = PadLaunchCount = PadPlacements = 0;
                ZoneSeconds = 0f;
                PresentTicks = 0;
            }
        }
    }
}
