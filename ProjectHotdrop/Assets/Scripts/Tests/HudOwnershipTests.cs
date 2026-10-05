using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Hotdrop.UI;

namespace Hotdrop.Tests
{
    /// <summary>
    /// The in-match HUD had two owners and no authority: the editor-only SceneSetup baked each
    /// panel's colour into Main.unity while MatchHUD restyled the live tree, so changing the
    /// design token changed nothing on screen and no test failed. These pin the two single owners
    /// that close that split.
    /// </summary>
    public class HudOwnershipTests
    {
        static readonly Rect Canvas1080 = new Rect(-960f, -540f, 1920f, 1080f);

        GameObject NewHud()
        {
            var hud = new GameObject("Hud", typeof(RectTransform));
            foreach (string name in HudAppearance.FloatingPanels)
            {
                var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(hud.transform, false);
                // Deliberately wrong, the way a baked scene file would be: opaque and wrong hue.
                panel.GetComponent<Image>().color = new Color(1f, 0f, 0f, 1f);
                panel.GetComponent<Image>().raycastTarget = true;
            }
            // A modal that must NOT be flattened into glass.
            var modal = new GameObject("Results", typeof(RectTransform), typeof(Image));
            modal.transform.SetParent(hud.transform, false);
            modal.GetComponent<Image>().color = UIDesignTokens.PanelBg;
            return hud;
        }

        [Test]
        public void ApplyingTheRuntimeRuleOverridesWhateverTheSceneFileBaked()
        {
            // This is the regression that was silently no-op before: the baked colour won.
            var hud = NewHud();
            HudAppearance.ApplyFloatingPanels(hud.GetComponent<RectTransform>());

            foreach (string name in HudAppearance.FloatingPanels)
            {
                var image = hud.transform.Find(name).GetComponent<Image>();
                Assert.AreEqual(UIDesignTokens.Hex(UIDesignTokens.HudGlass), UIDesignTokens.Hex(image.color),
                    name + " must render the token, not the serialized value");
            }
            Object.DestroyImmediate(hud);
        }

        [Test]
        public void FloatingPanelsAreTranslucent_AndDoNotSwallowTouch()
        {
            var hud = NewHud();
            HudAppearance.ApplyFloatingPanels(hud.GetComponent<RectTransform>());
            foreach (string name in HudAppearance.FloatingPanels)
            {
                var image = hud.transform.Find(name).GetComponent<Image>();
                Assert.Less(image.color.a, 1f, name + " must read as glass over the live arena");
                Assert.IsFalse(image.raycastTarget, name + " must not eat fire/drag gestures");
            }
            Object.DestroyImmediate(hud);
        }

        [Test]
        public void ModalsAreLeftAlone_SoTheyAreNotMistakenForTheFight()
        {
            var hud = NewHud();
            HudAppearance.ApplyFloatingPanels(hud.GetComponent<RectTransform>());
            var results = hud.transform.Find("Results").GetComponent<Image>();
            Assert.AreEqual(UIDesignTokens.Hex(UIDesignTokens.PanelBg), UIDesignTokens.Hex(results.color),
                "a modal stays opaque");
            Object.DestroyImmediate(hud);
        }

        [Test]
        public void MissingPanelsAreIgnored_SoRuntimeAndBakedHUDsBothWork()
        {
            var bare = new GameObject("Bare", typeof(RectTransform));
            Assert.DoesNotThrow(() => HudAppearance.ApplyFloatingPanels(bare.GetComponent<RectTransform>()));
            Assert.DoesNotThrow(() => HudAppearance.ApplyFloatingPanels(null));
            Object.DestroyImmediate(bare);
        }

        [Test]
        public void TheGeneratorAndTheRuntimeResolveToTheSameColour()
        {
            // SceneSetup calls FloatingPanelColor(); MatchHUD calls ApplyFloatingPanels().
            // If these ever diverge, a regenerated scene would bake a colour the live HUD
            // immediately overwrites, which is the silent-no-op defect all over again.
            Assert.AreEqual(UIDesignTokens.Hex(UIDesignTokens.HudGlass),
                UIDesignTokens.Hex(HudAppearance.FloatingPanelColor()),
                "one token, one rule, both owners");
        }

        [Test]
        public void PingsAndTheObjectiveMarkerShareOneExclusionPolicy()
        {
            // Both used to clamp independently, in different coordinate spaces, with different
            // numbers (16%/26% vs 25%/30%). Neither matched the chrome, so the objective
            // diamond rendered on the score bar: "◆▼ ALLY 0 2:58 ◆ 0 ENEMY ◆".
            var top = Canvas1080.height * 0.5f;
            var viaPings = PingSystem.ClampToReadableBand(new Vector2(0f, top), Canvas1080);
            var viaObjective = HudMarkerLayer.ClampToReadableBand(new Vector2(0f, top), Canvas1080);
            Assert.AreEqual(viaObjective.x, viaPings.x, 0.0001f, "one policy, one result");
            Assert.AreEqual(viaObjective.y, viaPings.y, 0.0001f);
        }

        [Test]
        public void ReservedBandsAreMeasuredFromTheRealChromeNotGuessed()
        {
            // Score board (100 units @ y=-80) + zone caption (60 @ y=-190) reach ~22% of height.
            Assert.GreaterOrEqual(HudMarkerLayer.TopReservedFraction, 0.22f,
                "the band must cover the score bar and zone caption it was derived from");
            Assert.GreaterOrEqual(HudMarkerLayer.BottomReservedFraction, 0.26f,
                "and the touch controls plus HP plate at the bottom");
        }

