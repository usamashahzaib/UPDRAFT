using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Hotdrop.Player;
using Hotdrop.UI;

namespace Hotdrop.Core
{
    /// <summary>
    /// Amendment 1 home screen: one primary Play action, one always-working offline option,
    /// the real in-game character, a single honest goal card and persistent navigation.
    /// Account, support and deletion stay reachable behind More; study tooling is unchanged.
    /// </summary>
    public class OfflineHomeScreen : MonoBehaviour
    {
        GameObject panel;
        GameBootstrap game;
        TextMeshProUGUI characterLabel, goalText, onlineStatus;
        RawImage previewImage;
        HomeCharacterPreview preview;
        GameObject morePanel;
        GameObject rigChoicePanel;

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            DisposePreview();
        }

        void DisposePreview()
        {
            if (preview != null) { preview.Dispose(); preview = null; }
        }

        public void Show(GameBootstrap game)
        {
            this.game = game;
            if (panel != null) { panel.SetActive(true); RefreshPreview(); Refresh(); return; }
            // QA-019/020: density-aware menu layout — every control stays above the 48 dp floor.
            var screen = Hotdrop.UI.MenuLayout.Create(game.matchHUD.transform, "Home", "HOTDROP", null);
            panel = screen.Root;
            screen.Title.fontSizeMax = 52; screen.Title.fontSize = 52;
            // Facilitator-only study controls: tap the title five times.
            var titleTap = screen.Title.gameObject.AddComponent<Button>();
            screen.Title.raycastTarget = true;
            titleTap.transition = Selectable.Transition.None;
            titleTap.onClick.AddListener(() =>
            {
                if (Time.unscaledTime - lastTitleTap > 1.5f) titleTaps = 0;
                lastTitleTap = Time.unscaledTime;
                if (++titleTaps >= 5) { titleTaps = 0; ToggleStudyPanel(); }
            });

            Hotdrop.UI.MenuLayout.AddText(screen.Content, "Three players. One team. Three minutes.", 22, Hotdrop.UI.MatchHUD.SecondaryText, "Tagline");

            // Hero: the real model on the left, the two actions and the single goal on the right.
            var hero = Hotdrop.UI.MenuLayout.Row(screen.Content, "Home_Hero");
            hero.GetComponent<LayoutElement>().preferredHeight = 392f;
            var character = Column(hero, "Home_Character", 1.15f);
            previewImage = HomeCharacterPreview.BuildCard(character);
            characterLabel = Hotdrop.UI.MenuLayout.AddText(character, "", 22, Hotdrop.UI.MatchHUD.MainText, "RunnerLabel");
            Hotdrop.UI.MenuLayout.AddButton(character, "CHANGE RUNNER", () => ShowRigChoice(firstRun: false), "CHANGE RUNNER", 76);

            var actions = Column(hero, "Home_Actions", 1f);
            Hotdrop.UI.MenuLayout.AddButton(actions, "PLAY 3-MINUTE RELAY", PlayOnline, "PLAY 3-MINUTE RELAY", 124);
            Hotdrop.UI.MenuLayout.AddButton(actions, "PLAY OFFLINE", PlayOffline, "PLAY OFFLINE", 124);
            goalText = Hotdrop.UI.MenuLayout.AddText(actions, "", 20, Hotdrop.UI.MatchHUD.SecondaryText, "Goal");

            var nav = Hotdrop.UI.MenuLayout.Row(screen.Content, "Home_Nav");
            Hotdrop.UI.MenuLayout.AddButton(nav, "PARTY", () => PlayerProductScreens.Open(game, panel, "Party"), "PARTY");
            Hotdrop.UI.MenuLayout.AddButton(nav, "APPEARANCE", () => PlayerProductScreens.Open(game, panel, "Appearance"), "APPEARANCE");
            Hotdrop.UI.MenuLayout.AddButton(nav, "MORE", ToggleMore, "MORE");

            onlineStatus = screen.Status;
            RefreshPreview();
            Refresh();
        }

        void PlayOnline()
        {
            string reason = OnlineHooks.UnavailableReason?.Invoke() ?? (OnlineHooks.PlayOnline == null ? "Online play is not included in this build." : null);
            if (reason != null) { onlineStatus.text = reason; return; }
            CloseForMatch();
            OnlineHooks.PlayOnline(game);
        }

        void PlayOffline()
        {
            CloseForMatch();
            game.StartOfflinePractice();
        }

