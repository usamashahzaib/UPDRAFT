using UnityEngine;

namespace Hotdrop.UI
{
    /// <summary>Semantic meaning behind a colour. Code picks a tone, never a raw RGB triple.</summary>
    public enum UiTone { Neutral, Friendly, Enemy, Objective, Warning, Success, Disabled }

    /// <summary>
    /// The single source of truth for HOTDROP interface styling.
    ///
    /// Sources, in priority order:
    ///  - docs/AI_IMPLEMENTATION_PROMPTS/00_START_HERE.md standing rules (the shipping palette),
    ///  - UIUX/**/aerospace_utility_sport/DESIGN.md (roles, spacing rhythm, elevation tiers),
    ///  - the existing MatchHUD / MenuLayout colour literals, which this file supersedes.
    ///
    /// Nothing here re-implements a value that already lives in <see cref="UiMetrics"/>: the
    /// 48 dp / 12 dp floors and the dp-to-canvas-unit conversion stay owned by UiMetrics and are
    /// referenced, so the two files can never drift apart.
    ///
    /// Units: this project authors at <see cref="UiMetrics.DpPerUnit"/> dp per canvas unit.
    /// Design sizes quoted from DESIGN.md are in px, and px is read as dp on these phones, so
    /// use the <c>Dp()</c> helpers rather than hardcoding unit numbers.
    /// </summary>
    public static class UIDesignTokens
    {
        // ---------------------------------------------------------------- surfaces

        /// <summary>#101827 - the opaque tactical canvas behind every screen.</summary>
        public static readonly Color Background = Rgb(0x101827);
        /// <summary>#1B2940 - raised instrument panels and cards.</summary>
        public static readonly Color SurfacePanel = Rgb(0x1B2940);
        /// <summary>#263A59 - selected rows, active tabs, elevated overlays.</summary>
        public static readonly Color SurfaceSelected = Rgb(0x263A59);
        /// <summary>#49627B - crisp structural borders and framing rails.</summary>
        public static readonly Color SurfaceStructural = Rgb(0x49627B);
        /// <summary>#536176 - locked equipment and empty slots.</summary>
        public static readonly Color SurfaceDisabled = Rgb(0x536176);
        /// <summary>#C3B59E - weathered concrete neutral, structural badges.</summary>
        public static readonly Color SurfaceConcrete = Rgb(0xC3B59E);
        /// <summary>Dim behind a modal. Not a palette colour: it is pure black at ModalScrimOpacity.</summary>
        public static readonly Color SurfaceScrim = new Color(0f, 0f, 0f, ModalScrimOpacity);

        // ---------------------------------------------------------------- signals

        /// <summary>#30D5E8 - allies, primary actions, capture routes.</summary>
        public static readonly Color ActionCyan = Rgb(0x30D5E8);
        /// <summary>#138EA3 - pressed primary actions and shaded friendly fills.</summary>
        public static readonly Color ActionCyanPressed = Rgb(0x138EA3);
        /// <summary>#FF6376 - enemies, danger, destructive actions.</summary>
        public static readonly Color HazardCoral = Rgb(0xFF6376);
        /// <summary>#B93A50 - pressed destructive actions and contested markers.</summary>
        public static readonly Color HazardCoralPressed = Rgb(0xB93A50);
        /// <summary>#FFD166 - objectives, rewards, contested zones.</summary>
        public static readonly Color ObjectiveGold = Rgb(0xFFD166);
        /// <summary>#7ED68A - shields active, match ready.</summary>
        public static readonly Color SignalSuccess = Rgb(0x7ED68A);
        /// <summary>#FFB45C - low ammunition, out of bounds, structural breach.</summary>
        public static readonly Color SignalWarning = Rgb(0xFFB45C);

        // ---------------------------------------------------------------- text

        /// <summary>#F4F7FB - primary labels and tabular stats.</summary>
        public static readonly Color TextPrimary = Rgb(0xF4F7FB);
        /// <summary>#BBC6D8 - sub-headers, equipment names, conditions.</summary>
        public static readonly Color TextSecondary = Rgb(0xBBC6D8);
        /// <summary>#7F8CA3 - metadata, units, timestamps, inactive rows.</summary>
        public static readonly Color TextMuted = Rgb(0x7F8CA3);
        /// <summary>#101827 - text drawn on a filled accent surface.</summary>
        public static readonly Color TextOnAccent = Background;

