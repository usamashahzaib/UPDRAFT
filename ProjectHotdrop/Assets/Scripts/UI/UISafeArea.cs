using System;
using UnityEngine;

namespace Hotdrop.UI
{
    /// <summary>The landscape aspect ratios HOTDROP is required to lay out against.</summary>
    public enum LandscapeAspect
    {
        /// <summary>16:9 - the narrowest supported phone.</summary>
        SixteenByNine,
        /// <summary>18:9 - common mid-range Android.</summary>
        EighteenByNine,
        /// <summary>19.5:9 - the current mainstream ratio.</summary>
        NineteenPointFiveByNine,
        /// <summary>20:9 - the ultrawide DESIGN.md lays out against.</summary>
        TwentyByNine,
    }

    /// <summary>Cutout and gesture intrusions in pixels, as reported by the OS.</summary>
    [Serializable]
    public struct ScreenInsets
    {
        public float left, right, top, bottom;

        public static readonly ScreenInsets None = new ScreenInsets { left = 0, right = 0, top = 0, bottom = 0 };

        /// <summary>Insets from a <see cref="Rect"/> in pixels (the shape <c>Screen.safeArea</c> returns).</summary>
        public static ScreenInsets FromSafeArea(Rect screen, Rect safeArea)
        {
            var screenMax = new Vector2(Mathf.Max(screen.xMax, screen.xMin), Mathf.Max(screen.yMax, screen.yMin));
            var safeMax = new Vector2(Mathf.Max(safeArea.xMax, safeArea.xMin), Mathf.Max(safeArea.yMax, safeArea.yMin));
            return new ScreenInsets
            {
                left = Mathf.Max(0f, safeArea.xMin - screen.xMin),
                right = Mathf.Max(0f, screenMax.x - safeMax.x),
                top = Mathf.Max(0f, screenMax.y - safeMax.y),
                bottom = Mathf.Max(0f, safeArea.yMin - screen.yMin),
            };
        }

        /// <summary>True when nothing intrudes on the screen.</summary>
        public bool IsEmpty => left <= 0f && right <= 0f && top <= 0f && bottom <= 0f;
    }

    /// <summary>Content insets in canvas units, applied by <see cref="SafeAreaRoot"/>.</summary>
    [Serializable]
    public struct SafeAreaInsets
    {
        public float left, right, top, bottom;

        public static readonly SafeAreaInsets None = new SafeAreaInsets();

        public float Horizontal => left + right;
        public float Vertical => top + bottom;

        /// <summary>True when no inset is in force on any edge.</summary>
        public bool IsEmpty => left <= 0f && right <= 0f && top <= 0f && bottom <= 0f;
    }

    /// <summary>
    /// Pure safe-area arithmetic. Kept free of GameObjects so the rules can be exercised
    /// directly in EditMode tests at every supported aspect ratio.
    ///
    /// The rules, in order:
    ///  1. the OS cutout/gesture inset is always honoured, converted to canvas units;
    ///  2. on top of that, DESIGN.md's fixed 48 dp horizontal margin keeps thumbs off curved
    ///     bezels and camera islands;
    ///  3. a gesture-area reserve is added at the bottom even when the OS reports none;
    ///  4. insets never consume more than the screen, so content always keeps a positive area.
    /// </summary>
    public static class SafeAreaMath
    {
        /// <summary>Aspect ratio (width / height) for a supported landscape ratio.</summary>
        public static float Ratio(LandscapeAspect aspect) => aspect switch
        {
            LandscapeAspect.SixteenByNine => 16f / 9f,
            LandscapeAspect.EighteenByNine => 18f / 9f,
            LandscapeAspect.NineteenPointFiveByNine => 19.5f / 9f,
            _ => 20f / 9f,
        };

        /// <summary>Canvas units per screen pixel, from the device's real density.</summary>
        public static float ScaleFor(float widthPx, float heightPx, float dpi) =>
            UiMetrics.ScaleFor(widthPx, heightPx, dpi);

