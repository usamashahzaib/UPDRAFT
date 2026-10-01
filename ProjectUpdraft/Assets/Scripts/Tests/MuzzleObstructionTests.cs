using NUnit.Framework;
using UnityEngine;
using Updraft.Weapons;

namespace Updraft.Tests
{
    /// <summary>
    /// QA-012 regression: a muzzle poked through (or into) thin cover must count as blocked;
    /// otherwise a ray starting at the muzzle never sees that wall and the shot passes through.
    /// </summary>
    public class MuzzleObstructionTests
    {
        const int Mask = 1; // Default layer
        GameObject wall;

        [TearDown]
        public void TearDown() { if (wall != null) Object.DestroyImmediate(wall); }

        void Wall(float z, float thickness)
        {
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0f, 1f, z);
            wall.transform.localScale = new Vector3(4f, 2f, thickness);
            Physics.SyncTransforms();
        }

        static readonly Vector3 Body = new Vector3(0f, 1.15f, 0f);
        static readonly Vector3 Muzzle = new Vector3(0.28f, 1.15f, 0.65f);

        [Test]
        public void ThinWallBetweenBodyAndMuzzle_IsBlocked()
        {
            Wall(0.4f, 0.05f); // gun barrel has passed through a 5 cm panel
            Assert.IsTrue(MuzzleObstruction.IsBuried(Physics.defaultPhysicsScene, Body, Muzzle, Mask, out var at));
            Assert.Less(at.z, 0.45f, "the shot stops at the panel, not beyond it");
        }

        [Test]
        public void MuzzleInsideThickCover_IsBlocked()
        {
            Wall(0.65f, 0.3f);
            Assert.IsTrue(MuzzleObstruction.IsBuried(Physics.defaultPhysicsScene, Body, Muzzle, Mask, out _));
        }

        [Test]
        public void ClearSpace_IsNotBlocked()
        {
            Wall(3f, 0.2f); // cover in front of the muzzle, not around it
            Assert.IsFalse(MuzzleObstruction.IsBuried(Physics.defaultPhysicsScene, Body, Muzzle, Mask, out _));
        }
    }
}
