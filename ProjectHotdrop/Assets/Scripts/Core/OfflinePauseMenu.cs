using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Hotdrop.Input;

namespace Hotdrop.Core
{
    /// <summary>
    /// Pause (offline practice only; never timeScale-pause a network match) and
    /// the full settings screen from spec sections 8/16/17. Amendment 2: labeled sliders with
    /// values instead of cyclic buttons, honest graphics presets that apply real changes,
    /// a larger-UI comfort toggle and per-section resets. All settings save on this device.
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

        public const string GraphicsQualityKey = "GraphicsQuality";   // 0 = low, 1 = smooth
        public const string LargerTextKey = "LargerText";

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

        // QA-019/020: density-aware scrolling layout (>= 48 dp controls, ~12 dp text).
        void CreatePanel()
        {
            var screen = Hotdrop.UI.MenuLayout.Create(game.matchHUD.transform, "OfflinePause", "PRACTICE PAUSED", Close);
            panel = screen.Root;
            title = screen.Title;
            backButton = screen.Back;
            content = screen.Content;
            screen.Status.text = "Settings save on this device and apply offline and online.";

            var pauseRow = Hotdrop.UI.MenuLayout.Row(content, "PauseActions");
            Hotdrop.UI.MenuLayout.AddButton(pauseRow, "RESUME", Close, "RESUME");
            Hotdrop.UI.MenuLayout.AddButton(pauseRow, "RETURN HOME", () => { Close(); game.ReturnToHome(); }, "RETURN HOME");
            resumeButton = homeButton = pauseRow.gameObject;

            var input = game.touchInput;
            Section("CONTROLS");
            Toggle("LEFT-HANDED", "LeftHandedMode", false, input.SetLeftHanded);
            Toggle("INVERT LOOK", "InvertY", false, input.SetInvertY);
            NextRow();
            Toggle("VIBRATION", "VibrationEnabled", true, input.SetVibration);
            Toggle("AIM ASSIST (SLOWDOWN ONLY)", "AimAssistEnabled", true, value =>
            {
                foreach (var player in Hotdrop.Player.PlayerController.All) player.SetAimAssistEnabled(value);
            });
            SliderRow("LOOK SENSITIVITY", "AimSensitivity", 0.5f, 2.5f, 1f, v => $"{v:0.00}x", input.SetAimSensitivity);
            if (TouchInputManager.GyroAvailable)
            {
                Toggle("GYRO AIM", "GyroAim", false, input.SetGyroAim);
                SliderRow("GYRO SENSITIVITY", "GyroSensitivity", 0.5f, 2f, 1f, v => $"{v:0.00}x", _ => { });
                NextRow();
                Hotdrop.UI.MenuLayout.AddButton(row, "CALIBRATE GYRO (HOLD STILL)", input.CalibrateGyroNeutral, "CALIBRATE");
                Hotdrop.UI.MenuLayout.AddText(content, "Gyro calibration is available on this device.", 18, Hotdrop.UI.MatchHUD.SecondaryText);
            }
            else
            {
                Hotdrop.UI.MenuLayout.AddText(content, "GYRO AIM — NOT AVAILABLE ON THIS DEVICE (no gyroscope detected)", 20, Hotdrop.UI.MatchHUD.SecondaryText);
            }
            NextRow();
            Hotdrop.UI.MenuLayout.AddButton(row, "CUSTOMIZE CONTROLS", () =>
            {
                panel.SetActive(false);
                input.BeginLayoutEdit(() =>
                {
                    input.enabled = false;
                    panel.SetActive(true);
                    panel.transform.SetAsLastSibling();
                });
            });
            Hotdrop.UI.MenuLayout.AddButton(row, "RESET CONTROLS", ResetControls, "RESET CONTROLS");

            Section("COMFORT");
            Toggle("CAMERA SHAKE", "CameraShakeEnabled", true, value => game.mainCamera.SetShakeEnabled(value));
            Toggle("LARGER TEXT / HUD", LargerTextKey, false, ApplyLargerText);
            Toggle("MUTE TEAM PINGS", Hotdrop.UI.PingSystem.MuteKey, false, _ => { });

            Section("GRAPHICS");
            var qualityRow = Hotdrop.UI.MenuLayout.Row(content, "QualityRow");
            AddQualityButton(qualityRow, 0, "LOW — 30 FPS TARGET", "Steady on low-end phones; render scale 80%.");
            AddQualityButton(qualityRow, 1, "SMOOTH — 60 FPS TARGET", "Full render scale where the device sustains it.");
            graphicsNote = Hotdrop.UI.MenuLayout.AddText(content, "", 18, Hotdrop.UI.MatchHUD.SecondaryText, "GraphicsNote");
            Hotdrop.UI.MenuLayout.AddButton(content, "RESET GRAPHICS", ResetGraphics, "RESET GRAPHICS");

            Section("AUDIO");
            Volume("MASTER VOLUME", GameAudio.MasterKey);
            Volume("SFX VOLUME", GameAudio.SfxKey);
            Volume("MUSIC VOLUME", GameAudio.MusicKey);
            Volume("ANNOUNCER VOLUME", GameAudio.AnnouncerKey);
            Hotdrop.UI.MenuLayout.AddButton(content, "RESET AUDIO", ResetAudio, "RESET AUDIO");

            Section("ACCOUNT & SUPPORT");
            Hotdrop.UI.MenuLayout.AddText(content,
                "Profile, linked account, support reports and deletion live on Home under MORE.",
                19, Hotdrop.UI.MatchHUD.SecondaryText);
        }

