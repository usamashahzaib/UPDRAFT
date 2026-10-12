using System.Collections.Generic;
using UnityEngine;

namespace Hotdrop.UI
{
    /// <summary>
    /// The single owner of world-to-screen projection and HUD exclusion for floating match markers
    /// (teammate pings and the objective marker).
    ///
    /// <para><b>Why this exists.</b> Both callers used to do this themselves, with different
    /// numbers in different coordinate spaces: pings clamped to 16%/26% of the canvas rect, the
    /// objective marker clamped to 25%/30% of the safe area. Neither matched the height the score
    /// bar and zone caption actually occupy, so in the 1920x1080 capture the objective diamond
    /// rendered on top of the score bar and read as `◆▼ ALLY 0  2:58 ◆ 0 ENEMY ◆`. Two owners
    /// meant no owner: the exclusion policy could not be changed in one place and be sure both
    /// kinds of marker honoured it.</para>
    ///
    /// <para><b>Coordinate spaces.</b> <see cref="ProjectToCanvas"/> works in canvas-local units
    /// (the space markers are positioned in). <see cref="ClampToEdgeBand"/> works in screen
    /// space because the objective marker needs the real safe area when it is off-screen. Keeping
    /// both here is what stops a caller from mixing them.</para>
    ///
    /// <para>Everything is static and side-effect free so the rules can be tested without a
    /// camera, a canvas or a running match.</para>
    /// </summary>
    public static class HudMarkerLayer
    {
        /// <summary>
        /// Bands the HUD reserves, as fractions of the canvas height.
        ///
        /// Measured from the 1920x1080 gameplay capture rather than guessed: the score board
        /// (100 units at y=-80) plus the zone caption (60 units at y=-190) reach ~22% of screen
        /// height, and the touch controls plus the HP plate occupy ~26% at the bottom. A marker
        /// inside either band is drawn over chrome the player is reading.
        /// </summary>
        public const float TopReservedFraction = 0.24f;
        public const float BottomReservedFraction = 0.26f;

        /// <summary>Edge padding for an off-screen objective marker, in reference pixels at 1080p.</summary>
        public const float EdgePaddingReference = 96f;

        /// <summary>
        /// Footprint every world-anchored marker caption is laid out to, in reference pixels at
        /// 1080p. Both marker kinds build their caption to this size, so the overlap rule below
        /// has one honest number instead of guessing each caller's label box.
        /// </summary>
        public static readonly Vector2 MarkerCaptionSize = new Vector2(200f, 30f);

        /// <summary>
        /// The whole marker - icon and caption together - in reference pixels at 1080p. This is the
        /// box the placement rules protect, and both marker kinds are laid out to fit inside it.
        ///
        /// <para><b>Why the caption box is not the footprint.</b> The first version of the panel rule
        /// protected only the caption, so the capture passed its assertion while the diamond still
        /// printed across <c>DAMAGE BLOCKED</c> on the weapon panel: the half of the marker a player
        /// actually reads was never tested. Protecting a box the marker does not fill is worse than
        /// protecting nothing, because the assertion then reports a clean frame.</para>
        ///
        /// <para><b>Why 36 and not 74.</b> Icon above caption made the objective 74 units tall and a
        /// ping 60. The preparation screen has exactly one gap inside its readable band - y 70..110,
        /// forty units - because the weapon-select panel covers y -190..70 and the preparation plate
        /// covers y 110..190. A 74-unit marker can never fit a 40-unit gap, so no placement rule can
        /// rescue it. Laying icon and caption out on one row makes the marker 36 units tall, which
        /// fits, and leaves the panel geometry untouched rather than special-casing a screen.</para>
        ///
        /// <para><b>Rotated icons count their diagonal.</b> The capture assertion measures the marker's
        /// real drawn bounds, and a diamond rotated 45 degrees has an axis-aligned box of
        /// <c>size x sqrt(2)</c>. A 34-unit diamond measured 48.09 tall against a declared 36, which
        /// put the marker straight back on the panel. Icon sizes are chosen against that diagonal,
        /// not against the authoring size.</para>
        /// </summary>
        public static readonly Vector2 MarkerFootprintSize = new Vector2(240f, 36f);

