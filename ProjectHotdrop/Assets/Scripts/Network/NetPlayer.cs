using System.Collections.Generic;
using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;
using Hotdrop.Config;
using Hotdrop.Match;
using Hotdrop.Movement;
using Hotdrop.UI;
using Hotdrop.Weapons;

namespace Hotdrop.Network
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
        // C19: the alias this seat had while it was still a human. MarkBotSubstitution rewrites
        // Alias to carry a "[BOT] " label (and trims it to fit), so Alias can no longer identify
        // the account for support reports. This snapshot is written once, on the server, when the
        // seat is a real player, and is never relabelled — moderation stays resolvable.
        [Networked] public NetworkString<_32> HumanAliasAtJoin { get; set; }
        // Amendment 3: body style | weapon wrap | badge | body rig. Never an account id.
        [Networked] public NetworkString<_64> Cosmetic { get; set; } // QA-024: shown on this avatar to everyone
        // Server-only: the backend account behind this seat. Never replicated — clients have no
        // need for another player's internal id (data minimisation; reports resolve by match slot).
        public string AccountId { get; set; } = "";
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
        [Networked] public float FireDebt { get; set; }       // QA-009: seconds until the next shot, remainder carried
        [Networked] public int ShotCounter { get; set; }

        [Networked] public Vector3 Velocity { get; set; }
        [Networked] public NetworkBool Launched { get; set; }
        [Networked] public NetworkBool HasPadCharge { get; set; }
        [Networked] public TickTimer PadRecharge { get; set; }
        [Networked] public TickTimer ActivationLockout { get; set; }
        [Networked] public TickTimer PingCooldown { get; set; }
        [Networked] public int PadPlacements { get; set; }
        [Networked] public byte PadRejectCode { get; set; }    // QA-006: why the last placement failed
        [Networked] public int PadRejectCounter { get; set; }
        [Networked] public NetworkButtons PreviousButtons { get; set; }

        [Networked] public int EliminationCount { get; set; }
        [Networked] public int HitConfirmCounter { get; set; } // QA-010: bumps when this player lands damage
        [Networked] public int DeathCount { get; set; }
        [Networked] public int PadLaunchCount { get; set; }
        [Networked] public float ZoneSeconds { get; set; }
        // C04: server-only progress facts. Never replicated — they feed only the
        // authenticated result that advances contracts/mastery. UncontestedZoneSeconds is
        // the time this player's team was the *sole* occupant of the scoring zone; the
        // per-weapon map is the server's own record of time spent holding each weapon.
        public float UncontestedZoneSeconds { get; set; }
        readonly Dictionary<string, float> weaponSeconds = new Dictionary<string, float>();
        [Networked] public int LastKillerSlot { get; set; }
        [Networked] public int LastKillerWeapon { get; set; }
        [Networked] public byte LastKillerDirection { get; set; } // 0 unknown, 1 front, 2 right, 3 behind, 4 left
        [Networked] public int PresentTicks { get; set; }
        [Networked] public int LastActiveTick { get; set; }
        [Networked] public NetworkBool AfkWarned { get; set; }
        [Networked] public NetworkBool AfkRemoved { get; set; } // spec 18: removed for inactivity; seat stays a bot

        public SimpleKCC Kcc { get; private set; }
        public NetInput LastInput { get; private set; }
        public System.Action<Vector3, Vector3> OnLocalShot;      // predicted FX per pellet: muzzle, hit point
        public System.Action OnLocalFired;                       // once per trigger pull (recoil, fire sound)
        public System.Action OnLocalDamaged;

        PlayerMovementSimulator sim;
        static readonly RaycastHit[] fxHits = new RaycastHit[16];
        static readonly RaycastHit[] pingHits = new RaycastHit[16];
        readonly PelletResult[] pelletBuffer = new PelletResult[16];

        MovementConfig M => NetConfigs.Movement;
        PadConfig P => NetConfigs.Pad;
        MapConfig Map => NetConfigs.Map;
        public WeaponDefinition Weapon => NetConfigs.Weapons[Mathf.Clamp(WeaponIndex, 0, 2)];
        NetMatch Match => NetMatch.For(Runner);

        // ---- IMatchParticipant ----
        public int ParticipantId => Slot;
        public string DisplayName => Alias.ToString();
        /// <summary>
        /// C19: the alias a support report should use. After a bot substitution the displayed
        /// alias is prefixed and may be trimmed, so fall back to the join-time snapshot. Returns
        /// "" for a seat that was never a human (a real bot), which is not reportable.
        /// </summary>
        public string ReportAlias
        {
            get
            {
                string current = Alias.ToString();
                if (!current.StartsWith("[BOT] ", System.StringComparison.Ordinal)) return current;
                string human = HumanAliasAtJoin.ToString();
                return string.IsNullOrEmpty(human) ? "" : human;
            }
        }
        public int TeamId => Team;
        public bool IsBot => BotControlled;
        public bool IsAlive => !Dead;
        public bool IsConnected => Connected;
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
        public bool IsReloading => ReloadTimer.IsRunning && !ReloadTimer.Expired(Runner);
        public float ReloadProgress => IsReloading && Weapon.reloadSeconds > 0f
            ? Mathf.Clamp01(1f - (ReloadTimer.RemainingTime(Runner) ?? Weapon.reloadSeconds) / Weapon.reloadSeconds) : 0f;
        public string LastAttackDirection => LastKillerDirection switch { 1 => "FRONT", 2 => "RIGHT", 3 => "BEHIND", 4 => "LEFT", _ => "" };

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

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            All.Remove(this);
            NetBotDriver.Forget(this); // QA-034: drop the bot brain so long-lived servers don't retain past players
        }

        // ---- QA-011: client-side threat presentation driven by replicated state ----
        // Every client (local and remote avatars alike) hears fire, reload, steps, jumps,
        // landings, pad launches, damage and eliminations spatialised at the avatar, and a
        // dead avatar's body is hidden until respawn. Replicated state alone plays nothing,
        // so these transitions are what make a fight legible online.
        int cueShotCounter = -1;
        string cueCosmetic;
        string cueRig = "";
        bool cueReloading, cueLaunched, cueDead, cueAirborne;
        float cueHealth, cueFallSpeed, cueStepTimer;

        public override void Render()
        {
            if (Application.isBatchMode && Runner.IsServer) return; // headless server plays nothing
            string cosmetic = Cosmetic.ToString(); // QA-024: everyone sees this player's equipped identity
            if (cosmetic != cueCosmetic)
            {
                var visual = transform.Find("Visual");
                var look = Hotdrop.UI.CosmeticLook.Parse(cosmetic);
                // Amendment 3: the chosen body rig replicates too, so remote viewers see it.
                if (visual != null && look.BodyRig != cueRig)
                {
                    cueRig = look.BodyRig;
                    Hotdrop.Network.NetAvatarVisual.ApplyRig(visual,
                        Hotdrop.Player.BodyRigs.FromStoredId(look.BodyRig), MatchView.IsFriendly(Team));
                }
                Hotdrop.UI.CosmeticLook.Apply(visual, look);
                cueCosmetic = cosmetic;
            }
            bool reloading = ReloadTimer.IsRunning && !ReloadTimer.Expired(Runner);
            bool grounded = Kcc != null && Kcc.IsGrounded;
            if (cueShotCounter < 0)
            {
                cueShotCounter = ShotCounter; cueReloading = reloading; cueLaunched = Launched;
                cueDead = Dead; cueAirborne = !grounded; cueHealth = Health;
                SetBodyVisible(!Dead);
                return;
            }

            bool mine = HasInputAuthority;
            Vector3 chest = transform.position + Vector3.up * 1.1f;
            if (ShotCounter > cueShotCounter)
                Hotdrop.Core.GameAudio.Play((Hotdrop.Core.GameCue)Mathf.Clamp(WeaponIndex, 0, 2), chest, mine ? 0.85f : 1f);
            cueShotCounter = Mathf.Max(cueShotCounter, ShotCounter); // resimulation never replays a shot

            if (reloading && !cueReloading)
            {
                if (Ammo <= 0) Hotdrop.Core.GameAudio.Play(Hotdrop.Core.GameCue.Empty, chest, 0.6f);
                Hotdrop.Core.GameAudio.Play(Hotdrop.Core.GameCue.Reload, chest, 0.55f);
            }
            cueReloading = reloading;

            if (Launched && !cueLaunched) Hotdrop.Core.GameAudio.Play(Hotdrop.Core.GameCue.PadLaunch, transform.position);
            cueLaunched = Launched;

            if (!Dead)
            {
                if (!grounded && !cueAirborne && Velocity.y > 2f && !Launched)
                    Hotdrop.Core.GameAudio.Play(Hotdrop.Core.GameCue.Jump, transform.position, 0.7f);
                if (grounded && cueAirborne && cueFallSpeed < -4f)
                    Hotdrop.Core.GameAudio.Play(Hotdrop.Core.GameCue.Land, transform.position, 0.8f);
                if (!grounded) cueFallSpeed = Velocity.y;
                cueAirborne = !grounded;

                if (grounded && new Vector2(Velocity.x, Velocity.z).sqrMagnitude > 1f)
                {
                    cueStepTimer -= Time.deltaTime;
                    if (cueStepTimer <= 0f)
                    {
                        Hotdrop.Core.GameAudio.Play(Hotdrop.Core.GameCue.Step, transform.position, 0.5f);
                        cueStepTimer = 0.38f;
                    }
                }
                else cueStepTimer = 0f;

                if (Health < cueHealth - 0.01f)
                {
                    if (mine) Hotdrop.Core.GameAudio.Play2D(Hotdrop.Core.GameCue.Hurt, 0.9f);
                    else Hotdrop.Core.GameAudio.Play(Hotdrop.Core.GameCue.Hurt, chest, 0.6f);
                }
            }
            cueHealth = Health;

            if (Dead != cueDead)
            {
                if (Dead) Hotdrop.Core.GameAudio.Play(Hotdrop.Core.GameCue.Elimination, chest);
                SetBodyVisible(!Dead);
                cueDead = Dead;
            }
        }

        void SetBodyVisible(bool visible)
        {
            var body = transform.Find("Visual");
            if (body != null && body.gameObject.activeSelf != visible) body.gameObject.SetActive(visible);
        }

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
            var previousInput = LastInput;
            LastInput = input;

            if (HasStateAuthority && hasInput && !BotControlled && IsActivity(input, previousInput)) { LastActiveTick = Runner.Tick; AfkWarned = false; }

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
            if (HasStateAuthority && input.Buttons.WasPressed(PreviousButtons, (int)NetButton.Pad)) TryPlacePad(match, input.Yaw);
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

        // QA-018: aim changes count as activity (spec 18), not only movement and buttons.
        static bool IsActivity(NetInput i, NetInput previous) =>
            AfkRules.IsActivity(i.Move, i.Buttons.Bits != 0, previous.Yaw, previous.Pitch, i.Yaw, i.Pitch);

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

        const float MaxAimDistance = 100f; // matches the client's crosshair ray (NetClientPresenter.SampleInput)

        void Weapons(NetInput input, NetMatch match)
        {
            var weapon = Weapon;
            if (ReloadTimer.IsRunning)
            {
                if (!ReloadTimer.Expired(Runner)) return;
                ReloadTimer = TickTimer.None;
                Ammo = weapon.magazineSize;
                FireDebt = 0f;
            }
            bool reload = input.Buttons.WasPressed(PreviousButtons, (int)NetButton.Reload);
            if (Ammo <= 0 || (reload && Ammo < weapon.magazineSize))
            {
                ReloadTimer = TickTimer.CreateFromSeconds(Runner, weapon.reloadSeconds);
                return;
            }

            bool firing = Match.IsLive && input.IsDown(NetButton.Fire) && !Map.IsInsideSpawnRoom(Team, transform.position);
            float debt = FireDebt;
            bool fires = FireCadence.Step(ref debt, Runner.DeltaTime, firing, weapon.SecondsPerShot);
            FireDebt = debt;
            if (!fires) return;

            // Kept for bot hearing ("recently fired"); cadence itself is FireDebt (QA-009).
            FireCooldown = TickTimer.CreateFromSeconds(Runner, weapon.SecondsPerShot);
            Ammo--;
            ShotCounter++;
            Protection = TickTimer.None; // firing ends spawn protection

            Vector3 eye = transform.position + Vector3.up * (M.capsuleHeight * 0.8f);
            Vector3 muzzle = GetMuzzlePosition(input.Yaw);
            // Anti-cheat: the client only chooses how far along its look ray the crosshair lands
            // (third-person parallax). The direction always comes from the replicated Yaw/Pitch
            // everyone sees, so a modified client can't face one way and shoot another ("silent aim").
            // Honest clients derive Yaw/Pitch from this same eye→point ray, so their shots are unchanged.
            Vector3 lookDir = Quaternion.Euler(input.Pitch, input.Yaw, 0f) * Vector3.forward;
            float aimDistance = input.AimPoint == Vector3.zero ? weapon.maxRange
                : Mathf.Clamp(Vector3.Distance(input.AimPoint, eye), 0.5f, MaxAimDistance);
            Vector3 cameraAimPoint = eye + lookDir * aimDistance;
            Vector3 aim = (cameraAimPoint - muzzle).sqrMagnitude > 0.0001f
                ? (cameraAimPoint - muzzle).normalized
                : Quaternion.Euler(input.Pitch, input.Yaw, 0f) * Vector3.forward;
            var state = !Kcc.IsGrounded ? MovementState.Airborne
                : new Vector2(Velocity.x, Velocity.z).sqrMagnitude > 0.25f ? MovementState.MovingGrounded : MovementState.StationaryGrounded;
            // Server-seeded spread: the seed is derived from shared tick/shot data, so client
            // prediction and the server roll the same pellets (spec section 7).
            uint seed = (uint)(Runner.Tick.Raw * 2654435761u) ^ (uint)(Slot * 40503 + ShotCounter * 97);

            // QA-012: a muzzle pushed into/through thin cover fires into that cover (no damage).
            var physics = Runner.GetPhysicsScene();
            bool buried = MuzzleObstruction.IsBuried(physics, new Vector3(transform.position.x, muzzle.y, transform.position.z),
                muzzle, MuzzleObstruction.GeometryMask, out Vector3 buriedAt);

            if (HasStateAuthority && !buried) match.QueueShot(this, muzzle, aim, seed, state);
            if (HasInputAuthority && Runner.IsForward)
            {
                // Predicted presentation only; the server decides every hit. QA-008: the tracers
                // run the SAME seeded pellet resolver the server runs (same seed, spread and
                // movement state), so BRICK shows its eight pellets and ARC/ZIP show their spread.
                OnLocalFired?.Invoke();
                if (buried) { OnLocalShot?.Invoke(muzzle, buriedAt); return; }
                int pelletCount = ShotResolver.ResolvePellets(weapon, aim, seed, state, pelletBuffer);
                for (int i = 0; i < pelletCount; i++)
                    OnLocalShot?.Invoke(muzzle, PredictPelletEnd(physics, muzzle, pelletBuffer[i].direction, weapon.maxRange));
            }
        }

        /// <summary>Where a pellet visibly stops, using the server's blocking rules: the shooter
        /// and living teammates never stop it; enemies, walls and pads do.</summary>
        Vector3 PredictPelletEnd(PhysicsScene physics, Vector3 muzzle, Vector3 direction, float range)
        {
            Vector3 end = muzzle + direction * range;
            int count = physics.Raycast(muzzle, direction, fxHits, range, ~0, QueryTriggerInteraction.Ignore);
            float nearest = range;
            for (int i = 0; i < count; i++)
            {
                if (fxHits[i].distance >= nearest) continue;
                var player = fxHits[i].collider.GetComponentInParent<NetPlayer>();
                if (player == this || (player != null && (player.Team == Team || player.Dead))) continue;
                nearest = fxHits[i].distance;
                end = fxHits[i].point;
            }
            return end;
        }

        void EquipWeapon(int index)
        {
            WeaponIndex = Mathf.Clamp(index, 0, 2);
            Ammo = Weapon.magazineSize;
            ReloadTimer = TickTimer.None;
            FireDebt = 0f;
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

        /// <summary>
        /// Full placement check shared by the server (authoritative) and the owning client's
        /// hold preview (QA-006), so the preview and the eventual rejection always agree.
        /// </summary>
        public string EvaluatePadPlacement(NetMatch match, float yaw, out Vector3 position, out Vector3 forward)
        {
            forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            position = transform.position + forward * P.placementForwardOffset;
            if (!Hotdrop.Core.StudySettings.PadsEnabled) return "PADS DISABLED IN THIS VERSION";
            if (match == null || !match.IsLive) return "NOT DURING THIS PHASE";
            if (!HasPadCharge) return "RECHARGING";
            if (!Kcc.IsGrounded) return "MUST BE GROUNDED";
            var physics = Runner.GetPhysicsScene();
            return Hotdrop.Pads.PadPlacementRules.ResolvePosition(physics, transform.position, forward, P, out position)
                ?? Hotdrop.Pads.PadPlacementRules.Validate(physics, position, forward, NetPad.ActiveCountExcludingOwner(Runner, Slot), P, Map, M);
        }

        void RejectPad(string reason)
        {
            PadRejectCode = Hotdrop.Pads.PadPlacementRules.CodeOf(reason);
            PadRejectCounter++; // the owning client shows the reason once per change
        }

        void TryPlacePad(NetMatch match, float yaw)
        {
            if (Dead) return;
            string reason = EvaluatePadPlacement(match, yaw, out var position, out var forward);
            if (reason == null && PadPlacements >= P.maxActivePadsPerMatch * 10) reason = "ARENA PAD LIMIT"; // sanity bound against a modified client
            if (reason != null) { RejectPad(reason); return; }
            match.SpawnPad(this, position, forward);
            HasPadCharge = false;
            PadPlacements++;
            // Death never refreshes the cooldown: the timer runs from placement.
            PadRecharge = TickTimer.CreateFromSeconds(Runner, P.rechargeSeconds);
        }

        // ---- Server-only state changes (called by NetMatch) ----

        public void ServerDamage(float amount, int attackerSlot, int weapon)
        {
            if (!Match.IsLive || Dead || IsSpawnProtected) return;
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
            LastKillerDirection = 0;
            foreach (var attacker in All)
            {
                if (attacker.Runner != Runner || attacker.Slot != killerSlot || attacker == this) continue;
                float angle = Vector3.SignedAngle(transform.forward, attacker.transform.position - transform.position, Vector3.up);
                LastKillerDirection = (byte)(Mathf.Abs(angle) < 45f ? 1 : Mathf.Abs(angle) > 135f ? 3 : angle > 0f ? 2 : 4);
                break;
            }
            Velocity = Vector3.zero;
            Launched = false;
            RespawnTimer = TickTimer.CreateFromSeconds(Runner, M.respawnDelaySeconds);
        }

        public void MarkBotSubstitution(bool afk = false)
        {
            BotControlled = true;
            HadBotSubstitution = true;
            if (afk) AfkRemoved = true;
            string alias = Alias.ToString();
            if (!alias.StartsWith("[BOT] ", System.StringComparison.Ordinal))
                Alias = "[BOT] " + alias.Substring(0, Mathf.Min(alias.Length, 25));
        }

        /// <summary>C04: server-only. Accumulate time spent holding the current weapon.</summary>
        public void RecordWeaponTime(float dt)
        {
            // Matches the goal catalog's weapon ids (ARC / ZIP / BRICK).
            string id = Weapon.id.ToString().ToUpperInvariant();
            weaponSeconds.TryGetValue(id, out var held);
            weaponSeconds[id] = held + dt;
        }

        /// <summary>C04: server-only. The weapon this player held longest ("" when none).</summary>
        public string MajorityWeaponId()
        {
            string best = "";
            float bestTime = -1f;
            foreach (var entry in weaponSeconds)
                if (entry.Value > bestTime) { bestTime = entry.Value; best = entry.Key; }
            return best;
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
                UncontestedZoneSeconds = 0f;
                weaponSeconds.Clear();
                PresentTicks = 0;
            }
        }
    }
}



