using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>The distinct shape a signal carries, independent of its colour.</summary>
    public enum CueShape
    {
        /// <summary>Closed ring - allies and friendly information.</summary>
        Ring,
        /// <summary>Ring with a diamond core - objectives and rewards.</summary>
        RingDiamond,
        /// <summary>Two opposed chevrons - enemies and threats.</summary>
        SplitChevron,
        /// <summary>Crossed bars - lethal, blocked or destructive state.</summary>
        Cross,
    }

    /// <summary>
    /// Team and objective signals that survive with colour removed, with colour blindness, or on a
    /// screen washed out by direct sun.
    ///
    /// Every cue is a distinct *shape* first and a colour second:
    ///  - friendly  - a closed ring,
    ///  - enemy     - a split chevron, or a cross for a lethal/blocked state,
    ///  - objective - a ring with a diamond core.
    ///
    /// The shapes are built from plain UI rectangles, so nothing here needs a licensed icon set.
    /// <see cref="Label"/> carries the same meaning as text, which is the third redundant channel.
    /// </summary>
    public class UICue : MonoBehaviour
    {
        [SerializeField] CueShape shape = CueShape.Ring;
        [SerializeField] UiTone tone = UiTone.Neutral;
        [SerializeField] string label = "";
        [SerializeField] float sizeUnits = 48f;

        /// <summary>Which shape this cue draws.</summary>
        public CueShape Shape { get => shape; set { shape = value; Rebuild(); } }

        /// <summary>Which signal colour it draws in.</summary>
        public UiTone Tone { get => tone; set { tone = value; Rebuild(); } }

        /// <summary>
        /// The word that carries the same meaning as the shape and colour. Never blank in
        /// shipping UI: colour must not be the only signal.
        /// </summary>
        public string Label { get => label; set => label = value ?? ""; }

        /// <summary>True when the text channel is present, as accessibility requires.</summary>
        public bool HasTextSignal => !string.IsNullOrEmpty(label);

        /// <summary>The default shape for a tone, so callers cannot mix them up.</summary>
        public static CueShape ShapeFor(UiTone tone) => tone switch
        {
            UiTone.Friendly => CueShape.Ring,
            UiTone.Enemy => CueShape.SplitChevron,
            UiTone.Objective => CueShape.RingDiamond,
            _ => CueShape.Ring,
        };

        /// <summary>The default word for a tone.</summary>
        public static string LabelFor(UiTone tone) => tone switch
        {
            UiTone.Friendly => "ALLY",
            UiTone.Enemy => "ENEMY",
            UiTone.Objective => "ZONE",
            _ => "",
        };

        /// <summary>Size of the cue box in canvas units.</summary>
        public float SizeUnits { get => sizeUnits; set { sizeUnits = Mathf.Max(8f, value); Rebuild(); } }

        void Awake() => Rebuild();

        /// <summary>Builds a cue at the given tone, label and size. Shared by the kit and the gallery.</summary>
        public static UICue Create(Transform parent, UiTone tone, string label = null, float sizeUnits = 48f)
        {
            var go = new GameObject("Cue_" + tone, typeof(RectTransform), typeof(UICue));
            go.transform.SetParent(parent, false);
            var cue = go.GetComponent<UICue>();
            cue.tone = tone;
            cue.shape = ShapeFor(tone);
            cue.label = label ?? LabelFor(tone);
            cue.sizeUnits = Mathf.Max(8f, sizeUnits);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(cue.sizeUnits, cue.sizeUnits);
            cue.Rebuild();
            return cue;
        }

        /// <summary>Throws away the old geometry and redraws the cue for the current shape.</summary>
        public void Rebuild()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            var color = UIDesignTokens.ForTone(tone);
            float weight = Mathf.Max(3f, sizeUnits * 0.11f);
            float half = sizeUnits * 0.5f;

            switch (shape)
            {
                case CueShape.Ring:
                    Ring(half, weight, color);
                    break;
                case CueShape.RingDiamond:
                    Ring(half, weight, color);
                    Diamond(half, color);
                    break;
                case CueShape.SplitChevron:
                    Chevron(-half * 0.30f, half * 1.15f, pointsRight: false, weight, color);
                    Chevron(half * 0.30f, half * 1.15f, pointsRight: true, weight, color);
                    break;
                case CueShape.Cross:
                    Bar(Vector2.zero, new Vector2(sizeUnits * 0.82f, weight), 38f, color);
                    Bar(Vector2.zero, new Vector2(sizeUnits * 0.82f, weight), -38f, color);
                    break;
            }
        }

        void Ring(float radius, float weight, Color color)
        {
            float diameter = radius * 2f;
            // Four bars form a closed square ring: crisp at any scale, no sprite needed.
            Bar(new Vector2(0f, radius - weight * 0.5f), new Vector2(diameter, weight), 0f, color);
            Bar(new Vector2(0f, -radius + weight * 0.5f), new Vector2(diameter, weight), 0f, color);
            Bar(new Vector2(-radius + weight * 0.5f, 0f), new Vector2(diameter, weight), 90f, color);
            Bar(new Vector2(radius - weight * 0.5f, 0f), new Vector2(diameter, weight), 90f, color);
        }

        void Diamond(float radius, Color color)
        {
            var go = new GameObject("Diamond", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(radius * 0.9f, radius * 0.9f);
            rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        void Chevron(float apexX, float length, bool pointsRight, float weight, Color color)
        {
            // A chevron is two bars meeting at an apex. The enemy cue puts a left-pointing one
            // on the left and a right-pointing one on the right, so the pair reads as a split.
            float half = length * 0.5f;
            float dx = pointsRight ? -Mathf.Sqrt(0.5f) : Mathf.Sqrt(0.5f);
            float dy = Mathf.Sqrt(0.5f);
            Bar(new Vector2(apexX + dx * half, dy * half), new Vector2(length, weight), pointsRight ? 135f : 45f, color);
            Bar(new Vector2(apexX + dx * half, -dy * half), new Vector2(length, weight), pointsRight ? 225f : -45f, color);
        }

        RectTransform Bar(Vector2 center, Vector2 size, float rotation, Color color)
        {
            var go = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = center;
            rt.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rt;
        }
    }
}
