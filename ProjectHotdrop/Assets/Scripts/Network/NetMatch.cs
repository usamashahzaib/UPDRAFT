using System.Collections.Generic;
using Fusion;
using UnityEngine;
using Hotdrop.Config;
using Hotdrop.Match;
using Hotdrop.UI;
using Hotdrop.Weapons;

namespace Hotdrop.Network
{
    /// <summary>
    /// Server-authoritative Relay match (spec sections 3-4, 10, 21). Runs after
    /// every avatar has moved, so each tick follows the spec order:
    /// inputs → movement/abilities (NetPlayer) → shots/damage → eliminations/respawns
    /// → zone transition → occupancy/scoring → match end. The same MatchFlowController
    /// and RelayScoringController drive offline practice.
    /// </summary>
    // Fusion 2.1 orders FixedUpdateNetwork by Unity script execution order (it has no
    // [OrderAfter]); a positive order runs the match after NetPlayer/NetPad (order 0).
    [DefaultExecutionOrder(100)]
    public class NetMatch : NetworkBehaviour, IMatchView
    {
        static readonly Dictionary<NetworkRunner, NetMatch> byRunner = new Dictionary<NetworkRunner, NetMatch>();
        public static NetMatch For(NetworkRunner runner) => runner != null && byRunner.TryGetValue(runner, out var m) ? m : null;

        [Networked] public MatchPhase Phase { get; set; }
        [Networked] public NetworkBool Started { get; set; }
        [Networked] public float PhaseElapsed { get; set; }
        [Networked] public float MatchClock { get; set; }
        [Networked] public int ScoreA { get; set; }
        [Networked] public int ScoreB { get; set; }
        [Networked] public NetworkBool Overtime { get; set; }
        [Networked] public int OutcomeValue { get; set; }
        [Networked] public NetworkBool Cancelled { get; set; }
        [Networked] public NetworkBool HasBots { get; set; }
        [Networked] public int ExpectedHumans { get; set; }
        [Networked] public TickTimer LoadTimeout { get; set; }
        [Networked] public NetworkBool ZoneAllyA { get; set; }
        [Networked] public NetworkBool ZoneAllyB { get; set; }
        [Networked] public NetworkString<_32> StudyCohortCode { get; set; }

        // ownerSlot (C04): the pad's owner, so the result can attribute a teammate's use
        // of someone else's pad back to the player who built it.
        public struct PadEvent { public string name; public string padId; public int slot; public int team; public int padTeam; public int ownerSlot; public float matchTime; public Vector3 position; }
        readonly List<PadEvent> serverPadEvents = new List<PadEvent>();
        public System.Collections.ObjectModel.ReadOnlyCollection<PadEvent> ServerPadEvents => serverPadEvents.AsReadOnly();
        ServerMatchReplay serverReplay;
        public System.Collections.Generic.IReadOnlyList<ServerMatchReplay.Sample> ServerReplaySamples => serverReplay?.Samples;
        public System.Action<NetMatch> OnServerMatchEnded;
        public System.Action<NetMatch> OnServerMatchCancelled;
        public event System.Action<int, int, PingKind, Vector3> OnPingBroadcast;
        public event System.Action<int, int, byte> OnQuickChatBroadcast;
        public NetworkObject PadPrefab;

        MatchFlowController flow;
        RelayScoringController scoring;
        readonly List<Shot> shots = new List<Shot>();
        readonly PelletResult[] pelletBuffer = new PelletResult[16];
        readonly List<LagCompensatedHit> hits = new List<LagCompensatedHit>();
        readonly Dictionary<NetPlayer, float> pendingDamage = new Dictionary<NetPlayer, float>();
        readonly Dictionary<NetPlayer, (int slot, int weapon)> lastAttacker = new Dictionary<NetPlayer, (int, int)>();
        readonly HashSet<NetPlayer> shootersWhoHit = new HashSet<NetPlayer>(); // QA-010
        readonly HashSet<NetPlayer> shootersWhoHeadshot = new HashSet<NetPlayer>();
        readonly List<IMatchParticipant> participants = new List<IMatchParticipant>();
        // C04: players physically inside each team's scoring zone this tick, so the
        // match can credit uncontested occupation only to the sole occupying team.
        readonly List<NetPlayer> presentA = new List<NetPlayer>();
        readonly List<NetPlayer> presentB = new List<NetPlayer>();
        bool endReported;

