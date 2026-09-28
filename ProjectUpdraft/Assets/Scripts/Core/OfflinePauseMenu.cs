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

        void CreatePanel()
        {
            panel = new GameObject("OfflinePause", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(game.matchHUD.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(.063f, .094f, .153f, .99f);
            title = Label("PRACTICE PAUSED", new Vector2(0, 420), 44);

            float y = 330f, step = 80f;
            resumeButton = Action("RESUME", new Vector2(-300, y), () => { Tap(); Close(); }).transform.parent.gameObject;
            homeButton = Action("RETURN HOME", new Vector2(300, y), () => { Tap(); Close(); game.ReturnToHome(); }).transform.parent.gameObject;
            backButton = Action("BACK", new Vector2(0, y), () => { Tap(); Close(); }).transform.parent.gameObject;

            var input = game.touchInput;
            y -= step;
            Toggle("LEFT-HANDED", "LeftHandedMode", false, new Vector2(-300, y), input.SetLeftHanded);
            Toggle("INVERT LOOK", "InvertY", false, new Vector2(300, y), input.SetInvertY);
            y -= step;
            Toggle("VIBRATION", "VibrationEnabled", true, new Vector2(-300, y), input.SetVibration);
            Toggle("AIM ASSIST", "AimAssistEnabled", true, new Vector2(300, y), value =>
            {
                foreach (var player in Updraft.Player.PlayerController.All) player.SetAimAssistEnabled(value);
            });
            y -= step;
            Cycle("LOOK SENSITIVITY", "AimSensitivity", 1f, new[] { .5f, .75f, 1f, 1.25f, 1.5f, 2f, 2.5f }, v => $"{v:0.00}x",
                new Vector2(-300, y), input.SetAimSensitivity);
            if (TouchInputManager.GyroAvailable)
                Toggle("GYRO AIM", "GyroAim", false, new Vector2(300, y), input.SetGyroAim);
            else
                Label("GYRO AIM  —  NO GYROSCOPE ON THIS DEVICE", new Vector2(300, y), 20, width: 550);
            y -= step;
            Toggle("CAMERA SHAKE", "CameraShakeEnabled", true, new Vector2(-300, y), value => game.mainCamera.SetShakeEnabled(value));
            Toggle("MUTE TEAM PINGS", Updraft.UI.PingSystem.MuteKey, false, new Vector2(300, y), _ => { });
            y -= step;
            Volume("MASTER VOLUME", GameAudio.MasterKey, new Vector2(-300, y));
            Volume("SFX VOLUME", GameAudio.SfxKey, new Vector2(300, y));
            y -= step;
            Volume("MUSIC VOLUME", GameAudio.MusicKey, new Vector2(-300, y));
            Volume("ANNOUNCER VOLUME", GameAudio.AnnouncerKey, new Vector2(300, y));
            y -= step;
            Action("CUSTOMIZE CONTROLS", new Vector2(-300, y), () =>
            {
                Tap();
                panel.SetActive(false);
                input.BeginLayoutEdit(() =>
                {
                    input.enabled = false;
                    panel.SetActive(true);
                    panel.transform.SetAsLastSibling();
                });
            });
            Action("RESET CONTROL LAYOUT", new Vector2(300, y), () => { Tap(); input.ResetButtonLayout(); });
            Label("Settings save on this device.", new Vector2(0, y - 75f), 20);
        }

        static void Tap() => GameAudio.Play2D(GameCue.UiTap);

        void Toggle(string name, string key, bool defaultValue, Vector2 position, System.Action<bool> apply)
        {
            var label = Action("", position, null);
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

        void Cycle(string name, string key, float defaultValue, float[] values, System.Func<float, string> format, Vector2 position, System.Action<float> apply)
        {
            var label = Action("", position, null);
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

        void Volume(string name, string key, Vector2 position) =>
            Cycle(name, key, GameAudio.Volume(key), new[] { 0f, .25f, .5f, .75f, 1f }, v => $"{v * 100:0}%", position, v => GameAudio.SetVolume(key, v));

        TextMeshProUGUI Action(string name, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var obj = new GameObject(string.IsNullOrEmpty(name) ? "Setting" : name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(panel.transform, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(550, 66);
            var image = obj.GetComponent<Image>(); image.color = new Color(.106f, .161f, .251f);
            var button = obj.GetComponent<Button>(); button.targetGraphic = image;
            if (action != null) button.onClick.AddListener(action);
            return Label(name, Vector2.zero, 24, obj.transform);
        }

        TextMeshProUGUI Label(string text, Vector2 position, float size, Transform parent = null, float width = 0f)
        {
            var obj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent == null ? panel.transform : parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(width > 0 ? width : parent == null ? 1400 : 540, size + 24);
            var tmp = obj.GetComponent<TextMeshProUGUI>(); tmp.text = text; tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white; tmp.raycastTarget = false;
            return tmp;
        }
    }
}