        /// <summary>
        /// Insets in canvas units for a screen of <paramref name="widthPx"/> x
        /// <paramref name="heightPx"/> at <paramref name="dpi"/> with the given OS intrusions.
        /// </summary>
        public static SafeAreaInsets Compute(float widthPx, float heightPx, float dpi, float scale,
            ScreenInsets osInsets,
            float edgeMarginDp = UIDesignTokens.EdgeMarginDp,
            float gestureInsetDp = UIDesignTokens.GestureInsetDp,
            float topCutoutDp = UIDesignTokens.TopCutoutInsetDp)
        {
            if (scale <= 0f) scale = ScaleFor(widthPx, heightPx, dpi);
            float unitsPerPx = 1f / scale;

            var insets = new SafeAreaInsets
            {
                left = Mathf.Max(0f, osInsets.left) * unitsPerPx,
                right = Mathf.Max(0f, osInsets.right) * unitsPerPx,
                top = Mathf.Max(0f, osInsets.top) * unitsPerPx,
                bottom = Mathf.Max(0f, osInsets.bottom) * unitsPerPx,
            };

            // DESIGN.md fixes a 48 dp lateral margin regardless of what the OS reports.
            float margin = UiMetrics.UnitsForDp(edgeMarginDp);
            insets.left = Mathf.Max(insets.left, margin);
            insets.right = Mathf.Max(insets.right, margin);

            // Never trust a device with no gesture bar to keep thumbs off the bottom edge.
            insets.bottom = Mathf.Max(insets.bottom, UiMetrics.UnitsForDp(gestureInsetDp));
            insets.top = Mathf.Max(insets.top, UiMetrics.UnitsForDp(topCutoutDp));

            return Clamp(widthPx * unitsPerPx, heightPx * unitsPerPx, insets);
        }

        /// <summary>Convenience overload using the density-derived scale for this screen.</summary>
        public static SafeAreaInsets Compute(float widthPx, float heightPx, float dpi, ScreenInsets osInsets,
            float edgeMarginDp = UIDesignTokens.EdgeMarginDp,
            float gestureInsetDp = UIDesignTokens.GestureInsetDp,
            float topCutoutDp = UIDesignTokens.TopCutoutInsetDp)
            => Compute(widthPx, heightPx, dpi, ScaleFor(widthPx, heightPx, dpi), osInsets,
                edgeMarginDp, gestureInsetDp, topCutoutDp);

        /// <summary>
        /// Insets for a supported landscape aspect held at <paramref name="heightPx"/> with the
        /// given cutout profile. Used by the gallery and by tests that need a whole aspect family
        /// without opening a device.
        /// </summary>
        public static SafeAreaInsets ForAspect(LandscapeAspect aspect, float heightPx, float dpi,
            ScreenInsets osInsets, float edgeMarginDp = UIDesignTokens.EdgeMarginDp,
            float gestureInsetDp = UIDesignTokens.GestureInsetDp)
        {
            float widthPx = Ratio(aspect) * heightPx;
            return Compute(widthPx, heightPx, dpi, osInsets, edgeMarginDp, gestureInsetDp);
        }

        /// <summary>
        /// A representative notch cutout for a landscape screen: a camera island eats into the
        /// short edge. <paramref name="side"/> -1 for the left edge, +1 for the right.
        /// </summary>
        public static ScreenInsets Notch(float screenHeightPx, float notchDepthDp, float dpi, int side = -1)
        {
            float dpPerPx = (dpi > 0f ? dpi : 160f) / 160f;
            float depthPx = notchDepthDp * dpPerPx;
            return side < 0
                ? new ScreenInsets { left = depthPx, right = 0, top = 0, bottom = 0 }
                : new ScreenInsets { left = 0, right = depthPx, top = 0, bottom = 0 };
        }

        /// <summary>Caps each edge so the inset can never eat the whole screen.</summary>
        public static SafeAreaInsets Clamp(float widthUnits, float heightUnits, SafeAreaInsets insets)
        {
            // Half the axis is the hard ceiling: content always keeps at least half, and in
            // practice far more, of the usable area.
            float maxHorizontal = widthUnits * 0.5f;
            float maxVertical = heightUnits * 0.5f;
            return new SafeAreaInsets
            {
                left = Mathf.Clamp(insets.left, 0f, maxHorizontal),
                right = Mathf.Clamp(insets.right, 0f, maxHorizontal),
                top = Mathf.Clamp(insets.top, 0f, maxVertical),
                bottom = Mathf.Clamp(insets.bottom, 0f, maxVertical),
            };
        }

        /// <summary>Usable content rect (canvas units) for a canvas of the given size and insets.</summary>
        public static Rect ContentRect(float widthUnits, float heightUnits, SafeAreaInsets insets) => new Rect(
            insets.left, insets.bottom, Mathf.Max(0f, widthUnits - insets.Horizontal), Mathf.Max(0f, heightUnits - insets.Vertical));

        /// <summary>
        /// True when a rect (canvas units, canvas origin at bottom-left) lies entirely inside the
        /// safe content rect with <paramref name="slackUnits"/> of tolerance for rounding.
        /// </summary>
        public static bool Contains(Rect content, Rect rect, float slackUnits = 1f) =>
            rect.xMin >= content.xMin - slackUnits && rect.xMax <= content.xMax + slackUnits &&
            rect.yMin >= content.yMin - slackUnits && rect.yMax <= content.yMax + slackUnits;
    }