        /// <summary>Where the icon sits inside <see cref="MarkerFootprintSize"/>, left of the caption.</summary>
        public const float MarkerIconCenterX = -100f;

        /// <summary>
        /// Largest icon edge length whose 45-degree-rotated box still fits
        /// <see cref="MarkerFootprintSize"/>: <c>MarkerFootprintSize.y / sqrt(2)</c> is about 25.5,
        /// and both markers use 24 or less.
        /// </summary>
        public const float MarkerIconMaxEdge = 25f;

        /// <summary>
        /// Where the caption sits inside <see cref="MarkerFootprintSize"/>. Both the caption box and
        /// its plate use this, and it is offset from the anchor rather than centred on it, so the
        /// protected footprint stays centred on the point gameplay hands the rules.
        /// </summary>
        public const float MarkerCaptionCenterX = 20f;

        /// <summary>
        /// Vertical pitch between two marker captions, in reference pixels at 1080p: one caption
        /// height plus a gap. Two captions closer than this on the vertical axis read as one
        /// unreadable smear, so this is the distance they are held apart by.
        /// </summary>
        public const float MarkerRowHeight = 44f;

        /// <summary>
        /// Deliberate clearance a marker must keep from every panel and every other marker, in
        /// canvas units. Placement is not "fits or does not fit": it must fit with room to spare.
        ///
        /// <para><b>Where the margin belongs, and why this number.</b> The tightest case is measured,
        /// not assumed. On the 1920x1080 capture the readable band runs y -172.8..187.2;
        /// <c>WeaponSelect</c> covers -190..70 and <c>Preparation</c> covers 110..190, so the only free
        /// slot is y 70..110 - forty units - and the marker's real drawn footprint is 33.94 tall
        /// (the diamond's 45-degree rotation makes 24 x sqrt(2)). That leaves 6.06 units of slack in
        /// total, so a symmetric requirement of C needs <c>33.94 + 2C <= 40</c>, i.e. C <= 3.03.
        ///
        /// <para><b>Where the margin belongs: here, in the rule, not in the footprint or the band.</b>
        /// A bigger footprint or a wider band would each buy clearance by taking room away from
        /// something else - the marker's own glyphs, or the space a marker has to be placed in.
        /// The margin is a separate requirement the placement rule enforces, so it costs layout
        /// nothing and applies to every panel and every other marker through one predicate.</para>
        ///
        /// <para><b>Why one and a half.</b> The binding constraint is the declared footprint,
        /// not the drawn ink: a slot must hold <c>MarkerFootprintSize.y + 2C</c>. The only
        /// free slot is forty units tall and the footprint is thirty-six, so the arithmetic
        /// bound is C <= 2 - but that bound is exactly what must not be used. Two needs
        /// 37.94 of 40 and leaves 2.06, and it was tried: on the live capture frame the slot
        /// measures 39.9993, not 40, because panel rects are laid out in floats and round.
        /// 36 + 2*2 then exceeds the slot by four thousandths of a unit, no candidate reaches
        /// the margin, the rule falls back to the original placement, and the marker is left
        /// <i>on the weapon panel</i>. A requirement that consumes its own slot is the same
        /// knife-edge wearing a different hat, and it fails silently by disabling the rule
        /// rather than by refusing.</para>
        ///
        /// <para>One and a half needs 36.94 of the slot and leaves about three units of
        /// slack, which float and layout rounding cannot consume. It is still comfortably
        /// above the 1.03 units (1.5 px at 1080p) the first-fit sweep left, so it rejects the
        /// placement that started this - and because the slot is only 40 units tall, a marker
        /// that clears the requirement in the middle of it ends up about two units from each
        /// panel, twice what the old first-fit result gave.</para>
        ///
        /// <para><see cref="AvoidPanels"/> picks the placement with the <i>largest</i> clearance and
        /// accepts it only once it reaches this, which in the 40-unit slot means 3.03 units on each
        /// side instead of 1.03 on one. Nothing here knows what a weapon panel is.</para>
        /// </summary>
        public const float MarkerClearance = 1.5f;