        [Test]
        public void ObjectiveMarkerClampsIntoTheSameBand_AndNeverOntoTheScoreBar()
        {
            var clamped = HudMarkerLayer.ClampToReadableBand(new Vector2(0f, 540f), Canvas1080);
            float maxAllowed = 540f - Canvas1080.height * HudMarkerLayer.TopReservedFraction;
            Assert.LessOrEqual(clamped.y, maxAllowed + 0.001f, "no diamond on the score bar");
        }

        [Test]
        public void OffScreenObjectiveMarkerAlsoRespectsTheBands()
        {
            var safe = new Rect(0f, 0f, 2400f, 1080f);
            var edge = HudMarkerLayer.ClampToEdgeBand(new Vector2(-99999f, 99999f), safe, 96f);
            Assert.LessOrEqual(edge.y, safe.yMax - safe.height * HudMarkerLayer.TopReservedFraction + 0.001f);
            Assert.GreaterOrEqual(edge.y, safe.yMin + safe.height * HudMarkerLayer.BottomReservedFraction - 0.001f);
        }

        // ---- behind-camera branch: hit whenever the objective leaves the frustum ----

        static Rect Safe1080() => new Rect(0f, 0f, 1920f, 1080f);

        [Test]
        public void BehindCamera_ObjectivePointsTheWayTheObjectiveActuallyLies()
        {
            // Objective behind and to the left: the marker must go to the left edge, not the right.
            var safe = Safe1080();
            var behindLeft = HudMarkerLayer.ClampToEdgeBand(new Vector2(0f, 540f), safe, 96f);
            Assert.Greater(behindLeft.x, safe.xMin, "marker sits inside the safe area");
            Assert.Less(behindLeft.x, safe.center.x, "behind-left resolves to the left edge");
        }

        [Test]
        public void BehindCamera_StraightBehind_DoesNotDivideByZero()
        {
            // Degenerate input: the mirrored guess lands exactly on the safe-area centre, so
            // the direction vector is zero. This is the one input that could produce NaN.
            var safe = Safe1080();
            var edge = HudMarkerLayer.ClampToEdgeBand(safe.center, safe, 96f);
            Assert.IsFalse(float.IsNaN(edge.x) || float.IsNaN(edge.y), "must not produce NaN");
            Assert.Less(edge.y, safe.center.y, "a zero direction falls back to pointing down");
        }

        [Test]
        public void BehindCamera_EveryDirectionStaysInsideTheSafeArea()
        {
            var safe = Safe1080();
            // Sweep the full circle: a player can lose the objective anywhere.
            for (int deg = 0; deg < 360; deg += 15)
            {
                float rad = deg * Mathf.Deg2Rad;
                var guess = safe.center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 5000f;
                var edge = HudMarkerLayer.ClampToEdgeBand(guess, safe, 96f);
                Assert.IsFalse(float.IsNaN(edge.x) || float.IsNaN(edge.y), $"NaN at {deg} degrees");
                Assert.GreaterOrEqual(edge.x, safe.xMin - 0.001f, $"x below safe area at {deg} degrees");
                Assert.LessOrEqual(edge.x, safe.xMax + 0.001f, $"x above safe area at {deg} degrees");
                Assert.GreaterOrEqual(edge.y, safe.yMin + safe.height * HudMarkerLayer.BottomReservedFraction - 0.001f,
                    $"marker entered the bottom band at {deg} degrees");
                Assert.LessOrEqual(edge.y, safe.yMax - safe.height * HudMarkerLayer.TopReservedFraction + 0.001f,
                    $"marker entered the score band at {deg} degrees");
            }
        }

        [Test]
        public void BehindCamera_MarkerStaysClearOfTheBottomControlsEvenLookingDown()
        {
            var safe = Safe1080();
            var down = HudMarkerLayer.ClampToEdgeBand(new Vector2(960f, -5000f), safe, 96f);
            Assert.GreaterOrEqual(down.y, safe.yMin + safe.height * HudMarkerLayer.BottomReservedFraction - 0.001f,
                "an objective below the player must not be pinned under the thumb controls");
        }

        [Test]
        public void BehindCamera_HandlesACutoutSafeAreaNotTheFullScreen()
        {
            // A notched phone: the safe area is offset, so the bands must follow it, not the screen.
            var safe = new Rect(120f, 90f, 1680f, 900f);
            for (int deg = 0; deg < 360; deg += 30)
            {
                float rad = deg * Mathf.Deg2Rad;
                var guess = safe.center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 5000f;
                var edge = HudMarkerLayer.ClampToEdgeBand(guess, safe, 96f);
                Assert.IsFalse(float.IsNaN(edge.y), $"NaN at {deg} degrees on a cutout safe area");
                Assert.LessOrEqual(edge.y, safe.yMax - safe.height * HudMarkerLayer.TopReservedFraction + 0.001f);
                Assert.GreaterOrEqual(edge.y, safe.yMin + safe.height * HudMarkerLayer.BottomReservedFraction - 0.001f);
            }
        }

        /// <summary>
        /// The edge candidate for a bearing, expressed in canvas-local units.
        ///
        /// <para>Production hands <see cref="HudMarkerLayer.ClampToEdgeBand"/> the safe content
        /// rect from <see cref="SafeAreaRoot.ContentRectFor"/> and gets a canvas-local position
        /// back, exactly as written here. The sweep below therefore needs no camera and does not
        /// depend on how a headless editor sizes its game view; the only thing the parity screen
        /// is pinned for is the safe-area insets, which are the same two statics the capture sets.
        /// Unity's screen-to-canvas step, and whether the objective really is behind the camera,
        /// are covered separately, on a rendered frame.</para>
        /// </summary>
        static Vector2 EdgeCandidate(float degrees, Rect canvasLocal)
        {
            float rad = degrees * Mathf.Deg2Rad;
            // The safe content rect, not the raw canvas: this is what MatchHUD clamps against now
            // that the behind-camera branch reads its geometry from SafeAreaRoot. Pinning the
            // screen here is what lets a headless editor answer it at all - batchmode has no
            // meaningful Screen, and the parity capture simulates the screen through exactly
            // these two statics.
            Rect content = SafeAreaRoot.ContentRectFor(canvasLocal);
            float padding = HudMarkerLayer.EdgePaddingReference * (canvasLocal.height / 1080f);
            var guess = content.center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 5000f;
            return HudMarkerLayer.ClampToEdgeBand(guess, content, padding);
        }

