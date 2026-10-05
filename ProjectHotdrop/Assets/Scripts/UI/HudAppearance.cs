using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>
    /// The single owner of in-match HUD panel appearance.
    ///
    /// <para><b>Why this exists.</b> The HUD had two owners and no authority. The editor-only
    /// <c>SceneSetup</c> baked each panel's colour into <c>Main.unity</c>, while <c>MatchHUD</c>
    /// built and restyled the live HUD at runtime. Changing the design token in the generator
    /// therefore changed nothing on screen, silently: the running game kept the serialized colour
    /// and no test failed. This type is the one place that decides what a floating match panel
    /// looks like, so both the generator and the runtime ask the same question and get the same
    /// answer.</para>
    ///
    /// <para><b>Direction of authority.</b> The token in <see cref="UIDesignTokens"/> is the
    /// source. <c>SceneSetup</c> calls <see cref="FloatingPanelColor"/> when it bakes a scene, and
    /// <c>MatchHUD</c> calls <see cref="ApplyFloatingPanels"/> on a live HUD. Neither may name a
    /// colour of its own.</para>
    /// </summary>
    public static class HudAppearance
    {
        /// <summary>
        /// Panels that float over the live arena and must read as glass. A player tracking an
        /// enemy loses too much peripheral awareness behind an opaque band.
        /// </summary>
        public static readonly string[] FloatingPanels =
        {
            "TopCenter", "ZoneInfo", "HPPanel", "AmmoPanel", "PadPanel", "Teammates",
        };

        /// <summary>The one colour rule for a floating match panel.</summary>
        public static Color FloatingPanelColor() => UIDesignTokens.HudGlass;

        /// <summary>
        /// The one colour rule for the caption plate behind a world-anchored marker. Both marker
        /// kinds land on arbitrary arena pixels, so they share this instead of each naming an alpha.
        /// </summary>
        public static Color MarkerPlateColor() => UIDesignTokens.WithAlpha(UIDesignTokens.Background, 0.62f);

        /// <summary>
        /// Applies the floating-panel rule to a live HUD tree. Safe to call on a HUD built either
        /// at runtime or deserialized from the scene: it matches by name and ignores anything
        /// that is not a floating panel, so a modal (death, preparation, results) stays opaque
        /// and is never mistaken for the fight.
        /// </summary>
        public static void ApplyFloatingPanels(RectTransform hudRoot)
        {
            if (hudRoot == null) return;
            foreach (string name in FloatingPanels)
            {
                var panel = hudRoot.Find(name);
                if (panel == null) continue;
                var image = panel.GetComponent<Image>();
                if (image == null) continue;
                image.color = FloatingPanelColor();
                // The panels are read-only score surfaces; catching drags here would eat
                // the fire/drag gestures that start on the same band of the screen.
                image.raycastTarget = false;
            }
        }
    }
}