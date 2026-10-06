using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Hotdrop.Core;
using Hotdrop.Player;
using Hotdrop.UI;

namespace Hotdrop.Tests.PlayMode
{
    /// <summary>Captures real, locally reachable screens, not a component gallery or mockup.
    /// Editor renders are layout evidence only, never Android device acceptance.</summary>
    public class PlayerJourneyVisualEvidenceTests
    {
        int tutorialDone, rigChosen;
        string rig;
        bool hadTutorial, hadRig, hadChosen;

        /// <summary>
        /// Screens that existed in the game but had never been captured, so had never been
        /// compared against the UIUX reference. Kept apart from the 10-02 journey folder so a
        /// parity pass can be re-run without silently mixing two capture sessions.
        /// </summary>
        const string ParityFolder = "parity_2026-10-05";

        [UnitySetUp]
        public IEnumerator LoadFirstRun()
        {
            hadTutorial = PlayerPrefs.HasKey(GameBootstrap.TutorialDoneKey);
            hadRig = PlayerPrefs.HasKey(BodyRigStore.PrefKey);
            hadChosen = PlayerPrefs.HasKey(BodyRigStore.ChosenKey);
            tutorialDone = PlayerPrefs.GetInt(GameBootstrap.TutorialDoneKey);
            rigChosen = PlayerPrefs.GetInt(BodyRigStore.ChosenKey);
            rig = PlayerPrefs.GetString(BodyRigStore.PrefKey);
            Time.timeScale = 1;
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            PlayerPrefs.DeleteKey(GameBootstrap.TutorialDoneKey);
            PlayerPrefs.DeleteKey(BodyRigStore.PrefKey);
            PlayerPrefs.DeleteKey(BodyRigStore.ChosenKey);
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            // Hold the branded startup frame for the whole test. It advances itself after 1.1 s
            // of unscaled time, and in batch mode a single frame can outlast that, which is how
            // the "01_startup" capture once photographed the runner choice. Applied here, before
            // any assertion, so the hold is in place whatever the setup cost.
            if (GameBootstrap.Instance != null)
            {
                var startup = GameBootstrap.Instance.GetComponent<StartupConnectionScreen>();
                // Unity's overloaded null check, not ?. - a destroyed object would still be a live
                // C# reference and the call would throw instead of being skipped.
                if (startup != null) startup.HoldForCapture(true);
            }
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (GameBootstrap.Instance != null)
            {
                var startup = GameBootstrap.Instance.GetComponent<StartupConnectionScreen>();
                if (startup != null) startup.HoldForCapture(false);
            }
            RestoreInt(GameBootstrap.TutorialDoneKey, hadTutorial, tutorialDone);
            RestoreInt(BodyRigStore.ChosenKey, hadChosen, rigChosen);
            if (hadRig) PlayerPrefs.SetString(BodyRigStore.PrefKey, rig); else PlayerPrefs.DeleteKey(BodyRigStore.PrefKey);
            Time.timeScale = 1;
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            var main = SceneManager.GetSceneByName("Main");
            SceneManager.SetActiveScene(SceneManager.CreateScene("VisualEvidenceCleanup"));
            if (main.IsValid() && main.isLoaded) yield return SceneManager.UnloadSceneAsync(main);
            yield return null;
        }

        static void RestoreInt(string key, bool existed, int value)
        {
            if (existed) PlayerPrefs.SetInt(key, value); else PlayerPrefs.DeleteKey(key);
        }

        [UnityTest]
        public IEnumerator FirstRunHomeSettingsAndSupport_CaptureRealScreensAtBothLandscapeRatios()
        {
            var game = GameBootstrap.Instance;
            Assert.IsNotNull(game);
            var canvas = game.matchHUD.GetComponent<Canvas>();
            // Startup is transient; capture it before deliberately advancing the live route.
            // It also advances itself after 1.1 s, and batch-mode scene load plus test setup can
            // take longer than that - which is how this capture once photographed the runner
            // choice and filed it as "01_startup". The hold is the screen's own hook, so the frame
            // photographed is the real first frame whatever the setup cost.
            var startup = game.GetComponent<StartupConnectionScreen>();
            yield return null;
            Capture(canvas, "01_startup", 1920);
            Capture(canvas, "01_startup", 2400);
            startup.HoldForCapture(false);
            startup.Continue();
            yield return null;
            Assert.IsNotNull(GameObject.Find("RigChoice"));
            yield return CaptureBoth(canvas, "02_runner_choice");
            GameObject.Find("Rig_rig_female").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(BodyRigStore.Chosen, "Selection cannot save until CONTINUE.");
            GameObject.Find("RigContinue").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.AreEqual(BodyRig.Female, BodyRigStore.Current);
            game.ReturnToHome();
            yield return null;
            Assert.IsNotNull(GameObject.Find("Home"));
            yield return CaptureBoth(canvas, "03_home");
            Assert.IsNull(GameObject.Find("SHOP"), "Shop must remain absent from Home.");
            game.GetComponent<OfflinePauseMenu>().OpenSettings();
            yield return null;
            yield return CaptureBoth(canvas, "10_settings");
            foreach (string tab in new[] { "GRAPHICS", "AUDIO", "ACCESSIBILITY", "ACCOUNT / SUPPORT" })
            {
                GameObject.Find("SettingsTab_" + tab).GetComponent<Button>().onClick.Invoke();
                yield return null;
                Assert.IsNotNull(GameObject.Find("Row_Section_" + tab), "The selected settings panel must be visible.");
                yield return CaptureBoth(canvas, "10_settings_" + tab.ToLowerInvariant().Replace(" / ", "_"));
            }
            game.GetComponent<OfflinePauseMenu>().Close();
            PlayerProductScreens.Open(game, GameObject.Find("Home"), "Support");
            yield return null;
            yield return CaptureBoth(canvas, "17_support");
            GameObject.Find("PlayerProductScreen").transform.Find("Nav_Bar/Nav_Back").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.IsNotNull(GameObject.Find("Home"), "BACK must return to the actual Home.");
        }

        static IEnumerator CaptureBoth(Canvas canvas, string label)
        {
            foreach (int width in new[] { 1920, 2400 })
            {
                yield return null;
                Capture(canvas, label, width);
            }
        }

