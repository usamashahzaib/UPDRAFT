using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

namespace Hotdrop.UI
{
    /// <summary>
    /// Exact-placement helpers for screens rebuilt from the UIUX HTML. Sizes are reference px
    /// (one canvas unit each, see <see cref="UiMetrics"/>); text sizes and tracking are the CSS
    /// values verbatim.
    /// </summary>
    public static class UiDraw
    {
        static readonly Dictionary<string, Texture2D> art = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        /// <summary>A CSS hex colour (#rgb or #rrggbb) with an optional alpha.</summary>
        public static Color Hex(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            c = ResolveLegacyPaletteColor(hex, c);
            c.a = alpha;
            return c;
        }

        // Older screens still declare the original palette by hex. Resolve those exact
        // palette entries through the active profile so A/B/C stays consistent app-wide.
        static Color ResolveLegacyPaletteColor(string hex, Color fallback)
        {
            switch (hex?.Trim().ToLowerInvariant())
            {
                case "#101827": case "#060e1d": case "#0c1220": case "#121619": return UIDesignTokens.Background;
                case "#1b2940": case "#0b1322": case "#141c2b": case "#17202e": case "#20272b": return UIDesignTokens.SurfacePanel;
                case "#263a59": case "#263444": case "#303a3d": case "#18202f": case "#222a3a": case "#2d3545": return UIDesignTokens.SurfaceSelected;
                case "#49627b": case "#434b4b": case "#859396": case "#3c494b": return UIDesignTokens.SurfaceStructural;
                case "#536176": case "#4e5858": return UIDesignTokens.SurfaceDisabled;
                case "#f4f7fb": case "#dbe2f8": case "#edf2f7": case "#f0f0ea": return UIDesignTokens.TextPrimary;
                case "#bbc6d8": case "#bbc9cb": case "#8a94a6": case "#a6aead": return UIDesignTokens.TextSecondary;
                case "#7f8ca3": return UIDesignTokens.TextMuted;
                case "#30d5e8": case "#7cefff": case "#38d9ec": case "#3de0c0": case "#54d2b3": return UIDesignTokens.PrimaryAction;
                case "#4da3ff": case "#66a7d9": return UIDesignTokens.Ally;
                case "#ff6376": case "#ffb2b7": case "#ff5a66": case "#d85d61": return UIDesignTokens.HazardCoral;
                case "#ffd166": case "#ffda88": case "#edc157": case "#ffb84d": case "#e2b85f": return UIDesignTokens.ObjectiveGold;
                case "#ffb45c": return UIDesignTokens.SignalWarning;
                case "#7ed68a": return UIDesignTokens.SignalSuccess;
                case "#930d2d": return Color.Lerp(UIDesignTokens.HazardCoral, UIDesignTokens.Background, .65f);
                case "#67001b": return Color.Lerp(UIDesignTokens.HazardCoral, UIDesignTokens.Background, .8f);
                case "#ff9da4": return Color.Lerp(UIDesignTokens.HazardCoral, UIDesignTokens.TextPrimary, .35f);
                case "#00363c": return UIDesignTokens.TextOn(UIDesignTokens.PrimaryAction);
                case "#3f2e00": return UIDesignTokens.TextOn(UIDesignTokens.ObjectiveGold);
                default: return fallback;
            }
        }

        /// <summary>A bare RectTransform child.</summary>
        public static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            UiPhysicalSizeFloor.MarkDirty();
            return (RectTransform)go.transform;
        }

        /// <summary>Anchors a rect to one point of its parent and places it from there.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Fits a texture inside its target rectangle without stretching its aspect ratio.</summary>
        public static void FitRawImage(RawImage image, Vector2 targetSize)
        {
            if (image == null || image.texture == null || targetSize.x <= 0f || targetSize.y <= 0f
                || image.texture.width <= 0 || image.texture.height <= 0) return;

            float aspect = image.texture.width / (float)image.texture.height;
            float width = targetSize.x;
            float height = targetSize.y;
            if (width / height > aspect) width = height * aspect;
            else height = width / aspect;
            image.rectTransform.sizeDelta = new Vector2(width, height);
        }

