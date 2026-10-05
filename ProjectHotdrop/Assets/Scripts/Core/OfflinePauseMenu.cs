using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Hotdrop.Input;
using Hotdrop.UI;

namespace Hotdrop.Core
{
    /// <summary>
    /// Pause (offline practice only; never timeScale-pause a network match) and the full settings
    /// screen, presented as UIUX screen 10's Match Menu console (CSS px). Every control is a real
    /// setting saved on this device; where the reference shows a feature this game does not have
    /// (control presets, focus aim) the slot carries the real equivalent (handedness, gyro).
    /// </summary>
    public class OfflinePauseMenu : MonoBehaviour
    {
        GameBootstrap game;
        GameObject panel;
        GameObject scrim;
        GameObject resumeButton, homeButton, backButton;
        TextMeshProUGUI title, subtitle, footerNote;
        RectTransform overlayChip, overlayChipPlate;
        float homeWidth;
        float previousScale = 1f;
        bool pausedGame;
        bool inputWasEnabled;
        readonly System.Collections.Generic.List<System.Action> refreshers = new System.Collections.Generic.List<System.Action>();
        readonly System.Collections.Generic.Dictionary<string, RectTransform> sections = new System.Collections.Generic.Dictionary<string, RectTransform>();
        readonly System.Collections.Generic.Dictionary<string, (Image plate, TextMeshProUGUI label, TextMeshProUGUI icon)> tabs =
            new System.Collections.Generic.Dictionary<string, (Image, TextMeshProUGUI, TextMeshProUGUI)>();
        RectTransform modal, scrollContent;
        ScrollRect scroll;

        public const string GraphicsQualityKey = "GraphicsQuality";   // 0 = low, 1 = smooth
        public const string LargerTextKey = "LargerText";

        static readonly Color Primary = UiDraw.Hex("#7cefff");
        static readonly Color OnPrimary = UiDraw.Hex("#00363c");
        static readonly Color Container = UiDraw.Hex("#18202f");
        static readonly Color High = UiDraw.Hex("#222a3a");
        static readonly Color Highest = UiDraw.Hex("#2d3545");
        static readonly Color Lowest = UiDraw.Hex("#060e1d");
        static readonly Color OnSurface = UiDraw.Hex("#dbe2f8");
        static readonly Color OnVariant = UiDraw.Hex("#bbc9cb");
        static readonly Color Outline = UiDraw.Hex("#859396");
        static readonly Color OutlineVariant = UiDraw.Hex("#3c494b");
        static readonly Color Tertiary = UiDraw.Hex("#ffda88");
        static readonly Color SecondaryDim = UiDraw.Hex("#ffb2b7");

        static readonly string[] TabIds = { "CONTROLS", "GRAPHICS", "AUDIO", "ACCESSIBILITY", "ACCOUNT / SUPPORT" };

        public bool IsOpen => panel != null && panel.activeSelf;

        void Awake() { game = GetComponent<GameBootstrap>(); }

        void LateUpdate()
        {
            if (game.touchInput == null || !game.touchInput.enabled || game.touchInput.EditingLayout || !game.touchInput.PausePressed) return;
            var challenges = game.GetComponent<OfflineChallengeRuntime>();
            if (challenges != null && challenges.Controller.CurrentState == ChallengeFlowState.Gameplay && challenges.Controller.IsAttemptActive)
            { challenges.Controller.PauseChallenge(); return; }
            // A networked match must continue simulating. The identical corner control opens a
            // Match Menu/settings overlay, never a pause and never changes timeScale.
            Show(asPause: !HasOnlinePresenter());
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
            inputWasEnabled = game.touchInput != null && game.touchInput.enabled;
            if (game.touchInput != null) game.touchInput.enabled = false;
            if (panel == null) CreatePanel();
            bool online = HasOnlinePresenter();
            bool inMatch = asPause || online;
            resumeButton.SetActive(inMatch);
            homeButton.SetActive(inMatch);
            backButton.SetActive(!inMatch);
            footerNote.text = online ? "Match is active. Leaving returns you to Home."
                : asPause ? "Practice is paused. Your run resumes where you left it."
                : "Settings save on this device and apply offline and online.";
            title.text = asPause ? "PRACTICE PAUSED" : online ? "MATCH MENU" : "SETTINGS";
            // The chip follows the title; the note starts where Return Home ends (or at the edge).
            float chipX = 50f + UiDraw.Width(title) + 12f;
            overlayChip.anchoredPosition = new Vector2(chipX, overlayChip.anchoredPosition.y);
            overlayChipPlate.anchoredPosition = new Vector2(chipX, overlayChipPlate.anchoredPosition.y);
            footerNote.rectTransform.anchoredPosition = new Vector2(inMatch ? 20f + homeWidth + 12f : 20f, 0f);
            subtitle.text = online ? "Online match continues in background. You remain vulnerable to fire."
                : "Settings save on this device and apply offline and online.";
            subtitle.color = online ? SecondaryDim : OnVariant;
            var warn = subtitle.transform.parent.Find("WarnIcon");
            if (warn != null) warn.gameObject.SetActive(online);
            if (scrim != null)
            {
                scrim.SetActive(true);
                scrim.GetComponent<Image>().color = inMatch ? new Color(.024f, .055f, .114f, .76f) : UiDraw.Hex("#0b1322");
                scrim.transform.SetAsLastSibling();
            }
            RefreshAll();
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false);
            if (scrim != null) scrim.SetActive(false);
            if (pausedGame) Time.timeScale = previousScale;
            if (game.touchInput != null && inputWasEnabled) game.touchInput.enabled = true;
            pausedGame = false;
        }