        // ---------------------------------------------------------------- opacity

        /// <summary>Panels float at 92% over the world (the match HUD must stay legible).</summary>
        public const float PanelOpacity = 0.92f;
        /// <summary>Disabled controls and inactive rows.</summary>
        public const float DisabledOpacity = 0.45f;
        /// <summary>Structural borders read at 60% so they frame without shouting.</summary>
        public const float BorderOpacity = 0.6f;
        /// <summary>Modal scrim over the world behind a dialog.</summary>
        public const float ModalScrimOpacity = 0.7f;
        /// <summary>Passive informational banners (offline) stay readable but recede.</summary>
        public const float BannerOpacity = 0.9f;

        /// <summary>The panel colour at <see cref="PanelOpacity"/>.</summary>
        public static Color PanelBg => WithAlpha(SurfacePanel, PanelOpacity);

        // ---------------------------------------------------------------- typography
        // Roles mirror DESIGN.md's type scale. Sizes are dp, not canvas units.

        /// <summary>headline-xl - victory banners, mode title.</summary>
        public const float DisplayXLargeDp = 40f;
        /// <summary>headline-xl-mobile - the same role when reduced text is opted out.</summary>
        public const float DisplayLargeDp = 30f;
        /// <summary>headline-lg - screen titles.</summary>
        public const float TitleLargeDp = 28f;
        /// <summary>headline-lg-mobile - screen titles at the smaller mobile step.</summary>
        public const float TitleLargeMobileDp = 22f;
        /// <summary>headline-md - section headers.</summary>
        public const float TitleMediumDp = 20f;
        /// <summary>title-lg - emphasised body copy, list headers.</summary>
        public const float BodyLargeDp = 18f;
        /// <summary>body-lg - readable body text.</summary>
        public const float BodyDp = 16f;
        /// <summary>body-md - dense body text.</summary>
        public const float BodySmallDp = 14f;
        /// <summary>body-sm / label-caps - captions and tracked small-caps labels.</summary>
        public const float CaptionDp = 12f;
        /// <summary>label-data-lg - timers, scores, ammo.</summary>
        public const float DataLargeDp = 18f;
        /// <summary>label-data-md - secondary telemetry.</summary>
        public const float DataMediumDp = 14f;
        /// <summary>
        /// label-data-sm - smallest data row. Raised from DESIGN.md's 11 px because the
        /// ~12 dp floor in <see cref="UiMetrics.MinTextUnits"/> is a hard gate.
        /// </summary>
        public const float DataSmallDp = 12f;

        /// <summary>Wide tracking DESIGN.md asks for on short uppercase labels.</summary>
        public const float LabelTrackingEm = 0.08f;
        /// <summary>
        /// Text must survive being 30% wider than the designer measured (translation
        /// expansion, larger-text comfort option, longer localised strings) before it clips.
        /// </summary>
        public const float TextExpansionTolerance = 0.30f;
        /// <summary>Multiplier applied by the "larger text" comfort option.</summary>
        public const float LargerTextScale = 1.15f;

        // ---------------------------------------------------------------- spacing
        // DESIGN.md spacing scale, in dp.

        /// <summary>space-xs - 4 dp.</summary>
        public const float SpaceXsDp = 4f;
        /// <summary>space-sm - 8 dp.</summary>
        public const float SpaceSmDp = 8f;
        /// <summary>space-md - 12 dp, the default component padding.</summary>
        public const float SpaceMdDp = 12f;
        /// <summary>gutter - 16 dp, the grid gutter between sibling controls.</summary>
        public const float SpaceGutterDp = 16f;
        /// <summary>margin - 24 dp, the screen margin and grid gap.</summary>
        public const float SpaceMarginDp = 24f;
        /// <summary>space-lg - 20 dp, inside cards.</summary>
        public const float SpaceCardDp = 20f;

        // ---------------------------------------------------------------- shapes

        /// <summary>rounded-sm - inputs, chips, small buttons.</summary>
        public const float RadiusSmDp = 8f;
        /// <summary>rounded-lg - cards, panels, modal windows.</summary>
        public const float RadiusMdDp = 16f;
        /// <summary>rounded-xl - HUD shells and floating drawers.</summary>
        public const float RadiusLgDp = 24f;