        struct Shot { public NetPlayer shooter; public Vector3 origin; public Vector3 aim; public uint seed; public MovementState state; }

        MatchRulesConfig R => NetConfigs.Rules;
        MapConfig Map => NetConfigs.Map;

        public bool IsLive => Phase == MatchPhase.InProgress || Phase == MatchPhase.Overtime;
        public MatchOutcome Outcome => (MatchOutcome)OutcomeValue;
        public bool IsEndgame => Phase == MatchPhase.Overtime || MatchClock >= Mathf.Max(0f, R.regulationDurationSeconds - R.endgameHudTrimSeconds);
        public string StudyCohort => StudyCohortCode.ToString();
        public bool PadsEnabled => Hotdrop.Core.StudySettings.PadsEnabledFor(StudyCohort);

        public override void Spawned()
        {
            if (HasStateAuthority)
                _ = NetConfigs.Progression; // Validate configured AFK/disconnect windows before the match starts.
            byRunner[Runner] = this;
            if (HasStateAuthority)
            {
                StudyCohortCode = Hotdrop.Core.StudySettings.NormalizeCohort(DedicatedServer.StudyCohort);
                flow = new MatchFlowController(R);
                scoring = new RelayScoringController(R);
                serverReplay = new ServerMatchReplay(R.regulationDurationSeconds + R.maxOvertimeSeconds, MatchmakingSize);
                Phase = MatchPhase.Preparation;
                LoadTimeout = TickTimer.CreateFromSeconds(Runner, R.loadTimeoutSeconds);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (For(runner) == this) byRunner.Remove(runner);
            MatchView.Unregister(this);
        }

        public void QueueShot(NetPlayer shooter, Vector3 origin, Vector3 aim, uint seed, MovementState state)
        {
            if (!HasStateAuthority || shooter == null || shooter.Runner != Runner) return;
            shots.Add(new Shot { shooter = shooter, origin = origin, aim = aim, seed = seed, state = state });
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || Phase == MatchPhase.Results) { shots.Clear(); return; }
            float dt = Runner.DeltaTime;

            if (!Started)
            {
                shots.Clear();
                // Spec 4: never start a public match with an empty slot; cancel after 30 s.
                if (ConnectedHumans() >= ExpectedHumans && CountPlayers() == MatchmakingSize) StartMatch();
                else if (LoadTimeout.Expired(Runner)) CancelMatch();
                return;
            }

            var before = flow.Phase;
            flow.Tick(dt);
            Phase = flow.Phase;
            PhaseElapsed = flow.PhaseElapsed;
            MatchClock = flow.MatchTime;
            if (before != MatchPhase.InProgress && Phase == MatchPhase.InProgress) PlaceAllAtSpawns(matchStart: true);

            ResolveShots();
            HandleEliminationsAndRespawns();
            HandleAbsence();
            if (IsLive) serverReplay?.Capture(Runner, dt, MatchClock);

            if (IsLive)
            {
                Vector3 zone = Map.ZoneCenter(R.GetScoringZone(MatchClock));
                presentA.Clear(); presentB.Clear();
                bool a = TeamPresent(0, zone, dt, presentA), b = TeamPresent(1, zone, dt, presentB);
                ZoneAllyA = a; ZoneAllyB = b;
                // C04: only the sole occupying team accrues *uncontested* occupation time
                // (the 1 pt/s uncontested rule); it feeds the contracts/mastery facts.
                if (a != b) foreach (var p in (a ? presentA : presentB)) p.UncontestedZoneSeconds += dt;
                scoring.Tick(a, b, MatchClock, dt);
                ScoreA = scoring.ScoreA; ScoreB = scoring.ScoreB;
                if (scoring.IsInOvertime && flow.Phase == MatchPhase.InProgress) { flow.NotifyOvertimeStarted(); Overtime = true; }
                foreach (var p in NetPlayer.All)
                    if (p.Runner == Runner && p.Connected && !p.BotControlled)
                    {
                        p.PresentTicks++;
                        // C04: track which weapon this human actually held (majority fact).
                        if (!p.Dead) p.RecordWeaponTime(dt);
                    }
                if (scoring.Outcome != MatchOutcome.InProgress) EndMatch(scoring.Outcome);
            }
        }