        /// <summary>
        /// Nudges a marker clear of captions already placed this frame.
        ///
        /// <para><b>Why this exists.</b> The bands stop a marker landing on HUD chrome, but they
        /// say nothing about two markers landing on each other. In the 1920x1080 capture the
        /// objective (34 m out) and an enemy ping (52 m out) projected about twenty pixels apart,
        /// so <c>ENEMY HERE 52m</c> and <c>CENTER 34m</c> were printed through one another and
        /// neither was readable. Coincidence of world positions is not something gameplay can
        /// prevent, so placement has to absorb it.</para>
        ///
        /// <para>Pure and ordered: <paramref name="taken"/> is the placements made earlier in this
        /// frame. It sweeps row by row and takes the first row that clears both the other markers
        /// and the panels by <see cref="MarkerClearance"/>, rather than nudging one step and
        /// re-testing - a step-at-a-time retry oscillates between two occupied rows and never
        /// escapes, which a cluster test caught. If the band has no free row it keeps the original
        /// placement, because a caption pushed into the score bar to cure a smear trades a
        /// legibility bug for a worse one.</para>
        /// </summary>
        public static Vector2 SeparateFromPlaced(Vector2 candidate, IReadOnlyList<Vector2> taken,
            Rect canvasRect, float rowHeight = MarkerRowHeight,
            IReadOnlyList<Rect> panels = null)
        {
            if (rowHeight <= 0f) return candidate;
            bool hasTaken = taken != null && taken.Count > 0;
            bool hasPanels = panels != null && panels.Count > 0;
            if (!hasTaken && !hasPanels) return candidate;
            if (!Blocked(candidate, taken, panels)) return candidate;

            float floorY = -canvasRect.height * 0.5f + canvasRect.height * BottomReservedFraction;
            float ceilingY = canvasRect.height * 0.5f - canvasRect.height * TopReservedFraction;

            for (float y = candidate.y; y + rowHeight <= ceilingY; y += rowHeight)
            {
                float row = y + rowHeight;
                if (!Blocked(new Vector2(candidate.x, row), taken, panels)) return new Vector2(candidate.x, row);
            }
            for (float y = candidate.y; y - rowHeight >= floorY; y -= rowHeight)
            {
                float row = y - rowHeight;
                if (!Blocked(new Vector2(candidate.x, row), taken, panels)) return new Vector2(candidate.x, row);
            }
            return candidate;
        }

        /// <summary>
        /// True when the marker at <paramref name="anchor"/> is blocked either by another marker
        /// already placed or by a panel on screen, allowing for <see cref="MarkerClearance"/>.
        ///
        /// <para>Both reasons are tested together in one sweep on purpose. The two rules used to run
        /// as separate passes over the same point: the first lifted the objective caption off the
        /// weapon panel and the second, knowing only about other markers, immediately put it back
        /// down onto it. A row is free only if it is clear of both, so the sweep has to ask about
        /// both or one rule undoes the other.</para>
        /// </summary>
        public static bool Blocked(Vector2 anchor, IReadOnlyList<Vector2> taken, IReadOnlyList<Rect> panels)
        {
            if (taken != null && OverlapsAny(anchor, taken)) return true;
            return panels != null && panels.Count > 0 && OverlapsPanel(anchor, panels);
        }

        /// <summary>Canvas-local box the whole marker occupies when its anchor sits at <paramref name="anchor"/>.</summary>
        public static Rect FootprintBox(Vector2 anchor)
        {
            return new Rect(anchor.x - MarkerFootprintSize.x * 0.5f, anchor.y - MarkerFootprintSize.y * 0.5f,
                MarkerFootprintSize.x, MarkerFootprintSize.y);
        }