        /// <summary>Runs a body with the parity capture's screen pinned, then restores it.</summary>
        static void WithParityScreen(System.Action body)
        {
            var screen = SafeAreaRoot.SimulatedScreen;
            var insets = SafeAreaRoot.SimulatedInsets;
            SafeAreaRoot.SimulatedScreen = new Vector4(1920f, 1080f, 400f, 0f);
            SafeAreaRoot.SimulatedInsets = ScreenInsets.None;
            try { body(); }
            finally
            {
                SafeAreaRoot.SimulatedScreen = screen;
                SafeAreaRoot.SimulatedInsets = insets;
            }
        }

        /// <summary>Panels measured on the preparation screen: the weapon-select panel, the
        /// preparation plate, and the teammate list, which is the one a left-edge footprint
        /// actually crosses.</summary>
        static List<Rect> PreparationPanels() => new List<Rect>
            { PreparationWeaponPanel, PreparationPlate, TeammatesPanel };

        [Test]
        public void BehindCamera_TheEdgeMarkerGetsTheSameMeasuredClearanceAsTheProjectedOne()
        {
            // This branch runs whenever the objective leaves the frustum - which in a six-player
            // match is most of the time. It used to end at the edge clamp: no panels measured and
            // no clearance required, so the marker could sit on the teammate or weapon panel while
            // both were up. The fix routes it through the same AvoidPanels call, and this asserts
            // the outcome over every bearing a player can lose the objective in.
            //
            // Geometry says the band does NOT need to move: WeaponSelect (y -190..70) and
            // Preparation (y 110..190) leave y 70..110, which is the slot the on-screen path
            // already uses. The one place it runs out is measured in the companion test below,
            // which is why that one exists rather than this one quietly tolerating it.
            var panels = PreparationPanels();
            WithParityScreen(() => SweepEdgeBearings(panels));
        }

        static void SweepEdgeBearings(List<Rect> panels)
        {
            int total = 0, moved = 0, cleared = 0, gaveUp = 0;
            for (int deg = 0; deg < 360; deg += 15)
            {
                var raw = EdgeCandidate(deg, PreparationCanvas);
                float before = HudMarkerLayer.ClearanceAt(raw, panels);
                var placed = HudMarkerLayer.AvoidPanels(raw, panels, PreparationCanvas,
                    HudMarkerLayer.MarkerRowHeight);
                float after = HudMarkerLayer.ClearanceAt(placed, panels);
                total++;

                if (before >= HudMarkerLayer.MarkerClearance)
                {
                    // Already clear: the rule must not drag it off the edge it is reporting.
                    Assert.AreEqual(raw, placed,
                        $"at {deg} degrees the edge marker had {before:0.00} units of clearance " +
                        "and was still moved");
                    cleared++;
                    continue;
                }

                moved++;
                if (after >= HudMarkerLayer.MarkerClearance) { cleared++; continue; }

                // No legal slot here. Giving up must at least not throw the marker somewhere worse.
                gaveUp++;
                Assert.AreEqual(raw, placed,
                    $"at {deg} degrees the rule could not reach the margin and still moved the " +
                    $"marker from {raw} to {placed}");
                Assert.GreaterOrEqual(after, before,
                    $"at {deg} degrees placement made the edge marker worse: {before:0.00} -> {after:0.00}");
                float floor = -PreparationCanvas.height * 0.5f +
                    PreparationCanvas.height * HudMarkerLayer.BottomReservedFraction;
                float ceiling = PreparationCanvas.height * 0.5f -
                    PreparationCanvas.height * HudMarkerLayer.TopReservedFraction;
                Assert.GreaterOrEqual(placed.y, floor - 0.01f,
                    $"at {deg} degrees giving up dropped the marker under the thumb controls");
                Assert.LessOrEqual(placed.y, ceiling + 0.01f,
                    $"at {deg} degrees giving up pushed the marker into the score band");
            }

            Assert.Greater(moved, 0,
                $"none of the {total} bearings put the edge marker on a panel, so this test cannot " +
                "show the behind-camera path being placed at all. The panels moved; update them " +
                "here and re-check the geometry claim above.");
            Assert.GreaterOrEqual(cleared * 3, total * 2,
                $"only {cleared} of {total} bearings reached the required clearance, and " +
                $"{gaveUp} had no slot. The residual no-slot arc is measured in " +
                "BehindCamera_TheOnlyBearingsWithNoSlotAreLeftAndOnlyWhileTheWeaponPanelIsUp; if " +
                "that one still passes and this does not, something new is in the way.");
        }

