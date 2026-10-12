using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>
    /// A transient message. It enters and leaves on a 200 ms ease-out; under Reduce Motion the
    /// travel collapses to a plain fade, because a message that slides is still a message that
    /// moves.
    /// </summary>
    public class ToastView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI message;
        [SerializeField] CanvasGroup group;
        [SerializeField] Image accent;
        [SerializeField] UiMotionDriver driver;
        [SerializeField] RectTransform rect;

        UiMotion motion;
        float hideAt = -1f;

        /// <summary>The message text currently shown.</summary>
        public string Message => message != null ? message.text : "";

        /// <summary>Builds a toast, hidden until <see cref="Show"/> is called.</summary>
        public static ToastView Create(Transform parent, string name = "Toast")
        {
            var root = UIKit.Rect(parent, name, new Vector2(UIDesignTokens.Dp(420f), UIDesignTokens.Dp(56f)));
            var view = root.gameObject.AddComponent<ToastView>();

            var fill = UIKit.Surface(root, "Fill", UIDesignTokens.PanelBg, UIDesignTokens.RadiusMdDp);
            UIKit.Stretch(fill.rectTransform);
            view.accent = UIKit.Bar(fill.rectTransform, "Accent", UIDesignTokens.PrimaryAction);
            var accentRt = view.accent.rectTransform;
            accentRt.anchorMin = new Vector2(0f, 0f);
            accentRt.anchorMax = new Vector2(0f, 1f);
            accentRt.sizeDelta = new Vector2(UIDesignTokens.Dp(UIDesignTokens.BorderBoldUnits), 0f);

            view.message = UIKit.Label(fill.rectTransform, "Message", "", UiTextRole.Body);
            view.message.alignment = TextAlignmentOptions.MidlineLeft;
            view.message.textWrappingMode = TextWrappingModes.Normal;
            UIKit.Stretch(view.message.rectTransform, UIDesignTokens.Dp(UIDesignTokens.SpaceCardDp), UIDesignTokens.Dp(UIDesignTokens.SpaceSmDp), UIDesignTokens.Dp(UIDesignTokens.SpaceCardDp), UIDesignTokens.Dp(UIDesignTokens.SpaceSmDp));

            view.group = root.gameObject.AddComponent<CanvasGroup>();
            view.rect = root;
            view.driver = root.gameObject.AddComponent<UiMotionDriver>();
            view.group.alpha = 0f;
            view.group.blocksRaycasts = false;
            view.group.interactable = false;
            return view;
        }

        /// <summary>Shows a message for <paramref name="seconds"/>.</summary>
        public void Show(string text, UiTone tone = UiTone.Neutral, float seconds = 2.5f)
        {
            if (message != null) message.text = text ?? "";
            if (accent != null) accent.color = UIDesignTokens.ForTone(tone);
            if (group != null) group.alpha = 0f;
            gameObject.SetActive(true);

            // Fade only: a toast that slides in is a toast that moves, and Reduce Motion must
            // never be the only reason a message is hard to miss.
            motion = UIMotion.Fade(group, rect, 0f, 1f, MotionRole.Standard);
            driver?.Play(motion);
            hideAt = Time.unscaledTime + Mathf.Max(0.1f, seconds);
        }

        void Update()
        {
            if (hideAt >= 0f && Time.unscaledTime >= hideAt) Hide();
        }

        /// <summary>Fades the toast out and releases it.</summary>
        public void Hide()
        {
            hideAt = -1f;
            var fade = UIMotion.Fade(group, rect, group != null ? group.alpha : 1f, 0f, MotionRole.Standard);
            driver?.Play(fade);
            if (driver == null) gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// A modal dialog: a dimmed scrim that blocks the world, a panel, and an action row whose
    /// buttons all clear 48 dp. The scrim is not clickable-through by design - a modal that can be
    /// dismissed by tapping behind it is a bug, not a convenience.
    /// </summary>
    public class ModalView : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] TextMeshProUGUI title;
        [SerializeField] TextMeshProUGUI body;
        [SerializeField] RectTransform actions;
        [SerializeField] UiMotionDriver driver;

        /// <summary>True while the modal is on screen.</summary>
        public bool IsOpen => gameObject != null && gameObject.activeSelf;

        /// <summary>Builds a modal, hidden until <see cref="Open"/> is called.</summary>
        public static ModalView Create(Transform parent, string titleText, string bodyText, string name = "Modal")
        {
            var root = UIKit.Stretch(parent, name);
            var view = root.gameObject.AddComponent<ModalView>();

            var scrim = UIKit.Bar(root, "Scrim", UIDesignTokens.SurfaceScrim);
            UIKit.Stretch(scrim.rectTransform);
            scrim.raycastTarget = true;

            var panel = UIKit.Panel(root, "Panel");
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(UIDesignTokens.Dp(420f), UIDesignTokens.Dp(240f));
            panel.anchoredPosition = Vector2.zero;

            var column = UIKit.Column(panel, "Column", UIDesignTokens.SpaceGutterDp);
            UIKit.Stretch(column, UIDesignTokens.Dp(UIDesignTokens.SpaceCardDp), UIDesignTokens.Dp(UIDesignTokens.SpaceCardDp), UIDesignTokens.Dp(UIDesignTokens.SpaceCardDp), UIDesignTokens.Dp(UIDesignTokens.SpaceCardDp));

            view.title = UIKit.Label(column, "Title", titleText, UiTextRole.TitleMedium);
            WrapRow(view.title.rectTransform);
            view.body = UIKit.Paragraph(column, "Body", bodyText, UiTextRole.Body);
            WrapRow(view.body.rectTransform);

            view.actions = UIKit.Row(column, "Actions", UIDesignTokens.SpaceMdDp);
            WrapRow(view.actions);
            var actionElement = view.actions.gameObject.AddComponent<LayoutElement>();
            actionElement.minHeight = UiMetrics.MinTouchUnits;

            view.group = root.gameObject.AddComponent<CanvasGroup>();
            view.driver = root.gameObject.AddComponent<UiMotionDriver>();
            view.group.alpha = 0f;
            return view;
        }

        static void WrapRow(RectTransform child)
        {
            var element = child.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;
            element.preferredWidth = 0f;
        }

        /// <summary>Adds an action button to the modal's row.</summary>
        public HotdropButton AddAction(string caption, ButtonVariant variant, Action onClick)
        {
            var button = UIKit.Button(actions, caption, variant, onClick, "Action_" + caption);
            return button;
        }

        /// <summary>Fades the modal in.</summary>
        public void Open()
        {
            gameObject.SetActive(true);
            if (group != null) { group.alpha = 0f; group.blocksRaycasts = true; group.interactable = true; }
            driver?.Play(UIMotion.Fade(group, null, 0f, 1f, MotionRole.Emphasis));
        }

        /// <summary>Fades the modal out and closes it.</summary>
        public void Close()
        {
            if (group != null) { group.blocksRaycasts = false; group.interactable = false; }
            if (driver == null) { gameObject.SetActive(false); return; }
            driver.Play(UIMotion.Fade(group, null, group != null ? group.alpha : 1f, 0f, MotionRole.Standard,
                onComplete: () => gameObject.SetActive(false)));
        }
    }

    /// <summary>
    /// An inline failure with a retry affordance. Failure copy always says what happened and
    /// offers a way forward; nothing here invents telemetry or a developer status line.
    /// </summary>
    public class ErrorRetryView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI message;
        [SerializeField] HotdropButton retry;

        /// <summary>The retry button, so callers can disable it while a retry is in flight.</summary>
        public HotdropButton Retry => retry;

        /// <summary>Builds an error/retry block.</summary>
        public static ErrorRetryView Create(Transform parent, string messageText, Action onRetry, string name = "ErrorRetry")
        {
            var root = UIKit.Rect(parent, name, new Vector2(UIDesignTokens.Dp(400f), UIDesignTokens.Dp(120f)));
            var view = root.gameObject.AddComponent<ErrorRetryView>();

            var fill = UIKit.Surface(root, "Fill", UIDesignTokens.WithAlpha(UIDesignTokens.SignalWarning, 0.14f), UIDesignTokens.RadiusMdDp);
            UIKit.Stretch(fill.rectTransform);
            UIKit.Border(root, "Border", UIDesignTokens.SignalWarning, UIDesignTokens.BorderNormalUnits);

            // The shape cue means the block still reads when the warning tint does not.
            var cue = UICue.Create(root, UiTone.Warning, UiDraw.L("network.errorRetry.offlineCue", "Offline"), UIDesignTokens.Dp(UIDesignTokens.BodyDp));
            cue.Shape = CueShape.Cross;
            var cueRt = cue.GetComponent<RectTransform>();
            cueRt.anchorMin = cueRt.anchorMax = new Vector2(0f, 1f);
            cueRt.pivot = new Vector2(0f, 1f);
            cueRt.anchoredPosition = new Vector2(UIDesignTokens.Dp(UIDesignTokens.SpaceCardDp), -UIDesignTokens.Dp(UIDesignTokens.SpaceCardDp));

            var column = UIKit.Stretch(root, "Column",
                UIDesignTokens.Dp(64f), UIDesignTokens.Dp(UIDesignTokens.SpaceCardDp),
                UIDesignTokens.Dp(UIDesignTokens.SpaceCardDp), UIDesignTokens.Dp(UIDesignTokens.SpaceCardDp));
            var group = column.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = UIDesignTokens.Dp(UIDesignTokens.SpaceMdDp);
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            view.message = UIKit.Paragraph(column, "Message", messageText, UiTextRole.BodySmall);
            view.message.alignment = TextAlignmentOptions.TopLeft;
            var messageElement = view.message.gameObject.AddComponent<LayoutElement>();
            messageElement.flexibleWidth = 1f;
            messageElement.preferredWidth = 0f;

            var row = UIKit.Row(column, "Actions", UIDesignTokens.SpaceMdDp);
            var rowElement = row.gameObject.AddComponent<LayoutElement>();
            rowElement.minHeight = UiMetrics.MinTouchUnits;
            view.retry = UIKit.Button(row, UiDraw.L("network.errorRetry.retry", "Try Again"), ButtonVariant.Primary, onRetry, "Retry");

            return view;
        }

        /// <summary>Replaces the failure copy.</summary>
        public void SetMessage(string value)
        {
            if (message != null) message.text = value ?? "";
        }

        /// <summary>Puts the retry into its loading state while a retry is in flight.</summary>
        public void SetRetrying(bool retrying)
        {
            retry?.SetLoading(retrying);
        }
    }

    /// <summary>
    /// A banner that appears when the player is offline. Offline never removes a capability, so
    /// the banner says what still works rather than only what does not.
    /// </summary>
    public class OfflineBannerView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI message;
        [SerializeField] CanvasGroup group;
        bool shown;

        /// <summary>
        /// True while the banner is meant to be showing. This is the requested state, not the
        /// alpha: the fade that follows <see cref="SetVisible"/> is presentation and may still be
        /// in flight.
        /// </summary>
        public bool IsVisible => shown;

        /// <summary>Builds an offline banner, hidden by default.</summary>
        public static OfflineBannerView Create(Transform parent, string messageText = null, string name = "OfflineBanner")
        {
            var root = UIKit.Rect(parent, name, new Vector2(0f, UIDesignTokens.Dp(40f)));
            var view = root.gameObject.AddComponent<OfflineBannerView>();

            var fill = UIKit.Bar(root, "Fill", UIDesignTokens.WithAlpha(UIDesignTokens.Background, UIDesignTokens.BannerOpacity));
            UIKit.Stretch(fill.rectTransform);

            var row = UIKit.Row(root, "Row", UIDesignTokens.SpaceSmDp);
            UIKit.Stretch(row);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            row.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;

            var cue = UICue.Create(row, UiTone.Warning, null, UIDesignTokens.Dp(UIDesignTokens.BodySmallDp));
            cue.Label = "";
            var cueElement = cue.gameObject.AddComponent<LayoutElement>();
            cueElement.preferredWidth = UIDesignTokens.Dp(UIDesignTokens.BodySmallDp);

            view.message = UIKit.Label(row, "Message", messageText ?? UiDraw.L("network.offlineBanner.savedContent", "Offline — saved content still works"), UiTextRole.Caption, UiTone.Warning);
            view.message.alignment = TextAlignmentOptions.MidlineLeft;

            view.group = root.gameObject.AddComponent<CanvasGroup>();
            view.group.alpha = 0f;
            view.group.blocksRaycasts = false;
            view.gameObject.SetActive(false);
            view.shown = false;
            return view;
        }

        /// <summary>Shows or hides the banner.</summary>
        public void SetVisible(bool visible)
        {
            if (visible == shown) return;
            shown = visible;
            if (visible) gameObject.SetActive(true);
            if (group == null) { gameObject.SetActive(visible); return; }

            group.alpha = visible ? 0f : group.alpha;
            var driver = GetComponent<UiMotionDriver>();
            if (driver == null) driver = gameObject.AddComponent<UiMotionDriver>();
            driver.Play(UIMotion.Fade(group, null, group.alpha, visible ? 1f : 0f, MotionRole.Standard,
                onComplete: () => { if (!shown) gameObject.SetActive(false); }));
        }
    }

    /// <summary>
    /// A connection readout: bars plus a word. The bar count and the word both carry the state, so
    /// a player can read "poor" or "good" without interpreting a colour.
    /// </summary>
    public class NetworkIndicatorView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] Image[] bars;
        [SerializeField] UiTone tone = UiTone.Neutral;

        /// <summary>Signal quality from 0 (none) to 4 (excellent).</summary>
        public int Quality { get; private set; } = 4;

        /// <summary>
        /// The word currently shown. This is the channel that survives greyscale, so it is part
        /// of the component's public contract rather than an implementation detail.
        /// </summary>
        public string Readout => label != null ? label.text : "";

        /// <summary>True when the device reports no usable connection.</summary>
        public bool IsOffline { get; private set; }

        /// <summary>Builds a network indicator.</summary>
        public static NetworkIndicatorView Create(Transform parent, string name = "NetworkIndicator")
        {
            var root = UIKit.Rect(parent, name, new Vector2(UIDesignTokens.Dp(140f), UIDesignTokens.Dp(28f)));
            var view = root.gameObject.AddComponent<NetworkIndicatorView>();

            var row = UIKit.Row(root, "Row", UIDesignTokens.SpaceXsDp);
            UIKit.Stretch(row);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            row.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;

            view.bars = new Image[4];
            for (int i = 0; i < view.bars.Length; i++)
            {
                var bar = UIKit.Bar(row, "Bar" + i, UIDesignTokens.SurfaceDisabled);
                var rt = bar.rectTransform;
                rt.sizeDelta = new Vector2(UIDesignTokens.Dp(6f), UIDesignTokens.Dp(8f + i * 5f));
                var element = bar.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = UIDesignTokens.Dp(6f);
                element.preferredHeight = UIDesignTokens.Dp(8f + i * 5f);
                view.bars[i] = bar;
            }

            view.label = UIKit.Label(row, "Label", LabelFor(4), UiTextRole.Caption);
            var labelElement = view.label.gameObject.AddComponent<LayoutElement>();
            labelElement.preferredWidth = UIDesignTokens.Dp(64f);
            labelElement.minHeight = UiMetrics.MinTextUnits;

            view.SetQuality(4, false);
            return view;
        }

        /// <summary>The word shown for a quality level. Exposed so tests can assert the text channel.</summary>
        public static string LabelFor(int quality) => quality switch
        {
            <= 0 => UiDraw.L("network.quality.offline", "Offline"),
            1 => UiDraw.L("network.quality.poor", "Poor"),
            2 => UiDraw.L("network.quality.fair", "Fair"),
            3 => UiDraw.L("network.quality.good", "Good"),
            _ => UiDraw.L("network.quality.excellent", "Excellent"),
        };

        /// <summary>The tone shown for a quality level.</summary>
        public static UiTone ToneFor(int quality) => quality switch
        {
            <= 0 => UiTone.Warning,
            1 => UiTone.Warning,
            2 => UiTone.Objective,
            _ => UiTone.Success,
        };

        /// <summary>Sets the signal quality and whether the device is offline.</summary>
        public void SetQuality(int quality, bool offline)
        {
            Quality = Mathf.Clamp(quality, 0, 4);
            IsOffline = offline;
            tone = ToneFor(Quality);

            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i] == null) continue;
                bool lit = i < Quality && !offline;
                bars[i].color = lit ? UIDesignTokens.ForTone(tone) : UIDesignTokens.SurfaceDisabled;
            }

            if (label != null)
            {
                label.text = offline ? LabelFor(0) : LabelFor(Quality);
                label.color = UIDesignTokens.ForTone(tone);
            }
        }
    }
}
