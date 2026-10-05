using NUnit.Framework;
using UnityEngine;
using Hotdrop.UI;

namespace Hotdrop.Tests
{
    /// <summary>
    /// Found by rendering the live 1920x1080 match view: a world-projected ping landed on the
    /// score bar, so the ping caption and the zone status were drawn on top of each other and
    /// both became unreadable mid-fight. These pin the clamp that keeps markers out of the
    /// HUD's reserved bands, and the translucency of the panels that float over the arena.
    /// </summary>
    public class PingBandAndHudGlassTests
    {
        static readonly Rect Canvas = new Rect(-960f, -540f, 1920f, 1080f);

        [Test]
        public void PingFarUpTheScreen_IsPulledBelowTheScoreBar()
        {
            // A marker projected at the very top of the view would sit inside the score band.
            var clamped = PingSystem.ClampToReadableBand(new Vector2(0f, 540f), Canvas);
            Assert.Less(clamped.y, 540f, "a top-edge ping must not stay on the top edge");
            Assert.Greater(clamped.y, 0f, "it is pulled down into the readable band, not teleported");
        }

        [Test]
        public void PingNearTheBottom_IsLiftedAboveTheTouchControls()
        {
            var clamped = PingSystem.ClampToReadableBand(new Vector2(0f, -540f), Canvas);
            Assert.Greater(clamped.y, -540f);
            Assert.Less(clamped.y, 0f);
        }

        [Test]
        public void PingInTheMiddleOfTheScreen_IsNotMoved()
        {
            var point = new Vector2(120f, -40f);
            var clamped = PingSystem.ClampToReadableBand(point, Canvas);
            Assert.AreEqual(point.x, clamped.x, 0.001f, "a clear ping must not drift");
            Assert.AreEqual(point.y, clamped.y, 0.001f);
        }

        [Test]
        public void PingNearTheScreenEdge_IsKeptFullyVisible()
        {
            var clamped = PingSystem.ClampToReadableBand(new Vector2(-5000f, 0f), Canvas);
            Assert.GreaterOrEqual(clamped.x, Canvas.xMin, "a marker never leaves the canvas");
            var right = PingSystem.ClampToReadableBand(new Vector2(5000f, 0f), Canvas);
            Assert.LessOrEqual(right.x, Canvas.xMax);
        }

        [Test]
        public void HudGlass_IsTranslucent_SoTheArenaReadsThroughIt()
        {
            // The reference floats glass over the live scene; an opaque band hides the fight.
            Assert.Less(UIDesignTokens.HudGlass.a, UIDesignTokens.PanelBg.a,
                "in-match panels must be more transparent than menu panels");
            Assert.Greater(UIDesignTokens.HudGlass.a, 0.4f, "but dark enough to keep text legible");
        }

        [Test]
        public void HudGlass_UsesThePanelSurfaceNotAHardcodedColour()
        {
            Assert.AreEqual(UIDesignTokens.Hex(UIDesignTokens.SurfacePanel),
                UIDesignTokens.Hex(new Color(UIDesignTokens.HudGlass.r, UIDesignTokens.HudGlass.g, UIDesignTokens.HudGlass.b)),
                "HUD glass is the surface token at its own opacity, not a private RGB triple");
        }
    }
}