        [Test]
        public void BehindCamera_TheOnlyBearingsWithNoSlotAreLeftAndOnlyWhileTheWeaponPanelIsUp()
        {
            // Measured, not wished, and this test exists so it stays that way. Seven of the
            // twenty-four bearings have no legal position - 135 through 225 in 15-degree steps,
            // contiguous, every one of them on the left arc. The arithmetic:
            //
            // The edge fixes the marker's X, so only Y is free to search. Clamped against the
            // safe content rect (x -565..565, y -335..322.5 at the parity screen, from the 75/25/
            // 37.5-unit dp reserves) and 64 units of edge padding, a straight-left bearing sits
            // at x -501 and the diagonal ones at -458.56 and -264.75. A 240-wide footprint
            // therefore spans the teammate list (x -590..-370) at every one of those anchors,
            // and Teammates (y 20..180) together with WeaponSelect (y -190..70) covers the whole
            // band y -164.05..164.70 - there is no free span to aim at and no row that clears.
            // Miss the teammate list and the free span 70..110 reappears, exactly 39 units, and
            // the bearing places at y 90 with 2.00 units on each side. That is the whole rule:
            // blocked exactly when the footprint crosses Teammates, which is what the loop at the
            // end of BlockedBearings asserts.
            //
            // It used to be four bearings, because the edge was built from the raw screen and so
            // sat at x -576 - outside the teammate panel entirely. Reading the same edge through
            // SafeAreaRoot is what moved it to -501, inside the overlap window, and the arc
            // widened at both ends to meet it. The edge is now the one the rest of the HUD is
            // laid out from, edge margin and cutout included, so the wider arc is the honest
            // number and this is where it stays recorded.
            //
            // No band or footprint change fixes it without costing something real. Raising the
            // ceiling past Teammates needs exactly 39 units above y 180 - knife-edge again, and
            // it lands inside the transient feedback strip and under the zone caption. Lowering
            // the floor past WeaponSelect puts markers in the thumb zone that
            // BottomReservedFraction exists to protect. Narrowing the footprint shrinks the
            // window but never closes it, because the anchor is pinned to the edge. So it is
            // reported and bounded rather than papered over with a special case.
            //
            // The second half is what makes this acceptable: take the weapon panel down - what
            // any ordinary fight looks like - and every one of the twenty-four bearings places,
            // the worst of them with 8.25 units to spare. The residual needs the preparation
            // panel specifically, plus a player facing away from the zone while it is up. The
            // left arc is where the teammate list lives, so it is not a rare bearing.
            var preparation = PreparationPanels();
            var fighting = new List<Rect> { TeammatesPanel };
            WithParityScreen(() => { _ = BlockedBearings(preparation, fighting); });
        }

        static List<int> BlockedBearings(List<Rect> preparation, List<Rect> fighting)
        {
            var blocked = new List<int>();
            for (int deg = 0; deg < 360; deg += 15)
            {
                var raw = EdgeCandidate(deg, PreparationCanvas);

                var duringFight = HudMarkerLayer.AvoidPanels(raw, fighting, PreparationCanvas,
                    HudMarkerLayer.MarkerRowHeight);
                float fought = HudMarkerLayer.ClearanceAt(duringFight, fighting);
                Assert.GreaterOrEqual(fought, HudMarkerLayer.MarkerClearance,
                    $"at {deg} degrees, with the weapon panel down, the edge marker still only had " +
                    $"{fought:0.00} units. If this fails, the residual is no longer confined to the " +
                    "preparation panel and the geometry claim above is stale.");

                var duringPreparation = HudMarkerLayer.AvoidPanels(raw, preparation, PreparationCanvas,
                    HudMarkerLayer.MarkerRowHeight);
                if (HudMarkerLayer.ClearanceAt(duringPreparation, preparation) < HudMarkerLayer.MarkerClearance)
                    blocked.Add(deg);
            }

            Assert.Greater(blocked.Count, 0,
                "no bearing is blocked any more, so this test no longer describes the residual. " +
                "Good news if it is real - but the companion sweep test would need updating too.");
            Assert.AreEqual(7, blocked.Count,
                $"the blocked set is {blocked.Count} of 24 ({string.Join(",", blocked)}); seven on " +
                "the left arc is what was measured. A different number means the claim above no " +
                "longer describes this geometry - re-measure it rather than widening the bound.");
            foreach (int deg in blocked)
                Assert.Less(EdgeCandidate(deg, PreparationCanvas).x, 0f,
                    $"bearing {deg} is blocked but sits on the right; the residual is supposed to be " +
                    "the left side only, and its cause is the teammate panel's x range");

            // The cause, stated as a predicate rather than a list: the left edge fixes the
            // marker's X, and the free-span search can only move it in Y, so a bearing has a slot
            // exactly when its 240-wide footprint misses the teammate list. If this stops
            // holding, the residual has a different cause and the arithmetic above is wrong.
            for (int deg = 0; deg < 360; deg += 15)
            {
                var footprint = HudMarkerLayer.FootprintBox(EdgeCandidate(deg, PreparationCanvas));
                // Horizontal only, which is the rule's own test: the free-span scan decides which
                // panels the footprint reaches by x and then has to find a Y between them, so a
                // footprint that spans the teammate list is blocked whatever height it arrives at.
                bool crossesTeammates = footprint.xMax > TeammatesPanel.xMin && footprint.xMin < TeammatesPanel.xMax;
                Assert.AreEqual(crossesTeammates, blocked.Contains(deg),
                    $"bearing {deg}: footprint x {footprint.xMin:0.00}..{footprint.xMax:0.00} spans " +
                    $"the teammate panel x {TeammatesPanel.xMin:0.00}..{TeammatesPanel.xMax:0.00} = " +
                    $"{crossesTeammates}, blocked = {blocked.Contains(deg)}. The residual is " +
                    "supposed to be exactly the bearings whose footprint spans that panel.");
            }
            return blocked;
        }

        [Test]
        public void ProjectionRefusesToInventAPositionBehindTheCamera()
        {
            // Empty input: no camera or no canvas must return false, never a stale zero.
            Assert.IsFalse(HudMarkerLayer.ProjectToCanvas(null, null, Vector3.one, out var none),
                "no camera is not a position");
            Assert.AreEqual(Vector2.zero, none);
        }

