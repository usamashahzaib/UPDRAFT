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
            panel = new GameObject("Home", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(game.matchHUD.transform, false);
            var rt = panel.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.063f, 0.094f, 0.153f, 0.98f);
            Label("UPDRAFT", new Vector2(0, 220), 88);
            // Facilitator-only study controls: tap the title five times.
            Button("", new Vector2(0, 220), () =>
            {
                if (Time.unscaledTime - lastTitleTap > 1.5f) titleTaps = 0;
                lastTitleTap = Time.unscaledTime;
                if (++titleTaps >= 5) { titleTaps = 0; ToggleStudyPanel(); }
            }, transparent: true);
            Label("THREE TEAMMATES. THREE MINUTES.", new Vector2(0, 135), 24);
            Label("Build shortcuts. Watch who follows.", new Vector2(0, 90), 24);
            Button("PLAY ONLINE  ·  3v3", new Vector2(0, 0), () =>
            {
                Tap();
                string reason = OnlineHooks.UnavailableReason?.Invoke() ?? (OnlineHooks.PlayOnline == null ? "Online play is not included in this build." : null);
                if (reason != null) { onlineStatus.text = reason; return; }
                panel.SetActive(false);
                OnlineHooks.PlayOnline(game);
            }, height: 80);
            Button("PRACTICE  /  RELAY", new Vector2(0, -90), () => { Tap(); panel.SetActive(false); game.touchInput.enabled = true; game.StartOfflinePractice(); }, height: 80);
            Button("REPLAY TRAINING", new Vector2(0, -180), () => { Tap(); panel.SetActive(false); game.touchInput.enabled = true; game.StartTutorial(); }, height: 80);
            Button("SETTINGS", new Vector2(0, -270), () => { Tap(); game.GetComponent<OfflinePauseMenu>().OpenSettings(); }, height: 80);
            onlineStatus = Label(OnlineHooks.UnavailableReason?.Invoke() ?? "", new Vector2(0, -345), 20);
            Label("All three weapons unlocked  •  Cosmetics never change power", new Vector2(0, -380), 18);
        }

        TextMeshProUGUI onlineStatus;
        static void Tap() => GameAudio.Play2D(GameCue.UiTap);

        int titleTaps;
        float lastTitleTap;
        GameObject studyPanel;
        TextMeshProUGUI studyStatus;

        void ToggleStudyPanel()
        {
            if (studyPanel != null) { studyPanel.SetActive(!studyPanel.activeSelf); RefreshStudy(); return; }
            var root = panel;
            studyPanel = new GameObject("StudyPanel", typeof(RectTransform), typeof(Image));
            studyPanel.transform.SetParent(root.transform, false);
            var rt = studyPanel.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(1300, 820);
            studyPanel.GetComponent<Image>().color = new Color(0.106f, 0.161f, 0.251f, 1f);
            panel = studyPanel;
            Label("PAD STUDY — FACILITATOR", new Vector2(0, 330), 36);
            studyStatus = Label("", new Vector2(0, 250), 24);
            Button("VERSION: WITH PADS", new Vector2(-300, 140), () => { StudySettings.SetCohort(StudySettings.PadsOn); RefreshStudy(); });
            Button("VERSION: WITHOUT PADS", new Vector2(300, 140), () => { StudySettings.SetCohort(StudySettings.PadsOff); RefreshStudy(); });
            Button("NORMAL PLAY (NOT STUDY)", new Vector2(0, 30), () => { StudySettings.SetCohort("default"); RefreshStudy(); });
            Button("NEW PARTICIPANT (REPLAY TUTORIAL)", new Vector2(0, -80), () =>
            {
                PlayerPrefs.DeleteKey(GameBootstrap.TutorialDoneKey);
                Telemetry.Track("study_participant_started");
                studyPanel.SetActive(false);
                root.SetActive(false);
                game.touchInput.enabled = true;
                game.StartTutorial();
            });
            Label("Collect data:  adb pull /sdcard/Android/data/" + Application.identifier + "/files/" + Telemetry.QueueFileName, new Vector2(0, -190), 20);
            Label("Never reward participants for choosing either version (spec section 25).", new Vector2(0, -235), 20);
            Button("CLOSE", new Vector2(0, -320), () => studyPanel.SetActive(false));
            panel = root;
            RefreshStudy();
        }

        void RefreshStudy()
        {
            if (studyStatus == null) return;
            studyStatus.text = $"Cohort: {Telemetry.Cohort}   ·   Pads {(StudySettings.PadsEnabled ? "ON" : "OFF")}   ·   " +
                               $"Queued events: {Telemetry.ReadQueued().Length}   ·   Tutorial will {(PlayerPrefs.GetInt(GameBootstrap.TutorialDoneKey, 0) == 0 ? "run" : "not run")} next launch";
        }

        TextMeshProUGUI Label(string value, Vector2 position, float size)
        {
            var obj = new GameObject(value, typeof(RectTransform), typeof(TextMeshProUGUI)); obj.transform.SetParent(panel.transform, false);
            var rt = obj.GetComponent<RectTransform>(); rt.anchoredPosition = position; rt.sizeDelta = new Vector2(1100, size + 20);
            var text = obj.GetComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = size; text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.957f, 0.969f, 0.984f); text.raycastTarget = false; return text;
        }
        void Button(string value, Vector2 position, UnityEngine.Events.UnityAction action, bool transparent = false, float height = 90)
        {
            var obj = new GameObject(value, typeof(RectTransform), typeof(Image), typeof(Button)); obj.transform.SetParent(panel.transform, false);
            var rt = obj.GetComponent<RectTransform>(); rt.anchoredPosition = position; rt.sizeDelta = new Vector2(540, height);
            var image = obj.GetComponent<Image>(); image.color = transparent ? new Color(0, 0, 0, 0) : new Color(0.106f, 0.161f, 0.251f);
            var button = obj.GetComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); label.transform.SetParent(obj.transform, false);
            label.GetComponent<RectTransform>().sizeDelta = new Vector2(520, 80);
            var text = label.GetComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = 26; text.alignment = TextAlignmentOptions.Center; text.color = new Color(0.188f, 0.835f, 0.91f); text.raycastTarget = false;
        }
    }
}
