using System;
using Fusion;
using UnityEngine;
using Updraft.Core;
using Updraft.Input;
using Updraft.Match;

namespace Updraft.Network
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
        bool wasDead;
        int lastAmmo = -1, lastEliminations, lastShotCounter;
        float lastHealth = -1f;
        byte pendingWeaponChoice;
        // Button presses are latched until the next network tick consumes them, so a
        // tap shorter than a tick is never lost.
        bool jumpLatch, padLatch, reloadLatch, pingLatch;
        bool shotBlocked;
        static readonly RaycastHit[] aimHits = new RaycastHit[8];
        static readonly RaycastHit[] obstructionHits = new RaycastHit[16];
        static readonly Collider[] aimAssistCandidates = new Collider[32];
        static readonly RaycastHit[] aimAssistSightHits = new RaycastHit[16];
        LayerMask shotObstructionMask;

        public bool ShowingResults { get; private set; }

        public void Bind(GameBootstrap bootstrap, NetSession net, Action onReturnHome)
        {
            game = bootstrap;
            session = net;
            returnHome = onReturnHome;
            shotObstructionMask = LayerMask.GetMask("Ground", "Cover", "SpawnBarrier");
            session.InputSource = SampleInput;
            session.OnShutdownComplete += () =>
            {
                MatchView.Unregister(match);
                game.mainCamera.SetTarget(null);
                if (!ShowingResults) return;
                ShowingResults = false;
            };
        }

        void Update()
        {
            if (session == null || session.Runner == null || !session.Runner.IsRunning) return;
            if (match == null) match = NetMatch.For(session.Runner);
            if (local == null) FindLocal();
            if (match == null || local == null) return;

            LatchButtons();
            UpdateAimAssist();
            ApplyLook();
            UpdateBlockedShotFeedback();
            UpdateHud();
        }

        void FindLocal()
        {
            foreach (var p in NetPlayer.All)
            {
                if (p.Runner != session.Runner || !p.Object.HasInputAuthority) continue;
                local = p;
                match.LocalParticipant = p;
                MatchView.Register(match);
                game.mainCamera.SetTarget(p.transform);
                game.mainCamera.SnapBehindTarget();
                game.matchHUD.InitializeNetwork(p, game.touchInput, NetConfigs.Weapons, PlayAgain, Home, i => pendingWeaponChoice = (byte)(i + 1));
                game.matchHUD.HideAllPanels();
                match.OnPingBroadcast += ReceivePing;
                p.OnLocalShot += (from, to) =>
            {
                Updraft.Weapons.ShotTracers.Show(from, to, true);
                game.mainCamera.AddYawPitch(0f, -p.Weapon.verticalRecoilDegreesPerShot);
            };
                Telemetry.Track("online_match_joined", ("slot", p.Slot.ToString()));
                return;
            }
        }

        void ReceivePing(int senderSlot, int team, Updraft.UI.PingKind kind, Vector3 position)
        {
            string senderName = "Teammate";
            foreach (var player in NetPlayer.All)
                if (player.Runner == session.Runner && player.Slot == senderSlot)
                {
                    senderName = player.DisplayName;
                    break;
                }

            if (Updraft.UI.PingSystem.Instance == null) return;
            Updraft.UI.PingSystem.Instance.Send(new Updraft.UI.PingSystem.Ping
            {
                kind = kind,
                position = position,
                teamId = team,
                senderId = senderSlot,
                senderName = senderName,
            });
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
                if (touch != null) touch.AimSensitivityScale = 1f;
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
                if (distance > AimAssistRules.MaxDistance || distance < 0.01f ||
                    Vector3.Angle(game.mainCamera.transform.forward, toTarget) > AimAssistRules.ConeDegrees) continue;

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
                if (foundObstruction && firstPlayer != candidate) continue;
                touch.AimSensitivityScale = AimAssistRules.SensitivityMultiplier;
                return;
            }

            touch.AimSensitivityScale = 1f;
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
            jumpLatch = padLatch = reloadLatch = pingLatch = false;
            input.WeaponChoice = pendingWeaponChoice;

            // Send the camera's selected world point. The server derives a muzzle-to-point
            // direction and resolves every pellet itself; the client supplies no hit result.
            var cam = game.mainCamera.transform;
            Vector3 eye = local.transform.position + Vector3.up * (NetConfigs.Movement.capsuleHeight * 0.8f);
            Vector3 target = cam.position + cam.forward * 100f;
            int count = Physics.RaycastNonAlloc(cam.position, cam.forward, aimHits, 100f, ~0, QueryTriggerInteraction.Ignore);
            float nearest = 100f;
            for (int i = 0; i < count; i++)
            {
                if (aimHits[i].distance >= nearest || aimHits[i].collider.GetComponentInParent<NetPlayer>() == local) continue;
                if (Vector3.Dot(aimHits[i].point - eye, cam.forward) < 0.5f) continue; // ignore things between camera and avatar
                nearest = aimHits[i].distance; target = aimHits[i].point;
            }
            Vector3 dir = (target - eye).normalized;
            var look = Quaternion.LookRotation(dir).eulerAngles;
            input.Yaw = look.y;
            input.Pitch = look.x > 180f ? look.x - 360f : look.x;
            input.AimPoint = target;
            return input;
        }

        void UpdateBlockedShotFeedback()
        {
            if (local == null || game.mainCamera == null || game.matchHUD == null || local.Dead)
            {
                shotBlocked = false;
                return;
            }

            var cam = game.mainCamera.transform;
            Vector3 target = cam.position + cam.forward * 100f;
            int aimCount = Physics.RaycastNonAlloc(cam.position, cam.forward, aimHits, 100f, ~0, QueryTriggerInteraction.Ignore);
            float nearestAim = 100f;
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
            bool blocked = false;
            if (distance > 0.1f && shotObstructionMask.value != 0)
            {
                int count = Physics.RaycastNonAlloc(muzzle, toTarget / distance, obstructionHits,
                    distance, shotObstructionMask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                    if (obstructionHits[i].distance < distance - 0.1f) { blocked = true; break; }
            }

            if (blocked && !shotBlocked && game.touchInput.FireHeld) game.matchHUD.ShowBlockedShot();
            shotBlocked = blocked;
        }

        // ---------- HUD ----------

        void UpdateHud()
        {
            var hud = game.matchHUD;
            var touch = game.touchInput;
            var phase = match.Phase;
            bool preparing = !match.Started;

            hud.UpdateScores(match.ScoreA, match.ScoreB);
            hud.UpdateTimer(phase == MatchPhase.InProgress || phase == MatchPhase.Overtime || phase == MatchPhase.Results ? match.MatchClock : 0f);
            if (Math.Abs(local.HealthFraction - lastHealth) > 0.001f)
            {
                if (local.HealthFraction < lastHealth && lastHealth > 0f) game.mainCamera.TriggerShake(0.6f);
                lastHealth = local.HealthFraction;
                hud.UpdateHealth(lastHealth);
            }
            if (local.Ammo != lastAmmo)
            {
                lastAmmo = local.Ammo;
                hud.UpdateAmmo(local.Ammo, local.Weapon.magazineSize);
                touch.AmmoLabel = $"{local.Ammo}/{local.Weapon.magazineSize}";
            }
            touch.PadAvailable = local.HasPadCharge;
            touch.PadCooldown = local.HasPadCharge ? 0f : local.PadRecharge.RemainingTime(session.Runner) ?? 0f;
            // Match the offline control affordance: jump becomes LAUNCH when the
            // local player is standing on an armed pad. NetPlayer still validates
            // and performs the launch on the server when the button is pressed.
            touch.LaunchAvailable = !local.Dead &&
                NetPad.FindUsableUnder(session.Runner, local.transform.position, NetConfigs.Pad) != null;

            if (local.EliminationCount > lastEliminations) { hud.ShowHitConfirmed(); GameAudio.Play2D(GameCue.EliminationConfirm); }
            lastEliminations = local.EliminationCount;

            if (preparing)
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
            if (phase == MatchPhase.Preparation) { hud.EnableWeaponSelect(true); hud.ShowPreparationCountdown(match.PhaseElapsed); }
            if (phase == MatchPhase.Countdown) { hud.EnableWeaponSelect(false); hud.ShowCountdown(match.PhaseElapsed); }
            if (phase == MatchPhase.InProgress || phase == MatchPhase.Overtime)
            {
                hud.ShowRotation(match.MatchClock);
                hud.UpdateZoneStatus(match.ActiveObjectivePosition, match.ZoneAllyA, match.ZoneAllyB);
            }
            if (phase != MatchPhase.Preparation && !local.Dead) pendingWeaponChoice = 0;

            if (local.Dead && !wasDead)
            {
                string killer = "";
                foreach (var p in NetPlayer.All)
                    if (p.Runner == session.Runner && p.Slot == local.LastKillerSlot && p != local) killer = p.DisplayName;
                var weaponId = NetConfigs.Weapons[Mathf.Clamp(local.LastKillerWeapon, 0, 2)].id;
                hud.ShowEliminated(killer, weaponId);
            }
            if (!local.Dead && wasDead) game.mainCamera.SetTarget(local.transform);
            if (local.Dead) SpectateTeammate();
            wasDead = local.Dead;

            if (local.AfkWarned) hud.ShowCombatFeedback("MOVE OR YOU'LL BE REPLACED BY A BOT", Updraft.UI.MatchHUD.Objective, 1f);
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
            string footer = match.Cancelled ? "MATCH CANCELLED  ·  NO RATING OR REWARDS CHANGED"
                : match.HasBots ? "BOT-FILLED MATCH  ·  RATING UNCHANGED" : "RANKED 3v3";
            game.matchHUD.ShowResults(won, draw, match.ScoreA, match.ScoreB, match.Participants, footer);
            GameAudio.Play2D(draw ? GameCue.ZoneMilestone : won ? GameCue.Victory : GameCue.Defeat);
            Telemetry.Track("online_match_completed", ("result", draw ? "draw" : won ? "win" : "loss"));
        }

        void PlayAgain()
        {
            Home();
            OnlineHooks.PlayOnline?.Invoke(game);
        }

        void Home()
        {
            ShowingResults = false;
            session.Leave();
            returnHome?.Invoke();
        }
    }
}