        [Test]
        public void ScoreBarNamesTheTeamSoBothSidesReadTheSameWay()
        {
            // The capture showed "ALLY 0" on the left and "0 ENEMY" on the right: flipped word
            // order, and neither side said which team the player was on.
            var onA = MatchHUD.ScoreLines(localIsA: true, scoreA: 12, scoreB: 34);
            StringAssert.Contains("TEAM A", onA.Left);
            StringAssert.Contains("YOU", onA.Left, "the local team must be marked");
            StringAssert.Contains("12", onA.Left, "the ally score is on the player's side");
            StringAssert.Contains("TEAM B", onA.Right, "the rival side names its team too");
            StringAssert.Contains("34", onA.Right);
            StringAssert.DoesNotContain("ENEMY", onA.Right,
                "in a 3v3 the other team is not 'enemy' - the player fights alongside them");
        }

        [Test]
        public void ScoreBarSwapsSidesButNeverSwapsTheRule()
        {
            var onA = MatchHUD.ScoreLines(localIsA: true, scoreA: 12, scoreB: 34);
            var onB = MatchHUD.ScoreLines(localIsA: false, scoreA: 12, scoreB: 34);
            // Playing Team B: B is the local side and must be marked, A is the rival.
            StringAssert.Contains("TEAM B", onB.Left);
            StringAssert.Contains("YOU", onB.Left);
            StringAssert.Contains("34", onB.Left, "34 is Team B's score");
            StringAssert.Contains("TEAM A", onB.Right);
            StringAssert.Contains("12", onB.Right);
            // Same grammar either way: the left caption leads with the team name, the right
            // caption ends with it, so the bar never changes shape mid-match.
            Assert.IsTrue(onB.Left.StartsWith("TEAM B"), "left leads with the local team name");
            Assert.IsTrue(onA.Left.StartsWith("TEAM A"), "left leads with the local team name");
            Assert.IsTrue(onA.Right.EndsWith("TEAM B"), "right ends with the rival team name");
            Assert.IsTrue(onB.Right.EndsWith("TEAM A"), "right ends with the rival team name");
            Assert.IsTrue(onA.Left.EndsWith("12"), "the local score sits adjacent to the clock");
            Assert.IsTrue(onA.Right.StartsWith("34"), "and so does the rival's");
        }

        // --- Marker vs marker -----------------------------------------------------------------
        // The bands stop a marker landing on HUD chrome but say nothing about two markers landing
        // on each other. The 1920x1080 capture is where that showed: an objective 34 m out and an
        // enemy ping 52 m out projected close enough to print "ENEMY HERE 52m" through
        // "CENTER 34m", and neither caption was readable.

        [Test]
        public void TwoMarkersProjectedOntoEachOtherArePushedApart()
        {
            // What the capture showed: roughly 20 px apart on both axes.
            var taken = new List<Vector2> { new Vector2(100f, 40f) };
            var result = HudMarkerLayer.SeparateFromPlaced(new Vector2(120f, 52f), taken, Canvas1080);

            Assert.GreaterOrEqual(Mathf.Abs(result.y - taken[0].y), HudMarkerLayer.MarkerCaptionSize.y,
                "the caption must clear the caption it was printed over");
            Assert.AreEqual(120f, result.x, 0.01f, "only the row changes; horizontal aim is untouched");
        }

        [Test]
        public void MarkersThatAreAlreadyClearAreNotMoved()
        {
            var taken = new List<Vector2> { new Vector2(-400f, 40f) };
            var candidate = new Vector2(120f, 60f);
            Assert.AreEqual(candidate, HudMarkerLayer.SeparateFromPlaced(candidate, taken, Canvas1080),
                "a marker with no neighbour must stay exactly where gameplay put it");
        }

        [Test]
        public void SideBySideCaptionsAreSeparatedEvenWhenTheyAreOnlyNarrowApart()
        {
            // 200 px wide captions overlap well past the row pitch: 60 px apart horizontally is
            // still a smear, so the rule has to test the caption box and not a square of the pitch.
            var taken = new List<Vector2> { new Vector2(0f, 0f) };
            var result = HudMarkerLayer.SeparateFromPlaced(new Vector2(60f, 10f), taken, Canvas1080);
            Assert.GreaterOrEqual(Mathf.Abs(result.y), HudMarkerLayer.MarkerCaptionSize.y);
        }

        [Test]
        public void AWholeClusterOfMarkersEndsUpWithEveryCaptionReadable()
        {
            // Eight pings plus an objective, all projected into the same small neighbourhood -
            // the worst case the placement rule has to absorb without a game ever arranging it.
            var taken = new List<Vector2>();
            for (int i = 0; i < 8; i++)
            {
                var point = new Vector2(30f + i * 2f, 20f + i * 1.5f);
                taken.Add(HudMarkerLayer.SeparateFromPlaced(point, taken, Canvas1080));
            }
            var objective = HudMarkerLayer.SeparateFromPlaced(new Vector2(44f, 28f), taken, Canvas1080);
            taken.Add(objective);

            for (int i = 0; i < taken.Count; i++)
                for (int j = i + 1; j < taken.Count; j++)
                {
                    bool clash = Mathf.Abs(taken[i].x - taken[j].x) < HudMarkerLayer.MarkerCaptionSize.x &&
                                 Mathf.Abs(taken[i].y - taken[j].y) < HudMarkerLayer.MarkerCaptionSize.y;
                    Assert.IsFalse(clash, $"captions {i} and {j} still overlap at {taken[i]} / {taken[j]}");
                }
        }

