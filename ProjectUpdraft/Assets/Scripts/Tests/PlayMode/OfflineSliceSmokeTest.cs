using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Updraft.Match;
using Updraft.Player;
using Updraft.Tutorial;
using Updraft.Core;
using Updraft.Map;
using Updraft.UI;
using Updraft.Pads;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Reflection;

namespace Updraft.Tests.PlayMode
{
    /// <summary>
    /// Milestone B acceptance check: does the generated offline-slice scene
    /// (Assets/Scenes/Main.unity) actually run without throwing? This is the
    /// first genuine Play Mode verification — everything before this only
    /// proved the project compiles, not that GameBootstrap/SceneSetup's
    /// generated hierarchy survives Awake/Start/Update.
    ///
    /// Unity's Test Framework fails a PlayMode test automatically on any
    /// Debug.LogError/LogException during the test, so "no exception" here
    /// is a real, checked assertion, not just an absence of a manual check.
    /// </summary>
    public class OfflineSliceSmokeTest
    {
        // GameBootstrap is a DontDestroyOnLoad singleton; each test needs a fresh one.
        InputSettings originalInputSettings;

        [UnitySetUp]
        public IEnumerator DestroyPreviousBootstrap()
        {
            Time.timeScale = 1f;
            // Batch-mode editors have no focused Game view, so by default synthetic
            // device events never reach play-mode (EnhancedTouch) updates.
            originalInputSettings = InputSystem.settings;
            var settings = Object.Instantiate(originalInputSettings);
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = settings;
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
        }

        [TearDown]
        public void RestoreInputSettings()
        {
            if (originalInputSettings != null) InputSystem.settings = originalInputSettings;
        }