        TextMeshProUGUI graphicsNote;
        RectTransform content, row;
        RectTransform NextRow() => row = Hotdrop.UI.MenuLayout.Row(content, "SettingsRow");

        void Section(string name)
        {
            NextRow();
            var text = Hotdrop.UI.MenuLayout.AddText(content, name, 24, Hotdrop.UI.MatchHUD.Friendly, "Section_" + name);
            text.fontStyle = FontStyles.Bold;
        }

        static void Tap() => GameAudio.Play2D(GameCue.UiTap);

        void Toggle(string name, string key, bool defaultValue, System.Action<bool> apply)
        {
            if (row == null) NextRow();
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

        /// <summary>Amendment 2: a labeled slider with its live value, no cyclic stepping.</summary>
        void SliderRow(string name, string key, float min, float max, float defaultValue, System.Func<float, string> format, System.Action<float> apply)
        {
            NextRow();
            Hotdrop.UI.MenuLayout.AddText(row, name, 20, Hotdrop.UI.MatchHUD.MainText, "Label");
            var value = Hotdrop.UI.MenuLayout.AddText(row, format(PlayerPrefs.GetFloat(key, defaultValue)), 20, Hotdrop.UI.MatchHUD.Friendly, "Value");
            var slider = BuildSlider(row, min, max, PlayerPrefs.GetFloat(key, defaultValue));
            slider.onValueChanged.AddListener(v =>
            {
                PlayerPrefs.SetFloat(key, v); PlayerPrefs.Save();
                value.text = format(v);
                apply(v);
            });
        }

        static Slider BuildSlider(Transform parent, float min, float max, float value)
        {
            var obj = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
            obj.transform.SetParent(parent, false);
            var element = obj.GetComponent<LayoutElement>();
            element.minHeight = Hotdrop.UI.UiMetrics.MinTouchUnits;
            element.minWidth = 220f; element.flexibleWidth = 1f;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(obj.transform, false);
            var backgroundRt = background.GetComponent<RectTransform>();
            backgroundRt.anchorMin = new Vector2(0f, 0.35f); backgroundRt.anchorMax = new Vector2(1f, 0.65f);
            backgroundRt.offsetMin = Vector2.zero; backgroundRt.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0.106f, 0.161f, 0.251f, 1f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(obj.transform, false);
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0.35f); fillAreaRt.anchorMax = new Vector2(1f, 0.65f);
            fillAreaRt.offsetMin = new Vector2(0f, 0f); fillAreaRt.offsetMax = new Vector2(0f, 0f);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = Hotdrop.UI.MatchHUD.Friendly;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(obj.transform, false);
            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero; handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = Vector2.zero; handleAreaRt.offsetMax = Vector2.zero;
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(34f, Hotdrop.UI.UiMetrics.MinTouchUnits);
            handle.GetComponent<Image>().color = Hotdrop.UI.MatchHUD.MainText;

            var slider = obj.GetComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min; slider.maxValue = max; slider.wholeNumbers = false;
            slider.fillRect = fillRt; slider.handleRect = handleRt;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.value = value;
            return slider;
        }

