using System;
using Fusion;
using UnityEngine;
using Hotdrop.Core;
using Hotdrop.Input;
using Hotdrop.Match;

namespace Hotdrop.Network
{
    /// <summary>
    /// Client-side presentation of a networked match: binds the shared HUD, camera and
    /// touch controls to the local avatar and turns touch + camera into NetInput. It reads
    /// replicated state only; every outcome shown here was decided by the server.
    /// </summary>
    public class NetClientPresenter : MonoBehaviour
    {
        GameBootstrap game;
        NetSession session;
        Action returnHome;
        NetPlayer local;
        NetMatch match;
        MatchPhase lastPhase = (MatchPhase)(-1);
        int lastCountdownBeep = -1;
        bool wasDead, wasZoneContested, lastLaunched;
        Hotdrop.Config.ZoneId? lastAudioZone;
        int lastMilestoneA, lastMilestoneB;
        int lastEliminations, lastShotCounter, lastHitConfirms, lastHeadshotConfirms;
        readonly int[] rttSampleBuckets = new int[6];
        int rttSampleCount, maxRttSampleMs;
        float nextRttTelemetryAt;
        float lastHealth = -1f;
        int lastDamageEventSequence;
        float nextLatencyRefresh;
        byte pendingWeaponChoice;
        byte pendingQuickChatChoice = byte.MaxValue;
        // Button presses are latched until the next network tick consumes them, so a
        // tap shorter than a tick is never lost.
        bool jumpLatch, padLatch, reloadLatch, pingLatch;
        bool shotBlocked;
        float nextBlockedWarning;
        float lastSampledYaw;
        bool hasSampledYaw;
        int lastPadRejectCounter = -1;
        readonly Hotdrop.Pads.PadPreviewArc padPreview = new Hotdrop.Pads.PadPreviewArc();
        static readonly RaycastHit[] aimHits = new RaycastHit[8];
        static readonly RaycastHit[] obstructionHits = new RaycastHit[16];
        static readonly Collider[] aimAssistCandidates = new Collider[32];
        static readonly RaycastHit[] aimAssistSightHits = new RaycastHit[16];
        LayerMask shotObstructionMask;

        public bool ShowingResults { get; private set; }
        /// <summary>Spec 18: the server replaced the local player with a bot for inactivity.</summary>
        public event Action OnRemovedForInactivity;
        bool removedForInactivity;

        public void Bind(GameBootstrap bootstrap, NetSession net, Action onReturnHome)
        {
            game = bootstrap;
            session = net;
            returnHome = onReturnHome;
            lastPhase = (MatchPhase)(-1);
            lastCountdownBeep = -1;
            wasZoneContested = false;
            lastAudioZone = null;
            lastMilestoneA = lastMilestoneB = 0;
            shotObstructionMask = Hotdrop.Weapons.MuzzleObstruction.GeometryMask;
            session.InputSource = SampleInput;
            session.OnShutdownComplete += () =>
            {
                MatchView.Unregister(match);
                padPreview.Hide();
                game.mainCamera.SetTarget(null);
                // QA-016: when the server closes after the match, keep the results screen and its
                // HOME / PLAY AGAIN actions usable; nothing else needs the session any more.
            };
        }

        void Update()
        {
            if (session == null || session.Runner == null || !session.Runner.IsRunning) { GameAudio.ClearFlightMix(); return; }
            if (match == null) match = NetMatch.For(session.Runner);
            if (local == null) FindLocal();
            if (match == null || local == null) { GameAudio.ClearFlightMix(); return; }
            var localVelocity = local.Kcc != null ? local.Kcc.RealVelocity : Vector3.zero;
            GameAudio.SetFlightMix(new Vector2(localVelocity.x, localVelocity.z).magnitude,
                localVelocity.y, local.Kcc != null && !local.Kcc.IsGrounded && !local.Dead);
            game.matchHUD?.UpdateFlightMotion(new Vector2(localVelocity.x, localVelocity.z).magnitude,
                local.Kcc != null && !local.Kcc.IsGrounded && !local.Dead);
            if (local.AfkRemoved && !ShowingResults)
            {
                // Without this the player would sit in the match with dead controls, watching a bot play as them.
                if (!removedForInactivity) { removedForInactivity = true; OnRemovedForInactivity?.Invoke(); }
                return;
            }

            LatchButtons();
            UpdateAimAssist();
            ApplyLook();
            UpdateBlockedShotFeedback();
            UpdatePadFeedback();
            UpdateHud();
        }