        /// <summary>The footprint grown by <see cref="MarkerClearance"/> on every side.</summary>
        static Rect ClearBox(Vector2 anchor)
        {
            var box = FootprintBox(anchor);
            box.xMin -= MarkerClearance; box.yMin -= MarkerClearance;
            box.xMax += MarkerClearance; box.yMax += MarkerClearance;
            return box;
        }

        /// <summary>
        /// Distance from this marker's footprint to the nearest panel or other marker, 0 when they
        /// touch or overlap, and <see cref="float.MaxValue"/> when nothing is near. Used both to
        /// reject a placement and to pick the least bad one.
        /// </summary>
        public static float ClearanceAt(Vector2 anchor, IReadOnlyList<Rect> panels,
            IReadOnlyList<Vector2> taken = null)
        {
            var box = FootprintBox(anchor);
            float nearest = float.MaxValue;
            if (panels != null)
                for (int i = 0; i < panels.Count; i++)
                {
                    float gap = RectGap(box, panels[i]);
                    if (gap < nearest) nearest = gap;
                }
            if (taken != null)
                for (int i = 0; i < taken.Count; i++)
                {
                    float gap = RectGap(box, FootprintBox(taken[i]));
                    if (gap < nearest) nearest = gap;
                }
            return nearest;
        }

