using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Updraft.Input;

namespace Updraft.Core
{
    /// <summary>
    /// Pause (offline practice only; never timeScale-pause a network match) and
    /// the full settings screen from spec sections 8/16/17. All settings save
    /// locally on this device.
    /// </summary>
    public class OfflinePauseMenu : MonoBehaviour
    {
        GameBootstrap game;
        GameObject panel;
        GameObject resumeButton, homeButton, backButton;
        TextMeshProUGUI title;
        float previousScale = 1f;
        bool pausedGame;
        readonly System.Collections.Generic.List<System.Action> refreshers = new System.Collections.Generic.List<System.Action>();

        public bool IsOpen => panel != null && panel.activeSelf;

        void Awake() { game = GetComponent<GameBootstrap>(); }

        void LateUpdate()
        {
            if (game.touchInput != null && game.touchInput.enabled && !game.touchInput.EditingLayout && game.touchInput.PausePressed) Open();
        }

        /// <summary>In-match pause with Resume / Return Home.</summary>
        public void Open() => Show(asPause: true);

        /// <summary>Settings opened from Home: Back instead of Resume.</summary>
        public void OpenSettings() => Show(asPause: false);

        void Show(bool asPause)
        {
            if (IsOpen || game.matchHUD == null) return;
            pausedGame = asPause;
            if (asPause)
            {
                previousScale = Time.timeScale;
                Time.timeScale = 0;
            }
            game.touchInput.enabled = false;
            if (panel == null) CreatePanel();
            resumeButton.SetActive(asPause);
            homeButton.SetActive(asPause);
            backButton.SetActive(!asPause);
            title.text = asPause ? "PRACTICE PAUSED" : "SETTINGS";
            foreach (var r in refreshers) r();
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false);
            if (pausedGame)
            {
                Time.timeScale = previousScale;
                game.touchInput.enabled = true;
            }
            pausedGame = false;
        }

        void OnDestroy() { if (IsOpen && pausedGame) Time.timeScale = previousScale; }

        // QA-019/020: density-aware scrolling layout (>= 48 dp controls, ~12 dp text) instead of
        // fixed 1920x1080 coordinates that overflowed a phone. BACK sits in the top bar.
        void CreatePanel()
        {
            var screen = Updraft.UI.MenuLayout.Create(game.matchHUD.transform, "OfflinePause", "PRACTICE PAUSED", Close);
            panel = screen.Root;
            title = screen.Title;
            backButton = screen.Back;
            content = screen.Content;
            screen.Status.text = "Settings save on this device.";

            var pauseRow = Updraft.UI.MenuLayout.Row(content, "PauseActions");
            Updraft.UI.MenuLayout.AddButton(pauseRow, "RESUME", Close, "RESUME");
            Updraft.UI.MenuLayout.AddButton(pauseRow, "RETURN HOME", () => { Close(); game.ReturnToHome(); }, "RETURN HOME");
            resumeButton = homeButton = pauseRow.gameObject;

            var input = game.touchInput;
            NextRow();
            Toggle("LEFT-HANDED", "LeftHandedMode", false, input.SetLeftHanded);
            Toggle("INVERT LOOK", "InvertY", false, input.SetInvertY);
            NextRow();
            Toggle("VIBRATION", "VibrationEnabled", true, input.SetVibration);
            Toggle("AIM ASSIST", "AimAssistEnabled", true, value =>
            {
                foreach (var player in Updraft.Player.PlayerController.All) player.SetAimAssistEnabled(value);
            });
            NextRow();
            Cycle("LOOK SENSITIVITY", "AimSensitivity", 1f, new[] { .5f, .75f, 1f, 1.25f, 1.5f, 2f, 2.5f }, v => $"{v:0.00}x", input.SetAimSensitivity);
            if (TouchInputManager.GyroAvailable)
            {
                Toggle("GYRO AIM", "GyroAim", false, input.SetGyroAim);
                NextRow();
                Updraft.UI.MenuLayout.AddText(row, "Hold device still, then calibrate neutral aim", 18, Updraft.UI.MatchHUD.SecondaryText);
                NextRow();
                Updraft.UI.MenuLayout.AddButton(row, "CALIBRATE GYRO (HOLD STILL)", input.CalibrateGyroNeutral, "CALIBRATE");
            }
            else
                Updraft.UI.MenuLayout.AddText(row, "GYRO AIM  —  NO GYROSCOPE ON THIS DEVICE", 20, Updraft.UI.MatchHUD.SecondaryText);
            NextRow();
            Toggle("CAMERA SHAKE", "CameraShakeEnabled", true, value => game.mainCamera.SetShakeEnabled(value));
            Toggle("MUTE TEAM PINGS", Updraft.UI.PingSystem.MuteKey, false, _ => { });
            NextRow();
            Volume("MASTER VOLUME", GameAudio.MasterKey);
            Volume("SFX VOLUME", GameAudio.SfxKey);
            NextRow();
            Volume("MUSIC VOLUME", GameAudio.MusicKey);
            Volume("ANNOUNCER VOLUME", GameAudio.AnnouncerKey);
            NextRow();
            Updraft.UI.MenuLayout.AddButton(row, "CUSTOMIZE CONTROLS", () =>
            {
                panel.SetActive(false);
                input.BeginLayoutEdit(() =>
                {
                    input.enabled = false;
                    panel.SetActive(true);
                    panel.transform.SetAsLastSibling();
                });
            });
            Updraft.UI.MenuLayout.AddButton(row, "RESET CONTROL LAYOUT", input.ResetButtonLayout);
        }

        RectTransform content, row;
        void NextRow() => row = Updraft.UI.MenuLayout.Row(content, "SettingsRow");

        static void Tap() => GameAudio.Play2D(GameCue.UiTap);

        void Toggle(string name, string key, bool defaultValue, System.Action<bool> apply)
        {
            var label = Action("", null);
            System.Action refresh = () => label.text = name + "  " + (PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1 ? "ON" : "OFF");
            refresh();
            refreshers.Add(refresh);
            label.GetComponentInParent<Button>().onClick.AddListener(() =>
            {
                Tap();
                bool value = PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 1;
                PlayerPrefs.SetInt(key, value ? 1 : 0); PlayerPrefs.Save(); apply(value); refresh();
            });
        }

        void Cycle(string name, string key, float defaultValue, float[] values, System.Func<float, string> format, System.Action<float> apply)
        {
            var label = Action("", null);
            System.Action refresh = () => label.text = $"{name}  {format(PlayerPrefs.GetFloat(key, defaultValue))}";
            refresh();
            refreshers.Add(refresh);
            label.GetComponentInParent<Button>().onClick.AddListener(() =>
            {
                Tap();
                float current = PlayerPrefs.GetFloat(key, defaultValue);
                int index = System.Array.FindIndex(values, v => v > current + 0.001f);
                float next = index < 0 ? values[0] : values[index];
                PlayerPrefs.SetFloat(key, next); PlayerPrefs.Save(); apply(next); refresh();
            });
        }

        void Volume(string name, string key) =>
            Cycle(name, key, GameAudio.Volume(key), new[] { 0f, .25f, .5f, .75f, 1f }, v => $"{v * 100:0}%", v => GameAudio.SetVolume(key, v));

        /// <summary>A settings button in the current row; returns its label for live refresh.</summary>
        TextMeshProUGUI Action(string name, UnityEngine.Events.UnityAction action)
        {
            var button = Updraft.UI.MenuLayout.AddButton(row, name, action, string.IsNullOrEmpty(name) ? "Setting" : name);
            return button.GetComponentInChildren<TextMeshProUGUI>();
        }
    }
}