        [Test]
        public void SeparationNeverPushesACaptionIntoTheHudBands()
        {
            // The objective was already clamped into the readable band; resolving a collision
            // must not push it back out through the score bar or into the touch controls.
            float floorY = Canvas1080.yMin + Canvas1080.height * HudMarkerLayer.BottomReservedFraction;
            float ceilingY = Canvas1080.yMax - Canvas1080.height * HudMarkerLayer.TopReservedFraction;
            var taken = new List<Vector2> { new Vector2(0f, ceilingY - 10f) };
            var result = HudMarkerLayer.SeparateFromPlaced(new Vector2(4f, ceilingY - 12f), taken, Canvas1080);
            Assert.GreaterOrEqual(result.y, floorY - 0.01f, "must not escape below the readable band");
            Assert.LessOrEqual(result.y, ceilingY + 0.01f, "must not escape above the readable band");
        }

        [Test]
        public void SeparationGivesUpRatherThanLoopingWhenTheBandIsFull()
        {
            // A wall of neighbours exactly one pitch apart: there is no free row, so the rule must
            // stop. It must still return a usable point, not an off-band escape or a hang.
            var taken = new List<Vector2>();
            for (int i = 0; i < 8; i++) taken.Add(new Vector2(0f, 150f - i * HudMarkerLayer.MarkerRowHeight));
            var result = HudMarkerLayer.SeparateFromPlaced(new Vector2(0f, 150f), taken, Canvas1080);
            Assert.IsFalse(float.IsNaN(result.x) || float.IsNaN(result.y), "must stay a usable point");
            float floorY = Canvas1080.yMin + Canvas1080.height * HudMarkerLayer.BottomReservedFraction;
            float ceilingY = Canvas1080.yMax - Canvas1080.height * HudMarkerLayer.TopReservedFraction;
            Assert.GreaterOrEqual(result.y, floorY - 0.01f);
            Assert.LessOrEqual(result.y, ceilingY + 0.01f);
        }

        // --- Marker vs HUD panels -------------------------------------------------------------------
        // The reserved bands are two fixed fractions measured from the gameplay HUD. The screen 07
        // capture showed what they miss: during preparation the weapon-select panel occupies the
        // lower middle, inside the readable band, and the objective caption printed across it.

        static readonly Rect WeaponPanel = new Rect(-675f, -220f, 1350f, 200f);

        [Test]
        public void AMarkerInsideAPanelIsMovedClearOfIt()
        {
            var panels = new List<Rect> { WeaponPanel };
            var result = HudMarkerLayer.AvoidPanels(new Vector2(0f, -120f), panels, Canvas1080);
            Assert.IsFalse(HudMarkerLayer.OverlapsPanel(result, panels),
                $"the caption must not stay inside the panel; landed at {result}");
        }

        [Test]
        public void AvoidPanelsPrefersMovingUpAwayFromTheThumbZones()
        {
            var panels = new List<Rect> { WeaponPanel };
            var result = HudMarkerLayer.AvoidPanels(new Vector2(0f, -120f), panels, Canvas1080);
            Assert.Greater(result.y, -120f, "the free direction is upward, away from the touch controls");
        }

        [Test]
        public void AvoidPanelsLeavesAMarkerThatIsAlreadyClearAlone()
        {
            var panels = new List<Rect> { WeaponPanel };
            var candidate = new Vector2(0f, 240f);
            Assert.AreEqual(candidate, HudMarkerLayer.AvoidPanels(candidate, panels, Canvas1080),
                "a marker with no panel under it must stay exactly where gameplay put it");
        }

        [Test]
        public void AvoidPanelsKeepsTheCaptionInsideTheReadableBand()
        {
            var panels = new List<Rect> { WeaponPanel };
            float floorY = Canvas1080.yMin + Canvas1080.height * HudMarkerLayer.BottomReservedFraction;
            float ceilingY = Canvas1080.yMax - Canvas1080.height * HudMarkerLayer.TopReservedFraction;
            // A panel wide enough to swallow the whole readable band: escape is impossible, so the
            // rule must keep the original placement rather than push the caption into the score bar.
            var everything = new List<Rect> { new Rect(-960f, floorY, 1920f, ceilingY - floorY) };
            var result = HudMarkerLayer.AvoidPanels(new Vector2(0f, 0f), everything, Canvas1080);
            Assert.AreEqual(new Vector2(0f, 0f), result, "no free row means keep the original placement");

            var result2 = HudMarkerLayer.AvoidPanels(new Vector2(0f, -120f), panels, Canvas1080);
            Assert.GreaterOrEqual(result2.y, floorY - 0.01f, "must not escape below the readable band");
            Assert.LessOrEqual(result2.y, ceilingY + 0.01f, "must not escape above the readable band");
        }

        [Test]
        public void AvoidPanelsHandlesAPanelTheCaptionOnlyOverlapsHorizontally()
        {
            // Panel far to the side: the caption box must be considered as a box, not a point.
            var panels = new List<Rect> { new Rect(-1500f, -220f, 1350f, 200f) };
            var result = HudMarkerLayer.AvoidPanels(new Vector2(0f, -120f), panels, Canvas1080);
            Assert.AreEqual(new Vector2(0f, -120f), result, "no overlap means no movement");
        }

        [Test]
        public void AvoidPanelsHandlesTheDegenerateInputs()
        {
            var candidate = new Vector2(3f, 4f);
            Assert.AreEqual(candidate, HudMarkerLayer.AvoidPanels(candidate, null, Canvas1080));
            Assert.AreEqual(candidate, HudMarkerLayer.AvoidPanels(candidate, new List<Rect>(), Canvas1080));
            Assert.AreEqual(candidate,
                HudMarkerLayer.AvoidPanels(candidate, new List<Rect> { WeaponPanel }, Canvas1080, 0f),
                "a zero pitch would never terminate; it must be refused instead of applied");
        }

