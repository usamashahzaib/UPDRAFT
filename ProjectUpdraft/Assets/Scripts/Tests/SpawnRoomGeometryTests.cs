using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Updraft.Config;
using Updraft.Player;

namespace Updraft.Tests
{
    /// <summary>
    /// QA-007 regression: offline spawn-room recognition (used to block firing and end
    /// protection) must cover the whole 20 x 8 m room, identical to the online MapConfig rule.
    /// </summary>
    public class SpawnRoomGeometryTests
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        MapConfig map;
        GameObject go;
        PlayerController player;

        [SetUp]
        public void SetUp()
        {
            map = AssetDatabase.LoadAssetAtPath<MapConfig>("Assets/Resources/Config/MapConfig.asset");
            go = new GameObject("SpawnRoomProbe");
            go.SetActive(false); // no lifecycle: only the geometry query is exercised
            player = go.AddComponent<PlayerController>();
            typeof(PlayerController).GetField("mapConfig", Hidden).SetValue(player, map);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(go);

        bool OfflineInside(int team, Vector3 position)
        {
            typeof(PlayerController).GetField("teamId", Hidden).SetValue(player, team);
            go.transform.position = position;
            return (bool)typeof(PlayerController).GetMethod("IsInsideOwnSpawnRoom", Hidden).Invoke(player, null);
        }

        [Test]
        public void OffCentrePositions_InsideRoom_AreRecognised()
        {
            for (int team = 0; team < 2; team++)
            {
                Vector3 c = team == 0 ? map.northSpawnCenter : map.southSpawnCenter;
                float hx = map.spawnRoomSize.x * 0.5f, hz = map.spawnRoomSize.y * 0.5f;
                foreach (var offset in new[] { new Vector3(0, 0, hz - 0.2f), new Vector3(0, 0, -hz + 0.2f),
                                               new Vector3(hx - 0.2f, 0, hz * 0.5f), new Vector3(-hx + 0.2f, 0, -hz * 0.5f) })
                {
                    Assert.IsTrue(OfflineInside(team, c + offset), $"team {team} offset {offset} is inside the room");
                    Assert.AreEqual(map.IsInsideSpawnRoom(team, c + offset), OfflineInside(team, c + offset), "offline must match online");
                }
            }
        }

        [Test]
        public void PositionsOutsideRoom_AreNotRecognised()
        {
            Vector3 c = map.northSpawnCenter;
            Assert.IsFalse(OfflineInside(0, c + new Vector3(0, 0, map.spawnRoomSize.y * 0.5f + 0.5f)));
            Assert.IsFalse(OfflineInside(0, c + new Vector3(map.spawnRoomSize.x * 0.5f + 0.5f, 0, 0)));
            Assert.IsFalse(OfflineInside(1, c), "the enemy spawn room is not your own");
        }
    }
}