        void OnDestroy()
        {
            if (IsOpen && pausedGame) Time.timeScale = previousScale;
            if (scrim != null) Destroy(scrim);
        }

        // Networking is compiled in a separate optional assembly. Check its live presenter by
        // name so the offline build stays independent while an online match can never pause.
        static bool HasOnlinePresenter()
        {
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (behaviour.GetType().FullName == "Hotdrop.Network.NetClientPresenter") return true;
            return false;
        }

        // ------------------------------------------------------------ console frame

        void CreatePanel()
        {
            var hudRect = game.matchHUD.GetComponent<RectTransform>();
            scrim = new GameObject("SettingsScrim", typeof(RectTransform), typeof(Image));
            scrim.transform.SetParent(game.matchHUD.transform, false);
            UiDraw.Fill((RectTransform)scrim.transform);
            scrim.SetActive(false);

            panel = new GameObject("OfflinePause", typeof(RectTransform)).gameObject;
            panel.transform.SetParent(game.matchHUD.transform, false);
            var root = (RectTransform)panel.transform;
            UiDraw.Fill(root);
            // Blocks touches to whatever is underneath while the console is up.
            root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            float canvasW = hudRect.rect.width > 0f ? hudRect.rect.width : 1280f;
            float width = Mathf.Min(1152f, canvasW - 32f);
            var card = UiDraw.Box(root, "Console", UiDraw.Hex("#18202f", .95f), 12f);
            modal = card.rectTransform;
            modal.anchorMin = new Vector2(.5f, 0f); modal.anchorMax = new Vector2(.5f, 1f); modal.pivot = new Vector2(.5f, .5f);
            modal.sizeDelta = new Vector2(width, -16f); modal.anchoredPosition = Vector2.zero;

            // Header: tune glyph, title, local-overlay chip, live note; saved-locally chip right.
            var header = UiDraw.Box(modal, "Nav_Bar", UiDraw.Hex("#222a3a", .8f), 12f).rectTransform;
            header.anchorMin = new Vector2(0f, 1f); header.anchorMax = Vector2.one; header.pivot = new Vector2(.5f, 1f);
            header.sizeDelta = new Vector2(0f, 76f); header.anchoredPosition = Vector2.zero;
            UiDraw.Place(UiDraw.Icon(header, "Tune", "tune", 22f, Primary).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(20f, -29f), new Vector2(22f, 22f));
            title = UiDraw.Text(header, "Nav_Title", "SETTINGS", UiFont.Heading, 28f, OnSurface, .025f);
            UiDraw.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(50f, -29f), new Vector2(320f, 34f));
            var chip = UiDraw.Text(header, "OverlayChip", "LOCAL SETTINGS // THIS DEVICE", UiFont.MonoMedium, 11f, Primary, .05f, TextAlignmentOptions.Midline);
            float cw = UiDraw.Width(chip) + 16f;
            var chipPlate = UiDraw.Box(header, "OverlayChipPlate", Highest, 4f);
            chipPlate.transform.SetSiblingIndex(chip.transform.GetSiblingIndex());
            UiDraw.Place(chipPlate.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(312f, -29f), new Vector2(cw, 20f));
            UiDraw.Place(chip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(312f, -29f), new Vector2(cw, 20f));
            overlayChip = chip.rectTransform; overlayChipPlate = chipPlate.rectTransform;
            UiDraw.Place(UiDraw.Icon(header, "WarnIcon", "warning", 14f, SecondaryDim).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(20f, -56f), new Vector2(14f, 14f));
            subtitle = UiDraw.Text(header, "Subtitle", "", UiFont.Body, 12f, OnVariant, .01f);
            UiDraw.Place(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(38f, -56f), new Vector2(600f, 16f));
            var saved = UiDraw.Box(header, "SavedChip", Lowest, 8f);
            var savedText = UiDraw.Text(saved.transform, "Text", "ALL SETTINGS SAVED LOCALLY", UiFont.MonoMedium, 11f, Primary, .05f);
            float sw = 12f + 16f + 4f + UiDraw.Width(savedText) + 12f;
            UiDraw.Place(saved.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-20f, 0f), new Vector2(sw, 30f));
            UiDraw.Place(UiDraw.Icon(saved.transform, "Icon", "check_circle", 16f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(16f, 16f));
            UiDraw.Place(savedText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(32f, 0f), new Vector2(UiDraw.Width(savedText) + 2f, 14f));

            // Tab bar.
            var tabBar = UiDraw.Box(modal, "SettingsTabs", UiDraw.Hex("#060e1d", .8f)).rectTransform;
            tabBar.anchorMin = new Vector2(0f, 1f); tabBar.anchorMax = Vector2.one; tabBar.pivot = new Vector2(.5f, 1f);
            tabBar.sizeDelta = new Vector2(0f, 54f); tabBar.anchoredPosition = new Vector2(0f, -76f);
            float tx = 20f;
            var icons = new[] { "sports_esports", "visibility", "volume_up", "accessibility_new", "badge" };
            for (int i = 0; i < TabIds.Length; i++)
            {
                string id = TabIds[i];
                string caption = id == "ACCOUNT / SUPPORT" ? "ACCOUNT & SUPPORT" : id;
                var label = UiDraw.Text(tabBar, "Caption", caption, UiFont.Heading, 18f, OnVariant, .01f);
                float w = 20f + 18f + 6f + UiDraw.Width(label) + 20f;
                var tab = UiDraw.Tap(tabBar, "SettingsTab_" + id, new Color(0f, 0f, 0f, 0f), 8f, () => { Tap(); SelectSection(id); });
                UiDraw.Place((RectTransform)tab.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(tx, 0f), new Vector2(w, 50f));
                var glyph = UiDraw.Icon(tab.transform, "Icon", icons[i], 18f, OnVariant);
                UiDraw.Place(glyph.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(20f, 0f), new Vector2(18f, 18f));
                label.transform.SetParent(tab.transform, false);
                UiDraw.Place(label.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(44f, 0f), new Vector2(UiDraw.Width(label) + 2f, 26f));
                tabs[id] = (tab.GetComponent<Image>(), label, glyph);
                tx += w + 4f;
            }

            // Scrolling content between the tab bar and the footer.
            var viewport = UiDraw.Node(modal, "Nav_Scroll");
            UiDraw.Fill(viewport, 0f, 130f, 0f, 72f);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            scrollContent = UiDraw.Node(viewport, "Content");
            scrollContent.anchorMin = scrollContent.anchorMax = new Vector2(0f, 1f); scrollContent.pivot = new Vector2(0f, 1f);
            scrollContent.sizeDelta = new Vector2(width, 600f);
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = scrollContent; scroll.viewport = viewport; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            BuildFooter(width);
            BuildSections(width);
            SelectSection("CONTROLS");
        }

        void BuildFooter(float width)
        {
            var footer = UiDraw.Box(modal, "PauseActions", UiDraw.Hex("#222a3a", .8f), 12f).rectTransform;
            footer.anchorMin = Vector2.zero; footer.anchorMax = new Vector2(1f, 0f); footer.pivot = new Vector2(.5f, 0f);
            footer.sizeDelta = new Vector2(0f, 72f); footer.anchoredPosition = Vector2.zero;

            var home = UiDraw.Tap(footer, "RETURN HOME", Lowest, 8f, () => { Tap(); Close(); game.ReturnToHome(); });
            var homeText = UiDraw.Text(home.transform, "Label", "RETURN HOME", UiFont.Heading, 18f, SecondaryDim, .01f);
            float hw = 16f + 18f + 8f + UiDraw.Width(homeText) + 16f;
            UiDraw.Place((RectTransform)home.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(20f, 0f), new Vector2(hw, 48f));
            UiDraw.Place(UiDraw.Icon(home.transform, "Icon", "logout", 18f, SecondaryDim).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(18f, 18f));
            UiDraw.Place(homeText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(42f, 0f), new Vector2(UiDraw.Width(homeText) + 2f, 24f));
            homeButton = home.gameObject;
            homeWidth = hw;

            footerNote = UiDraw.Text(footer, "Nav_Status", "", UiFont.Body, 12f, OnVariant, .01f);
            footerNote.textWrappingMode = TextWrappingModes.Normal;
            UiDraw.Place(footerNote.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(20f + hw + 12f, 0f), new Vector2(380f, 36f));

            var resume = PrimaryButton(footer, "RESUME", "play_arrow", "RETURN TO MATCH", () => { Tap(); Close(); });
            resumeButton = resume.gameObject;
            var back = PrimaryButton(footer, "Nav_Back", "check", "DONE", () => { Tap(); Close(); });
            backButton = back.gameObject;
        }

        Button PrimaryButton(RectTransform footer, string name, string icon, string caption, System.Action action)
        {
            var button = UiDraw.Tap(footer, name, UiDraw.Hex("#30d5e8"), 8f, action);
            var label = UiDraw.Text(button.transform, "Label", caption, UiFont.Heading, 20f, OnPrimary, .02f);
            float w = 40f + 20f + 12f + UiDraw.Width(label) + 40f;
            UiDraw.Place((RectTransform)button.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-20f, 0f), new Vector2(w, 48f));
            UiDraw.Place(UiDraw.Icon(button.transform, "Icon", icon, 20f, OnPrimary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(40f, 0f), new Vector2(20f, 20f));
            UiDraw.Place(label.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(72f, 0f), new Vector2(UiDraw.Width(label) + 2f, 26f));
            return button;
        }

        // ------------------------------------------------------------ sections

        float colLeftX, colLeftW, colRightX, colRightW;

        void BuildSections(float width)
        {
            float inner = width - 40f, unit = (inner - 11f * 20f) / 12f;
            colLeftX = 20f; colLeftW = unit * 7f + 20f * 6f;
            colRightX = 20f + colLeftW + 20f; colRightW = unit * 5f + 20f * 4f;
            var input = game.touchInput;

            // CONTROLS
            var s = Section("CONTROLS");
            float ly = 20f, ry = 20f;
            ly = Segmented(s, colLeftX, ly, colLeftW, "CONTROL LAYOUT", "Thumb cluster side for landscape touch", "INPUT",
                "RIGHT-HANDED", "LEFT-HANDED", "LeftHandedMode", input.SetLeftHanded) + 12f;
            ly = SliderCard(s, colLeftX, ly, colLeftW, "LOOK SENSITIVITY", "Free rotation camera yaw & pitch rate", "AimSensitivity",
                .5f, 2.5f, 1f, v => $"{v:0.00}", "x", "LOW ACCURACY", "STANDARD 1.00x", "RAPID SNAP", input.SetAimSensitivity) + 12f;
            if (TouchInputManager.GyroAvailable)
                ly = SliderCard(s, colLeftX, ly, colLeftW, "GYRO SENSITIVITY", "Device-tilt aim rate when gyro aim is on", "GyroSensitivity",
                    .5f, 2f, 1f, v => $"{v:0.00}", "x", "STABILITY", "STANDARD 1.00x", "DYNAMIC TRACK", _ => { }) + 12f;
            ly = GyroCard(s, colLeftX, ly, colLeftW, input) + 12f;
            var toggles = Card(s, "Row_Toggles", colRightX, ry, colRightW, 10f);
            float ty = 12f;
            ty = ToggleRow(toggles, colRightW, ty, "INVERT Y-AXIS", "Aviation-style stick pull", "InvertY", false, input.SetInvertY, false);
            ty = ToggleRow(toggles, colRightW, ty, "VIBRATION", "Haptic pulse on hits and launches", "VibrationEnabled", true, input.SetVibration, true);
            ty = ToggleRow(toggles, colRightW, ty, "AIM ASSIST", "Subtle friction slowdown (no snap)", "AimAssistEnabled", true, value =>
            {
                foreach (var player in Hotdrop.Player.PlayerController.All) player.SetAimAssistEnabled(value);
            }, true);
            toggles.sizeDelta = new Vector2(colRightW, ty + 12f);
            ry += ty + 12f + 12f;
            ry = WorkspaceCard(s, colRightX, ry, colRightW, input) + 12f;
            s.sizeDelta = new Vector2(width, Mathf.Max(ly, ry) + 8f);

            // GRAPHICS
            s = Section("GRAPHICS");
            ly = QualityCard(s, colLeftX, 20f, colLeftW) + 12f;
            var gfx = Card(s, "Row_GraphicsReset", colRightX, 20f, colRightW, 10f);
            float gy = Paragraph(gfx, colRightW, 12f, "RENDER PROFILE", "Presets change frame-rate target and render scale for real. Measure on device; there is no automatic fallback.");
            gy = ActionButton(gfx, colRightW, gy + 8f, "RESET GRAPHICS", "restart_alt", ResetGraphics);
            gfx.sizeDelta = new Vector2(colRightW, gy + 12f);
            s.sizeDelta = new Vector2(width, Mathf.Max(ly, 20f + gy + 12f) + 8f);

            // AUDIO
            s = Section("AUDIO");
            ly = 20f;
            ly = VolumeCard(s, colLeftX, ly, colLeftW, "MASTER VOLUME", "Everything the game plays", GameAudio.MasterKey) + 12f;
            ly = VolumeCard(s, colLeftX, ly, colLeftW, "SFX VOLUME", "Weapons, pads and impacts", GameAudio.SfxKey) + 12f;
            ry = 20f;
            ry = VolumeCard(s, colRightX, ry, colRightW, "MUSIC VOLUME", "Menus and match score", GameAudio.MusicKey) + 12f;
            ry = VolumeCard(s, colRightX, ry, colRightW, "ANNOUNCER VOLUME", "Zone calls and results", GameAudio.AnnouncerKey) + 12f;
            var audioReset = Card(s, "Row_AudioReset", colRightX, ry, colRightW, 10f);
            float ay = ActionButton(audioReset, colRightW, 12f, "RESET AUDIO", "restart_alt", ResetAudio);
            audioReset.sizeDelta = new Vector2(colRightW, ay + 12f);
            ry += ay + 24f;
            s.sizeDelta = new Vector2(width, Mathf.Max(ly, ry) + 8f);

            // ACCESSIBILITY
            s = Section("ACCESSIBILITY");
            var access = Card(s, "Row_Access", colLeftX, 20f, colLeftW, 10f);
            float acy = 12f;
            acy = ToggleRow(access, colLeftW, acy, "CAMERA SHAKE", "Recoil and impact camera motion", "CameraShakeEnabled", true, value => game.mainCamera.SetShakeEnabled(value), false);
            acy = ToggleRow(access, colLeftW, acy, "LARGER TEXT / HUD", "Raises every label without moving controls", LargerTextKey, false, ApplyLargerText, true);
            acy = ToggleRow(access, colLeftW, acy, "MUTE TEAM PINGS", "Silence teammate ping sounds", Hotdrop.UI.PingSystem.MuteKey, false, _ => { }, true);
            access.sizeDelta = new Vector2(colLeftW, acy + 12f);
            s.sizeDelta = new Vector2(width, 20f + acy + 12f + 8f);

            // ACCOUNT / SUPPORT
            s = Section("ACCOUNT / SUPPORT");
            var acct = Card(s, "Row_Account", colLeftX, 20f, colLeftW, 10f);
            float py = Paragraph(acct, colLeftW, 12f, "ACCOUNT & SUPPORT", "Profile, linked account, support reports and deletion live on Home under More. Account links are available from Home, not during a match.");
            foreach (string page in new[] { "Account", "Support" })
            {
                string destination = page;
                py = ActionButton(acct, colLeftW, py + 8f, destination.ToUpperInvariant(), destination == "Account" ? "badge" : "support_agent", () =>
                {
                    Close();
                    PlayerProductScreens.Open(game, GameObject.Find("Home"), destination);
                }, "SettingsLink_" + destination);
            }
            acct.sizeDelta = new Vector2(colLeftW, py + 12f);
            s.sizeDelta = new Vector2(width, 20f + py + 12f + 8f);
        }

        RectTransform Section(string name)
        {
            var node = UiDraw.Node(scrollContent, "Row_Section_" + name);
            node.anchorMin = node.anchorMax = new Vector2(0f, 1f); node.pivot = new Vector2(0f, 1f);
            node.anchoredPosition = Vector2.zero;
            sections[name] = node;
            return node;
        }

        static RectTransform Card(RectTransform parent, string name, float x, float y, float w, float h)
        {
            var card = UiDraw.Box(parent, name, High, 8f);
            UiDraw.Place(card.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(w, h));
            return card.rectTransform;
        }

        static void TL(RectTransform rt, float x, float y, float w, float h) =>
            UiDraw.Place(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(w, h));

        static void Heading(RectTransform card, float w, float y, string title, string sub, UiFont font = UiFont.BodySemiBold, float size = 18f)
        {
            TL(UiDraw.Text(card, "Title", title, font, size, OnSurface, .01f).rectTransform, 16f, y, w - 140f, 24f);
            TL(UiDraw.Text(card, "Sub", sub, UiFont.Body, 12f, OnVariant, .01f).rectTransform, 16f, y + 26f, w - 140f, 16f);
        }

        float Segmented(RectTransform parent, float x, float y, float w, string title, string sub, string tag,
            string offCaption, string onCaption, string key, System.Action<bool> apply)
        {
            var card = Card(parent, "Row_" + key, x, y, w, 128f);
            Heading(card, w, 16f, title, sub);
            var tagText = UiDraw.Text(card, "Tag", tag, UiFont.Heading, 12f, Primary, .08f, TextAlignmentOptions.Midline);
            float tw = UiDraw.Width(tagText) + 16f;
            var tagPlate = UiDraw.Box(card, "TagPlate", Lowest, 4f);
            tagPlate.transform.SetSiblingIndex(tagText.transform.GetSiblingIndex());
            UiDraw.Place(tagPlate.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -32f), new Vector2(tw, 20f));
            UiDraw.Place(tagText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -32f), new Vector2(tw, 20f));
            float bw = (w - 32f - 8f) / 2f;
            var parts = new (Image plate, TextMeshProUGUI label, TextMeshProUGUI icon)[2];
            for (int i = 0; i < 2; i++)
            {
                bool value = i == 1;
                var b = UiDraw.Tap(card, "Setting_" + (i == 0 ? offCaption : onCaption), Lowest, 8f, () =>
                {
                    Tap(); PlayerPrefs.SetInt(key, value ? 1 : 0); PlayerPrefs.Save(); apply(value); RefreshAll();
                });
                TL((RectTransform)b.transform, 16f + i * (bw + 8f), 64f, bw, 48f);
                var label = UiDraw.Text(b.transform, "Label", i == 0 ? offCaption : onCaption, UiFont.HeadingSemiBold, 20f, OnVariant, 0f);
                float lw = UiDraw.Width(label);
                var icon = UiDraw.Icon(b.transform, "Icon", "radio_button_unchecked", 20f, OnVariant);
                UiDraw.Place(icon.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-(lw + 8f) / 2f, 0f), new Vector2(20f, 20f));
                UiDraw.Place(label.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(14f, 0f), new Vector2(lw + 2f, 26f));
                parts[i] = (b.GetComponent<Image>(), label, icon);
            }
            refreshers.Add(() =>
            {
                bool on = PlayerPrefs.GetInt(key, 0) == 1;
                for (int i = 0; i < 2; i++)
                {
                    bool active = (i == 1) == on;
                    parts[i].plate.color = active ? Primary : Lowest;
                    parts[i].label.color = active ? OnPrimary : OnVariant;
                    parts[i].icon.color = active ? OnPrimary : OnVariant;
                    parts[i].icon.text = UIIcons.Glyph(active ? "radio_button_checked" : "radio_button_unchecked");
                }
            });
            return y + 128f;
        }

        float SliderCard(RectTransform parent, float x, float y, float w, string title, string sub, string key,
            float min, float max, float defaultValue, System.Func<float, string> format, string unit,
            string left, string mid, string right, System.Action<float> apply)
        {
            var card = Card(parent, "Row_" + key, x, y, w, 152f);
            Heading(card, w, 16f, title, sub, UiFont.BodySemiBold, 18f);
            var plate = UiDraw.Box(card, "ValuePlate", Lowest, 4f);
            UiDraw.Place(plate.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -36f), new Vector2(110f, 40f));
            var value = UiDraw.Text(plate.transform, "Value", "", UiFont.Mono, 22f, Primary, .02f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(value.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-34f, 0f), new Vector2(80f, 26f));
            UiDraw.Place(UiDraw.Text(plate.transform, "Unit", unit, UiFont.MonoMedium, 12f, Outline, .05f).rectTransform,
                new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-12f, -2f), new Vector2(20f, 14f));
            TL(UiDraw.Text(card, "Min", min.ToString("0.0"), UiFont.MonoMedium, 12f, Outline, .05f).rectTransform, 16f, 84f, 30f, 20f);
            var maxText = UiDraw.Text(card, "Max", max.ToString("0.0"), UiFont.MonoMedium, 12f, Outline, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(maxText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -84f), new Vector2(30f, 20f));
            var slider = BuildSlider(card, min, max, PlayerPrefs.GetFloat(key, defaultValue));
            TL((RectTransform)slider.transform, 58f, 80f, w - 116f, 28f);
            TL(UiDraw.Text(card, "Left", left, UiFont.MonoMedium, 11f, OnVariant, .05f).rectTransform, 16f, 118f, 160f, 16f);
            var midText = UiDraw.Text(card, "Mid", mid, UiFont.MonoMedium, 11f, Tertiary, .05f, TextAlignmentOptions.Midline);
            UiDraw.Place(midText.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -118f), new Vector2(240f, 16f));
            var rightText = UiDraw.Text(card, "Right", right, UiFont.MonoMedium, 11f, OnVariant, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(rightText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -118f), new Vector2(160f, 16f));
            refreshers.Add(() =>
            {
                float stored = PlayerPrefs.GetFloat(key, defaultValue);
                slider.SetValueWithoutNotify(stored);
                value.text = format(stored);
            });
            slider.onValueChanged.AddListener(v =>
            {
                PlayerPrefs.SetFloat(key, v); PlayerPrefs.Save();
                value.text = format(v);
                apply(v);
            });
            return y + 152f;
        }

        float VolumeCard(RectTransform parent, float x, float y, float w, string title, string sub, string key) =>
            SliderCard(parent, x, y, w, title, sub, key, 0f, 1f, GameAudio.Volume(key), v => $"{v * 100:0}", "%",
                "MUTED", "", "FULL", v => GameAudio.SetVolume(key, v));

        float GyroCard(RectTransform parent, float x, float y, float w, TouchInputManager input)
        {
            var card = Card(parent, "Row_Gyro", x, y, w, 90f);
            bool available = TouchInputManager.GyroAvailable;
            Heading(card, w, 22f, "GYROSCOPE AIM", available ? "Hardware motion tracking via device sensor" : "Not available on this device (no gyroscope detected)");
            if (available)
            {
                var cal = UiDraw.Tap(card, "CALIBRATE", Lowest, 8f, () => { Tap(); input.CalibrateGyroNeutral(); });
                var calText = UiDraw.Text(cal.transform, "Label", "CALIBRATE", UiFont.Mono, 14f, Primary, .03f);
                float cw = 16f + 16f + 6f + UiDraw.Width(calText) + 16f;
                UiDraw.Place((RectTransform)cal.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-16f - 56f - 16f, 0f), new Vector2(cw, 48f));
                UiDraw.Place(UiDraw.Icon(cal.transform, "Icon", "screen_rotation_alt", 16f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(16f, 16f));
                UiDraw.Place(calText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(38f, 0f), new Vector2(UiDraw.Width(calText) + 2f, 18f));
            }
            var sw = Switch(card, "GyroAim", false, input.SetGyroAim, available);
            UiDraw.Place(sw, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-16f, 0f), new Vector2(56f, 48f));
            return y + 90f;
        }

        float ToggleRow(RectTransform card, float w, float y, string title, string sub, string key, bool defaultValue, System.Action<bool> apply, bool divider)
        {
            if (divider) TL(UiDraw.Box(card, "Divider", Container).rectTransform, 16f, y, w - 32f, 1f);
            float top = y + (divider ? 8f : 0f);
            TL(UiDraw.Text(card, "Title_" + key, title, UiFont.BodySemiBold, 16f, OnSurface, .01f).rectTransform, 16f, top + 8f, w - 100f, 22f);
            TL(UiDraw.Text(card, "Sub_" + key, sub, UiFont.Body, 12f, OnVariant, .01f).rectTransform, 16f, top + 30f, w - 100f, 16f);
            var sw = Switch(card, key, defaultValue, apply, true);
            TL(sw, w - 16f - 56f, top + 4f, 56f, 48f);
            return top + 56f;
        }

        RectTransform Switch(RectTransform parent, string key, bool defaultValue, System.Action<bool> apply, bool enabled)
        {
            var track = UiDraw.Tap(parent, "Setting_" + key, Lowest, 24f, null);
            var thumb = UiDraw.Dot(track.transform, "Thumb", 32f, OutlineVariant);
            var glyph = UiDraw.Icon(thumb.transform, "Icon", "close", 16f, OnVariant);
            glyph.rectTransform.anchoredPosition = Vector2.zero;
            track.interactable = enabled;
            System.Action refresh = () =>
            {
                bool on = enabled && PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
                track.GetComponent<Image>().color = on ? UiDraw.Hex("#7cefff", .2f) : Lowest;
                thumb.color = on ? Primary : OutlineVariant;
                glyph.text = UIIcons.Glyph(on ? "check" : "close");
                glyph.color = on ? OnPrimary : OnVariant;
                UiDraw.Place(thumb.rectTransform, new Vector2(on ? 1f : 0f, .5f), new Vector2(on ? 1f : 0f, .5f), new Vector2(on ? -6f : 6f, 0f), new Vector2(32f, 32f));
                thumb.rectTransform.pivot = new Vector2(on ? 1f : 0f, .5f);
                var group = track.GetComponent<CanvasGroup>();
                if (group == null) group = track.gameObject.AddComponent<CanvasGroup>();
                group.alpha = enabled ? 1f : .45f;
            };
            refreshers.Add(refresh);
            refresh();
            track.onClick.AddListener(() =>
            {
                Tap();
                bool value = PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 1;
                PlayerPrefs.SetInt(key, value ? 1 : 0); PlayerPrefs.Save(); apply(value); refresh();
            });
            return (RectTransform)track.transform;
        }

        float WorkspaceCard(RectTransform parent, float x, float y, float w, TouchInputManager input)
        {
            var card = Card(parent, "Row_Workspace", x, y, w, 10f);
            TL(UiDraw.Text(card, "Title", "HUD WORKSPACE", UiFont.HeadingSemiBold, 20f, OnSurface).rectTransform, 16f, 16f, w - 160f, 26f);
            var tag = UiDraw.Text(card, "Tag", "20:9 SAFE CLUSTERS", UiFont.Heading, 12f, Tertiary, .08f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(tag.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -29f), new Vector2(160f, 14f));
            var body = UiDraw.Text(card, "Body", "Move and resize Fire, Launch, Pad and Ping, and set each control's opacity.", UiFont.Body, 14f, OnVariant);
            body.textWrappingMode = TextWrappingModes.Normal; body.alignment = TextAlignmentOptions.TopLeft;
            float bh = body.GetPreferredValues(body.text, w - 32f, 0f).y;
            TL(body.rectTransform, 16f, 50f, w - 32f, bh);
            float py = 50f + bh + 16f;
            var preview = UiDraw.Box(card, "Preview", Lowest, 8f);
            TL(preview.rectTransform, 16f, py, w - 32f, 120f);
            foreach (var (name, anchor, x2) in new[] { ("L-ARC", 0f, 52f), ("R-FIRE", 1f, -52f) })
            {
                var disc = UiDraw.Dot(preview.transform, name, 80f, UiDraw.Hex("#7cefff", .1f));
                UiDraw.Place(disc.rectTransform, new Vector2(anchor, .5f), new Vector2(.5f, .5f), new Vector2(x2, 0f), new Vector2(80f, 80f));
                UiDraw.Fill(UiDraw.Text(disc.transform, "Label", name == "L-ARC" ? "MOVE" : "FIRE", UiFont.MonoMedium, 12f, Primary, .05f, TextAlignmentOptions.Midline).rectTransform);
            }
            UiDraw.Place(UiDraw.Text(preview.transform, "Centre", "CENTER PASSIVE HUD\n<size=10>NO TOUCH ZONES</size>", UiFont.MonoMedium, 12f, UiDraw.Hex("#859396", .6f), .1f, TextAlignmentOptions.Midline).rectTransform,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(220f, 40f));
            py += 120f + 20f;
            var customize = UiDraw.Tap(card, "CUSTOMIZE CONTROLS", Container, 8f, () =>
            {
                Tap();
                panel.SetActive(false);
                if (scrim != null) scrim.SetActive(false);
                input.BeginLayoutEdit(() =>
                {
                    input.enabled = false;
                    panel.SetActive(true);
                    if (scrim != null) scrim.SetActive(true);
                    panel.transform.SetAsLastSibling();
                });
            });
            TL((RectTransform)customize.transform, 16f, py, w - 32f - 48f - 12f, 60f);
            var cText = UiDraw.Text(customize.transform, "Label", "CUSTOMIZE CONTROLS", UiFont.HeadingSemiBold, 20f, Primary, 0f);
            float ctw = UiDraw.Width(cText);
            UiDraw.Place(UiDraw.Icon(customize.transform, "Icon", "open_with", 20f, Primary).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-(ctw + 8f) / 2f, 0f), new Vector2(20f, 20f));
            UiDraw.Place(cText.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(14f, 0f), new Vector2(ctw + 2f, 26f));
            var reset = UiDraw.Tap(card, "RESET CONTROLS", Lowest, 8f, () => { Tap(); ResetControls(); });
            TL((RectTransform)reset.transform, w - 16f - 48f, py, 48f, 60f);
            UiDraw.Fill(UiDraw.Icon(reset.transform, "Icon", "restart_alt", 20f, OnSurface).rectTransform);
            float h = py + 60f + 16f;
            card.sizeDelta = new Vector2(w, h);
            return y + h;
        }

        float QualityCard(RectTransform parent, float x, float y, float w)
        {
            var card = Card(parent, "Row_Quality", x, y, w, 10f);
            Heading(card, w, 16f, "GRAPHICS PRESET", "Applies immediately: frame-rate target and render scale");
            float bw = (w - 32f - 8f) / 2f;
            var parts = new (Image plate, TextMeshProUGUI label, TextMeshProUGUI note)[2];
            for (int i = 0; i < 2; i++)
            {
                int quality = i;
                var b = UiDraw.Tap(card, "Quality_" + i, Lowest, 8f, () =>
                {
                    Tap(); PlayerPrefs.SetInt(GraphicsQualityKey, quality); PlayerPrefs.Save();
                    GameBootstrap.ApplyGraphicsQuality(quality); RefreshAll();
                });
                TL((RectTransform)b.transform, 16f + i * (bw + 8f), 64f, bw, 72f);
                var label = UiDraw.Text(b.transform, "Label", i == 0 ? "LOW — 30 FPS" : "SMOOTH — 60 FPS", UiFont.HeadingSemiBold, 20f, OnVariant, 0f, TextAlignmentOptions.Midline);
                UiDraw.Place(label.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 10f), new Vector2(bw - 16f, 26f));
                var note = UiDraw.Text(b.transform, "Note", i == 0 ? "Render scale 80% • steady on low-end phones" : "Full render scale where the device sustains it",
                    UiFont.MonoMedium, 11f, OnVariant, .03f, TextAlignmentOptions.Midline);
                UiDraw.Place(note.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -14f), new Vector2(bw - 16f, 16f));
                parts[i] = (b.GetComponent<Image>(), label, note);
            }
            var active = UiDraw.Text(card, "GraphicsNote", "", UiFont.MonoMedium, 11f, Tertiary, .05f);
            TL(active.rectTransform, 16f, 148f, w - 32f, 16f);
            refreshers.Add(() =>
            {
                int q = PlayerPrefs.GetInt(GraphicsQualityKey, 0);
                for (int i = 0; i < 2; i++)
                {
                    bool on = i == q;
                    parts[i].plate.color = on ? Primary : Lowest;
                    parts[i].label.color = on ? OnPrimary : OnVariant;
                    parts[i].note.color = on ? OnPrimary : Outline;
                }
                active.text = q == 0 ? "ACTIVE: LOW — 30 FPS TARGET, 80% RENDER SCALE" : "ACTIVE: SMOOTH — 60 FPS TARGET, FULL RENDER SCALE";
            });
            card.sizeDelta = new Vector2(w, 176f);
            return y + 176f;
        }

        static float Paragraph(RectTransform card, float w, float y, string title, string body)
        {
            TL(UiDraw.Text(card, "Title", title, UiFont.BodySemiBold, 18f, OnSurface, .01f).rectTransform, 16f, y + 4f, w - 32f, 24f);
            var t = UiDraw.Text(card, "Body", body, UiFont.Body, 12f, OnVariant, .01f);
            t.textWrappingMode = TextWrappingModes.Normal; t.alignment = TextAlignmentOptions.TopLeft;
            float h = t.GetPreferredValues(body, w - 32f, 0f).y;
            TL(t.rectTransform, 16f, y + 32f, w - 32f, h);
            return y + 32f + h;
        }

        static float ActionButton(RectTransform card, float w, float y, string caption, string icon, System.Action action, string name = null)
        {
            var b = UiDraw.Tap(card, name ?? caption, Container, 8f, () => { Tap(); action(); });
            TL((RectTransform)b.transform, 16f, y, w - 32f, 48f);
            var label = UiDraw.Text(b.transform, "Label", caption, UiFont.HeadingSemiBold, 16f, Primary, .02f);
            float lw = UiDraw.Width(label);
            UiDraw.Place(UiDraw.Icon(b.transform, "Icon", icon, 18f, Primary).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-(lw + 8f) / 2f, 0f), new Vector2(18f, 18f));
            UiDraw.Place(label.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(13f, 0f), new Vector2(lw + 2f, 22f));
            return y + 48f;
        }

        void SelectSection(string name)
        {
            foreach (var section in sections) section.Value.gameObject.SetActive(section.Key == name);
            foreach (var tab in tabs)
            {
                bool on = tab.Key == name;
                tab.Value.plate.color = on ? Container : new Color(0f, 0f, 0f, 0f);
                tab.Value.label.color = on ? Primary : OnVariant;
                tab.Value.icon.color = on ? Primary : OnVariant;
            }
            if (sections.TryGetValue(name, out var active))
                scrollContent.sizeDelta = new Vector2(scrollContent.sizeDelta.x, active.sizeDelta.y);
            if (scroll != null) { scroll.StopMovement(); scroll.verticalNormalizedPosition = 1; }
            if (sections.TryGetValue("ACCOUNT / SUPPORT", out var links))
                foreach (var button in links.GetComponentsInChildren<Button>(true)) button.interactable = Hotdrop.Player.PlayerController.All.Count == 0;
        }

        static void Tap() => GameAudio.Play2D(GameCue.UiTap);

        static Slider BuildSlider(Transform parent, float min, float max, float value)
        {
            var obj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            obj.transform.SetParent(parent, false);
            var track = UiDraw.Box(obj.transform, "Background", Lowest, 5f);
            var trt = track.rectTransform;
            trt.anchorMin = new Vector2(0f, .5f); trt.anchorMax = new Vector2(1f, .5f); trt.sizeDelta = new Vector2(0f, 10f);
            var fillArea = UiDraw.Node(obj.transform, "Fill Area");
            fillArea.anchorMin = new Vector2(0f, .5f); fillArea.anchorMax = new Vector2(1f, .5f); fillArea.sizeDelta = new Vector2(0f, 10f);
            var fill = UiDraw.Box(fillArea, "Fill", UiDraw.Hex("#7cefff", .35f), 5f);
            fill.rectTransform.sizeDelta = Vector2.zero;
            var handleArea = UiDraw.Node(obj.transform, "Handle Slide Area");
            UiDraw.Fill(handleArea, 10f, 0f, 10f, 0f);
            var handle = UiDraw.Dot(handleArea, "Handle", 20f, Primary);
            handle.raycastTarget = true;
            handle.rectTransform.sizeDelta = new Vector2(20f, 0f);
            var slider = obj.GetComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min; slider.maxValue = max; slider.wholeNumbers = false;
            slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.value = value;
            // The hit area is the whole row, not the 10 px track.
            obj.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            return slider;
        }

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

        void ResetGraphics()
        {
            PlayerPrefs.DeleteKey(GraphicsQualityKey); PlayerPrefs.DeleteKey(LargerTextKey);
            PlayerPrefs.Save();
            GameBootstrap.ApplyGraphicsQuality(0);
            ApplyLargerText(false);
            RefreshAll();
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
    }
}
