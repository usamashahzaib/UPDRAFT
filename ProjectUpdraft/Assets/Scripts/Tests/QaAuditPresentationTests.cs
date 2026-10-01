using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Updraft.Camera;
using Updraft.Core;
using Updraft.Pads;
using Updraft.Player;

namespace Updraft.Tests
{
    /// <summary>
    /// Regressions for QA-021 (camera shake preference restore), QA-022 (SFX volume on
    /// PlayerController's direct AudioSource path), QA-023 (non-colour team cue) and
    /// QA-034 (NetBotDriver brain retention). Network types are resolved by name so this
    /// assembly needs no Fusion references.
    /// </summary>
    public class QaAuditPresentationTests
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        const BindingFlags HiddenStatic = BindingFlags.Static | BindingFlags.NonPublic;

        // ---------- QA-021 ----------

        static ThirdPersonCamera CreateCameraWithPref(int prefValue, out GameObject go)
        {
            PlayerPrefs.SetInt(ThirdPersonCamera.ShakePrefKey, prefValue);
            go = new GameObject("ShakePrefCamera");
            go.SetActive(false);
            var cam = go.AddComponent<ThirdPersonCamera>();
            typeof(ThirdPersonCamera).GetMethod("Awake", Hidden).Invoke(cam, null);
            return cam;
        }

        [Test]
        public void CameraShake_SavedOff_IsRestoredOnAwake()
        {
            bool had = PlayerPrefs.HasKey(ThirdPersonCamera.ShakePrefKey);
            int old = PlayerPrefs.GetInt(ThirdPersonCamera.ShakePrefKey, 1);
            GameObject go = null;
            try
            {
                var cam = CreateCameraWithPref(0, out go);
                cam.TriggerShake();
                Assert.IsFalse(cam.IsShaking, "a saved OFF shake preference must survive a restart");
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
                if (had) PlayerPrefs.SetInt(ThirdPersonCamera.ShakePrefKey, old); else PlayerPrefs.DeleteKey(ThirdPersonCamera.ShakePrefKey);
            }
        }

        [Test]
        public void CameraShake_SavedOn_StillShakes()
        {
            bool had = PlayerPrefs.HasKey(ThirdPersonCamera.ShakePrefKey);
            int old = PlayerPrefs.GetInt(ThirdPersonCamera.ShakePrefKey, 1);
            GameObject go = null;
            try
            {
                var cam = CreateCameraWithPref(1, out go);
                cam.TriggerShake();
                Assert.IsTrue(cam.IsShaking);
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
                if (had) PlayerPrefs.SetInt(ThirdPersonCamera.ShakePrefKey, old); else PlayerPrefs.DeleteKey(ThirdPersonCamera.ShakePrefKey);
            }
        }

        // ---------- QA-022 ----------

        static float PlayerSfxVolume(float baseVolume)
        {
            var method = typeof(PlayerController).GetMethod("EffectiveSfxVolume", HiddenStatic);
            Assert.IsNotNull(method, "PlayerController.EffectiveSfxVolume missing");
            return (float)method.Invoke(null, new object[] { baseVolume });
        }

        [Test]
        public void PlayerDirectSounds_HonourSfxVolume()
        {
            bool had = PlayerPrefs.HasKey(GameAudio.SfxKey);
            float old = PlayerPrefs.GetFloat(GameAudio.SfxKey, 1f);
            try
            {
                PlayerPrefs.SetFloat(GameAudio.SfxKey, 0f);
                Assert.AreEqual(0f, GameAudio.SfxVolume);
                Assert.AreEqual(0f, PlayerSfxVolume(0.85f), "weapon fire must be silent at SFX 0");
                Assert.AreEqual(0f, PlayerSfxVolume(1f), "land/jump/damage must be silent at SFX 0");

                PlayerPrefs.SetFloat(GameAudio.SfxKey, 0.5f);
                Assert.AreEqual(0.425f, PlayerSfxVolume(0.85f), 1e-5f);
            }
            finally
            {
                if (had) PlayerPrefs.SetFloat(GameAudio.SfxKey, old); else PlayerPrefs.DeleteKey(GameAudio.SfxKey);
            }
        }

        // ---------- QA-023 ----------

        static void AssertCue(Transform avatar, bool friendly)
        {
            var root = avatar.Find(TeamCueVisual.RootName);
            Assert.IsNotNull(root, "team cue root missing");
            var marker = root.Find(friendly ? TeamCueVisual.AllyMarkerName : TeamCueVisual.EnemyMarkerName);
            var pattern = root.Find(friendly ? TeamCueVisual.AllyPatternName : TeamCueVisual.EnemyPatternName);
            Assert.IsNotNull(marker, "head marker missing");
            Assert.IsNotNull(pattern, "body pattern missing");
            Assert.IsNull(root.Find(friendly ? TeamCueVisual.EnemyMarkerName : TeamCueVisual.AllyMarkerName));
            Assert.IsNull(root.Find(friendly ? TeamCueVisual.EnemyPatternName : TeamCueVisual.AllyPatternName));
            Assert.AreEqual(0, root.GetComponentsInChildren<Collider>(true).Length, "cue must never be a hit surface");
        }

