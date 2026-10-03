using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>
    /// A structural rail built from four bars, held behind one reference so a control that
    /// repaints per state can retint and re-weight the whole frame at once.
    ///
    /// DESIGN.md forbids muddy drop shadows: hierarchy comes from surface luminance and a crisp
    /// border instead. Four bars give that border at any density and under any canvas scale mode,
    /// which a nine-slice outline sprite would not.
    /// </summary>
    public class UiBorder : MonoBehaviour
    {
        [SerializeField] Image[] bars;
        [SerializeField] float weightUnits = UIDesignTokens.BorderNormalUnits;
        [SerializeField] float inset;

        /// <summary>Current border weight in canvas units.</summary>
        public float WeightUnits => weightUnits;

        /// <summary>The bars that make up the frame.</summary>
        public Image[] Bars => bars;

        /// <summary>Wires the frame up. Called by <see cref="UIKit.Border"/>.</summary>
        public void Configure(Image[] frameBars, float initialWeight, float insetUnits)
        {
            bars = frameBars;
            weightUnits = Mathf.Max(1f, initialWeight);
            inset = Mathf.Max(0f, insetUnits);
        }

        /// <summary>Retints every rail.</summary>
        public void Tint(Color color)
        {
            if (bars == null) return;
            foreach (var bar in bars)
                if (bar != null) bar.color = color;
        }

        /// <summary>Shows or hides the whole rail without touching the hierarchy.</summary>
        public bool Visible
        {
            get => gameObject.activeSelf;
            set { if (gameObject.activeSelf != value) gameObject.SetActive(value); }
        }

        /// <summary>
        /// Changes the rail's weight. A heavier rail is how a selected card or an active objective
        /// is emphasised, which keeps the emphasis off colour alone.
        /// </summary>
        public void SetWeight(float units)
        {
            units = Mathf.Max(1f, units);
            weightUnits = units;
            if (bars == null) return;
            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i] == null) continue;
                var rt = bars[i].rectTransform;
                bool horizontal = Mathf.Approximately(rt.anchorMin.y, rt.anchorMax.y);
                var size = rt.sizeDelta;
                if (horizontal) size.y = units; else size.x = units;
                rt.sizeDelta = size;

                var position = rt.anchoredPosition;
                position.y = horizontal ? (i == 0 ? -inset - units * 0.5f : inset + units * 0.5f) : 0f;
                position.x = horizontal ? 0f : (i == 2 ? -inset - units * 0.5f : inset + units * 0.5f);
                rt.anchoredPosition = position;
            }
        }
    }
}