        /// <summary>
        /// Shortest distance between two axis-aligned boxes; 0 when they touch or overlap.
        /// Public because the capture assertion measures the same quantity with the same
        /// definition - a second, looser gap formula in the test could disagree with the rule
        /// and pass a frame the rule would have rejected.
        /// </summary>
        public static float RectGap(Rect a, Rect b)
        {
            float dx = Mathf.Max(Mathf.Max(b.xMin - a.xMax, a.xMin - b.xMax), 0f);
            float dy = Mathf.Max(Mathf.Max(b.yMin - a.yMax, a.yMin - b.yMax), 0f);
            if (dx <= 0f && dy <= 0f) return 0f;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Keeps a marker's whole footprint off every HUD panel on screen, with deliberate clearance.
        ///
        /// <para><b>Why this exists.</b> The reserved bands are two fixed fractions of the canvas
        /// height, measured from the gameplay HUD. They describe the score bar and the touch
        /// controls, but the set of panels on screen changes with the match phase, and nothing
        /// measured the ones that are not there during a normal fight. Capturing screen 07 for the
        /// first time showed the consequence: during preparation the weapon-select panel occupies
        /// the lower middle, inside the "readable" band, and the objective caption printed straight
        /// across its weapon names. Same defect class as the score-bar collision, on a panel the
        /// fixed bands never accounted for.
        ///
        /// <para>General, not a preparation special case: <paramref name="panels"/> is whatever the
        /// HUD actually has on screen, measured from real rects by the caller, so any panel that
        /// appears in any phase is honoured without this rule knowing it exists.</para>
        ///
        /// <para><b>Largest clearance wins, not first fit.</b> The sweep used to return the first
        /// row that did not collide. That is "fits or does not fit", and it spent the whole 6.06
        /// units of slack in the 40-unit slot on one side, leaving the marker 1.03 units - 1.5 px -
        /// from the weapon panel. Technically clear, and the first thing a small layout change would
        /// undo. Every candidate is now scored by <see cref="ClearanceAt"/> and the roomiest wins,
        /// which puts the marker in the middle of the gap with 3.03 units on each side.</para>
        ///
        /// <para>Candidates are the swept rows plus the centre of each free vertical gap. A result
        /// is accepted only once it reaches <see cref="MarkerClearance"/>; otherwise the original
        /// placement is kept, because a caption pushed into the score bar to cure a collision trades
        /// a legibility bug for a worse one.</para>
        /// </summary>
        public static Vector2 AvoidPanels(Vector2 candidate, IReadOnlyList<Rect> panels,
            Rect canvasRect, float rowHeight = MarkerRowHeight,
            IReadOnlyList<Vector2> taken = null)
        {
            if (panels == null || panels.Count == 0 || rowHeight <= 0f) return candidate;
            float current = ClearanceAt(candidate, panels, taken);
            if (current >= MarkerClearance) return candidate;

            float floorY = -canvasRect.height * 0.5f + canvasRect.height * BottomReservedFraction;
            float ceilingY = canvasRect.height * 0.5f - canvasRect.height * TopReservedFraction;

            Vector2 best = candidate;
            float bestClearance = current;
            foreach (float y in CandidateYs(candidate, floorY, ceilingY, rowHeight, panels))
            {
                var probe = new Vector2(candidate.x, y);
                float clearance = ClearanceAt(probe, panels, taken);
                if (clearance <= bestClearance) continue;
                bestClearance = clearance;
                best = probe;
            }
            return bestClearance >= MarkerClearance ? best : candidate;
        }

        /// <summary>
        /// Every vertical position worth trying: the swept rows, then the centre of each free gap.
        /// Both are needed - the rows keep the marker near where gameplay put it, and the gap
        /// centres catch the slots the 44-unit pitch steps over.
        /// </summary>
        static List<float> CandidateYs(Vector2 preferred, float floorY, float ceilingY, float rowHeight,
            IReadOnlyList<Rect> panels)
        {
            var ys = new List<float>();
            for (float y = preferred.y + rowHeight; y + rowHeight <= ceilingY; y += rowHeight)
                ys.Add(y + rowHeight);
            for (float y = preferred.y - rowHeight; y - rowHeight >= floorY; y -= rowHeight)
                ys.Add(y - rowHeight);

            // Free vertical spans of the readable band, after subtracting every panel the
            // footprint's horizontal extent actually crosses.
            float half = MarkerFootprintSize.y * 0.5f + MarkerClearance;
            float minX = preferred.x - MarkerFootprintSize.x * 0.5f;
            float maxX = preferred.x + MarkerFootprintSize.x * 0.5f;
            // Sorted low to high first. The scan below keeps a running cursor and only ever
            // raises it, which is correct for intervals arriving in ascending order and silently
            // wrong otherwise: a panel listed later but sitting lower is skipped as "already
            // covered" and the gap it actually occupies never becomes a candidate. Callers
            // publish panels in whatever order their fields happen to be in - the HUD lists
            // weapon, preparation, countdown, overtime, teammates, then the floating panels -
            // so that order is not sorted, and treating it as if it were lost the one gap the
            // preparation screen has.
            var crossing = new List<(float lo, float hi)>();
            for (int i = 0; i < panels.Count; i++)
            {
                var panel = panels[i];
                if (panel.xMax <= minX || panel.xMin >= maxX) continue;   // footprint does not cross it
                crossing.Add((Mathf.Max(panel.yMin, floorY), Mathf.Min(panel.yMax, ceilingY)));
            }
            crossing.Sort((a, b) => a.lo.CompareTo(b.lo));

            var spans = new List<(float lo, float hi)>();
            float cursor = floorY;
            for (int i = 0; i < crossing.Count; i++)
            {
                float lo = crossing[i].lo;
                float hi = crossing[i].hi;
                if (hi <= cursor) continue;
                if (lo > cursor) spans.Add((cursor, lo));
                cursor = Mathf.Max(cursor, hi);
            }
            if (cursor < ceilingY) spans.Add((cursor, ceilingY));

            foreach (var span in spans)
            {
                // The span must hold the footprint plus the clearance on both sides.
                if (span.hi - span.lo < half * 2f) continue;
                // The middle of the span, not its nearest edge. Clamping the preferred row into
                // [lo + half, hi - half] snaps it to whichever edge is closer - and on the
                // preparation screen the slot is 40.0008 units against a 36-unit footprint, so
                // every legal position is already within half a unit of the margin and the
                // clamp landed on it exactly, giving 1.4996 units instead of the 2.0004 the
                // slot actually has. A rule that guarantees the minimum clearance is still
                // guaranteeing a knife edge; the roomiest placement is the one in the middle.
                float low = span.lo + half;
                float high = span.hi - half;
                ys.Add(preferred.y >= low && preferred.y <= high
                    ? preferred.y
                    : (low + high) * 0.5f);
            }
            return ys;
        }

        /// <summary>
        /// True when this marker would land on a panel the player is reading, allowing for
        /// <see cref="MarkerClearance"/> on every side.
        /// </summary>
        public static bool OverlapsPanel(Vector2 point, IReadOnlyList<Rect> panels)
        {
            if (panels == null) return false;
            var box = ClearBox(point);
            for (int i = 0; i < panels.Count; i++) if (panels[i].Overlaps(box)) return true;
            return false;
        }

        /// <summary>True when this marker would land on an already-placed one, allowing for clearance.</summary>
        static bool OverlapsAny(Vector2 point, IReadOnlyList<Vector2> taken)
        {
            var box = ClearBox(point);
            for (int i = 0; i < taken.Count; i++)
                if (box.Overlaps(ClearBox(taken[i]))) return true;
            return false;
        }

        /// <summary>
        /// Projects a world point into canvas-local units. Returns false when the point is behind
        /// the camera, in which case <paramref name="local"/> is meaningless and the caller should
        /// fall back to an edge marker rather than drawing a marker at a mirrored guess.
        /// </summary>
        public static bool ProjectToCanvas(UnityEngine.Camera camera, RectTransform canvas, Vector3 world, out Vector2 local)
        {
            local = Vector2.zero;
            if (camera == null || canvas == null) return false;
            Vector3 screen = camera.WorldToScreenPoint(world);
            if (screen.z < 0f) return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out local);
            return true;
        }

