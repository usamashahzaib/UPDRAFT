using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Hotdrop.UI
{
    /// <summary>
    /// A development-only contact sheet for the UI foundation.
    ///
    /// It exists so a designer or a reviewer can see every token, every button state and every
    /// overlay at once, on a real device, at a real aspect ratio. It is deliberately unreachable
    /// from player navigation:
    ///
    ///  - <see cref="IsAvailable"/> is false in a non-development build, so the editor menu item
    ///    and any scripted call both refuse to open it;
    ///  - nothing in the game's navigation code references this class;
    ///  - there is no runtime auto-launch: the editor menu item or an explicit scripted call is
    ///    the only way in.
    ///
    /// It is a review tool, not a screen. No player-facing flow is implemented by this file.
    /// </summary>
    public static class UIComponentGallery
    {
        const string RootName = "UIComponentGallery";

        /// <summary>The open gallery, if any.</summary>
        public static UIComponentGalleryView Current { get; private set; }

        /// <summary>
        /// True only in the Unity Editor or a development build. A release player always answers
        /// false, so the gallery cannot appear in a shipped navigation path.
        /// </summary>
        public static bool IsAvailable => Application.isEditor || Debug.isDebugBuild;

        /// <summary>Why the gallery is unavailable, for a clear log instead of a silent no-op.</summary>
        public static string UnavailableReason =>
            "The UI component gallery exists only in the Unity Editor or a development build.";

        /// <summary>
        /// Opens the gallery under <paramref name="parent"/>. Returns null and logs why when the
        /// current build is not allowed to show it.
        /// </summary>
        public static UIComponentGalleryView Open(Transform parent = null)
        {
            if (!IsAvailable)
            {
                Debug.LogWarning("[UIComponentGallery] " + UnavailableReason);
                return null;
            }

            if (Current != null) Close();

            if (parent == null)
            {
                var existing = GameObject.Find(RootName);
                if (existing != null) Object.Destroy(existing);
                var root = new GameObject(RootName, typeof(RectTransform));
                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32000;
                if (root.GetComponent<CanvasScaler>() == null) root.AddComponent<CanvasScaler>();
                UiFloor.Apply(root.transform);
                parent = root.transform;
            }

            Current = UIComponentGalleryView.Build(parent, LandscapeAspect.SixteenByNine);
            return Current;
        }

        /// <summary>Closes the gallery if it is open.</summary>
        public static void Close()
        {
            if (Current == null) return;
            Current.Dispose();
            Current = null;
        }

        /// <summary>Opens or closes the gallery.</summary>
        public static void Toggle(Transform parent = null)
        {
            if (Current != null) Close();
            else Open(parent);
        }
    }

    /// <summary>The gallery's built content: one specimen per token group and per component.</summary>
    public class UIComponentGalleryView : MonoBehaviour
    {
        [SerializeField] RectTransform header;
        [SerializeField] RectTransform scrollContent;
        [SerializeField] TextMeshProUGUI subtitle;
        [SerializeField] List<HotdropButton> stateSpecimens = new List<HotdropButton>();
        [SerializeField] LandscapeAspect aspect = LandscapeAspect.SixteenByNine;

        /// <summary>The aspect ratio currently on show.</summary>
        public LandscapeAspect Aspect => aspect;

        /// <summary>How many button state specimens the gallery demonstrates.</summary>
        public int StateSpecimenCount => stateSpecimens.Count;

        /// <summary>The aspect ratios the gallery can demonstrate.</summary>
        public static readonly LandscapeAspect[] SupportedAspects =
        {
            LandscapeAspect.SixteenByNine,
            LandscapeAspect.EighteenByNine,
            LandscapeAspect.NineteenPointFiveByNine,
            LandscapeAspect.TwentyByNine,
        };

        /// <summary>Builds the gallery under <paramref name="parent"/>.</summary>
        public static UIComponentGalleryView Build(Transform parent, LandscapeAspect aspect = LandscapeAspect.SixteenByNine)
        {
            var root = UIKit.Stretch(parent, "Gallery");
            var view = root.gameObject.AddComponent<UIComponentGalleryView>();
            view.aspect = aspect;

            var background = UIKit.Bar(root, "Background", UIDesignTokens.Background);
            UIKit.Stretch(background.rectTransform);

            // Header pinned to the top edge. Stretching it instead would let the row centre
            // itself over the whole sheet.
            float headerHeight = UIDesignTokens.Dp(56f);
            view.header = UIKit.Rect(root, "Header", new Vector2(0f, headerHeight));
            view.header.anchorMin = new Vector2(0f, 1f);
            view.header.anchorMax = new Vector2(1f, 1f);
            view.header.pivot = new Vector2(0.5f, 1f);
            view.header.anchoredPosition = Vector2.zero;
            var headerFill = UIKit.Surface(view.header, "Fill", UIDesignTokens.SurfacePanel, UIDesignTokens.RadiusSmDp);
            UIKit.Stretch(headerFill.rectTransform);

            var headerRow = UIKit.Row(view.header, "HeaderRow", UIDesignTokens.SpaceGutterDp);
            UIKit.Stretch(headerRow, UIDesignTokens.Dp(UIDesignTokens.SpaceMarginDp), UIDesignTokens.Dp(UIDesignTokens.SpaceXsDp),
                UIDesignTokens.Dp(UIDesignTokens.SpaceMarginDp), UIDesignTokens.Dp(UIDesignTokens.SpaceXsDp));
            headerRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            headerRow.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;

            var title = UIKit.Label(headerRow, "Title", "HOTDROP UI FOUNDATION — DEVELOPMENT GALLERY", UiTextRole.TitleMedium);
            title.enableAutoSizing = true;
            title.fontSizeMin = UiMetrics.MinTextUnits;
            var titleElement = title.gameObject.AddComponent<LayoutElement>();
            titleElement.flexibleWidth = 1f;
            titleElement.preferredWidth = 0f;
            titleElement.minHeight = UiMetrics.MinTouchUnits;

            view.subtitle = UIKit.Label(headerRow, "Subtitle", AspectLabel(aspect), UiTextRole.Caption, UiTone.Objective);
            var subtitleElement = view.subtitle.gameObject.AddComponent<LayoutElement>();
            subtitleElement.preferredWidth = UIDesignTokens.Dp(220f);
            subtitleElement.minHeight = UiMetrics.MinTextUnits;

            // A safe-area root for everything below the header, so the sheet itself demonstrates
            // that controls stay clear of cutouts and the gesture area.
            var safeAreaHost = UIKit.Stretch(root, "SafeAreaHost", 0f, headerHeight, 0f, 0f);
            // Keep the header offset on a separate host. SafeAreaRoot owns the anchors and
            // offsets of its own RectTransform, so placing it on safeAreaHost would erase the
            // reserved header band every time safe-area insets are reapplied.
            var safeAreaRect = UIKit.Stretch(safeAreaHost, "SafeArea");
            var safeRoot = safeAreaRect.gameObject.AddComponent<SafeAreaRoot>();
            safeRoot.Apply();

            var scrollRect = UIKit.Stretch(safeRoot.Content, "Scroll");
            var viewport = UIKit.Stretch(scrollRect, "Viewport");
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f); // catches drags

            // The content is anchored to the top of the viewport and grows downward; stretching it
            // vertically would fight the ContentSizeFitter and overflow the sheet.
            var content = UIKit.Rect(viewport, "Content", Vector2.zero);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            int margin = Mathf.RoundToInt(UIDesignTokens.Dp(UIDesignTokens.SpaceMarginDp));
            var column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = UIDesignTokens.Dp(UIDesignTokens.SpaceMarginDp);
            column.padding = new RectOffset(margin, margin, margin, margin);
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroller = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroller.viewport = viewport;
            scroller.content = content;
            scroller.horizontal = false;
            scroller.movementType = ScrollRect.MovementType.Clamped;
            scroller.scrollSensitivity = 40f;

            view.scrollContent = content;
            view.BuildContent(content);

            view.SetAspect(aspect);
            return view;
        }

        /// <summary>Re-labels the gallery for a different aspect ratio and re-applies the safe area.</summary>
        public void SetAspect(LandscapeAspect value)
        {
            aspect = value;
            if (subtitle != null) subtitle.text = AspectLabel(value);
            foreach (var safeArea in GetComponentsInChildren<SafeAreaRoot>(true)) safeArea.Apply();
        }

        /// <summary>Moves to the next supported aspect ratio. Used by the gallery's own control.</summary>
        public void NextAspect()
        {
            int index = System.Array.IndexOf(SupportedAspects, aspect);
            SetAspect(SupportedAspects[(index + 1) % SupportedAspects.Length]);
        }

        static string AspectLabel(LandscapeAspect aspect)
        {
            float ratio = SafeAreaMath.Ratio(aspect);
            return $"{ratio * 9f:0.0}:9  ·  SAFE AREA ACTIVE  ·  48DP TARGETS";
        }

        void BuildContent(RectTransform column)
        {
            Section(column, "BUTTON VARIANTS");
            // Six minimum-width controls cannot truthfully fit one 16:9 phone row. Demonstrate
            // deliberate wrapping instead of allowing the layout group to overlap captions.
            var variantsMain = UIKit.Row(column, "VariantsMain");
            AddSpecimen(UIKit.Button(variantsMain, "PRIMARY", ButtonVariant.Primary));
            AddSpecimen(UIKit.Button(variantsMain, "SECONDARY", ButtonVariant.Secondary));
            AddSpecimen(UIKit.Button(variantsMain, "QUIET", ButtonVariant.Quiet));
            AddSpecimen(UIKit.Button(variantsMain, "DESTRUCTIVE", ButtonVariant.Destructive));
            var variantsIcons = UIKit.Row(column, "VariantsIcons");
            AddSpecimen(UIKit.IconButton(variantsIcons, "BACK", UiGlyph.Back));
            AddSpecimen(UIKit.IconButton(variantsIcons, "CLOSE", UiGlyph.Close));

            Section(column, "BUTTON STATES");
            var statesA = UIKit.Row(column, "StatesA");
            AddSpecimen(UIKit.Button(statesA, "NORMAL", ButtonVariant.Primary));
            var pressed = UIKit.Button(statesA, "PRESSED", ButtonVariant.Primary);
            // A genuinely held pointer, so the specimen shows the real pressed treatment rather
            // than a debug-only override of the state machine.
            pressed.OnPointerDown(new UnityEngine.EventSystems.PointerEventData(null));
            AddSpecimen(pressed);
            var held = UIKit.Button(statesA, "HELD", ButtonVariant.Primary);
            held.SetHeld(true);
            AddSpecimen(held);
            var statesB = UIKit.Row(column, "StatesB");
            var disabled = UIKit.Button(statesB, "DISABLED", ButtonVariant.Primary);
            disabled.SetInteractable(false);
            AddSpecimen(disabled);
            var cooldown = UIKit.Button(statesB, "COOLDOWN 2.0s", ButtonVariant.Primary);
            cooldown.BeginCooldown(2f);
            AddSpecimen(cooldown);
            var loading = UIKit.Button(statesB, "LOADING", ButtonVariant.Primary);
            loading.SetLoading(true);
            AddSpecimen(loading);

            Section(column, "SHAPE CUES — COLOUR IS NEVER THE ONLY SIGNAL");
            var cues = UIKit.Row(column, "Cues");
            cues.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var friendly = UIKit.Cue(cues, UiTone.Friendly);
            var enemy = UIKit.Cue(cues, UiTone.Enemy);
            var objective = UIKit.Cue(cues, UiTone.Objective);
            var cross = UIKit.Cue(cues, UiTone.Enemy, "BLOCKED");
            cross.GetComponentInChildren<UICue>().Shape = CueShape.Cross;
            WrapInRow(friendly, 1f); WrapInRow(enemy, 1f); WrapInRow(objective, 1f); WrapInRow(cross, 1f);

            Section(column, "PANELS, CARDS AND HEADERS");
            var surfaces = UIKit.Row(column, "Surfaces");
            surfaces.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            WrapInRow(UIKit.Card(surfaces, "CardA", "FLIGHT SEAL", "Complete 2 matches  ·  1/2"), 1f);
            WrapInRow(UIKit.Card(surfaces, "CardB", "ROUTE BUILDER", "3 / 12 / 40 teammate uses"), 1f);
            WrapInRow(UIKit.Card(surfaces, "CardC", "ROUTE BUILDER", "3 / 12 / 40 teammate uses"), 1f);
            foreach (Transform card in surfaces)
            {
                var element = card.gameObject.AddComponent<LayoutElement>();
                element.minHeight = UIDesignTokens.Dp(UIDesignTokens.BodyDp) * 6f;
            }

            var headerDemo = UIKit.Header(column, "HOME", "THREE PLAYERS. ONE TEAM. THREE MINUTES.");
            WrapInRow(headerDemo, 1f);

            Section(column, "NAVIGATION AND STATUS");
            var nav = UIKit.Row(column, "Nav");
            nav.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            WrapInRow((RectTransform)UIKit.NavItem(nav, "PLAY", true).transform, 1f);
            WrapInRow((RectTransform)UIKit.NavItem(nav, "PARTY").transform, 1f);
            WrapInRow((RectTransform)UIKit.NavItem(nav, "MORE").transform, 1f);

            var badges = UIKit.Row(column, "Badges", UIDesignTokens.SpaceSmDp);
            badges.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            foreach (var (caption, tone) in new[]
            {
                ("[ZONE A // CAPTURED]", UiTone.Objective),
                ("[ALLY // 24MS]", UiTone.Friendly),
                ("[SHOT BLOCKED]", UiTone.Enemy),
                ("[MATCH READY]", UiTone.Success),
                ("[NO SIGNAL]", UiTone.Disabled),
            })
            {
                var badge = UIKit.StatusBadge(badges, caption, tone);
                var element = badge.gameObject.AddComponent<LayoutElement>();
                // Share the row evenly: a fixed dp width would overflow a narrow phone.
                element.flexibleWidth = 1f;
                element.preferredWidth = 0f;
                element.preferredHeight = UIDesignTokens.Dp(28f);
            }

            Section(column, "DATA CONTROLS");
            var progress = ProgressBarView.Create(column, "ProgressBar", UiTone.Friendly);
            progress.SetValue(0.62f);
            WrapInRow((RectTransform)progress.transform, 1f);

            var second = ProgressBarView.Create(column, "ProgressBarEnemy", UiTone.Enemy);
            second.SetValue(0.28f, "LOW");
            WrapInRow((RectTransform)second.transform, 1f);

            var sliderRow = UIKit.Row(column, "SliderRow");
            sliderRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var slider = SliderView.Create(sliderRow, "MASTER VOLUME", 0f, 1f, 0.7f);
            WrapInRow((RectTransform)slider.transform, 1f);
            var toggle = ToggleView.Create(sliderRow, "REDUCE MOTION", false);
            WrapInRow((RectTransform)toggle.transform, 1f);
            var toggle2 = ToggleView.Create(sliderRow, "LARGER TEXT", false);
            WrapInRow((RectTransform)toggle2.transform, 1f);

            var input = InputFieldView.Create(column, "CALLSIGN", "ENTER A NAME");
            WrapInRow((RectTransform)input.transform, 1f);

            Section(column, "NETWORK AND OVERLAYS");
            var networkRow = UIKit.Row(column, "NetworkRow");
            networkRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var network = NetworkIndicatorView.Create(networkRow, "Good");
            network.SetQuality(4, false);
            WrapInRow((RectTransform)network.transform, 1f);
            var poor = NetworkIndicatorView.Create(networkRow, "Poor");
            poor.SetQuality(1, false);
            WrapInRow((RectTransform)poor.transform, 1f);
            var offline = NetworkIndicatorView.Create(networkRow, "Offline");
            offline.SetQuality(0, true);
            WrapInRow((RectTransform)offline.transform, 1f);

            var banner = OfflineBannerView.Create(column);
            WrapInRow((RectTransform)banner.transform, 1f);
            banner.SetVisible(true);

            var toast = ToastView.Create(column, "ToastSample");
            WrapInRow((RectTransform)toast.transform, 1f);
            toast.Show("OBJECTIVE ROTATING — ZONE B IN 30s", UiTone.Objective, 1e6f);

            var error = ErrorRetryView.Create(column, "MATCHMAKING COULD NOT REACH THE SERVER. OFFLINE PRACTICE IS STILL AVAILABLE.", () => {});
            WrapInRow((RectTransform)error.transform, 1f);

            var modal = ModalView.Create(column, "CONFIRM", "LEAVE THE CURRENT MATCH?");
            modal.AddAction("CANCEL", ButtonVariant.Secondary, null);
            modal.AddAction("LEAVE", ButtonVariant.Destructive, null);
            WrapInRow((RectTransform)modal.transform, 1f);
            modal.Open();

            Section(column, "TYPOGRAPHY ROLES");
            foreach (var role in UITypography.AllRoles)
            {
                var style = UITypography.Style(role);
                var row = UIKit.Row(column, "Role_" + role);
                row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                var name = UIKit.Label(row, "RoleName", role.ToString(), UiTextRole.Caption, UiTone.Disabled);
                WrapInRow(name.rectTransform, 1f);
                var specimenText = UITypography.IsTabular(role) ? "12:34   086   24/30" : "The quick brown fox jumps over the lazy dog";
                var specimen = UIKit.Label(row, "Specimen", specimenText, role);
                WrapInRow(specimen.rectTransform, 3f);
            }

            Section(column, "EXPANSION TOLERANCE — 30% WIDER TEXT STILL FITS");
            var expansion = UIKit.Paragraph(column, "Expansion",
                "THIS SENTENCE IS SET AT THE BODY ROLE AND MUST SURVIVE BEING 30% WIDER THAN MEASURED, " +
                "WHICH IS WHAT A LONGER TRANSLATION OR THE LARGER-TEXT COMFORT OPTION PRODUCES. " +
                "IF IT CLIPS HERE, THE CONTAINER IS TOO NARROW.", UiTextRole.Body);
            WrapInRow(expansion.rectTransform, 1f);
        }

        HotdropButton AddSpecimen(HotdropButton button)
        {
            stateSpecimens.Add(button);
            return button;
        }

        void Section(Transform parent, string title)
        {
            var label = UIKit.Label(parent, "Section", title, UiTextRole.Caption, UiTone.Friendly);
            var element = label.gameObject.AddComponent<LayoutElement>();
            element.minHeight = UiMetrics.MinTextUnits;
            element.preferredHeight = UiMetrics.MinTextUnits * 1.5f;
            element.flexibleWidth = 1f;
            UIKit.Divider(parent, "SectionDivider");
        }

        static void WrapInRow(RectTransform child, float weight)
        {
            var element = child.gameObject.GetComponent<LayoutElement>();
            if (element == null) element = child.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = weight;
            element.preferredWidth = 0f;
        }

        /// <summary>Tears the gallery down.</summary>
        public void Dispose()
        {
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }
    }
}