        [UnityTest]
        public IEnumerator MainScene_LoadsAndRunsForFiveSeconds_WithoutErrors()
        {
            PlayerPrefs.DeleteKey(GameBootstrap.TutorialDoneKey); // first launch
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            Assert.AreEqual(1, Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length,
                "Tutorial startup must create one player, not also auto-start a practice match.");
            Assert.AreEqual(1, Object.FindObjectsByType<TutorialController>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(0, Object.FindFirstObjectByType<MatchController>().ActivePlayerCount,
                "MatchController must wait for GameBootstrap to choose a mode.");
            Assert.AreEqual(1, Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length);
            Assert.IsNotNull(Object.FindFirstObjectByType<TutorialUI>(), "Training must show instructions and a usable skip button.");
            Assert.AreEqual(1f, GameBootstrap.Instance.matchHUD.healthSlider.value);
            Assert.AreEqual(UnityEngine.UI.Selectable.Transition.None, GameBootstrap.Instance.matchHUD.healthSlider.transition);
            Assert.AreEqual(6, Object.FindObjectsByType<SpawnExitBarrier>(FindObjectsSortMode.None).Length);
            var map = Object.FindFirstObjectByType<SwitchyardGenerator>();
            int ramps = 0;
            foreach (Transform child in map.transform)
            {
                if (child.name != "Ramp") continue;
                ramps++;
                Vector3 top = child.position + child.up * 0.075f + child.forward * child.localScale.z * 0.5f;
                Vector3 bottom = child.position + child.up * 0.075f - child.forward * child.localScale.z * 0.5f;
                Assert.AreEqual(map.mapConfig.platformHeight, top.y, 0.01f);
                Assert.AreEqual(map.mapConfig.groundLevelY, bottom.y, 0.01f);
            }
            Assert.AreEqual(8, ramps);

            yield return ExerciseRecordedTouchInput();

            float elapsed = 0f;
            while (elapsed < 5f)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            var tutorial = Object.FindFirstObjectByType<TutorialController>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            yield return ExerciseTutorialJourney(tutorial, player);
            GameBootstrap.Instance.StartTutorial();
            yield return null;
            tutorial = Object.FindFirstObjectByType<TutorialController>();
            player = Object.FindFirstObjectByType<PlayerController>();
            yield return new WaitForSeconds(1f);
            var targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.name = "Weapon-damage regression target";
            // Keep the target in the clear firing lane before the Z=20 sightline blocker.
            targetObject.transform.position = player.playerCamera.transform.position + player.playerCamera.transform.forward * 6f;
            targetObject.transform.localScale = new Vector3(1, 3, 1);
            var trainingTarget = targetObject.AddComponent<TutorialTarget>();
            trainingTarget.health = 1000;
            Physics.SyncTransforms();
            player.SetExternalInput(Vector2.zero, false, true, false);
            yield return new WaitForSeconds(0.4f);
            player.SetExternalInput(Vector2.zero, false, false, false);
            Assert.Less(trainingTarget.health, 1000, "The real firing path must damage a tutorial target.");
            Object.Destroy(targetObject);

            var gameConfig = GameBootstrap.Instance;
            var padData = new LaunchPad(gameConfig.padConfig, 999, 1, player.transform.position, Vector3.right);
            padData.CompleteArming();
            var padObject = new GameObject("Enemy-pad launch regression");
            padObject.transform.position = padData.Position;
            padObject.AddComponent<LaunchPadBehaviour>().Initialize(padData);
            Physics.SyncTransforms();
            Assert.IsTrue(player.IsGrounded, "Pad activation setup requires a grounded player.");
            Assert.IsTrue(Physics.Raycast(player.transform.position + Vector3.up * 0.5f, Vector3.down, out _, 1f, LayerMask.GetMask("Pad")),
                "The newly created pad collider must be queryable before sending the jump input.");
            bool launched = false;
            player.OnPadActivated += _ => launched = true;
            player.SetExternalInput(Vector2.zero, true, false, false);
            yield return null;
            player.SetExternalInput(Vector2.zero, false, false, false);
            Assert.IsTrue(launched, "Jump must use an armed enemy pad before ordinary jump movement consumes grounding.");
            Assert.IsFalse(player.CanActivatePad, "Launch must apply the per-player activation lockout.");
            Assert.IsTrue(player.HasPadCharge, "Using an enemy pad must not spend placement charge.");
            Object.Destroy(padObject);
            tutorial.SkipTutorial();
            yield return null;
            Assert.AreEqual(0, Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length);
            Assert.IsNotNull(GameObject.Find("Home"), "Tutorial completion must offer an offline home journey.");
            var game = GameBootstrap.Instance;
            game.StartOfflinePractice();
            yield return null;
            var match = Object.FindFirstObjectByType<MatchController>();
            Assert.AreEqual(6, match.ActivePlayerCount);
            Assert.AreEqual(6, Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length);
            var pause = game.GetComponent<OfflinePauseMenu>();
            pause.Open();
            Assert.IsTrue(pause.IsOpen);
            Assert.AreEqual(0, Time.timeScale);
            Assert.IsFalse(game.touchInput.enabled);
            pause.Close();
            Assert.AreEqual(1, Time.timeScale);
            Assert.IsTrue(game.touchInput.enabled);
            match.OnReturnHome();
            yield return null;
            Assert.AreEqual(0, match.ActivePlayerCount);
            Assert.AreEqual(0, Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length);
            game.StartOfflinePractice();
            yield return null;
            Assert.AreEqual(6, match.ActivePlayerCount, "Returning home must allow another practice match.");

            float originalScale = Time.timeScale;
            try
            {
                Time.timeScale = 20f;
                float deadline = Time.realtimeSinceStartup + 40f;
                while (!game.matchHUD.resultsPanel.activeSelf && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.IsTrue(game.matchHUD.resultsPanel.activeSelf, "Offline match must reach results within regulation plus overtime.");
            }
            finally { Time.timeScale = originalScale; }
            match.OnPlayAgain();
            yield return null;
            Assert.AreEqual(6, match.ActivePlayerCount);
            Assert.AreEqual(6, Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length);
        }

        [UnityTest]
        public IEnumerator LaterLaunch_OpensHome_LabelsBots_AndRotatesObjective()
        {
            PlayerPrefs.SetInt(GameBootstrap.TutorialDoneKey, 1);
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            Assert.IsNotNull(GameObject.Find("Home"), "After the first run the game must open on Home, not force the tutorial.");
            Assert.AreEqual(0, Object.FindObjectsByType<TutorialController>(FindObjectsSortMode.None).Length);

            var game = GameBootstrap.Instance;
            game.StartOfflinePractice();
            yield return null;
            var match = MatchController.Current;
            Assert.IsNotNull(match);
            int bots = 0;
            foreach (var p in match.Players)
                if (p.IsBot) { bots++; StringAssert.StartsWith("[BOT] ", p.DisplayName, "Bots must always be labeled."); }
            Assert.AreEqual(5, bots);
            StringAssert.Contains("ARC", game.matchHUD.weaponButtons[0].nameText.text);
            StringAssert.Contains("ZIP", game.matchHUD.weaponButtons[1].nameText.text);
            StringAssert.Contains("BRICK", game.matchHUD.weaponButtons[2].nameText.text);
            Assert.AreEqual(Updraft.Config.ZoneId.Center, match.ScoringZone);

            float originalScale = Time.timeScale;
            try
            {
                Time.timeScale = 20f;
                float deadline = Time.realtimeSinceStartup + 20f;
                while (match.MatchTime < 35f && Time.realtimeSinceStartup < deadline) yield return null;
            }
            finally { Time.timeScale = originalScale; }
            Assert.GreaterOrEqual(match.MatchTime, 35f);
            Assert.AreEqual(Updraft.Config.ZoneId.West, match.ScoringZone);
            Assert.AreEqual(game.mapConfig.westZone, match.ActiveObjectivePosition, "Bots read this position, so it must follow the rotation.");
            match.OnReturnHome();
            yield return null;
        }

        /// <summary>
        /// Gate A touch ergonomics case matrix: rapid taps, holds, drag-fire,
        /// cancel and UI consumption. The second-finger fire acquisition inside
        /// the main regression is the original Gate A defect; this test covers
        /// the remaining ergonomics around it.
        /// </summary>
        [UnityTest]
        public IEnumerator TouchCaseMatrix_RapidTapsHoldsDragFireCancelAndUIConsumption()
        {
            PlayerPrefs.SetInt(GameBootstrap.TutorialDoneKey, 1);
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            var game = GameBootstrap.Instance;
            var input = game.touchInput;
            var screen = InputSystem.AddDevice<Touchscreen>("Ergonomics touchscreen");
            var type = typeof(Updraft.Input.TouchInputManager);
            const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
            Rect fire = (Rect)type.GetField("fireButtonRect", hidden).GetValue(input);
            Rect jump = (Rect)type.GetField("jumpButtonRect", hidden).GetValue(input);
            Rect pad = (Rect)type.GetField("padButtonRect", hidden).GetValue(input);
            Rect reload = (Rect)type.GetField("reloadButtonRect", hidden).GetValue(input);
            try
            {
                game.StartOfflinePractice();
                yield return null;
                var match = Object.FindFirstObjectByType<MatchController>();

                // UI consumption: a touch over a gameplay UGUI control must be
                // consumed - never fire, never drive movement.
                var weaponButton = game.matchHUD.weaponButtons[0].button;
                Assert.IsTrue(weaponButton.gameObject.activeInHierarchy, "Weapon select must be visible during preparation for this test.");
                Vector2 uiPoint = RectTransformUtility.WorldToScreenPoint(null, weaponButton.transform.position);
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 9, phase = UnityEngine.InputSystem.TouchPhase.Began, position = uiPoint });
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 9, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = uiPoint });
                yield return null;
                yield return null;
                Assert.IsFalse(input.FireHeld, "A touch on a UI control must not fire.");
                Assert.AreEqual(Vector2.zero, input.MoveInput, "A touch on a UI control must not move the player.");
                Assert.AreEqual(Vector2.zero, input.AimDelta, "A touch on a UI control must not rotate the camera.");

