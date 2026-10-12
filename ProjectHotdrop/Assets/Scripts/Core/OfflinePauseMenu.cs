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
    /// setting saved on this device; focus aim is a hold-to-zoom camera control and never changes
    /// authoritative weapon accuracy, damage, or hit geometry.
    /// </summary>
    public class OfflinePauseMenu : MonoBehaviour
    {
        static string L(string key, string fallback) => UiDraw.L("settings." + key, fallback);
        static string F(string key, string fallback, params object[] args) => UiDraw.LFormat("settings." + key, fallback, args);
        static string Copy(string fallback) => UiDraw.L("settings.copy." + CopyKey(fallback), fallback);

        static string CopyKey(string value)
        {
            var key = new System.Text.StringBuilder();
            bool separator = false;
            foreach (char c in value ?? string.Empty)
            {
                if (char.IsLetterOrDigit(c))
                {
                    if (separator && key.Length > 0) key.Append('.');
                    key.Append(char.ToLowerInvariant(c));
                    separator = false;
                }
                else separator = true;
            }
            return key.ToString();
        }

        const float CaptionDp = UIDesignTokens.CaptionDp;
        const float UiDp = UIDesignTokens.BodyLargeDp;
        const float TitleDp = UIDesignTokens.TitleMediumDp;
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
        ModalView resetConfirmation;
        System.Action<bool> pendingReset;
        TextMeshProUGUI savedStatus;
        RectTransform savedChip;
        string savedStatusDefault;
        float savedStatusUntil;
        bool gyroCalibrationWasActive;
        RectTransform settingsTabContent;
        ScrollRect scroll;
        float configuredWidth;
        Vector3 configuredTabScreen;
        float configuredTabTextScale;
        string selectedSection = "CONTROLS";

        public const string GraphicsQualityKey = "GraphicsQuality";   // 0 = Performance, 1 = Balanced, 2 = Cinematic
        public const string LargerTextKey = "LargerText";
        public const string CockpitModeKey = "hotdrop.cockpitMode";
        public const string NorthUpRadarKey = "hotdrop.radarNorthUp";
        public const string HighContrastObjectiveMarkerKey = "hotdrop.highContrastObjectiveMarker";

        static Color Primary => UIDesignTokens.PrimaryAction;
        static Color OnPrimary => UIDesignTokens.TextOn(Primary);
        static Color Container => UIDesignTokens.Background;
        static Color High => UIDesignTokens.SurfacePanel;
        static Color Highest => UIDesignTokens.SurfaceSelected;
        static Color Lowest => UIDesignTokens.Background;
        static Color OnSurface => UIDesignTokens.TextPrimary;
        static Color OnVariant => UIDesignTokens.TextSecondary;
        static Color Outline => UIDesignTokens.BorderColor();
        static Color OutlineVariant => UIDesignTokens.WithAlpha(UIDesignTokens.SurfaceStructural, .45f);
        static Color Tertiary => UIDesignTokens.ObjectiveGold;
        static Color Warning => UIDesignTokens.SignalWarning;

        static readonly string[] TabIds = { "CONTROLS", "GRAPHICS", "AUDIO", "ACCESSIBILITY", "ACCOUNT / SUPPORT" };

        public bool IsOpen => panel != null && panel.activeSelf;

        void Awake() { game = GetComponent<GameBootstrap>(); }

        void LateUpdate()
        {
            if (IsOpen) RefreshForComposedFrame();
            if (game != null && game.touchInput != null)
            {
                bool calibrating = game.touchInput.IsGyroCalibrating;
                if (gyroCalibrationWasActive && !calibrating) ShowSavedConfirmation();
                gyroCalibrationWasActive = calibrating;
            }
            if (savedStatus != null && savedStatusUntil > 0f && Time.unscaledTime >= savedStatusUntil)
            {
                SetSavedStatusText(savedStatusDefault);
                savedStatusUntil = 0f;
            }
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
            footerNote.text = online ? L("status.matchActive", "Match is active. Leaving returns you to Home.")
                : asPause ? L("status.practicePaused", "Practice is paused. Your run resumes where you left it.")
                : L("status.settingsSaved", "Settings save on this device and apply offline and online.");
            title.text = asPause ? L("title.practicePaused", "Practice Paused") : online ? L("title.matchMenu", "Match Menu") : L("title.settings", "Settings");
            // The chip follows the title; the note starts where Return Home ends (or at the edge).
            float chipX = 50f + UiDraw.Width(title) + 12f;
            overlayChip.anchoredPosition = new Vector2(chipX, overlayChip.anchoredPosition.y);
            overlayChipPlate.anchoredPosition = new Vector2(chipX, overlayChipPlate.anchoredPosition.y);
            footerNote.rectTransform.anchoredPosition = new Vector2(inMatch ? 20f + homeWidth + 12f : 20f, 0f);
            subtitle.text = online ? L("subtitle.onlineMatch", "Online match continues in background. You remain vulnerable to fire.")
                : L("status.settingsSaved", "Settings save on this device and apply offline and online.");
            subtitle.color = online ? Warning : OnVariant;
            var warn = subtitle.transform.parent.Find("WarnIcon");
            if (warn != null) warn.gameObject.SetActive(online);
            if (scrim != null)
            {
                scrim.SetActive(true);
                scrim.GetComponent<Image>().color = inMatch
                    ? UIDesignTokens.WithAlpha(UIDesignTokens.Background, .76f) : UIDesignTokens.SurfaceScrim;
                scrim.transform.SetAsLastSibling();
            }
            RefreshAll();
            panel.SetActive(true);
            Hotdrop.UI.UiScreenEnterMotion.Reopen(panel);
            panel.transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (!IsOpen) return;
            var closingPanel = panel;
            Hotdrop.UI.UiScreenEnterMotion.Exit(closingPanel, () =>
            {
                if (closingPanel != null) closingPanel.SetActive(false);
            });
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
            configuredWidth = width;
            var card = UiDraw.Box(root, "Console", UiDraw.Hex("#18202f", .95f), 12f);
            modal = card.rectTransform;
            modal.anchorMin = new Vector2(.5f, 0f); modal.anchorMax = new Vector2(.5f, 1f); modal.pivot = new Vector2(.5f, .5f);
            modal.sizeDelta = new Vector2(width, -16f); modal.anchoredPosition = Vector2.zero;

            // Header: tune glyph, title, local-overlay chip, live note; saved-locally chip right.
            var header = UiDraw.Box(modal, "Nav_Bar", UiDraw.Hex("#222a3a", .8f), 12f).rectTransform;
            header.anchorMin = new Vector2(0f, 1f); header.anchorMax = Vector2.one; header.pivot = new Vector2(.5f, 1f);
            header.sizeDelta = new Vector2(0f, 76f); header.anchoredPosition = Vector2.zero;
            UiDraw.Place(UiDraw.Icon(header, "Tune", "tune", 22f, Primary).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(20f, -29f), new Vector2(22f, 22f));
            title = UiDraw.Text(header, "Nav_Title", L("title.settings", "Settings"), UiFont.Heading, TitleDp, OnSurface, .025f);
            UiDraw.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(50f, -29f), new Vector2(320f, 34f));
            var chip = UiDraw.Text(header, "OverlayChip", L("header.deviceChip", "SETTINGS  ·  THIS DEVICE"), UiFont.MonoMedium, CaptionDp, Primary, .05f, TextAlignmentOptions.Midline);
            float cw = UiDraw.Width(chip) + 16f;
            var chipPlate = UiDraw.Box(header, "OverlayChipPlate", Highest, 4f);
            chipPlate.transform.SetSiblingIndex(chip.transform.GetSiblingIndex());
            UiDraw.Place(chipPlate.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(312f, -29f), new Vector2(cw, 20f));
            UiDraw.Place(chip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(312f, -29f), new Vector2(cw, 20f));
            overlayChip = chip.rectTransform; overlayChipPlate = chipPlate.rectTransform;
            UiDraw.Place(UiDraw.Icon(header, "WarnIcon", "warning", 14f, Warning).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(20f, -56f), new Vector2(14f, 14f));
            subtitle = UiDraw.Text(header, "Subtitle", "", UiFont.Body, CaptionDp, OnVariant, .01f);
            UiDraw.Place(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(38f, -56f), new Vector2(600f, 16f));
            var saved = UiDraw.Box(header, "SavedChip", Lowest, 8f);
            savedChip = saved.rectTransform;
            savedStatusDefault = L("header.savedLocally", "All Settings Saved Locally");
            savedStatus = UiDraw.Text(saved.transform, "Text", savedStatusDefault, UiFont.MonoMedium, CaptionDp, Primary, .05f);
            var savedText = savedStatus;
            float sw = 12f + 16f + 4f + UiDraw.Width(savedText) + 12f;
            UiDraw.Place(saved.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-20f, 0f), new Vector2(sw, 30f));
            UiDraw.Place(UiDraw.Icon(saved.transform, "Icon", "check_circle", 16f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(16f, 16f));
            UiDraw.Place(savedText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(32f, 0f), new Vector2(UiDraw.Width(savedText) + 2f, 14f));

            // Tab bar.
            var tabBar = UiDraw.Box(modal, "SettingsTabs", UIDesignTokens.WithAlpha(UIDesignTokens.Background, .8f)).rectTransform;
            tabBar.anchorMin = new Vector2(0f, 1f); tabBar.anchorMax = Vector2.one; tabBar.pivot = new Vector2(.5f, 1f);
            // Tabs scroll horizontally when translated or enlarged labels need more room.
            tabBar.sizeDelta = new Vector2(0f, TabBarHeight); tabBar.anchoredPosition = new Vector2(0f, -76f);
            var tabViewport = UiDraw.Node(tabBar, "Viewport");
            UiDraw.Fill(tabViewport, 20f, 0f, 20f, 0f);
            tabViewport.gameObject.AddComponent<RectMask2D>();
            tabViewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            settingsTabContent = UiDraw.Node(tabViewport, "Content");
            settingsTabContent.anchorMin = settingsTabContent.anchorMax = new Vector2(0f, .5f);
            settingsTabContent.pivot = new Vector2(0f, .5f);
            var tabScroll = tabViewport.gameObject.AddComponent<ScrollRect>();
            tabScroll.content = settingsTabContent;
            tabScroll.viewport = tabViewport;
            tabScroll.horizontal = true;
            tabScroll.vertical = false;
            tabScroll.movementType = ScrollRect.MovementType.Clamped;
            tabScroll.inertia = false;
            tabScroll.horizontalNormalizedPosition = 0f;
            RebuildSettingsTabs();

            // Scrolling content between the tab bar and the footer.
            var viewport = UiDraw.Node(modal, "Nav_Scroll");
            UiDraw.Fill(viewport, 0f, 76f + TabBarHeight, 0f, FooterHeight);
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
            CreateResetConfirmation();
            panel.AddComponent<Hotdrop.UI.UiScreenEnterMotion>();
        }

        /// <summary>Reflow the console when its canvas size changes, including synchronous
        /// evidence captures where Unity has no intervening LateUpdate.</summary>
        public void RefreshForComposedFrame()
        {
            if (modal == null || scrollContent == null || game.matchHUD == null) return;
            ApplyBarHeights();
            var tabScreen = UiMetrics.CurrentScreen;
            if (tabScreen != configuredTabScreen || !Mathf.Approximately(UiMetrics.TextScale, configuredTabTextScale))
                RebuildSettingsTabs();
            float canvasW = game.matchHUD.GetComponent<RectTransform>().rect.width;
            if (canvasW <= 0f) return;
            float width = Mathf.Min(1152f, canvasW - 32f);
            if (Mathf.Abs(width - configuredWidth) < 2f) return;
            configuredWidth = width;
            modal.sizeDelta = new Vector2(width, -16f);
            foreach (Transform child in scrollContent)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            sections.Clear();
            refreshers.Clear();
            scrollContent.sizeDelta = new Vector2(width, scrollContent.sizeDelta.y);
            BuildSections(width);
            SelectSection(selectedSection);
            RefreshAll();
        }

        // 72 px design bar around 48 px actions; grows with the 48 dp floor on dense phones.
        static float FooterHeight => UiMetrics.BarHeight(72f, 48f);
        static float TabBarHeight => UiMetrics.BarHeight(54f, 50f);

        void RebuildSettingsTabs()
        {
            if (settingsTabContent == null) return;
            foreach (Transform child in settingsTabContent)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            tabs.Clear();
            float x = 0f;
            float height = Mathf.Max(50f, UiMetrics.MinTouchUnits);
            var icons = new[] { "sports_esports", "visibility", "volume_up", "accessibility_new", "badge" };
            for (int i = 0; i < TabIds.Length; i++)
            {
                string id = TabIds[i];
                string caption = id switch
                {
                    "CONTROLS" => L("tab.controls", "Controls"),
                    "GRAPHICS" => L("tab.graphics", "Graphics"),
                    "AUDIO" => L("tab.audio", "Audio"),
                    "ACCESSIBILITY" => L("tab.accessibility", "Accessibility"),
                    "ACCOUNT / SUPPORT" => L("tab.accountSupport", "Account & Support"),
                    _ => id
                };
                var tab = UiDraw.Tap(settingsTabContent, "SettingsTab_" + id, new Color(0f, 0f, 0f, 0f), 8f,
                    () => { Tap(); SelectSection(id); });
            var label = UiDraw.Text(tab.transform, "Caption", caption, UiFont.Heading, UiDp, OnVariant, .01f);
                // Ellipsis overflow is for wrapped/multi-line text; this label never wraps (NoWrap),
                // so there is nothing for it to truncate and it was left over from a copy-paste.
                // The box below is already sized to the measured text plus a comfortable margin,
                // so letting it overflow (the family default) is also the safer fallback if a
                // translation ever runs long, rather than a mode that does nothing for this case.
                float labelWidth = UiDraw.Width(label);
                float labelBoxWidth = labelWidth + 12f;
                float width = Mathf.Max(UiMetrics.MinTouchUnits, 38f + labelBoxWidth + 20f);
                UiDraw.Place((RectTransform)tab.transform, new Vector2(0f, .5f), new Vector2(0f, .5f),
                    new Vector2(x, 0f), new Vector2(width, height));
                var glyph = UiDraw.Icon(tab.transform, "Icon", icons[i], 18f, OnVariant);
                UiDraw.Place(glyph.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f),
                    new Vector2(12f, 0f), new Vector2(18f, 18f));
                UiDraw.Place(label.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f),
                    new Vector2(38f, 0f), new Vector2(labelBoxWidth, 26f));
                tabs[id] = (tab.GetComponent<Image>(), label, glyph);
                x += width + 4f;
            }
            settingsTabContent.sizeDelta = new Vector2(Mathf.Max(0f, x - 4f), height);
            var tabScroll = settingsTabContent.GetComponentInParent<ScrollRect>();
            if (tabScroll != null) tabScroll.horizontalNormalizedPosition = 0f;
            configuredTabScreen = UiMetrics.CurrentScreen;
            configuredTabTextScale = UiMetrics.TextScale;
            RefreshTabAppearance();
        }

        /// <summary>The console is built once; re-fit its bars when the display density changes.</summary>
        void ApplyBarHeights()
        {
            if (modal.Find("PauseActions") is RectTransform footer && Mathf.Abs(footer.sizeDelta.y - FooterHeight) > .5f)
                footer.sizeDelta = new Vector2(footer.sizeDelta.x, FooterHeight);
            if (modal.Find("SettingsTabs") is RectTransform tabs && Mathf.Abs(tabs.sizeDelta.y - TabBarHeight) > .5f)
                tabs.sizeDelta = new Vector2(tabs.sizeDelta.x, TabBarHeight);
            if (modal.Find("Nav_Scroll") is RectTransform viewport)
            {
                viewport.offsetMin = new Vector2(viewport.offsetMin.x, FooterHeight);
                viewport.offsetMax = new Vector2(viewport.offsetMax.x, -(76f + TabBarHeight));
            }
        }

        void BuildFooter(float width)
        {
            var footer = UiDraw.Box(modal, "PauseActions", UIDesignTokens.WithAlpha(UIDesignTokens.SurfacePanel, .8f), 12f).rectTransform;
            footer.anchorMin = Vector2.zero; footer.anchorMax = new Vector2(1f, 0f); footer.pivot = new Vector2(.5f, 0f);
            footer.sizeDelta = new Vector2(0f, FooterHeight); footer.anchoredPosition = Vector2.zero;

            var home = UiDraw.Tap(footer, "RETURN HOME", Lowest, 8f, () => { Tap(); Close(); game.ReturnToHome(); });
            var homeText = UiDraw.Text(home.transform, "Label", L("action.returnHome", "Return Home"), UiFont.Heading, UiDp, OnVariant, .01f);
            float hw = 16f + 18f + 8f + UiDraw.Width(homeText) + 16f;
            UiDraw.Place((RectTransform)home.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(20f, 0f), new Vector2(hw, 48f));
            UiDraw.Place(UiDraw.Icon(home.transform, "Icon", "logout", 18f, OnVariant).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(18f, 18f));
            UiDraw.Place(homeText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(42f, 0f), new Vector2(UiDraw.Width(homeText) + 2f, 24f));
            homeButton = home.gameObject;
            homeWidth = hw;

            footerNote = UiDraw.Text(footer, "Nav_Status", "", UiFont.Body, CaptionDp, OnVariant, .01f);
            footerNote.textWrappingMode = TextWrappingModes.Normal;
            UiDraw.Place(footerNote.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(20f + hw + 12f, 0f), new Vector2(380f, 36f));

            var resume = PrimaryButton(footer, "RESUME", "play_arrow", L("action.returnMatch", "Return to Match"), () => { Tap(); Close(); });
            resumeButton = resume.gameObject;
            var back = PrimaryButton(footer, "Nav_Back", "check", L("action.done", "Done"), () => { Tap(); Close(); });
            backButton = back.gameObject;
        }

        Button PrimaryButton(RectTransform footer, string name, string icon, string caption, System.Action action)
        {
            var button = UiDraw.Tap(footer, name, UIDesignTokens.PrimaryAction, 8f, action);
            var label = UiDraw.Text(button.transform, "Label", caption, UiFont.Heading, TitleDp, OnPrimary, .02f);
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
            ly = Segmented(s, colLeftX, ly, colLeftW, "FOCUS AIM MODE", "Press and hold, or tap once to latch the zoom", "AIM",
                "HOLD", "TOGGLE", "FocusAimToggleMode", input.SetFocusAimMode) + 12f;
            ly = SliderCard(s, colLeftX, ly, colLeftW, "LOOK SENSITIVITY", "10 steps for free-rotation camera yaw and pitch", "AimSensitivity",
                0.5f, 2.5f, 1f, v => $"{v:0.00}", "x", "LOW", "STANDARD 1.00x", "RAPID", input.SetAimSensitivity, true) + 12f;
            ly = SliderCard(s, colLeftX, ly, colLeftW, "FOCUS AIM SENSITIVITY", "10 steps while holding Focus Aim", "FocusAimSensitivity",
                0.5f, 2.5f, 1f, v => $"{v:0.00}", "x", "LOW", "STANDARD 1.00x", "RAPID", v =>
                {
                    input.SetFocusAimSensitivity(v);
                    Hotdrop.Core.Telemetry.Track("focus_aim_sensitivity_changed", ("scale", v));
                }, true) + 12f;
            if (TouchInputManager.GyroAvailable)
                ly = SliderCard(s, colLeftX, ly, colLeftW, "GYRO SENSITIVITY", "Device-tilt aim rate when gyro aim is on", "GyroSensitivity",
                    TouchInputManager.GyroSensitivityMinimum, TouchInputManager.GyroSensitivityMaximum, 1f,
                    v => $"{v:0.00}", "x", "STABILITY", "STANDARD 1.00x", "DYNAMIC TRACK", input.SetGyroSensitivity) + 12f;
            ly = GyroCard(s, colLeftX, ly, colLeftW, input) + 12f;
            var toggles = Card(s, "Row_Toggles", colRightX, ry, colRightW, 10f);
            float ty = 12f;
            ty = ToggleRow(toggles, colRightW, ty, "INVERT Y-AXIS", "Aviation-style stick pull", "InvertY", false, input.SetInvertY, false);
            ty = ToggleRow(toggles, colRightW, ty, "VIBRATION", "Haptic pulse on hits and launches", "VibrationEnabled", true, input.SetVibration, true);
            var assistModel = UiDraw.Text(toggles, "AimAssistModel", "", UiFont.MonoMedium, CaptionDp, OnVariant, .05f);
            assistModel.textWrappingMode = TextWrappingModes.Normal;
            assistModel.overflowMode = TextOverflowModes.Ellipsis;
            assistModel.enableAutoSizing = true;
            assistModel.fontSizeMin = UiMetrics.MinTextUnits;
            assistModel.fontSizeMax = CaptionDp;
            void RefreshAssistModel(bool enabled) => assistModel.text = enabled
                ? L("aimAssist.onDetails", "ON · 0.70× sensitivity in a 3° / 25 m clear-LOS cone · no snap or tracking")
                : L("aimAssist.offDetails", "OFF · 1.00× sensitivity · assist changes neither aim nor fire automatically");
            ty = ToggleRow(toggles, colRightW, ty, "AIM ASSIST", "Subtle friction slowdown (no snap)", "AimAssistEnabled", true, value =>
            {
                foreach (var player in Hotdrop.Player.PlayerController.All) player.SetAimAssistEnabled(value);
                RefreshAssistModel(value);
            }, true);
            RefreshAssistModel(PlayerPrefs.GetInt("AimAssistEnabled", 1) == 1);
            TL(assistModel.rectTransform, 16f, ty + 2f, colRightW - 32f, 34f);
            ty += 38f;
            refreshers.Add(() => RefreshAssistModel(PlayerPrefs.GetInt("AimAssistEnabled", 1) == 1));
            toggles.sizeDelta = new Vector2(colRightW, ty + 12f);
            ry += ty + 12f + 12f;
            ry = SliderCard(s, colRightX, ry, colRightW, "HAPTIC INTENSITY", "Mobile vibration strength", HapticFeedback.IntensityKey,
                0f, 1f, 0.7f, v => $"{v * 100:0}", "%", "OFF", "LIGHT", "STRONG",
                HapticFeedback.SetIntensity) + 12f;
            ry = WorkspaceCard(s, colRightX, ry, colRightW, input) + 12f;
            s.sizeDelta = new Vector2(width, Mathf.Max(ly, ry) + 8f);

            // GRAPHICS
            s = Section("GRAPHICS");
            ly = QualityCard(s, colLeftX, 20f, colLeftW) + 12f;
            var gfx = Card(s, "Row_GraphicsReset", colRightX, 20f, colRightW, 10f);
            float gy = Paragraph(gfx, colRightW, 12f, "RENDER PROFILE", "Auto detect applies the existing memory-tier recommendation. Measure each preset on device; runtime quality does not auto-fallback.");
            gy = ActionButton(gfx, colRightW, gy + 8f, "AUTO DETECT", "restart_alt", AutoDetectGraphics);
            gfx.sizeDelta = new Vector2(colRightW, gy + 12f);
            s.sizeDelta = new Vector2(width, Mathf.Max(ly, 20f + gy + 12f) + 8f);

            // AUDIO
            s = Section("AUDIO");
            ly = 20f;
            ly = VolumeCard(s, colLeftX, ly, colLeftW, "MASTER VOLUME", "Everything the game plays", GameAudio.MasterKey) + 12f;
            ly = VolumeCard(s, colLeftX, ly, colLeftW, "SFX VOLUME", "Weapons, pads and impacts", GameAudio.SfxKey) + 12f;
            ry = 20f;
            ry = VolumeCard(s, colRightX, ry, colRightW, "MUSIC VOLUME", "Menus and match score", GameAudio.MusicKey) + 12f;
            ry = VolumeCard(s, colRightX, ry, colRightW, "ANNOUNCER VOLUME", "Match direction, zone calls and results", GameAudio.VoiceKey) + 12f;
            var mono = Card(s, "Row_MonoOutput", colRightX, ry, colRightW, 84f);
            float monoY = ToggleRow(mono, colRightW, 12f, "MONO OUTPUT", "Center spatial cues for one speaker", GameAudio.MonoOutputKey, false, GameAudio.SetMonoOutput, false);
            mono.sizeDelta = new Vector2(colRightW, monoY + 12f);
            ry += monoY + 24f;
            var mutePings = Card(s, "Row_MuteTeamPings", colRightX, ry, colRightW, 76f);
            float mutePingsY = ToggleRow(mutePings, colRightW, 12f, "MUTE TEAM PINGS", "Silence teammate ping sounds", Hotdrop.UI.PingSystem.MuteKey, false, _ => { }, true);
            mutePings.sizeDelta = new Vector2(colRightW, mutePingsY + 12f);
            ry += mutePingsY + 24f;
            var audioReset = Card(s, "Row_AudioReset", colRightX, ry, colRightW, 10f);
            float ay = ActionButton(audioReset, colRightW, 12f, "RESET TO DEFAULT", "restart_alt",
                () => ConfirmReset(AudioCustomized(), ResetAudio), "ResetAudioToDefault");
            audioReset.sizeDelta = new Vector2(colRightW, ay + 12f);
            ry += ay + 24f;
            s.sizeDelta = new Vector2(width, Mathf.Max(ly, ry) + 8f);

            // ACCESSIBILITY
            s = Section("ACCESSIBILITY");
            var access = Card(s, "Row_Access", colLeftX, 20f, colLeftW, 10f);
            float acy = 12f;
            acy = ToggleRow(access, colLeftW, acy, "CAMERA SHAKE", "Recoil and impact camera motion", "CameraShakeEnabled", true, value => game.mainCamera.SetShakeEnabled(value), false);
            acy = ToggleRow(access, colLeftW, acy, "REDUCE MOTION", "Reduce camera kick and shake", Hotdrop.UI.UIMotionSettings.ReduceMotionKey, false, value => Hotdrop.UI.UIMotionSettings.ReduceMotion = value, true);
            acy = ToggleRow(access, colLeftW, acy, "LARGER TEXT / HUD", "Enlarge HUD and menu text", LargerTextKey, false, ApplyLargerText, true);
            acy = ToggleRow(access, colLeftW, acy, "GAMEPLAY SOUND CAPTIONS", "Caption nearby and objective sounds", Hotdrop.Core.GameAudio.SoundCaptionsKey, true, _ => { }, true);
            acy = ToggleRow(access, colLeftW, acy, "STREAM-SAFE DISPLAY", "Hide names and progression in captures", Hotdrop.UI.StreamSafePresentation.PreferenceKey, false, Hotdrop.UI.StreamSafePresentation.SetEnabled, true);
            if (Hotdrop.UI.QuickChatWheelView.AvailabilityGateOpen)
                acy = ToggleRow(access, colLeftW, acy, "MUTE TEAM QUICK CHAT", "Hide teammate quick-chat messages", Hotdrop.UI.QuickChatWheelView.MuteKey, false, _ => { }, true);
            acy = ToggleRow(access, colLeftW, acy, "COCKPIT MODE", "Fade radar and roster near combat and endgame", CockpitModeKey, true, _ => { }, true);
            acy = ToggleRow(access, colLeftW, acy, "NORTH-UP MINIMAP", "Keep north at the top of the radar", NorthUpRadarKey, false, _ => { }, true);
            acy = ToggleRow(access, colLeftW, acy, "HIGH-CONTRAST OBJECTIVE MARKER", "Dark backing behind the objective ring", HighContrastObjectiveMarkerKey, false, _ => { }, true);
            acy = ActionButton(access, colLeftW, acy + 8f, "RESET TO DEFAULT", "restart_alt",
                () => ConfirmReset(AccessibilityCustomized(), ResetAccessibility), "ResetAccessibilityToDefault") + 12f;
            access.sizeDelta = new Vector2(colLeftW, acy + 12f);
            BuildPresentationProfileCard(s, colRightX, 20f, colRightW);
            s.sizeDelta = new Vector2(width, 20f + acy + 12f + 8f);

            // ACCOUNT / SUPPORT
            s = Section("ACCOUNT / SUPPORT");
            var acct = Card(s, "Row_Account", colLeftX, 20f, colLeftW, 10f);
            float py = Paragraph(acct, colLeftW, 12f, "ACCOUNT & SUPPORT", "Profile, linked account, support reports and deletion live on Home under More. Account links are available from Home, not during a match.");
            foreach (string page in new[] { "Account", "Support" })
            {
                string destination = page;
                py = ActionButton(acct, colLeftW, py + 8f, destination, destination == "Account" ? "badge" : "support_agent", () =>
                {
                    Close();
                    PlayerProductScreens.Open(game, GameObject.Find("Home"), destination);
                }, "SettingsLink_" + destination);
            }
            acct.sizeDelta = new Vector2(colLeftW, py + 12f);
            s.sizeDelta = new Vector2(width, 20f + py + 12f + 8f);
        }

        void BuildPresentationProfileCard(RectTransform parent, float x, float y, float w)
        {
            const float height = 166f;
            var card = Card(parent, "Row_PresentationProfile", x, y, w, height);
            TL(UiDraw.Text(card, "Title", L("presentation.title", "Presentation Style"), UiFont.BodySemiBold, UiDp, OnSurface).rectTransform,
                16f, 12f, w - 32f, 24f);
            TL(UiDraw.Text(card, "Description", L("presentation.description", "UI and Switchyard style only. Gameplay is unchanged."), UiFont.Body, CaptionDp, OnVariant).rectTransform,
                16f, 38f, w - 32f, 18f);

            var options = new[]
            {
                (UiPresentationProfileId.StylizedFlight, L("presentation.profileA", "A · STYLIZED")),
                (UiPresentationProfileId.GroundedTacticalFlight, L("presentation.profileB", "B · GROUNDED")),
                (UiPresentationProfileId.Hybrid, L("presentation.profileC", "C · HYBRID")),
            };
            var buttons = new Button[options.Length];
            float gap = 8f;
            float buttonWidth = (w - 32f - gap * 2f) / 3f;
            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var button = UiDraw.Tap(card, "Profile_" + option.Item1, Lowest, 8f,
                    () => ChoosePresentationProfile(option.Item1, buttons, options));
                buttons[i] = button;
                TL((RectTransform)button.transform, 16f + i * (buttonWidth + gap), 70f, buttonWidth, 46f);
                UiDraw.Text(button.transform, "Label", option.Item2, UiFont.Heading, CaptionDp,
                    OnSurface, .01f, TextAlignmentOptions.Midline);
            }
            TL(UiDraw.Text(card, "SavedNote", L("presentation.savedNote", "Saved locally. UI and world update immediately."),
                UiFont.Body, CaptionDp, OnVariant).rectTransform, 16f, 124f, w - 32f, 28f);
            RefreshProfileButtons(buttons, options);
        }

        static void RefreshProfileButtons(Button[] buttons, (UiPresentationProfileId, string)[] options)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                var image = buttons[i].GetComponent<Image>();
                if (image != null) image.color = UiPresentationProfile.Current == options[i].Item1
                    ? UIDesignTokens.WithAlpha(UIDesignTokens.PrimaryAction, .24f) : Lowest;
            }
        }

        void ChoosePresentationProfile(UiPresentationProfileId profile, Button[] buttons,
            (UiPresentationProfileId, string)[] options)
        {
            bool reopenAsPause = pausedGame;
            bool changed = UiPresentationProfile.Current != profile;
            if (!changed) return;
            UiPresentationProfile.Select(profile);
            RefreshProfileButtons(buttons, options);
            Close();
            StartCoroutine(ReopenSettingsAfterExit(reopenAsPause));
        }

        System.Collections.IEnumerator ReopenSettingsAfterExit(bool asPause)
        {
            while (panel != null && panel.activeSelf) yield return null;
            Show(asPause);
            if (IsOpen) ShowSavedConfirmation();
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

        static void Heading(RectTransform card, float w, float y, string title, string sub, UiFont font = UiFont.BodySemiBold, float size = UiDp)
        {
            TL(UiDraw.Text(card, "Title", Copy(title), font, size, OnSurface, .01f).rectTransform, 16f, y, w - 140f, 24f);
            TL(UiDraw.Text(card, "Sub", Copy(sub), UiFont.Body, CaptionDp, OnVariant, .01f).rectTransform, 16f, y + 26f, w - 140f, 16f);
        }

        float Segmented(RectTransform parent, float x, float y, float w, string title, string sub, string tag,
            string offCaption, string onCaption, string key, System.Action<bool> apply)
        {
            var card = Card(parent, "Row_" + key, x, y, w, 128f);
            Heading(card, w, 16f, title, sub);
            var tagText = UiDraw.Text(card, "Tag", Copy(tag), UiFont.Heading, CaptionDp, Primary, .08f, TextAlignmentOptions.Midline);
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
                    PlayerPrefs.SetInt(key, value ? 1 : 0); PlayerPrefs.Save(); apply(value); RefreshAll(); ShowSavedConfirmation();
                });
                TL((RectTransform)b.transform, 16f + i * (bw + 8f), 64f, bw, 48f);
                var label = UiDraw.Text(b.transform, "Label", Copy(i == 0 ? offCaption : onCaption), UiFont.HeadingSemiBold, TitleDp, OnVariant, 0f);
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
            string left, string mid, string right, System.Action<float> apply, bool tenStepSensitivity = false)
        {
            var card = Card(parent, "Row_" + key, x, y, w, 152f);
            Heading(card, w, 16f, title, sub, UiFont.BodySemiBold, UiDp);
            var plate = UiDraw.Box(card, "ValuePlate", Lowest, 4f);
            UiDraw.Place(plate.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -36f), new Vector2(110f, 40f));
            var value = UiDraw.Text(plate.transform, "Value", "", UiFont.Mono, TitleDp, Primary, .02f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(value.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-34f, 0f), new Vector2(80f, 26f));
            UiDraw.Place(UiDraw.Text(plate.transform, "Unit", unit, UiFont.MonoMedium, CaptionDp, Outline, .05f).rectTransform,
                new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-12f, -2f), new Vector2(20f, 14f));
            TL(UiDraw.Text(card, "Min", min.ToString("0.0"), UiFont.MonoMedium, CaptionDp, Outline, .05f).rectTransform, 16f, 84f, 30f, 20f);
            var maxText = UiDraw.Text(card, "Max", max.ToString("0.0"), UiFont.MonoMedium, CaptionDp, Outline, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(maxText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -84f), new Vector2(30f, 20f));
            float savedValue = PlayerPrefs.GetFloat(key, defaultValue);
            float sliderMin = tenStepSensitivity ? 0f : min;
            float sliderMax = tenStepSensitivity ? TouchInputManager.AimSensitivityStepCount - 1 : max;
            float sliderValue = tenStepSensitivity
                ? TouchInputManager.AimSensitivityStepFor(savedValue)
                : savedValue;
            var slider = BuildSlider(card, sliderMin, sliderMax, sliderValue, tenStepSensitivity);
            TL((RectTransform)slider.transform, 58f, 80f, w - 116f, 28f);
            TL(UiDraw.Text(card, "Left", Copy(left), UiFont.MonoMedium, CaptionDp, OnVariant, .05f).rectTransform, 16f, 118f, 160f, 16f);
            var midText = UiDraw.Text(card, "Mid", Copy(mid), UiFont.MonoMedium, CaptionDp, Tertiary, .05f, TextAlignmentOptions.Midline);
            UiDraw.Place(midText.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -118f), new Vector2(240f, 16f));
            var rightText = UiDraw.Text(card, "Right", Copy(right), UiFont.MonoMedium, CaptionDp, OnVariant, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(rightText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -118f), new Vector2(160f, 16f));
            refreshers.Add(() =>
            {
                float stored = PlayerPrefs.GetFloat(key, defaultValue);
                float shown = tenStepSensitivity ? TouchInputManager.NormalizeAimSensitivity(stored) : stored;
                slider.SetValueWithoutNotify(tenStepSensitivity
                    ? TouchInputManager.AimSensitivityStepFor(shown)
                    : shown);
                value.text = format(shown);
            });
            slider.onValueChanged.AddListener(v =>
            {
                float actual = tenStepSensitivity
                    ? TouchInputManager.AimSensitivityAtStep(Mathf.RoundToInt(v))
                    : v;
                PlayerPrefs.SetFloat(key, actual); PlayerPrefs.Save();
                value.text = format(actual);
                apply(actual);
                // onValueChanged fires on every drag step; keep sliders silent as before.
                ShowSavedConfirmation(playCue: false);
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
                var calText = UiDraw.Text(cal.transform, "Label", Copy("CALIBRATE"), UiFont.Mono, UiDp, Primary, .03f);
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
            string plainTitle = title.ToLowerInvariant();
            plainTitle = char.ToUpperInvariant(plainTitle[0]) + plainTitle.Substring(1);
            plainTitle = plainTitle.Replace("y-axis", "Y-axis").Replace("hud", "HUD");
            TL(UiDraw.Text(card, "Title_" + key, Copy(plainTitle), UiFont.BodySemiBold, UiDp, OnSurface, .01f).rectTransform, 16f, top + 8f, w - 100f, 22f);
            var description = UiDraw.Text(card, "Sub_" + key, Copy(sub), UiFont.Body, CaptionDp, OnVariant, .01f);
            description.overflowMode = TextOverflowModes.Ellipsis;
            TL(description.rectTransform, 16f, top + 30f, w - 100f, 16f);
            var sw = Switch(card, key, defaultValue, apply, true);
            TL(sw, w - 16f - 56f, top + 4f, 56f, 48f);
            const float minimumRowHeight = 56f;
            return top + Mathf.Max(minimumRowHeight, 48f);
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
                track.GetComponent<Image>().color = on ? UIDesignTokens.WithAlpha(UIDesignTokens.PrimaryAction, .2f) : Lowest;
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
                bool value = PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 1;
                PlayerPrefs.SetInt(key, value ? 1 : 0); PlayerPrefs.Save(); apply(value); refresh(); ShowSavedConfirmation();
            });
            return (RectTransform)track.transform;
        }

        float WorkspaceCard(RectTransform parent, float x, float y, float w, TouchInputManager input)
        {
            var card = Card(parent, "Row_Workspace", x, y, w, 10f);
            TL(UiDraw.Text(card, "Title", Copy("HUD WORKSPACE"), UiFont.HeadingSemiBold, TitleDp, OnSurface).rectTransform, 16f, 16f, w - 160f, 26f);
            var tag = UiDraw.Text(card, "Tag", Copy("20:9 SAFE CLUSTERS"), UiFont.Heading, CaptionDp, Tertiary, .08f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(tag.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -29f), new Vector2(160f, 14f));
            var body = UiDraw.Text(card, "Body", Copy("Move and resize Fire, Launch, Pad and Ping, and set each control's opacity."), UiFont.Body, UiDp, OnVariant);
            body.textWrappingMode = TextWrappingModes.Normal; body.alignment = TextAlignmentOptions.TopLeft;
            float bh = body.GetPreferredValues(body.text, w - 32f, 0f).y;
            TL(body.rectTransform, 16f, 50f, w - 32f, bh);
            float py = 50f + bh + 16f;
            var preview = UiDraw.Box(card, "Preview", Lowest, 8f);
            TL(preview.rectTransform, 16f, py, w - 32f, 120f);
            foreach (var (name, anchor, x2) in new[] { ("L-ARC", 0f, 52f), ("R-FIRE", 1f, -52f) })
            {
                var disc = UiDraw.Dot(preview.transform, name, 80f, UIDesignTokens.WithAlpha(UIDesignTokens.PrimaryAction, .1f));
                UiDraw.Place(disc.rectTransform, new Vector2(anchor, .5f), new Vector2(.5f, .5f), new Vector2(x2, 0f), new Vector2(80f, 80f));
                UiDraw.Fill(UiDraw.Text(disc.transform, "Label", Copy(name == "L-ARC" ? "MOVE" : "FIRE"), UiFont.MonoMedium, CaptionDp, Primary, .05f, TextAlignmentOptions.Midline).rectTransform);
            }
            UiDraw.Place(UiDraw.Text(preview.transform, "Centre", Copy("CENTER PASSIVE HUD\nNO TOUCH ZONES"), UiFont.MonoMedium, CaptionDp, UiDraw.Hex("#bbc6d8", .6f), .1f, TextAlignmentOptions.Midline).rectTransform,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(220f, 40f));
            py += 120f + 20f;
            var customize = UiDraw.Tap(card, "CUSTOMIZE CONTROLS", Container, 8f, () =>
            {
                Tap();
                var closingPanel = panel;
                Hotdrop.UI.UiScreenEnterMotion.Exit(closingPanel, () =>
                {
                    if (closingPanel != null) closingPanel.SetActive(false);
                });
                if (scrim != null) scrim.SetActive(false);
                input.BeginLayoutEdit(() =>
                {
                    input.enabled = false;
                    panel.SetActive(true);
                    Hotdrop.UI.UiScreenEnterMotion.Reopen(panel);
                    if (scrim != null) scrim.SetActive(true);
                    panel.transform.SetAsLastSibling();
                    if (input.LayoutEditCommitted) ShowSavedConfirmation();
                });
            });
            float customizeW = w - 32f;
            TL((RectTransform)customize.transform, 16f, py, customizeW, 60f);
            var cText = UiDraw.Text(customize.transform, "Label", Copy("CUSTOMIZE CONTROLS"), UiFont.HeadingSemiBold, TitleDp, Primary, 0f);
            // Clamp the translated label to this button's own width so its centered icon/text pair
            // cannot overlap neighboring controls.
            const float iconSize = 20f, iconGap = 8f, sidePad = 16f;
            float maxTextWidth = Mathf.Max(0f, customizeW - iconSize - iconGap - sidePad * 2f);
            cText.text = UiDraw.Truncate(cText, cText.text, maxTextWidth);
            float ctw = UiDraw.Width(cText);
            UiDraw.Place(UiDraw.Icon(customize.transform, "Icon", "open_with", iconSize, Primary).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-(ctw + iconGap) / 2f, 0f), new Vector2(iconSize, iconSize));
            UiDraw.Place(cText.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2((iconSize + iconGap) / 2f, 0f), new Vector2(ctw + 2f, 26f));
            float resetY = py + 60f + 8f;
            float endY = ActionButton(card, w, resetY, "RESET TO DEFAULT", "restart_alt",
                () => ConfirmReset(ControlsCustomized(), ResetControls), "ResetControlsToDefault");
            float h = endY + 12f;
            card.sizeDelta = new Vector2(w, h);
            return y + h;
        }

        float QualityCard(RectTransform parent, float x, float y, float w)
        {
            var card = Card(parent, "Row_Quality", x, y, w, 10f);
            Heading(card, w, 16f, "GRAPHICS PRESET", "Choose the frame-rate and render-scale target");
            float bw = (w - 32f - 16f) / 3f;
            var parts = new (Image plate, TextMeshProUGUI label, TextMeshProUGUI note)[3];
            // Player-facing Low/Medium/High names map to the existing
            // Performance/Balanced/Cinematic presets in the same order.
            string[] labels = { Copy("LOW"), Copy("MEDIUM"), Copy("HIGH") };
            var graphicsConfig = game != null ? game.graphicsConfig : null;
            int performanceFps = graphicsConfig != null ? graphicsConfig.performanceTargetFps : Hotdrop.Config.GraphicsConfig.DefaultPerformanceTargetFps;
            int balancedFps = graphicsConfig != null ? graphicsConfig.balancedTargetFps : Hotdrop.Config.GraphicsConfig.DefaultBalancedTargetFps;
            int cinematicFps = graphicsConfig != null ? graphicsConfig.cinematicPresetTargetFps : Hotdrop.Config.GraphicsConfig.DefaultCinematicTargetFps;
            float performanceScale = graphicsConfig != null ? graphicsConfig.performanceRenderScale : Hotdrop.Config.GraphicsConfig.DefaultPerformanceRenderScale;
            float balancedScale = graphicsConfig != null ? graphicsConfig.balancedRenderScale : Hotdrop.Config.GraphicsConfig.DefaultBalancedRenderScale;
            float cinematicScale = graphicsConfig != null ? graphicsConfig.cinematicRenderScale : Hotdrop.Config.GraphicsConfig.DefaultCinematicRenderScale;
            string[] notes =
            {
                F("graphics.targetTemplate", "{0} FPS • {1}% scale", performanceFps, Mathf.RoundToInt(performanceScale * 100f)),
                F("graphics.targetTemplate", "{0} FPS • {1}% scale", balancedFps, Mathf.RoundToInt(balancedScale * 100f)),
                F("graphics.targetTemplate", "{0} FPS • {1}% scale", cinematicFps, Mathf.RoundToInt(cinematicScale * 100f)),
            };
            for (int i = 0; i < parts.Length; i++)
            {
                int quality = i;
                var b = UiDraw.Tap(card, "Quality_" + i, Lowest, 8f, () =>
                {
                    PlayerPrefs.SetInt(GraphicsQualityKey, quality); PlayerPrefs.Save();
                    GameBootstrap.ApplyGraphicsQuality(quality); RefreshAll(); ShowSavedConfirmation();
                });
                TL((RectTransform)b.transform, 16f + i * (bw + 8f), 64f, bw, 72f);
                var label = UiDraw.Text(b.transform, "Label", labels[i], UiFont.HeadingSemiBold, UiDp, OnVariant, 0f, TextAlignmentOptions.Midline);
                UiDraw.Place(label.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 10f), new Vector2(bw - 16f, 24f));
                var note = UiDraw.Text(b.transform, "Note", notes[i],
                    UiFont.MonoMedium, CaptionDp, OnVariant, .03f, TextAlignmentOptions.Midline);
                // Fixed, uncompressed English copy does not fit this button's width on every
                // device/aspect ratio (overflowed past the button edge on a 20:9 phone, and
                // shrinking alone cannot fix it: the accessibility text floor is wider than the
                // single-line space here). Wrap to two lines instead of spilling into neighbours.
                note.textWrappingMode = TextWrappingModes.Normal;
                note.overflowMode = TextOverflowModes.Overflow;
                note.enableAutoSizing = true;
                note.fontSizeMin = UiMetrics.MinTextUnits;
                note.fontSizeMax = CaptionDp;
                UiDraw.Place(note.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -16f), new Vector2(bw - 16f, 28f));
                parts[i] = (b.GetComponent<Image>(), label, note);
            }
            var active = UiDraw.Text(card, "GraphicsNote", "", UiFont.MonoMedium, CaptionDp, Tertiary, .05f);
            TL(active.rectTransform, 16f, 148f, w - 32f, 16f);
            refreshers.Add(() =>
            {
                int q = Mathf.Clamp(PlayerPrefs.GetInt(GraphicsQualityKey, 0), 0, 2);
                for (int i = 0; i < parts.Length; i++)
                {
                    bool on = i == q;
                    parts[i].plate.color = on ? Primary : Lowest;
                    parts[i].label.color = on ? OnPrimary : OnVariant;
                    parts[i].note.color = on ? OnPrimary : Outline;
                }
                active.text = F("graphics.activeTemplate", "ACTIVE: {0} — {1} TARGET", labels[q], notes[q]);
            });

            var shadowPolicy = UiDraw.Text(card, "GraphicsShadowPolicy",
                F("graphics.shadowPolicy", "SHADOWS: {0} OFF · {1} + {2} ON", labels[0], labels[1], labels[2]),
                UiFont.Body, CaptionDp, OnVariant, .01f);
            shadowPolicy.textWrappingMode = TextWrappingModes.Normal;
            shadowPolicy.overflowMode = TextOverflowModes.Overflow;
            shadowPolicy.enableAutoSizing = true;
            shadowPolicy.fontSizeMin = UiMetrics.MinTextUnits;
            shadowPolicy.fontSizeMax = CaptionDp;
            shadowPolicy.alignment = TextAlignmentOptions.TopLeft;
            TL(shadowPolicy.rectTransform, 16f, 170f, w - 32f, 32f);

            // The settings brief calls for a truthful disabled state when a graphics feature is
            // unavailable. Dynamic resolution has no runtime policy or consumer in this build.
            var dynamicRow = UiDraw.Tap(card, "DynamicResolution", Lowest, 8f, null, OutlineVariant);
            dynamicRow.interactable = false;
            TL((RectTransform)dynamicRow.transform, 16f, 206f, w - 32f, 72f);
            var dynamicTitle = UiDraw.Text(dynamicRow.transform, "Label",
                L("graphics.dynamicResolution", "Dynamic Resolution"), UiFont.BodySemiBold,
                CaptionDp, OnVariant, .01f);
            TL(dynamicTitle.rectTransform, 12f, 7f, w - 56f, 20f);
            var dynamicReason = UiDraw.Text(dynamicRow.transform, "UnavailableReason",
                L("graphics.dynamicResolutionUnavailable", "Adaptive scaling is not configured in this build."),
                UiFont.Body, CaptionDp, OnVariant, .01f);
            dynamicReason.textWrappingMode = TextWrappingModes.Normal;
            dynamicReason.overflowMode = TextOverflowModes.Overflow;
            dynamicReason.enableAutoSizing = true;
            dynamicReason.fontSizeMin = UiMetrics.MinTextUnits;
            dynamicReason.fontSizeMax = CaptionDp;
            TL(dynamicReason.rectTransform, 12f, 31f, w - 56f, 34f);

            card.sizeDelta = new Vector2(w, 290f);
            return y + 290f;
        }

        static float Paragraph(RectTransform card, float w, float y, string title, string body)
        {
            TL(UiDraw.Text(card, "Title", Copy(title), UiFont.BodySemiBold, UiDp, OnSurface, .01f).rectTransform, 16f, y + 4f, w - 32f, 24f);
            var t = UiDraw.Text(card, "Body", Copy(body), UiFont.Body, CaptionDp, OnVariant, .01f);
            t.textWrappingMode = TextWrappingModes.Normal; t.alignment = TextAlignmentOptions.TopLeft;
            float h = t.GetPreferredValues(t.text, w - 32f, 0f).y;
            TL(t.rectTransform, 16f, y + 32f, w - 32f, h);
            return y + 32f + h;
        }

        static float ActionButton(RectTransform card, float w, float y, string caption, string icon, System.Action action, string name = null)
        {
            var b = UiDraw.Tap(card, name ?? caption, Container, 8f, () => { Tap(); action(); });
            TL((RectTransform)b.transform, 16f, y, w - 32f, 48f);
            var label = UiDraw.Text(b.transform, "Label", Copy(caption), UiFont.HeadingSemiBold, UiDp, Primary, .02f);
            float lw = UiDraw.Width(label);
            UiDraw.Place(UiDraw.Icon(b.transform, "Icon", icon, 18f, Primary).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-(lw + 8f) / 2f, 0f), new Vector2(18f, 18f));
            UiDraw.Place(label.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(13f, 0f), new Vector2(lw + 2f, 22f));
            return y + 48f;
        }

        void SelectSection(string name)
        {
            selectedSection = name;
            foreach (var section in sections) section.Value.gameObject.SetActive(section.Key == name);
            RefreshTabAppearance();
            if (sections.TryGetValue(name, out var active))
                scrollContent.sizeDelta = new Vector2(scrollContent.sizeDelta.x, active.sizeDelta.y);
            if (scroll != null) { scroll.StopMovement(); scroll.verticalNormalizedPosition = 1; }
            if (sections.TryGetValue("ACCOUNT / SUPPORT", out var links))
                foreach (var button in links.GetComponentsInChildren<Button>(true)) button.interactable = Hotdrop.Player.PlayerController.All.Count == 0;
        }

        void RefreshTabAppearance()
        {
            foreach (var tab in tabs)
            {
                bool on = tab.Key == selectedSection;
                tab.Value.plate.color = on ? Container : new Color(0f, 0f, 0f, 0f);
                tab.Value.label.color = on ? Primary : OnVariant;
                tab.Value.icon.color = on ? Primary : OnVariant;
            }
        }

        static void Tap() => GameAudio.Play2D(GameCue.UiTap);

        static Slider BuildSlider(Transform parent, float min, float max, float value, bool wholeNumbers = false)
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
            slider.minValue = min; slider.maxValue = max; slider.wholeNumbers = wholeNumbers;
            slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.value = value;
            // The hit area is the whole row, not the 10 px track.
            obj.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            return slider;
        }

        void ResetControls(bool playCue)
        {
            PlayerPrefs.DeleteKey("LeftHandedMode"); PlayerPrefs.DeleteKey("InvertY");
            PlayerPrefs.DeleteKey("AimSensitivity"); PlayerPrefs.DeleteKey("FocusAimSensitivity"); PlayerPrefs.DeleteKey("AimAssistEnabled");
            PlayerPrefs.DeleteKey("FocusAimToggleMode");
            PlayerPrefs.DeleteKey("GyroAim"); PlayerPrefs.DeleteKey("GyroSensitivity"); PlayerPrefs.DeleteKey("VibrationEnabled");
            game.touchInput.ResetGyroCalibration();
            HapticFeedback.SetIntensity(0.7f);
            PlayerPrefs.Save();
            game.touchInput.SetLeftHanded(false); game.touchInput.SetInvertY(false);
            game.touchInput.SetAimSensitivity(1f);
            game.touchInput.SetFocusAimSensitivity(1f);
            game.touchInput.SetFocusAimMode(false);
            game.touchInput.SetGyroAim(false);
            game.touchInput.SetGyroSensitivity(1f);
            game.touchInput.SetVibration(true);
            game.touchInput.ResetButtonLayout();
            foreach (var player in Hotdrop.Player.PlayerController.All) player.SetAimAssistEnabled(true);
            RefreshAll();
            ShowSavedConfirmation(playCue);
        }

        void AutoDetectGraphics()
        {
            int recommendedQuality = GameBootstrap.GetRecommendedGraphicsQuality();
            ConfirmReset(GraphicsCustomized(recommendedQuality), playCue =>
            {
                PlayerPrefs.SetInt(GraphicsQualityKey, recommendedQuality);
                PlayerPrefs.Save();
                GameBootstrap.ApplyGraphicsQuality(recommendedQuality);
                RefreshAll();
                ShowSavedConfirmation(playCue);
            });
        }

        void ResetAccessibility(bool playCue)
        {
            if (game.mainCamera != null) game.mainCamera.SetShakeEnabled(true);
            Hotdrop.UI.UIMotionSettings.ReduceMotion = false;
            ApplyLargerText(false);
            PlayerPrefs.SetInt(Hotdrop.Core.GameAudio.SoundCaptionsKey, 1);
            Hotdrop.UI.StreamSafePresentation.SetEnabled(false);
            Hotdrop.UI.QuickChatWheelView.Muted = false;
            PlayerPrefs.SetInt(CockpitModeKey, 1);
            PlayerPrefs.SetInt(NorthUpRadarKey, 0);
            PlayerPrefs.SetInt(HighContrastObjectiveMarkerKey, 0);
            Hotdrop.UI.UiPresentationProfile.Select(Hotdrop.UI.UiPresentationProfileId.Hybrid);
            PlayerPrefs.Save();
            RefreshAll();
            ShowSavedConfirmation(playCue);
        }

        void ResetAudio(bool playCue)
        {
            GameAudio.SetVolume(GameAudio.MasterKey, 1f);
            GameAudio.SetVolume(GameAudio.SfxKey, 1f);
            GameAudio.SetVolume(GameAudio.MusicKey, 0.6f);
            GameAudio.SetVolume(GameAudio.VoiceKey, 0.8f);
            GameAudio.SetMonoOutput(false);
            PlayerPrefs.SetInt(Hotdrop.UI.PingSystem.MuteKey, 0);
            PlayerPrefs.Save();
            RefreshAll();
            ShowSavedConfirmation(playCue);
        }

        static bool IntPreferenceDiffers(string key, int defaultValue) =>
            PlayerPrefs.HasKey(key) && PlayerPrefs.GetInt(key, defaultValue) != defaultValue;

        static bool FloatPreferenceDiffers(string key, float defaultValue) =>
            PlayerPrefs.HasKey(key) && !Mathf.Approximately(PlayerPrefs.GetFloat(key, defaultValue), defaultValue);

        static bool GraphicsCustomized(int recommendedQuality) => PlayerPrefs.HasKey(GraphicsQualityKey) &&
            PlayerPrefs.GetInt(GraphicsQualityKey) != recommendedQuality;

        bool ControlsCustomized()
        {
            if (IntPreferenceDiffers("LeftHandedMode", 0) || IntPreferenceDiffers("InvertY", 0) ||
                IntPreferenceDiffers("FocusAimToggleMode", 0) || IntPreferenceDiffers("GyroAim", 0) ||
                IntPreferenceDiffers("VibrationEnabled", 1) || IntPreferenceDiffers("AimAssistEnabled", 1) ||
                FloatPreferenceDiffers("AimSensitivity", 1f) || FloatPreferenceDiffers("FocusAimSensitivity", 1f) ||
                FloatPreferenceDiffers("GyroSensitivity", 1f) || FloatPreferenceDiffers(HapticFeedback.IntensityKey, 0.7f) ||
                FloatPreferenceDiffers("GyroBiasX", 0f) || FloatPreferenceDiffers("GyroBiasY", 0f) ||
                FloatPreferenceDiffers("GyroBiasZ", 0f)) return true;

            int buttonCount = System.Enum.GetValues(typeof(TouchInputManager.ControlButton)).Length;
            for (int i = 0; i < buttonCount; i++)
            {
                if (FloatPreferenceDiffers($"Control.{i}.Scale", 1f) ||
                    FloatPreferenceDiffers($"Control.{i}.Opacity", 0.65f) ||
                    FloatPreferenceDiffers($"Control.{i}.OffsetX", 0f) ||
                    FloatPreferenceDiffers($"Control.{i}.OffsetY", 0f)) return true;
            }
            return false;
        }

        static bool AudioCustomized() =>
            FloatPreferenceDiffers(GameAudio.MasterKey, 1f) || FloatPreferenceDiffers(GameAudio.SfxKey, 1f) ||
            FloatPreferenceDiffers(GameAudio.MusicKey, 0.6f) || FloatPreferenceDiffers(GameAudio.VoiceKey, 0.8f) ||
            IntPreferenceDiffers(GameAudio.MonoOutputKey, 0) || IntPreferenceDiffers(Hotdrop.UI.PingSystem.MuteKey, 0);

        static bool AccessibilityCustomized() =>
            IntPreferenceDiffers("CameraShakeEnabled", 1) ||
            IntPreferenceDiffers(Hotdrop.UI.UIMotionSettings.ReduceMotionKey, 0) ||
            IntPreferenceDiffers(LargerTextKey, 0) || IntPreferenceDiffers(GameAudio.SoundCaptionsKey, 1) ||
            IntPreferenceDiffers(Hotdrop.UI.StreamSafePresentation.PreferenceKey, 0) ||
            IntPreferenceDiffers(Hotdrop.UI.QuickChatWheelView.MuteKey, 0) || IntPreferenceDiffers(CockpitModeKey, 1) ||
            IntPreferenceDiffers(NorthUpRadarKey, 0) || IntPreferenceDiffers(HighContrastObjectiveMarkerKey, 0) ||
            Hotdrop.UI.UiPresentationProfile.Current != Hotdrop.UI.UiPresentationProfileId.Hybrid;

        // The reset callback receives whether to play the positive result cue. A
        // no-op reset already plays the button tap, so it only shows SAVED.
        void ConfirmReset(bool wouldDiscardCustomization, System.Action<bool> reset)
        {
            if (!wouldDiscardCustomization || resetConfirmation == null)
            {
                reset?.Invoke(wouldDiscardCustomization);
                return;
            }
            pendingReset = reset;
            resetConfirmation.Open();
        }

        void CreateResetConfirmation()
        {
            resetConfirmation = ModalView.Create(modal,
                L("resetConfirmation.title", "Reset to default?"),
                L("resetConfirmation.body", "Reset this tab's saved settings to their defaults?"),
                "SettingsResetConfirmation");
            resetConfirmation.AddAction(L("resetConfirmation.cancel", "Cancel"), ButtonVariant.Secondary, () =>
            {
                pendingReset = null;
                resetConfirmation.Close();
            });
            resetConfirmation.AddAction(L("resetConfirmation.confirm", "Reset"), ButtonVariant.Destructive, () =>
            {
                var reset = pendingReset;
                pendingReset = null;
                resetConfirmation.Close();
                reset?.Invoke(true);
            });
            resetConfirmation.gameObject.SetActive(false);
        }

        void ShowSavedConfirmation(bool playCue = true)
        {
            if (savedStatus == null) return;
            if (playCue) GameAudio.Play2D(GameCue.UiPositive);
            SetSavedStatusText(L("status.saved", "Saved"));
            savedStatusUntil = Time.unscaledTime + 1.5f;
        }

        void SetSavedStatusText(string text)
        {
            if (savedStatus == null || savedChip == null) return;
            savedStatus.text = text;
            float textWidth = UiDraw.Width(savedStatus);
            float chipWidth = 12f + 16f + 4f + textWidth + 12f;
            UiDraw.Place(savedChip, new Vector2(1f, .5f), new Vector2(1f, .5f),
                new Vector2(-20f, 0f), new Vector2(chipWidth, 30f));
            UiDraw.Place(savedStatus.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f),
                new Vector2(32f, 0f), new Vector2(textWidth + 2f, 14f));
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