        void FindLocal()
        {
            foreach (var p in NetPlayer.All)
            {
                if (p.Runner != session.Runner || !p.Object.HasInputAuthority) continue;
                local = p;
                lastPadRejectCounter = -1;
                lastEliminations = p.EliminationCount;
                lastHitConfirms = p.HitConfirmCounter;
                lastDamageEventSequence = p.DamageEventSequence;
                lastHeadshotConfirms = p.HeadshotConfirmCounter;
                lastLaunched = p.Launched;
                Array.Clear(rttSampleBuckets, 0, rttSampleBuckets.Length);
                rttSampleCount = maxRttSampleMs = 0;
                nextRttTelemetryAt = 0f;
                hasSampledYaw = false;
                match.LocalParticipant = p;
                lastAudioZone = match.ScoringZone;
                lastMilestoneA = match.ScoreA / 30;
                lastMilestoneB = match.ScoreB / 30;
                wasZoneContested = match.ZoneAllyA && match.ZoneAllyB;
                MatchView.Register(match);
                game.mainCamera.SetTarget(p.transform);
                game.mainCamera.SnapBehindTarget();
                string reportMatchId = MatchId;
                game.matchHUD.InitializeNetwork(p, game.touchInput, NetConfigs.Weapons, PlayAgain, Home,
                    i => pendingWeaponChoice = (byte)(i + 1),
                    string.IsNullOrWhiteSpace(reportMatchId) ? null : slot => ReportParticipantFromResults(slot, reportMatchId),
                    string.IsNullOrWhiteSpace(reportMatchId) ? null : slot => IsReportableParticipantSlot(slot, reportMatchId));
                game.matchHUD.HideAllPanels();
                match.OnPingBroadcast += ReceivePing;
                if (Hotdrop.UI.QuickChatWheelView.AvailabilityGateOpen)
                {
                    match.OnQuickChatBroadcast += ReceiveQuickChat;
                    var quickChat = game.matchHUD.GetComponent<Hotdrop.UI.QuickChatWheelView>();
                    if (quickChat != null)
                    {
                        quickChat.MessageSelected -= QueueQuickChat;
                        quickChat.MessageSelected += QueueQuickChat;
                    }
                }
                p.OnLocalShot += (from, to) => Hotdrop.Weapons.ShotTracers.Show(from, to, true);
                p.OnLocalFired += shotInMagazine =>
                {
                    game.mainCamera.AddRecoilPitch(-p.Weapon.GetVerticalRecoilDegrees(shotInMagazine));
                    game.touchInput?.PulseFireFeedback();
                };
                Telemetry.Track("online_match_joined", ("slot", p.Slot.ToString()));
                return;
            }
        }

        void ReceivePing(int senderSlot, int team, Hotdrop.UI.PingKind kind, Vector3 position)
        {
            string senderName = "Teammate";
            foreach (var player in NetPlayer.All)
                if (player.Runner == session.Runner && player.Slot == senderSlot)
                {
                    senderName = player.DisplayName;
                    break;
                }

            if (Hotdrop.UI.PingSystem.Instance == null) return;
            Hotdrop.UI.PingSystem.Instance.Send(new Hotdrop.UI.PingSystem.Ping
            {
                kind = kind,
                position = position,
                teamId = team,
                senderId = senderSlot,
                senderName = senderName,
            });
        }

        void QueueQuickChat(byte choice)
        {
            if (Hotdrop.UI.QuickChatWheelView.AvailabilityGateOpen &&
                choice < Hotdrop.UI.QuickChatWheelView.MessageCount) pendingQuickChatChoice = choice;
        }

        void ReceiveQuickChat(int senderSlot, int team, byte choice)
        {
            if (!Hotdrop.UI.QuickChatWheelView.AvailabilityGateOpen || local == null || team != local.Team ||
                choice >= Hotdrop.UI.QuickChatWheelView.MessageCount) return;
            if (Hotdrop.UI.QuickChatWheelView.Muted && senderSlot != local.Slot) return;
            game.matchHUD.GetComponent<Hotdrop.UI.QuickChatWheelView>()?.ShowMessage(choice);
        }

        // ---------- Input ----------

        void LatchButtons()
        {
            var touch = game.touchInput;
            jumpLatch |= touch.JumpPressed;
            padLatch |= touch.PadPressed;
            reloadLatch |= touch.ReloadPressed;
            pingLatch |= touch.PingPressed;
        }

        void ApplyLook()
        {
            var touch = game.touchInput;
            Vector2 aim = touch.AimDelta;
            if (aim.sqrMagnitude > 0f) game.mainCamera.AddYawPitch(aim.x, -aim.y);
        }

