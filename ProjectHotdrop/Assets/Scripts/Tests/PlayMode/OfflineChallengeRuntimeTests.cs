using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Hotdrop.Config;
using Hotdrop.Core;
using Hotdrop.Player;
using Hotdrop.Tutorial;
using Hotdrop.UI;

namespace Hotdrop.Tests.PlayMode
{
    public class OfflineChallengeRuntimeTests
    {
        GameBootstrap game;
        OfflineChallengeRuntime runtime;
        string savePath;
        int tutorialDone;
        bool hadTutorial;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            hadTutorial = PlayerPrefs.HasKey(GameBootstrap.TutorialDoneKey);
            tutorialDone = PlayerPrefs.GetInt(GameBootstrap.TutorialDoneKey);
            PlayerPrefs.SetInt(GameBootstrap.TutorialDoneKey, 1);
            Time.timeScale = 1;
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            game = GameBootstrap.Instance;
            game.GetComponent<StartupConnectionScreen>().Continue();
            yield return null;
            game.OpenOfflineLibrary();
            runtime = game.GetComponent<OfflineChallengeRuntime>();
            savePath = Path.Combine(Application.temporaryCachePath, "challenge_runtime_" + System.Guid.NewGuid().ToString("N") + ".json");
            runtime.Controller.customSavePath = savePath;
            runtime.Controller.InitializeController();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (hadTutorial) PlayerPrefs.SetInt(GameBootstrap.TutorialDoneKey, tutorialDone); else PlayerPrefs.DeleteKey(GameBootstrap.TutorialDoneKey);
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            foreach (string suffix in new[] { "", ".tmp", ".bak" }) if (File.Exists(savePath + suffix)) File.Delete(savePath + suffix);
        }

        [UnityTest]
        public IEnumerator HomeOfflineRoute_AllTwelveEntriesBuildRealWorlds_WithoutOnlineRewards()
        {
            Assert.IsNotNull(GameObject.Find("ChallengeLibrary"));
            var entries = runtime.Controller.config.entries;
            Assert.AreEqual(12, entries.Count);
            foreach (var entry in entries)
            {
                Assert.IsTrue(runtime.Controller.StartChallenge(entry.id), entry.id);
                yield return null;
                Assert.IsNotNull(runtime.Player, entry.id + " must spawn a real player");
                Assert.AreEqual(ChallengeFlowState.Gameplay, runtime.Controller.CurrentState);
                if (entry.chapter == ChallengeChapter.RooftopOperator)
                    Assert.AreEqual(entry.targetCount, Object.FindObjectsByType<TutorialTarget>(FindObjectsSortMode.None).Length, entry.id);
                if (entry.objectiveType == ChallengeObjectiveType.DefendAgainstBots)
                    Assert.AreEqual(3, PlayerController.All.Count, "One real player and two authored bots");
                if (entry.objectiveType == ChallengeObjectiveType.CompleteOfflineRelay)
                    Assert.AreEqual(6, game.matchController.ActivePlayerCount, "Reuse the standard 3v3 local Relay");
                runtime.Controller.OpenLibrary();
                yield return null;
                Assert.AreEqual(0, PlayerController.All.Count, "No player leaks between drills");
            }
            Assert.IsNull(GameObject.Find("SHOP"));
            Assert.AreEqual(0, runtime.Controller.SaveSystem.Data.records.Count, "Spawning does not grant completion");
            game.StartOfflinePractice();
            yield return null;
            Assert.AreSame(game.matchController, Hotdrop.Match.MatchView.Current, "Normal practice must reclaim its real HUD/bot objective view after drills");
            Assert.AreEqual(6, PlayerController.All.Count);
            game.ReturnToHome();
        }