        /// <summary>Stretches a rect over its parent with CSS-style insets.</summary>
        public static RectTransform Fill(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(.5f, .5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>
        /// Canvas units per local unit of the page being built. A page laid out in dp and scaled up by
        /// this factor sets it while building so its labels keep their dp size; 1 everywhere else.
        /// </summary>
        public static float TextUnitScale = 1f;

        /// <summary>A label in one of the UIUX families, at a CSS size and letter-spacing.</summary>
        public static TextMeshProUGUI Text(Transform parent, string name, string value, UiFont font, float px,
            Color color, float trackingEm = 0f, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var rt = Node(parent, name);
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            UIFonts.Use(text, font);
            text.text = UiPresentationProfile.NormalizeRichText(value);
            // Inside a UiDpPanel the panel's own scale already turns local units into dp, so the
            // label is authored in local units; converting it again drew HUD text ~2.8x too large.
            text.fontSize = UiDpPanel.Contains(parent)
                ? Mathf.Max(px * UiMetrics.TextScale, UiMetrics.MinTextDp)
                : UiMetrics.ScaleTextSize(px) / TextUnitScale;
            // White was used as a neutral text default by older screens. Resolve it
            // through the active profile so profile switching also reaches labels
            // that predate UIDesignTokens.
            text.color = color == Color.white ? UIDesignTokens.TextPrimary : color;
            text.characterSpacing = trackingEm * 100f;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>A Material Symbols glyph at a CSS size.</summary>
        public static TextMeshProUGUI Icon(Transform parent, string name, string icon, float px, Color color, bool filled = false)
        {
            var text = Text(parent, name, UIIcons.Glyph(icon), filled ? UiFont.IconFilled : UiFont.Icon, px, color, 0f,
                TextAlignmentOptions.Center);
            text.rectTransform.sizeDelta = new Vector2(px, px);
            return text;
        }

        /// <summary>Rasterised UIUX art from Resources/UIUX, drawn at its reference size.</summary>
        public static RawImage Art(Transform parent, string name, string resource, Vector2 size)
        {
            var rt = Node(parent, name);
            rt.sizeDelta = size;
            var image = rt.gameObject.AddComponent<RawImage>();
            image.texture = Texture(resource);
            image.raycastTarget = false;
            return image;
        }

        /// <summary>The same art as a sprite, for filled or masked drawing.</summary>
        public static Image ArtSprite(Transform parent, string name, string resource, Vector2 size)
        {
            var rt = Node(parent, name);
            rt.sizeDelta = size;
            var image = rt.gameObject.AddComponent<Image>();
            if (!sprites.TryGetValue(resource, out var sprite) || sprite == null)
            {
                var tex = Texture(resource);
                sprite = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 100f) : null;
                sprites[resource] = sprite;
            }
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// Rasterised UIUX art (rendered at 3x) as a nine-slice: <paramref name="borderPx"/> reference
        /// px on every side keep corners and chamfers exact while the centre stretches.
        /// </summary>
        public static Image ArtSliced(Transform parent, string name, string resource, float borderPx)
        {
            string key = resource + "#" + borderPx;
            var rt = Node(parent, name);
            var image = rt.gameObject.AddComponent<Image>();
            if (!sprites.TryGetValue(key, out var sprite) || sprite == null)
            {
                var tex = Texture(resource);
                float b = borderPx * 3f;
                sprite = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 300f, 0,
                    SpriteMeshType.FullRect, new Vector4(b, b, b, b)) : null;
                sprites[key] = sprite;
            }
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return image;
        }

        public static Texture2D Texture(string resource)
        {
            if (art.TryGetValue(resource, out var tex) && tex != null) return tex;
            tex = Resources.Load<Texture2D>("UIUX/" + resource);
            // New model-rendered weapon portraits take precedence when present. Until the
            // editor generator has run and the outputs are reviewed, keep the current silhouettes
            // available through the same lookup so this migration cannot blank the HUD.
            if (tex == null && resource != null && resource.StartsWith("WeaponPortraits/", System.StringComparison.Ordinal))
                tex = Resources.Load<Texture2D>("UIUX/" + resource.Substring("WeaponPortraits/".Length));
            art[resource] = tex;
            return tex;
        }

        /// <summary>A solid or rounded panel with an optional 1px border, CSS box style.</summary>
        public static Image Box(Transform parent, string name, Color fill, float radius = 0f, Color? border = null, float borderWidth = 1f)
        {
            var rt = Node(parent, name);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = fill;
            image.raycastTarget = false;
            if (radius > 0f)
            {
                image.sprite = UIRoundedSprites.RoundedRect(radius);
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
            }
            if (border.HasValue)
            {
                var frame = Node(rt, "Border");
                Fill(frame);
                var b = frame.gameObject.AddComponent<Image>();
                b.raycastTarget = false;
                b.color = border.Value;
                b.sprite = UIRoundedSprites.RoundedOutline(radius, borderWidth);
                b.type = Image.Type.Sliced;
                b.pixelsPerUnitMultiplier = 1f;
            }
            return image;
        }

        static readonly Dictionary<long, Sprite> glows = new Dictionary<long, Sprite>();

        /// <summary>
        /// CSS <c>box-shadow: 0 0 {blur}px color</c> around a rounded rect: a child of
        /// <paramref name="target"/> that only paints outside its edge, so it can sit on top.
        /// </summary>
        public static Image Glow(RectTransform target, Color color, float radius, float blur)
        {
            int r = Mathf.Max(0, Mathf.RoundToInt(radius)), b = Mathf.Max(1, Mathf.RoundToInt(blur));
            long key = ((long)r << 16) | (long)b;
            if (!glows.TryGetValue(key, out var sprite) || sprite == null)
            {
                int pad = b * 2, edge = r + pad, size = edge * 2 + 4;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
                    { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave, name = "Glow" };
                float sigma = b / 2f;
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Signed distance from the rounded rect that starts pad px in from each side.
                    float qx = Mathf.Abs(x + .5f - size / 2f) - (size / 2f - pad - r);
                    float qy = Mathf.Abs(y + .5f - size / 2f) - (size / 2f - pad - r);
                    float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
                    float a = outside <= 0f ? 0f : .5f * Erfc(outside / (sigma * 1.41421356f));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect,
                    new Vector4(edge, edge, edge, edge));
                sprite.hideFlags = HideFlags.HideAndDontSave;
                glows[key] = sprite;
            }
            var node = Node(target, "Glow");
            Fill(node, -b * 2, -b * 2, -b * 2, -b * 2);
            node.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var image = node.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static float Erfc(float x)
        {
            // Abramowitz-Stegun 7.1.26, |error| < 1.5e-7.
            float t = 1f / (1f + .3275911f * Mathf.Abs(x));
            float y = t * (.254829592f + t * (-.284496736f + t * (1.421413741f + t * (-1.453152027f + t * 1.061405429f)))) * Mathf.Exp(-x * x);
            return x >= 0f ? y : 2f - y;
        }

        /// <summary>A tappable box: the rounded fill, optional border, and a Button on it.</summary>
        public static Button Tap(Transform parent, string name, Color fill, float radius, System.Action onClick, Color? border = null)
        {
            var box = Box(parent, name, fill, radius, border);
            box.raycastTarget = true;
            var button = box.gameObject.AddComponent<Button>();
            button.targetGraphic = box;
            button.transition = Selectable.Transition.None;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        static readonly Dictionary<string, Texture2D> gradients = new Dictionary<string, Texture2D>();

        /// <summary>A vertical CSS gradient (bottom, middle, top stops) stretched over a rect.</summary>
        public static RawImage VerticalGradient(Transform parent, string name, Color bottom, Color middle, Color top)
        {
            string key = bottom.ToString() + middle + top;
            if (!gradients.TryGetValue(key, out var tex) || tex == null)
            {
                tex = new Texture2D(1, 256, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                for (int y = 0; y < 256; y++)
                {
                    float v = y / 255f;
                    tex.SetPixel(0, y, v < .5f ? Color.Lerp(bottom, middle, v * 2f) : Color.Lerp(middle, top, (v - .5f) * 2f));
                }
                tex.Apply(false, true);
                gradients[key] = tex;
            }
            var image = Node(parent, name).gameObject.AddComponent<RawImage>();
            Fill(image.rectTransform);
            image.texture = tex;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A horizontal CSS gradient (left, middle, right stops) stretched over a rect.</summary>
        public static RawImage HorizontalGradient(Transform parent, string name, Color left, Color middle, Color right)
        {
            string key = "h" + left + middle + right;
            if (!gradients.TryGetValue(key, out var tex) || tex == null)
            {
                tex = new Texture2D(256, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                for (int x = 0; x < 256; x++)
                {
                    float u = x / 255f;
                    tex.SetPixel(x, 0, u < .5f ? Color.Lerp(left, middle, u * 2f) : Color.Lerp(middle, right, (u - .5f) * 2f));
                }
                tex.Apply(false, true);
                gradients[key] = tex;
            }
            var image = Node(parent, name).gameObject.AddComponent<RawImage>();
            Fill(image.rectTransform);
            image.texture = tex;
            image.raycastTarget = false;
            return image;
        }

        static Texture2D vignette;

        /// <summary>
        /// The UIUX screen backdrop: base colour, the venue photo dimmed to
        /// <paramref name="brightness"/>, a bottom-to-top shade, optionally the elliptical
        /// vignette, a bottom fade of <paramref name="bottomFade"/> px and the 48 px grid.
        /// </summary>
        public static RectTransform Backdrop(RectTransform root, float brightness, Color bottom, Color middle, Color top,
            bool withVignette = false, float bottomFade = 0f, float gridAlpha = 0f)
        {
            var layer = Node(root, "Backdrop");
            Fill(layer);
            layer.SetAsFirstSibling();
            Fill(Box(layer, "Base", Hex("#0b1322")).rectTransform);
            var rooftop = UiPresentationProfile.LoadRooftopArtwork();
            if (rooftop != null)
            {
                var photo = Node(layer, "Photo").gameObject.AddComponent<RawImage>();
                Fill(photo.rectTransform);
                photo.texture = rooftop;
                photo.color = new Color(brightness * .96f, brightness, brightness * 1.03f, 1f);
                photo.raycastTarget = false;
            }
            VerticalGradient(layer, "Shade", bottom, middle, top);
            var deep = Hex("#060e1d");
            if (withVignette)
            {
                if (vignette == null)
                {
                    const int n = 128;
                    vignette = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                    for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + .5f) / n * 2f - 1f, dy = (y + .5f) / n * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy) / Mathf.Sqrt(2f);
                        vignette.SetPixel(x, y, new Color(deep.r, deep.g, deep.b, Mathf.InverseLerp(.40f, .95f, d)));
                    }
                    vignette.Apply(false, true);
                }
                var v = Node(layer, "Vignette").gameObject.AddComponent<RawImage>();
                Fill(v.rectTransform);
                v.texture = vignette;
                v.raycastTarget = false;
            }
            if (bottomFade > 0f)
            {
                var fade = VerticalGradient(layer, "BottomFade", deep, new Color(deep.r, deep.g, deep.b, .5f), new Color(deep.r, deep.g, deep.b, 0f));
                fade.rectTransform.anchorMax = new Vector2(1f, 0f);
                fade.rectTransform.pivot = new Vector2(.5f, 0f);
                fade.rectTransform.sizeDelta = new Vector2(0f, bottomFade);
            }
            if (gridAlpha > 0f)
            {
                var grid = Node(layer, "Grid");
                Fill(grid);
                grid.gameObject.AddComponent<RectMask2D>();
                var c = Hex("#30d5e8", gridAlpha);
                for (int i = 0; i < 60; i++)
                {
                    var gv = Box(grid, "GridV", c).rectTransform;
                    gv.anchorMin = new Vector2(0f, 0f); gv.anchorMax = new Vector2(0f, 1f); gv.pivot = new Vector2(0f, .5f);
                    gv.sizeDelta = new Vector2(1f, 0f); gv.anchoredPosition = new Vector2(i * 48f, 0f);
                    var gh = Box(grid, "GridH", c).rectTransform;
                    gh.anchorMin = new Vector2(0f, 1f); gh.anchorMax = new Vector2(1f, 1f); gh.pivot = new Vector2(.5f, 1f);
                    gh.sizeDelta = new Vector2(0f, 1f); gh.anchoredPosition = new Vector2(0f, -i * 48f);
                }
            }
            return layer;
        }

        static readonly Dictionary<int, Sprite> rings = new Dictionary<int, Sprite>();

        /// <summary>
        /// A circle outline <paramref name="width"/> px thick, solid or with <paramref name="dashes"/>
        /// dashes (CSS <c>border-dashed</c> on a rounded-full box), at <paramref name="diameter"/> px.
        /// </summary>
        public static Image Ring(Transform parent, string name, float diameter, float width, Color color, int dashes = 0)
        {
            int key = (Mathf.RoundToInt(diameter) << 12) ^ (Mathf.RoundToInt(width * 4f) << 6) ^ dashes;
            if (!rings.TryGetValue(key, out var sprite) || sprite == null)
            {
                int size = Mathf.Clamp(Mathf.CeilToInt(diameter * 2f), 16, 512);
                float scale = size / diameter;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
                    { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave, name = "Ring" };
                var px = new Color32[size * size];
                float outer = size / 2f, inner = outer - width * scale, c = size / 2f;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + .5f - c, dy = y + .5f - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(outer - d) * Mathf.Clamp01(d - inner);
                    if (dashes > 0)
                    {
                        float t = (Mathf.Atan2(dy, dx) / (Mathf.PI * 2f) + 1f) * dashes % 1f;
                        a *= t < .5f ? 1f : 0f;
                    }
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
                sprite.hideFlags = HideFlags.HideAndDontSave;
                rings[key] = sprite;
            }
            var rt = Node(parent, name);
            rt.sizeDelta = new Vector2(diameter, diameter);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A filled circle at <paramref name="diameter"/> px.</summary>
        public static Image Dot(Transform parent, string name, float diameter, Color color)
        {
            var image = Box(parent, name, color, diameter / 2f);
            image.rectTransform.sizeDelta = new Vector2(diameter, diameter);
            return image;
        }

        /// <summary>The width a label needs for its text, for sizing CSS inline boxes.</summary>
        public static float Width(TMP_Text text, string value = null) => text.GetPreferredValues(value ?? text.text).x;

        /// <summary>Trims <paramref name="text"/> to the longest prefix (plus a literal "...", not
        /// the single-glyph ellipsis character) that fits <paramref name="maxWidth"/>, measured on
        /// <paramref name="measurer"/> without changing its displayed text. Built for call sites
        /// that need the box geometry to actually clip long content: TMP's own Ellipsis overflow
        /// mode was tried first and found to render the whole label blank in this project's batch
        /// PlayMode captures (reproduced on both rich-text and plain-string labels, and on a
        /// pre-existing call site, not just a new one), so this never changes a component's own
        /// overflow mode.</summary>
        public static string Truncate(TMP_Text measurer, string text, float maxWidth)
        {
            if (string.IsNullOrEmpty(text) || Width(measurer, text) <= maxWidth) return text;
            const string suffix = "...";
            if (Width(measurer, suffix) > maxWidth) return suffix;
            int lo = 0, hi = text.Length;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (Width(measurer, text.Substring(0, mid) + suffix) <= maxWidth) lo = mid; else hi = mid - 1;
            }
            return text.Substring(0, lo) + suffix;
        }

        /// <summary>The shared string table used by authored player-facing UI copy.
        /// Screen-specific keys share this collection; split it only if the key count becomes
        /// difficult to manage.</summary>
        public const string UiStringTable = "UIStrings";

        /// <summary>Looks up <paramref name="key"/> in <see cref="UiStringTable"/> and returns
        /// <paramref name="fallback"/> (the English copy, inline at the call site) if the tables
        /// aren't initialized yet, the key is missing, or the lookup throws for any other reason -
        /// this must never be the thing that breaks a screen build. The lookup is forced
        /// synchronous (<c>WaitForCompletion</c>): every call site here builds its whole screen in
        /// one synchronous method, same as before localization, so an async result would just be
        /// read before it arrived.</summary>
        public static string L(string key, string fallback)
        {
            return TryL(key, fallback, out string result) ? result : fallback;
        }

        /// <summary>Looks up a localized string like <see cref="L"/> and reports whether it
        /// came from the configured table. The fallback is returned through <paramref name="result"/>
        /// on an unavailable table, missing key, empty entry, or lookup failure.</summary>
        public static bool TryL(string key, string fallback, out string result)
        {
            try
            {
                var op = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(UiStringTable, key);
                string localized = op.IsDone ? op.Result : op.WaitForCompletion();
                // A missing table entry does not fail this lookup or return empty - the
                // Localization package hands back its own developer-facing diagnostic text
                // ("No translation found for ...") as a normal, non-empty result. Treat that the
                // same as missing rather than let it reach a player's screen.
                if (string.IsNullOrEmpty(localized) || localized.Contains("No translation found"))
                {
                    result = fallback;
                    return false;
                }
                result = localized;
                return true;
            }
            catch
            {
                result = fallback;
                return false;
            }
        }

        /// <summary>Formats a localized template and falls back to the authored English template
        /// if a translation contains an invalid format placeholder.</summary>
        public static string LFormat(string key, string fallback, params object[] args)
        {
            string template = L(key, fallback);
            try
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, template, args ?? System.Array.Empty<object>());
            }
            catch (System.FormatException)
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, fallback, args ?? System.Array.Empty<object>());
            }
        }

        /// <summary>Applies uppercase styling using the currently selected locale's casing rules.</summary>
        public static string ToLocaleUpper(string value)
        {
            if (string.IsNullOrEmpty(value)) return value ?? "";
            try
            {
                var culture = LocalizationSettings.SelectedLocale?.Identifier.CultureInfo;
                return culture != null ? culture.TextInfo.ToUpper(value) : value.ToUpperInvariant();
            }
            catch
            {
                return value.ToUpperInvariant();
            }
        }

        /// <summary>A 1 px rule across a parent at <paramref name="y"/> from its top.</summary>
        public static Image HRule(Transform parent, string name, Color color, float y, float left = 0f, float right = 0f)
        {
            var rule = Box(parent, name, color);
            var rt = rule.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(.5f, 1f);
            rt.offsetMin = new Vector2(left, 0f); rt.offsetMax = new Vector2(-right, 0f);
            rt.anchoredPosition = new Vector2((left - right) * .5f, -y);
            rt.sizeDelta = new Vector2(-(left + right), 1f);
            return rule;
        }
    }
}