        // ---------------------------------------------------------------- borders

        /// <summary>Inactive framing rails.</summary>
        public const float BorderThinUnits = 1f;
        /// <summary>Default element border.</summary>
        public const float BorderNormalUnits = 2f;
        /// <summary>Selected cards and active objective rings.</summary>
        public const float BorderBoldUnits = 3f;

        // ---------------------------------------------------------------- motion
        // Every duration sits inside the 180-240 ms band DESIGN.md asks for.

        /// <summary>180 ms - press feedback, badge swaps.</summary>
        public const float MotionQuickSeconds = 0.180f;
        /// <summary>200 ms - panels, toasts, screen transitions.</summary>
        public const float MotionStandardSeconds = 0.200f;
        /// <summary>240 ms - modals and the slowest legal transition.</summary>
        public const float MotionEmphasisSeconds = 0.240f;
        /// <summary>Shortest duration any role may use.</summary>
        public const float MotionMinSeconds = 0.180f;
        /// <summary>Longest duration any role may use.</summary>
        public const float MotionMaxSeconds = 0.240f;
        /// <summary>How far a card travels while entering, as a fraction of its own height.</summary>
        public const float MotionEnterSlideFraction = 0.04f;

        // ---------------------------------------------------------------- ergonomics

        /// <summary>DESIGN.md fixes a 48 dp horizontal margin for cutouts and curved bezels.</summary>
        public const float EdgeMarginDp = 48f;
        /// <summary>Reserved band under the gesture navigation area.</summary>
        public const float GestureInsetDp = 16f;
        /// <summary>Extra top room for a camera cutout on devices that report one.</summary>
        public const float TopCutoutInsetDp = 24f;
        /// <summary>Percent of screen width reserved for each thumb cluster.</summary>
        public const float ThumbErgonomicsFraction = 0.35f;

        // ---------------------------------------------------------------- helpers

        /// <summary>Exact 8-bit colour from a 0xRRGGBB literal, no float drift.</summary>
        public static Color Rgb(int rgb, float alpha = 1f) => new Color(
            ((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);

        /// <summary>Same hue at a different alpha.</summary>
        public static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        /// <summary>The signal colour a tone means. Disabled is muted, not merely transparent.</summary>
        public static Color ForTone(UiTone tone) => tone switch
        {
            UiTone.Friendly => ActionCyan,
            UiTone.Enemy => HazardCoral,
            UiTone.Objective => ObjectiveGold,
            UiTone.Warning => SignalWarning,
            UiTone.Success => SignalSuccess,
            UiTone.Disabled => SurfaceDisabled,
            _ => TextPrimary,
        };

        /// <summary>
        /// Text colour that stays readable on <paramref name="background"/>. Filled accent
        /// surfaces take the dark canvas colour; dark surfaces take primary text.
        /// </summary>
        public static Color TextOn(Color background)
        {
            float luminance = 0.2126f * background.r + 0.7152f * background.g + 0.0722f * background.b;
            return luminance > 0.45f ? TextOnAccent : TextPrimary;
        }

        /// <summary>Design px/dp expressed in canvas units.</summary>
        public static float Dp(float dp) => UiMetrics.UnitsForDp(dp);

        /// <summary>Border weight expressed in canvas units.</summary>
        public static float BorderUnits(float weight) => Mathf.Max(weight, 1f);

        /// <summary>The structural border colour at <see cref="BorderOpacity"/>.</summary>
        public static Color BorderColor(float alpha = BorderOpacity) => WithAlpha(SurfaceStructural, alpha);

        /// <summary>Hex string for tooling and tests, e.g. "30D5E8".</summary>
        public static string Hex(Color color) => ColorUtility.ToHtmlStringRGB(color);

        /// <summary>True when two colours agree well enough to be the same token.</summary>
        public static bool SameColor(Color a, Color b, float tolerance = 0.005f) =>
            Mathf.Abs(a.r - b.r) <= tolerance && Mathf.Abs(a.g - b.g) <= tolerance &&
            Mathf.Abs(a.b - b.b) <= tolerance && Mathf.Abs(a.a - b.a) <= tolerance;
    }
}
