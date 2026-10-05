using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Hotdrop.Input;

namespace Hotdrop.UI
{
    /// <summary>
    /// Draws the in-match touch controls as real Unity UI.
    ///
    /// <para><b>Why this exists.</b> The controls used to be drawn from
    /// <c>TouchInputManager.OnGUI</c> with immediate-mode calls. Two things followed from that,
    /// both of them wrong. First, IMGUI draws in the game view's own pass, so a capture taken by
    /// rendering the HUD canvas into a RenderTexture - which is how this project produces its
    /// screen evidence - photographed a match with no joystick, no FIRE cluster and no pad button
    /// on it, and every parity review of screens 04/08/09 was comparing a reference with a frame
    /// that had never contained the controls. Second, the standing rules for this project ask for
    /// TextMeshPro + Unity UI, and IMGUI cannot be scaled by the canvas, re-tinted by a design
    /// token, or reached by a layout test.</para>
    ///
    /// <para><b>What this class does and does not own.</b> It owns pixels only. Every rectangle it
    /// draws comes from <see cref="TouchInputManager"/>'s own button rects, and every touch is
    /// still routed by <c>TouchInputManager.ProcessTouch</c> against those same rects. Nothing
    /// here hit-tests, so switching the controls to uGUI cannot change what a tap does: the
    /// images are all <c>raycastTarget = false</c> and sit outside the input path entirely.</para>
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class TouchControlsView : MonoBehaviour
    {
        TouchInputManager input;
        RectTransform canvasRect;
        Canvas canvas;

        static Sprite discSprite;
        static Sprite ringSprite;

        RectTransform fireDisc, jumpDisc, padDisc, reloadDisc, pingDisc, pauseDisc;
        TextMeshProUGUI fireLabel, jumpLabel, padLabel, reloadLabel, pingLabel, pauseLabel;
        Image fireFill, jumpFill, padFill, reloadFill, pingFill, pauseFill;
        Image fireBorder, jumpBorder, padBorder, reloadBorder, pingBorder, pauseBorder;

        RectTransform crosshair;
        RectTransform joystickBase, joystickNub;
        Image joystickNubFill;
        RectTransform padPreview;
        TextMeshProUGUI padPreviewLabel;

        RectTransform editorRoot;
        readonly RectTransform[] editorActions = new RectTransform[6];
        readonly Image[] editorActionFills = new Image[6];
        readonly TextMeshProUGUI[] editorActionLabels = new TextMeshProUGUI[6];
        TextMeshProUGUI editorHint;
        Image editorSelectionImage;
        RectTransform editorSelection;

        /// <summary>Creates the view under a canvas if one is not already there.</summary>
        public static TouchControlsView Ensure(Canvas canvas, TouchInputManager input)
        {
            if (canvas == null || input == null) return null;
            var existing = canvas.GetComponentInChildren<TouchControlsView>(true);
            if (existing != null) { existing.Bind(canvas, input); return existing; }
            var rt = UIKit.Rect(canvas.transform, "TouchControls", Vector2.zero);
            UIKit.Stretch(rt);
            var view = rt.gameObject.AddComponent<TouchControlsView>();
            view.Bind(canvas, input);
            return view;
        }

        void Bind(Canvas targetCanvas, TouchInputManager source)
        {
            canvas = targetCanvas;
            canvasRect = targetCanvas.GetComponent<RectTransform>();
            input = source;
            Build();
        }

        /// <summary>
        /// Re-reads the input's geometry and re-places every control. Public so an exporter that
        /// pins the frame size in one frame can ask for the frame's own layout in the same frame.
        /// </summary>
        public void RefreshForComposedFrame()
        {
            if (input == null) return;
            input.RecalculateForComposedFrame();
            Refresh();
        }

        // ------------------------------------------------------------ sprites

        /// <summary>
        /// A soft filled circle. This is the same radial alpha the immediate-mode version painted,
        /// generated once and shared, so the discs keep the shape players already learned.
        /// </summary>
        static Sprite DiscSprite()
        {
            if (discSprite != null) return discSprite;
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "HotdropControlDisc",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var pixels = new Color32[size * size];
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    float alpha = Mathf.Clamp01(radius - distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            texture.SetPixels32(pixels); texture.Apply();
            discSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, Vector4.zero);
            discSprite.name = "HotdropControlDisc";
            discSprite.hideFlags = HideFlags.HideAndDontSave;
            return discSprite;
        }

        /// <summary>A stroked circle, for the reference's ring-outlined controls.</summary>
        static Sprite RingSprite()
        {
            if (ringSprite != null) return ringSprite;
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "HotdropControlRing",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var pixels = new Color32[size * size];
            float radius = size * 0.5f - 2f;
            float weight = size * 0.055f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f));
                    float alpha = Mathf.Clamp01(weight - Mathf.Abs(distance - radius) + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            texture.SetPixels32(pixels); texture.Apply();
            ringSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, Vector4.zero);
            ringSprite.name = "HotdropControlRing";
            ringSprite.hideFlags = HideFlags.HideAndDontSave;
            return ringSprite;
        }

        // ------------------------------------------------------------ build

        // UIUX screen 08 palette for the action cluster.
        static readonly Color Primary = UiDraw.Hex("#7cefff");
        static readonly Color OnPrimaryContainer = UiDraw.Hex("#005862");
        static readonly Color High = UiDraw.Hex("#222a3a");
        static readonly Color OnSurfaceVariant = UiDraw.Hex("#bbc9cb");
        static readonly Color Outline = UiDraw.Hex("#859396");
        static readonly Color Tertiary = UiDraw.Hex("#ffda88");

        Image jumpPlate, jumpGlow;
        TextMeshProUGUI jumpIcon, reloadWeapon, reloadAmmo, reloadIcon;
        RectTransform joystickRest;

        void Build()
        {
            if (canvasRect == null || input == null) return;
            BuildDisc("Fire", out fireDisc, out fireFill, out fireBorder, out fireLabel);
            BuildDisc("Jump", out jumpDisc, out jumpFill, out jumpBorder, out jumpLabel);
            BuildDisc("Pad", out padDisc, out padFill, out padBorder, out padLabel);
            BuildDisc("Reload", out reloadDisc, out reloadFill, out reloadBorder, out reloadLabel);
            BuildDisc("Ping", out pingDisc, out pingFill, out pingBorder, out pingLabel);
            BuildDisc("Pause", out pauseDisc, out pauseFill, out pauseBorder, out pauseLabel);

            // FIRE: 80 px rounded-2xl, cyan gradient with its shadow (rasterised), martial-arts glyph.
            var fireArt = UiDraw.Art(fireDisc, "Face", "hud_fire", Vector2.zero);
            UiDraw.Place(fireArt.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -20f), new Vector2(160f, 160f));
            UiDraw.Place(UiDraw.Icon(fireDisc, "Icon", "sports_martial_arts", 34f, OnPrimaryContainer).rectTransform,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 8f), new Vector2(34f, 34f));
            fireLabel.transform.SetAsLastSibling();
            Caption(fireLabel, UiFont.Heading, 13f, OnPrimaryContainer, -.05f, -20f);

            // LAUNCH / JUMP: 64 px rounded-2xl; cyan with a glow when a pad can launch.
            jumpGlow = UiDraw.Glow(jumpDisc, UiDraw.Hex("#7cefff", .25f), 16f, 20f);
            jumpPlate = UiDraw.Box(jumpDisc, "Plate", UiDraw.Hex("#30d5e8"), 16f);
            UiDraw.Place(jumpPlate.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(64f, 64f));
            jumpIcon = UiDraw.Icon(jumpDisc, "Icon", "keyboard_double_arrow_up", 28f, OnPrimaryContainer);
            UiDraw.Place(jumpIcon.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 7f), new Vector2(28f, 28f));
            jumpLabel.transform.SetAsLastSibling();
            Caption(jumpLabel, UiFont.Heading, 10f, OnPrimaryContainer, .05f, -16f);

            // PAD: 56 px rounded-xl, gold take-off glyph and status.
            Plate(padDisc, 56f, 12f);
            UiDraw.Place(UiDraw.Icon(padDisc, "Icon", "flight_takeoff", 22f, Tertiary).rectTransform,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 6f), new Vector2(22f, 22f));
            padLabel.transform.SetAsLastSibling();
            Caption(padLabel, UiFont.MonoMedium, 9f, Tertiary, 0f, -14f);

            // RELOAD: weapon + magazine, sync glyph, live ammo.
            Plate(reloadDisc, 56f, 12f);
            reloadWeapon = UiDraw.Text(reloadDisc, "Weapon", "", UiFont.Mono, 10f, Outline, .05f, TextAlignmentOptions.Midline);
            UiDraw.Place(reloadWeapon.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 15f), new Vector2(56f, 14f));
            reloadIcon = UiDraw.Icon(reloadDisc, "Icon", "sync", 16f, Primary);
            UiDraw.Place(reloadIcon.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 0f), new Vector2(16f, 16f));
            reloadAmmo = reloadLabel;
            reloadLabel.transform.SetAsLastSibling();
            Caption(reloadLabel, UiFont.MonoMedium, 9f, OnSurfaceVariant, .05f, -15f);

            // Ping (explore) and menu (settings): 40 px rounded-lg in the top-right row.
            Plate(pingDisc, 40f, 8f, 1f);
            UiDraw.Place(UiDraw.Icon(pingDisc, "Icon", "explore", 20f, Primary).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(20f, 20f));
            Plate(pauseDisc, 40f, 8f, 1f);
            UiDraw.Place(UiDraw.Icon(pauseDisc, "Icon", "settings", 20f, OnSurfaceVariant).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(20f, 20f));
            pingLabel.gameObject.SetActive(false);
            pauseLabel.gameObject.SetActive(false);

            // Crosshair: 6 px dot, four 2x8 ticks 32 px out, primary/80.
            crosshair = UIKit.Rect(transform, "Crosshair", new Vector2(80f, 80f));
            UiDraw.Place(UiDraw.Dot(crosshair, "Dot", 6f, Primary).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(6f, 6f));
            for (int i = 0; i < 4; i++)
            {
                var tick = UiDraw.Box(crosshair, "Tick" + i, UiDraw.Hex("#7cefff", .8f), 1f).rectTransform;
                tick.anchorMin = tick.anchorMax = new Vector2(.5f, .5f);
                tick.sizeDelta = i < 2 ? new Vector2(2f, 8f) : new Vector2(8f, 2f);
                tick.anchoredPosition = i switch { 0 => new Vector2(0f, 32f), 1 => new Vector2(0f, -32f), 2 => new Vector2(-32f, 0f), _ => new Vector2(32f, 0f) };
            }
            crosshair.gameObject.SetActive(false);

            // Joystick: 144 px well at rest bottom-left, dashed index ring, 64 px nub.
            joystickBase = UIKit.Rect(transform, "JoystickBase", new Vector2(144f, 144f));
            var well = UiDraw.Dot(joystickBase, "Well", 144f, UiDraw.Hex("#060e1d", .6f));
            UiDraw.Fill(well.rectTransform);
            UiDraw.Place(UiDraw.Ring(joystickBase, "IndexRing", 128f, 1f, UiDraw.Hex("#859396", .3f), 28).rectTransform,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(128f, 128f));
            joystickNub = UIKit.Rect(joystickBase, "Nub", new Vector2(64f, 64f));
            joystickNubFill = UiDraw.Dot(joystickNub, "Fill", 64f, UiDraw.Hex("#222a3a", .9f));
            UiDraw.Fill(joystickNubFill.rectTransform);
            UiDraw.Fill(UiDraw.Icon(joystickNub, "Icon", "navigation", 24f, Primary).rectTransform);
            joystickRest = joystickBase;

            padPreview = UIKit.Rect(transform, "PadPreview", new Vector2(460f, 54f));
            var previewFill = UiDraw.Box(padPreview, "Fill", UiDraw.Hex("#060e1d", .9f), 12f, UiDraw.Hex("#49627b", .5f));
            UiDraw.Fill(previewFill.rectTransform);
            padPreviewLabel = UiDraw.Text(padPreview, "Label", "", UiFont.Mono, 12f, Primary, .05f, TextAlignmentOptions.Midline);
            UiDraw.Fill(padPreviewLabel.rectTransform);
            padPreview.gameObject.SetActive(false);

            BuildEditor();
        }

        static void Plate(RectTransform disc, float size, float radius, float alpha = .9f)
        {
            var plate = UiDraw.Box(disc, "Plate", UiDraw.Hex("#222a3a", alpha), radius);
            UiDraw.Place(plate.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(size, size));
        }

        static void Caption(TextMeshProUGUI label, UiFont font, float size, Color color, float tracking, float y)
        {
            UIFonts.Use(label, font);
            label.fontSize = size;
            label.enableAutoSizing = false;
            label.color = color;
            label.characterSpacing = tracking * 100f;
            label.alignment = TextAlignmentOptions.Midline;
            UiDraw.Place(label.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, y), new Vector2(80f, 14f));
        }

        void BuildDisc(string name, out RectTransform root, out Image fill, out Image border, out TextMeshProUGUI label)
        {
            root = UIKit.Rect(transform, name + "Control", new Vector2(120f, 120f));
            root.gameObject.AddComponent<CanvasGroup>();
            // The face is drawn by Build; these stay as invisible handles the editor and tests use.
            fill = root.gameObject.AddComponent<Image>();
            fill.color = new Color(0f, 0f, 0f, 0f);
            fill.raycastTarget = false;
            var borderRoot = UIKit.Rect(root, "Outline", Vector2.zero);
            UIKit.Stretch(borderRoot);
            border = borderRoot.gameObject.AddComponent<Image>();
            border.color = new Color(0f, 0f, 0f, 0f);
            border.raycastTarget = false;
            label = UiDraw.Text(root, "Label", "", UiFont.Heading, 10f, Color.white, 0f, TextAlignmentOptions.Midline);
        }

        void BuildEditor()
        {
            editorRoot = UIKit.Rect(transform, "LayoutEditor", Vector2.zero);
            UIKit.Stretch(editorRoot);
            var scrim = editorRoot.gameObject.AddComponent<Image>();
            scrim.color = new Color(0.063f, 0.094f, 0.153f, 0.55f);
            scrim.raycastTarget = false;

            editorSelection = UIKit.Rect(editorRoot, "Selection", new Vector2(80f, 80f));
            editorSelectionImage = editorSelection.gameObject.AddComponent<Image>();
            editorSelectionImage.sprite = RingSprite(); editorSelectionImage.type = Image.Type.Simple;
            editorSelectionImage.color = UIDesignTokens.ObjectiveGold;
            editorSelectionImage.raycastTarget = false;
            // The ring is drawn a little larger than the control it marks, as the old
            // immediate-mode outline was, so the selection reads as a surround rather than a
            // cover. The tint is on the image; the size stays on the rect.
            editorSelectionImage.rectTransform.sizeDelta = new Vector2(80f, 80f);

            for (int i = 0; i < 6; i++)
            {
                var slot = UIKit.Rect(editorRoot, "EditAction" + i, new Vector2(130f, 75f));
                editorActions[i] = slot;
                editorActionFills[i] = slot.gameObject.AddComponent<Image>();
                editorActionFills[i].raycastTarget = false;
                editorActionLabels[i] = UIKit.CenteredLabel(slot, "Label", "", UiTextRole.Caption, UiTone.Friendly);
                UIKit.Stretch(editorActionLabels[i].rectTransform, 4f, 0f, 4f, 0f);
                editorActionLabels[i].enableAutoSizing = true;
                editorActionLabels[i].fontSizeMin = UiMetrics.MinTextUnits;
                editorActionLabels[i].fontSizeMax = 24f;
            }
            editorHint = UIKit.CenteredLabel(editorRoot, "Hint", "", UiTextRole.Caption, UiTone.Neutral);
            editorHint.rectTransform.sizeDelta = new Vector2(1100f, 40f);
            editorHint.enableAutoSizing = true;
            editorHint.fontSizeMin = UiMetrics.MinTextUnits;
            editorHint.fontSizeMax = 24f;
            editorRoot.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------ per frame

        void LateUpdate() => Refresh();

        /// <summary>
        /// Re-reads the control geometry and re-places every control.
        ///
        /// <para>Public, and called by the frame itself, for the same reason
        /// <c>StartupConnectionScreen.RefreshFrameLabel</c> is: a synchronous capture exports in the
        /// very frame it pins the frame size, so no <c>Update</c>/<c>LateUpdate</c> runs in between and
        /// the view would still hold the geometry of whatever frame was composed before it - the
        /// editor's window, not the published one. Without this the controls in a capture sat where
        /// the editor had been, which is how a 1920-wide evidence frame showed its controls
        /// occupying the middle of an image sized for 640.</para>
        /// </summary>
        public void Refresh()
        {
            if (input == null || canvasRect == null) return;
            if (canvas == null) canvas = GetComponentInParent<Canvas>();

            PlaceDisc(fireDisc, input.FireRect);
            PlaceDisc(jumpDisc, input.JumpRect);
            PlaceDisc(padDisc, input.PadRect);
            PlaceDisc(reloadDisc, input.ReloadRect);
            PlaceDisc(pingDisc, input.PingRect);
            PlaceDisc(pauseDisc, input.PauseRect);

            // The player's per-button opacity setting applies to the whole face.
            Opacity(fireDisc, input.FireOpacity);
            Opacity(jumpDisc, input.JumpOpacity);
            Opacity(padDisc, input.PadOpacity);
            Opacity(reloadDisc, input.ReloadOpacity);
            Opacity(pingDisc, input.PingOpacity);
            Opacity(pauseDisc, input.PauseOpacity);

            fireLabel.text = "FIRE";
            bool launch = input.LaunchAvailable;
            jumpLabel.text = launch ? "LAUNCH" : "JUMP";
            jumpPlate.color = launch ? UiDraw.Hex("#30d5e8") : UiDraw.Hex("#222a3a", .9f);
            jumpGlow.enabled = launch;
            jumpIcon.color = launch ? OnPrimaryContainer : Primary;
            jumpLabel.color = launch ? OnPrimaryContainer : OnSurfaceVariant;
            padLabel.text = input.PadDisplayLabel;
            string ammo = input.AmmoLabel ?? "";
            bool reloading = ammo.StartsWith("RELOADING");
            reloadAmmo.text = reloading ? ammo.Replace("RELOADING ", "") : ammo;
            reloadWeapon.text = input.WeaponLabel ?? "";
            reloadIcon.rectTransform.localRotation = reloading ? Quaternion.Euler(0f, 0f, -Time.unscaledTime * 360f) : Quaternion.identity;
            padDisc.gameObject.SetActive(input.PadVisible);

            // Crosshair and joystick are anchored to the composed frame, so a capture and a device
            // agree on where the centre is.
            var frame = SafeAreaRoot.ScreenSize;
            float u = UiMetrics.ScaleFor(frame.x, frame.y, 0f);
            if (crosshair != null)
            {
                crosshair.gameObject.SetActive(input.ShowCrosshair);
                if (input.ShowCrosshair) Place(crosshair, new Vector2(frame.x * 0.5f, frame.y * 0.5f), new Vector2(80f, 80f) * u);
            }
            if (joystickBase != null)
            {
                // At rest the well sits in its design spot (24 + 48 + 16 px in, 32 px up); while a
                // thumb is down it follows the thumb, and the nub shows the real deflection.
                joystickBase.gameObject.SetActive(!input.EditingLayout);
                bool moving = input.JoystickActive;
                var rest = new Vector2(ComposedSafeLeft(frame) + (24f + 48f + 16f + 72f) * u, (32f + 72f) * u);
                Place(joystickBase, moving ? input.JoystickCenter : rest, new Vector2(144f, 144f) * u);
                var offset = moving ? input.JoystickOffset / Mathf.Max(1f, input.JoystickReachPx) : Vector2.zero;
                joystickNub.anchoredPosition = offset * 40f;
            }
            if (padPreview != null)
            {
                bool show = input.PadPreviewActive && !input.EditingLayout;
                padPreview.gameObject.SetActive(show);
                if (show)
                {
                    padPreviewLabel.text = input.PadPreviewCaption;
                    float w = UiDraw.Width(padPreviewLabel) + 32f;
                    Place(padPreview, new Vector2(frame.x * 0.5f, frame.y * 0.5f - 72f * u), new Vector2(w, 40f) * u);
                    padPreviewLabel.color = input.PadPreviewReason == null ? Primary : UiDraw.Hex("#ff6376");
                }
            }
            UpdateEditor();
        }

        static float ComposedSafeLeft(Vector2 frame)
        {
            if (SafeAreaRoot.SimulatedScreen.HasValue) return (SafeAreaRoot.SimulatedInsets ?? ScreenInsets.None).left;
            var safe = Screen.safeArea;
            return safe.width > 0f ? safe.xMin : 0f;
        }

        static void Opacity(RectTransform disc, float value)
        {
            if (disc == null) return;
            var group = disc.GetComponent<CanvasGroup>();
            // 0.65 is the shipped default and draws the design's full face; lower settings dim it.
            if (group != null) group.alpha = Mathf.Clamp01(Mathf.Max(.25f, value / .65f));
        }

        void UpdateEditor()
        {
            if (editorRoot == null) return;
            bool editing = input.EditingLayout;
            if (editorRoot.gameObject.activeSelf != editing) editorRoot.gameObject.SetActive(editing);
            if (!editing) return;

            var actions = TouchInputManager.EditActionCaptions;
            for (int i = 0; i < 6; i++)
            {
                var rect = input.EditActionRectFor(i);
                PlaceDisc(editorActions[i], rect);
                editorActionFills[i].color = i == 5
                    ? UIDesignTokens.WithAlpha(UIDesignTokens.ActionCyan, 0.95f)
                    : UIDesignTokens.WithAlpha(UIDesignTokens.SurfacePanel, 0.95f);
                editorActionLabels[i].text = actions[i];
            }
            PlaceDisc(editorSelection, input.SelectedRect);
            editorSelection.sizeDelta += new Vector2(10f, 10f);
            editorSelection.SetAsLastSibling();
            editorHint.text = input.EditHintText;
            var hintRect = editorHint.rectTransform;
            hintRect.anchoredPosition = new Vector2(0f, editorActions[0].anchoredPosition.y - 26f);
        }

        /// <summary>Places a control at a screen-space rect (pixels, bottom-left origin).</summary>
        void PlaceDisc(RectTransform target, Rect rect)
        {
            if (target == null) return;
            if (target.gameObject.activeSelf != (rect.width > 0f)) target.gameObject.SetActive(rect.width > 0f);
            if (rect.width <= 0f) return;
            Place(target, rect.center, new Vector2(rect.width, rect.height));
        }

        /// <summary>
        /// Places and sizes an element from a rect given in composed-frame pixels.
        ///
        /// <para>The mapping is a fraction of the frame, not a screen-point conversion. A capture
        /// composes 1920x1080 inside whatever window the editor owns, so <c>Screen</c> and the
        /// frame disagree in exactly the frame the evidence publishes; converting a screen point
        /// through <c>RectTransformUtility</c> reads the window, and every control then lands in
        /// the wrong part of the image. Taking the rect's share of the frame and applying it to
        /// the canvas rect is true on a device (where the canvas spans the screen) and true in a
        /// capture (where the canvas spans the composed frame) without a branch.</para>
        /// </summary>
        void Place(RectTransform target, Vector2 framePoint, Vector2 frameSize)
        {
            if (target == null || canvasRect == null) return;
            var frame = SafeAreaRoot.ScreenSize;
            var rect = canvasRect.rect;
            if (frame.x <= 0f || frame.y <= 0f) return;
            target.anchoredPosition = new Vector2(
                (framePoint.x / frame.x - 0.5f) * rect.width,
                (framePoint.y / frame.y - 0.5f) * rect.height);
            target.sizeDelta = new Vector2(
                frameSize.x / frame.x * rect.width,
                frameSize.y / frame.y * rect.height);
        }


    }
}