        void UpdateAimAssist()
        {
            var touch = game.touchInput;
            if (touch == null || local == null || local.Dead ||
                PlayerPrefs.GetInt("AimAssistEnabled", 1) == 0)
            {
                if (touch != null) touch.AimSensitivityScale = AimAssistRules.FullSensitivityScale;
                return;
            }

            Vector3 origin = game.mainCamera.transform.position;
            int count = Physics.OverlapSphereNonAlloc(origin, AimAssistRules.MaxDistance,
                aimAssistCandidates, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var candidate = aimAssistCandidates[i].GetComponentInParent<NetPlayer>();
                if (candidate == null || candidate == local || candidate.Dead || candidate.Team == local.Team) continue;

                Vector3 target = candidate.transform.position + Vector3.up * 1.4f;
                Vector3 toTarget = target - origin;
                float distance = toTarget.magnitude;
                if (distance > AimAssistRules.MaxDistance || distance < 0.01f) continue;

                int sightCount = Physics.RaycastNonAlloc(origin, toTarget / distance,
                    aimAssistSightHits, distance, ~0, QueryTriggerInteraction.Ignore);
                float nearest = distance;
                NetPlayer firstPlayer = null;
                bool foundObstruction = false;
                for (int hitIndex = 0; hitIndex < sightCount; hitIndex++)
                {
                    var hit = aimAssistSightHits[hitIndex];
                    if (hit.distance >= nearest) continue;
                    var hitPlayer = hit.collider.GetComponentInParent<NetPlayer>();
                    if (hitPlayer == local) continue;
                    nearest = hit.distance;
                    firstPlayer = hitPlayer;
                    foundObstruction = true;
                }
                if (AimAssistRules.Qualifies(origin, game.mainCamera.transform.forward, target,
                        !foundObstruction || firstPlayer == candidate))
                {
                    touch.AimSensitivityScale = AimAssistRules.SensitivityScale(true);
                    return;
                }
            }

            touch.AimSensitivityScale = AimAssistRules.FullSensitivityScale;
        }

        NetInput SampleInput()
        {
            var input = new NetInput();
            if (local == null) return input;
            var touch = game.touchInput;
            input.Move = touch.enabled ? touch.MoveInput : Vector2.zero;
            input.Set(NetButton.Fire, touch.enabled && touch.FireHeld);
            input.Set(NetButton.Jump, jumpLatch);
            input.Set(NetButton.Pad, padLatch);
            input.Set(NetButton.Reload, reloadLatch);
            input.Set(NetButton.Ping, pingLatch);
            input.QuickChatChoice = pendingQuickChatChoice;
            input.Set(NetButton.QuickChat, pendingQuickChatChoice < Hotdrop.UI.QuickChatWheelView.MessageCount);
            jumpLatch = padLatch = reloadLatch = pingLatch = false;
            pendingQuickChatChoice = byte.MaxValue;
            input.WeaponChoice = pendingWeaponChoice;

            // Send the camera's selected world point. The server derives a muzzle-to-point
            // direction and resolves every pellet itself; the client supplies no hit result.
            var cam = game.mainCamera.transform;
            float rayDistance = NetConfigs.Rules.cameraAimRayDistanceMeters;
            Vector3 eye = local.transform.position + Vector3.up * (NetConfigs.Movement.capsuleHeight * 0.8f);
            Vector3 target = cam.position + cam.forward * rayDistance;
            int count = Physics.RaycastNonAlloc(cam.position, cam.forward, aimHits, rayDistance, ~0, QueryTriggerInteraction.Ignore);
            float nearest = rayDistance;
            for (int i = 0; i < count; i++)
            {
                if (aimHits[i].distance >= nearest || aimHits[i].collider.GetComponentInParent<NetPlayer>() == local) continue;
                if (Vector3.Dot(aimHits[i].point - eye, cam.forward) < 0.5f) continue; // ignore things between camera and avatar
                nearest = aimHits[i].distance; target = aimHits[i].point;
            }
            Vector3 dir = (target - eye).normalized;
            var look = Quaternion.LookRotation(dir).eulerAngles;
            input.Yaw = look.y;
            lastSampledYaw = look.y;
            hasSampledYaw = true;
            input.Pitch = look.x > 180f ? look.x - 360f : look.x;
            input.AimPoint = target;
            return input;
        }

