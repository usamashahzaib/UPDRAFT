using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Hotdrop.Config;
using Hotdrop.Map;

namespace Hotdrop.Tests
{
    /// <summary>
    /// Gate B mirrored-spawn audit (spec section 9): the generated Switchyard is the same
    /// for both teams. Every solid collider has a partner reflected across Z = 0 (north vs
    /// south team) and across X = 0 (West vs East zone), and no exit-to-exit spawn line is open.
    /// </summary>
    public class SwitchyardMirrorTests
    {
        GameObject root;
        MapConfig map;
        readonly List<Bounds> solids = new List<Bounds>();

        [SetUp]
        public void SetUp()
        {
            map = AssetDatabase.LoadAssetAtPath<MapConfig>("Assets/Resources/Config/MapConfig.asset");
            root = new GameObject("Switchyard (mirror audit)");
            var generator = root.AddComponent<SwitchyardGenerator>();
            generator.mapConfig = map;
            generator.GenerateMap();
            Physics.SyncTransforms();
            solids.Clear();
            foreach (var c in root.GetComponentsInChildren<Collider>())
                if (c.enabled && !c.isTrigger) solids.Add(c.bounds);
            Assert.Greater(solids.Count, 20, "The arena must actually generate.");
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.name == "Switchyard sunlight") Object.DestroyImmediate(light.gameObject);
        }

        void AssertMirrored(System.Func<Vector3, Vector3> mirror, string axis)
        {
            var unmatched = new List<string>();
            foreach (var b in solids)
            {
                Vector3 want = mirror(b.center);
                bool found = false;
                foreach (var other in solids)
                    if ((other.center - want).sqrMagnitude < 0.05f * 0.05f && (other.size - b.size).sqrMagnitude < 0.05f * 0.05f)
                    { found = true; break; }
                if (!found) unmatched.Add($"{b.center} size {b.size}");
            }
            Assert.IsEmpty(unmatched, $"Solids with no mirror across {axis}:\n" + string.Join("\n", unmatched));
        }

        [Test] public void Geometry_IsMirroredBetweenTeams_AcrossZ() => AssertMirrored(c => new Vector3(c.x, c.y, -c.z), "Z = 0");

        [Test] public void Geometry_IsMirroredBetweenFlanks_AcrossX() => AssertMirrored(c => new Vector3(-c.x, c.y, c.z), "X = 0");

        [Test]
        public void EachSpawnRoom_HasThreeExits_OwnedByItsTeam()
        {
            int north = 0, south = 0;
            foreach (var barrier in root.GetComponentsInChildren<SpawnExitBarrier>())
            {
                if (barrier.OwnerTeam == 0) { north++; Assert.Greater(barrier.transform.position.z, 0f); }
                else { south++; Assert.Less(barrier.transform.position.z, 0f); }
            }
            Assert.AreEqual(map.exitsPerSpawn, north);
            Assert.AreEqual(map.exitsPerSpawn, south);
        }

        [Test]
        public void EveryPlatform_HasTwoRampsReachingIt()
        {
            foreach (var platform in map.platformCenters)
            {
                int ramps = 0;
                foreach (Transform child in root.transform)
                {
                    if (child.name != "Ramp") continue;
                    Vector3 top = child.position + child.up * 0.075f + child.forward * child.localScale.z * 0.5f;
                    if (Mathf.Abs(top.y - map.platformHeight) < 0.05f &&
                        Mathf.Abs(top.x - platform.x) <= map.platformSize.x * 0.5f + 0.05f &&
                        Mathf.Abs(top.z - platform.z) <= map.platformSize.y * 0.5f + 0.05f) ramps++;
                }
                Assert.AreEqual(2, ramps, $"Platform at {platform}");
            }
        }

        [Test]
        public void NoDirectShot_FromAnySpawnExitToTheOtherSpawnExits()
        {
            int mask = LayerMask.GetMask("Ground", "Cover");
            var open = new List<string>();
            float frontZ = map.northSpawnCenter.z - map.spawnRoomSize.y * 0.5f - 0.5f;
            for (int a = -1; a <= 1; a++)
                for (int b = -1; b <= 1; b++)
                    for (float dx = -1.5f; dx <= 1.5f; dx += 1.5f)
                    {
                        // Standing just outside each exit (exits are 4 m wide at X = -6/0/6), eye to chest.
                        Vector3 from = new Vector3(a * 6f + dx, 1.6f, frontZ);
                        Vector3 to = new Vector3(b * 6f - dx, 1.4f, -frontZ);
                        if (!Physics.Linecast(from, to, mask, QueryTriggerInteraction.Ignore)) open.Add($"{from} -> {to}");
                    }
            Assert.IsEmpty(open, "Open spawn-to-spawn sightlines:\n" + string.Join("\n", open));
        }

        [Test]
        public void CenterPresentation_IsVisibleButNeverCompetitiveGeometry()
        {
            var presentation = root.transform.Find("Center communications tower (presentation only)");
            Assert.IsNotNull(presentation, "Package 1 needs one reviewable dressed Switchyard section");
            Assert.Greater(presentation.childCount, 0, "the Center landmark must contain visible presentation parts");
            Assert.AreEqual(0, presentation.GetComponentsInChildren<Collider>(true).Length,
                "presentation dressing must not create cover, routes, hit surfaces, or change zone occupation");
        }
    }
}


