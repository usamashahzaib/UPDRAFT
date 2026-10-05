using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Hotdrop.Core;
using Hotdrop.Player;

namespace Hotdrop.Tests.PlayMode
{
    /// <summary>
    /// Renders real screens at 1920x1080 and 2400x1080 into UIEvidence/match so each one can be
    /// laid over the UIUX reference rendered at the same resolution. Layout evidence only.
    /// </summary>
    public class UiMatchCaptureTests
    {
        const string Folder = "match";
        int tutorialDone, rigChosen;
        string rig;
        bool hadTutorial, hadRig, hadChosen;

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
            var startup = GameBootstrap.Instance != null ? GameBootstrap.Instance.GetComponent<StartupConnectionScreen>() : null;
            if (startup != null) startup.HoldForCapture(true);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            var startup = GameBootstrap.Instance != null ? GameBootstrap.Instance.GetComponent<StartupConnectionScreen>() : null;
            if (startup != null) startup.HoldForCapture(false);
            if (hadTutorial) PlayerPrefs.SetInt(GameBootstrap.TutorialDoneKey, tutorialDone); else PlayerPrefs.DeleteKey(GameBootstrap.TutorialDoneKey);
            if (hadChosen) PlayerPrefs.SetInt(BodyRigStore.ChosenKey, rigChosen); else PlayerPrefs.DeleteKey(BodyRigStore.ChosenKey);
            if (hadRig) PlayerPrefs.SetString(BodyRigStore.PrefKey, rig); else PlayerPrefs.DeleteKey(BodyRigStore.PrefKey);
            Time.timeScale = 1;
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            var main = SceneManager.GetSceneByName("Main");
            SceneManager.SetActiveScene(SceneManager.CreateScene("MatchCaptureCleanup"));
            if (main.IsValid() && main.isLoaded) yield return SceneManager.UnloadSceneAsync(main);
            yield return null;
        }

        static Canvas CanvasOf(GameBootstrap game) => game.matchHUD.GetComponent<Canvas>();

        static IEnumerator Both(Canvas canvas, string label)
        {
            for (int i = 0; i < 3; i++) yield return null;
            PlayerJourneyVisualEvidenceTests.Capture(canvas, label, 1920, Folder);
            PlayerJourneyVisualEvidenceTests.Capture(canvas, label, 2400, Folder);
        }

        [UnityTest]
        public IEnumerator S01_S02_S03()
        {
            var game = GameBootstrap.Instance;
            Assert.IsNotNull(game);
            var canvas = CanvasOf(game);
            yield return Both(canvas, "s01");
            var startup = game.GetComponent<StartupConnectionScreen>();
            startup.HoldForCapture(false);
            startup.Continue();
            yield return null;
            yield return Both(canvas, "s02");
            var female = GameObject.Find("Rig_rig_female");
            if (female != null) female.GetComponent<Button>().onClick.Invoke();
            var go = GameObject.Find("RigContinue");
            if (go != null) go.GetComponent<Button>().onClick.Invoke();
            yield return null;
            game.ReturnToHome();
            yield return Both(canvas, "s03");
        }

        static IEnumerator ToHome(GameBootstrap game)
        {
            var startup = game.GetComponent<StartupConnectionScreen>();
            startup.HoldForCapture(false);
            startup.Continue();
            yield return null;
            var go = GameObject.Find("RigContinue");
            if (go != null) go.GetComponent<Button>().onClick.Invoke();
            yield return null;
            game.ReturnToHome();
            yield return null;
        }

        static IEnumerator Gameplay(Canvas canvas, string label)
        {
            for (int i = 0; i < 3; i++) yield return null;
            PlayerJourneyVisualEvidenceTests.Capture(canvas, label, 1920, Folder, true, assertHud: false);
            PlayerJourneyVisualEvidenceTests.Capture(canvas, label, 2400, Folder, true, assertHud: false);
        }

        [UnityTest]
        public IEnumerator S04_Training()
        {
            var game = GameBootstrap.Instance;
            yield return ToHome(game);
            game.StartTutorial();
            for (int i = 0; i < 10; i++) yield return null;
            yield return Gameplay(CanvasOf(game), "s04");
        }

        [UnityTest]
        public IEnumerator S07_S08_S09_S11_Match()
        {
            var game = GameBootstrap.Instance;
            PlayerPrefs.SetInt(GameBootstrap.TutorialDoneKey, 1);
            yield return ToHome(game);
            var canvas = CanvasOf(game);
            game.StartOfflinePractice();
            yield return new WaitForSeconds(2f);
            yield return Gameplay(canvas, "s07");
            float deadline = Time.realtimeSinceStartup + 20f;
            while (game.matchController.CurrentPhase != Hotdrop.Match.MatchPhase.InProgress && Time.realtimeSinceStartup < deadline) yield return null;
            yield return Gameplay(canvas, "s08");
            var local = game.matchController.LocalPlayer;
            if (local != null) { if (local.TryPlacePad() == null) local.TryPlacePad(); }
            yield return Gameplay(canvas, "s09");
            Time.timeScale = 20f;
            deadline = Time.realtimeSinceStartup + 90f;
            while (!game.matchHUD.resultsPanel.activeSelf && Time.realtimeSinceStartup < deadline) yield return null;
            Time.timeScale = 1f;
            yield return null;
            yield return Gameplay(canvas, "s11");
        }

        [UnityTest]
        public IEnumerator S10_S12_S13_S15_S16_S17_Menus()
        {
            var game = GameBootstrap.Instance;
            yield return ToHome(game);
            var canvas = CanvasOf(game);
            game.GetComponent<OfflinePauseMenu>().OpenSettings();
            yield return Both(canvas, "s10");
            game.GetComponent<OfflinePauseMenu>().Close();
            yield return null;
            foreach (var (page, label) in new[] { ("Contracts", "s12"), ("Mastery", "s13"), ("Appearance", "s15"), ("Profile", "s16"), ("Support", "s17"), ("Party", "s05") })
            {
                PlayerProductScreens.Open(game, GameObject.Find("Home"), page);
                for (int i = 0; i < 15; i++) yield return null;
                yield return Both(canvas, label);
                var screen = GameObject.Find("PlayerProductScreen");
                var back = screen != null ? screen.transform.Find("Nav_Bar/Nav_Back") : null;
                if (back != null) back.GetComponent<Button>().onClick.Invoke();
                else if (screen != null) Object.Destroy(screen);
                yield return null;
                game.ReturnToHome();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator S14_Challenges()
        {
            var game = GameBootstrap.Instance;
            PlayerPrefs.SetInt(GameBootstrap.TutorialDoneKey, 1);
            yield return ToHome(game);
            game.OpenOfflineLibrary();
            for (int i = 0; i < 10; i++) yield return null;
            yield return Both(CanvasOf(game), "s14");
        }

        [UnityTest]
        public IEnumerator S06_Search()
        {
            var game = GameBootstrap.Instance;
            yield return ToHome(game);
            var canvas = CanvasOf(game);
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
            Hotdrop.UI.MatchSearchView.Show(host, 48, "US East", party, "self", true, () => { }, () => { });
            yield return Both(canvas, "s06");
            Object.Destroy(host);
        }
    }
}