        static string MarkerShape(Transform avatar, bool friendly)
        {
            var marker = avatar.Find(TeamCueVisual.RootName).Find(friendly ? TeamCueVisual.AllyMarkerName : TeamCueVisual.EnemyMarkerName);
            var filters = marker.GetComponentsInChildren<MeshFilter>(true);
            var shape = filters.Length + ":";
            foreach (var f in filters) shape += f.sharedMesh != null ? f.sharedMesh.name : "none";
            return shape;
        }

        [Test]
        public void TeamCue_AllyAndEnemy_DifferByShapeNotOnlyColour()
        {
            var ally = new GameObject("Ally");
            var enemy = new GameObject("Enemy");
            try
            {
                TeamCueVisual.Apply(ally.transform, true);
                TeamCueVisual.Apply(enemy.transform, false);
                AssertCue(ally.transform, true);
                AssertCue(enemy.transform, false);

                // Same material would still leave the silhouettes distinct.
                Assert.AreNotEqual(MarkerShape(ally.transform, true), MarkerShape(enemy.transform, false));
                int allyPatternParts = ally.transform.Find(TeamCueVisual.RootName).Find(TeamCueVisual.AllyPatternName).childCount;
                int enemyPatternParts = enemy.transform.Find(TeamCueVisual.RootName).Find(TeamCueVisual.EnemyPatternName).childCount;
                Assert.AreNotEqual(allyPatternParts, enemyPatternParts, "chest band vs shoulder spikes");

                // Re-applying (team/viewer change) replaces rather than stacks.
                TeamCueVisual.Apply(ally.transform, false);
                int roots = 0;
                foreach (Transform child in ally.transform) if (child.name == TeamCueVisual.RootName) roots++;
                Assert.AreEqual(1, roots);
                AssertCue(ally.transform, false);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ally);
                UnityEngine.Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void OfflineAvatar_TeamSetup_AddsViewerRelativeShapeCue()
        {
            var a = new GameObject("OfflineAvatarA");
            var b = new GameObject("OfflineAvatarB");
            a.SetActive(false);
            b.SetActive(false);
            try
            {
                var pcA = a.AddComponent<PlayerController>();
                var pcB = b.AddComponent<PlayerController>();
                var setTeam = typeof(PlayerController).GetMethod("SetTeamMaterial", Hidden);
                Assert.IsNotNull(setTeam);
                setTeam.Invoke(pcA, new object[] { 0 });
                setTeam.Invoke(pcB, new object[] { 1 });

                bool friendlyA = LaunchPadBehaviour.IsFriendlyToViewer(0);
                bool friendlyB = LaunchPadBehaviour.IsFriendlyToViewer(1);
                Assert.AreNotEqual(friendlyA, friendlyB, "teams 0 and 1 must read as opposite sides to any viewer");
                AssertCue(a.transform, friendlyA);
                AssertCue(b.transform, friendlyB);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(a);
                UnityEngine.Object.DestroyImmediate(b);
            }
        }

        // ---------- QA-034 ----------

        static Type DriverType()
        {
            var t = Type.GetType("Updraft.Network.NetBotDriver, Updraft.Network");
            Assert.IsNotNull(t, "NetBotDriver type not found");
            return t;
        }

        static IDictionary Brains(Type driver) => (IDictionary)driver.GetField("brains", HiddenStatic).GetValue(null);

        static object NewBrain(Type driver)
        {
            var brainType = driver.GetNestedType("Brain", BindingFlags.NonPublic);
            Assert.IsNotNull(brainType);
            return Activator.CreateInstance(brainType, true);
        }

        static Component NewNetPlayer(out GameObject go)
        {
            var playerType = Type.GetType("Updraft.Network.NetPlayer, Updraft.Network");
            Assert.IsNotNull(playerType);
            go = new GameObject("RetentionNetPlayer");
            go.SetActive(false);
            return go.AddComponent(playerType);
        }

        [Test]
        public void BotDriver_PrunesDestroyedPlayers()
        {
            var driver = DriverType();
            var brains = Brains(driver);
            var player = NewNetPlayer(out var go);
            brains[player] = NewBrain(driver);
            Assert.IsTrue(brains.Contains(player));

            UnityEngine.Object.DestroyImmediate(go);
            driver.GetMethod("PruneDestroyed", HiddenStatic).Invoke(null, null);

            Assert.IsFalse(brains.Contains(player), "destroyed players must not be retained by the bot driver");
        }

        [Test]
        public void BotDriver_ForgetRemovesEntry()
        {
            var driver = DriverType();
            var brains = Brains(driver);
            var player = NewNetPlayer(out var go);
            try
            {
                brains[player] = NewBrain(driver);
                driver.GetMethod("Forget", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { player });
                Assert.IsFalse(brains.Contains(player));
            }
            finally
            {
                brains.Remove(player);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