    /// <summary>
    /// A full-screen container whose content is inset clear of cutouts, curved bezels and the
    /// gesture area. Any screen that later reuses this foundation parents its layout to
    /// <see cref="Content"/> rather than to the raw canvas.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class SafeAreaRoot : MonoBehaviour
    {
        [Tooltip("Optional child that receives the insets. Left empty, this object is inset in place.")]
        [SerializeField] RectTransform content;

        [Header("Reserve")]
        [Tooltip("DESIGN.md's fixed lateral margin that keeps thumbs off bezels.")]
        [SerializeField] float edgeMarginDp = UIDesignTokens.EdgeMarginDp;
        [Tooltip("Bottom band reserved for the system gesture area.")]
        [SerializeField] float gestureInsetDp = UIDesignTokens.GestureInsetDp;
        [Tooltip("Top reserve for a reported camera cutout.")]
        [SerializeField] float topCutoutDp = UIDesignTokens.TopCutoutInsetDp;

        /// <summary>Tests and the gallery pin a simulated screen instead of the real one.</summary>
        public static Vector4? SimulatedScreen;      // (width px, height px, dpi, unused)
        /// <summary>Simulated OS intrusions in pixels. Null uses <c>Screen.safeArea</c>.</summary>
        public static ScreenInsets? SimulatedInsets;

        RectTransform target;
        SafeAreaInsets applied;
        int lastWidth = -1, lastHeight = -1;
        bool applying;

        /// <summary>The child that carries the insets; created on demand.</summary>
        public RectTransform Content => target;

        /// <summary>Insets currently in force, in canvas units.</summary>
        public SafeAreaInsets Applied => applied;

        /// <summary>Insets this root would use right now, without applying them.</summary>
        public SafeAreaInsets Current()
        {
            float w = SimulatedScreen?.x ?? Screen.width;
            float h = SimulatedScreen?.y ?? Screen.height;
            float dpi = SimulatedScreen?.z ?? Screen.dpi;
            var os = SimulatedInsets ?? ReadOsInsets(w, h);
            return SafeAreaMath.Compute(w, h, dpi, os, edgeMarginDp, gestureInsetDp, topCutoutDp);
        }

        void OnEnable()
        {
            if (target == null) target = content != null ? content : (RectTransform)transform;
            Apply();
        }

        void OnRectTransformDimensionsChange()
        {
            // Apply() resizes this rect when it is its own content target, and resizing raises
            // this callback again. The guard breaks that loop; the change-only writes below keep
            // the editor from dirtying the scene on every repaint.
            if (applying || Application.isPlaying) return;
            Apply();
        }

        void Update()
        {
            float w = SimulatedScreen?.x ?? Screen.width;
            float h = SimulatedScreen?.y ?? Screen.height;
            if (w != lastWidth || h != lastHeight) Apply();
        }

        /// <summary>Re-reads the screen and pushes the insets onto <see cref="Content"/>.</summary>
        public void Apply()
        {
            if (applying) return;
            if (target == null) target = content != null ? content : (RectTransform)transform;
            float w = SimulatedScreen?.x ?? Screen.width;
            float h = SimulatedScreen?.y ?? Screen.height;
            float dpi = SimulatedScreen?.z ?? Screen.dpi;
            lastWidth = (int)w; lastHeight = (int)h;
            if (w <= 0f || h <= 0f) return;

            var os = SimulatedInsets ?? ReadOsInsets(w, h);
            var insets = SafeAreaMath.Compute(w, h, dpi, os, edgeMarginDp, gestureInsetDp, topCutoutDp);

            applying = true;
            try
            {
                applied = insets;
                if (target.anchorMin != Vector2.zero) target.anchorMin = Vector2.zero;
                if (target.anchorMax != Vector2.one) target.anchorMax = Vector2.one;
                if (target.pivot != new Vector2(0.5f, 0.5f)) target.pivot = new Vector2(0.5f, 0.5f);

                // Written unconditionally. A rect laid out while its parent was still zero-sized
                // keeps a stale sizeDelta, and skipping the write because the offsets "already
                // match" would leave it permanently wrong once the parent does get its size.
                // The re-entrancy flag above is what makes this safe to do every time.
                target.offsetMin = new Vector2(insets.left, insets.bottom);
                target.offsetMax = new Vector2(-insets.right, -insets.top);
            }
            finally
            {
                applying = false;
            }
        }

        static ScreenInsets ReadOsInsets(float w, float h)
        {
            Rect screen = new Rect(0, 0, w, h);
            Rect safe = Screen.safeArea;
            if (safe.width <= 0f || safe.height <= 0f) return ScreenInsets.None;
            return ScreenInsets.FromSafeArea(screen, safe);
        }
    }
}
