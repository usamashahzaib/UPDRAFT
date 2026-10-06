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
        public static Color Background => UiPresentationProfile.Palette.Background;
        /// <summary>#1B2940 - raised instrument panels and cards.</summary>
        public static Color SurfacePanel => UiPresentationProfile.Palette.Surface;
        /// <summary>#263A59 - selected rows, active tabs, elevated overlays.</summary>
        public static Color SurfaceSelected => UiPresentationProfile.Palette.SurfaceSelected;
        /// <summary>#49627B - crisp structural borders and framing rails.</summary>
        public static Color SurfaceStructural => UiPresentationProfile.Palette.SurfaceStructural;
        /// <summary>#536176 - locked equipment and empty slots.</summary>
        public static Color SurfaceDisabled => UiPresentationProfile.Palette.Disabled;
        /// <summary>Profile concrete neutral for world-connected UI and structural badges.</summary>
        public static Color SurfaceConcrete => UiPresentationProfile.Palette.Concrete;
        /// <summary>Dim behind a modal. Not a palette colour: it is pure black at ModalScrimOpacity.</summary>
        public static readonly Color SurfaceScrim = new Color(0f, 0f, 0f, ModalScrimOpacity);

        // ---------------------------------------------------------------- signals

        /// <summary>Primary action signal; never use this token for team identity.</summary>
        public static Color PrimaryAction => UiPresentationProfile.Palette.PrimaryAction;
        /// <summary>Compatibility alias for older UI callers; new code should use PrimaryAction or Ally.</summary>
        public static Color ActionCyan => PrimaryAction;
        /// <summary>Pressed primary action tint.</summary>
        public static Color ActionCyanPressed => Color.Lerp(PrimaryAction, Background, 0.28f);
        /// <summary>Ally signal; kept separate from the primary action role.</summary>
        public static Color Ally => UiPresentationProfile.Palette.Ally;
        /// <summary>Enemies, danger, destructive actions.</summary>
        public static Color HazardCoral => UiPresentationProfile.Palette.Enemy;
        /// <summary>Pressed-danger tint derived from the selected profile's enemy signal.</summary>
        public static Color HazardCoralPressed => Color.Lerp(HazardCoral, Background, 0.28f);
        /// <summary>Objectives, rewards, contested zones.</summary>
        public static Color ObjectiveGold => UiPresentationProfile.Palette.Objective;
        /// <summary>#7ED68A - shields active, match ready.</summary>
        public static Color SignalSuccess => UiPresentationProfile.Palette.Success;
        /// <summary>#FFB45C - low ammunition, out of bounds, structural breach.</summary>
        public static Color SignalWarning => UiPresentationProfile.Palette.Warning;

        // ---------------------------------------------------------------- text

        /// <summary>#F4F7FB - primary labels and tabular stats.</summary>
        public static Color TextPrimary => UiPresentationProfile.Palette.TextPrimary;
        /// <summary>#BBC6D8 - sub-headers, equipment names, conditions.</summary>
        public static Color TextSecondary => UiPresentationProfile.Palette.TextSecondary;
        /// <summary>#7F8CA3 - metadata, units, timestamps, inactive rows.</summary>
        public static Color TextMuted => Color.Lerp(TextSecondary, Background, 0.28f);
        /// <summary>Dark canvas colour, used as text drawn on a filled accent surface.</summary>
        public static Color TextOnAccent => Background;

        // ---------------------------------------------------------------- opacity

        /// <summary>Panels float at 92% over the world (the match HUD must stay legible).</summary>
        public const float PanelOpacity = 0.92f;
        /// <summary>Disabled controls and inactive rows.</summary>
        public const float DisabledOpacity = 0.45f;
        /// <summary>Structural borders read at 60% so they frame without shouting.</summary>
        public const float BorderOpacity = 0.1f;
        /// <summary>Modal scrim over the world behind a dialog.</summary>
        public const float ModalScrimOpacity = 0.7f;
        /// <summary>Passive informational banners (offline) stay readable but recede.</summary>
        public const float BannerOpacity = 0.9f;

        /// <summary>The panel colour at <see cref="PanelOpacity"/>.</summary>
        public static Color PanelBg => WithAlpha(SurfacePanel, PanelOpacity);

        /// <summary>
        /// HUD glass: the floating panels that sit over the live 3D arena while a match runs.
        /// Deliberately more transparent than <see cref="PanelBg"/> — the reference composition
        /// reads the arena *through* the score bar, kill feed and health plate, and a player
        /// tracking a target through glass loses far less peripheral awareness than one staring
        /// at an opaque band. Dark enough to keep text legible over a bright sky.
        /// </summary>
        public const float HudGlassOpacity = 0.66f;

        /// <summary>The in-match floating panel colour, floated over the live scene.</summary>
        public static Color HudGlass => WithAlpha(SurfacePanel, HudGlassOpacity);

        // ---------------------------------------------------------------- typography
        // Roles mirror DESIGN.md's type scale. Sizes are dp, not canvas units.

        /// <summary>headline-xl - victory banners, mode title.</summary>
        public const float DisplayXLargeDp = 44f;
        /// <summary>headline-xl-mobile - the same role when reduced text is opted out.</summary>
        public const float DisplayLargeDp = 30f;
        /// <summary>headline-lg - screen titles.</summary>
        public const float TitleLargeDp = 30f;
        /// <summary>headline-lg-mobile - screen titles at the smaller mobile step.</summary>
        public const float TitleLargeMobileDp = 22f;
        /// <summary>headline-md - section headers.</summary>
        public const float TitleMediumDp = 22f;
        /// <summary>title-lg - emphasised body copy, list headers.</summary>
        public const float BodyLargeDp = 17f;
        /// <summary>body-lg - readable body text.</summary>
        public const float BodyDp = 14f;
        /// <summary>body-md - dense body text.</summary>
        public const float BodySmallDp = 14f;
        /// <summary>body-sm / label-caps - captions and tracked small-caps labels.</summary>
        public const float CaptionDp = 12f;
        /// <summary>label-data-lg - timers, scores, ammo.</summary>
        public const float DataLargeDp = 17f;
        /// <summary>label-data-md - secondary telemetry.</summary>
        public const float DataMediumDp = 14f;
        /// <summary>label-data-sm - smallest data row at the legibility floor.</summary>
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
        /// <summary>Maximum card inset from the current spacing token scale.</summary>
        public const float SpaceCardDp = 24f;

        // ---------------------------------------------------------------- shapes

        /// <summary>rounded-sm - inputs, chips, small buttons.</summary>
        public const float RadiusSmDp = 12f;
        /// <summary>rounded-lg - cards, panels, modal windows.</summary>
        public const float RadiusMdDp = 12f;
        /// <summary>rounded-xl - HUD shells and floating drawers.</summary>
        public const float RadiusLgDp = 12f;

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
        /// <summary>Button compression while pressed.</summary>
        public const float ButtonPressScale = 0.96f;

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
            UiTone.Friendly => Ally,
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
