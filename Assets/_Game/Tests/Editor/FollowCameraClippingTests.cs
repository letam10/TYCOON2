using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class FollowCameraClippingTests
    {
        [Test]
        public void SnapPullsCameraBeforeBuildingWithoutChangingPitchOrYaw()
        {
            var root = new GameObject("FollowCameraClippingFixture");
            var cameraObject = new GameObject("Camera");
            cameraObject.transform.SetParent(root.transform);
            var camera = cameraObject.AddComponent<Camera>();
            var follow = cameraObject.AddComponent<FollowCamera>();
            var target = new GameObject("Target");
            target.transform.SetParent(root.transform);
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "BuildingBlocker";
            blocker.transform.SetParent(root.transform);
            blocker.transform.localScale = new Vector3(2f, 2f, 2f);
            var blockerRenderer = blocker.GetComponent<Renderer>();

            follow.Target = target.transform;
            follow.Pitch = 55f;
            follow.Yaw = 40f;
            follow.Distance = 16f;
            follow.CollisionRadius = .25f;
            follow.CollisionPadding = .25f;
            follow.MinimumClearDistance = 1.5f;
            follow.CollisionMask = ~0;
            follow.OcclusionMask = ~0;
            follow.HideOccluders = false;

            Quaternion expectedRotation = Quaternion.Euler(follow.Pitch, follow.Yaw, 0f);
            Vector3 focus = target.transform.position + follow.FocusOffset;
            Vector3 cameraDirection = -(expectedRotation * Vector3.forward);
            blocker.transform.position = focus + cameraDirection * 8f;
            Physics.SyncTransforms();

            try
            {
                follow.Snap();

                float resolvedDistance = Vector3.Distance(focus, cameraObject.transform.position);
                Assert.That(resolvedDistance, Is.LessThan(follow.Distance - 1f));
                Assert.That(Quaternion.Angle(cameraObject.transform.rotation, expectedRotation),
                    Is.LessThan(.001f));

                follow.CollisionMask = 0;
                follow.HideOccluders = true;
                follow.Snap();
                Assert.That(blockerRenderer.enabled, Is.False);

                follow.HideOccluders = false;
                follow.Snap();
                Assert.That(blockerRenderer.enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
