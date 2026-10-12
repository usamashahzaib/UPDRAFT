using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hotdrop.Config
{
    public enum SwitchyardSpawnBoxRole
    {
        Floor,
        TeamStripe,
        Wall,
        FrontPier,
        ExitBarrier,
        ExitHeader,
        Roof,
    }

    public struct SwitchyardSpawnBoxGeometry
    {
        public readonly string Name;
        public readonly SwitchyardSpawnBoxRole Role;
        public readonly Vector3 LocalPosition;
        public readonly Vector3 Size;
        public readonly bool HasCollider;

        public SwitchyardSpawnBoxGeometry(string name, SwitchyardSpawnBoxRole role,
            Vector3 localPosition, Vector3 size, bool hasCollider)
        {
            Name = name;
            Role = role;
            LocalPosition = localPosition;
            Size = size;
            HasCollider = hasCollider;
        }
    }

    // Spec section 10: Switchyard graybox coordinates. One Unity unit = one meter.
    [CreateAssetMenu(fileName = "MapConfig", menuName = "Hotdrop/Config/Map Config")]
    public class MapConfig : ScriptableObject
    {
        public string mapName = "Switchyard";

        [Header("Footprint")]
        public Vector2 boundsMin = new Vector2(-48f, -40f); // X, Z
        public Vector2 boundsMax = new Vector2(48f, 40f);
        public float groundLevelY = 0f;

        [Header("Objectives")]
        public Vector3 centerZone = new Vector3(0f, 0f, 0f);
        public Vector3 westZone = new Vector3(-22f, 0f, 0f);
        public Vector3 eastZone = new Vector3(22f, 0f, 0f);
        public float zoneRadius = 6f;

        public const int SwitchyardOuterBreakersPerZone = 4;
        public const float SwitchyardDeckThicknessMeters = 0.5f;
        public const float SwitchyardDeckTopToCenterOffsetMeters = 0.25f;
        public const float OuterLaneBreakerHeightMeters = 2f;
        public const float OuterLaneBreakerWidthMeters = 3f;
        public const float OuterLaneBreakerDepthMeters = 1f;
        public const float OuterLaneBreakerOffsetBeyondZoneMeters = 3f;
        public const float SwitchyardZoneRingLineWidthMeters = 0.14f;
        public const float SwitchyardZoneCenterMarkerDiameterMeters = 1f;

        /// <summary>
        /// Reproduces the generated Switchyard breaker transform and its exact world-axis bounds.
        /// Generation and config validation use this single geometry definition.
        /// </summary>
        public bool TryGetSwitchyardOuterLaneBreakerGeometry(Vector3 zoneCenter, int breakerIndex,
            out Vector3 position, out Quaternion rotation, out Bounds bounds)
        {
            position = default;
            rotation = Quaternion.identity;
            bounds = default;
            if (breakerIndex < 0 || breakerIndex >= SwitchyardOuterBreakersPerZone ||
                !IsFinite(zoneCenter) || !IsPositiveFinite(zoneRadius)) return false;

            float angle = 360f / SwitchyardOuterBreakersPerZone * breakerIndex;
            float offsetRadius = zoneRadius + OuterLaneBreakerOffsetBeyondZoneMeters;
            if (!IsFinite(offsetRadius)) return false;

            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * offsetRadius;
            rotation = Quaternion.Euler(0f, angle + 90f, 0f);
            position = zoneCenter + offset + Vector3.up * (OuterLaneBreakerHeightMeters * 0.5f);

            Vector3 halfSize = new Vector3(OuterLaneBreakerWidthMeters, OuterLaneBreakerHeightMeters,
                OuterLaneBreakerDepthMeters) * 0.5f;
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            Vector3 forward = rotation * Vector3.forward;
            Vector3 halfBounds = new Vector3(
                Mathf.Abs(right.x) * halfSize.x + Mathf.Abs(up.x) * halfSize.y + Mathf.Abs(forward.x) * halfSize.z,
                Mathf.Abs(right.y) * halfSize.x + Mathf.Abs(up.y) * halfSize.y + Mathf.Abs(forward.y) * halfSize.z,
                Mathf.Abs(right.z) * halfSize.x + Mathf.Abs(up.z) * halfSize.y + Mathf.Abs(forward.z) * halfSize.z);
            if (!IsFinite(position) || !IsFinite(halfBounds)) return false;

            bounds = new Bounds(position, halfBounds * 2f);
            return IsFinite(bounds.min) && IsFinite(bounds.max);
        }

        [Header("Spawns")]
        public Vector3 northSpawnCenter = new Vector3(0f, 0f, 35f);
        public Vector2 spawnRoomSize = new Vector2(20f, 8f);
        public Vector3 southSpawnCenter = new Vector3(0f, 0f, -35f);
        public int exitsPerSpawn = 3;

        [Header("Raised Platforms")]
        public float platformHeight = 3f;
        public Vector2 platformSize = new Vector2(10f, 8f);
        public Vector3[] platformCenters = new[]
        {
            // Keep the original four indices stable; SceneSetup uses index 1 in training.
            new Vector3(20f, 3f, 14f),
            new Vector3(-20f, 3f, 14f),
            new Vector3(20f, 3f, -14f),
            new Vector3(-20f, 3f, -14f),
            // Four additional flank roofs on each half produce six raised roof plates per half.
            new Vector3(34f, 3f, 26f),
            new Vector3(-34f, 3f, 26f),
            new Vector3(36f, 3f, 12f),
            new Vector3(-36f, 3f, 12f),
            new Vector3(34f, 3f, -26f),
            new Vector3(-34f, 3f, -26f),
            new Vector3(36f, 3f, -12f),
            new Vector3(-36f, 3f, -12f),
        };

        [Tooltip("Horizontal run of each generated inward ramp, from the ground end to the deck edge.")]
        public float rampRunMeters = 7f;
        [Tooltip("Width of each generated inward ramp.")]
        public float rampWidthMeters = 4f;

        [Header("Routes")]
        public float minimumMainRouteWidth = 4f;

        [Header("Sightline Blockers")]
        public float sightlineBlockerZ = 20f; // placed at Z = +/-20 between each spawn and Center

        public const float SightlineBlockerWidthMeters = 18f;
        public const float SightlineBlockerHeightMeters = 5f;
        public const float SightlineBlockerDepthMeters = 2f;

        public const string SwitchyardName = "Switchyard";
        // Design Bible 4.1: 6-10 connected roof plates per half.
        const int MinRaisedPlatformsPerSide = 6;
        const int MaxRaisedPlatformsPerSide = 10;
        const float GeometryTolerance = 1e-3f;
        /// <summary>Thickness of each generated ramp slab; SwitchyardGenerator.CreateRamp uses the same value.</summary>
        public const float RampThicknessMeters = 0.15f;

        /// <summary>
        /// Fail-closed check of the Switchyard roof layout; other map names are not constrained.
        /// Returns null when valid, otherwise the first problem found. Ramp footprints mirror
        /// SwitchyardGenerator.GenerateRamps: one X ramp and one Z ramp per deck, both inward.
        /// Ramp slab thickness is included in footprint bounds; the walkable ramp surfaces must
        /// retain at least the configured player capsule height below any other deck.
        /// </summary>
        public string ValidateSwitchyardRoofGeometry(MovementConfig movementConfig)
        {
            if (!string.Equals(mapName, SwitchyardName, StringComparison.Ordinal)) return null;
            string boundsError = ValidateBounds();
            if (boundsError != null) return boundsError;
            if (movementConfig == null) return "MovementConfig is required for ramp-to-deck player-clearance validation.";
            float minimumPlayerHeightMeters = movementConfig.capsuleHeight;
            float playerRadiusMeters = movementConfig.capsuleRadius;
            if (platformCenters == null) return "platformCenters is missing.";
            if (!IsPositiveFinite(platformSize.x) || !IsPositiveFinite(platformSize.y)) return "platformSize must be positive and finite.";
            if (!IsPositiveFinite(platformHeight)) return "platformHeight must be positive and finite.";
            if (!IsPositiveFinite(rampRunMeters)) return "rampRunMeters must be positive and finite.";
            if (!IsPositiveFinite(rampWidthMeters)) return "rampWidthMeters must be positive and finite.";
            if (!IsPositiveFinite(minimumPlayerHeightMeters)) return "MovementConfig.capsuleHeight must be positive and finite.";
            if (!IsPositiveFinite(playerRadiusMeters)) return "MovementConfig.capsuleRadius must be positive and finite.";
            if (!IsFinite(groundLevelY)) return "groundLevelY must be finite.";

            int north = 0, south = 0;
            float deckTopY = groundLevelY + platformHeight;
            for (int i = 0; i < platformCenters.Length; i++)
            {
                Vector3 c = platformCenters[i];
                if (!IsFinite(c.x) || !IsFinite(c.y) || !IsFinite(c.z)) return $"platformCenters[{i}] must be finite.";
                if (Mathf.Abs(c.y - deckTopY) > GeometryTolerance)
                    return $"platformCenters[{i}].y {c.y} must equal groundLevelY + platformHeight ({deckTopY}).";
                if (Mathf.Abs(c.x) <= GeometryTolerance || Mathf.Abs(c.z) <= GeometryTolerance)
                    return $"platformCenters[{i}] must not lie on x=0 or z=0; the generator ramp direction is undefined there.";
                if (c.z > 0f) north++; else south++;
                if (!InsideBounds(DeckFootprint(c))) return $"platformCenters[{i}] deck footprint leaves the map bounds.";
                if (!RampFootprints(c, out Rect xRamp, out Rect zRamp))
                    return $"platformCenters[{i}] ramp geometry is not finite or has a zero length.";
                if (!InsideBounds(xRamp)) return $"platformCenters[{i}] X ramp footprint leaves the map bounds.";
                if (!InsideBounds(zRamp)) return $"platformCenters[{i}] Z ramp footprint leaves the map bounds.";

                if (!TryGetSwitchyardRampGeometry(c, alongX: true, out Vector3 xGround, out Vector3 xEdge,
                        out _, out _, out _, out _))
                    return $"platformCenters[{i}] X ramp geometry is not finite or has a zero length.";
                string xClearanceError = ValidateRampHeadroom(i, xGround, xEdge, minimumPlayerHeightMeters, playerRadiusMeters);
                if (xClearanceError != null) return xClearanceError;
                if (!TryGetSwitchyardRampGeometry(c, alongX: false, out Vector3 zGround, out Vector3 zEdge,
                        out _, out _, out _, out _))
                    return $"platformCenters[{i}] Z ramp geometry is not finite or has a zero length.";
                string zClearanceError = ValidateRampHeadroom(i, zGround, zEdge, minimumPlayerHeightMeters, playerRadiusMeters);
                if (zClearanceError != null) return zClearanceError;
            }
            if (north < MinRaisedPlatformsPerSide || north > MaxRaisedPlatformsPerSide)
                return $"{north} raised platforms at positive z; {MinRaisedPlatformsPerSide}-{MaxRaisedPlatformsPerSide} required.";
            if (south < MinRaisedPlatformsPerSide || south > MaxRaisedPlatformsPerSide)
                return $"{south} raised platforms at negative z; {MinRaisedPlatformsPerSide}-{MaxRaisedPlatformsPerSide} required.";

            for (int i = 0; i < platformCenters.Length; i++)
            {
                Rect a = DeckFootprint(platformCenters[i]);
                for (int j = i + 1; j < platformCenters.Length; j++)
                {
                    Rect b = DeckFootprint(platformCenters[j]);
                    float overlapX = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
                    float overlapZ = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                    if (overlapX > GeometryTolerance && overlapZ > GeometryTolerance)
                        return $"platformCenters[{i}] and [{j}] have overlapping or duplicate deck footprints.";
                }
            }

            int rampCount = platformCenters.Length * 2;
            for (int rampA = 0; rampA < rampCount; rampA++)
            {
                int platformA = rampA / 2;
                bool alongXA = rampA % 2 == 0;
                if (!TryGetSwitchyardRampGeometry(platformCenters[platformA], alongXA,
                        out _, out _, out Vector3 positionA, out Quaternion rotationA, out Vector3 sizeA, out _))
                    return $"platformCenters[{platformA}] {(alongXA ? "X" : "Z")} ramp geometry is not finite.";
                for (int rampB = rampA + 1; rampB < rampCount; rampB++)
                {
                    int platformB = rampB / 2;
                    bool alongXB = rampB % 2 == 0;
                    if (!TryGetSwitchyardRampGeometry(platformCenters[platformB], alongXB,
                            out _, out _, out Vector3 positionB, out Quaternion rotationB, out Vector3 sizeB, out _))
                        return $"platformCenters[{platformB}] {(alongXB ? "X" : "Z")} ramp geometry is not finite.";
                    if (OrientedBoxesOverlap(positionA, rotationA, sizeA, positionB, rotationB, sizeB))
                        return $"platformCenters[{platformA}] {(alongXA ? "X" : "Z")} ramp intersects platformCenters[{platformB}] {(alongXB ? "X" : "Z")} ramp.";
                }
            }

            for (int i = 0; i < platformCenters.Length; i++)
            {
                Vector3 c = platformCenters[i];
                if (!HasPlatformAt(new Vector3(-c.x, c.y, c.z))) return $"platformCenters[{i}] has no X mirror.";
                if (!HasPlatformAt(new Vector3(c.x, c.y, -c.z))) return $"platformCenters[{i}] has no Z mirror.";
                if (!HasPlatformAt(new Vector3(-c.x, c.y, -c.z))) return $"platformCenters[{i}] has no X+Z mirror.";
            }
            return null;
        }

        public bool TryGetSwitchyardDeckGeometry(Vector3 topCenter, out Vector3 position, out Vector3 size, out Bounds bounds)
        {
            position = topCenter - Vector3.up * SwitchyardDeckTopToCenterOffsetMeters;
            size = new Vector3(platformSize.x, SwitchyardDeckThicknessMeters, platformSize.y);
            bounds = new Bounds(position, size);
            return IsFinite(position) && IsFinite(size) && IsFinite(bounds.min) && IsFinite(bounds.max);
        }

        Rect DeckFootprint(Vector3 c)
        {
            if (!TryGetSwitchyardDeckGeometry(c, out _, out _, out Bounds bounds))
                return new Rect(float.NaN, float.NaN, float.NaN, float.NaN);
            return Rect.MinMaxRect(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
        }

        // Same endpoints as SwitchyardGenerator.GenerateRamps (inward = -Mathf.Sign of the centre).
        bool RampFootprints(Vector3 c, out Rect xRamp, out Rect zRamp)
        {
            bool xValid = TryGetSwitchyardRampGeometry(c, alongX: true, out _, out _, out _, out _, out _, out Bounds xBounds);
            bool zValid = TryGetSwitchyardRampGeometry(c, alongX: false, out _, out _, out _, out _, out _, out Bounds zBounds);
            xRamp = xValid ? HorizontalFootprint(xBounds) : new Rect(float.NaN, float.NaN, float.NaN, float.NaN);
            zRamp = zValid ? HorizontalFootprint(zBounds) : new Rect(float.NaN, float.NaN, float.NaN, float.NaN);
            return xValid && zValid;
        }

        static Rect HorizontalFootprint(Bounds bounds) =>
            Rect.MinMaxRect(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);

        static bool OrientedBoxesOverlap(Vector3 positionA, Quaternion rotationA, Vector3 sizeA,
            Vector3 positionB, Quaternion rotationB, Vector3 sizeB)
        {
            Vector3 a0 = rotationA * Vector3.right, a1 = rotationA * Vector3.up, a2 = rotationA * Vector3.forward;
            Vector3 b0 = rotationB * Vector3.right, b1 = rotationB * Vector3.up, b2 = rotationB * Vector3.forward;
            Vector3 delta = positionB - positionA;
            Vector3 halfA = sizeA * 0.5f, halfB = sizeB * 0.5f;

            if (SeparatedOnAxis(a0, delta, a0, a1, a2, halfA, b0, b1, b2, halfB) ||
                SeparatedOnAxis(a1, delta, a0, a1, a2, halfA, b0, b1, b2, halfB) ||
                SeparatedOnAxis(a2, delta, a0, a1, a2, halfA, b0, b1, b2, halfB) ||
                SeparatedOnAxis(b0, delta, a0, a1, a2, halfA, b0, b1, b2, halfB) ||
                SeparatedOnAxis(b1, delta, a0, a1, a2, halfA, b0, b1, b2, halfB) ||
                SeparatedOnAxis(b2, delta, a0, a1, a2, halfA, b0, b1, b2, halfB)) return false;

            return !SeparatedOnAxis(Vector3.Cross(a0, b0), delta, a0, a1, a2, halfA, b0, b1, b2, halfB) &&
                !SeparatedOnAxis(Vector3.Cross(a0, b1), delta, a0, a1, a2, halfA, b0, b1, b2, halfB) &&
                !SeparatedOnAxis(Vector3.Cross(a0, b2), delta, a0, a1, a2, halfA, b0, b1, b2, halfB) &&
                !SeparatedOnAxis(Vector3.Cross(a1, b0), delta, a0, a1, a2, halfA, b0, b1, b2, halfB) &&
                !SeparatedOnAxis(Vector3.Cross(a1, b1), delta, a0, a1, a2, halfA, b0, b1, b2, halfB) &&
                !SeparatedOnAxis(Vector3.Cross(a1, b2), delta, a0, a1, a2, halfA, b0, b1, b2, halfB) &&
                !SeparatedOnAxis(Vector3.Cross(a2, b0), delta, a0, a1, a2, halfA, b0, b1, b2, halfB) &&
                !SeparatedOnAxis(Vector3.Cross(a2, b1), delta, a0, a1, a2, halfA, b0, b1, b2, halfB) &&
                !SeparatedOnAxis(Vector3.Cross(a2, b2), delta, a0, a1, a2, halfA, b0, b1, b2, halfB);
        }

        static bool SeparatedOnAxis(Vector3 axis, Vector3 centerDelta,
            Vector3 a0, Vector3 a1, Vector3 a2, Vector3 halfA,
            Vector3 b0, Vector3 b1, Vector3 b2, Vector3 halfB)
        {
            float magnitudeSquared = axis.sqrMagnitude;
            if (!IsFinite(magnitudeSquared) || magnitudeSquared <= GeometryTolerance * GeometryTolerance) return false;
            axis /= Mathf.Sqrt(magnitudeSquared);
            float radiusA = halfA.x * Mathf.Abs(Vector3.Dot(axis, a0))
                + halfA.y * Mathf.Abs(Vector3.Dot(axis, a1))
                + halfA.z * Mathf.Abs(Vector3.Dot(axis, a2));
            float radiusB = halfB.x * Mathf.Abs(Vector3.Dot(axis, b0))
                + halfB.y * Mathf.Abs(Vector3.Dot(axis, b1))
                + halfB.z * Mathf.Abs(Vector3.Dot(axis, b2));
            float distance = Mathf.Abs(Vector3.Dot(centerDelta, axis));
            return distance >= radiusA + radiusB - GeometryTolerance;
        }

        void RampEndpoints(Vector3 c, bool alongX, out Vector3 ground, out Vector3 edge)
        {
            float towardCenter = -Mathf.Sign(alongX ? c.x : c.z);
            float platformHalf = (alongX ? platformSize.x : platformSize.y) * 0.5f;
            if (alongX)
            {
                ground = new Vector3(c.x + towardCenter * (platformHalf + rampRunMeters), groundLevelY, c.z);
                edge = c + new Vector3(towardCenter * platformHalf, 0f, 0f);
            }
            else
            {
                ground = new Vector3(c.x, groundLevelY, c.z + towardCenter * (platformHalf + rampRunMeters));
                edge = c + new Vector3(0f, 0f, towardCenter * platformHalf);
            }
        }

        public bool TryGetSwitchyardRampGeometry(Vector3 platformCenter, bool alongX,
            out Vector3 ground, out Vector3 edge, out Vector3 position, out Quaternion rotation,
            out Vector3 size, out Bounds bounds)
        {
            RampEndpoints(platformCenter, alongX, out ground, out edge);
            bounds = default;
            if (!TryRampTransform(ground, edge, out position, out rotation, out size)) return false;
            return TryRampBounds(ground, edge, out bounds);
        }

        string ValidateRampHeadroom(int rampPlatformIndex, Vector3 ground, Vector3 edge, float minimumPlayerHeightMeters, float playerRadiusMeters)
        {
            bool alongX = Mathf.Abs(edge.x - ground.x) > Mathf.Abs(edge.z - ground.z);
            Rect surface = RampSurfaceFootprint(ground, edge, alongX);
            for (int deckIndex = 0; deckIndex < platformCenters.Length; deckIndex++)
            {
                if (deckIndex == rampPlatformIndex) continue;
                Vector3 deck = platformCenters[deckIndex];
                if (!TryGetSwitchyardDeckGeometry(deck, out _, out _, out Bounds deckBounds))
                    return $"platformCenters[{deckIndex}] deck bounds are not finite.";
                Rect footprint = HorizontalFootprint(deckBounds);
                // Check centre positions whose capsule body can touch the deck underside,
                // including centres just outside the deck's horizontal footprint.
                footprint.xMin -= playerRadiusMeters;
                footprint.xMax += playerRadiusMeters;
                footprint.yMin -= playerRadiusMeters;
                footprint.yMax += playerRadiusMeters;
                float overlapMinX = Mathf.Max(surface.xMin, footprint.xMin);
                float overlapMaxX = Mathf.Min(surface.xMax, footprint.xMax);
                float overlapMinZ = Mathf.Max(surface.yMin, footprint.yMin);
                float overlapMaxZ = Mathf.Min(surface.yMax, footprint.yMax);
                if (overlapMaxX - overlapMinX <= GeometryTolerance || overlapMaxZ - overlapMinZ <= GeometryTolerance)
                    continue;

                float surfacePosition = alongX
                    ? (edge.x > ground.x ? overlapMaxX : overlapMinX)
                    : (edge.z > ground.z ? overlapMaxZ : overlapMinZ);
                float groundPosition = alongX ? ground.x : ground.z;
                float edgePosition = alongX ? edge.x : edge.z;
                float t = Mathf.Clamp01((surfacePosition - groundPosition) / (edgePosition - groundPosition));
                float rampTopY = Mathf.Lerp(ground.y, edge.y, t);
                float deckBottomY = deckBounds.min.y;
                float headroom = deckBottomY - rampTopY;
                if (!IsFinite(headroom) || headroom < minimumPlayerHeightMeters - GeometryTolerance)
                    return $"ramp for platformCenters[{rampPlatformIndex}] passes under platformCenters[{deckIndex}] with {headroom:0.###} m headroom; {minimumPlayerHeightMeters:0.###} m required for the configured player capsule.";
            }
            return null;
        }

        Rect RampSurfaceFootprint(Vector3 ground, Vector3 edge, bool alongX)
        {
            float halfWidth = rampWidthMeters * 0.5f;
            return alongX
                ? Rect.MinMaxRect(Mathf.Min(ground.x, edge.x), ground.z - halfWidth, Mathf.Max(ground.x, edge.x), ground.z + halfWidth)
                : Rect.MinMaxRect(ground.x - halfWidth, Mathf.Min(ground.z, edge.z), ground.x + halfWidth, Mathf.Max(ground.z, edge.z));
        }

        bool InsideBounds(Rect r) =>
            r.xMin >= boundsMin.x - GeometryTolerance && r.xMax <= boundsMax.x + GeometryTolerance &&
            r.yMin >= boundsMin.y - GeometryTolerance && r.yMax <= boundsMax.y + GeometryTolerance;

        bool HasPlatformAt(Vector3 p)
        {
            foreach (var c in platformCenters)
                if (Mathf.Abs(c.x - p.x) <= GeometryTolerance && Mathf.Abs(c.y - p.y) <= GeometryTolerance && Mathf.Abs(c.z - p.z) <= GeometryTolerance) return true;
            return false;
        }

        static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        static bool IsFinite(Vector3 v) => IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);
        static bool IsPositiveFinite(float v) => IsFinite(v) && v > 0f;
        /// <summary>Centre of the configured X/Z rectangle (boundsMin + half the extent, which cannot overflow when the extent is finite).</summary>
        public Vector2 BoundsCenter => boundsMin + (boundsMax - boundsMin) * 0.5f;

        /// <summary>
        /// Generic bounds check for every map name: finite endpoints, min below max on X and Z, and a finite
        /// width, depth and centre (finite endpoints can still overflow when subtracted).
        /// </summary>
        public string ValidateBounds()
        {
            if (!IsFinite(boundsMin.x) || !IsFinite(boundsMin.y) || !IsFinite(boundsMax.x) || !IsFinite(boundsMax.y))
                return "boundsMin/boundsMax must be finite.";
            if (!(boundsMin.x < boundsMax.x) || !(boundsMin.y < boundsMax.y))
                return "boundsMin must be below boundsMax on both X and Z.";
            float width = boundsMax.x - boundsMin.x, depth = boundsMax.y - boundsMin.y;
            Vector2 center = BoundsCenter;
            if (!IsFinite(width) || !IsFinite(depth) || !IsFinite(center.x) || !IsFinite(center.y))
                return "map bounds width, depth and centre must be finite.";
            return null;
        }

        /// <summary>
        /// Fail-closed check that each objective marker circle (centre x/z +/- zoneRadius, the ring and trigger
        /// SwitchyardGenerator.CreateZoneMarker builds) stays inside the map bounds. This applies across map
        /// names; Switchyard additionally requires each objective centre Y to equal groundLevelY.
        /// Switchyard's generated outer-lane breaker boxes are also bounded using their generated footprints.
        /// For Switchyard the visual ring and centre-marker extents are included; breaker overlap with other
        /// gameplay geometry is not included.
        /// </summary>
        public string ValidateObjectiveZones()
        {
            string boundsError = ValidateBounds();
            if (boundsError != null) return boundsError;
            if (!IsPositiveFinite(zoneRadius)) return "zoneRadius must be positive and finite.";
            for (int zone = 0; zone < 3; zone++)
            {
                string name = zone == 0 ? "centerZone" : zone == 1 ? "westZone" : "eastZone";
                Vector3 c = zone == 0 ? centerZone : zone == 1 ? westZone : eastZone;
                if (!IsFinite(c.x) || !IsFinite(c.y) || !IsFinite(c.z)) return $"{name} must be finite.";
                if (string.Equals(mapName, SwitchyardName, StringComparison.Ordinal) &&
                    !(Mathf.Abs(c.y - groundLevelY) <= GeometryTolerance))
                    return $"{name}.y {c.y} must equal groundLevelY ({groundLevelY}) for Switchyard ground-level objectives.";
                float markerExtent = zoneRadius;
                if (string.Equals(mapName, SwitchyardName, StringComparison.Ordinal))
                    markerExtent = Mathf.Max(zoneRadius + SwitchyardZoneRingLineWidthMeters * 0.5f,
                        SwitchyardZoneCenterMarkerDiameterMeters * 0.5f);
                if (!IsFinite(markerExtent) ||
                    !InsideBounds(Rect.MinMaxRect(c.x - markerExtent, c.z - markerExtent, c.x + markerExtent, c.z + markerExtent)))
                    return $"{name} marker footprint (extent {markerExtent}) leaves the map bounds.";
                if (string.Equals(mapName, SwitchyardName, StringComparison.Ordinal))
                {
                    string breakerError = ValidateOuterLaneBreakerFootprints(name, c);
                    if (breakerError != null) return breakerError;
                }
            }
            return null;
        }

        string ValidateOuterLaneBreakerFootprints(string zoneName, Vector3 center)
        {
            for (int i = 0; i < SwitchyardOuterBreakersPerZone; i++)
            {
                if (!TryGetSwitchyardOuterLaneBreakerGeometry(center, i, out _, out _, out Bounds bounds))
                    return $"{zoneName} outer lane breaker {i} bounds are not finite.";
                if (!InsideBounds(Rect.MinMaxRect(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z)))
                    return $"{zoneName} outer lane breaker {i} footprint leaves the map bounds.";
            }
            return null;
        }

        /// <summary>Wall box depth/width used by SwitchyardGenerator.CreateSpawnRoom (its CreateSpawnBox calls use 0.5).</summary>
        public const float SpawnRoomWallThicknessMeters = 0.5f;
        /// <summary>Number of openings SwitchyardGenerator.CreateSpawnRoom builds in each spawn room's front wall.</summary>
        public const int SwitchyardSpawnExitCount = 3;
        /// <summary>Distance between adjacent generated spawn openings (centres at -pitch, 0, +pitch from the room centre).</summary>
        public const float SwitchyardSpawnExitPitchMeters = 6f;
        /// <summary>Clear width of each generated Switchyard spawn-room opening.</summary>
        public const float SwitchyardSpawnExitOpeningWidthMeters = 4f;
        /// <summary>Depth of the generated exit barrier slab, along the spawn-room wall.</summary>
        public const float SwitchyardSpawnExitBarrierDepthMeters = 0.12f;
        /// <summary>Depth of the collider-free team stripe applied to the spawn-room floor.</summary>
        public const float SwitchyardSpawnRoomTeamStripeDepthMeters = 0.5f;
        /// <summary>Inset of each team spawn point from its generated front wall.</summary>
        public const float SwitchyardSpawnExitInsetFromFrontMeters = 2f;
        /// <summary>Height of the generated spawn-room walls and exit barriers above ground.</summary>
        public const float SwitchyardSpawnRoomWallHeightMeters = 3f;
        /// <summary>Height of the generated exit header, whose bottom sets the clear opening height.</summary>
        public const float SwitchyardSpawnExitHeaderHeightMeters = 0.2f;
        /// <summary>Thickness of the generated spawn-room roof slab.</summary>
        public const float SwitchyardSpawnRoomRoofThicknessMeters = 0.5f;
        /// <summary>Clear vertical span of an exit below its header.</summary>
        public static readonly float SwitchyardSpawnExitClearHeightMeters =
            SwitchyardSpawnRoomWallHeightMeters - SwitchyardSpawnExitHeaderHeightMeters * 0.5f;

        /// <summary>Local Z offset of a generated team's spawn-room front wall from its room centre.</summary>
        public static float SwitchyardSpawnFrontWallOffsetMeters(float roomDepth, int team) =>
            (team == 0 ? -1f : 1f) * roomDepth * 0.5f;

        public bool TryGetSwitchyardSpawnRoomGeometry(int teamId,
            out SwitchyardSpawnBoxGeometry[] boxes, out Bounds solidBounds)
        {
            boxes = null;
            solidBounds = default;
            if (teamId < 0 || teamId > 1 || !IsPositiveFinite(spawnRoomSize.x) ||
                !IsPositiveFinite(spawnRoomSize.y) || !IsFinite(groundLevelY)) return false;

            var generated = new List<SwitchyardSpawnBoxGeometry>(16);
            bool hasSolidBounds = false;
            bool valid = true;
            Bounds calculatedSolidBounds = default;
            void AddBox(string name, SwitchyardSpawnBoxRole role, Vector3 position, Vector3 size, bool hasCollider = true)
            {
                if (!IsFinite(position) || !IsFinite(size))
                {
                    valid = false;
                    return;
                }
                generated.Add(new SwitchyardSpawnBoxGeometry(name, role, position, size, hasCollider));
                if (!hasCollider) return;
                var boxBounds = new Bounds(position, size);
                if (!IsFinite(boxBounds.min) || !IsFinite(boxBounds.max))
                {
                    valid = false;
                    return;
                }
                if (!hasSolidBounds)
                {
                    calculatedSolidBounds = boxBounds;
                    hasSolidBounds = true;
                }
                else calculatedSolidBounds.Encapsulate(boxBounds);
            }

            float facing = teamId == 0 ? -1f : 1f;
            float halfX = spawnRoomSize.x * 0.5f;
            float frontWallOffset = SwitchyardSpawnFrontWallOffsetMeters(spawnRoomSize.y, teamId);
            float wall = SpawnRoomWallThicknessMeters;
            float wallHeight = SwitchyardSpawnRoomWallHeightMeters;

            AddBox("Floor", SwitchyardSpawnBoxRole.Floor,
                new Vector3(0f, groundLevelY + 0.01f, 0f), new Vector3(spawnRoomSize.x, 0.02f, spawnRoomSize.y));
            AddBox("Team stripe", SwitchyardSpawnBoxRole.TeamStripe,
                new Vector3(0f, groundLevelY + 0.025f, frontWallOffset - facing * 0.6f),
                new Vector3(spawnRoomSize.x - 1f, 0.03f, SwitchyardSpawnRoomTeamStripeDepthMeters), hasCollider: false);
            AddBox("Back", SwitchyardSpawnBoxRole.Wall,
                new Vector3(0f, groundLevelY + wallHeight * 0.5f, -frontWallOffset),
                new Vector3(spawnRoomSize.x, wallHeight, wall));
            AddBox("Left", SwitchyardSpawnBoxRole.Wall,
                new Vector3(-halfX, groundLevelY + wallHeight * 0.5f, 0f),
                new Vector3(wall, wallHeight, spawnRoomSize.y));
            AddBox("Right", SwitchyardSpawnBoxRole.Wall,
                new Vector3(halfX, groundLevelY + wallHeight * 0.5f, 0f),
                new Vector3(wall, wallHeight, spawnRoomSize.y));

            float cursor = -halfX;
            float openingHalfWidth = SwitchyardSpawnExitOpeningWidthMeters * 0.5f;
            for (int i = 0; i < SwitchyardSpawnExitCount; i++)
            {
                float exitX = (i - 1) * SwitchyardSpawnExitPitchMeters;
                float length = exitX - openingHalfWidth - cursor;
                if (length > 0f)
                    AddBox("Front pier", SwitchyardSpawnBoxRole.FrontPier,
                        new Vector3(cursor + length * 0.5f, groundLevelY + wallHeight * 0.5f, frontWallOffset),
                        new Vector3(length, wallHeight, wall));
                AddBox("Exit barrier", SwitchyardSpawnBoxRole.ExitBarrier,
                    new Vector3(exitX, groundLevelY + wallHeight * 0.5f, frontWallOffset),
                    new Vector3(SwitchyardSpawnExitOpeningWidthMeters, wallHeight, SwitchyardSpawnExitBarrierDepthMeters));
                AddBox("Exit header", SwitchyardSpawnBoxRole.ExitHeader,
                    new Vector3(exitX, groundLevelY + wallHeight, frontWallOffset),
                    new Vector3(SwitchyardSpawnExitOpeningWidthMeters, SwitchyardSpawnExitHeaderHeightMeters, wall));
                cursor = exitX + openingHalfWidth;
            }
            if (cursor < halfX)
                AddBox("Front pier", SwitchyardSpawnBoxRole.FrontPier,
                    new Vector3((cursor + halfX) * 0.5f, groundLevelY + wallHeight * 0.5f, frontWallOffset),
                    new Vector3(halfX - cursor, wallHeight, wall));
            float roofThickness = SwitchyardSpawnRoomRoofThicknessMeters;
            AddBox("Roof", SwitchyardSpawnBoxRole.Roof,
                new Vector3(0f, groundLevelY + wallHeight + roofThickness * 0.5f, 0f),
                new Vector3(spawnRoomSize.x, roofThickness, spawnRoomSize.y));

            if (!valid || !hasSolidBounds || !IsFinite(calculatedSolidBounds.min) || !IsFinite(calculatedSolidBounds.max))
                return false;
            boxes = generated.ToArray();
            solidBounds = calculatedSolidBounds;
            return true;
        }

        /// <summary>
        /// Fail-closed check of both spawn rooms against the map bounds and of the per-exit spawn
        /// positions produced by <see cref="SpawnExit"/> against their own room and the bounds.
        /// Not Switchyard-specific. <paramref name="roomWallThickness"/> widens each room footprint by
        /// half that value per side (generated walls are centred on the room edge); online load passes 0.
        /// SpawnExit is linear in the exit index, so the first and last exit are the extremes.
        /// </summary>
        public string ValidateSpawnGeometry(float roomWallThickness = 0f)
        {
            string boundsError = ValidateBounds();
            if (boundsError != null) return boundsError;
            if (!IsFinite(groundLevelY)) return "groundLevelY must be finite.";
            if (!IsPositiveFinite(spawnRoomSize.x) || !IsPositiveFinite(spawnRoomSize.y)) return "spawnRoomSize must be positive and finite.";
            if (exitsPerSpawn <= 0) return "exitsPerSpawn must be positive.";
            if (!IsFinite(roomWallThickness) || roomWallThickness < 0f) return "room wall thickness must be finite and non-negative.";

            float margin = roomWallThickness * 0.5f;
            for (int team = 0; team < 2; team++)
            {
                string name = team == 0 ? "northSpawnCenter" : "southSpawnCenter";
                Vector3 center = team == 0 ? northSpawnCenter : southSpawnCenter;
                if (!IsFinite(center.x) || !IsFinite(center.y) || !IsFinite(center.z)) return $"{name} must be finite.";
                float halfX = spawnRoomSize.x * 0.5f + margin, halfZ = spawnRoomSize.y * 0.5f + margin;
                if (!InsideBounds(Rect.MinMaxRect(center.x - halfX, center.z - halfZ, center.x + halfX, center.z + halfZ)))
                    return $"{name} spawn room footprint leaves the map bounds.";
                foreach (int exit in new[] { 0, exitsPerSpawn - 1 })
                {
                    Vector3 p = SpawnExit(team, exit).position;
                    if (!IsFinite(p.x) || !IsFinite(p.y) || !IsFinite(p.z)) return $"{name} exit {exit} spawn position is not finite.";
                    if (!(Mathf.Abs(p.x - center.x) <= spawnRoomSize.x * 0.5f - GeometryTolerance) || !(Mathf.Abs(p.z - center.z) <= spawnRoomSize.y * 0.5f - GeometryTolerance))
                        return $"{name} exit {exit} spawn position is not strictly inside its spawn room (on or beyond the boundary).";
                    if (!InsideBounds(Rect.MinMaxRect(p.x, p.z, p.x, p.z)))
                        return $"{name} exit {exit} spawn position is outside the map bounds.";
                }
            }
            return null;
        }

        /// <summary>
        /// Switchyard-only: SpawnExit positions must line up with the openings CreateSpawnRoom builds, and
        /// the spawn centres must have Y = 0. The generator places each room root at the centre (so a
        /// non-zero Y lifts the floor, walls and openings) and adds groundLevelY to the floor itself,
        /// while SpawnExit uses groundLevelY + 0.55 and ignores the centre Y. Other map names return null.
        /// </summary>
        public string ValidateSwitchyardSpawnLayout()
        {
            if (!string.Equals(mapName, SwitchyardName, StringComparison.Ordinal)) return null;
            if (exitsPerSpawn != SwitchyardSpawnExitCount)
                return $"exitsPerSpawn {exitsPerSpawn} must be {SwitchyardSpawnExitCount}; the generated spawn room has {SwitchyardSpawnExitCount} openings.";
            if (!IsPositiveFinite(spawnRoomSize.x) || !IsPositiveFinite(spawnRoomSize.y)) return "spawnRoomSize must be positive and finite.";
            if (!IsFinite(groundLevelY)) return "groundLevelY must be finite.";
            for (int team = 0; team < 2; team++)
            {
                string name = team == 0 ? "northSpawnCenter" : "southSpawnCenter";
                Vector3 center = team == 0 ? northSpawnCenter : southSpawnCenter;
                if (!IsFinite(center.x) || !IsFinite(center.y) || !IsFinite(center.z)) return $"{name} must be finite.";
                if (team == 0 && !(center.z > 0f)) return "northSpawnCenter.z must be positive so Team A faces toward the arena.";
                if (team == 1 && !(center.z < 0f)) return "southSpawnCenter.z must be negative so Team B faces toward the arena.";
                if (!(Mathf.Abs(center.y) <= GeometryTolerance))
                    return $"{name}.y {center.y} must be 0: the generated room root uses it, while the floor already adds groundLevelY and SpawnExit ignores it.";
                for (int exit = 0; exit < SwitchyardSpawnExitCount; exit++)
                {
                    float expected = (exit - 1) * SwitchyardSpawnExitPitchMeters;
                    Vector3 spawnPosition = SpawnExit(team, exit).position;
                    float actual = spawnPosition.x - center.x;
                    if (!(Mathf.Abs(actual - expected) <= GeometryTolerance))
                        return $"{name} exit {exit} spawns at x offset {actual} but the generated opening is at {expected}; spawnRoomSize.x / exitsPerSpawn must allow a {SwitchyardSpawnExitPitchMeters} m spacing.";
                    float facing = team == 0 ? -1f : 1f;
                    float expectedZ = SwitchyardSpawnFrontWallOffsetMeters(spawnRoomSize.y, team) - facing * SwitchyardSpawnExitInsetFromFrontMeters;
                    float actualZ = spawnPosition.z - center.z;
                    if (!(Mathf.Abs(actualZ - expectedZ) <= GeometryTolerance))
                        return $"{name} exit {exit} spawns at z offset {actualZ} but the generated front wall is at {SwitchyardSpawnFrontWallOffsetMeters(spawnRoomSize.y, team)} with a {SwitchyardSpawnExitInsetFromFrontMeters} m inset.";
                }
            }
            return null;
        }

        /// <summary>
        /// Switchyard-only: both fixed-width sightline blockers must remain inside the bounds and
        /// between the centre and the inner edge of each generated spawn room, and each full opening
        /// interval must remain inside both blockers' shared X span, and their height must cover the
        /// full clear opening. This covers every straight line between points in opposing openings.
        /// </summary>
        public string ValidateSwitchyardSightlineBlockers()
        {
            if (!string.Equals(mapName, SwitchyardName, StringComparison.Ordinal)) return null;
            string boundsError = ValidateBounds();
            if (boundsError != null) return boundsError;
            string spawnLayoutError = ValidateSwitchyardSpawnLayout();
            if (spawnLayoutError != null) return spawnLayoutError;
            if (!IsPositiveFinite(sightlineBlockerZ)) return "sightlineBlockerZ must be positive and finite.";
            if (SightlineBlockerHeightMeters < SwitchyardSpawnExitClearHeightMeters - GeometryTolerance)
                return "sightline blockers must cover the full clear height of the generated spawn exits.";

            if (!TryGetSwitchyardSightlineBlockerGeometry(0, out _, out _, out Bounds northBounds) ||
                !TryGetSwitchyardSightlineBlockerGeometry(1, out _, out _, out Bounds southBounds))
                return "sightline-blocker bounds are not finite.";
            float roomHalfDepth = spawnRoomSize.y * 0.5f + SpawnRoomWallThicknessMeters * 0.5f;
            float northRoomInnerEdge = northSpawnCenter.z - roomHalfDepth;
            float southRoomInnerEdge = -southSpawnCenter.z - roomHalfDepth;
            float nearestRoomInnerEdge = Mathf.Min(northRoomInnerEdge, southRoomInnerEdge);
            if (northBounds.min.z <= GeometryTolerance ||
                northBounds.max.z >= nearestRoomInnerEdge - GeometryTolerance)
                return "sightline blockers must fit between the centre and the inner edge of both spawn rooms.";

            Rect northBlocker = HorizontalFootprint(northBounds);
            Rect southBlocker = HorizontalFootprint(southBounds);
            if (!InsideBounds(northBlocker) || !InsideBounds(southBlocker))
                return "sightline blocker footprints leave the map bounds.";

            // The approved map decision is that no spawn-exit pair has an open line. Cover each full
            // generated opening interval, inset by the existing tolerance so edge contact is rejected.
            float blockerInteriorMinX = northBounds.min.x + GeometryTolerance;
            float blockerInteriorMaxX = northBounds.max.x - GeometryTolerance;
            float openingHalfWidth = SwitchyardSpawnExitOpeningWidthMeters * 0.5f;
            for (int team = 0; team < 2; team++)
            {
                for (int exit = 0; exit < SwitchyardSpawnExitCount; exit++)
                {
                    float exitCenterX = SpawnExit(team, exit).position.x;
                    float openingMinX = exitCenterX - openingHalfWidth;
                    float openingMaxX = exitCenterX + openingHalfWidth;
                    if (!(openingMinX >= blockerInteriorMinX && openingMaxX <= blockerInteriorMaxX))
                        return $"team {(team == 0 ? "A" : "B")} exit {exit} opening ({openingMinX} to {openingMaxX} m) is not fully covered by the sightline blockers' X span ({blockerInteriorMinX} to {blockerInteriorMaxX} m).";
                }
            }
            return null;
        }

        /// <summary>
        /// Switchyard-only: the configured player capsule must fit inside each generated spawn exit
        /// from its actual SpawnExit pose. PlayerController anchors the CharacterController capsule
        /// at the transform position, so its top is position.y + capsuleHeight.
        /// </summary>
        public string ValidateSwitchyardSpawnCapsuleClearance(MovementConfig movementConfig)
        {
            if (!string.Equals(mapName, SwitchyardName, StringComparison.Ordinal)) return null;
            if (movementConfig == null) return "MovementConfig is required to validate spawn-exit capsule clearance.";
            if (!IsPositiveFinite(movementConfig.capsuleHeight)) return "MovementConfig.capsuleHeight must be positive and finite.";
            if (!IsPositiveFinite(movementConfig.capsuleRadius)) return "MovementConfig.capsuleRadius must be positive and finite.";

            float requiredWidth = movementConfig.capsuleRadius * 2f;
            if (!IsPositiveFinite(requiredWidth) || requiredWidth > SwitchyardSpawnExitOpeningWidthMeters - GeometryTolerance)
                return $"player capsule diameter ({requiredWidth} m) must fit inside the {SwitchyardSpawnExitOpeningWidthMeters} m spawn openings.";

            float openingTop = groundLevelY + SwitchyardSpawnExitClearHeightMeters;
            if (!IsFinite(openingTop)) return "spawn-exit clear height must be finite.";
            for (int team = 0; team < 2; team++)
            {
                for (int exit = 0; exit < SwitchyardSpawnExitCount; exit++)
                {
                    Vector3 spawn = SpawnExit(team, exit).position;
                    float capsuleTop = spawn.y + movementConfig.capsuleHeight;
                    if (!IsFinite(capsuleTop) || capsuleTop > openingTop - GeometryTolerance)
                        return $"team {(team == 0 ? "A" : "B")} exit {exit} capsule top ({capsuleTop} m) exceeds the opening top ({openingTop} m).";
                }
            }
            return null;
        }

        /// <summary>
        /// Switchyard-only: the solid spawn-room envelope must not overlap raised decks, generated
        /// ramps, sightline blockers, or objective breakers. Bounds follow the generator's box sizes.
        /// </summary>
        public string ValidateSwitchyardSpawnRoomClearance()
        {
            if (!string.Equals(mapName, SwitchyardName, StringComparison.Ordinal)) return null;
            string boundsError = ValidateBounds();
            if (boundsError != null) return boundsError;
            string objectiveError = ValidateObjectiveZones();
            if (objectiveError != null) return objectiveError;
            string spawnGeometryError = ValidateSpawnGeometry(SpawnRoomWallThicknessMeters);
            if (spawnGeometryError != null) return spawnGeometryError;
            string spawnLayoutError = ValidateSwitchyardSpawnLayout();
            if (spawnLayoutError != null) return spawnLayoutError;
            string sightlineError = ValidateSwitchyardSightlineBlockers();
            if (sightlineError != null) return sightlineError;
            if (platformCenters == null) return "platformCenters is missing.";
            if (!IsPositiveFinite(platformSize.x) || !IsPositiveFinite(platformSize.y)) return "platformSize must be positive and finite.";
            if (!IsPositiveFinite(rampWidthMeters) || !IsPositiveFinite(rampRunMeters)) return "ramp dimensions must be positive and finite.";
            if (!IsPositiveFinite(spawnRoomSize.x) || !IsPositiveFinite(spawnRoomSize.y)) return "spawnRoomSize must be positive and finite.";
            if (!IsFinite(groundLevelY)) return "groundLevelY must be finite.";

            Bounds northRoomBounds = default;
            for (int team = 0; team < 2; team++)
            {
                string roomName = team == 0 ? "north" : "south";
                Vector3 roomCenter = team == 0 ? northSpawnCenter : southSpawnCenter;
                if (!TryGetSwitchyardSpawnRoomGeometry(team, out _, out Bounds roomBounds))
                    return $"{roomName} spawn-room bounds are not finite.";
                roomBounds.center += roomCenter;
                if (!IsFinite(roomBounds.min) || !IsFinite(roomBounds.max))
                    return $"{roomName} spawn-room bounds are not finite.";
                if (team == 0) northRoomBounds = roomBounds;
                else if (MeaningfullyOverlaps(northRoomBounds, roomBounds))
                    return "north and south spawn rooms overlap.";

                for (int i = 0; i < platformCenters.Length; i++)
                {
                    Vector3 deck = platformCenters[i];
                    if (!IsFinite(deck)) return $"platformCenters[{i}] must be finite.";
                    if (Mathf.Abs(deck.x) <= GeometryTolerance || Mathf.Abs(deck.z) <= GeometryTolerance)
                        return $"platformCenters[{i}] must not lie on x=0 or z=0; its inward ramp direction is undefined.";
                    if (!InsideBounds(DeckFootprint(deck)))
                        return $"platformCenters[{i}] deck footprint leaves the map bounds.";
                    if (!TryGetSwitchyardDeckGeometry(deck, out _, out _, out Bounds deckBounds))
                        return $"platformCenters[{i}] deck bounds are not finite.";
                    if (MeaningfullyOverlaps(roomBounds, deckBounds))
                        return $"{roomName} spawn room overlaps platformCenters[{i}].";

                    for (int axis = 0; axis < 2; axis++)
                    {
                        RampEndpoints(deck, alongX: axis == 0, out Vector3 ground, out Vector3 edge);
                        if (!IsFinite(ground) || !IsFinite(edge))
                            return $"platformCenters[{i}] ramp endpoints are not finite.";
                        if (!TryRampBounds(ground, edge, out Bounds rampBounds))
                            return $"platformCenters[{i}] ramp bounds are not finite or have a zero length.";
                        Rect rampFootprint = Rect.MinMaxRect(rampBounds.min.x, rampBounds.min.z, rampBounds.max.x, rampBounds.max.z);
                        if (!InsideBounds(rampFootprint))
                            return $"platformCenters[{i}] ramp footprint leaves the map bounds.";
                        if (MeaningfullyOverlaps(roomBounds, rampBounds))
                            return $"{roomName} spawn room overlaps the ramp for platformCenters[{i}] ({(axis == 0 ? "X" : "Z")}).";
                    }
                }

                for (int blockerTeam = 0; blockerTeam < 2; blockerTeam++)
                {
                    if (!TryGetSwitchyardSightlineBlockerGeometry(blockerTeam, out _, out _, out Bounds blockerBounds))
                        return "sightline-blocker bounds are not finite.";
                    if (MeaningfullyOverlaps(roomBounds, blockerBounds))
                        return $"{roomName} spawn room overlaps the {(blockerTeam == 0 ? "north" : "south")} sightline blocker.";
                }

                Vector3[] zones = { centerZone, westZone, eastZone };
                for (int zone = 0; zone < zones.Length; zone++)
                {
                    Vector3 zoneCenter = zones[zone];
                    for (int i = 0; i < SwitchyardOuterBreakersPerZone; i++)
                    {
                        if (!TryGetSwitchyardOuterLaneBreakerGeometry(zoneCenter, i, out _, out _, out Bounds breakerBounds))
                            return $"outer breaker {zone}:{i} bounds are not finite.";
                        if (MeaningfullyOverlaps(roomBounds, breakerBounds))
                            return $"{roomName} spawn room overlaps outer breaker {zone}:{i}.";
                    }
                }
            }
            return null;
        }

        static bool MeaningfullyOverlaps(Bounds a, Bounds b)
        {
            Vector3 overlap = Vector3.Min(a.max, b.max) - Vector3.Max(a.min, b.min);
            return overlap.x > GeometryTolerance && overlap.y > GeometryTolerance && overlap.z > GeometryTolerance;
        }

        public bool TryGetSwitchyardSightlineBlockerGeometry(int team, out Vector3 position, out Vector3 size, out Bounds bounds)
        {
            // Keep the shared blocker span centred between the two configured spawn rooms so
            // mirrored openings remain covered if a map layout moves both teams off X = 0.
            float blockerCenterX = northSpawnCenter.x * 0.5f + southSpawnCenter.x * 0.5f;
            position = new Vector3(blockerCenterX, groundLevelY + SightlineBlockerHeightMeters * 0.5f,
                (team == 0 ? 1f : -1f) * sightlineBlockerZ);
            size = new Vector3(SightlineBlockerWidthMeters, SightlineBlockerHeightMeters, SightlineBlockerDepthMeters);
            bounds = new Bounds(position, size);
            return team >= 0 && team <= 1 && IsFinite(position) && IsFinite(size) &&
                IsFinite(bounds.min) && IsFinite(bounds.max);
        }

        bool TryRampTransform(Vector3 ground, Vector3 edge, out Vector3 position, out Quaternion rotation, out Vector3 size)
        {
            position = default;
            rotation = Quaternion.identity;
            size = default;
            if (!IsFinite(ground) || !IsFinite(edge)) return false;
            float length = Vector3.Distance(ground, edge);
            if (!IsPositiveFinite(length) || !IsPositiveFinite(rampWidthMeters)) return false;
            rotation = Quaternion.LookRotation((edge - ground).normalized, Vector3.up);
            position = (ground + edge) * 0.5f - rotation * Vector3.up * (RampThicknessMeters * 0.5f);
            size = new Vector3(rampWidthMeters, RampThicknessMeters, length);
            return IsFinite(position) && IsFinite(size);
        }

        bool TryRampBounds(Vector3 ground, Vector3 edge, out Bounds bounds)
        {
            bounds = default;
            if (!TryRampTransform(ground, edge, out Vector3 position, out Quaternion rotation, out Vector3 size)) return false;
            Vector3 half = size * 0.5f;
            Vector3 first = position + rotation * new Vector3(-half.x, -half.y, -half.z);
            if (!IsFinite(first)) return false;
            bounds = new Bounds(first, Vector3.zero);
            for (int corner = 1; corner < 8; corner++)
            {
                Vector3 local = new Vector3((corner & 1) == 0 ? -half.x : half.x,
                    (corner & 2) == 0 ? -half.y : half.y, (corner & 4) == 0 ? -half.z : half.z);
                Vector3 vertex = position + rotation * local;
                if (!IsFinite(vertex)) return false;
                bounds.Encapsulate(vertex);
            }
            return IsFinite(bounds.min) && IsFinite(bounds.max);
        }

        /// <summary>Spawn point just inside a configured team exit, facing the arena (team 0 = north).</summary>
        public Pose SpawnExit(int team, int exit)
        {
            Vector3 center = team == 0 ? northSpawnCenter : southSpawnCenter;
            float facing = team == 0 ? -1f : 1f;
            int exitCount = Mathf.Max(1, exitsPerSpawn);
            int exitIndex = ((exit % exitCount) + exitCount) % exitCount;
            float lateralSpacing = Mathf.Min(6f, spawnRoomSize.x / exitCount);
            float lateralOffset = (exitIndex - (exitCount - 1) * 0.5f) * lateralSpacing;
            float frontWallOffset = SwitchyardSpawnFrontWallOffsetMeters(spawnRoomSize.y, team);
            var position = center + new Vector3(lateralOffset, groundLevelY + 0.55f,
                frontWallOffset - facing * SwitchyardSpawnExitInsetFromFrontMeters);
            return new Pose(position, Quaternion.Euler(0, team == 0 ? 180f : 0f, 0));
        }

        public Vector3 ZoneCenter(ZoneId zone) => zone switch
        {
            ZoneId.West => westZone,
            ZoneId.East => eastZone,
            _ => centerZone,
        };

        public bool IsInsideSpawnRoom(int team, Vector3 position)
        {
            Vector3 center = team == 0 ? northSpawnCenter : southSpawnCenter;
            return Mathf.Abs(position.x - center.x) <= spawnRoomSize.x * 0.5f && Mathf.Abs(position.z - center.z) <= spawnRoomSize.y * 0.5f;
        }
    }
}
