using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>
    /// Screen sizing. One canvas unit is one CSS pixel of the UIUX reference screens (Stitch
    /// frames are 1280 px wide). The canvas is fitted to a 1280x720 design frame: 16:9 shows
    /// exactly that frame, wider phones (20:9) keep the 720 px height and gain width, which is
    /// how the references' flex layouts compose at those ratios.
    /// </summary>
    public static class UiMetrics
    {
        /// <summary>Display-independent pixels represented by one canvas unit at the current device scale.</summary>
        public static float DpPerUnit => CurrentPixelsPerCanvasUnit * 160f / CurrentDpi;
        public const float DesignWidth = 1280f;
        public const float DesignHeight = 720f;

        /// <summary>The hard interactive-size gate: 48 dp, from the design system rules.</summary>
        public const float MinTouchDp = 48f;
        /// <summary>The legibility floor for compact labels.</summary>
        public const float MinTextDp = 12f;

        /// <summary>48 dp expressed in canvas units at the current device scale.</summary>
        public static float MinTouchUnits => UnitsForDp(MinTouchDp);
        /// <summary>12 dp expressed in canvas units at the current device scale.</summary>
        public static float MinTextUnits => UnitsForDp(MinTextDp);

        /// <summary>Comfort option (Amendment 2): multiplies every label built after the setting changes.</summary>
        public static float TextScale = 1f;

        /// <summary>The size the "larger text" comfort option scales labels to.</summary>
        public const float LargerTextScale = UIDesignTokens.LargerTextScale;

        /// <summary>A design font size in dp, converted to canvas units and kept above the legibility floor.</summary>
        public static float TextSize(float designDp) => UnitsForDp(Mathf.Max(designDp * TextScale, MinTextDp));

        /// <summary>Scales an authored font size in dp to canvas units without changing its layout.</summary>
        public static float ScaleTextSize(float designDp) => TextSize(designDp);

        /// <summary>Design dp expressed in canvas units using current display density and canvas scale.</summary>
        public static float UnitsForDp(float dp) => dp * CurrentDpi / (160f * CurrentPixelsPerCanvasUnit);

        /// <summary>Canvas units back in design dp, for layout maths at the current device scale.</summary>
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
        public static float ScaleFor(float widthPx, float heightPx, float dpi) =>
            Mathf.Max(0.01f, Mathf.Min(widthPx / DesignWidth, heightPx / DesignHeight));

        /// <summary>Size in dp of a length expressed in canvas units at the given scale.</summary>
        public static float UnitsToDp(float units, float scale, float dpi) => units * scale / ((dpi > 0f ? dpi : 160f) / 160f);

        internal static Vector3 CurrentScreen
        {
            get
            {
                if (SafeAreaRoot.SimulatedScreen.HasValue)
                {
                    var simulated = SafeAreaRoot.SimulatedScreen.Value;
                    return new Vector3(simulated.x, simulated.y, simulated.z);
                }
                if (DensityCanvasScaler.SimulatedScreen.HasValue) return DensityCanvasScaler.SimulatedScreen.Value;
                float width = Screen.width > 0 ? Screen.width : DesignWidth;
                float height = Screen.height > 0 ? Screen.height : DesignHeight;
                return new Vector3(width, height, Screen.dpi);
            }
        }

        static float CurrentDpi
        {
            get
            {
                float dpi = CurrentScreen.z;
                return dpi > 0f && !float.IsNaN(dpi) && !float.IsInfinity(dpi) ? dpi : 160f;
            }
        }

        static float CurrentPixelsPerCanvasUnit
        {
            get
            {
                var screen = CurrentScreen;
                return ScaleFor(screen.x, screen.y, screen.z);
            }
        }
    }

    /// <summary>
    /// Raises rendered text to the 12 dp floor and interactive controls to the 48 dp floor under a root,
    /// accounting for nested transform scale. Used by both generated menus and the scene-authored HUD.
    /// </summary>
    public static class UiFloor
    {
        public static void Apply(Transform root)
        {
            if (root == null) return;
            var canvas = root.GetComponentInParent<Canvas>();
            if (canvas == null) canvas = root.GetComponentInChildren<Canvas>(true);
            var referenceCanvas = canvas != null && canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            var canvasTransform = referenceCanvas != null ? referenceCanvas.transform : root;
            float canvasScaleX = Mathf.Max(0.0001f, Mathf.Abs(canvasTransform.lossyScale.x));
            float canvasScaleY = Mathf.Max(0.0001f, Mathf.Abs(canvasTransform.lossyScale.y));

            foreach (var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                float textScaleY = Mathf.Max(0.0001f, Mathf.Abs(text.transform.lossyScale.y) / canvasScaleY);
                float minFontSize = UiMetrics.MinTextUnits / textScaleY;
                if (text.fontSize < minFontSize) text.fontSize = minFontSize;
                if (text.enableAutoSizing && text.fontSizeMin < minFontSize) text.fontSizeMin = minFontSize;
                if (text.enableAutoSizing && text.fontSizeMax < text.fontSizeMin) text.fontSizeMax = text.fontSizeMin;
            }
            foreach (var selectable in root.GetComponentsInChildren<Selectable>(true))
            {
                // Non-interactive selectables (e.g. a display-only health Slider) are not tap
                // targets, so the 48dp touch-target floor doesn't apply - only real buttons.
                if (!selectable.interactable) continue;
                var rt = selectable.GetComponent<RectTransform>();
                if (rt == null) continue;
                float touchWidth = UiMetrics.MinTouchUnits * canvasScaleX / Mathf.Max(0.0001f, Mathf.Abs(rt.lossyScale.x));
                float touchHeight = UiMetrics.MinTouchUnits * canvasScaleY / Mathf.Max(0.0001f, Mathf.Abs(rt.lossyScale.y));
                if (rt.GetComponentInParent<LayoutGroup>() != null)
                {
                    var element = rt.GetComponent<LayoutElement>();
                    if (element == null) element = rt.gameObject.AddComponent<LayoutElement>();
                    element.minWidth = Mathf.Max(element.minWidth, touchWidth);
                    element.minHeight = Mathf.Max(element.minHeight, touchHeight);
                    continue;
                }
                var size = rt.sizeDelta;
                if (rt.anchorMin.y == rt.anchorMax.y && size.y < touchHeight) size.y = touchHeight;
                if (rt.anchorMin.x == rt.anchorMax.x && size.x < touchWidth) size.x = touchWidth;
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
        float lastDpi = -1f;
        Vector3 lastMetrics;

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
            var metrics = UiMetrics.CurrentScreen;
            if (Screen.width != lastWidth || Screen.height != lastHeight ||
                !Mathf.Approximately(Screen.dpi, lastDpi) || metrics != lastMetrics) Apply();
        }

        public void Apply()
        {
            lastWidth = Screen.width; lastHeight = Screen.height; lastDpi = Screen.dpi;
            var s = UiMetrics.CurrentScreen;
            lastMetrics = s;
            scaler.scaleFactor = UiMetrics.ScaleFor(s.x, s.y, s.z);
            UiFloor.Apply(transform);
        }

        public static void Install(Canvas canvas)
        {
            if (canvas == null) return;
            if (canvas.GetComponent<DensityCanvasScaler>() == null)
            {
                if (canvas.GetComponent<CanvasScaler>() == null) canvas.gameObject.AddComponent<CanvasScaler>();
                canvas.gameObject.AddComponent<DensityCanvasScaler>();
            }
            if (canvas.GetComponent<UiPhysicalSizeFloor>() == null) canvas.gameObject.AddComponent<UiPhysicalSizeFloor>();
        }
    }

    /// <summary>Reapplies physical-size floors after UI generation or a display-size/density change.</summary>
    public sealed class UiPhysicalSizeFloor : MonoBehaviour
    {
        static bool dirty;
        Vector3 lastScreen = new Vector3(-1f, -1f, -1f);

        public static void MarkDirty() => dirty = true;

        void LateUpdate()
        {
            Vector3 screen = UiMetrics.CurrentScreen;
            if (!dirty && screen == lastScreen) return;
            dirty = false;
            lastScreen = screen;
            UiFloor.Apply(transform);
        }
    }
}

