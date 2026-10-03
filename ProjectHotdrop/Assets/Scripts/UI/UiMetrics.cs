using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>
    /// QA-019 / QA-020: physical sizing for every screen. The canvas scale is chosen from the
    /// device's real density so one canvas unit is ~0.64 dp; every touch target is at least
    /// <see cref="MinTouchUnits"/> (48 dp) and every text at least <see cref="MinTextUnits"/>
    /// (~12 dp) after scaling. Menus are authored to fit the <see cref="DesignWidth"/> x
    /// <see cref="DesignHeight"/> area; if a screen is physically smaller than that the scale
    /// shrinks to keep everything visible (and long lists scroll).
    /// </summary>
    public static class UiMetrics
    {
        public const float DpPerUnit = 0.64f;
        public const float DesignWidth = 1280f;
        public const float DesignHeight = 580f;

        /// <summary>The hard interactive-size gate: 48 dp, from the design system rules.</summary>
        public const float MinTouchDp = 48f;
        /// <summary>The hard legibility gate: ~12 dp, the smallest readable label.</summary>
        public const float MinTextDp = 12f;

        /// <summary>48 dp expressed in canvas units. Every tappable control is at least this tall.</summary>
        public const float MinTouchUnits = MinTouchDp / DpPerUnit;   // 75 units = 48 dp
        /// <summary>~12 dp expressed in canvas units. No label is ever rendered smaller.</summary>
        public const float MinTextUnits = MinTextDp / DpPerUnit;     // ~19 units = 12 dp

        /// <summary>Comfort option (Amendment 2): multiplies every label built after the setting changes.</summary>
        public static float TextScale = 1f;

        /// <summary>The size the "larger text" comfort option scales labels to.</summary>
        public const float LargerTextScale = UIDesignTokens.LargerTextScale;

        /// <summary>A design font size at the current comfort scale, never below the ~12 dp floor.</summary>
        public static float TextSize(float designSize) => Mathf.Max(designSize * TextScale, MinTextUnits);

        /// <summary>Design dp (DESIGN.md px) expressed in canvas units. The UI kit authors in dp.</summary>
        public static float UnitsForDp(float dp) => dp / DpPerUnit;

        /// <summary>Canvas units back in design dp, for layout maths at a known scale.</summary>
        public static float DpForUnits(float units) => units * DpPerUnit;

        /// <summary>Rounds a canvas-unit size up so it can never dip below <paramref name="dp"/>.</summary>
        public static float AtLeastDp(float units, float dp) => Mathf.Max(units, UnitsForDp(dp));

        /// <summary>
        /// Largest size no smaller than <paramref name="floorDp"/> dp that still fits
        /// <paramref name="naturalWidth"/> inside <paramref name="availableWidth"/> once the text
        /// has expanded by <paramref name="tolerance"/>. Returns <paramref name="designDp"/> unchanged
        /// when the text already fits with room to spare.
        /// </summary>
        public static float SizeWithinWidth(float designDp, float naturalWidth, float availableWidth, float floorDp = MinTextDp, float tolerance = UIDesignTokens.TextExpansionTolerance)
        {
            if (naturalWidth <= 0f || availableWidth <= 0f) return Mathf.Max(designDp, floorDp);
            float budget = availableWidth / Mathf.Max(1f, naturalWidth * (1f + tolerance));
            return Mathf.Max(floorDp, designDp * budget);
        }

        /// <summary>Pixels per canvas unit for a screen of the given size and density.</summary>
        public static float ScaleFor(float widthPx, float heightPx, float dpi)
        {
            float pxPerDp = (dpi > 0f ? dpi : 160f) / 160f;
            float density = pxPerDp * DpPerUnit;
            float fit = Mathf.Min(widthPx / DesignWidth, heightPx / DesignHeight);
            return Mathf.Max(0.01f, Mathf.Min(density, fit));
        }

        /// <summary>Size in dp of a length expressed in canvas units at the given scale.</summary>
        public static float UnitsToDp(float units, float scale, float dpi) => units * scale / ((dpi > 0f ? dpi : 160f) / 160f);
    }

    /// <summary>
    /// Raises every text to the ~12 dp floor and every button to the 48 dp floor under a root.
    /// Used for the scene-authored match HUD (menus built with <see cref="MenuLayout"/> already comply).
    /// </summary>
    public static class UiFloor
    {
        public static void Apply(Transform root)
        {
            foreach (var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (text.fontSize < UiMetrics.MinTextUnits) text.fontSize = UiMetrics.MinTextUnits;
                if (text.enableAutoSizing && text.fontSizeMin < UiMetrics.MinTextUnits) text.fontSizeMin = UiMetrics.MinTextUnits;
                if (text.enableAutoSizing && text.fontSizeMax < text.fontSizeMin) text.fontSizeMax = text.fontSizeMin;
            }
            foreach (var selectable in root.GetComponentsInChildren<Selectable>(true))
            {
                // Non-interactive selectables (e.g. a display-only health Slider) are not tap
                // targets, so the 48dp touch-target floor doesn't apply - only real buttons.
                if (!selectable.interactable) continue;
                var rt = selectable.GetComponent<RectTransform>();
                if (rt == null || rt.GetComponentInParent<LayoutGroup>() != null) continue; // layout-driven controls size themselves
                var size = rt.sizeDelta;
                if (rt.anchorMin.y == rt.anchorMax.y && size.y < UiMetrics.MinTouchUnits) size.y = UiMetrics.MinTouchUnits;
                if (rt.anchorMin.x == rt.anchorMax.x && size.x < UiMetrics.MinTouchUnits) size.x = UiMetrics.MinTouchUnits;
                rt.sizeDelta = size;
            }
        }
    }

    /// <summary>
    /// Replaces the fixed 1920x1080 reference scaling with <see cref="UiMetrics.ScaleFor"/>,
    /// re-evaluated whenever the resolution changes (rotation, split screen, resize).
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class DensityCanvasScaler : MonoBehaviour
    {
        CanvasScaler scaler;
        int lastWidth = -1, lastHeight = -1;

        /// <summary>Tests pin a simulated device; null uses the real screen.</summary>
        public static Vector3? SimulatedScreen; // (width px, height px, dpi)

        void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            Apply();
        }

        void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight) Apply();
        }

        public void Apply()
        {
            lastWidth = Screen.width; lastHeight = Screen.height;
            var s = SimulatedScreen ?? new Vector3(Screen.width, Screen.height, Screen.dpi);
            scaler.scaleFactor = UiMetrics.ScaleFor(s.x, s.y, s.z);
        }

        public static void Install(Canvas canvas)
        {
            if (canvas == null || canvas.GetComponent<DensityCanvasScaler>() != null) return;
            if (canvas.GetComponent<CanvasScaler>() == null) canvas.gameObject.AddComponent<CanvasScaler>();
            canvas.gameObject.AddComponent<DensityCanvasScaler>();
        }
    }
}