        public const int MatchmakingSize = 6;

        int CountPlayers()
        {
            int n = 0;
            foreach (var p in NetPlayer.All) if (p.Runner == Runner) n++;
            return n;
        }

        int ConnectedHumans()
        {
            int n = 0;
            foreach (var p in NetPlayer.All) if (p.Runner == Runner && p.Connected && !p.BotControlled) n++;
            return n;
        }

        void StartMatch()
        {
            Started = true;
            foreach (var p in NetPlayer.All) if (p.Runner == Runner && p.BotControlled) HasBots = true;
            PlaceAllAtSpawns(matchStart: true);
        }

        void CancelMatch()
        {
            Cancelled = true;
            Phase = MatchPhase.Results;
            OnServerMatchCancelled?.Invoke(this);
        }

        void PlaceAllAtSpawns(bool matchStart)
        {
            int[] nextExit = new int[2];
            foreach (var p in SortedPlayers())
                p.ServerRespawn(Map.SpawnExit(p.Team, nextExit[p.Team]++), matchStart);
        }

        readonly List<NetPlayer> sorted = new List<NetPlayer>();

        List<NetPlayer> SortedPlayers()
        {
            sorted.Clear();
            foreach (var p in NetPlayer.All) if (p.Runner == Runner) sorted.Add(p);
            sorted.Sort((x, y) => x.Slot.CompareTo(y.Slot));
            return sorted;
        }

        void ResolveShots()
        {
            if (shots.Count == 0) return;
            // Deterministic same-tick ties: shots resolve in slot order and damage is
            // applied only after every shot this tick, so mutual eliminations both land.
            shots.Sort((x, y) => x.shooter.Slot.CompareTo(y.shooter.Slot));
            pendingDamage.Clear();
            shootersWhoHit.Clear();
            shootersWhoHeadshot.Clear();
            foreach (var shot in shots)
            {
                if (shot.shooter == null || shot.shooter.Dead) continue;
                var weapon = shot.shooter.Weapon;
                int pelletCount = ShotResolver.ResolvePellets(weapon, shot.aim, shot.seed, shot.state, pelletBuffer);
                for (int pelletIndex = 0; pelletIndex < pelletCount; pelletIndex++)
                {
                    var pellet = pelletBuffer[pelletIndex];
                    hits.Clear();
                    // Lag-compensated to the shooter's view (buffer capped at 150 ms in the project config).
                    Runner.LagCompensation.RaycastAll(shot.origin, pellet.direction, weapon.maxRange, shot.shooter.Object.InputAuthority,
                        hits, -1, true, HitOptions.IncludePhysX | HitOptions.SubtickAccuracy);
                    hits.Sort((x, y) => x.Distance.CompareTo(y.Distance));
                    foreach (var hit in hits)
                    {
                        var target = hit.Hitbox != null ? hit.Hitbox.Root.GetComponent<NetPlayer>() : hit.GameObject != null ? hit.GameObject.GetComponentInParent<NetPlayer>() : null;
                        if (target == shot.shooter) continue;
                        if (target != null)
                        {
                            if (target.Team == shot.shooter.Team || target.Dead) continue; // friendly bodies don't block
                            bool head = hit.Hitbox != null && hit.Hitbox.name == NetAvatarVisual.HeadHitboxName;
                            float damage = ShotResolver.GetDamage(weapon, hit.Distance, head);
                            pendingDamage.TryGetValue(target, out float sum);
                            pendingDamage[target] = sum + damage;
                            lastAttacker[target] = (shot.shooter.Slot, shot.shooter.WeaponIndex);
                            if (IsLive && !target.IsSpawnProtected) shootersWhoHit.Add(shot.shooter); // only hits that deal damage confirm
                            if (head && IsLive && !target.IsSpawnProtected) shootersWhoHeadshot.Add(shot.shooter);
                            break;
                        }
                        var pad = hit.GameObject != null ? hit.GameObject.GetComponentInParent<NetPad>() : null;
                        if (pad != null) pad.ServerDamage(ShotResolver.GetDamage(weapon, hit.Distance, false), shot.shooter.Team);
                        break; // walls and pads stop the pellet
                    }
                }
            }
            shots.Clear();
            foreach (var p in SortedPlayers())
                if (pendingDamage.TryGetValue(p, out float amount))
                {
                    var (slot, weaponIndex) = lastAttacker[p];
                    p.ServerDamage(amount, slot, weaponIndex);
                }
            // QA-010: every shooter who landed damage this tick gets an immediate hit confirmation.
            foreach (var shooter in shootersWhoHit) if (shooter != null) shooter.HitConfirmCounter++;
            foreach (var shooter in shootersWhoHeadshot) if (shooter != null) shooter.HeadshotConfirmCounter++;
        }