        /// <summary>Uses sample room data only to compare the roster geometry against Screen 05.
        /// Real party membership and readiness are still supplied by the backend at runtime.</summary>
        [UnityTest]
        public IEnumerator PartyRoomLayoutFixture_CaptureBothLandscapeRatios()
        {
            var game = GameBootstrap.Instance;
            var canvas = game.matchHUD.GetComponent<Canvas>();
            game.GetComponent<StartupConnectionScreen>().Continue();
            yield return null;
            GameObject.Find("Rig_rig_female").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("RigContinue").GetComponent<Button>().onClick.Invoke();
            game.ReturnToHome();
            yield return null;
            PlayerProductScreens.Open(game, GameObject.Find("Home"), "Support");
            var self = BackendClient.Instance.PlayerId;
            var party = new PartyDto
            {
                code = "K7X9-M2W4",
                leaderId = self,
                memberIds = new List<string> { self, "fixture-member-1", "fixture-member-2" },
                readyIds = new List<string> { self, "fixture-member-1", "fixture-member-2" },
                members = new[]
                {
                    new PartyMemberDto { playerId = self, alias = "PLAYER", isReady = true },
                    new PartyMemberDto { playerId = "fixture-member-1", alias = "KESTREL_99", isReady = true },
                    new PartyMemberDto { playerId = "fixture-member-2", alias = "VECTOR_APEX", isReady = true }
                }
            };
            var render = typeof(PlayerProductScreens).GetMethod("RenderParty", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(render);
            render.Invoke(null, new object[] { party });
            yield return null;
            Assert.IsNotNull(GameObject.Find("PartyRoomVisual"));
            Capture(canvas, "05_party_layout_fixture", 1920, ParityFolder);
            Capture(canvas, "05_party_layout_fixture", 2400, ParityFolder);
            GameObject.Find("HomeButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.IsNotNull(GameObject.Find("Home"));
        }

        [UnityTest]
        public IEnumerator MatchSearchLayoutFixture_CaptureBothLandscapeRatios()
        {
            var game = GameBootstrap.Instance;
            var canvas = game.matchHUD.GetComponent<Canvas>();
            var host = new GameObject("SearchFixture", typeof(RectTransform));
            host.transform.SetParent(canvas.transform, false);
            var rect = host.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var party = new PartyDto
            {
                members = new[]
                {
                    new PartyMemberDto { playerId = "self", alias = "PLAYER", isReady = true },
                    new PartyMemberDto { playerId = "second", alias = "KESTREL_99", isReady = true },
                    new PartyMemberDto { playerId = "third", alias = "VECTOR_APEX", isReady = true }
                }
            };
            MatchSearchView.Show(host, 48, "US East", party, "self", true, () => { }, () => { });
            yield return null;
            Capture(canvas, "06_search_layout_fixture", 1920, ParityFolder);
            Capture(canvas, "06_search_layout_fixture", 2400, ParityFolder);
            Object.Destroy(host);
        }

        /// <summary>Layout-only fixture for gated pages; these values are never used by runtime navigation.</summary>
        [UnityTest]
        public IEnumerator GoalsLayoutsFixture_CaptureBothLandscapeRatios()
        {
            var canvas = GameBootstrap.Instance.matchHUD.GetComponent<Canvas>();
            var host = new GameObject("GoalsFixture", typeof(RectTransform));
            host.transform.SetParent(canvas.transform, false);
            var rect = host.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var goals = new GoalsDto
            {
                unlocked = true, eligiblePublicMatches = 2, seals = 7,
                templates = new[]
                {
                    new GoalTemplateDto { id = "finish_together", target = 2 },
                    new GoalTemplateDto { id = "hold_the_relay", target = 45 },
                    new GoalTemplateDto { id = "build_a_team_route", target = 2 }
                },
                goals = new[]
                {
                    new GoalInstanceDto { templateId = "finish_together", progress = 1, target = 2, status = "paused" },
                    new GoalInstanceDto { templateId = "hold_the_relay", progress = 45, target = 45, status = "active" }
                },
                mastery = new[]
                {
                    new MasteryProgressDto { trackId = "relay_specialist", weaponId = "", progress = 412 },
                    new MasteryProgressDto { trackId = "route_builder", weaponId = "", progress = 18 },
                    new MasteryProgressDto { trackId = "weapon_familiarity", weaponId = "ARC", progress = 28 }
                },
                relayThresholds = new[] { 60, 300, 1200 },
                routeThresholds = new[] { 3, 12, 40 },
                weaponThresholds = new[] { 5, 20, 60 }
            };
            GoalsPagesView.Contracts(host, goals, () => { }, () => { }, _ => { });
            yield return null;
            Capture(canvas, "12_contracts_layout_fixture", 1920, ParityFolder);
            Capture(canvas, "12_contracts_layout_fixture", 2400, ParityFolder);
            GoalsPagesView.Mastery(host, goals, () => { }, () => { });
            yield return null;
            Capture(canvas, "13_mastery_layout_fixture", 1920, ParityFolder);
            Capture(canvas, "13_mastery_layout_fixture", 2400, ParityFolder);
            Object.Destroy(host);
        }

        [UnityTest]
        public IEnumerator ContractsAndMastery_AreReachableReadOnlyWhenProgressionIsGated()
        {
            var game = GameBootstrap.Instance;
            var canvas = game.matchHUD.GetComponent<Canvas>();
            game.GetComponent<StartupConnectionScreen>().Continue();
            yield return null;
            GameObject.Find("Rig_rig_female").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("RigContinue").GetComponent<Button>().onClick.Invoke();
            game.ReturnToHome();
            yield return null;
            Assert.IsFalse(ContractsMasteryFeature.Enabled);
            GameObject.Find("MORE").GetComponent<Button>().onClick.Invoke();
            yield return null;
            var menuScroll = GameObject.Find("More").transform.Find("Nav_Scroll")
                .GetComponent<ScrollRect>();
            Canvas.ForceUpdateCanvases();
            menuScroll.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
            AssertRaycastReachesButton(canvas, GameObject.Find("CONTRACTS"));
            AssertRaycastReachesButton(canvas, GameObject.Find("MASTERY"));
            GameObject.Find("CONTRACTS").GetComponent<Button>().onClick.Invoke();
            yield return null;
            var visual = GameObject.Find("GoalsVisual");
            Assert.IsNotNull(visual);
            var firstDirective = visual.transform.Find("Viewport/Page/Directive_0");
            Assert.IsNotNull(firstDirective);
            Assert.IsFalse(firstDirective.Find("Select").GetComponent<Button>().interactable,
                "Gated contracts cannot select or claim rewards.");
            Capture(canvas, "12_contracts_read_only_v2", 1920, ParityFolder);
            Capture(canvas, "12_contracts_read_only_v2", 2400, ParityFolder);
            visual.transform.Find("Footer/Other").GetComponent<Button>().onClick.Invoke();
            yield return null;
            visual = GameObject.Find("GoalsVisual");
            Assert.IsNotNull(visual.transform.Find("Viewport/Page/Track_01"));
            Capture(canvas, "13_mastery_read_only_v2", 1920, ParityFolder);
            Capture(canvas, "13_mastery_read_only_v2", 2400, ParityFolder);
            visual.transform.Find("Footer/Back").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.IsNotNull(GameObject.Find("Home"));
        }

        [UnityTest]
        public IEnumerator ShopConcept_IsolatedFromHome_CaptureBothLandscapeRatios()
        {
            var game = GameBootstrap.Instance;
            var canvas = game.matchHUD.GetComponent<Canvas>();
            game.GetComponent<StartupConnectionScreen>().Continue();
            yield return null;
            GameObject.Find("Rig_rig_female").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("RigContinue").GetComponent<Button>().onClick.Invoke();
            game.ReturnToHome();
            yield return null;
            Assert.IsNull(GameObject.Find("SHOP"), "The concept must stay outside the Home flow.");
            PlayerProductScreens.Open(game, GameObject.Find("Home"), "Shop");
            yield return null;
            Assert.IsNotNull(GameObject.Find("ShopConceptVisual"));
            Assert.IsNull(GameObject.Find("Purchase"), "No billing action is offered by the concept.");
            Capture(canvas, "18_shop_final", 1920, ParityFolder);
            Capture(canvas, "18_shop_final", 2400, ParityFolder);
            GameObject.Find("ShopConceptVisual").transform.Find("Footer/Back")
                .GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.IsNotNull(GameObject.Find("Home"));
        }

        [UnityTest]
        public IEnumerator NewPageActions_AreActualTouchTargets()
        {
            var game = GameBootstrap.Instance;
            var canvas = game.matchHUD.GetComponent<Canvas>();
            game.GetComponent<StartupConnectionScreen>().Continue();
            yield return null;
            GameObject.Find("Rig_rig_female").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("RigContinue").GetComponent<Button>().onClick.Invoke();
            game.ReturnToHome();
            yield return null;
            game.OpenOfflineLibrary();
            yield return null;
            AssertRaycastReachesButton(canvas, GameObject.Find("Challenge_ch_basic_route"));
            AssertRaycastReachesButton(canvas, GameObject.Find("OfflineChallengeVisual")
                .transform.Find("Footer/DeployChallenge").gameObject);
            var challengeScroll = GameObject.Find("OfflineChallengeVisual")
                .transform.Find("Viewport").GetComponent<ScrollRect>();
            Canvas.ForceUpdateCanvases();
            challengeScroll.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
            AssertRaycastReachesButton(canvas, GameObject.Find("StartChallenge"));
            game.ReturnToHome();
            yield return null;
            PlayerProductScreens.Open(game, GameObject.Find("Home"), "Support");
            yield return null;
            AssertRaycastReachesButton(canvas, GameObject.Find("ManageAccount"));
            AssertRaycastReachesButton(canvas, GameObject.Find("DeleteAccount"));
            GameObject.Find("SupportHubVisual").transform.Find("TopBar/Back").GetComponent<Button>().onClick.Invoke();
            yield return null;
            PlayerProductScreens.Open(game, GameObject.Find("Home"), "Appearance");
            yield return null;
            AssertRaycastReachesButton(canvas, GameObject.Find("InventoryTab_STYLES"));
            AssertRaycastReachesButton(canvas, GameObject.Find("ChangeRunner"));
            GameObject.Find("AppearanceLockerVisual").transform.Find("Footer/Back").GetComponent<Button>().onClick.Invoke();
            yield return null;
            PlayerProductScreens.Open(game, GameObject.Find("Home"), "Profile");
            yield return null;
            AssertRaycastReachesButton(canvas, GameObject.Find("OperativeProfileVisual").transform.Find("Footer/Appearance").gameObject);
            GameObject.Find("OperativeProfileVisual").transform.Find("Footer/Back").GetComponent<Button>().onClick.Invoke();
            yield return null;
            PlayerProductScreens.Open(game, GameObject.Find("Home"), "Shop");
            yield return null;
            AssertRaycastReachesButton(canvas, GameObject.Find("ShopConceptVisual").transform.Find("Footer/Back").gameObject);
        }

        static void AssertRaycastReachesButton(Canvas canvas, GameObject target)
        {
            Assert.IsNotNull(target);
            Assert.IsNotNull(target.GetComponent<Button>());
            Canvas.ForceUpdateCanvases();
            var rect = target.GetComponent<RectTransform>();
            var screen = RectTransformUtility.WorldToScreenPoint(null,
                rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = screen };
            var hits = new List<RaycastResult>();
            canvas.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            Assert.IsTrue(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == target.GetComponent<Button>(),
                target.name + " is visible but does not receive the touch raycast.");
        }

        /// <summary>
        /// Drives a real product page through its own entry point, lets its asynchronous load
        /// settle, captures it, and proves BACK lands on the real Home. Capturing without the
        /// settle frames would photograph a loading row that no player ever sees.
        /// </summary>
        IEnumerator OpenCaptureAndReturn(GameBootstrap game, Canvas canvas, string page, string label)
        {
            PlayerProductScreens.Open(game, GameObject.Find("Home"), page);
            float started = Time.realtimeSinceStartup;
            int frames = 0;
            while (Time.realtimeSinceStartup - started < 5f)
            {
                var screenStatus = GameObject.Find("PlayerProductScreen")?.transform.Find("Nav_Status")
                    ?.GetComponent<TMPro.TextMeshProUGUI>();
                if (frames++ >= 15 && screenStatus != null && !screenStatus.text.StartsWith("Loading")) break;
                yield return null;
            }
            Capture(canvas, label, 1920, ParityFolder);
            Capture(canvas, label, 2400, ParityFolder);
            var screen = GameObject.Find("PlayerProductScreen");
            Assert.IsNotNull(screen, page + " must build a real screen to be compared.");
            screen.transform.Find("Nav_Bar/Nav_Back").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.IsNotNull(GameObject.Find("Home"), page + " BACK must return to the actual Home.");
        }

        /// <summary>
        /// Screens 04, 15 and 16 existed and were reachable but had never been rendered, so they
        /// had never been compared against the reference. No backend, no art, no gate: each is
        /// driven through the same public entry point a player taps.
        /// </summary>
        [UnityTest]
        public IEnumerator TrainingAppearanceAndProfile_CaptureScreensNeverCompared()
        {
            var game = GameBootstrap.Instance;
            var canvas = game.matchHUD.GetComponent<Canvas>();
            game.GetComponent<StartupConnectionScreen>().Continue();
            yield return null;
            GameObject.Find("Rig_rig_female").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("RigContinue").GetComponent<Button>().onClick.Invoke();
            yield return null;

            game.StartTutorial();
            yield return null;
            yield return null;
            var training = Object.FindFirstObjectByType<TutorialUI>();
            Assert.IsNotNull(training, "Training must show its instruction surface before it can be compared.");
            Assert.IsTrue(training.gameObject.activeInHierarchy, "The training card must actually be on screen.");
            AssertSingleTrainingCard(canvas);
            // The contrast guard below runs inside Capture, on the pixels it just wrote to disk.
            // Guided training has its own instruction card, not the Relay HUD panel set.
            Capture(canvas, "04_training", 1920, ParityFolder, true, false, false);

            game.ReturnToHome();
            yield return null;
            Assert.IsNotNull(GameObject.Find("Home"), "Leaving training must land on the real Home.");

            yield return OpenCaptureAndReturn(game, canvas, "Appearance", "15_appearance_checked");
            yield return OpenCaptureAndReturn(game, canvas, "Profile", "16_profile_verified");
        }

        [UnityTest]
        public IEnumerator AppearanceCategoryTab_ChangesTheVisibleOwnedItems()
        {
            var game = GameBootstrap.Instance;
            game.GetComponent<StartupConnectionScreen>().Continue();
            yield return null;
            GameObject.Find("Rig_rig_female").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("RigContinue").GetComponent<Button>().onClick.Invoke();
            yield return null;
            game.ReturnToHome();
            yield return null;
            PlayerProductScreens.Open(game, GameObject.Find("Home"), "Support");
            GameObject.Find("PlayerProductScreen").transform.Find("Nav_Bar/Nav_Title")
                .GetComponent<TMPro.TextMeshProUGUI>().text = "APPEARANCE · TEST INVENTORY";
            var type = typeof(PlayerProductScreens);
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
            type.GetMethod("ClearRows", flags).Invoke(null, null);
            type.GetMethod("BuildLocalAppearance", flags).Invoke(null, null);
            var items = new[]
            {
                new InventoryItemDto { id = 1, cosmeticId = "body_standard", acquiredVia = "starter" },
                new InventoryItemDto { id = 2, cosmeticId = "wrap_hazard", acquiredVia = "unlock" }
            };
            type.GetField("appearanceCategory", flags).SetValue(null, "Body Presentation");
            type.GetMethod("RenderAppearanceInventory", flags).Invoke(null, new object[] { items, "body_standard", "wrap_hazard", "" });
            var content = GameObject.Find("PlayerProductScreen").transform.Find("Nav_Scroll/Viewport/Content");
            var inventory = content.Find("Row_AppearanceHero/AppearanceInformation");
            Assert.IsNotNull(inventory.Find("Row_InventoryItem_1"));
            inventory.Find("Row_InventoryTabs/InventoryTab_WRAPS").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.IsNull(inventory.Find("Row_InventoryItem_1"));
            Assert.IsNotNull(inventory.Find("Row_InventoryItem_2"));
            Capture(game.matchHUD.GetComponent<Canvas>(), "15_appearance_owned_fixture", 1920, ParityFolder);
            type.GetField("appearanceCategory", flags).SetValue(null, "Body Presentation");
            GameObject.Find("PlayerProductScreen").transform.Find("Nav_Bar/Nav_Back").GetComponent<Button>().onClick.Invoke();
        }

        /// <summary>
        /// Screens 07, 09 and 11 existed and were reachable but had never been rendered. A real
        /// offline match is played to reach them rather than calling the panel methods directly:
        /// the loadout screen only exists during preparation, and results only exist because the
        /// match actually ended.
        /// </summary>
        [UnityTest]
        public IEnumerator PreparationPadFeedbackAndResults_CaptureScreensNeverCompared()
        {
            var game = GameBootstrap.Instance;
            PlayerPrefs.SetInt(GameBootstrap.TutorialDoneKey, 1);
            game.GetComponent<StartupConnectionScreen>().Continue();
            game.ReturnToHome();
            yield return null;
            GameObject.Find("PLAY 3-MINUTE RELAY").GetComponent<Button>().onClick.Invoke();
            yield return null;
            var canvas = game.matchHUD.GetComponent<Canvas>();

            // Preparation lasts 8 s of real time and is the only phase the loadout screen lives in.
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(Hotdrop.Match.MatchPhase.Preparation, game.matchController.CurrentPhase,
                "Screen 07 only exists during preparation; capturing later photographs the wrong screen.");
            Capture(canvas, "07_preparation", 1920, ParityFolder, true);

            // The next screens must be captured after the 8 s preparation and 3 s countdown.
            // Capturing immediately after Screen 07 only photographs the loadout overlay again.
            float liveDeadline = Time.realtimeSinceStartup + 20f;
            while (game.matchController.CurrentPhase != Hotdrop.Match.MatchPhase.InProgress &&
                   Time.realtimeSinceStartup < liveDeadline) yield return null;
            Assert.AreEqual(Hotdrop.Match.MatchPhase.InProgress, game.matchController.CurrentPhase);
            var local = game.matchController.LocalPlayer;
            Assert.IsNotNull(local, "A live match must have a local player for the gameplay capture.");
            // Photograph the playable open roof instead of the protected spawn room.
            local.WarpForTraining(new Vector3(-35f, .55f, 10f), Quaternion.Euler(0f, 90f, 0f));
            yield return new WaitForSeconds(.25f);
            Capture(canvas, "08_live_hud_cladding", 1920, ParityFolder, true);

            // Screen 09: place pads for real and let the game's own rejection path speak. One
            // placement consumes the charge, so the second is refused by the real rule.
            Assert.IsNotNull(local, "A live match must have a local player to place a pad with.");
            // Keep the open-roof training pose from Screen 08 so the placed disc and its
            // trajectory feedback stay visible instead of hiding behind the spawn ramp.
            yield return null;
            float groundDeadline = Time.realtimeSinceStartup + 3f;
            while (!local.IsGrounded && Time.realtimeSinceStartup < groundDeadline) yield return null;
            string first = local.TryPlacePad();
            if (first == null)
            {
                yield return null;
                Capture(canvas, "09_pad_placed_fixture", 1920, ParityFolder, true);
            }
            string rejection = first ?? local.TryPlacePad();
            Assert.IsNotNull(rejection, "Two placements in a row cannot both succeed; the HUD feedback would be unreachable.");
            yield return null;
            Capture(canvas, "09_pad_feedback", 1920, ParityFolder, true);

            var pause = game.GetComponent<OfflinePauseMenu>();
            pause.Open();
            yield return null;
            Capture(canvas, "10_match_menu_offline_v3", 1920, ParityFolder, true, assertHud: false);
            pause.Close();

            // Play the match out and photograph the results screen the game produced itself.
            float scale = Time.timeScale;
            Time.timeScale = 20f;
            float deadline = Time.realtimeSinceStartup + 90f;
            while (!game.matchHUD.resultsPanel.activeSelf && Time.realtimeSinceStartup < deadline) yield return null;
            Time.timeScale = scale;
            Assert.IsTrue(game.matchHUD.resultsPanel.activeSelf, "A played match must reach Results to be compared.");
            yield return null;
            // The results overlay replaces the Relay HUD panels entirely.
            Capture(canvas, "11_results", 1920, ParityFolder, true, false, false);
        }

        /// <summary>
        /// The behind-camera objective marker, on a rendered frame, through the real path.
        ///
        /// <para>Every other capture in this class had the objective inside the frustum, so the
        /// branch that runs when the player has turned their back on the zone - the state a
        /// six-player match spends most of its time in - had never been photographed, and could
        /// not be: that branch read <c>Screen.safeArea</c> itself while this harness simulates the
        /// screen through <c>SafeAreaRoot</c>, so it answered from a screen nobody was rendering.
        /// It now takes its edge from <c>SafeAreaRoot.ContentRectFor</c> like every other HUD
        /// element, and this test turns the player round so the branch really runs and then
        /// inspects the frame it produced.</para>
        ///
        /// <para>Asserted here, on the rendered frame: that the objective genuinely is behind the
        /// camera; that the marker landed at a real canvas position inside the canvas rather than
        /// tens of thousands of units off it, which is what writing a screen-space position into a
        /// canvas-local slot used to do; and that there were panels and a caption to measure, so
        /// the capture's own clearance assertion could not have returned early. The rule across
        /// all twenty-four bearings is
        /// <c>HudOwnershipTests.BehindCamera_TheEdgeMarkerGetsTheSameMeasuredClearanceAsTheProjectedOne</c>;
        /// this is the one frame that proves it is reached, not just the arithmetic.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator PreparationWithTheObjectiveBehindThePlayer_CapturesTheEdgeMarker()
        {
            var game = GameBootstrap.Instance;
            PlayerPrefs.SetInt(GameBootstrap.TutorialDoneKey, 1);
            game.GetComponent<StartupConnectionScreen>().Continue();
            yield return null;
            // The same call the Home screen's PLAY button makes, which hides Home, stops the
            // tutorial and starts the relay. Driven directly so this test depends on the match and
            // the HUD rather than on Home's layout, which is not what is under test here.
            game.StartOfflinePractice();
            var canvas = game.matchHUD.GetComponent<Canvas>();

            // Waited for rather than slept through: the loadout screen only exists during
            // preparation, and a fixed delay that lands before the player exists photographs
            // nothing at all.
            float deadline = Time.realtimeSinceStartup + 15f;
            while (Time.realtimeSinceStartup < deadline &&
                   (game.matchController.LocalPlayer == null ||
                    game.matchController.CurrentPhase != Hotdrop.Match.MatchPhase.Preparation))
                yield return null;
            Assert.AreEqual(Hotdrop.Match.MatchPhase.Preparation, game.matchController.CurrentPhase,
                "Screen 07 only exists during preparation; capturing later photographs the wrong screen.");

            var local = game.matchController.LocalPlayer;
            Assert.IsNotNull(local, "A live match must have a local player to turn round.");
            Assert.IsNotNull(game.mainCamera, "The match camera is what the marker is placed against.");
            var root = canvas.GetComponent<RectTransform>();
            var world = game.matchController.ActiveObjectivePosition + Vector3.up * 1.5f;
            Assert.IsTrue(Hotdrop.UI.HudMarkerLayer.ProjectToCanvas(game.matchHUD.mainCamera, root, world,
                out var onScreen),
                "the objective starts inside the frustum, so this frame would photograph the ordinary " +
                $"path. It projected to {onScreen}; if that stops being true the setup needs reworking.");

            // Two frames, one per end of the arc. Straight behind, the edge marker lands in the
            // middle of the bottom edge and the rule has to find the slot beside the weapon panel.
            // Behind and over the left shoulder, the marker's X is pinned to the left edge; this
            // used to be an unavoidable residual against the legacy teammate list, which the
            // screen 08 HUD no longer shows during preparation, so both frames carry the full
            // clearance gate. Both are photographed; neither is chosen for its result.
            yield return FaceObjectiveFromBehind(game, world, 0f);
            AssertObjectiveIsBehind(root, game.matchHUD.mainCamera, world);
            Capture(canvas, "07b_preparation_objective_behind", 1920, ParityFolder, true);

            yield return FaceObjectiveFromBehind(game, world, -45f);
            AssertObjectiveIsBehind(root, game.matchHUD.mainCamera, world);
            Capture(canvas, "07c_preparation_objective_behind_left", 1920, ParityFolder, true);
        }

        /// <summary>
        /// Faces the local player so the objective sits <paramref name="degrees"/> behind their
        /// left shoulder: 0 is straight behind, +90 is behind and to the left.
        ///
        /// <para>Less than 90, always. The objective starts 180 degrees off the axis - directly
        /// behind - so turning by more than 90 swings it back into the frustum, where the HUD takes
        /// the on-screen branch and clamps it to the canvas edge instead of reporting an edge
        /// marker. 45 puts it 45 degrees past the plane and well to the side, which is the arc the
        /// residual lives on.</para>
        ///
        /// <para>The rig's own smoothing is frame-rate dependent: <c>rotationLerpSpeed *
        /// deltaTime</c> exceeds 1 in a batch run, so the camera overshoots and rings around the
        /// yaw it was asked for instead of settling on it. The transform is therefore put on the
        /// yaw the rig is already holding - <c>SnapBehindTarget</c> has set its angles to that
        /// value - so LateUpdate has nothing left to move. Waiting for the rig's convergence
        /// photographs a mid-swing state, which is a real frame of the marker and of nothing else.</para>
        /// </summary>
        static IEnumerator FaceObjectiveFromBehind(GameBootstrap game, Vector3 world, float degrees)
        {
            var local = game.matchController.LocalPlayer;
            // The camera the marker is placed against, not whatever Camera.main resolves to. A
            // frame measured through a different camera proves nothing about the HUD's own.
            var hud = game.matchHUD;
            // The rig and the HUD's camera are two components on one GameObject; it is the
            // transform that has to agree, and if it ever does not the capture would be measuring
            // a camera the marker never saw.
            Assert.AreEqual(game.mainCamera.transform, hud.mainCamera.transform,
                "the HUD must place its markers against the match camera; if these have diverged, " +
                "the capture would be measuring a camera the marker never saw.");
            Vector3 away = local.Position - world;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = Vector3.back;
            var facing = Quaternion.Euler(0f, degrees, 0f) * Quaternion.LookRotation(away, Vector3.up);
            local.transform.rotation = facing;
            game.mainCamera.SnapBehindTarget();

            float target = facing.eulerAngles.y;
            var look = game.mainCamera.transform.rotation.eulerAngles;
            game.mainCamera.transform.rotation = Quaternion.Euler(look.x, target, 0f);
            yield return null;   // one frame for the rig's position smoothing, not its angles
            float error = Mathf.Abs(Mathf.DeltaAngle(game.mainCamera.transform.eulerAngles.y, target));
            Assert.Less(error, 1f,
                $"the camera drifted {error:0.#} degrees off the yaw it was set to, so this frame " +
                "would photograph a state the rig does not hold.");
        }

        static void AssertObjectiveIsBehind(RectTransform root, UnityEngine.Camera camera, Vector3 world)
        {
            Assert.IsFalse(Hotdrop.UI.HudMarkerLayer.ProjectToCanvas(camera, root, world,
                out var projected),
                "the objective must be behind the camera on this frame, or the behind-camera branch " +
                $"never ran and the capture is just the ordinary one. It projected to {projected}.");
        }

        [UnityTest]
        public IEnumerator HomePlayNow_StartsSixPlayerOfflineMatch_AndReturnsHome()
        {
            var game = GameBootstrap.Instance;
            game.GetComponent<StartupConnectionScreen>().Continue();
            game.ReturnToHome();
            yield return null;
            GameObject.Find("PLAY 3-MINUTE RELAY").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.AreEqual(6, game.matchController.ActivePlayerCount, "Play Now must start a real match without waiting for a server.");
            Assert.IsFalse(GameObject.Find("Home") != null, "Home must not block the match.");
            Assert.IsNull(GameObject.Find("RigChoice"), "Character choice must not cover an active match.");
            foreach (var player in PlayerController.All)
            {
                var body = player.transform.Find("Body");
                Assert.IsNotNull(body.Find("Rig"));
                foreach (Transform child in body)
                    Assert.IsTrue(child.name == "Rig" || child.name == "TeamCue" || child.name == "Cosmetic",
                        "Legacy body geometry must not render alongside the new runner: " + child.name);
            }
            yield return new WaitForSeconds(14);
            Assert.AreEqual(Hotdrop.Match.MatchPhase.InProgress, game.matchController.CurrentPhase);
            Capture(game.matchHUD.GetComponent<Canvas>(), "08_playable_relay", 1920, "playable_2026-10-03", true);
            game.ReturnToHome();
            yield return null;
            Assert.IsNotNull(GameObject.Find("Home"));
            Assert.AreEqual(0, PlayerController.All.Count, "Returning Home cleans up all six match actors.");
        }

        /// <summary>
        /// World-rect of a RectTransform in the current render space, for overlap tests that must
        /// compare two UI elements rather than two RectTransforms' local rects.
        /// </summary>
        static Rect WorldRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float minX = corners[0].x, maxX = corners[0].x, minY = corners[0].y, maxY = corners[0].y;
            for (int i = 1; i < 4; i++)
            {
                minX = Mathf.Min(minX, corners[i].x); maxX = Mathf.Max(maxX, corners[i].x);
                minY = Mathf.Min(minY, corners[i].y); maxY = Mathf.Max(maxY, corners[i].y);
            }
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// A rect's footprint expressed in <paramref name="root"/>'s local units, so it can be
        /// compared with <see cref="Hotdrop.UI.MatchHUD.ActivePanelRects"/> output.
        /// </summary>
        static Rect CanvasLocalRect(RectTransform root, RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                var local = root.InverseTransformPoint(corners[i]);
                minX = Mathf.Min(minX, local.x); maxX = Mathf.Max(maxX, local.x);
                minY = Mathf.Min(minY, local.y); maxY = Mathf.Max(maxY, local.y);
            }
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>Canvas-local bounds of a text element's real ink, not its container box.</summary>
        static Rect CanvasLocalRect(RectTransform root, RectTransform rt, TMPro.TMP_Text text)
        {
            var bounds = text.textBounds;
            if (bounds.size.sqrMagnitude == 0f) return CanvasLocalRect(root, rt);
            Vector3 min = rt.TransformPoint(bounds.min);
            Vector3 max = rt.TransformPoint(bounds.max);
            var a = root.InverseTransformPoint(min);
            var b = root.InverseTransformPoint(max);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y),
                Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        static RectTransform FindNamed(RectTransform root, string name)
        {
            var direct = root.Find(name) as RectTransform;
            if (direct != null) return direct;
            foreach (var candidate in root.GetComponentsInChildren<RectTransform>(true))
                if (candidate.name == name) return candidate;
            return null;
        }

        /// <summary>
        /// There must be exactly one training card.
        ///
        /// <para>The parity capture found two <c>Training instruction</c> Graphics at the same rect
        /// (Unity draw depths -11 and -16). <c>GameBootstrap.StartTutorial</c> rebuilt the view on
        /// every entry and nothing removed the old one, so a second entry - reachable by a player,
        /// since first run auto-starts the tutorial and Home &gt; More &gt; TRAINING starts it again -
        /// stacked a second card on top and its 93%-opaque plate composited over the first card's
        /// caption. Asserting the count keeps that from coming back silently.</para>
        /// </summary>
        static void AssertSingleTrainingCard(Canvas canvas)
        {
            int cards = 0;
            foreach (var rt in canvas.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "Training instruction") cards++;
            Assert.AreEqual(1, cards,
                "Exactly one training card must exist; a stacked second card hides the first one's caption.");
        }

        /// <summary>
        /// A marker caption must never land on a HUD panel the player is reading.
        ///
        /// <para>Distinct from both checks above. The reserved bands are two fixed fractions of the
        /// canvas height measured from the gameplay HUD, so they cover the score bar and the touch
        /// controls but nothing that only appears in other phases. The screen 07 capture caught the
        /// consequence: during preparation the weapon-select panel sits in the lower middle, inside
        /// the "readable" band, and the objective caption printed across its weapon names.
        /// <see cref="Hotdrop.UI.HudMarkerLayer.AvoidPanels"/> is the rule; this is the outcome.</para>
        /// </summary>
        static void AssertNoMarkerOverlapsPanel(Canvas canvas, int width, int height)
        {
            var hud = canvas.GetComponent<Hotdrop.UI.MatchHUD>();
            if (hud == null) return;
            var panels = new List<Rect>();
            hud.ActivePanelRects(panels);
            Assert.Greater(panels.Count, 0,
                $"no HUD panels were published at {width}x{height}, so this check had nothing to " +
                "measure and would have reported the frame as clean.");
            var root = canvas.GetComponent<RectTransform>();
            var footprints = new List<Rect>();
            var reserved = new List<Rect>();
            var labels = new List<string>();
            foreach (var marker in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (marker.name != "ObjectiveMarker" && marker.name != "Ping") continue;
                if (!marker.gameObject.activeInHierarchy) continue;
                var caption = marker.Find("Label") as RectTransform;
                if (caption == null || !caption.gameObject.activeInHierarchy) continue;
                var text = caption.GetComponent<TMPro.TextMeshProUGUI>();
                if (text == null || string.IsNullOrEmpty(text.text)) continue;
                // The WHOLE marker, icon and caption both, measured from the parts that actually
                // draw. Checking only the caption box let a frame pass its assertion while the
                // diamond still printed across the panel text - the half a player reads was never
                // tested. Union of the visible children's real rects, so the declared
                // MarkerFootprintSize is checked against reality rather than trusted.
                var parts = new List<RectTransform>();
                foreach (var part in marker.GetComponentsInChildren<RectTransform>(true))
                {
                    if (part == marker) continue;
                    if (!part.gameObject.activeInHierarchy) continue;
                    if (part.GetComponent<UnityEngine.UI.Graphic>() == null) continue;
                    parts.Add(part);
                }
                if (parts.Count == 0) continue;
                // Canvas-local, not world: ActivePanelRects reports panels in canvas-local units,
                // and under ScreenSpaceCamera the two spaces do not coincide. Comparing world
                // rects against local rects would never intersect, making this assertion pass
                // without proving anything.
                var box = UnionCanvasLocal(root, parts);
                footprints.Add(box);
                // The position itself, not just its relationships. A marker handed a screen-space
                // position lands tens of thousands of units off a canvas-local slot, and every
                // relationship below would then be measured between things that are not on screen.
                Assert.IsTrue(root.rect.Contains(CanvasLocalRect(root, marker).center),
                    $"'{marker.name}:{text.text}' is placed at {CanvasLocalRect(root, marker).center} " +
                    $"at {width}x{height}, which is not inside the canvas {root.rect}. A marker must " +
                    "be positioned in canvas-local units; a converted screen position is off-canvas " +
                    "entirely, and no panel or band check below can see that.");
                // The box the placement rule was asked to protect: the marker's own root rect is
                // built at MarkerFootprintSize and centred on the anchor, so its canvas-local
                // bounds are exactly HudMarkerLayer.FootprintBox(anchor). Both marker kinds build
                // it that way, and it is a superset of the ink above - the diamond's 45-degree
                // box fits inside it - so a margin measured here is a margin the drawn glyphs
                // also have. Measuring the footprint rather than the ink is what lets this assert
                // the clearance the rule promises instead of a weaker ink-only number.
                reserved.Add(CanvasLocalRect(root, marker));
                labels.Add($"{marker.name}:{text.text}");
            }
            for (int i = 0; i < footprints.Count; i++)
                for (int p = 0; p < panels.Count; p++)
                {
                    if (!footprints[i].Overlaps(panels[p])) continue;
                    Assert.Fail(
                        $"Marker overlaps a HUD panel at {width}x{height}. " +
                        $"'{labels[i]}' footprint {footprints[i]} intersects panel {p} {panels[p]}. " +"A world marker - icon and caption both - must never be drawn over a panel "
                        + "the player is reading.");
                }

            // Non-overlap is not the contract, and this frame is the reason. The objective marker
            // on this screen cleared the weapon panel by 1.03 canvas units - 1.5 px at 1080p -
            // which passes every "do they touch" check while leaving nothing for the next small
            // change to the marker, the card, the band or the panel. Placement promises
            // MarkerClearance of deliberate slack, so the frame has to carry it.
            //
            // The tolerance is float rounding only, about a hundredth of a pixel, and is well
            // below the 1.03 units that started this. Halving MarkerClearance would still fail
            // here by roughly a unit; nothing this assertion permits is the knife-edge it replaced.
            const float rounding = 0.01f;
            for (int i = 0; i < reserved.Count; i++)
                for (int p = 0; p < panels.Count; p++)
                {
                    float gap = Hotdrop.UI.HudMarkerLayer.RectGap(reserved[i], panels[p]);
                    if (gap >= Hotdrop.UI.HudMarkerLayer.MarkerClearance - rounding) continue;
                    Assert.Fail(
                        $"Marker clearance is knife-edge at {width}x{height}. '{labels[i]}' reserved "
                        + $"footprint {reserved[i]} keeps only {gap:0.00} units from panel {p} "
                        + $"{panels[p]}, and the placement rule requires "
                        + $"{Hotdrop.UI.HudMarkerLayer.MarkerClearance}. Drawn ink {footprints[i]} is "
                        + "clear, which is exactly why the margin needs its own check: a non-overlap "
                        + "assertion alone reports this frame as good. Move the marker to the "
                        + "roomiest slot rather than the first one that fits.");
                }

            // Readable text that is not a panel is the case a panel-only check cannot see. On screen
            // 09 the transient feedback line sat inside the gap markers occupy, and the frame was
            // "clear" only by a few pixels of luck. Both halves are asserted: the element must be in
            // the measured exclusion set, and no marker may touch its ink.
            foreach (var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (!text.gameObject.activeInHierarchy || string.IsNullOrEmpty(text.text)) continue;
                if (!IsTransientFeedback(text)) continue;
                var ink = CanvasLocalRect(root, text.rectTransform, text);
                bool measured = false;
                for (int p = 0; p < panels.Count; p++)
                    if (panels[p].Overlaps(ink) || Mathf.Abs(panels[p].yMin - ink.yMin) < 0.5f) measured = true;
                Assert.IsTrue(measured,
                    $"transient feedback '{text.text}' was visible at {width}x{height} but its ink " +
                    $"{ink} is not in the measured exclusion set [{string.Join(", ", panels)}]. " +
                    "An element the placement rule cannot see is the defect this check exists to catch.");
                for (int i = 0; i < footprints.Count; i++)
                {
                    if (!footprints[i].Overlaps(ink)) continue;
                    Assert.Fail(
                        $"Marker overlaps transient HUD feedback at {width}x{height}. " +
                        $"'{labels[i]}' footprint {footprints[i]} intersects '{text.text}' ink {ink}. " +
                        "Feedback the player is reading must stay readable.");
                }
            }
        }

        /// <summary>
        /// Holds a marker frame to the residual it claims to be, for the behind-camera arc where
        /// the pinned residual lives.
        ///
        /// <para>The edge fixes the marker's X and <see cref="Hotdrop.UI.HudMarkerLayer.AvoidPanels"/>
        /// can only search in Y, so on that arc there is no legal position at all and the marker is
        /// measured touching a panel. That is a recorded finding rather than a clean frame, so it
        /// gets its own assertion instead of being skipped, and it has to earn the label twice
        /// over: the marker must have no clear air at all - a knife-edge between 0 and
        /// MarkerClearance would be a rule that found a slot and did not take it, which is a real
        /// failure - and its footprint must span the teammate list, the documented cause. If the
        /// residual ever shrinks, this fails and says so, rather than quietly going green.</para>
        /// </summary>
        static void AssertResidualFrame(Canvas canvas, int width, int height)
        {
            var hud = canvas.GetComponent<Hotdrop.UI.MatchHUD>();
            if (hud == null) return;
            var panels = new List<Rect>();
            hud.ActivePanelRects(panels);
            Assert.Greater(panels.Count, 0,
                $"no panels were published at {width}x{height}, so there is nothing to classify and " +
                "this frame would have read as clean without ever being measured.");
            var root = canvas.GetComponent<RectTransform>();
            int measured = 0;
            foreach (var marker in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (marker.name != "ObjectiveMarker" && marker.name != "Ping") continue;
                if (!marker.gameObject.activeInHierarchy) continue;
                var caption = marker.Find("Label") as RectTransform;
                var text = caption != null ? caption.GetComponent<TMPro.TextMeshProUGUI>() : null;
                if (text == null || string.IsNullOrEmpty(text.text)) continue;
                measured++;

                var footprint = CanvasLocalRect(root, marker);
                Assert.IsTrue(root.rect.Contains(footprint.center),
                    $"'{marker.name}:{text.text}' is placed at {footprint.center} at {width}x{height}, " +
                    $"which is not inside the canvas {root.rect}. A marker must be positioned in " +
                    "canvas-local units; a converted screen position is off-canvas entirely.");
                float gap = float.MaxValue;
                Rect nearest = default;
                for (int p = 0; p < panels.Count; p++)
                {
                    float measuredGap = Hotdrop.UI.HudMarkerLayer.RectGap(footprint, panels[p]);
                    if (measuredGap >= gap) continue;
                    gap = measuredGap;
                    nearest = panels[p];
                }

                Assert.AreEqual(0f, gap,
                    $"'{marker.name}:{text.text}' footprint {footprint} keeps {gap:0.00} units from " +
                    $"panel {nearest} at {width}x{height}. This frame is the behind-camera residual, " +
                    "where the rule has no slot at all, so the marker should be touching a panel. " +
                    "Clear air here means the residual has shrunk and the panel gate - not this - " +
                    "should be judging the frame again.");
                int teammates = panels.FindIndex(p => p.xMax < 0f && p.width < 400f && p.height < 400f);
                Assert.GreaterOrEqual(teammates, 0,
                    $"the residual's cause is a panel in the left third of the screen and the frame " +
                    $"published none of those ({string.Join(", ", panels)}); re-measure before trusting it.");
                Assert.IsTrue(footprint.xMax > panels[teammates].xMin &&
                              footprint.xMin < panels[teammates].xMax,
                    $"'{marker.name}:{text.text}' footprint x {footprint.xMin:0.00}.." +
                    $"{footprint.xMax:0.00} does not span the teammate list x " +
                    $"{panels[teammates].xMin:0.00}..{panels[teammates].xMax:0.00}, so this is not the " +
                    "documented no-slot residual and the marker simply landed on a panel it could " +
                    "have avoided.");
            }
            Assert.Greater(measured, 0,
                $"no captioned marker was measured at {width}x{height}; the frame proves nothing.");
        }

        /// <summary>
        /// Every weapon card's stat line must sit inside the card it belongs to.
        ///
        /// <para>The stat block is the numbers a player picks a weapon by, so a line that renders
        /// below its card's border is not a cosmetic nit - it reads as belonging to the gap between
        /// the cards. Measured on the 1920x1080 capture: the stat text box spans y -115.5..-60.5 in a
        /// card that ends at y -100, and its ink ran further still.</para>
        ///
        /// <para>Asserted against the card's real rect and the text's real ink rather than against
        /// the declared box, so the check cannot pass while the rendered text spills.</para>
        /// </summary>
        static void AssertWeaponStatsStayInsideTheirCard(Canvas canvas, int width, int height)
        {
            var hud = canvas.GetComponent<Hotdrop.UI.MatchHUD>();
            if (hud?.weaponSelectPanel == null || !hud.weaponSelectPanel.activeInHierarchy) return;
            var root = canvas.GetComponent<RectTransform>();

            int checkedCards = 0;
            foreach (var card in hud.weaponSelectPanel.GetComponentsInChildren<RectTransform>(true))
            {
                var stats = card.Find("Stats") as RectTransform;
                if (stats == null || !stats.gameObject.activeInHierarchy) continue;
                var text = stats.GetComponent<TMPro.TMP_Text>();
                if (text == null || string.IsNullOrEmpty(text.text)) continue;
                var name = card.Find("Name");
                var nameText = name?.GetComponent<TMPro.TMP_Text>();
                string who = nameText != null && !string.IsNullOrEmpty(nameText.text) ? nameText.text : card.name;

                var cardRect = CanvasLocalRect(root, card);
                var ink = CanvasLocalRect(root, stats, text);
                checkedCards++;
                if (ink.yMin >= cardRect.yMin && ink.yMax <= cardRect.yMax &&
                    ink.xMin >= cardRect.xMin && ink.xMax <= cardRect.xMax) continue;

                Assert.Fail(
                    $"Weapon card stat text escapes its card at {width}x{height}. '{who}' stats " +
                    $"'{text.text}' ink {ink} is not inside card {cardRect}; it overflows by " +
                    $"{Mathf.Max(cardRect.yMin - ink.yMin, ink.yMax - cardRect.yMax):0.##} units. " +
                    "A player must be able to see which card a number belongs to.");
            }
            if (checkedCards == 0)
                Assert.Fail("No weapon card carried a stat line, so this check proved nothing. " +
                    "The card layout may have changed and the assertion must follow it.");
        }

        /// <summary>
        /// True for the HUD's transient messaging elements - readable, on screen, and not a panel.
        /// Named by role rather than by screen: one channel carries pad, shot, elimination and
        /// countdown messaging in every phase, so nothing here knows which screen is being captured.
        /// </summary>
        static bool IsTransientFeedback(TMPro.TMP_Text text)
        {
            var name = text.rectTransform.name;
            return name == "CombatFeedback" || name == "ZoneCaption" || name == "NextZone";
        }

        /// <summary>
        /// Canvas-local bounds of several rects as one box. Each rect's four world corners are
        /// converted into <paramref name="root"/>'s local space first, so a rotated icon such as the
        /// objective diamond is measured as it is drawn rather than as an unrotated authoring rect.
        /// </summary>
        static Rect UnionCanvasLocal(RectTransform root, List<RectTransform> rects)
        {
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            var corners = new Vector3[4];
            foreach (var rect in rects)
            {
                rect.GetWorldCorners(corners);
                for (int i = 0; i < 4; i++)
                {
                    var local = root.InverseTransformPoint(corners[i]);
                    minX = Mathf.Min(minX, local.x); maxX = Mathf.Max(maxX, local.x);
                    minY = Mathf.Min(minY, local.y); maxY = Mathf.Max(maxY, local.y);
                }
            }
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// The objective marker must never be drawn over the score bar or the zone caption.
        ///
        /// A gameplay capture showed it there, rendering as
        /// "◆▼ ALLY 0  2:58 ◆ 0 ENEMY ◆" — unreadable. <see cref="Hotdrop.UI.HudMarkerLayer"/>
        /// reserves bands for exactly this, so this asserts the outcome on the real frame.
        /// A marker that is inactive, or a HUD without those panels, is not a collision.
        /// </summary>
        static void AssertNoMarkerOverlapsHud(Canvas canvas, int width, int height)
        {
            var root = canvas.GetComponent<RectTransform>();
            var marker = FindNamed(root, "ObjectiveMarker");
            if (marker == null || !marker.gameObject.activeInHierarchy) return;

            var reserved = new List<Rect>();
            foreach (string name in new[] { "TopCenter", "ZoneInfo" })
            {
                var panel = FindNamed(root, name);
                if (panel != null && panel.gameObject.activeInHierarchy) reserved.Add(WorldRect(panel));
            }
            if (reserved.Count == 0) return;

            var markerRect = WorldRect(marker);
            foreach (var chrome in reserved)
            {
                if (!markerRect.Overlaps(chrome)) continue;
                Assert.Fail(
                    $"ObjectiveMarker overlaps HUD chrome at {width}x{height}. " +
                    $"marker={markerRect} chrome={chrome} markerLocal={marker.localPosition} " +
                    $"canvasRect={root.rect} canvasScale={root.lossyScale}. " +
                    "A world marker must never be drawn over the score bar or zone caption.");
            }
        }

        /// <summary>
        /// Two world-anchored captions must not be printed through one another.
        ///
        /// <para>Distinct from the band check above: the bands keep a marker off the score bar but
        /// say nothing about two markers landing on each other. The capture showed exactly that -
        /// an objective 34 m out and an enemy ping 52 m out projected close enough that
        /// <c>ENEMY HERE 52m</c> and <c>CENTER 34m</c> overlapped and neither was readable. The
        /// caption box is measured here rather than the whole marker, because the smear is the
        /// text; the icons may legitimately sit close.</para>
        /// </summary>
        static void AssertNoMarkerOverlapsMarker(Canvas canvas, int width, int height)
        {
            var root = canvas.GetComponent<RectTransform>();
            var captions = new List<Rect>();
            var labels = new List<string>();
            foreach (var marker in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (marker.name != "ObjectiveMarker" && marker.name != "Ping") continue;
                if (!marker.gameObject.activeInHierarchy) continue;
                var caption = marker.Find("Label") as RectTransform;
                if (caption == null || !caption.gameObject.activeInHierarchy) continue;
                var text = caption.GetComponent<TMPro.TextMeshProUGUI>();
                if (text == null || string.IsNullOrEmpty(text.text)) continue;
                captions.Add(WorldRect(caption));
                labels.Add($"{marker.name}:{text.text}");
            }
            for (int i = 0; i < captions.Count; i++)
                for (int j = i + 1; j < captions.Count; j++)
                {
                    if (!captions[i].Overlaps(captions[j])) continue;
                    Assert.Fail(
                        $"Marker captions overlap at {width}x{height}. " +
                        $"'{labels[i]}' {captions[i]} intersects '{labels[j]}' {captions[j]}. " +
                        "World positions a few pixels apart must not print their captions through each other.");
                }
        }

        internal static void Capture(Canvas canvas, string label, int width, string directoryName = "journey_2026-10-02", bool includeGameplay = false, bool expectResidualFrame = false, bool assertHud = true)
        {
            const int height = 1080;
            const float dpi = 400;
            var mode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            float oldDistance = canvas.planeDistance;
            var scaler = canvas.GetComponent<CanvasScaler>();
            var density = canvas.GetComponent<DensityCanvasScaler>();
            bool densityEnabled = density != null && density.enabled;
            var scaleMode = scaler.uiScaleMode;
            float oldScale = scaler.scaleFactor;
            var oldScreen = SafeAreaRoot.SimulatedScreen;
            var oldInsets = SafeAreaRoot.SimulatedInsets;
            var cameraObject = new GameObject("JourneyEvidenceCamera", typeof(UnityEngine.Camera));
            var captureCamera = cameraObject.GetComponent<UnityEngine.Camera>();
            captureCamera.transform.position = new Vector3(0, -2000, 0);
            captureCamera.orthographic = true;
            captureCamera.orthographicSize = height * .5f;
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = UIDesignTokens.Background;
            captureCamera.nearClipPlane = .1f;
            captureCamera.farClipPlane = 100;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var renderCamera = includeGameplay && UnityEngine.Camera.main != null ? UnityEngine.Camera.main : captureCamera;
            var originalTarget = renderCamera.targetTexture;
            Texture2D texture = null;
            var previous = RenderTexture.active;
            try
            {
                renderCamera.targetTexture = target;
                if (density != null) density.enabled = false;
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = UiMetrics.ScaleFor(width, height, dpi);
                SafeAreaRoot.SimulatedScreen = new Vector4(width, height, dpi, 0);
                SafeAreaRoot.SimulatedInsets = ScreenInsets.None;
                // The startup annotation prints the composed frame, and this synchronous exporter
                // renders in the same frame it pins that frame - no Update runs in between, so the
                // label would otherwise print the editor window's size. Ask for the refresh the
                // screen would do on its own next frame.
                // Searched from the scene, not from the canvas: the startup screen is a component on
                // the bootstrap object, which owns the canvas rather than sitting inside it.
                var startupScreen = Object.FindFirstObjectByType<StartupConnectionScreen>();
                if (startupScreen != null) startupScreen.RefreshFrameLabel();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = renderCamera;
                canvas.planeDistance = includeGameplay ? renderCamera.nearClipPlane + .05f : 10;
                foreach (var safe in canvas.GetComponentsInChildren<SafeAreaRoot>()) safe.Apply();
                Canvas.ForceUpdateCanvases();
                GoalsPagesView.RefreshVisible(canvas);
                SupportHubView.RefreshVisible(canvas);
                var pauseConsole = Object.FindFirstObjectByType<OfflinePauseMenu>();
                if (pauseConsole != null && pauseConsole.IsOpen) pauseConsole.RefreshForComposedFrame();
                Canvas.ForceUpdateCanvases();
                // ContentWidthFitter normally runs on LateUpdate. This synchronous exporter
                // must settle that same width before asking the real layout groups to rebuild.
                foreach (var scroll in canvas.GetComponentsInChildren<ScrollRect>())
                {
                    if (scroll.GetComponent<ContentWidthFitter>() == null) continue;
                    scroll.content.sizeDelta = new Vector2(Mathf.Min(MenuLayout.ContentMaxWidth,
                        ((RectTransform)scroll.transform).rect.width - 48), scroll.content.sizeDelta.y);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
                }
                Canvas.ForceUpdateCanvases();
                foreach (var text in canvas.GetComponentsInChildren<TMPro.TMP_Text>()) text.ForceMeshUpdate();
                // The canvas geometry just changed, so world-anchored markers clamped against the
                // old rect are stale. A device re-places them on its next LateUpdate; a capture
                // renders immediately, so ask for that placement explicitly. Without this the
                // screenshot shows a state no device ever produces.
                if (includeGameplay)
                {
                    foreach (var hud in canvas.GetComponentsInChildren<Hotdrop.UI.MatchHUD>(true))
                        hud.RefreshHudMarkers();
                    Canvas.ForceUpdateCanvases();
                }
                // Same reason, and the same place, as the marker refresh above: the touch controls
                // are placed from the canvas rect and from the composed frame, so they can only be
                // re-placed once the canvas has been switched to camera mode and laid out. Doing it
                // earlier - while the canvas was still in its previous mode and had no rect - put
                // the controls at zero size, and a synchronous exporter runs no LateUpdate to
                // repair it. This must be the last thing before the frame is drawn.
                foreach (var view in canvas.GetComponentsInChildren<Hotdrop.UI.TouchControlsView>(true))
                    view.RefreshForComposedFrame();
                Canvas.ForceUpdateCanvases();
                if (label == "17_support")
                {
                    // The three columns must be laid out for this frame: inside the canvas width,
                    // side by side without overlap, and the page as wide as the canvas.
                    RectTransform support = null;
                    foreach (var t in canvas.transform.GetComponentsInChildren<RectTransform>(true))
                        if (t.name == "SupportHubVisual") { support = t; break; }
                    Assert.IsNotNull(support, "The Support page must be on screen.");
                    var page = (RectTransform)support.Find("Viewport/Page");
                    float canvasWidth = ((RectTransform)canvas.transform).rect.width;
                    Assert.AreEqual(canvasWidth, page.sizeDelta.x, 1f, $"Support page must match the {width}x{height} canvas width.");
                    float previousRight = float.NegativeInfinity;
                    foreach (string column in new[] { "Account", "Report", "Assistance" })
                    {
                        var card = (RectTransform)page.Find(column);
                        float left = card.anchoredPosition.x, right = left + card.sizeDelta.x;
                        Assert.Greater(left, previousRight, $"{column} must not overlap the column before it at {width}x{height}.");
                        Assert.LessOrEqual(right, canvasWidth, $"{column} must stay inside the canvas at {width}x{height}.");
                        previousRight = right;
                    }
                }
                if (label.StartsWith("10_settings"))
                {
                    foreach (var tab in canvas.GetComponentsInChildren<Button>())
                    {
                        if (!tab.name.StartsWith("SettingsTab_")) continue;
                        var text = tab.GetComponentInChildren<TMPro.TMP_Text>();
                        var rect = tab.GetComponent<RectTransform>();
                        foreach (Vector3 corner in new[] { text.textBounds.min, text.textBounds.max })
                        {
                            Vector3 point = rect.InverseTransformPoint(text.transform.TransformPoint(corner));
                            Assert.IsTrue(point.x >= rect.rect.xMin - 1 && point.x <= rect.rect.xMax + 1 &&
                                point.y >= rect.rect.yMin - 1 && point.y <= rect.rect.yMax + 1,
                                $"{tab.name} caption must remain inside its button at {width}x{height}.");
                        }
                    }
                }
                if (label == "02_runner_choice")
                {
                    var left = GameObject.Find("RigCard_rig_female").GetComponent<RectTransform>();
                    var right = GameObject.Find("RigCard_rig_male").GetComponent<RectTransform>();
                    var leftCorners = new Vector3[4];
                    var rightCorners = new Vector3[4];
                    left.GetWorldCorners(leftCorners);
                    right.GetWorldCorners(rightCorners);
                    var first = leftCorners[0].x <= rightCorners[0].x ? leftCorners : rightCorners;
                    var second = leftCorners[0].x <= rightCorners[0].x ? rightCorners : leftCorners;
                    Assert.Less(first[2].x + 8f, second[0].x,
                        $"Runner-choice cards must stay separate at {width}x{height}.");
                }
                if (label == "03_home")
                {
                    var home = GameObject.Find("Home");
                    var stage = (RectTransform)home.transform;
                    foreach (string action in new[] { "PLAY 3-MINUTE RELAY", "PLAY OFFLINE",
                        "HomeGoalAction", "PARTY", "APPEARANCE", "MORE" })
                    {
                        var corners = new Vector3[4];
                        GameObject.Find(action).GetComponent<RectTransform>().GetWorldCorners(corners);
                        foreach (var corner in corners)
                        {
                            Vector2 point = stage.InverseTransformPoint(corner);
                            Assert.IsTrue(stage.rect.Contains(point), $"{action} must be fully visible on Home at {width}x{height}.");
                        }
                    }
                }
                // Measured here, not before: the canvas has just been re-scaled and switched to
                // ScreenSpaceCamera, so this observes exactly the frame that is about to be
                // written to disk. A collision found only after this mutation would be a harness
                // artefact; a collision present in the unmutated frame would be a real clamp bug.
                if (includeGameplay && assertHud)
                {
                    AssertNoMarkerOverlapsHud(canvas, width, height);
                    AssertNoMarkerOverlapsMarker(canvas, width, height);
                    // A frame that photographs the pinned residual cannot pass the panel gate: on
                    // that arc there is no legal slot, so the marker is measured on a panel and the
                    // gate fails by construction. Those frames are held to the residual they claim
                    // to be instead - the marker must have no clear air at all, and its footprint
                    // must span the teammate list, the documented cause. Skipping the check would
                    // be the worse lie: the frame would read as clean.
                    if (expectResidualFrame) AssertResidualFrame(canvas, width, height);
                    else AssertNoMarkerOverlapsPanel(canvas, width, height);
                    AssertWeaponStatsStayInsideTheirCard(canvas, width, height);
                }
                foreach (var preview in Object.FindObjectsByType<UnityEngine.Camera>(FindObjectsSortMode.None))
                    if (preview != captureCamera && preview.targetTexture != null && preview.enabled) preview.Render();
                renderCamera.Render();
                RenderTexture.active = target;
                texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                // Measured on the frame that was just written, not on component properties: the
                // caption's colour was always white, so only the rendered pixels reveal the fault.
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "UIEvidence", directoryName));
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, $"{label}_{width}x{height}.png"), texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                canvas.renderMode = mode;
                canvas.worldCamera = oldCamera;
                canvas.planeDistance = oldDistance;
                scaler.uiScaleMode = scaleMode;
                scaler.scaleFactor = oldScale;
                if (density != null) density.enabled = densityEnabled;
                SafeAreaRoot.SimulatedScreen = oldScreen;
                SafeAreaRoot.SimulatedInsets = oldInsets;
                foreach (var safe in canvas.GetComponentsInChildren<SafeAreaRoot>()) safe.Apply();
                foreach (var view in canvas.GetComponentsInChildren<TouchControlsView>(true))
                    view.RefreshForComposedFrame();
                renderCamera.targetTexture = originalTarget;
                target.Release();
                if (texture != null) Object.Destroy(texture);
                Object.Destroy(target);
                Object.Destroy(cameraObject);
            }
        }
    }
}