        [UnityTest]
        public IEnumerator Checkpoint_PauseResumeAndProcessRecovery_KeepLocalRecordAndRealWorld()
        {
            runtime.Controller.StartChallenge("ch_basic_route");
            yield return new WaitForSeconds(.2f);
            var player = runtime.Player;
            var entry = runtime.Controller.SelectedEntry;
            player.WarpForTraining(entry.routePoints[0], Quaternion.identity);
            yield return new WaitForSeconds(.2f);
            Assert.AreEqual(1, runtime.Controller.CurrentTargetsCompleted);
            runtime.Controller.PauseChallenge();
            Assert.AreEqual(0, Time.timeScale);
            float pausedAt = runtime.Controller.CurrentElapsedTime;
            Assert.IsNotNull(GameObject.Find("ChallengePause"));
            yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(pausedAt, runtime.Controller.CurrentElapsedTime);
            runtime.Controller.UnpauseChallenge();
            Assert.AreEqual(player, runtime.Player, "Same-process resume must preserve the live player");
            Assert.AreEqual(1, Time.timeScale);
            runtime.Controller.PauseChallenge();
            runtime.Controller.OpenLibrary();
            yield return null;
            runtime.Controller.SelectChallenge(entry.id);
            Assert.IsTrue(runtime.Controller.CanResumeSelected, runtime.Controller.SaveSystem.GetSnapshot(entry.id)?.customStateJson);
            Capture(game.matchHUD.GetComponent<Canvas>(), "14_resume_checkpoint");
            Assert.IsTrue(runtime.Controller.ResumeFromSnapshot());
            yield return null;
            Assert.AreEqual(1, runtime.Controller.CurrentTargetsCompleted);
            for (int i = 1; i < entry.routePoints.Length; i++)
            {
                runtime.Player.WarpForTraining(entry.routePoints[i], Quaternion.identity);
                yield return new WaitForSeconds(.2f);
            }
            Assert.AreEqual(ChallengeFlowState.Result, runtime.Controller.CurrentState);
            Assert.IsTrue(runtime.Controller.LastResult.saveSucceeded);
            Assert.IsNotNull(GameObject.Find("ChallengeResult"));
            var saved = new LocalChallengeSave(savePath);
            Assert.IsTrue(saved.GetRecord(entry.id).isCompleted);
            // A completed run earns a medal; the exact tier depends on the authored
            // time thresholds and must never be ChallengeMedal.None.
            Assert.AreNotEqual(ChallengeMedal.None, saved.GetRecord(entry.id).medal);
        }

        [UnityTest]
        public IEnumerator Targets_RealWeaponRaycastsAndReload_CompleteDrill_NoSyntheticTargetCallbacks()
        {
            runtime.Controller.StartChallenge("ch_reload_discipline");
            yield return new WaitForSeconds(.2f);
            var player = runtime.Player;
            // Same simulator input API used by bots: real cadence, ammo, spread, muzzle
            // raycasts and reload timers. No RegisterTargetCompleted or TakeDamage calls.
            player.playerCamera = null;
            player.WarpForTraining(new Vector3(0, .05f, 14), Quaternion.Euler(0, 180, 0));
            var muzzle = player.GetComponentInChildren<Transform>();
            foreach (var t in player.GetComponentsInChildren<Transform>()) if (t.name == "Muzzle") muzzle = t;
            foreach (var target in Object.FindObjectsByType<TutorialTarget>(FindObjectsSortMode.None))
            {
                float deadline = Time.realtimeSinceStartup + 5;
                while (target != null && Time.realtimeSinceStartup < deadline)
                {
                    player.SetExternalAim(target.transform.position + Vector3.up - muzzle.position);
                    player.SetExternalInput(Vector2.zero, false, true, player.CurrentAmmo == 0);
                    yield return null;
                }
                Assert.IsTrue(target == null, "Real weapon must eliminate the installed target");
            }
            player.SetExternalInput(Vector2.zero, false, false, true);
            yield return null;
            player.SetExternalInput(Vector2.zero, false, false, false);
            yield return new WaitForSeconds(player.CurrentWeapon.reloadSeconds + .3f);
            Assert.AreEqual(ChallengeFlowState.Result, runtime.Controller.CurrentState);
            Assert.IsTrue(runtime.Controller.LastResult.isSuccess);
            Assert.Greater(runtime.Controller.LastResult.accuracyPercent, 0);
            Assert.LessOrEqual(runtime.Controller.LastResult.accuracyPercent, 100);
        }

