using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>A full-screen menu: fixed top bar (title, optional BACK), scrolling content, status line.</summary>
    public sealed class MenuScreen
    {
        public GameObject Root;
        public RectTransform Content;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Status;
        public GameObject Back;
    }

    /// <summary>
    /// QA-019 / QA-020: one layout for every menu. Controls flow top-to-bottom in a scroll view,
    /// never below 48 dp tall (<see cref="UiMetrics.MinTouchUnits"/>) and text never below ~12 dp
    /// (<see cref="UiMetrics.MinTextUnits"/>), so no screen depends on fixed coordinates that
    /// overflow a small phone. BACK lives in the top bar, never among the page rows (QA-002).
    /// </summary>
    public static class MenuLayout
    {
        // UI foundation (Prompt 01): these are aliases of the central tokens, not a second
        // palette. Existing screens keep reading MenuLayout.Background / ButtonFill unchanged.
        public static Color Background => UIDesignTokens.Background;      // opaque: HUD must not show through
        public static Color ButtonFill => UIDesignTokens.SurfacePanel;
        public const float TopBarHeight = 80f;
        public const float StatusHeight = 52f;
        public const float ContentMaxWidth = 1120f;
        public const float Spacing = 14f;

        public static MenuScreen Create(Transform parent, string name, string title, UnityAction back)
        {
            var screen = new MenuScreen();
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());
            root.GetComponent<Image>().color = Background;
            root.AddComponent<UiScreenEnterMotion>();
            screen.Root = root;

            // Top bar: title centred, BACK on the left.
            var bar = new GameObject("Nav_Bar", typeof(RectTransform));
            bar.transform.SetParent(root.transform, false);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 1); barRt.anchorMax = new Vector2(1, 1); barRt.pivot = new Vector2(0.5f, 1);
            barRt.sizeDelta = new Vector2(0, TopBarHeight); barRt.anchoredPosition = Vector2.zero;
            var barFill = UIKit.Bar(bar.transform, "Fill", UIDesignTokens.SurfacePanel);
            Stretch(barFill.rectTransform);
            var barRule = UIKit.Bar(bar.transform, "Rule", UIDesignTokens.BorderColor());
            barRule.rectTransform.anchorMin = new Vector2(0, 0); barRule.rectTransform.anchorMax = new Vector2(1, 0);
            barRule.rectTransform.sizeDelta = new Vector2(0, 2);
            screen.Title = Text(bar.transform, "Nav_Title", title, 30, MatchHUD.MainText);
            Stretch(screen.Title.rectTransform, back == null ? 30 : 190, 0, 24, 0);
            screen.Title.alignment = TextAlignmentOptions.MidlineLeft;
            screen.Title.enableWordWrapping = false;
            screen.Title.enableAutoSizing = true; screen.Title.fontSizeMin = UiMetrics.MinTextUnits; screen.Title.fontSizeMax = UiMetrics.TextSize(30);
            if (back != null)
            {
                screen.Back = ButtonObject(bar.transform, "Nav_Back", "BACK", back);
                var backRt = screen.Back.GetComponent<RectTransform>();
                backRt.anchorMin = backRt.anchorMax = new Vector2(0, 0.5f); backRt.pivot = new Vector2(0, 0.5f);
                backRt.anchoredPosition = new Vector2(24, 0); backRt.sizeDelta = new Vector2(142, UiMetrics.MinTouchUnits);
            }

            // Status line pinned to the bottom.
            screen.Status = Text(root.transform, "Nav_Status", "", 19, MatchHUD.SecondaryText);
            screen.Status.alignment = TextAlignmentOptions.MidlineLeft;
            var statusRt = screen.Status.rectTransform;
            statusRt.anchorMin = new Vector2(0, 0); statusRt.anchorMax = new Vector2(1, 0); statusRt.pivot = new Vector2(0.5f, 0);
            statusRt.sizeDelta = new Vector2(-48, StatusHeight); statusRt.anchoredPosition = new Vector2(0, 4);
            screen.Status.enableAutoSizing = true; screen.Status.fontSizeMin = UiMetrics.MinTextUnits; screen.Status.fontSizeMax = UiMetrics.TextSize(19);
            var statusFill = UIKit.Bar(root.transform, "StatusFill", UIDesignTokens.SurfacePanel);
            statusFill.rectTransform.anchorMin = Vector2.zero; statusFill.rectTransform.anchorMax = new Vector2(1, 0);
            statusFill.rectTransform.pivot = new Vector2(0.5f, 0);
            statusFill.rectTransform.sizeDelta = new Vector2(0, StatusHeight + 8);
            statusFill.transform.SetSiblingIndex(screen.Status.transform.GetSiblingIndex());

            // Scrolling content between the bar and the status line.
            var scroll = new GameObject("Nav_Scroll", typeof(RectTransform), typeof(ScrollRect));
            scroll.transform.SetParent(root.transform, false);
            var scrollRt = scroll.GetComponent<RectTransform>();
            Stretch(scrollRt, 0, TopBarHeight, 0, StatusHeight + 4);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scroll.transform, false);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0); // catches drags for scrolling
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 1); contentRt.anchorMax = new Vector2(0.5f, 1); contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(ContentMaxWidth, 0); contentRt.anchoredPosition = new Vector2(0, -8);
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = Spacing; layout.padding = new RectOffset(0, 0, 4, 12);
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scrollRect = scroll.GetComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>(); scrollRect.content = contentRt;
            scrollRect.horizontal = false; scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scroll.AddComponent<ContentWidthFitter>().Bind(contentRt, scrollRt);
            screen.Content = contentRt;
            return screen;
        }

        /// <summary>A horizontal group of equal-width controls on one row.</summary>
        public static RectTransform Row(Transform parent, string name = "Row_Group")
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.spacing = Spacing; h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            row.GetComponent<LayoutElement>().minHeight = UiMetrics.MinTouchUnits;
            return row.GetComponent<RectTransform>();
        }

        public static Button AddButton(Transform parent, string caption, UnityAction action, string name = "Row_Button", float height = 84f,
            ButtonVariant variant = ButtonVariant.Secondary)
        {
            var obj = ButtonObject(parent, name, caption, action, variant);
            var element = obj.GetComponent<LayoutElement>();
            element.minHeight = UiMetrics.MinTouchUnits;
            element.preferredHeight = Mathf.Max(height, UiMetrics.MinTouchUnits);
            element.flexibleWidth = 1; element.preferredWidth = 0; // rows split evenly
            return obj.GetComponent<Button>();
        }

        /// <summary>A framed content card that participates in the menu's responsive layout.</summary>
        public static RectTransform Card(Transform parent, string name = "Row_Card", float height = 0f)
        {
            var card = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            card.transform.SetParent(parent, false);
            var element = card.GetComponent<LayoutElement>();
            element.flexibleWidth = 1; element.preferredWidth = 0;
            if (height > 0) element.preferredHeight = height;
            var layout = card.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8; layout.padding = new RectOffset(22, 22, 16, 16);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var fill = UIKit.Surface(card.transform, "Fill", UIDesignTokens.SurfacePanel);
            Stretch(fill.rectTransform);
            fill.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var frame = UIKit.Border(card.transform, "Frame", UIDesignTokens.BorderColor());
            frame.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return card.GetComponent<RectTransform>();
        }

        public static TextMeshProUGUI AddText(Transform parent, string value, float size, Color color, string name = "Row_Text")
        {
            var text = Text(parent, name, value, size, color);
            text.enableWordWrapping = true;
            var textElement = text.gameObject.AddComponent<LayoutElement>(); textElement.flexibleWidth = 1; textElement.preferredWidth = 0;
            return text;
        }

        public static TMP_InputField AddInput(Transform parent, string placeholder, int maxLength, string name = "Row_Input")
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
            root.transform.SetParent(parent, false);
            root.GetComponent<Image>().color = ButtonFill;
            root.GetComponent<Image>().sprite = UIRoundedSprites.RoundedRect(UIDesignTokens.Dp(UIDesignTokens.RadiusSmDp));
            root.GetComponent<Image>().type = Image.Type.Sliced;
            UIKit.Border(root.transform, "Frame", UIDesignTokens.BorderColor());
            var element = root.GetComponent<LayoutElement>();
            element.minHeight = UiMetrics.MinTouchUnits; element.preferredHeight = UiMetrics.MinTouchUnits + 4; element.flexibleWidth = 1; element.preferredWidth = 0;
            var area = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            area.transform.SetParent(root.transform, false);
            Stretch(area.GetComponent<RectTransform>(), 16, 6, 16, 6);
            var text = Text(area.transform, "Text", "", 24, MatchHUD.MainText);
            text.alignment = TextAlignmentOptions.MidlineLeft; Stretch(text.rectTransform);
            var hint = Text(area.transform, "Placeholder", placeholder, 22, MatchHUD.SecondaryText);
            hint.alignment = TextAlignmentOptions.MidlineLeft; Stretch(hint.rectTransform);
            var input = root.GetComponent<TMP_InputField>();
            input.textViewport = area.GetComponent<RectTransform>();
            input.textComponent = text; input.placeholder = hint; input.targetGraphic = root.GetComponent<Image>();
            input.characterLimit = maxLength; input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }

        static GameObject ButtonObject(Transform parent, string name, string caption, UnityAction action,
            ButtonVariant variant = ButtonVariant.Secondary)
        {
            var styled = UIKit.Button(parent, caption, variant, () =>
            {
                if (action == null) return;
                Hotdrop.Core.GameAudio.Play2D(Hotdrop.Core.GameCue.UiTap);
                action();
            }, name);
            var obj = styled.gameObject;
            var label = obj.GetComponentInChildren<TextMeshProUGUI>();
            label.textWrappingMode = TextWrappingModes.Normal;
            label.enableAutoSizing = true; label.fontSizeMin = UiMetrics.MinTextUnits; label.fontSizeMax = 26 * UiMetrics.TextScale;
            label.GetComponent<LayoutElement>().preferredHeight = caption.Contains("\n") ? 66f : 40f;
            return obj;
        }

        static TextMeshProUGUI Text(Transform parent, string name, string value, float size, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<TextMeshProUGUI>();
            text.text = value; text.fontSize = UiMetrics.TextSize(size);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = color == Color.white ? UIDesignTokens.TextPrimary : color;
            text.raycastTarget = false;
            return text;
        }

        public static void Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom); rt.offsetMax = new Vector2(-right, -top);
        }
    }

    /// <summary>Keeps the content column at most <see cref="MenuLayout.ContentMaxWidth"/> but never wider than the view.</summary>
    public class ContentWidthFitter : MonoBehaviour
    {
        RectTransform content, view;
        public float MaxWidth = MenuLayout.ContentMaxWidth;
        public void Bind(RectTransform contentRect, RectTransform viewRect) { content = contentRect; view = viewRect; Fit(); }
        void OnRectTransformDimensionsChange() => Fit();
        void LateUpdate() => Fit();
        void Fit()
        {
            if (content == null || view == null) return;
            float width = Mathf.Min(MaxWidth, view.rect.width - 48f);
            if (Mathf.Abs(content.sizeDelta.x - width) > 0.5f) content.sizeDelta = new Vector2(width, content.sizeDelta.y);
        }
    }
}