        public void Eliminate(NetPlayer victim, int killerSlot, int weapon)
        {
            if (!HasStateAuthority || victim == null || victim.Runner != Runner || victim.Dead) return;
            victim.ServerKill(killerSlot, weapon);
            foreach (var p in NetPlayer.All)
                if (p.Runner == Runner && p.Slot == killerSlot && p != victim) p.EliminationCount++;
        }

        void HandleEliminationsAndRespawns()
        {
            int[] exitCursor = { Runner.Tick.Raw % 3, (Runner.Tick.Raw + 1) % 3 };
            foreach (var p in SortedPlayers())
                if (p.Dead && p.RespawnTimer.Expired(Runner))
                    p.ServerRespawn(Map.SpawnExit(p.Team, exitCursor[p.Team]++), matchStart: false);
        }

        // Spec 10 (disconnect → labeled bot) and 18 (AFK warning and substitution windows).
        void HandleAbsence()
        {
            var progression = NetConfigs.Progression;
            float warnSeconds = progression.afkWarningSeconds;
            float removeSeconds = warnSeconds + progression.afkRemovalAfterWarningSeconds;
            int afkWarnTicks = Mathf.RoundToInt(warnSeconds / Runner.DeltaTime);
            int afkRemoveTicks = Mathf.RoundToInt(removeSeconds / Runner.DeltaTime);
            foreach (var p in NetPlayer.All)
            {
                if (p.Runner != Runner || p.BotControlled) continue;
                if (!p.Connected)
                {
                    if (p.DisconnectGrace.Expired(Runner)) p.MarkBotSubstitution();
                    continue;
                }
                if (!IsLive) continue;
                int idle = Runner.Tick.Raw - p.LastActiveTick;
                if (idle >= afkRemoveTicks) p.MarkBotSubstitution(afk: true);
                else if (idle >= afkWarnTicks) p.AfkWarned = true;
            }
        }

        bool TeamPresent(int team, Vector3 zone, float dt, List<NetPlayer> present)
        {
            foreach (var p in NetPlayer.All)
            {
                if (p.Runner != Runner || p.Team != team || p.Dead) continue;
                if (ZoneOccupancy.IsValid(p.transform.position, zone, Map.groundLevelY, R, true, p.IsSpawnProtected))
                {
                    p.ZoneSeconds += dt;
                    present.Add(p);
                }
            }
            return present.Count > 0;
        }

        void EndMatch(MatchOutcome outcome)
        {
            OutcomeValue = (int)outcome;
            flow.NotifyMatchEnded();
            Phase = MatchPhase.Results;
            if (!endReported) { endReported = true; OnServerMatchEnded?.Invoke(this); }
        }

        /// <summary>Spec 18: present for at least 75% of the match, not AFK-removed, stayed through results.</summary>
        public bool IsEligible(NetPlayer p)
        {
            int matchTicks = Mathf.Max(1, Mathf.RoundToInt(MatchClock / Runner.DeltaTime));
            return !p.HadBotSubstitution && p.Connected && p.PresentTicks >= matchTicks * 0.75f;
        }

        // ---- Pads ----

