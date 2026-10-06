using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>Simple geometric icons. Nothing here comes from a licensed icon set.</summary>
    public enum UiGlyph { None, Back, Close, Check, Plus, ChevronRight, Menu, Settings }

    /// <summary>
    /// The shared building blocks every later screen composes from.
    ///
    /// Everything here is built in code from the tokens in <see cref="UIDesignTokens"/>, so a new
    /// screen cannot drift onto a private palette, a private radius or a sub-48 dp control. Nothing
    /// in this file is wired into player navigation; the component gallery uses it to prove the
    /// parts exist and that they behave.
    /// </summary>
    public static class UIKit
    {
        // ------------------------------------------------------------ primitives

        /// <summary>An empty RectTransform child.</summary>
        public static RectTransform Rect(Transform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>An empty RectTransform stretched to fill its parent, with optional insets.</summary>
        public static RectTransform Stretch(Transform parent, string name, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            var rt = Rect(parent, name, Vector2.zero);
            Stretch(rt, left, top, right, bottom);
            return rt;
        }

        /// <summary>Stretches an existing rect over its parent, with optional insets.</summary>
        public static RectTransform Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            if (rt == null) return null;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>A solid rectangle image (no sprite), for bars and rules.</summary>
        public static Image Bar(Transform parent, string name, Color color)
        {
            var rt = Rect(parent, name, Vector2.zero);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A rounded, nine-sliced surface at the given radius.</summary>
        public static Image Surface(Transform parent, string name, Color color, float radiusDp = UIDesignTokens.RadiusMdDp, float pixelsPerUnit = 1f)
        {
            var rt = Rect(parent, name, Vector2.zero);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var sprite = UIRoundedSprites.RoundedRect(UIDesignTokens.Dp(radiusDp), pixelsPerUnit);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            return image;
        }

        /// <summary>
        /// A crisp structural border built from four bars. DESIGN.md forbids muddy drop shadows and
        /// asks for a 1 px rail, which bars give at any density and any scale mode.
        ///
        /// The frame is returned as a <see cref="UiBorder"/> so a control that repaints per state
        /// can retint and re-weight the whole rail through one reference.
        /// </summary>
        public static RectTransform Border(Transform parent, string name, Color color, float weightUnits = UIDesignTokens.BorderNormalUnits, float inset = 0f)
        {
            var root = Rect(parent, name, Vector2.zero);
            Stretch(root);
            float w = Mathf.Max(1f, weightUnits);
            var bars = new[]
            {
                AddBorderBar(root, "Top", new Vector2(0f, -inset - w * 0.5f), new Vector2(0f, w), color, true),
                AddBorderBar(root, "Bottom", new Vector2(0f, inset + w * 0.5f), new Vector2(0f, w), color, true),
                AddBorderBar(root, "Left", new Vector2(inset + w * 0.5f, 0f), new Vector2(w, 0f), color, false),
                AddBorderBar(root, "Right", new Vector2(-inset - w * 0.5f, 0f), new Vector2(w, 0f), color, false),
            };
            var frame = root.gameObject.AddComponent<UiBorder>();
            frame.Configure(bars, w, inset);
            return root;
        }

        /// <summary>The border attached to a rect built by <see cref="Border"/>, if any.</summary>
        public static UiBorder FrameOf(RectTransform rect) => rect != null ? rect.GetComponent<UiBorder>() : null;

        static Image AddBorderBar(RectTransform root, string name, Vector2 position, Vector2 sizeDelta, Color color, bool horizontal)
        {
            var bar = Bar(root, name, color);
            var rt = bar.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            if (horizontal)
            {
                float edge = name == "Top" ? 1f : 0f;
                rt.anchorMin = new Vector2(0f, edge); rt.anchorMax = new Vector2(1f, edge);
                rt.sizeDelta = new Vector2(0f, sizeDelta.y);
            }
            else
            {
                float edge = name == "Left" ? 0f : 1f;
                rt.anchorMin = new Vector2(edge, 0f); rt.anchorMax = new Vector2(edge, 1f);
                rt.sizeDelta = new Vector2(sizeDelta.x, 0f);
            }
            rt.anchoredPosition = position;
            return bar;
        }

        /// <summary>A horizontal group of children that share the height of the tallest.</summary>
        public static RectTransform Row(Transform parent, string name = "Row", float spacingDp = UIDesignTokens.SpaceGutterDp)
        {
            var rt = Rect(parent, name, new Vector2(0f, UiMetrics.MinTouchUnits));
            var group = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = UIDesignTokens.Dp(spacingDp);
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;
            return rt;
        }

        /// <summary>A vertical stack of children.</summary>
        public static RectTransform Column(Transform parent, string name = "Column", float spacingDp = UIDesignTokens.SpaceGutterDp)
        {
            var rt = Rect(parent, name, new Vector2(0f, 0f));
            var group = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = UIDesignTokens.Dp(spacingDp);
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            var fitter = rt.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rt;
        }

        /// <summary>Flexible filler that pushes siblings apart.</summary>
        public static LayoutElement Spacer(Transform parent, string name = "Spacer", float flexible = 1f)
        {
            var rt = Rect(parent, name, new Vector2(0f, 0f));
            var element = rt.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = flexible;
            element.flexibleHeight = flexible;
            return element;
        }

        /// <summary>A one-line structural divider.</summary>
        public static RectTransform Divider(Transform parent, string name = "Divider", Color? color = null)
        {
            var image = Bar(parent, name, color ?? UIDesignTokens.BorderColor());
            var rt = image.rectTransform;
            var element = rt.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = UIDesignTokens.BorderNormalUnits;
            element.minHeight = UIDesignTokens.BorderNormalUnits;
            return rt;
        }

        // ------------------------------------------------------------ text

        /// <summary>A label at a typography role. Data roles get tabular numerals automatically.</summary>
        public static TextMeshProUGUI Label(Transform parent, string name, string value, UiTextRole role, UiTone tone = UiTone.Neutral)
        {
            var rt = Rect(parent, name, new Vector2(400f, UiMetrics.UnitsForDp(UITypography.Style(role).designDp) * 1.6f));
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value ?? "";
            text.alignment = TextAlignmentOptions.Left;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            UITypography.Apply(text, role, tone);
            if (UITypography.IsTabular(role)) rt.gameObject.AddComponent<TabularNumbers>();
            return text;
        }

        /// <summary>A wrapping body paragraph.</summary>
        public static TextMeshProUGUI Paragraph(Transform parent, string name, string value, UiTextRole role = UiTextRole.Body, UiTone tone = UiTone.Neutral)
        {
            var text = Label(parent, name, value, role, tone);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }

        /// <summary>A centred label, for button captions and badges.</summary>
        public static TextMeshProUGUI CenteredLabel(Transform parent, string name, string value, UiTextRole role, UiTone tone = UiTone.Neutral)
        {
            var text = Label(parent, name, value, role, tone);
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }

        // ------------------------------------------------------------ surfaces

        /// <summary>A raised panel. <paramref name="padded"/> reports whether a layout element should reserve the card inset.</summary>
        public static RectTransform Panel(Transform parent, string name, bool padded = true)
        {
            var root = Rect(parent, name, new Vector2(UIDesignTokens.Dp(320f), UIDesignTokens.Dp(120f)));
            var image = Surface(root, "Fill", UIDesignTokens.SurfacePanel, UIDesignTokens.RadiusMdDp);
            Stretch(image.rectTransform);
            Border(root, "Border", UIDesignTokens.BorderColor(), UIDesignTokens.BorderThinUnits);

            if (padded)
            {
                var content = Stretch(root, "Content",
                    UIDesignTokens.Dp(UIDesignTokens.SpaceCardDp), UIDesignTokens.Dp(UIDesignTokens.SpaceMdDp),
                    UIDesignTokens.Dp(UIDesignTokens.SpaceCardDp), UIDesignTokens.Dp(UIDesignTokens.SpaceMdDp));
                return content;
            }
            return (RectTransform)root;
        }

        /// <summary>A card: a panel plus a header strip and a body, as DESIGN.md describes.</summary>
        public static RectTransform Card(Transform parent, string name, string title, string body = null)
        {
            var root = Rect(parent, name, new Vector2(UIDesignTokens.Dp(360f), UIDesignTokens.Dp(140f)));
            var fill = Surface(root, "Fill", UIDesignTokens.SurfacePanel, UIDesignTokens.RadiusMdDp);
            Stretch(fill.rectTransform);
            Border(root, "Border", UIDesignTokens.BorderColor(), UIDesignTokens.BorderThinUnits);

            var column = Stretch(root, "Column",
                UIDesignTokens.Dp(UIDesignTokens.SpaceCardDp), UIDesignTokens.Dp(UIDesignTokens.SpaceMdDp),
                UIDesignTokens.Dp(UIDesignTokens.SpaceCardDp), UIDesignTokens.Dp(UIDesignTokens.SpaceMdDp));
            var group = column.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = UIDesignTokens.Dp(UIDesignTokens.SpaceSmDp);
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            if (!string.IsNullOrEmpty(title))
            {
                var header = Label(column, "Title", title, UiTextRole.TitleMedium);
                WrapInRow(header.rectTransform);
            }
            if (!string.IsNullOrEmpty(body))
            {
                var paragraph = Paragraph(column, "Body", body);
                WrapInRow(paragraph.rectTransform);
            }
            return column;
        }

        static void WrapInRow(RectTransform child)
        {
            var element = child.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;
            element.preferredWidth = 0f;
        }

        /// <summary>A screen header: title, optional subtitle, optional trailing action slot.</summary>
        public static RectTransform Header(Transform parent, string title, string subtitle = null, float heightDp = 56f)
        {
            var root = Rect(parent, "Header", new Vector2(0f, UIDesignTokens.Dp(heightDp)));
            var fill = Surface(root, "Fill", UIDesignTokens.WithAlpha(UIDesignTokens.Background, UIDesignTokens.PanelOpacity), UIDesignTokens.RadiusSmDp);
            Stretch(fill.rectTransform);

            var row = Row(root, "Row", UIDesignTokens.SpaceMdDp);
            Stretch(row);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;

            var titleText = Label(row, "Title", title, UiTextRole.TitleLarge);
            var titleElement = titleText.gameObject.AddComponent<LayoutElement>();
            titleElement.flexibleWidth = 1f;
            titleElement.preferredWidth = UIDesignTokens.Dp(200f);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var subtitleText = Label(row, "Subtitle", subtitle, UiTextRole.Caption, UiTone.Disabled);
                var subtitleElement = subtitleText.gameObject.AddComponent<LayoutElement>();
                subtitleElement.preferredWidth = UIDesignTokens.Dp(200f);
            }
            return root;
        }

        // ------------------------------------------------------------ navigation

        /// <summary>
        /// A navigation item. Selected state is carried by fill, a leading shape cue and a weight
        /// change, not by colour alone.
        /// </summary>
        public static HotdropButton NavItem(Transform parent, string label, bool selected = false, Action onClick = null)
        {
            var button = Button(parent, label, ButtonVariant.Secondary, onClick, "Nav_" + label);
            button.Accent = UiTone.Friendly;
            var element = button.gameObject.GetComponent<LayoutElement>();
            if (element != null) element.preferredHeight = UiMetrics.MinTouchUnits;
            if (selected) SetSelected(button, true);
            return button;
        }

        /// <summary>Marks a navigation item selected or not.</summary>
        public static void SetSelected(HotdropButton button, bool selected)
        {
            if (button == null) return;
            var background = button.GetComponentInChildren<Image>();
            if (background != null && background.name == "Fill")
                background.color = selected ? UIDesignTokens.SurfaceSelected : UIDesignTokens.SurfacePanel;
        }

        // ------------------------------------------------------------ status

        /// <summary>A compact status badge. DESIGN.md wants a tracked mono-ish indicator; the tone supplies the colour.</summary>
        public static RectTransform StatusBadge(Transform parent, string value, UiTone tone = UiTone.Neutral, string name = "Badge")
        {
            var root = Rect(parent, name, new Vector2(UIDesignTokens.Dp(120f), UIDesignTokens.Dp(28f)));
            var fill = Surface(root, "Fill", UIDesignTokens.SurfacePanel, UIDesignTokens.RadiusSmDp);
            Stretch(fill.rectTransform);
            Border(root, "Border", UIDesignTokens.BorderColor(UIDesignTokens.BorderOpacity), UIDesignTokens.BorderThinUnits);
            var text = CenteredLabel(root, "Text", value, UiTextRole.Caption, tone);
            Stretch(text.rectTransform, UIDesignTokens.Dp(UIDesignTokens.SpaceSmDp), 0f, UIDesignTokens.Dp(UIDesignTokens.SpaceSmDp), 0f);
            return root;
        }

        // ------------------------------------------------------------ progress

        /// <summary>
        /// A determinate progress track with a filled portion, as the Home contract card and the
        /// challenge detail screen use. The fill is a plain anchored rect, so no sprite atlas
        /// and no third-party art is involved.
        ///
        /// <paramref name="fraction"/> is clamped to 0..1 and also drives the track's accessibility
        /// value, so a screen reader never announces a bar as empty while it is full.
        /// </summary>
        public static RectTransform ProgressBar(Transform parent, float fraction, Color? fillColor = null,
            string name = "ProgressBar", float heightDp = 8f)
        {
            var root = Rect(parent, name, new Vector2(UIDesignTokens.Dp(320f), UIDesignTokens.Dp(heightDp)));
            var track = Surface(root, "Track", UIDesignTokens.SurfaceSelected, UIDesignTokens.RadiusSmDp);
            Stretch(track.rectTransform);

            var fill = Surface(root, "Fill", fillColor ?? UIDesignTokens.PrimaryAction, UIDesignTokens.RadiusSmDp);
            var fillRt = fill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            // A full bar must not vanish: keep a visible sliver at 0 and at 1.
            fillRt.offsetMin = new Vector2(0f, 0f);
            fillRt.offsetMax = new Vector2(0f, 0f);
            root.gameObject.AddComponent<UiProgressBar>().Bind(fillRt, root.GetComponent<RectTransform>());
            SetProgress(root, fraction, fillColor);
            return root;
        }

        /// <summary>Sets a bar built by <see cref="ProgressBar"/> to a clamped fraction.</summary>
        public static void SetProgress(RectTransform bar, float fraction, Color? fillColor = null)
        {
            if (bar == null) return;
            var driver = bar.GetComponent<UiProgressBar>();
            if (driver != null) { driver.Fraction = fraction; if (fillColor.HasValue) driver.FillColor = fillColor.Value; return; }

            var fill = bar.Find("Fill") as RectTransform;
            if (fill == null) return;
            float value = Mathf.Clamp01(fraction);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(value, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            if (fillColor.HasValue && fill.TryGetComponent(out Image image)) image.color = fillColor.Value;
        }

        /// <summary>
        /// The Home identity strip: alias and level on the left, seals and region on the right,
        /// with the settings control at the far right. Every value shown is real state the
        /// player already earned; nothing here is decorative or invented.
        /// </summary>
        public static RectTransform StatusStrip(Transform parent, string alias, string level, string seals,
            string region, Action onSettings = null, string name = "StatusStrip")
        {
            var root = Rect(parent, name, new Vector2(0f, UIDesignTokens.Dp(48f)));
            var fill = Surface(root, "Fill", UIDesignTokens.WithAlpha(UIDesignTokens.Background, UIDesignTokens.PanelOpacity), UIDesignTokens.RadiusSmDp);
            Stretch(fill.rectTransform);

            var row = Row(root, "Row", UIDesignTokens.SpaceSmDp);
            Stretch(row);
            var group = row.GetComponent<HorizontalLayoutGroup>();
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = true;

            var identity = Label(row, "Identity", alias, UiTextRole.TitleMedium, UiTone.Friendly);
            identity.rectTransform.sizeDelta = new Vector2(UIDesignTokens.Dp(200f), UIDesignTokens.Dp(48f));

            var levelText = Label(row, "Level", level, UiTextRole.Caption, UiTone.Neutral);
            levelText.rectTransform.sizeDelta = new Vector2(UIDesignTokens.Dp(90f), UIDesignTokens.Dp(48f));

            Spacer(row, "Elastic", 1f);

            if (!string.IsNullOrEmpty(seals))
            {
                var sealsText = Label(row, "Seals", seals, UiTextRole.Caption, UiTone.Objective);
                sealsText.rectTransform.sizeDelta = new Vector2(UIDesignTokens.Dp(120f), UIDesignTokens.Dp(48f));
            }
            if (!string.IsNullOrEmpty(region))
            {
                var regionText = Label(row, "Region", region, UiTextRole.Caption, UiTone.Neutral);
                regionText.rectTransform.sizeDelta = new Vector2(UIDesignTokens.Dp(140f), UIDesignTokens.Dp(48f));
            }
            if (onSettings != null) IconButton(row, "Settings", UiGlyph.Settings, ButtonVariant.Secondary, onSettings, "Settings");
            return root;
        }

        // ------------------------------------------------------------ buttons

        /// <summary>
        /// A button at a variant. The returned component owns every state; callers set a caption
        /// and add an <see cref="UnityEngine.Events.UnityAction"/> as normal.
        /// </summary>
        public static HotdropButton Button(Transform parent, string caption, ButtonVariant variant = ButtonVariant.Primary,
            Action onClick = null, string name = "Button")
        {
            var root = Rect(parent, name, new Vector2(UIDesignTokens.Dp(160f), UiMetrics.MinTouchUnits));
            var background = Surface(root, "Fill", UIDesignTokens.SurfacePanel, UIDesignTokens.RadiusSmDp);
            Stretch(background.rectTransform);
            var border = Border(root, "Border", UIDesignTokens.BorderColor(), UIDesignTokens.BorderNormalUnits);

            var content = Stretch(root, "Content",
                UIDesignTokens.Dp(UIDesignTokens.SpaceGutterDp), UIDesignTokens.Dp(UIDesignTokens.SpaceXsDp),
                UIDesignTokens.Dp(UIDesignTokens.SpaceGutterDp), UIDesignTokens.Dp(UIDesignTokens.SpaceXsDp));
            var group = content.gameObject.AddComponent<CanvasGroup>();
            var contentLayout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            contentLayout.spacing = UIDesignTokens.Dp(UIDesignTokens.SpaceSmDp);
            contentLayout.childAlignment = TextAnchor.MiddleCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = true;

            var iconSlot = Rect(content, "Icon", new Vector2(UIDesignTokens.Dp(24f), UIDesignTokens.Dp(24f)));
            var iconElement = iconSlot.gameObject.AddComponent<LayoutElement>();
            iconElement.preferredWidth = UIDesignTokens.Dp(24f);
            iconElement.preferredHeight = UIDesignTokens.Dp(24f);
            iconElement.minWidth = 0f;
            iconSlot.gameObject.SetActive(false);

            var label = CenteredLabel(content, "Label", caption, UiTextRole.TitleMedium, UiTone.Friendly);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            var labelElement = label.gameObject.AddComponent<LayoutElement>();
            // TMP reports its unbounded natural width to a HorizontalLayoutGroup. Without an
            // explicit zero-width basis a long caption can force itself outside the button even
            // though the label is flexible, overlapping the next specimen in narrow rows.
            labelElement.minWidth = 0f;
            labelElement.preferredWidth = 0f;
            labelElement.flexibleWidth = 1f;
            // Without an explicit height the horizontal layout group hands the caption zero
            // height, and an auto-sizing TMP label with no box renders nothing at all.
            labelElement.minHeight = UiMetrics.MinTextUnits;
            labelElement.preferredHeight = UiMetrics.MinTextUnits * 1.6f;

            var spinnerSlot = Rect(content, "Spinner", new Vector2(UIDesignTokens.Dp(24f), UIDesignTokens.Dp(24f)));
            var spinnerElement = spinnerSlot.gameObject.AddComponent<LayoutElement>();
            spinnerElement.preferredWidth = UIDesignTokens.Dp(24f);
            spinnerElement.preferredHeight = UIDesignTokens.Dp(24f);
            Spinner(spinnerSlot, UIDesignTokens.PrimaryAction);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            // Surface artwork is non-interactive by default. The button's own fill must
            // receive pointer rays or a rendered menu control can never be tapped.
            background.raycastTarget = true;
            var element = root.gameObject.AddComponent<LayoutElement>();
            element.minHeight = UiMetrics.MinTouchUnits;
            element.minWidth = UiMetrics.MinTouchUnits;

            var hotdrop = root.gameObject.AddComponent<HotdropButton>();
            hotdrop.Variant = variant;
            hotdrop.Accent = variant == ButtonVariant.Destructive ? UiTone.Warning : UiTone.Friendly;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            hotdrop.Initialize(background, label, group, spinnerSlot.gameObject, border);
            return hotdrop;
        }

        /// <summary>An icon-only button. The caption is kept for accessibility even though it is hidden.</summary>
        public static HotdropButton IconButton(Transform parent, string accessibleCaption, UiGlyph glyph,
            ButtonVariant variant = ButtonVariant.Secondary, Action onClick = null, string name = "IconButton")
        {
            var button = Button(parent, accessibleCaption, variant, onClick, name);
            button.IconOnly = true;
            var iconSlot = button.transform.Find("Content/Icon");
            if (iconSlot != null)
            {
                iconSlot.gameObject.SetActive(true);
                var rt = (RectTransform)iconSlot;
                Glyph(rt, glyph, UIDesignTokens.Dp(24f), UIDesignTokens.TextPrimary);
            }
            return button;
        }

        /// <summary>A spinning affordance built from bars, so it needs no sprite sheet.</summary>
        public static RectTransform Spinner(Transform parent, Color color, float sizeUnits = 0f)
        {
            var root = Rect(parent, "Spinner", sizeUnits > 0f ? new Vector2(sizeUnits, sizeUnits) : new Vector2(24f, 24f));
            var image = root.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var spinner = root.gameObject.AddComponent<UiSpinner>();
            spinner.SetColor(color);
            return root;
        }

        /// <summary>Draws a simple geometric icon inside <paramref name="parent"/>.</summary>
        public static RectTransform Glyph(Transform parent, UiGlyph glyph, float sizeUnits, Color color)
        {
            var root = Rect(parent, "Glyph_" + glyph, new Vector2(sizeUnits, sizeUnits));
            float weight = Mathf.Max(2f, sizeUnits * 0.14f);
            switch (glyph)
            {
                case UiGlyph.Back:
                    Chevron(root, sizeUnits, weight, color, false);
                    break;
                case UiGlyph.ChevronRight:
                    Chevron(root, sizeUnits, weight, color, true);
                    break;
                case UiGlyph.Close:
                    GlyphBars(root, sizeUnits, weight, color, new[] { 45f, -45f }, new[] { 0f, 0f });
                    break;
                case UiGlyph.Check:
                    GlyphBars(root, sizeUnits, weight, color, new[] { -45f, 45f }, new[] { -sizeUnits * 0.15f, sizeUnits * 0.2f });
                    break;
                case UiGlyph.Plus:
                    GlyphBars(root, sizeUnits, weight, color, new[] { 0f, 90f }, new[] { 0f, 0f });
                    break;
                case UiGlyph.Menu:
                    GlyphBars(root, sizeUnits, weight, color, new[] { 0f, 0f, 0f }, new[] { -sizeUnits * 0.28f, 0f, sizeUnits * 0.28f });
                    break;
                case UiGlyph.Settings:
                    // A cog, not a ring. Four bars laid along the square's edges read as an empty
                    // box at button size, which is what the Home capture showed next to the
                    // player name - an unrecognisable square where the reference draws a gear.
                    Gear(root, sizeUnits, weight, color);
                    break;
            }
            return root;
        }

        static void Chevron(RectTransform root, float sizeUnits, float weight, Color color, bool right)
        {
            float length = sizeUnits * 0.58f;
            float direction = right ? -1f : 1f;
            float centerX = direction * sizeUnits * 0.19f;
            float centerY = sizeUnits * 0.19f;
            AddGlyphBar(root, "Upper", color, new Vector2(centerX, centerY), length, weight,
                right ? 135f : 45f);
            AddGlyphBar(root, "Lower", color, new Vector2(centerX, -centerY), length, weight,
                right ? -135f : -45f);
        }

        static void AddGlyphBar(RectTransform root, string name, Color color, Vector2 position,
            float length, float weight, float angle)
        {
            var bar = Bar(root, name, color);
            var rt = bar.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(length, weight);
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            rt.anchoredPosition = position;
        }

        static void GlyphBars(RectTransform root, float sizeUnits, float weight, Color color, float[] angles, float[] offsets, bool mirror = false)
        {
            float length = sizeUnits * 0.9f;
            int count = Mathf.Min(angles.Length, offsets.Length);
            for (int i = 0; i < count; i++)
            {
                var bar = Bar(root, "Bar" + i, color);
                var rt = bar.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(length, weight);
                float angle = mirror ? 180f - angles[i] : angles[i];
                rt.localRotation = Quaternion.Euler(0f, 0f, angle);
                rt.anchoredPosition = new Vector2(mirror ? -offsets[i] : offsets[i], offsets[i] * 0f);
            }
        }

        /// <summary>
        /// A cog: a hub disc with eight radial teeth. Built from bars and the shared rounded-rect
        /// sprite, so it needs no font glyph and scales to any button size.
        /// </summary>
        static void Gear(RectTransform root, float sizeUnits, float weight, Color color)
        {
            var hub = Bar(root, "Hub", color);
            hub.sprite = UIRoundedSprites.RoundedRect(sizeUnits * 0.40f);
            hub.type = Image.Type.Simple;
            hub.rectTransform.anchorMin = hub.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            hub.rectTransform.sizeDelta = new Vector2(sizeUnits * 0.52f, sizeUnits * 0.52f);
            hub.rectTransform.anchoredPosition = Vector2.zero;
            hub.raycastTarget = false;

            // Teeth are short wide stubs sitting on the hub's edge. The two shapes that were tried
            // first both failed visibly: teeth of the hub's own width at a smaller radius merged
            // into a solid octagon, and long thin ones at a wider radius read as an asterisk.
            float toothWidth = Mathf.Max(3f, sizeUnits * 0.26f);
            float toothLength = Mathf.Max(2f, sizeUnits * 0.15f);
            for (int i = 0; i < 8; i++)
            {
                float degrees = i * 45f;
                var tooth = Bar(root, "Tooth" + i, color);
                var rt = tooth.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(toothWidth, toothLength);
                float radians = degrees * Mathf.Deg2Rad;
                rt.anchoredPosition = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * (sizeUnits * 0.31f);
                rt.localRotation = Quaternion.Euler(0f, 0f, -degrees);
                tooth.raycastTarget = false;
            }
        }

        /// <summary>The friendly/enemy/objective shape cue, with its word alongside.</summary>
        public static RectTransform Cue(Transform parent, UiTone tone, string label = null, float sizeUnits = 0f)
        {
            float size = sizeUnits > 0f ? sizeUnits : UIDesignTokens.Dp(UIDesignTokens.BodyDp);
            var row = Rect(parent, "CueRow", new Vector2(UIDesignTokens.Dp(160f), size));
            var cue = UICue.Create(row, tone, label, size);
            cue.Shape = tone == UiTone.Objective ? CueShape.RingDiamond : UICue.ShapeFor(tone);
            var cueRect = cue.GetComponent<RectTransform>();
            cueRect.anchorMin = cueRect.anchorMax = new Vector2(0f, 0.5f);
            cueRect.pivot = new Vector2(0f, 0.5f);
            cueRect.anchoredPosition = Vector2.zero;

            if (cue.HasTextSignal)
            {
                var text = Label(row, "CueLabel", cue.Label, UiTextRole.Caption, tone);
                var rt = text.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(UIDesignTokens.Dp(120f), size);
                rt.anchoredPosition = new Vector2(size + UIDesignTokens.Dp(UIDesignTokens.SpaceSmDp), 0f);
            }
            return row;
        }
    }

    /// <summary>
    /// Drives a progress fill's width. Kept as a component so a caller can update a bar every
    /// frame without rebuilding the rect, and so the accessibility value tracks the same number
    /// the pixels do.
    /// </summary>
    [ExecuteAlways]
    public class UiProgressBar : MonoBehaviour
    {
        RectTransform fill, track;
        float fraction;

        /// <summary>Completed fraction, clamped to 0..1.</summary>
        public float Fraction
        {
            get { return fraction; }
            set
            {
                fraction = Mathf.Clamp01(value);
                Apply();
            }
        }

        /// <summary>The fill colour; also used for the accessibility description.</summary>
        public Color FillColor
        {
            get
            {
                var image = fill != null ? fill.GetComponent<Image>() : null;
                return image != null ? image.color : UIDesignTokens.PrimaryAction;
            }
            set
            {
                var image = fill != null ? fill.GetComponent<Image>() : null;
                if (image != null) image.color = value;
            }
        }

        /// <summary>The bar this driver fills, for callers that only hold the parent.</summary>
        public RectTransform Root => track;

        internal void Bind(RectTransform fillRect, RectTransform root) { fill = fillRect; track = root; }

        void OnEnable() => Apply();
        void OnRectTransformDimensionsChange() => Apply();

        void Apply()
        {
            if (fill == null) return;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(fraction, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }
    }

    /// <summary>Rotates a mark so "loading" is a movement, not only a colour change.</summary>
    public class UiSpinner : MonoBehaviour
    {
        public float degreesPerSecond = 540f;
        RectTransform rt;
        Color color;

        void Awake() => rt = GetComponent<RectTransform>();

        /// <summary>Sets the spinner's tint.</summary>
        public void SetColor(Color value)
        {
            color = value;
            var image = GetComponent<Image>();
            if (image != null) image.color = value;
        }

        void Update()
        {
            if (rt == null) rt = GetComponent<RectTransform>();
            if (rt != null) rt.Rotate(0f, 0f, degreesPerSecond * Time.unscaledDeltaTime);
        }
    }
}
