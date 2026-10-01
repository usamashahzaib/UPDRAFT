using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Updraft.Core
{
    /// <summary>Offline home flow; online service availability is stated explicitly.</summary>
    public class OfflineHomeScreen : MonoBehaviour
    {
        GameObject panel;
        public void Hide() { if (panel != null) panel.SetActive(false); }
        GameBootstrap game;

        public void Show(GameBootstrap game)
        {
            this.game = game;
            if (panel != null) { panel.SetActive(true); return; }
            // QA-019/020: density-aware menu layout — two columns of >= 48 dp buttons that fit a
            // phone screen; online availability and the fairness note share the status line.
            var screen = Updraft.UI.MenuLayout.Create(game.matchHUD.transform, "Home", "UPDRAFT", null);
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
            var columns = Updraft.UI.MenuLayout.Row(screen.Content, "Home_Columns");
            var play = Column(columns, "Home_Play");
            var account = Column(columns, "Home_Account");
            Updraft.UI.MenuLayout.AddButton(play, "PLAY ONLINE  ·  3v3", () =>
            {
                string reason = OnlineHooks.UnavailableReason?.Invoke() ?? (OnlineHooks.PlayOnline == null ? "Online play is not included in this build." : null);
                if (reason != null) { onlineStatus.text = reason; return; }
                panel.SetActive(false);
                OnlineHooks.PlayOnline(game);
            }, "PLAY ONLINE", HomeButtonHeight);
            Updraft.UI.MenuLayout.AddButton(play, "PRACTICE  /  RELAY", () => { panel.SetActive(false); game.touchInput.enabled = true; game.StartOfflinePractice(); }, "PRACTICE", HomeButtonHeight);
            Updraft.UI.MenuLayout.AddButton(play, "REPLAY TRAINING", () => { panel.SetActive(false); game.touchInput.enabled = true; game.StartTutorial(); }, "REPLAY TRAINING", HomeButtonHeight);
            Updraft.UI.MenuLayout.AddButton(play, "SETTINGS", () => game.GetComponent<OfflinePauseMenu>().OpenSettings(), "SETTINGS", HomeButtonHeight);
            Updraft.UI.MenuLayout.AddText(play, "THREE TEAMMATES. THREE MINUTES.\nBuild shortcuts. Watch who follows.", 22, Updraft.UI.MatchHUD.MainText, "Tagline");
            Updraft.UI.MenuLayout.AddButton(account, "PARTY", () => PlayerProductScreens.Open(game, panel, "Party"), "PARTY", HomeButtonHeight);
            Updraft.UI.MenuLayout.AddButton(account, "PROFILE", () => PlayerProductScreens.Open(game, panel, "Profile"), "PROFILE", HomeButtonHeight);
            Updraft.UI.MenuLayout.AddButton(account, "APPEARANCE", () => PlayerProductScreens.Open(game, panel, "Appearance"), "APPEARANCE", HomeButtonHeight);
            Updraft.UI.MenuLayout.AddButton(account, "ACCOUNT", () => PlayerProductScreens.Open(game, panel, "Account"), "ACCOUNT", HomeButtonHeight);
            Updraft.UI.MenuLayout.AddButton(account, "SUPPORT / REPORT", () => PlayerProductScreens.Open(game, panel, "Support"), "SUPPORT", HomeButtonHeight);
            onlineStatus = screen.Status;
            onlineStatus.text = OnlineHooks.UnavailableReason?.Invoke() ?? "All three weapons unlocked  •  Cosmetics never change power";
        }

        static RectTransform Column(Transform parent, string name)
        {
            var column = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            var element = column.GetComponent<LayoutElement>(); element.preferredWidth = 0; element.flexibleWidth = 1; // equal columns
            column.transform.SetParent(parent, false);
            var layout = column.GetComponent<VerticalLayoutGroup>();
            layout.spacing = Updraft.UI.MenuLayout.Spacing;
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            return column.GetComponent<RectTransform>();
        }

        // Five rows must fit a 20:9 phone without scrolling; still above the 48 dp floor.
        const float HomeButtonHeight = Updraft.UI.UiMetrics.MinTouchUnits + 1f;

        TextMeshProUGUI onlineStatus;

        int titleTaps;
        float lastTitleTap;
        GameObject studyPanel;
        TextMeshProUGUI studyStatus;

        void ToggleStudyPanel()
        {
            if (studyPanel != null) { studyPanel.SetActive(!studyPanel.activeSelf); RefreshStudy(); return; }
            var root = panel;
            var screen = Updraft.UI.MenuLayout.Create(root.transform, "StudyPanel", "PAD STUDY — FACILITATOR", () => studyPanel.SetActive(false));
            studyPanel = screen.Root;
            var content = screen.Content;
            studyStatus = Updraft.UI.MenuLayout.AddText(content, "", 22, Updraft.UI.MatchHUD.MainText, "StudyStatus");
            var versions = Updraft.UI.MenuLayout.Row(content);
            Updraft.UI.MenuLayout.AddButton(versions, "VERSION: WITH PADS", () => { StudySettings.SetCohort(StudySettings.PadsOn); RefreshStudy(); });
            Updraft.UI.MenuLayout.AddButton(versions, "VERSION: WITHOUT PADS", () => { StudySettings.SetCohort(StudySettings.PadsOff); RefreshStudy(); });
            Updraft.UI.MenuLayout.AddButton(content, "NORMAL PLAY (NOT STUDY)", () => { StudySettings.SetCohort("default"); RefreshStudy(); });
            Updraft.UI.MenuLayout.AddButton(content, "TOGGLE INCENTIVIZED TESTER FLAG", () => { StudySettings.SetIncentivizedTester(!StudySettings.IncentivizedTester); RefreshStudy(); });
            Updraft.UI.MenuLayout.AddButton(content, "NEW PARTICIPANT (REPLAY TUTORIAL)", () =>
            {
                PlayerPrefs.DeleteKey(GameBootstrap.TutorialDoneKey);
                Telemetry.Track("study_participant_started");
                studyPanel.SetActive(false);
                root.SetActive(false);
                game.touchInput.enabled = true;
                game.StartTutorial();
            });
            Updraft.UI.MenuLayout.AddText(content, "Collect data:  adb pull /sdcard/Android/data/" + Application.identifier + "/files/" + Telemetry.QueueFileName, 20, Updraft.UI.MatchHUD.SecondaryText);
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