                // Rapid taps: each quick tap registers pressed and released.
                for (int i = 0; i < 3; i++)
                {
                    InputSystem.QueueStateEvent(screen, new TouchState { touchId = 10, phase = UnityEngine.InputSystem.TouchPhase.Began, position = fire.center });
                    yield return null;
                    Assert.IsTrue(input.FireHeld, $"Tap {i} must acquire fire.");
                    InputSystem.QueueStateEvent(screen, new TouchState { touchId = 10, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = fire.center });
                    yield return null;
                    Assert.IsFalse(input.FireHeld, $"Tap {i} must release fire.");
                }

                // Hold: a long press stays held but jump stays edge-triggered.
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 11, phase = UnityEngine.InputSystem.TouchPhase.Began, position = jump.center });
                yield return null;
                Assert.IsTrue(input.JumpPressed, "Jump press must register.");
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.IsFalse(input.JumpPressed, "Jump must be an edge-triggered press, not a hold-repeat.");
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 11, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = jump.center });
                yield return null;

                // Drag-fire: dragging on the fire button aims while firing.
                // Began and Moved land in different Update frames, so aim is
                // asserted one frame after acquisition.
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 12, phase = UnityEngine.InputSystem.TouchPhase.Began, position = fire.center });
                yield return null;
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 12, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = fire.center + Vector2.right * 40f });
                yield return null;
                Assert.IsTrue(input.FireHeld);
                Assert.Greater(input.AimDelta.x, 0f, "Dragging the fire finger must aim.");
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 12, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = fire.center });
                yield return null;

                // Pad cancel: hold past the preview threshold, drag far off the
                // button, release - the placement must cost nothing.
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 13, phase = UnityEngine.InputSystem.TouchPhase.Began, position = pad.center });
                yield return new WaitForSecondsRealtime(0.25f);
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 13, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = pad.center - new Vector2(pad.width * 3f, 0f) });
                yield return null;
                Assert.IsTrue(input.PadCancelled, "Dragging off the pad button must cancel.");
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 13, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = pad.center - new Vector2(pad.width * 3f, 0f) });
                yield return null;
                Assert.IsFalse(input.PadPressed, "A cancelled pad hold must not place.");
                var player = Object.FindFirstObjectByType<PlayerController>();
                Assert.IsTrue(player.HasPadCharge, "Cancelling must cost nothing.");

                // A fast reload tap must survive native event coalescing.
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 14, phase = UnityEngine.InputSystem.TouchPhase.Began, position = reload.center });
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 14, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = reload.center });
                yield return null;
                Assert.IsTrue(input.ReloadPressed, "A fast reload tap must reach the reload action.");

                match.OnReturnHome();
                yield return null;
            }
            finally
            {
                InputSystem.RemoveDevice(screen);
            }
        }

        [UnityTest]
        public IEnumerator Bots_PlayTheObjective_AndUseLaunchPads()
        {
            PlayerPrefs.SetInt(GameBootstrap.TutorialDoneKey, 1);
            StudySettings.SetCohort("study_pads_on");
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            var game = GameBootstrap.Instance;
            game.StartOfflinePractice();
            yield return null;
            var match = MatchController.Current;

            float originalScale = Time.timeScale;
            try
            {
                // Modest speed-up so bot movement, arming and launches keep realistic physics steps.
                Time.timeScale = 4f;
                float deadline = Time.realtimeSinceStartup + 45f;
                while (match.MatchTime < 100f && Time.realtimeSinceStartup < deadline) yield return null;
            }
            finally { Time.timeScale = originalScale; }
            Assert.GreaterOrEqual(match.MatchTime, 100f);

            int launches = 0;
            float allyZone = 0f, enemyZone = 0f;
            foreach (var p in match.Players)
            {
                if (!p.IsBot) continue;
                launches += p.PadLaunches;
                if (p.TeamId == match.LocalPlayer.TeamId) allyZone += p.ObjectiveSeconds; else enemyZone += p.ObjectiveSeconds;
            }
            Assert.Greater(launches, 0, "Bots must place and ride launch pads (the core mechanic), not just walk.");
            Assert.Greater(allyZone, 10f, "Ally bots must contest the objective.");
            Assert.Greater(enemyZone, 10f, "Enemy bots must contest the objective.");
            match.OnReturnHome();
            yield return null;
        }

        static IEnumerator RunUntil(System.Func<bool> done, float timeScale, float realSeconds)
        {
            float originalScale = Time.timeScale;
            try
            {
                Time.timeScale = timeScale;
                float deadline = Time.realtimeSinceStartup + realSeconds;
                while (!done() && Time.realtimeSinceStartup < deadline) yield return null;
            }
            finally { Time.timeScale = originalScale; }
        }

        /// <summary>
        /// Gate B: Results -> Play again restores the controls, and repeated matches leave
        /// nothing behind (players, bots, pads including owner-ticked ones, colliders,
        /// line renderers, materials).
        /// </summary>
        [UnityTest]
        public IEnumerator RepeatedMatches_RematchRestoresControls_AndLeaksNothing()
        {
            PlayerPrefs.SetInt(GameBootstrap.TutorialDoneKey, 1);
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            var game = GameBootstrap.Instance;
            game.StartOfflinePractice();
            yield return null;
            var match = MatchController.Current;

            const int rounds = 4;
            var colliders = new int[rounds];
            var lines = new int[rounds];
            var materials = new int[rounds];
            for (int round = 0; round < rounds; round++)
            {
                yield return RunUntil(() => match.CurrentPhase == MatchPhase.InProgress, 20f, 10f);
                Assert.AreEqual(MatchPhase.InProgress, match.CurrentPhase);

                // Every real placement is ticked by its owner; one must not outlive the match.
                var local = match.LocalPlayer;
                var data = new LaunchPad(game.padConfig, local.PlayerId, local.TeamId, new Vector3(-36f, 0.05f, -12f), Vector3.right);
                var padObject = new GameObject("Owner-ticked regression pad");
                padObject.transform.position = data.Position;
                padObject.AddComponent<LaunchPadBehaviour>().Initialize(data, true);
                Assert.Greater(LaunchPadBehaviour.All.Count, 0);

                yield return RunUntil(() => game.matchHUD.resultsPanel.activeSelf, 20f, 40f);
                Assert.IsTrue(game.matchHUD.resultsPanel.activeSelf, $"Round {round} must reach Results.");
                Assert.IsFalse(game.touchInput.enabled, "Results hides combat controls.");

                match.OnPlayAgain();
                yield return null;
                yield return null;

                Assert.IsTrue(game.touchInput.enabled, "Play again must hand the controls back.");
                Assert.IsFalse(game.matchHUD.resultsPanel.activeSelf);
                Assert.AreEqual(MatchPhase.Preparation, match.CurrentPhase, "A rematch starts with a fresh preparation phase.");
                Assert.AreEqual(0, LaunchPadBehaviour.All.Count, "No pad survives into the rematch.");
                Assert.AreEqual(0, Object.FindObjectsByType<LaunchPadBehaviour>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(6, PlayerController.All.Count);
                Assert.AreEqual(6, Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length);
                if (PingSystem.Instance != null) Assert.LessOrEqual(PingSystem.Instance.ActivePings.Count, 6, "At most one live ping per player.");

                colliders[round] = Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
                lines[round] = Object.FindObjectsByType<LineRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
                materials[round] = Resources.FindObjectsOfTypeAll<Material>().Length;
            }
            // Round 0 may create lazy singletons (tracer pool, audio); later rounds must be flat.
            Assert.AreEqual(colliders[1], colliders[rounds - 1], "Collider count grows across matches.");
            Assert.AreEqual(lines[1], lines[rounds - 1], "LineRenderer count grows across matches.");
            Assert.AreEqual(materials[1], materials[rounds - 1], "Material count grows across matches.");
            match.OnReturnHome();
            yield return null;
            Assert.AreEqual(0, PlayerController.All.Count);
            Assert.AreEqual(0, LaunchPadBehaviour.All.Count);
        }

        /// <summary>Gate B: prep permissions, and pad damage by real firing (enemy destroys, friendly fire can't).</summary>
        [UnityTest]
        public IEnumerator PrepBlocksDamageAndPads_LiveFire_DestroysEnemyPads_NotFriendly()
        {
            PlayerPrefs.SetInt(GameBootstrap.TutorialDoneKey, 1);
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            var game = GameBootstrap.Instance;
            game.StartOfflinePractice();
            yield return null;
            var match = MatchController.Current;
            var local = match.LocalPlayer;

            Assert.AreEqual(MatchPhase.Preparation, match.CurrentPhase);
            Assert.AreEqual("NOT DURING THIS PHASE", local.TryPlacePad());
            local.ApplyDamage(50f, 99, Updraft.Config.WeaponId.Arc);
            Assert.AreEqual(local.MaxHealth, local.CurrentHealth, "No damage during preparation.");

            yield return RunUntil(() => match.CurrentPhase == MatchPhase.InProgress, 20f, 10f);
            Assert.AreEqual(MatchPhase.InProgress, match.CurrentPhase);
            // Outside the spawn room (no firing from inside it) and away from bots' lanes.
            local.WarpForTraining(new Vector3(-40f, 0.05f, -12f), Quaternion.LookRotation(Vector3.right));
            game.mainCamera.SnapBehindTarget();
            yield return null;
            yield return null;

            foreach (int team in new[] { local.TeamId, 1 - local.TeamId })
            {
                var cam = local.playerCamera.transform;
                var data = new LaunchPad(game.padConfig, 900 + team, team, cam.position + cam.forward * 6f, Vector3.right);
                data.CompleteArming();
                var padObject = new GameObject($"Team {team} shooting pad");
                padObject.transform.position = data.Position;
                // Stand the disc up to face the camera so the crosshair ray meets it.
                padObject.transform.rotation = Quaternion.LookRotation(Vector3.up, -cam.forward);
                var behaviour = padObject.AddComponent<LaunchPadBehaviour>();
                behaviour.Initialize(data);
                Physics.SyncTransforms();

                local.SetExternalInput(Vector2.zero, false, true, false);
                yield return new WaitForSeconds(0.6f);
                local.SetExternalInput(Vector2.zero, false, false, false);
                yield return null;

                if (team == local.TeamId)
                    Assert.AreEqual(game.padConfig.health, data.Health, "Friendly fire must not damage a pad.");
                else
                    Assert.AreEqual(PadState.Destroyed, data.State, "Enemy fire must destroy a 60 HP pad.");
                Object.Destroy(padObject);
                yield return new WaitForSeconds(0.2f);
            }
            match.OnReturnHome();
            yield return null;
        }

        static IEnumerator ExerciseRecordedTouchInput()
        {
            var game = GameBootstrap.Instance;
            var input = game.touchInput;
            var screen = InputSystem.AddDevice<Touchscreen>("Regression touchscreen");
            Rect fire = (Rect)typeof(Updraft.Input.TouchInputManager).GetField("fireButtonRect", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(input);
            Rect pause = (Rect)typeof(Updraft.Input.TouchInputManager).GetField("pauseButtonRect", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(input);
            Vector2 moveOrigin = new Vector2(Screen.width * .25f, Screen.height * .25f);
            try
            {
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = moveOrigin });
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Began, position = fire.center });
                yield return null;
                Assert.IsTrue(input.FireHeld, "A second finger must acquire fire while the first owns movement.");
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = moveOrigin + Vector2.up * 60 });
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = fire.center + Vector2.right * 30 });
                yield return null;
                Assert.Greater(input.MoveInput.y, .5f);
                Assert.IsTrue(input.FireHeld);
                Assert.Greater(input.AimDelta.x, 0, "Dragging the fire finger must aim without stealing the movement finger.");
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = moveOrigin });
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = fire.center });
                yield return null;
                Assert.IsFalse(input.FireHeld);
                Assert.AreEqual(Vector2.zero, input.MoveInput);
                // Pad: hold > 180 ms shows the preview; dragging off the button cancels without placing.
                Rect padRect = (Rect)typeof(Updraft.Input.TouchInputManager).GetField("padButtonRect", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(input);
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 4, phase = UnityEngine.InputSystem.TouchPhase.Began, position = padRect.center });
                yield return null;
                Assert.IsFalse(input.PadPressed, "Pressing the pad button must not place until release.");
                yield return new WaitForSecondsRealtime(0.25f);
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 4, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = padRect.center + Vector2.one });
                yield return null;
                Assert.IsTrue(input.PadPreviewActive, "Holding the pad button past 180 ms must show the preview.");
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 4, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = padRect.center - new Vector2(padRect.width * 3f, 0) });
                yield return null;
                Assert.IsTrue(input.PadCancelled);
                Assert.IsFalse(input.PadPreviewActive);
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 4, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = padRect.center - new Vector2(padRect.width * 3f, 0) });
                yield return null;
                Assert.IsFalse(input.PadPressed, "A cancelled hold must not place a pad.");
                Assert.IsTrue(Object.FindFirstObjectByType<PlayerController>().HasPadCharge, "Cancelling must cost nothing.");

                // Native events may begin AND end before a render frame.
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 3, phase = UnityEngine.InputSystem.TouchPhase.Began, position = pause.center });
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 3, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = pause.center });
                yield return null;
                yield return null;
                Assert.IsTrue(game.GetComponent<OfflinePauseMenu>().IsOpen,
                    "A fast press must survive native event coalescing and reach the pause action.");
            }
            finally
            {
                game.GetComponent<OfflinePauseMenu>().Close();
                InputSystem.RemoveDevice(screen);
            }
        }

        // State/physics regression, not a substitute for a human touch playtest.
        static IEnumerator ExerciseTutorialJourney(TutorialController tutorial, PlayerController player)
        {
            player.SetExternalInput(Vector2.zero, false, false, false);
            player.WarpForTraining(tutorial.moveTargetPosition.position, Quaternion.identity);
            yield return WaitForStep(tutorial, TutorialController.TutorialStep.KillStationaryTarget);
            yield return new WaitForSeconds(.2f);
            var targets = Object.FindObjectsByType<TutorialTarget>(FindObjectsSortMode.None);
            Assert.AreEqual(1, targets.Length, "Holding a completed condition must not spawn repeated advancement coroutines.");
            targets[0].TakeDamage(1000);
            yield return WaitForStep(tutorial, TutorialController.TutorialStep.ReloadWeapon);
            player.SetExternalInput(Vector2.zero, false, false, true);
            yield return null;
            player.SetExternalInput(Vector2.zero, false, false, false);
            yield return new WaitForSeconds(.2f);
            Assert.AreEqual(TutorialController.TutorialStep.ReloadWeapon, tutorial.CurrentStep,
                "Starting reload must not immediately advance the tutorial before reload finishes.");
            yield return WaitForStep(tutorial, TutorialController.TutorialStep.PlacePad);
            yield return new WaitForSeconds(.5f);
            player.TryPlacePad();
            yield return WaitForStep(tutorial, TutorialController.TutorialStep.RideOwnPad);
            LaunchPadBehaviour ownPad = null;
            foreach (var pad in Object.FindObjectsByType<LaunchPadBehaviour>(FindObjectsSortMode.None))
                if (pad.PadData.OwnerId == player.PlayerId) ownPad = pad;
            Assert.IsNotNull(ownPad, "Authored training pad lane must permit real placement.");
            player.WarpForTraining(ownPad.PadData.Position, Quaternion.LookRotation(ownPad.PadData.ForwardDirection));
            Physics.SyncTransforms();
            yield return new WaitForSeconds(.2f);
            player.SetExternalInput(Vector2.zero, true, false, false);
            yield return null;
            player.SetExternalInput(Vector2.zero, false, false, false);
            yield return WaitForStep(tutorial, TutorialController.TutorialStep.UseEnemyPad);
            var enemyPad = FindTrainingEnemyPad();
            player.WarpForTraining(enemyPad.PadData.Position, Quaternion.LookRotation(enemyPad.PadData.ForwardDirection));
            Physics.SyncTransforms();
            yield return new WaitForSeconds(.2f);
            player.SetExternalInput(Vector2.zero, true, false, false);
            yield return null;
            player.SetExternalInput(Vector2.zero, false, false, false);
            yield return WaitForStep(tutorial, TutorialController.TutorialStep.DestroyEnemyPad);
            FindTrainingEnemyPad().ApplyDamage(1000);
            yield return WaitForStep(tutorial, TutorialController.TutorialStep.CaptureZone);
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(TutorialController.TutorialStep.CaptureZone, tutorial.CurrentStep);
            yield return WaitForStep(tutorial, TutorialController.TutorialStep.BotFight);
            var bot = Object.FindFirstObjectByType<Updraft.Bots.BotController>().GetComponent<PlayerController>();
            bot.ApplyDamage(1000, player.PlayerId, player.CurrentWeapon.id);
            yield return null;
            Assert.IsNotNull(GameObject.Find("Home"), "Completing all nine authored steps must return home.");
            Assert.AreEqual(0, Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length);
        }

        static LaunchPadBehaviour FindTrainingEnemyPad()
        {
            foreach (var pad in Object.FindObjectsByType<LaunchPadBehaviour>(FindObjectsSortMode.None))
                if (pad.PadData.OwnerId == 999) return pad;
            Assert.Fail("Training enemy pad was not created.");
            return null;
        }

        static IEnumerator WaitForStep(TutorialController tutorial, TutorialController.TutorialStep expected)
        {
            float deadline = Time.realtimeSinceStartup + 6f;
            while (tutorial.CurrentStep != expected && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(expected, tutorial.CurrentStep);
        }
    }
}
