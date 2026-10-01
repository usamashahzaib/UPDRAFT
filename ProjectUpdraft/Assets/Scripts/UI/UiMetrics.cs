using UnityEngine;
using UnityEngine.UI;

namespace Updraft.UI
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
        public const float MinTouchUnits = 48f / DpPerUnit;   // 75 units = 48 dp
        public const float MinTextUnits = 12f / DpPerUnit;    // ~19 units = 12 dp
        public const float DesignWidth = 1280f;
        public const float DesignHeight = 580f;

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
