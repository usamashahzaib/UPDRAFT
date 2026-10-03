using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>
    /// Rounded-rectangle sprites generated at runtime, so the UI can honour the design system's
    /// corner radii and nine-slice borders without importing any third-party sprite sheet.
    ///
    /// Sprites are cached per (radius, pixel size, border weight) and deliberately never
    /// destroyed: a screen's geometry is rebuilt often, but the texture count must not grow with
    /// it. Each texture is a few hundred pixels at most.
    /// </summary>
    public static class UIRoundedSprites
    {
        struct Key : System.IEquatable<Key>
        {
            public int radius, size, border;
            public bool Equals(Key other) => radius == other.radius && size == other.size && border == other.border;
            public override bool Equals(object obj) => obj is Key other && Equals(other);
            public override int GetHashCode() => (radius * 397 ^ size) * 397 ^ border;
        }

        static readonly Dictionary<Key, Sprite> cache = new Dictionary<Key, Sprite>();

        /// <summary>Number of distinct sprites generated so far. Tests assert this stays bounded.</summary>
        public static int CachedCount => cache.Count;

        /// <summary>
        /// A nine-sliced rounded rect. <paramref name="radiusUnits"/> is in canvas units;
        /// <paramref name="pixelsPerUnit"/> is the canvas scale so corners stay crisp.
        /// </summary>
        public static Sprite RoundedRect(float radiusUnits, float pixelsPerUnit = 1f, int borderUnits = 0)
        {
            int radius = Mathf.Clamp(Mathf.RoundToInt(radiusUnits * Mathf.Max(0.01f, pixelsPerUnit)), 0, 64);
            int size = Mathf.Clamp(radius * 2 + 8, 8, 160);
            int border = Mathf.Clamp(borderUnits, 0, Mathf.Max(0, size / 2 - 1));
            var key = new Key { radius = radius, size = size, border = border };
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var sprite = Build(radius, size, border);
            cache[key] = sprite;
            return sprite;
        }

        static Sprite Build(int radius, int size, int border)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"HotdropRounded_{radius}_{size}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color32[size * size];
            float inner = Mathf.Max(0f, radius - border);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float coverage = Coverage(x + 0.5f, y + 0.5f, size, radius);
                    float hole = inner <= 0.5f ? 0f : 1f - Coverage(x + 0.5f, y + 0.5f, size, inner);
                    float alpha = Mathf.Clamp01(coverage - hole);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            // Slice at the corner radius so the centre stretches without bending the corners.
            float edge = Mathf.Max(1f, radius);
            var borderVector = new Vector4(edge, edge, edge, edge);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, borderVector);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        /// <summary>Anti-aliased 0..1 coverage of a rounded rect at a pixel centre.</summary>
        static float Coverage(float x, float y, float size, float radius)
        {
            float inset = radius;
            float cx = Mathf.Clamp(x, inset, size - inset);
            float cy = Mathf.Clamp(y, inset, size - inset);
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            if (radius <= 0f) return 1f;
            return Mathf.Clamp01(radius - distance + 0.5f);
        }
    }
}