        public void SpawnPad(NetPlayer owner, Vector3 position, Vector3 forward)
        {
            if (!HasStateAuthority || owner == null || owner.Runner != Runner) return;
            // New placement removes the owner's previous pad (max 1 per owner).
            var previous = NetPad.ActiveOwnedBy(Runner, owner.Slot);
            if (previous != null) previous.Removed = true;
            if (PadPrefab == null) PadPrefab = Resources.Load<NetworkObject>("NetPrefabs/NetPad");
            string padId = System.Guid.NewGuid().ToString("N");
            Runner.Spawn(PadPrefab, position, Quaternion.LookRotation(forward), PlayerRef.None,
                (runner, obj) => obj.GetComponent<NetPad>().Init(owner.Slot, owner.Team, forward, padId));
            serverPadEvents.Add(new PadEvent { name = "pad_placed", padId = padId, slot = owner.Slot, team = owner.Team, padTeam = owner.Team, ownerSlot = owner.Slot, matchTime = MatchClock, position = position });
        }

        public void RecordPadUse(NetPad pad, NetPlayer user)
        {
            if (!HasStateAuthority || pad == null || user == null || pad.Runner != Runner || user.Runner != Runner) return;
            serverPadEvents.Add(new PadEvent { name = "pad_used", padId = pad.TelemetryId, slot = user.Slot, team = user.Team, padTeam = pad.Team, ownerSlot = pad.OwnerSlot, matchTime = MatchClock, position = pad.transform.position });
        }

        public void RecordPadDestroyed(NetPad pad)
        {
            if (!HasStateAuthority || pad == null || pad.Runner != Runner) return;
            serverPadEvents.Add(new PadEvent { name = "pad_destroyed", padId = pad.TelemetryId, slot = pad.OwnerSlot, team = pad.Team, padTeam = pad.Team, ownerSlot = pad.OwnerSlot, matchTime = MatchClock, position = pad.transform.position });
        }

        public void BroadcastPing(int senderSlot, int team, PingKind kind, Vector3 position) =>
            BroadcastPingIfAuthority(senderSlot, team, kind, position);

        void BroadcastPingIfAuthority(int senderSlot, int team, PingKind kind, Vector3 position)
        {
            if (!HasStateAuthority) return;
            RPC_BroadcastPing(senderSlot, team, (int)kind, position);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        void RPC_BroadcastPing(int senderSlot, int team, int kind, Vector3 position)
        {
            if (kind < (int)PingKind.GoHere || kind > (int)PingKind.PadReady) return;
            OnPingBroadcast?.Invoke(senderSlot, team, (PingKind)kind, position);
        }

        public void BroadcastQuickChat(int senderSlot, int team, byte choice)
        {
            if (!HasStateAuthority || !Hotdrop.UI.QuickChatWheelView.AvailabilityGateOpen ||
                choice >= Hotdrop.UI.QuickChatWheelView.MessageCount) return;
            RPC_BroadcastQuickChat(senderSlot, team, choice);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        void RPC_BroadcastQuickChat(int senderSlot, int team, byte choice)
        {
            if (!Hotdrop.UI.QuickChatWheelView.AvailabilityGateOpen ||
                choice >= Hotdrop.UI.QuickChatWheelView.MessageCount) return;
            OnQuickChatBroadcast?.Invoke(senderSlot, team, choice);
        }

        // ---- IMatchView (clients' presentation) ----

        public MatchRulesConfig Rules => R;
        MapConfig IMatchView.MapConfig => Map;
        public MatchPhase CurrentPhase => Phase;
        public float MatchTime => MatchClock;
        public ZoneId ScoringZone => R.GetScoringZone(MatchClock);
        public Vector3 ActiveObjectivePosition => Map.ZoneCenter(ScoringZone);
        public bool IsLiveMatch => Started && !Cancelled;
        public IMatchParticipant LocalParticipant { get; set; }
        public IMatchParticipant SpectatedTeammate { get; set; }

        public IReadOnlyList<IMatchParticipant> Participants
        {
            get
            {
                participants.Clear();
                foreach (var p in SortedPlayers()) participants.Add(p);
                return participants;
            }
        }
    }
}