        void AddQualityButton(Transform parent, int quality, string caption, string note)
        {
            var button = Hotdrop.UI.MenuLayout.AddButton(parent, caption, () =>
            {
                PlayerPrefs.SetInt(GraphicsQualityKey, quality); PlayerPrefs.Save();
                GameBootstrap.ApplyGraphicsQuality(quality);
                RefreshGraphicsNote();
            }, "Quality_" + quality, 100);
            button.GetComponentInChildren<TextMeshProUGUI>().fontSizeMax = 22;
        }

        void RefreshGraphicsNote()
        {
            if (graphicsNote == null) return;
            int quality = PlayerPrefs.GetInt(GraphicsQualityKey, 0);
            graphicsNote.text = quality == 0
                ? "ACTIVE: LOW — 30 FPS target, 80% render scale. Applies immediately; measure on device."
                : "ACTIVE: SMOOTH — 60 FPS target, full render scale. Falls back nowhere; watch thermals.";
        }

        void Volume(string name, string key) =>
            SliderRow(name, key, 0f, 1f, GameAudio.Volume(key), v => $"{v * 100:0}%", v => GameAudio.SetVolume(key, v));

        void ResetControls()
        {
            PlayerPrefs.DeleteKey("LeftHandedMode"); PlayerPrefs.DeleteKey("InvertY");
            PlayerPrefs.DeleteKey("AimSensitivity"); PlayerPrefs.DeleteKey("AimAssistEnabled");
            PlayerPrefs.DeleteKey("GyroAim"); PlayerPrefs.DeleteKey("GyroSensitivity");
            PlayerPrefs.Save();
            game.touchInput.SetLeftHanded(false); game.touchInput.SetInvertY(false);
            game.touchInput.SetAimSensitivity(1f);
            game.touchInput.ResetButtonLayout();
            foreach (var player in Hotdrop.Player.PlayerController.All) player.SetAimAssistEnabled(true);
            RefreshAll();
        }

        static void ResetGraphics()
        {
            PlayerPrefs.DeleteKey(GraphicsQualityKey); PlayerPrefs.DeleteKey(LargerTextKey);
            PlayerPrefs.Save();
            GameBootstrap.ApplyGraphicsQuality(0);
            ApplyLargerText(false);
        }

        void ResetAudio()
        {
            GameAudio.SetVolume(GameAudio.MasterKey, 1f);
            GameAudio.SetVolume(GameAudio.SfxKey, 1f);
            GameAudio.SetVolume(GameAudio.MusicKey, 0.6f);
            GameAudio.SetVolume(GameAudio.AnnouncerKey, 0.8f);
            RefreshAll();
        }

        void RefreshAll()
        {
            foreach (var r in refreshers) r();
            RefreshGraphicsNote();
        }

        /// <summary>
        /// Amendment 2 comfort option: raises (or restores) every on-screen label - the match HUD
        /// and every menu - without moving the touch controls. Screens built after the change use
        /// <see cref="Hotdrop.UI.UiMetrics.TextScale"/>; screens already on screen scale in place.
        /// </summary>
        public static void ApplyLargerText(bool enabled)
        {
            float target = enabled ? Hotdrop.UI.UiMetrics.LargerTextScale : 1f;
            float factor = target / Hotdrop.UI.UiMetrics.TextScale;
            Hotdrop.UI.UiMetrics.TextScale = target;
            if (!Mathf.Approximately(factor, 1f))
            {
                foreach (var text in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    text.fontSize *= factor;
                    if (text.enableAutoSizing)
                    {
                        text.fontSizeMin *= factor;
                        text.fontSizeMax *= factor;
                    }
                }
            }
            PlayerPrefs.SetInt(LargerTextKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            var scaler = Object.FindFirstObjectByType<Hotdrop.UI.DensityCanvasScaler>(FindObjectsInactive.Include);
            if (scaler != null) scaler.Apply();
        }

        /// <summary>A settings button in the current row; returns its label for live refresh.</summary>
        TextMeshProUGUI Action(string name, UnityEngine.Events.UnityAction action)
        {
            var button = Hotdrop.UI.MenuLayout.AddButton(row, name, action, string.IsNullOrEmpty(name) ? "Setting" : name);
            return button.GetComponentInChildren<TextMeshProUGUI>();
        }
    }
}



