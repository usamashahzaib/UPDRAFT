using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>
    /// A determinate progress bar with an optional tabular value readout.
    ///
    /// The fill is always paired with a number or a word, so progress is never communicated by
    /// bar length alone.
    /// </summary>
    public class ProgressBarView : MonoBehaviour
    {
        [SerializeField] RectTransform track;
        [SerializeField] RectTransform fill;
        [SerializeField] TextMeshProUGUI valueLabel;

        float value = 1f;

        /// <summary>Current fill from 0 to 1.</summary>
        public float Value
        {
            get => value;
            set
            {
                this.value = Mathf.Clamp01(value);
                if (fill != null && track != null)
                {
                    var size = fill.sizeDelta;
                    size.x = Mathf.Max(0f, track.rect.width * this.value);
                    fill.sizeDelta = size;
                }
            }
        }

        /// <summary>The readout, when the bar was built with one.</summary>
        public TextMeshProUGUI ValueLabel => valueLabel;

        /// <summary>Builds a progress bar. <paramref name="readout"/> adds a tabular percentage.</summary>
        public static ProgressBarView Create(Transform parent, string name = "ProgressBar", UiTone tone = UiTone.Friendly, bool readout = true)
        {
            var root = UIKit.Rect(parent, name, new Vector2(UIDesignTokens.Dp(280f), UIDesignTokens.Dp(24f)));
            var view = root.gameObject.AddComponent<ProgressBarView>();

            var trackImage = UIKit.Surface(root, "Track", UIDesignTokens.SurfacePanel, UIDesignTokens.RadiusSmDp);
            UIKit.Stretch(trackImage.rectTransform);
            view.track = trackImage.rectTransform;

            var fillImage = UIKit.Bar(trackImage.rectTransform, "Fill", UIDesignTokens.ForTone(tone));
            var fillRt = fillImage.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.sizeDelta = new Vector2(0f, 0f);
            view.fill = fillRt;

            if (readout)
            {
                var label = UIKit.CenteredLabel(trackImage.rectTransform, "Value", "0%", UiTextRole.DataSmall, tone);
                UIKit.Stretch(label.rectTransform, UIDesignTokens.Dp(UIDesignTokens.SpaceSmDp), 0f, UIDesignTokens.Dp(UIDesignTokens.SpaceSmDp), 0f);
                view.valueLabel = label;
            }

            view.Value = 1f;
            return view;
        }

        /// <summary>Sets the fill and, when present, the readout.</summary>
        public void SetValue(float normalized, string text = null)
        {
            Value = normalized;
            if (valueLabel != null) valueLabel.text = text ?? $"{Mathf.RoundToInt(Value * 100f)}%";
        }
    }

    /// <summary>
    /// A labelled slider with a tabular value readout.
    ///
    /// The track is taller than the visual line so the grab area still clears the 48 dp floor
    /// even though the line itself is thin.
    /// </summary>
    public class SliderView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI caption;
        [SerializeField] TextMeshProUGUI readout;
        [SerializeField] Slider slider;
        [SerializeField] RectTransform handleArea;

        /// <summary>The underlying slider.</summary>
        public Slider Slider => slider;

        /// <summary>The tabular readout beside the track.</summary>
        public TextMeshProUGUI Readout => readout;

        string numericFormat = "0";
        string suffix = "";

        /// <summary>Builds a slider.</summary>
        public static SliderView Create(Transform parent, string label, float min = 0f, float max = 1f,
            float initial = 1f, UiTone tone = UiTone.Neutral, string name = "Slider")
        {
            var root = UIKit.Rect(parent, name, new Vector2(UIDesignTokens.Dp(320f), UIDesignTokens.Dp(56f)));
            var view = root.gameObject.AddComponent<SliderView>();

            var row = UIKit.Row(root, "Row", UIDesignTokens.SpaceMdDp);
            UIKit.Stretch(row);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;

            view.caption = UIKit.Label(row, "Caption", label, UiTextRole.BodySmall);
            var captionElement = view.caption.gameObject.AddComponent<LayoutElement>();
            captionElement.preferredWidth = UIDesignTokens.Dp(120f);
            captionElement.minHeight = UiMetrics.MinTextUnits;

            view.readout = UIKit.Label(row, "Readout", "0", UiTextRole.DataMedium, tone == UiTone.Neutral ? UiTone.Neutral : tone);
            view.readout.alignment = TextAlignmentOptions.Right;
            var readoutElement = view.readout.gameObject.AddComponent<LayoutElement>();
            readoutElement.preferredWidth = UIDesignTokens.Dp(64f);

            // The grab strip stretches over the whole field, so the target is never thin even
            // though the visible track line is.
            var handleArea = UIKit.Stretch(root, "HandleArea");
            view.handleArea = handleArea;

            var background = UIKit.Surface(handleArea, "Track", UIDesignTokens.SurfacePanel, UIDesignTokens.RadiusSmDp);
            var trackRt = background.rectTransform;
            trackRt.anchorMin = new Vector2(0f, 0.5f);
            trackRt.anchorMax = new Vector2(1f, 0.5f);
            trackRt.sizeDelta = new Vector2(0f, UIDesignTokens.Dp(8f));

            var fillArea = UIKit.Rect(trackRt, "Fill Area", Vector2.zero);
            UIKit.Stretch(fillArea);
            var fill = UIKit.Bar(fillArea, "Fill", tone == UiTone.Neutral ? UIDesignTokens.PrimaryAction : UIDesignTokens.ForTone(tone));
            fill.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            fill.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.sizeDelta = new Vector2(0f, UIDesignTokens.Dp(8f));

            var handleAreaRect = UIKit.Rect(trackRt, "Handle Slide Area", Vector2.zero);
            UIKit.Stretch(handleAreaRect);
            var handle = UIKit.Surface(handleAreaRect, "Handle", UIDesignTokens.SurfaceConcrete, UIDesignTokens.RadiusSmDp);
            handle.rectTransform.sizeDelta = new Vector2(UIDesignTokens.Dp(24f), UIDesignTokens.Dp(24f));

            var slider = handleArea.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = initial;

            var element = root.gameObject.AddComponent<LayoutElement>();
            element.minHeight = UiMetrics.MinTouchUnits;

            view.slider = slider;
            slider.onValueChanged.AddListener(value => view.UpdateReadout(value));
            view.SetValue(initial);
            return view;
        }

        /// <summary>
        /// Splits a format like <c>"0%"</c> or <c>"0.0"</c> into a numeric format and a suffix, so
        /// the readout can show the live value rather than echoing the format string back.
        /// </summary>
        static void SplitFormat(string format, out string numeric, out string trailing)
        {
            format = string.IsNullOrEmpty(format) ? "0" : format;
            int i = 0;
            while (i < format.Length && (char.IsDigit(format[i]) || format[i] == '.' || format[i] == '#' || format[i] == ','))
                i++;
            numeric = i > 0 ? format.Substring(0, i) : "0";
            trailing = format.Substring(i);
        }

        void UpdateReadout(float value)
        {
            if (readout == null) return;
            readout.text = value.ToString(numericFormat) + suffix;
        }

        /// <summary>Sets the value and the readout format, e.g. <c>"0"</c> or <c>"0%"</c>.</summary>
        public void SetValue(float value, string format = "0")
        {
            SplitFormat(format, out numericFormat, out suffix);
            if (slider == null) return;
            slider.SetValueWithoutNotify(value);
            UpdateReadout(value);
        }
    }

    /// <summary>
    /// A switch with a mechanical thumb. The state is carried by the thumb's position and by the
    /// caption, not by colour alone.
    /// </summary>
    public class ToggleView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI caption;
        [SerializeField] RectTransform thumb;
        [SerializeField] RectTransform track;
        [SerializeField] Toggle toggle;

        /// <summary>The underlying toggle.</summary>
        public Toggle Toggle => toggle;

        /// <summary>Builds a toggle.</summary>
        public static ToggleView Create(Transform parent, string label, bool initial = false, string name = "Toggle")
        {
            var root = UIKit.Rect(parent, name, new Vector2(UIDesignTokens.Dp(320f), UiMetrics.MinTouchUnits));
            var view = root.gameObject.AddComponent<ToggleView>();

            var row = UIKit.Row(root, "Row", UIDesignTokens.SpaceMdDp);
            UIKit.Stretch(row);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;

            view.caption = UIKit.Label(row, "Caption", label, UiTextRole.Body);
            var captionElement = view.caption.gameObject.AddComponent<LayoutElement>();
            captionElement.flexibleWidth = 1f;
            captionElement.minHeight = UiMetrics.MinTouchUnits;

            // 48 x 28 dp recessed pill, per DESIGN.md's switch spec.
            var trackImage = UIKit.Surface(row, "Track", UIDesignTokens.Background, UIDesignTokens.RadiusSmDp);
            var trackRt = trackImage.rectTransform;
            trackRt.sizeDelta = new Vector2(UIDesignTokens.Dp(48f), UIDesignTokens.Dp(28f));
            var trackElement = trackImage.gameObject.AddComponent<LayoutElement>();
            trackElement.preferredWidth = UIDesignTokens.Dp(48f);
            trackElement.preferredHeight = UIDesignTokens.Dp(28f);
            trackElement.minWidth = UIDesignTokens.Dp(48f);
            view.track = trackRt;

            var thumbImage = UIKit.Surface(trackRt, "Thumb", UIDesignTokens.SurfaceConcrete, UIDesignTokens.RadiusSmDp);
            var thumbRt = thumbImage.rectTransform;
            thumbRt.sizeDelta = new Vector2(UIDesignTokens.Dp(20f), UIDesignTokens.Dp(20f));
            thumbRt.anchorMin = new Vector2(0f, 0.5f);
            thumbRt.anchorMax = new Vector2(0f, 0.5f);
            thumbRt.pivot = new Vector2(0.5f, 0.5f);
            thumbRt.anchoredPosition = new Vector2(UIDesignTokens.Dp(14f), 0f);
            view.thumb = thumbRt;

            var toggle = trackRt.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = trackImage;
            toggle.graphic = thumbImage;
            toggle.isOn = initial;
            view.toggle = toggle;
            toggle.onValueChanged.AddListener(view.ApplyState);

            var element = root.gameObject.AddComponent<LayoutElement>();
            element.minHeight = UiMetrics.MinTouchUnits;

            view.ApplyState(initial);
            return view;
        }

        /// <summary>Sets the value without firing listeners.</summary>
        public void SetValue(bool value)
        {
            if (toggle == null) return;
            toggle.SetIsOnWithoutNotify(value);
            ApplyState(value);
        }

        /// <summary>Current value.</summary>
        public bool Value => toggle != null && toggle.isOn;

        void ApplyState(bool on)
        {
            if (thumb != null && track != null)
            {
                float inset = UIDesignTokens.Dp(4f);
                float half = thumb.sizeDelta.x * 0.5f;
                // Before a layout group has run, rect.width is still zero; fall back to the
                // authored size so the thumb lands somewhere sane rather than in one corner.
                float width = track.rect.width > 0f ? track.rect.width : track.sizeDelta.x;
                float rest = inset + half;
                float engaged = Mathf.Max(rest, width - inset - half);
                var position = thumb.anchoredPosition;
                position.x = Mathf.Lerp(rest, engaged, on ? 1f : 0f);
                thumb.anchoredPosition = position;
            }
            if (thumb != null)
            {
                var image = thumb.GetComponent<Image>();
                if (image != null) image.color = on ? UIDesignTokens.PrimaryAction : UIDesignTokens.SurfaceConcrete;
            }
            if (caption != null) caption.color = on ? UIDesignTokens.TextPrimary : UIDesignTokens.TextSecondary;
        }
    }

    /// <summary>
    /// A single-line input with a placeholder, a character limit and a data-styled readout.
    /// The tap target is the full 48 dp field, not just the caret line.
    /// </summary>
    public class InputFieldView : MonoBehaviour
    {
        [SerializeField] TMP_InputField input;
        [SerializeField] TextMeshProUGUI label;

        /// <summary>The underlying field.</summary>
        public TMP_InputField Input => input;

        /// <summary>Current text.</summary>
        public string Value { get => input != null ? input.text : ""; set { if (input != null) input.text = value ?? ""; } }

        /// <summary>Builds an input field.</summary>
        public static InputFieldView Create(Transform parent, string label, string placeholder = "", int maxLength = 32, string name = "InputField")
        {
            var root = UIKit.Rect(parent, name, new Vector2(UIDesignTokens.Dp(320f), UiMetrics.MinTouchUnits));
            var view = root.gameObject.AddComponent<InputFieldView>();

            var fieldImage = UIKit.Surface(root, "Field", UIDesignTokens.Background, UIDesignTokens.RadiusSmDp);
            UIKit.Stretch(fieldImage.rectTransform);
            UIKit.Border(root, "Border", UIDesignTokens.BorderColor(), UIDesignTokens.BorderThinUnits);

            if (!string.IsNullOrEmpty(label))
            {
                view.label = UIKit.Label(root, "Label", label, UiTextRole.Caption, UiTone.Disabled);
                var labelRt = view.label.rectTransform;
                labelRt.anchorMin = labelRt.anchorMax = new Vector2(0f, 1f);
                labelRt.pivot = new Vector2(0f, 1f);
                labelRt.anchoredPosition = new Vector2(UIDesignTokens.Dp(UIDesignTokens.SpaceMdDp), -UIDesignTokens.Dp(UIDesignTokens.SpaceXsDp));
            }

            var viewport = UIKit.Stretch(root, "TextArea", UIDesignTokens.Dp(UIDesignTokens.SpaceMdDp), UIDesignTokens.Dp(UIDesignTokens.SpaceXsDp), UIDesignTokens.Dp(UIDesignTokens.SpaceMdDp), UIDesignTokens.Dp(UIDesignTokens.SpaceXsDp));
            viewport.gameObject.AddComponent<RectMask2D>();

            var text = UIKit.Label(viewport, "Text", "", UiTextRole.DataMedium);
            UIKit.Stretch(text.rectTransform);
            text.alignment = TextAlignmentOptions.MidlineLeft;

            var hint = UIKit.Label(viewport, "Placeholder", placeholder, UiTextRole.DataMedium, UiTone.Disabled);
            UIKit.Stretch(hint.rectTransform);
            hint.alignment = TextAlignmentOptions.MidlineLeft;

            var input = root.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = hint;
            input.targetGraphic = fieldImage;
            input.characterLimit = maxLength;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.caretColor = UIDesignTokens.PrimaryAction;
            view.input = input;

            var element = root.gameObject.AddComponent<LayoutElement>();
            element.minHeight = UiMetrics.MinTouchUnits;

            return view;
        }
    }
}
