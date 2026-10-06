using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Hotdrop.UI
{
    /// <summary>DESIGN.md's type roles. Sizes are dp; roles never map below the text floor.</summary>
    public enum UiTextRole
    {
        DisplayXLarge,
        DisplayLarge,
        TitleLarge,
        TitleMedium,
        BodyLarge,
        Body,
        BodySmall,
        Caption,
        DataLarge,
        DataMedium,
        DataSmall,
    }

    /// <summary>One role's resolved metrics.</summary>
    public struct TextStyle
    {
        public float designDp;      // the role's size at the 1x comfort scale
        public bool tabular;        // monospaced advance, for values that change every frame
        public bool uppercase;      // DESIGN.md wants short competitive labels in caps
        public float trackingEm;    // letter spacing in em

        public float SizeUnits => UiMetrics.TextSize(designDp);
    }

    /// <summary>
    /// Typography rules: one style per role, tabular numerals wherever digits change during play,
    /// a 30% expansion tolerance for translation and the larger-text option, and a floor of
    /// ~12 dp that no role or auto-size setting is allowed to breach.
    /// </summary>
    public static class UITypography
    {
        /// <summary>Resolves a role to its metrics at the current comfort scale.</summary>
        public static TextStyle Style(UiTextRole role) => role switch
        {
            UiTextRole.DisplayXLarge => new TextStyle { designDp = UIDesignTokens.DisplayXLargeDp, uppercase = true, trackingEm = UIDesignTokens.LabelTrackingEm },
            UiTextRole.DisplayLarge => new TextStyle { designDp = UIDesignTokens.DisplayLargeDp, uppercase = true, trackingEm = UIDesignTokens.LabelTrackingEm },
            UiTextRole.TitleLarge => new TextStyle { designDp = UIDesignTokens.TitleLargeDp, uppercase = true, trackingEm = UIDesignTokens.LabelTrackingEm },
            UiTextRole.TitleMedium => new TextStyle { designDp = UIDesignTokens.TitleMediumDp, uppercase = true },
            UiTextRole.BodyLarge => new TextStyle { designDp = UIDesignTokens.BodyLargeDp },
            UiTextRole.Body => new TextStyle { designDp = UIDesignTokens.BodyDp },
            UiTextRole.BodySmall => new TextStyle { designDp = UIDesignTokens.BodySmallDp },
            UiTextRole.Caption => new TextStyle { designDp = UIDesignTokens.CaptionDp, uppercase = true, trackingEm = UIDesignTokens.LabelTrackingEm },
            UiTextRole.DataLarge => new TextStyle { designDp = UIDesignTokens.DataLargeDp, tabular = true },
            UiTextRole.DataMedium => new TextStyle { designDp = UIDesignTokens.DataMediumDp, tabular = true },
            _ => new TextStyle { designDp = UIDesignTokens.DataSmallDp, tabular = true },
        };

        /// <summary>Every role, for tests and for the gallery's specimen column.</summary>
        public static readonly UiTextRole[] AllRoles =
        {
            UiTextRole.DisplayXLarge, UiTextRole.DisplayLarge, UiTextRole.TitleLarge, UiTextRole.TitleMedium,
            UiTextRole.BodyLarge, UiTextRole.Body, UiTextRole.BodySmall, UiTextRole.Caption,
            UiTextRole.DataLarge, UiTextRole.DataMedium, UiTextRole.DataSmall,
        };

        /// <summary>True when the role's digits must not jitter between updates.</summary>
        public static bool IsTabular(UiTextRole role) => Style(role).tabular;

        /// <summary>Applies a role's size, case, tracking and auto-size floor to a label.</summary>
        public static TextMeshProUGUI Apply(TextMeshProUGUI text, UiTextRole role, UiTone tone = UiTone.Neutral)
        {
            if (text == null) return null;
            var style = Style(role);
            float max = style.SizeUnits;
            text.fontSize = max;
            UIFonts.Use(text, UIFonts.ForRole(role));
            text.color = UIDesignTokens.ForTone(tone);
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Min(UiMetrics.MinTextUnits, max);
            text.fontSizeMax = max;
            if (style.uppercase) text.text = (text.text ?? "").ToUpperInvariant();
            text.characterSpacing = style.trackingEm * 100f; // TMP characterSpacing is a percentage of em
            return text;
        }

        /// <summary>Wraps a caption in tracked small-caps markup, as DESIGN.md asks for labels.</summary>
        public static string Caps(string value) => (value ?? "").ToUpperInvariant();

        /// <summary>
        /// Formats a clock as M:SS with zero-padded seconds so the string never changes width
        /// mid-match beyond the digit swap itself.
        /// </summary>
        public static string Clock(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{total / 60}:{total % 60:00}";
        }

        /// <summary>Formats ammo as current/mag, the form the HUD draws by the fire control.</summary>
        public static string Ammo(int current, int magazine) => $"{Mathf.Max(0, current)}/{Mathf.Max(0, magazine)}";

        /// <summary>
        /// The width a string may occupy, including the expansion tolerance the layout must
        /// survive (translation, the larger-text option, longer localised copy).
        /// </summary>
        public static float ExpandedWidth(float naturalWidth) =>
            Mathf.Max(0f, naturalWidth) * (1f + UIDesignTokens.TextExpansionTolerance);

        /// <summary>
        /// Largest size for <paramref name="designDp"/> that still fits <paramref name="availableWidth"/>
        /// once expanded, never below the ~12 dp floor.
        /// </summary>
        public static float SizeThatFits(float designDp, float naturalWidth, float availableWidth) =>
            UiMetrics.SizeWithinWidth(designDp, naturalWidth, availableWidth, UiMetrics.MinTextDp, UIDesignTokens.TextExpansionTolerance);
    }

    /// <summary>
    /// Gives every digit the same advance so a timer, score or ammo counter never shifts
    /// sideways while it counts down.
    ///
    /// The project's TMP font is proportional, so this measures the real per-digit advance once
    /// and pads the narrow digits with a monospace-space tag sized from the difference. That keeps
    /// the existing font asset (SIL OFL LiberationSans) rather than shipping a licensed mono font,
    /// and degrades to plain text if a font ever supplies real tabular figures.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    [ExecuteAlways]
    public class TabularNumbers : MonoBehaviour
    {
        const string First = "0123456789";
        const int MaxDigitsMeasured = 10;

        TextMeshProUGUI text;
        readonly float[] advances = new float[MaxDigitsMeasured];
        float maxAdvance;
        string applied = "";

        /// <summary>The label this component normalises.</summary>
        public TextMeshProUGUI Text
        {
            get { if (text == null) text = GetComponent<TextMeshProUGUI>(); return text; }
            set { text = value; applied = ""; }
        }

        /// <summary>The source string last written into the label, before padding.</summary>
        public string Source { get; private set; } = "";

        void OnEnable() => Normalize();
        void Update() { if (Text != null && Text.text != applied) Normalize(); }

        /// <summary>Re-measures the digits and rewrites the label with even advances.</summary>
        public void Normalize()
        {
            var label = Text;
            if (label == null) return;
            Source = label.text ?? "";
            Measure(label);
            string padded = Pad(Source, advances, maxAdvance, Mathf.Max(1f, label.fontSize));
            applied = padded;
            if (label.text != padded) label.text = padded;
        }

        void Measure(TextMeshProUGUI label)
        {
            // One measurement pass: render the digits alone and read their advances back.
            string original = label.text;
            label.text = First;
            label.ForceMeshUpdate();
            var info = label.textInfo;
            int count = Mathf.Min(MaxDigitsMeasured, info?.characterCount ?? 0);
            maxAdvance = 0f;
            for (int i = 0; i < count; i++)
            {
                float advance = info.characterInfo[i].xAdvance;
                advances[i] = advance;
                if (advance > maxAdvance) maxAdvance = advance;
            }
            label.text = original;
            if (maxAdvance <= 0f)
            {
                for (int i = 0; i < MaxDigitsMeasured; i++) advances[i] = 0f;
            }
        }

        /// <summary>
        /// Inserts a monospace-space of the shortfall after every digit narrower than
        /// <paramref name="maxAdvance"/>. Exposed as a pure function so the rule can be tested
        /// without a font.
        /// </summary>
        public static string Pad(string value, float[] digitAdvances, float maxAdvance, float fontSize)
        {
            if (string.IsNullOrEmpty(value) || digitAdvances == null || digitAdvances.Length < MaxDigitsMeasured || maxAdvance <= 0f || fontSize <= 0f)
                return value ?? "";

            var sb = new StringBuilder(value.Length + 16);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                sb.Append(c);
                int index = c - '0';
                if (index < 0 || index >= MaxDigitsMeasured) continue;
                float shortfall = maxAdvance - digitAdvances[index];
                if (shortfall <= 0.001f) continue;
                sb.Append("<mspace=").Append((shortfall / fontSize).ToString("0.####")).Append("em>");
            }
            return sb.ToString();
        }
    }
}