        [UnityTest]
        public IEnumerator PadRoutes_RealActivationAndBallisticLanding_CompleteWithoutTeleportingFlight()
        {
            // Scripted waypoint traversal exercises completion/storage, not human route comfort.
            runtime.Controller.StartChallenge("ch_basic_route");
            foreach (var point in runtime.Controller.SelectedEntry.routePoints)
            { runtime.Player.WarpForTraining(point, Quaternion.identity); yield return new WaitForSeconds(.2f); }
            Assert.IsTrue(runtime.Controller.LastResult.isSuccess);
            foreach (string id in new[] { "ch_own_pad_route", "ch_enemy_pad_route", "ch_safe_landing" })
            {
                runtime.Controller.StartChallenge(id);
                yield return new WaitForSeconds(.2f);
                var player = runtime.Player;
                if (id == "ch_own_pad_route")
                {
                    Assert.IsNull(player.TryPlacePad(), "The authored lane must permit real placement");
                    player.WarpForTraining(player.PadManager.ActivePad.Position + Vector3.up * .05f, player.transform.rotation);
                    yield return new WaitForSeconds(game.padConfig.armingSeconds + .2f);
                }
                player.SetExternalInput(Vector2.zero, true, false, false);
                yield return null;
                player.SetExternalInput(Vector2.zero, false, false, false);
                float deadline = Time.realtimeSinceStartup + 5;
                while (runtime.Controller.CurrentState == ChallengeFlowState.Gameplay && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.Greater(player.PadLaunches, 0, id + " must activate the actual pad");
                Assert.AreEqual(ChallengeFlowState.Result, runtime.Controller.CurrentState,
                    id + " actual landing: " + player.Position + " target: " + runtime.Controller.SelectedEntry.landingPosition);
                Assert.IsTrue(runtime.Controller.LastResult.isSuccess);
                runtime.Controller.OpenLibrary();
                yield return null;
            }
            Capture(game.matchHUD.GetComponent<Canvas>(), "14_first_flight_completed_scripted");
        }

        [UnityTest]
        public IEnumerator RemainingTargetDrills_RealStationaryMovingAndPadAirborneHits()
        {
            foreach (string id in new[] { "ch_stationary_targets", "ch_moving_targets", "ch_launch_and_hit" })
            {
                runtime.Controller.StartChallenge(id);
                yield return new WaitForSeconds(.2f);
                var player = runtime.Player; player.playerCamera = null;
                var muzzle = (Transform)typeof(PlayerController).GetField("muzzleTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(player);
                var targets = Object.FindObjectsByType<TutorialTarget>(FindObjectsSortMode.None);
                foreach (var target in targets)
                {
                    if (id == "ch_launch_and_hit")
                    {
                        float groundedDeadline = Time.realtimeSinceStartup + 3;
                        while (!player.IsGrounded && Time.realtimeSinceStartup < groundedDeadline) yield return null;
                        Assert.IsTrue(player.IsGrounded, "Ground-hit rejection must be tested after the preceding flight actually lands");
                        float before = target.health;
                        player.SetExternalAim(target.transform.position + Vector3.up - muzzle.position);
                        player.SetExternalInput(Vector2.zero, false, true, false);
                        yield return new WaitForSeconds(.3f);
                        player.SetExternalInput(Vector2.zero, false, false, false);
                        Assert.AreEqual(before, target.health, "Grounded hits cannot advance Launch and Hit");
                        for (int flight = 0; target != null && flight < 4; flight++)
                        {
                            player.WarpForTraining(runtime.Controller.SelectedEntry.padPosition + Vector3.up * .05f, player.transform.rotation);
                            if (player.CurrentAmmo < 6)
                            { player.SetExternalInput(Vector2.zero, false, false, true); yield return null; player.SetExternalInput(Vector2.zero, false, false, false); yield return new WaitForSeconds(player.CurrentWeapon.reloadSeconds + .1f); }
                            yield return new WaitForSeconds(.2f);
                            player.SetExternalInput(Vector2.zero, true, false, false); yield return null;
                            player.SetExternalInput(Vector2.zero, false, true, false);
                            float deadline = Time.realtimeSinceStartup + 3;
                            while (target != null && !player.IsGrounded && Time.realtimeSinceStartup < deadline)
                            { player.SetExternalAim(target.transform.position + Vector3.up - muzzle.position); yield return null; }
                            player.SetExternalInput(Vector2.zero, false, false, false);
                        }
                        Assert.IsTrue(target == null, "Actual pad-flight fire must eliminate target");
                    }
                    else
                    {
                        float deadline = Time.realtimeSinceStartup + 7;
                        while (target != null && Time.realtimeSinceStartup < deadline)
                        {
                            player.SetExternalAim(target.transform.position + Vector3.up - muzzle.position);
                            player.SetExternalInput(Vector2.zero, false, true, player.CurrentAmmo == 0); yield return null;
                        }
                        Assert.IsTrue(target == null, id + " must complete through real raycasts");
                    }
                }
                yield return null;
                Assert.AreEqual(ChallengeFlowState.Result, runtime.Controller.CurrentState, id);
                Assert.IsTrue(runtime.Controller.LastResult.isSuccess, id);
                runtime.Controller.OpenLibrary(); yield return null;
            }
        }

        [UnityTest]
        public IEnumerator RelayDrills_RealOccupationRotationDefenderFireAndFullMatchCompletion()
        {
            runtime.Controller.StartChallenge("ch_capture_one_zone");
            runtime.Player.WarpForTraining(game.mapConfig.centerZone + Vector3.up * 2, Quaternion.identity);
            runtime.Player.SetMatchPermissions(false, false, false, false);
            yield return new WaitForSeconds(.2f);
            Assert.AreEqual(0, runtime.HoldSeconds, "Flying above the zone cannot capture");
            runtime.Player.SetMatchPermissions(true, true, true, false);
            runtime.Player.WarpForTraining(game.mapConfig.centerZone + new Vector3(0, .05f, 3), Quaternion.identity);
            Time.timeScale = 4;
            float deadline = Time.realtimeSinceStartup + 10;
            while (runtime.Controller.CurrentState == ChallengeFlowState.Gameplay && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(runtime.Controller.LastResult.isSuccess);
            Time.timeScale = 1;

            runtime.Controller.StartChallenge("ch_rotate_between_zones"); yield return new WaitForSeconds(.2f);
            Assert.AreEqual(1, runtime.Controller.CurrentTargetsCompleted);
            runtime.Player.WarpForTraining(game.mapConfig.eastZone + Vector3.up * .05f, Quaternion.identity);
            yield return new WaitForSeconds(.2f);
            Assert.AreEqual(1, runtime.Controller.CurrentTargetsCompleted, "An inactive zone cannot advance rotation");
            Time.timeScale = 6;
            foreach (var zone in new[] { ZoneId.West, ZoneId.East })
            {
                deadline = Time.realtimeSinceStartup + 15;
                while (runtime.ScoringZone != zone && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.AreEqual(zone, runtime.ScoringZone);
                runtime.Player.WarpForTraining(game.mapConfig.ZoneCenter(zone) + Vector3.up * .05f, Quaternion.identity);
                yield return new WaitForSeconds(.2f);
            }
            Assert.IsTrue(runtime.Controller.LastResult.isSuccess);
            Time.timeScale = 1;

            runtime.Controller.StartChallenge("ch_defend_against_bots"); yield return new WaitForSeconds(.2f);
            var player = runtime.Player; player.playerCamera = null;
            var muzzle = (Transform)typeof(PlayerController).GetField("muzzleTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(player);
            int index = 0;
            foreach (var bot in Object.FindObjectsByType<Hotdrop.Bots.BotController>(FindObjectsSortMode.None))
            {
                // Deterministic combat-condition test, not a bot difficulty/route acceptance.
                bot.enabled = false;
                var opponent = bot.GetComponent<PlayerController>(); opponent.SetExternalInput(Vector2.zero, false, false, false);
                opponent.WarpForTraining(game.mapConfig.centerZone + new Vector3(index++ == 0 ? -2 : 2, .05f, 1), Quaternion.identity);
                deadline = Time.realtimeSinceStartup + 5;
                while (opponent.IsAlive && Time.realtimeSinceStartup < deadline)
                { player.SetExternalAim(opponent.Position + Vector3.up * 1.2f - muzzle.position); player.SetExternalInput(Vector2.zero, false, true, player.CurrentAmmo == 0); yield return null; }
                Assert.IsFalse(opponent.IsAlive, "Defender completion must use real weapon damage");
            }
            yield return null; Assert.IsTrue(runtime.Controller.LastResult.isSuccess);

            runtime.Controller.StartChallenge("ch_complete_offline_relay");
            Time.timeScale = 12;
            deadline = Time.realtimeSinceStartup + 30;
            while (runtime.Controller.CurrentState == ChallengeFlowState.Gameplay && Time.realtimeSinceStartup < deadline) yield return null;
            Time.timeScale = 1;
            Assert.AreEqual(ChallengeFlowState.Result, runtime.Controller.CurrentState);
            Assert.IsTrue(runtime.Controller.LastResult.isSuccess);
            Assert.AreEqual(1, runtime.Controller.CurrentTargetsCompleted);
            Assert.IsFalse(game.matchHUD.resultsPanel.activeSelf, "One local challenge result replaces the duplicate practice panel");
        }

        [UnityTest]
        public IEnumerator LibraryDetailGameplayPauseResultAndRecovery_CaptureActualLocalScreens()
        {
            var canvas = game.matchHUD.GetComponent<Canvas>();
            Capture(canvas, "14_library_empty");
            GameObject.Find("Challenge_ch_basic_route").GetComponent<Button>().onClick.Invoke();
            yield return null; Capture(canvas, "14_detail");
            GameObject.Find("StartChallenge").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSeconds(.3f); Capture(canvas, "14_gameplay", true);
            runtime.Controller.PauseChallenge();
            yield return null; Capture(canvas, "14_pause");
            runtime.Controller.UnpauseChallenge();
            foreach (var point in runtime.Controller.SelectedEntry.routePoints)
            {
                runtime.Player.WarpForTraining(point, Quaternion.identity);
                yield return new WaitForSeconds(.2f);
            }
            Capture(canvas, "14_result");
            runtime.Controller.StartChallenge("ch_complete_offline_relay");
            yield return null;
            runtime.Controller.PauseChallenge(); runtime.Controller.OpenLibrary();
            runtime.Controller.SelectChallenge("ch_complete_offline_relay");
            Assert.IsFalse(runtime.Controller.CanResumeSelected, "OS loss of bot world must explicitly offer clean restart");
            Assert.IsNull(GameObject.Find("ResumeChallenge"));
            Assert.IsNotNull(GameObject.Find("StartChallenge"));
            Capture(canvas, "14_recovery_restart");
        }

        static void Capture(Canvas canvas, string label, bool gameplay = false)
        {
            foreach (int width in new[] { 1920, 2400 })
                // Solo drills use the challenge tracker, not MatchHUD's Relay objective panels.
                PlayerJourneyVisualEvidenceTests.Capture(canvas, label, width, "challenges_2026-10-05",
                    gameplay, false, false);
        }
    }
}
