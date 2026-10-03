using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Hotdrop.UI;

namespace Hotdrop.Editor
{
    /// <summary>
    /// Editor-only entry point to the UI component gallery.
    ///
    /// This file is compiled into the Hotdrop.Editor assembly, which the player build never
    /// includes, so the gallery cannot be opened from a shipped build by any route: not from the
    /// menu, not from a script, and not from player navigation.
    /// </summary>
    [InitializeOnLoad]
    public static class UIComponentGalleryMenu
    {
        const string AutoOpenKey = "Hotdrop.UIComponentGallery.OnPlay";

        static UIComponentGalleryMenu()
        {
            // Deferred: InitializeOnLoad runs before the editor is ready for menus.
            EditorApplication.delayCall += () =>
            {
                if (!UIComponentGallery.IsAvailable) return;
                Menu.SetChecked("Hotdrop/UI/Gallery/Open On Play", EditorPrefs.GetBool(AutoOpenKey, false));
            };
        }

        [MenuItem("Hotdrop/UI/Component Gallery", priority = 100)]
        public static void Open()
        {
            var view = UIComponentGallery.Open();
            if (view == null) return;
            Selection.activeGameObject = view.gameObject;
            SceneView.RepaintAll();
        }

        [MenuItem("Hotdrop/UI/Component Gallery", validate = true)]
        public static bool CanOpen() => UIComponentGallery.IsAvailable;

        [MenuItem("Hotdrop/UI/Gallery/Close", priority = 101)]
        public static void Close()
        {
            UIComponentGallery.Close();
            SceneView.RepaintAll();
        }

        [MenuItem("Hotdrop/UI/Gallery/Open On Play", priority = 102)]
        public static void ToggleOpenOnPlay()
        {
            bool next = !EditorPrefs.GetBool(AutoOpenKey, false);
            EditorPrefs.SetBool(AutoOpenKey, next);
            Menu.SetChecked("Hotdrop/UI/Gallery/Open On Play", next);
        }

        [MenuItem("Hotdrop/UI/Capture Gallery To PNG", priority = 120)]
        public static void Capture()
        {
            string directory = Path.Combine(Application.dataPath, "..", "UIEvidence");
            Directory.CreateDirectory(directory);
            foreach (var aspect in new[] { LandscapeAspect.SixteenByNine, LandscapeAspect.TwentyByNine })
            {
                string path = Path.Combine(directory, $"ui_gallery_{aspect}.png");
                UiGalleryCapture.Capture(aspect, path);
                Debug.Log($"[UIComponentGallery] wrote {path}");
            }
        }

        [MenuItem("Hotdrop/UI/Capture Gallery To PNG", validate = true)]
        public static bool CanCapture() => UIComponentGallery.IsAvailable;
    }

    /// <summary>
    /// Renders the development gallery offscreen at a chosen aspect ratio and writes a PNG.
    ///
    /// The gallery is built on a ScreenSpaceCamera canvas over an orthographic camera rather than
    /// in an overlay canvas, because an overlay canvas cannot be rendered into a RenderTexture. The
    /// canvas scale comes from <see cref="UiMetrics.ScaleFor"/> at a stated density, so the capture
    /// shows the same physical proportions a device would.
    /// </summary>
    public static class UiGalleryCapture
    {
        /// <summary>Reference device density for the capture (a 400 dpi ultrawide phone).</summary>
        public const float CaptureDpi = 400f;

        /// <summary>Temporary layout probe: logs the rect chain of the gallery sheet.</summary>
        public static void Diagnose()
        {
            int heightPx = 1080;
            float ratio = SafeAreaMath.Ratio(LandscapeAspect.SixteenByNine);
            int widthPx = Mathf.RoundToInt(heightPx * ratio);
            float scale = UiMetrics.ScaleFor(widthPx, heightPx, CaptureDpi);
            SafeAreaRoot.SimulatedScreen = new Vector4(widthPx, heightPx, CaptureDpi, 0f);
            Debug.Log($"[Diagnose] screenPx={widthPx}x{heightPx} scale={scale} canvasUnits={widthPx / scale}x{heightPx / scale}");

            var go = new GameObject("DiagCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = scale;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(widthPx / scale, heightPx / scale);

            var view = UIComponentGalleryView.Build(go.transform, LandscapeAspect.SixteenByNine);
            Canvas.ForceUpdateCanvases();
            Canvas.ForceUpdateCanvases();

            Log("root", view.transform);
            foreach (Transform child in view.transform) Log("  " + child.name, child);
            var safe = view.transform.Find("SafeAreaHost");
            if (safe != null) Log("    " + safe.name, safe);
            if (safe != null)
            {
                foreach (Transform c in safe) Log("      " + c.name, c);
                var scroll = safe.Find("Scroll");
                if (scroll != null) foreach (Transform c in scroll) Log("        " + c.name, c);
            }
            SafeAreaRoot.SimulatedScreen = null;
            Object.DestroyImmediate(go);
        }

        static void Log(string label, Transform t)
        {
            if (t == null) { Debug.Log($"[Diagnose] {label}: MISSING"); return; }
            var rt = t as RectTransform;
            if (rt == null) { Debug.Log($"[Diagnose] {label}: not a RectTransform"); return; }
            Debug.Log($"[Diagnose] {label}: sizeDelta={rt.sizeDelta} anchors={rt.anchorMin}-{rt.anchorMax} offsetMin={rt.offsetMin} offsetMax={rt.offsetMax} rect={rt.rect.size}");
        }

        /// <summary>Renders the gallery at <paramref name="aspect"/> and writes it to <paramref name="path"/>.</summary>
        public static void Capture(LandscapeAspect aspect, string path, int heightPx = 1080)
        {
            float ratio = SafeAreaMath.Ratio(aspect);
            int widthPx = Mathf.RoundToInt(heightPx * ratio);

            // Fully qualified: the project also has a "Camera" namespace in use.
            var cameraGo = new GameObject("GalleryCaptureCamera", typeof(UnityEngine.Camera));
            var camera = cameraGo.GetComponent<UnityEngine.Camera>();
            camera.orthographic = true;
            camera.orthographicSize = heightPx * 0.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UIDesignTokens.Background;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;

            float scale = UiMetrics.ScaleFor(widthPx, heightPx, CaptureDpi);

            var canvasGo = new GameObject("GalleryCaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = scale;

            // The gallery's safe-area maths needs a screen to read, so pin a simulated one.
            SafeAreaRoot.SimulatedScreen = new Vector4(widthPx, heightPx, CaptureDpi, 0f);

            var view = UIComponentGallery.Open(canvasGo.transform);
            if (view != null) view.SetAspect(aspect);

            Canvas.ForceUpdateCanvases();
            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(widthPx / scale, heightPx / scale);

            var target = new RenderTexture(widthPx, heightPx, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            camera.Render();
            camera.targetTexture = null;

            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(widthPx, heightPx, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, widthPx, heightPx), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;

            File.WriteAllBytes(path, texture.EncodeToPNG());

            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(canvasGo);
            Object.DestroyImmediate(cameraGo);
            SafeAreaRoot.SimulatedScreen = null;
        }
    }
}