        void CloseForMatch()
        {
            if (panel != null) panel.SetActive(false);
            DisposePreview();
            game.touchInput.enabled = true;
        }

        void Refresh()
        {
            if (characterLabel != null)
                characterLabel.text = "RUNNER — " + BodyRigs.DisplayName(BodyRigStore.Current).ToUpperInvariant() +
                                      "\nAppearance only: same capsule, same hitboxes.";
            if (goalText != null) goalText.text = GoalLine();
            if (onlineStatus != null)
                onlineStatus.text = OnlineHooks.UnavailableReason?.Invoke() ?? "All weapons unlocked  •  Cosmetics never change power";
        }

        static string GoalLine()
        {
            bool tutorialDone = PlayerPrefs.GetInt(GameBootstrap.TutorialDoneKey, 0) != 0;
            return tutorialDone
                ? "ONE GOAL — training medal earned locally\nReplay Training any time from More."
                : "ONE GOAL — complete the guided lesson (~90 s)\nReward: Training medal (stays on this device)";
        }

        void RefreshPreview()
        {
            if (previewImage == null) return;
            DisposePreview();
            try
            {
                preview = new HomeCharacterPreview();
                previewImage.texture = preview.Texture;
            }
            catch (System.Exception exception)
            {
                preview = null;
                previewImage.texture = null;
                Debug.LogWarning("Home character preview unavailable: " + exception.Message);
            }
        }