        /// <summary>
        /// Clamps a canvas-local marker into the band between the HUD's reserved regions.
        /// Pure; this is the rule both pings and the objective marker obey.
        /// </summary>
        public static Vector2 ClampToReadableBand(Vector2 canvasLocal, Rect canvasRect)
        {
            float halfHeight = canvasRect.height * 0.5f;
            float halfWidth = canvasRect.width * 0.5f;
            return new Vector2(
                Mathf.Clamp(canvasLocal.x, -halfWidth + 8f, halfWidth - 8f),
                Mathf.Clamp(canvasLocal.y,
                    -halfHeight + canvasRect.height * BottomReservedFraction,
                    halfHeight - canvasRect.height * TopReservedFraction));
        }

        /// <summary>
        /// Pushes a marker to the nearest edge of <paramref name="safe"/>, then applies the same
        /// reserved bands so an edge marker cannot land on the score bar either.
        ///
        /// <para>Written against whatever rect it is handed, so the caller decides the space and
        /// that space has one owner. The behind-camera objective marker passes the safe content
        /// rect from <see cref="SafeAreaRoot.ContentRectFor"/>; screen-space callers pass a screen
        /// rect. It used to be screen-only, which is what let the edge marker carry its own copy
        /// of the screen geometry and ignore the edge margin and camera cutout the rest of the HUD
        /// is laid out from.</para>
        /// </summary>
        public static Vector2 ClampToEdgeBand(Vector2 screenPoint, Rect safe, float padding)
        {
            Vector2 centre = safe.center;
            Vector2 direction = screenPoint - centre;
            if (direction.sqrMagnitude < 1f) direction = Vector2.down;

            float halfW = Mathf.Max(0.001f, Mathf.Abs(direction.x));
            float halfH = Mathf.Max(0.001f, Mathf.Abs(direction.y));
            float sx = (safe.width * 0.5f - padding) / halfW;
            float sy = (safe.height * 0.5f - padding) / halfH;
            Vector2 edge = centre + direction * Mathf.Min(sx, sy);
            edge.x = Mathf.Clamp(edge.x, safe.xMin + padding, safe.xMax - padding);
            edge.y = ClampToBands(edge.y, safe);
            return edge;
        }

        /// <summary>Applies the reserved bands to a Y value in the same space as <paramref name="safe"/>.</summary>
        public static float ClampToBands(float screenY, Rect safe) =>
            Mathf.Clamp(screenY, safe.yMin + safe.height * BottomReservedFraction, safe.yMax - safe.height * TopReservedFraction);
    }
}