using NUnit.Framework;
using UnityEngine;
using Updraft.Camera;

namespace Updraft.Tests
{
    /// <summary>
    /// QA-004 regression: the camera sphere must stop on the near (player) side of an
    /// obstruction. The old placement, hit.point + direction * radius, pushed it into or
    /// past the wall.
    /// </summary>
    public class CameraCollisionTests
    {
        const float Radius = 0.3f;
        const int Mask = 1; // Default layer
        GameObject wall;

        [TearDown]
        public void TearDown()
        {
            if (wall != null) Object.DestroyImmediate(wall);
        }

        void MakeWall(float nearFaceZ, float thickness)
        {
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0f, 1.5f, nearFaceZ - thickness * 0.5f);
            wall.transform.localScale = new Vector3(6f, 3f, thickness);
            Physics.SyncTransforms();
        }

        [Test]
        public void ThinWall_BehindPlayer_CameraStaysOnPlayerSide()
        {
            MakeWall(nearFaceZ: -1.5f, thickness: 0.1f); // thin wall 1.5 m behind the pivot
            var pivot = new Vector3(0f, 1.4f, 0f);
            var desired = new Vector3(0f, 1.4f, -3.2f);

            Vector3 result = ThirdPersonCamera.ClampToClearSpace(pivot, desired, Radius, Mask);

            Assert.Greater(result.z, -1.5f + Radius - 0.06f, "camera sphere must not penetrate the wall's near face");
            Assert.Less(result.z, -1.0f, "camera should still pull back as far as the wall allows");
        }

        [Test]
        public void NoObstruction_ReturnsDesired()
        {
            var pivot = new Vector3(50f, 1.4f, 0f);
            var desired = new Vector3(50f, 1.4f, -3.2f);
            Assert.AreEqual(desired, ThirdPersonCamera.ClampToClearSpace(pivot, desired, Radius, Mask));
        }

        [Test]
        public void PivotPressedAgainstWall_StillCannotPassThrough()
        {
            MakeWall(nearFaceZ: -0.1f, thickness: 0.2f); // pivot sphere already overlaps the wall
            var pivot = new Vector3(0f, 1.4f, 0f);
            var desired = new Vector3(0f, 1.4f, -3.2f);

            Vector3 result = ThirdPersonCamera.ClampToClearSpace(pivot, desired, Radius, Mask);

            Assert.Greater(result.z, -0.1f, "an overlapping start must not let the camera tunnel through");
        }
    }
}