        void ToggleMore()
        {
            if (morePanel != null)
            {
                morePanel.SetActive(!morePanel.activeSelf);
                if (morePanel.activeSelf) morePanel.transform.SetAsLastSibling();
                return;
            }
            var screen = Hotdrop.UI.MenuLayout.Create(panel.transform, "More", "MORE", () => morePanel.SetActive(false));
            morePanel = screen.Root;
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, "PROFILE", () => PlayerProductScreens.Open(game, panel, "Profile"), "PROFILE");
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, "TRAINING", () => { Hide(); game.StartTutorial(); }, "TRAINING");
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, "SETTINGS", () => game.GetComponent<OfflinePauseMenu>().OpenSettings(), "SETTINGS");
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, "ACCOUNT", () => PlayerProductScreens.Open(game, panel, "Account"), "ACCOUNT");
            Hotdrop.UI.MenuLayout.AddButton(screen.Content, "SUPPORT / REPORT", () => PlayerProductScreens.Open(game, panel, "Support"), "SUPPORT");
            screen.Status.text = "Account, support and deletion stay reachable here.";
        }

        /// <summary>
        /// Amendment 1: first launch offers a skippable runner choice with a ready-to-play default;
        /// the same chooser is reachable later from Home and Appearance.
        /// </summary>
        public void ShowRigChoice(bool firstRun)
        {
            if (rigChoicePanel != null) { rigChoicePanel.SetActive(true); rigChoicePanel.transform.SetAsLastSibling(); return; }
            // First run opens this before Home.Show has assigned the bootstrap reference.
            // Bind the already-initialized singleton rather than creating a second home flow.
            if (game == null) game = GameBootstrap.Instance;
            if (game == null || game.matchHUD == null)
            {
                Debug.LogError("Cannot open runner choice before GameBootstrap HUD initialization.");
                return;
            }
            var parent = panel != null ? panel.transform : game.matchHUD.transform;
            // There is no dead-end Back on first launch: backing out is the same safe,
            // cosmetic-only default as Decide Later. Later Appearance simply returns Home.
            UnityEngine.Events.UnityAction close = firstRun
                ? () => ChooseRig(BodyRigs.Default, true)
                : () => rigChoicePanel.SetActive(false);
            var screen = Hotdrop.UI.MenuLayout.Create(parent, "RigChoice",
                firstRun ? "CHOOSE YOUR RUNNER" : "CHANGE RUNNER", close);
            rigChoicePanel = screen.Root;
            Hotdrop.UI.MenuLayout.AddText(screen.Content,
                "Appearance only — both runners share one capsule, one hitbox set and one animation set.",
                20, Hotdrop.UI.MatchHUD.SecondaryText, "RigNote");
            var row = Hotdrop.UI.MenuLayout.Row(screen.Content, "RigOptions");
            AddRigOption(row, BodyRig.Female, firstRun);
            AddRigOption(row, BodyRig.Male, firstRun);
            if (firstRun)
                Hotdrop.UI.MenuLayout.AddButton(screen.Content, "DECIDE LATER", () => ChooseRig(BodyRigs.Default, firstRun), "DECIDE LATER");
            screen.Status.text = firstRun ? "You can change this any time in Appearance." : "Applies to your next spawn in offline play.";
        }

        void AddRigOption(Transform parent, BodyRig rig, bool firstRun)
        {
            string detail = rig == BodyRig.Female ? "NARROWER FRAME" : "BROADER FRAME";
            Hotdrop.UI.MenuLayout.AddButton(parent, BodyRigs.DisplayName(rig).ToUpperInvariant() + "\n" + detail,
                () => ChooseRig(rig, firstRun), "Rig_" + BodyRigs.Id(rig), 150);
        }

        void ChooseRig(BodyRig rig, bool firstRun)
        {
            BodyRigStore.Set(rig);
            if (rigChoicePanel != null) rigChoicePanel.SetActive(false);
            if (firstRun)
            {
                game.StartTutorial();
                return;
            }
            RefreshPreview();
            Refresh();
        }

        static RectTransform Column(Transform parent, string name, float flexibleWidth)
        {
            var column = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            var element = column.GetComponent<LayoutElement>(); element.preferredWidth = 0; element.flexibleWidth = flexibleWidth;
            column.transform.SetParent(parent, false);
            var layout = column.GetComponent<VerticalLayoutGroup>();
            layout.spacing = Hotdrop.UI.MenuLayout.Spacing;
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            return column.GetComponent<RectTransform>();
        }

        int titleTaps;
        float lastTitleTap;
        GameObject studyPanel;
        TextMeshProUGUI studyStatus;

        void ToggleStudyPanel()
        {
            if (studyPanel != null) { studyPanel.SetActive(!studyPanel.activeSelf); RefreshStudy(); return; }
            var root = panel;
            var screen = Hotdrop.UI.MenuLayout.Create(root.transform, "StudyPanel", "PAD STUDY — FACILITATOR", () => studyPanel.SetActive(false));
            studyPanel = screen.Root;
            var content = screen.Content;
            studyStatus = Hotdrop.UI.MenuLayout.AddText(content, "", 22, Hotdrop.UI.MatchHUD.MainText, "StudyStatus");
            var versions = Hotdrop.UI.MenuLayout.Row(content);
            Hotdrop.UI.MenuLayout.AddButton(versions, "VERSION: WITH PADS", () => { StudySettings.SetCohort(StudySettings.PadsOn); RefreshStudy(); });
            Hotdrop.UI.MenuLayout.AddButton(versions, "VERSION: WITHOUT PADS", () => { StudySettings.SetCohort(StudySettings.PadsOff); RefreshStudy(); });
            Hotdrop.UI.MenuLayout.AddButton(content, "NORMAL PLAY (NOT STUDY)", () => { StudySettings.SetCohort("default"); RefreshStudy(); });
            Hotdrop.UI.MenuLayout.AddButton(content, "TOGGLE INCENTIVIZED TESTER FLAG", () => { StudySettings.SetIncentivizedTester(!StudySettings.IncentivizedTester); RefreshStudy(); });
            Hotdrop.UI.MenuLayout.AddButton(content, "NEW PARTICIPANT (REPLAY TUTORIAL)", () =>
            {
                PlayerPrefs.DeleteKey(GameBootstrap.TutorialDoneKey);
                Telemetry.Track("study_participant_started");
                studyPanel.SetActive(false);
                root.SetActive(false);
                game.touchInput.enabled = true;
                game.StartTutorial();
            });
            Hotdrop.UI.MenuLayout.AddText(content, "Collect data:  adb pull /sdcard/Android/data/" + Application.identifier + "/files/" + Telemetry.QueueFileName, 20, Hotdrop.UI.MatchHUD.SecondaryText);
            screen.Status.text = "Never reward participants for choosing either version (spec section 25).";
            RefreshStudy();
        }

        void RefreshStudy()
        {
            if (studyStatus == null) return;
            studyStatus.text = $"Cohort: {Telemetry.Cohort}   ·   Pads {(StudySettings.PadsEnabled ? "ON" : "OFF")}   ·   Incentivized: {(Telemetry.IncentivizedTester ? "YES" : "NO")}   ·   " +
                               $"Queued events: {Telemetry.ReadQueued().Length}   ·   Tutorial will {(PlayerPrefs.GetInt(GameBootstrap.TutorialDoneKey, 0) == 0 ? "run" : "not run")} next launch";
        }
    }
}