        /// <summary>
        /// QA-006: the same hold-to-preview arc and reason as offline play, evaluated with the
        /// server's own rule set against this client's scene; server rejections are shown too.
        /// </summary>
        void UpdatePadFeedback()
        {
            var touch = game.touchInput;
            if (lastPadRejectCounter < 0) lastPadRejectCounter = local.PadRejectCounter; // ignore history on join
            if (local.PadRejectCounter != lastPadRejectCounter)
            {
                lastPadRejectCounter = local.PadRejectCounter;
                string rejected = Hotdrop.Pads.PadPlacementRules.ReasonOf(local.PadRejectCode);
                if (rejected != null) game.matchHUD.ShowPadRejected(rejected);
            }

            if (touch == null || !touch.PadPreviewActive || local.Dead) { padPreview.Hide(); return; }
            float yaw = hasSampledYaw ? lastSampledYaw : game.mainCamera.transform.eulerAngles.y;
            string reason = local.EvaluatePadPlacement(match, yaw, out Vector3 position, out Vector3 forward);
            padPreview.Show(position, forward, reason == null, NetConfigs.Pad, NetConfigs.Movement.gravity);
            touch.PadPreviewReason = reason;
        }

        void OnDestroy()
        {
            if (match != null)
            {
                match.OnPingBroadcast -= ReceivePing;
                match.OnQuickChatBroadcast -= ReceiveQuickChat;
            }
            var quickChat = game?.matchHUD != null ? game.matchHUD.GetComponent<Hotdrop.UI.QuickChatWheelView>() : null;
            if (quickChat != null) quickChat.MessageSelected -= QueueQuickChat;
            padPreview.Destroy();
        }

        void OnDisable() => GameAudio.ClearFlightMix();