        /// <summary>
        /// The real preparation-screen geometry, measured off the 1920x1080 capture at the scale the
        /// harness renders it: canvas 1280x720, weapon panel y -190..70, preparation plate y 110..190.
        /// The only free gap is y 70..110 - forty units, enough for the thirty-unit caption, but it
        /// sits between two rows of the forty-four-unit sweep, so the sweep alone cannot reach it.
        /// </summary>
        static readonly Rect PreparationCanvas = new Rect(-640f, -360f, 1280f, 720f);
        static readonly Rect PreparationWeaponPanel = new Rect(-450f, -190f, 900f, 260f);
        static readonly Rect PreparationPlate = new Rect(-310f, 110f, 620f, 80f);

        /// <summary>
        /// The measured Teammates panel, same convention as the two rects above: read off the
        /// scene and the capture rather than recomputed here. It is the only one of the three that
        /// a left-edge marker footprint actually crosses, which is what makes the behind-camera
        /// path non-vacuous.
        /// </summary>
        static readonly Rect TeammatesPanel = new Rect(-590f, 20f, 220f, 160f);

        [Test]
        public void AvoidPanelsFindsTheGapTheRowPitchCannotReach()
        {
            var panels = new List<Rect> { PreparationWeaponPanel, PreparationPlate };
            var result = HudMarkerLayer.AvoidPanels(new Vector2(0f, 0f), panels, PreparationCanvas,
                HudMarkerLayer.MarkerRowHeight);

            Assert.AreNotEqual(new Vector2(0f, 0f), result,
                "the caption sat on the weapon panel; the rule must move it even when no row fits");
            Assert.IsFalse(HudMarkerLayer.OverlapsPanel(result, panels),
                $"the caption must end up clear of both panels; landed at {result}");
        }

        [Test]
        public void TheGapTheRuleUsesIsTheOneThatWasLeftFree()
        {
            var panels = new List<Rect> { PreparationWeaponPanel, PreparationPlate };
            var result = HudMarkerLayer.AvoidPanels(new Vector2(0f, 0f), panels, PreparationCanvas,
                HudMarkerLayer.MarkerRowHeight);
            var box = HudMarkerLayer.FootprintBox(result);
            Assert.Greater(box.yMin, PreparationWeaponPanel.yMax - 0.01f,
                "it belongs in the gap above the weapon panel, not somewhere invented");
            Assert.Less(box.yMax, PreparationPlate.yMin + 0.01f,
                "and it must not run into the preparation plate above it");
        }

        [Test]
        public void TheMeasuredPreparationSlotIsNotSpentToZeroSlack()
        {
            // The frame this pass caught cleared the weapon panel by 1.03 units - 1.5 px at
            // 1080p. Non-overlap cannot see that: it is "clear". So the placement rule now
            // promises a deliberate margin and the outcome is held to it here, on the real
            // measured geometry rather than on a diagram.
            var panels = new List<Rect> { PreparationWeaponPanel, PreparationPlate };
            var result = HudMarkerLayer.AvoidPanels(new Vector2(0f, 0f), panels, PreparationCanvas,
                HudMarkerLayer.MarkerRowHeight);

            float clearance = HudMarkerLayer.ClearanceAt(result, panels);
            Assert.GreaterOrEqual(clearance, HudMarkerLayer.MarkerClearance,
                $"marker landed at {result} keeping only {clearance:0.000} units from the nearest " +
                $"panel, and the rule requires {HudMarkerLayer.MarkerClearance}. A clear-but-" +
                "knife-edge placement is what this assertion exists to reject.");
            // And it must be the *roomiest* placement, not merely one that clears. The slot is
            // exactly as tall as footprint + 2 * clearance, so its only legal position is the
            // middle - and that is precisely the placement first fit could not find, having
            // stopped one row short with 1.03 units of air on one side and nothing on the other.
            float slotMiddle = (PreparationWeaponPanel.yMax + PreparationPlate.yMin) * 0.5f;
            Assert.AreEqual(slotMiddle, result.y, 0.01f,
                $"a clearance of exactly {HudMarkerLayer.MarkerClearance} leaves one legal position " +
                $"in this slot, its middle at {slotMiddle}, but the rule settled for {result}. " +
                "First fit that merely clears is what put the marker 1.5 px from the panel.");
        }

        [Test]
        public void TheClearanceLeavesSlackInTheSlotRatherThanConsumingIt()
        {
            // Pins *why* the margin is not larger, because "the biggest that fits" is a
            // trap. The binding constraint is the declared footprint: a candidate slot must
            // hold MarkerFootprintSize.y + 2C, and the only free slot on this screen is
            // forty units tall. Forty minus thirty-six over two is exactly 2.0 - so a
            // clearance of 2 needs 37.94 and appears to leave 2.06 of slack.
            //
            // It was tried, on the live capture frame, and it fails: panel rects are laid out
            // in floats, the real slot measures 39.9993 rather than 40, and 36 + 2*2 then
            // exceeds it by four thousandths of a unit. No candidate reaches the margin, the
            // rule falls back to the original placement, and the marker is left on the weapon
            // panel - the requirement silently disabling the rule instead of being met.
            // So the margin has to leave slack in the slot, not consume it.
            float gap = PreparationPlate.yMin - PreparationWeaponPanel.yMax;
            float footprint = HudMarkerLayer.MarkerFootprintSize.y;
            float c = HudMarkerLayer.MarkerClearance;

            Assert.LessOrEqual(footprint + 2f * c, gap - 1f,
                $"footprint {footprint} plus two clearances of {c} is {footprint + 2f * c}, leaving " +
                $"less than a unit of the {gap}-unit slot. The slot measures a hair under {gap} once " +
                "float layout is applied, so a margin that consumes the slot finds nowhere legal to " +
                "go, gives up, and leaves the marker on the weapon panel - exactly what a clearance " +
                "of 2 did before it was measured against a live frame.");

            // And it must still be enough to reject the 1.03-unit placement that started
            // this, or the margin exists and does not do any work.
            Assert.Greater(c, 1.03f,
                "the old first-fit placement cleared the panel by 1.03 units; a margin at or below " +
                "that accepts the very frame this replaced");
        }