        void UpdateBlockedShotFeedback()
        {
            if (local == null || game.mainCamera == null || game.matchHUD == null || local.Dead)
            {
                shotBlocked = false;
                return;
            }

            var cam = game.mainCamera.transform;
            float rayDistance = NetConfigs.Rules.cameraAimRayDistanceMeters;
            Vector3 target = cam.position + cam.forward * rayDistance;
            int aimCount = Physics.RaycastNonAlloc(cam.position, cam.forward, aimHits, rayDistance, ~0, QueryTriggerInteraction.Ignore);
            float nearestAim = rayDistance;
            Vector3 eye = local.transform.position + Vector3.up * (NetConfigs.Movement.capsuleHeight * 0.8f);
            for (int i = 0; i < aimCount; i++)
            {
                if (aimHits[i].distance >= nearestAim || aimHits[i].collider.GetComponentInParent<NetPlayer>() == local) continue;
                if (Vector3.Dot(aimHits[i].point - eye, cam.forward) < 0.5f) continue;
                nearestAim = aimHits[i].distance;
                target = aimHits[i].point;
            }

            Vector3 muzzle = local.GetMuzzlePosition(cam.eulerAngles.y);
            Vector3 toTarget = target - muzzle;
            float distance = toTarget.magnitude;
            Vector3 body = new Vector3(local.transform.position.x, muzzle.y, local.transform.position.z);
            bool blocked = Hotdrop.Weapons.MuzzleObstruction.IsBuried(Physics.defaultPhysicsScene, body, muzzle, shotObstructionMask, out _);
            if (!blocked && distance > 0.1f && shotObstructionMask.value != 0)
            {
                int count = Physics.RaycastNonAlloc(muzzle, toTarget / distance, obstructionHits,
                    distance, shotObstructionMask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                    if (obstructionHits[i].distance < distance - 0.1f) { blocked = true; break; }
            }

            // QA-012: warn whenever the trigger is held on a blocked line — including aiming at
            // cover first and firing afterwards — repeating while held, not only on the transition.
            if (blocked && game.touchInput.FireHeld &&
                (!shotBlocked || game.touchInput.FirePressedThisFrame || Time.unscaledTime >= nextBlockedWarning))
            {
                game.matchHUD.ShowBlockedShot();
                nextBlockedWarning = Time.unscaledTime + 0.7f;
            }
            shotBlocked = blocked;
        }

        // ---------- HUD ----------

        void UpdateHud()
        {
            var hud = game.matchHUD;
            var touch = game.touchInput;
            var phase = match.Phase;
            bool preparing = !match.Started;
            if (phase == MatchPhase.Countdown)
                GameAudio.SetCountdownMusic(match.PhaseElapsed, NetConfigs.Rules.countdownSeconds, NetConfigs.Rules);
            else if (preparing)
                GameAudio.StopMusic();

            if (phase == MatchPhase.Countdown)
            {
                int count = Mathf.CeilToInt(NetConfigs.Rules.countdownSeconds - match.PhaseElapsed);
                if (count != lastCountdownBeep && count > 0)
                {
                    lastCountdownBeep = count;
                    GameAudio.Play2D(GameCue.Countdown);
                    GameAudio.ReportLocalCaption(GameCue.Countdown, count.ToString());
                }
            }
            else if (phase == MatchPhase.InProgress && lastPhase == MatchPhase.Countdown && lastCountdownBeep != 0)
            {
                lastCountdownBeep = 0;
                GameAudio.Play2D(GameCue.CountdownGo);
                GameAudio.ReportLocalCaption(GameCue.CountdownGo, Hotdrop.UI.UiDraw.L("hud.countdown.go", "Go"));
            }

            // Fusion's measured round trip time is the only source for the live ping label.
            // Keep the read outside Runtime UI to preserve the one-way assembly dependency.
            if (Time.unscaledTime >= nextLatencyRefresh)
            {
                nextLatencyRefresh = Time.unscaledTime + 0.5f;
                double rtt = session.Runner.GetPlayerRtt(session.Runner.LocalPlayer);
                hud.Overlay?.SetLatency(rtt > 0d ? Mathf.RoundToInt((float)(rtt * 1000d)) : 0);
                var phaseForRtt = match.Phase;
                if (rtt > 0d && match.Started &&
                    (phaseForRtt == MatchPhase.InProgress || phaseForRtt == MatchPhase.Overtime) &&
                    Time.unscaledTime >= nextRttTelemetryAt)
                {
                    nextRttTelemetryAt = Time.unscaledTime + 2f;
                    RecordRttSample(Mathf.RoundToInt((float)(rtt * 1000d)));
                }
            }

            hud.UpdateScores(match.ScoreA, match.ScoreB);
            hud.UpdateTimer(phase == MatchPhase.InProgress || phase == MatchPhase.Overtime || phase == MatchPhase.Results ? match.MatchClock : 0f);
            if (Math.Abs(local.HealthFraction - lastHealth) > 0.001f)
            {
                if (local.HealthFraction < lastHealth && lastHealth > 0f) game.mainCamera.TriggerShake(0.6f);
                lastHealth = local.HealthFraction;
                hud.UpdateHealth(lastHealth);
            }
            if (local.DamageEventSequence != lastDamageEventSequence)
            {
                lastDamageEventSequence = local.DamageEventSequence;
                if (!local.Dead) hud.ShowDamageFrom(local.DamageSourceDirection); // Bible 2.5 hit arc
            }
            hud.UpdateAmmo(local.Ammo, local.Weapon.magazineSize, local.IsReloading, local.ReloadProgress);
            touch.PadAvailable = local.HasPadCharge;
            touch.PadCooldown = local.HasPadCharge ? 0f : local.PadRecharge.RemainingTime(session.Runner) ?? 0f;
            // Match the offline control affordance: jump becomes LAUNCH when the
            // local player is standing on an armed pad. NetPlayer still validates
            // and performs the launch on the server when the button is pressed.
            touch.LaunchAvailable = !local.Dead &&
                NetPad.FindUsableUnder(session.Runner, local.transform.position, NetConfigs.Pad) != null;
            if (local.Launched && !lastLaunched)
            {
                HapticFeedback.Pulse();
                game.mainCamera.TriggerPadLaunchFeedback();
            }
            lastLaunched = local.Launched;

            // QA-010: every landed hit confirms (marker + sound); eliminations get the stronger cue.
            bool eliminated = local.EliminationCount > lastEliminations;
            bool headshot = local.HeadshotConfirmCounter != lastHeadshotConfirms;
            if (eliminated) { hud.ShowHitConfirmed(elimination: true); GameAudio.Play2D(GameCue.EliminationConfirm); HapticFeedback.Pulse(0.8f); }
            else if (local.HitConfirmCounter != lastHitConfirms) { hud.ShowHitConfirmed(headshot: headshot); GameAudio.Play2D(GameCue.Hit, 0.8f); HapticFeedback.Pulse(0.6f); }
            lastEliminations = local.EliminationCount;
            lastHitConfirms = local.HitConfirmCounter;
            lastHeadshotConfirms = local.HeadshotConfirmCounter;

            if (preparing && phase == MatchPhase.Preparation)
            {
                hud.ShowPhase(MatchPhase.Preparation, 0f);
                return;
            }

            if (phase != lastPhase)
            {
                if (phase == MatchPhase.Countdown) hud.ShowPhase(MatchPhase.Countdown, NetConfigs.Rules.countdownSeconds);
                if (phase == MatchPhase.InProgress) { hud.HideAllPanels(); hud.OnMatchStart(); game.mainCamera.SnapBehindTarget(); }
                if (phase == MatchPhase.Overtime) hud.ShowOvertime();
                if (phase == MatchPhase.Results) ShowResults();
                lastPhase = phase;
            }
            if (phase == MatchPhase.InProgress || phase == MatchPhase.Overtime)
                GameAudio.SetMatchMusic(match.MatchClock, phase == MatchPhase.Overtime, NetConfigs.Rules);
            if (phase == MatchPhase.Preparation) { hud.EnableWeaponSelect(true); hud.ShowPreparationCountdown(match.PhaseElapsed); }
            if (phase == MatchPhase.Countdown) { hud.EnableWeaponSelect(false); hud.ShowCountdown(match.PhaseElapsed); }
            if (phase == MatchPhase.InProgress || phase == MatchPhase.Overtime)
            {
                hud.ShowRotation(match.MatchClock);
                hud.UpdateZoneStatus(match.ActiveObjectivePosition, match.ZoneAllyA, match.ZoneAllyB);
                var zone = match.ScoringZone;
                if (lastAudioZone.HasValue && lastAudioZone.Value != zone)
                    GameAudio.Play2D(GameCue.ZoneActivate);
                lastAudioZone = zone;

                int milestoneA = match.ScoreA / 30, milestoneB = match.ScoreB / 30;
                if (milestoneA > lastMilestoneA || milestoneB > lastMilestoneB)
                    GameAudio.Play2D(GameCue.ZoneMilestone);
                lastMilestoneA = milestoneA;
                lastMilestoneB = milestoneB;

                bool contested = match.ZoneAllyA && match.ZoneAllyB;
                if (contested && !wasZoneContested) GameAudio.Play2D(GameCue.ZoneContest);
                wasZoneContested = contested;
            }
            else wasZoneContested = false;
            if (phase != MatchPhase.Preparation && !local.Dead) pendingWeaponChoice = 0;

            if (local.Dead && !wasDead)
            {
                NetPlayer killer = null;
                foreach (var p in NetPlayer.All)
                    if (p.Runner == session.Runner && p.Slot == local.LastKillerSlot && p != local) killer = p;
                var weaponId = NetConfigs.Weapons[Mathf.Clamp(local.LastKillerWeapon, 0, 2)].id;
                hud.ShowEliminated(killer, weaponId, local.LastAttackDirection);
            }
            if (!local.Dead && wasDead) game.mainCamera.SetTarget(local.transform);
            if (local.Dead) SpectateTeammate();
            wasDead = local.Dead;

            if (local.AfkWarned) hud.ShowCombatFeedback(Hotdrop.UI.MatchHUD.Localized("afkWarning", "Move or a squadmate will take over"), Hotdrop.UI.MatchHUD.Objective, 1f);
        }

        void SpectateTeammate()
        {
            var current = match.SpectatedTeammate as NetPlayer;
            if (current != null && !current.Dead) return;
            match.SpectatedTeammate = null;
            foreach (var p in NetPlayer.All)
                if (p.Runner == session.Runner && p != local && p.Team == local.Team && !p.Dead)
                {
                    match.SpectatedTeammate = p;
                    game.mainCamera.SetTarget(p.transform);
                    return;
                }
        }

        void ShowResults()
        {
            ShowingResults = true;
            var outcome = match.Outcome;
            bool draw = outcome == MatchOutcome.Draw;
            bool won = !draw && ((outcome == MatchOutcome.TeamAWins && local.Team == 0) || (outcome == MatchOutcome.TeamBWins && local.Team == 1));
            GameAudio.ResolveMatchMusic(!won && !draw && !match.Cancelled);
            // Footer is informational only; the authoritative outcome/status is passed explicitly so the results screen never infers
            // cancellation or receipt state from footer copy.
            var receiptState = match.Cancelled ? Hotdrop.UI.ResultsTrustState.Cancelled
                : string.IsNullOrWhiteSpace(MatchId) ? Hotdrop.UI.ResultsTrustState.UnverifiedDevelopment
                : BackendClient.Instance == null ? Hotdrop.UI.ResultsTrustState.Unavailable
                : Hotdrop.UI.ResultsTrustState.Pending;
            game.matchHUD.ShowResults(won, draw, match.ScoreA, match.ScoreB, null, match.Participants,
                match.Cancelled ? Hotdrop.UI.MatchHUD.Localized("result.cancelledFooter", "MATCH CANCELLED  ·  NO RATING OR REWARDS CHANGED")
                    : match.HasBots ? Hotdrop.UI.MatchHUD.Localized("result.mixedRosterFooter", "MIXED ROSTER  ·  RATING UNCHANGED")
                    : Hotdrop.UI.MatchHUD.Localized("result.rankedFooter", "RANKED 3v3"),
                trustState: receiptState);
            // A cancelled match has no receipt to poll: its explicit Cancelled trust state already
            // told the screen not to imply progression. The sticky Play Again action is built for
            // every state, so keeping the screen open here needs no extra state change.
            if (!match.Cancelled) PollRewardReceipt();
            // QA-025: remember who was in this match so the player can report someone by name.
            var others = new System.Collections.Generic.List<RecentMatch.Entry>();
            foreach (var p in NetPlayer.All)
                if (p.Runner == session.Runner && p != local && (!p.BotControlled || p.HadBotSubstitution)) // humans who quit/idled stay reportable
                {
                    // C19: resolve support reports from the join-time account alias.
                    string reportAlias = p.ReportAlias;
                    if (string.IsNullOrEmpty(reportAlias)) continue; // a seat that was never human
                    others.Add(new RecentMatch.Entry { alias = reportAlias, slot = p.Slot, team = p.Team });
                }
            RecentMatch.Remember(MatchId, others, local != null ? local.Team : -1);
            if (!match.Cancelled)
            {
                var resultCue = draw ? GameCue.ZoneMilestone : won ? GameCue.Victory : GameCue.Defeat;
                GameAudio.Play2D(resultCue);
                string resultCaption = Hotdrop.UI.ResultsView.Localized(draw ? "outcome.draw"
                    : won ? "outcome.victory" : "outcome.defeat", draw ? "DRAW" : won ? "VICTORY" : "DEFEAT");
                GameAudio.ReportLocalCaption(resultCue, resultCaption);
            }
            Telemetry.Track("online_match_completed",
                ("result", match.Cancelled ? "cancelled" : draw ? "draw" : won ? "win" : "loss"),
                ("network_rtt_sample_count", rttSampleCount),
                ("rtt_0_50_ms_count", rttSampleBuckets[0]),
                ("rtt_51_100_ms_count", rttSampleBuckets[1]),
                ("rtt_101_150_ms_count", rttSampleBuckets[2]),
                ("rtt_151_200_ms_count", rttSampleBuckets[3]),
                ("rtt_201_300_ms_count", rttSampleBuckets[4]),
                ("rtt_over_300_ms_count", rttSampleBuckets[5]),
                ("network_rtt_max_ms", maxRttSampleMs));
        }

        void RecordRttSample(int milliseconds)
        {
            if (milliseconds <= 0) return;
            int bucket = milliseconds <= 50 ? 0 : milliseconds <= 100 ? 1 : milliseconds <= 150 ? 2
                : milliseconds <= 200 ? 3 : milliseconds <= 300 ? 4 : 5;
            rttSampleBuckets[bucket]++;
            rttSampleCount++;
            maxRttSampleMs = Mathf.Max(maxRttSampleMs, milliseconds);
        }

        string matchIdOverride;
        public string MatchId
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(matchIdOverride)) return matchIdOverride;
                if (session != null) return !string.IsNullOrWhiteSpace(session.MatchId) ? session.MatchId : null;
                return RecentMatch.TryGet(out string recentMatchId, out _) ? recentMatchId : null;
            }
            set => matchIdOverride = value;
        }

        /// <summary>
        /// QA-017: show what the player actually earned, as recorded by the backend. Until the
        /// server's result arrives (it may be retried from the server outbox) the screen says the
        /// reward is pending instead of implying nothing was earned. Runs while results are shown.
        /// </summary>
        async void PollRewardReceipt()
        {
            var hud = game.matchHUD;
            if (string.IsNullOrWhiteSpace(MatchId))
            {
                hud.SetResultsReceipt(Hotdrop.UI.ResultsView.Localized("receipt.noMatchId", "REWARD NOT VERIFIED — no match receipt ID is available for this development session"),
                    Hotdrop.UI.MatchHUD.SecondaryText,
                    Hotdrop.UI.ResultsView.Localized("save.development", "No server receipt is available for this development session."), Hotdrop.UI.ResultsTrustState.UnverifiedDevelopment);
                return;
            }
            if (BackendClient.Instance == null)
            {
                hud.SetResultsReceipt(Hotdrop.UI.ResultsView.Localized("receipt.serviceUnavailable", "REWARD STATUS UNAVAILABLE — the server receipt service is not configured"),
                    Hotdrop.UI.MatchHUD.SecondaryText,
                    Hotdrop.UI.ResultsView.Localized("save.unavailable", "This screen could not confirm the receipt. Check your profile later."), Hotdrop.UI.ResultsTrustState.Unavailable);
                return;
            }
            hud.SetResultsReceipt(Hotdrop.UI.ResultsView.Localized("receipt.confirming", "REWARD PENDING — confirming your result with the server…"), Hotdrop.UI.MatchHUD.SecondaryText,
                trustState: Hotdrop.UI.ResultsTrustState.Pending);
            float delay = 2f;
            while (ShowingResults)
            {
                var receipt = await BackendClient.Instance.MatchReceiptAsync(MatchId);
                if (!ShowingResults) return;
                if (receipt.Ok && receipt.Value != null && receipt.Value.recorded)
                {
                    var r = receipt.Value;
                    if (!r.eligible)
                        hud.SetResultsReceipt(Hotdrop.UI.ResultsView.Localized("receipt.ineligible", "NO REWARD FOR THIS MATCH (not eligible: left early, replaced by a bot, or server failure)"), Hotdrop.UI.MatchHUD.SecondaryText,
                            Hotdrop.UI.ResultsView.Localized("save.recordedNoRewards", "The server recorded this match without XP or credits."), Hotdrop.UI.ResultsTrustState.RecordedIneligible);
                    else
                    {
                        hud.SetResultsReward(r, Hotdrop.UI.MatchHUD.Objective);
                    }
                    return;
                }
                if (!receipt.Ok && receipt.Error != BackendError.NoInternet && receipt.Error != BackendError.ServerBusy && receipt.Error != BackendError.ServerError)
                {
                    hud.SetResultsReceipt(Hotdrop.UI.ResultsView.Localized("receipt.unavailable", "REWARD STATUS UNAVAILABLE — it will appear in your profile once recorded"), Hotdrop.UI.MatchHUD.SecondaryText,
                        Hotdrop.UI.ResultsView.Localized("save.unavailable", "This screen could not confirm the receipt. Check your profile later."), Hotdrop.UI.ResultsTrustState.Unavailable);
                    return;
                }
                hud.SetResultsReceipt(Hotdrop.UI.ResultsView.Localized("receipt.deliveryPending", "REWARD PENDING — the server is still delivering this result; it is kept until it arrives"),
                    Hotdrop.UI.MatchHUD.SecondaryText, trustState: Hotdrop.UI.ResultsTrustState.Pending);
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(delay));
                delay = Mathf.Min(delay * 1.5f, 15f);
            }
        }

        void PlayAgain()
        {
            Home();
            OnlineHooks.PlayOnline?.Invoke(game);
        }

        // QA-016: results can stay on screen long after the server has closed the session (its
        // teardown destroys this component's GameObject). The results actions are plain delegates,
        // so they must not assume the session still exists: leave it only if it is still alive.
        void Home()
        {
            ShowingResults = false;
            if (session != null) session.Leave(); // Unity null: false once the session is destroyed
            returnHome?.Invoke();
            GameAudio.SetHubMusic(true);
        }

        void ReportParticipantFromResults(int slot, string resultMatchId)
        {
            if (slot < 0 || string.IsNullOrWhiteSpace(resultMatchId) || game == null
                || !RecentMatch.TryGet(out string recentMatchId, out var recentPlayers)
                || !string.Equals(recentMatchId, resultMatchId, StringComparison.Ordinal)
                || !ContainsReportSlot(recentPlayers, slot)) return;
            Home();
            game.GetComponent<OfflineHomeScreen>()?.OpenSupportReport(slot);
        }

        bool IsReportableParticipantSlot(int slot, string resultMatchId)
        {
            if (slot < 0 || string.IsNullOrWhiteSpace(resultMatchId)) return false;
            if (RecentMatch.TryGet(out string recentMatchId, out var recentPlayers)
                && string.Equals(recentMatchId, resultMatchId, StringComparison.Ordinal))
                return ContainsReportSlot(recentPlayers, slot);
            if (session == null || session.Runner == null) return false;
            foreach (var player in NetPlayer.All)
                if (player != null && player.Runner == session.Runner && player.Slot == slot)
                    return RecentMatch.IsReportableAlias(player.ReportAlias)
                        && (!player.BotControlled || player.HadBotSubstitution);
            return false;
        }

        static bool ContainsReportSlot(System.Collections.Generic.IReadOnlyList<RecentMatch.Entry> players, int slot)
        {
            if (players == null) return false;
            for (int i = 0; i < players.Count; i++)
                if (players[i] != null && players[i].slot == slot && !string.IsNullOrWhiteSpace(players[i].alias))
                    return true;
            return false;
        }
    }
}