        [Test]
        public void TheToastBandClearsTheMarkerBand()
        {
            // The defect this pins: transient feedback is readable HUD that is not a panel, and when
            // it was laid out inside the marker band it took the only gap a marker had on the
            // preparation screen. Measured on the 720-unit canvas the 1920x1080 capture renders.
            float ceilingY = PreparationCanvas.yMax - PreparationCanvas.height * HudMarkerLayer.TopReservedFraction;
            float inkBottom = Hotdrop.UI.MatchHUD.CombatFeedbackBandY - Hotdrop.UI.MatchHUD.CombatFeedbackInkHalfHeight;

            Assert.Greater(inkBottom, ceilingY,
                $"transient feedback ink starting at {inkBottom} enters the marker band, which ends at " +
                $"{ceilingY}; it would contend with every marker on this screen");

            // And it must still fit in the strip it was given, above the band and below the zone
            // caption measured at y=222 in the same capture.
            float inkTop = Hotdrop.UI.MatchHUD.CombatFeedbackBandY + Hotdrop.UI.MatchHUD.CombatFeedbackInkHalfHeight;
            Assert.Less(inkTop, 222f,
                $"transient feedback ink reaching {inkTop} runs into the zone caption at 222");
        }

        [Test]
        public void TheProtectedFootprintIsTheWholeMarkerNotJustItsCaption()
        {
            // The rule protects one box, and both marker kinds are laid out inside it. Icon above
            // caption made the objective 74 units tall against a 40-unit gap, which is why this is
            // asserted rather than left to the capture: a future "tidy" that restacks the icon and
            // caption would put the marker back where no placement rule can clear it.
            float gap = PreparationPlate.yMin - PreparationWeaponPanel.yMax;
            Assert.LessOrEqual(HudMarkerLayer.MarkerFootprintSize.y, gap,
                $"the whole marker ({HudMarkerLayer.MarkerFootprintSize.y} tall) must fit the only " +
                $"free gap on the preparation screen ({gap} tall), or no rule can place it there");

            // And the protected box must contain both parts of the drawn marker.
            var box = HudMarkerLayer.FootprintBox(new Vector2(0f, 0f));
            float iconLeft = HudMarkerLayer.MarkerIconCenterX - 17f;
            float iconRight = HudMarkerLayer.MarkerIconCenterX + 17f;
            float captionLeft = HudMarkerLayer.MarkerCaptionCenterX - HudMarkerLayer.MarkerCaptionSize.x * 0.5f;
            float captionRight = HudMarkerLayer.MarkerCaptionCenterX + HudMarkerLayer.MarkerCaptionSize.x * 0.5f;
            Assert.LessOrEqual(box.xMin, iconLeft, "the icon sits inside the protected footprint, not outside it");
            Assert.LessOrEqual(iconRight, box.xMax, "the icon's right edge must also be inside it");
            Assert.LessOrEqual(box.xMin, captionLeft, "the caption sits inside the protected footprint");
            Assert.LessOrEqual(captionRight, box.xMax, "the caption's right edge must also be inside it");
            Assert.LessOrEqual(iconRight, captionLeft, "icon and caption must not be drawn on top of each other");
        }

        [Test]
        public void TheIconIsProtectedNotJustTheCaption()
        {
            // A rule testing only the caption would report clear for a marker whose icon still sat
            // on the panel. That is the exact miss the capture had: the caption cleared, the diamond
            // kept printing across the text, and the frame passed its assertion.
            // A panel that reaches the icon on the marker's left but stops short of the caption: the
            // caption box clears it, the icon does not. Checking only the caption reports clear.
            var beside = new Rect(-160f, 80f, 70f, 36f);
            var panels = new List<Rect> { beside };
            var anchor = new Vector2(0f, 98f);
            var captionOnly = new Rect(HudMarkerLayer.MarkerCaptionCenterX - HudMarkerLayer.MarkerCaptionSize.x * 0.5f,
                anchor.y - HudMarkerLayer.MarkerCaptionSize.y * 0.5f,
                HudMarkerLayer.MarkerCaptionSize.x, HudMarkerLayer.MarkerCaptionSize.y);
            Assert.IsFalse(beside.Overlaps(captionOnly),
                $"the caption alone must be clear here, or this test is not the case it claims: {captionOnly} vs {beside}");
            var footprint = HudMarkerLayer.FootprintBox(anchor);
            Assert.IsTrue(beside.Overlaps(footprint),
                $"the whole marker must be blocked here, or this test proves nothing: {footprint} vs {beside}");

            var moved = HudMarkerLayer.AvoidPanels(anchor, panels, PreparationCanvas,
                HudMarkerLayer.MarkerRowHeight);
            var box = HudMarkerLayer.FootprintBox(moved);
            for (int i = 0; i < panels.Count; i++)
                Assert.IsFalse(panels[i].Overlaps(box),
                    $"after moving to {moved}, the icon and caption box {box} still sits on panel {i} {panels[i]}");
        }

        [Test]
        public void SeparationHandlesTheDegenerateInputs()
        {
            Assert.AreEqual(Vector2.one, HudMarkerLayer.SeparateFromPlaced(Vector2.one, null, Canvas1080),
                "no placements yet is not a collision");
            Assert.AreEqual(Vector2.one, HudMarkerLayer.SeparateFromPlaced(Vector2.one, new List<Vector2>(), Canvas1080));
            Assert.AreEqual(Vector2.one,
                HudMarkerLayer.SeparateFromPlaced(Vector2.one, new List<Vector2> { Vector2.zero }, Canvas1080, 0f),
                "a zero pitch would divide the search; it must be refused instead of applied");
        }
    }
}