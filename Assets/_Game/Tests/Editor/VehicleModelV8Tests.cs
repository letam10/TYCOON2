using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class VehicleModelV8Tests
    {
        readonly List<GameObject> roots = new();

        [TearDown]
        public void Clear()
        {
            foreach (var root in roots)
                if (root) Object.DestroyImmediate(root);
            roots.Clear();
        }

        [Test]
        public void EveryKindHasDistinctGeometryAndIndependentAnimatedWheels()
        {
            var signatures = new HashSet<string>();
            foreach (CityVehicleKind kind in Enum.GetValues(typeof(CityVehicleKind)))
            {
                var model = Build(kind);
                var rig = model.GetComponent<CityVehicleRig>();
                Assert.That(rig.Kind, Is.EqualTo(kind));
                Assert.That(model.localScale, Is.EqualTo(Vector3.one));
                Assert.That(rig.Wheels.Length, Is.GreaterThanOrEqualTo(4));
                Assert.That(rig.RightDoors.Length, Is.GreaterThanOrEqualTo(1));
                Assert.That(rig.WheelRadius, Is.GreaterThan(.3f));
                Assert.That(model.GetComponentsInChildren<MeshRenderer>().Length, Is.LessThanOrEqualTo(32));
                var hull = model.GetComponentsInChildren<MeshRenderer>().Where(r => r.transform.parent == model);
                Assert.That(hull.Count(r => !rig.Wheels.Contains(r.transform) &&
                    !rig.LeftDoors.Contains(r.transform) && !rig.RightDoors.Contains(r.transform)),
                    Is.LessThanOrEqualTo(12));
                var bounds = Bounds(model);
                int vertices = model.GetComponentsInChildren<MeshFilter>().Sum(m => m.sharedMesh.vertexCount);
                Assert.That(signatures.Add(bounds.size.ToString("F3") + ":" + vertices), Is.True, kind.ToString());
                var before = rig.Wheels.Select(w => w.localRotation).ToArray();
                rig.SetTravel(4, .2f);
                for (int i = 0; i < before.Length; i++)
                    Assert.That(Quaternion.Angle(before[i], rig.Wheels[i].localRotation), Is.GreaterThan(1));
                foreach (var obstacle in model.GetComponentsInChildren<NavMeshObstacle>())
                {
                    Assert.That(obstacle.carving, Is.True);
                    Assert.That(obstacle.carveOnlyStationary, Is.True);
                }
            }
        }

        [TestCase(0, CityVehicleKind.Bus, 8.6f, 2.45f, .54f, 2.6f)]
        [TestCase(1, CityVehicleKind.Coach, 8.6f, 2.45f, .54f, 2.6f)]
        [TestCase(2, CityVehicleKind.Taxi, 4.2f, 1.85f, .2f, -.35f)]
        public void PublicTransitAttachesRigToProvidedRootAndKeepsPassengerFootprint(int kind,
            CityVehicleKind expected, float length, float width, float floor, float doorZ)
        {
            var root = new GameObject("TransitGeometryTest");
            roots.Add(root);
            var door = CityPublicTransportModel.Build(kind, root.transform);
            var rig = root.GetComponent<CityVehicleRig>();
            Assert.That(rig.Kind, Is.EqualTo(expected));
            Assert.That(door, Is.SameAs(rig.RightDoors[0]));
            Assert.That(door.parent, Is.EqualTo(root.transform));
            Assert.That(root.GetComponent<BoxCollider>().size.x, Is.EqualTo(width).Within(.001f));
            Assert.That(root.GetComponent<BoxCollider>().size.z, Is.EqualTo(length).Within(.001f));
            Assert.That(rig.DoorInsideLocal.x, Is.GreaterThan(0));
            Assert.That(rig.DoorInsideLocal.y, Is.EqualTo(floor).Within(.001f));
            Assert.That(rig.DoorInsideLocal.z, Is.EqualTo(doorZ).Within(.001f));
            Assert.That(Bounds(root.transform).size.z, Is.InRange(length - .15f, length + .3f));
        }

        [TestCase(CityVehicleKind.Taxi)]
        [TestCase(CityVehicleKind.Bus)]
        [TestCase(CityVehicleKind.Train)]
        public void DoorAnimationClampsAndClosesExactlyWithoutChangingHull(CityVehicleKind kind)
        {
            var model = Build(kind);
            var rig = model.GetComponent<CityVehicleRig>();
            var door = rig.RightDoors[0];
            var closedPosition = door.localPosition;
            var closedRotation = door.localRotation;
            var meshes = model.GetComponentsInChildren<MeshFilter>().Select(m => m.sharedMesh).ToArray();
            rig.SetDoorOpen(1);
            var openPosition = door.localPosition;
            var openRotation = door.localRotation;
            if (kind == CityVehicleKind.Taxi)
                Assert.That(Quaternion.Angle(closedRotation, openRotation), Is.EqualTo(68).Within(.01f));
            else
                Assert.That(Vector3.Distance(closedPosition, openPosition), Is.GreaterThan(1));
            rig.SetDoorOpen(3);
            Assert.That(door.localPosition, Is.EqualTo(openPosition));
            Assert.That(door.localRotation, Is.EqualTo(openRotation));
            rig.SetDoorOpen(-1);
            Assert.That(door.localPosition, Is.EqualTo(closedPosition));
            Assert.That(Quaternion.Angle(door.localRotation, closedRotation), Is.LessThan(.001f));
            Assert.That(model.GetComponentsInChildren<MeshFilter>().Select(m => m.sharedMesh), Is.EqualTo(meshes));
            if (kind != CityVehicleKind.Train) return;
            rig.SetDoorOpen(1, -1);
            Assert.That(door.localPosition, Is.EqualTo(closedPosition));
            Assert.That(rig.LeftDoors[0].localPosition.z, Is.LessThan(-10.5f));
        }

        [Test]
        public void RailHasSeparateCoachCollidersAndFitsStationEnvelope()
        {
            var model = Build(CityVehicleKind.Train);
            var rig = model.GetComponent<CityVehicleRig>();
            var bounds = Bounds(model);
            Assert.That(bounds.size.z, Is.LessThanOrEqualTo(32));
            Assert.That(bounds.size.x, Is.LessThanOrEqualTo(2.801f));
            Assert.That(bounds.max.y, Is.LessThanOrEqualTo(3.6f));
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(-.001f));
            Assert.That(rig.DoorInsideLocal, Is.EqualTo(new Vector3(1.3f, .6f, -10)));
            Assert.That(rig.WheelRadius, Is.EqualTo(.38f));
            Assert.That(rig.Wheels, Has.Length.EqualTo(12));
            Assert.That(rig.RightDoors, Has.Length.EqualTo(2));
            Assert.That(rig.LeftDoors, Has.Length.EqualTo(2));
            Assert.That(model.GetComponent<BoxCollider>(), Is.Null);
            var colliders = model.GetComponentsInChildren<BoxCollider>();
            Assert.That(colliders, Has.Length.EqualTo(3));
            Assert.That(colliders.All(c => c.size.z <= 9.4f), Is.True);
            Assert.That(colliders.Select(c => c.transform.localPosition.z).Distinct().Count(), Is.EqualTo(3));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SharedMaterialsSurviveDestroyWhileOwnedMeshesAreReleased(bool deactivateBeforeDestroy)
        {
            var first = Build(CityVehicleKind.Sedan);
            var second = Build(CityVehicleKind.Sedan);
            var firstMeshes = first.GetComponentsInChildren<MeshFilter>().Select(m => m.sharedMesh).ToArray();
            var secondMeshes = second.GetComponentsInChildren<MeshFilter>().Select(m => m.sharedMesh).ToArray();
            var firstMaterials = first.GetComponentsInChildren<MeshRenderer>()
                .SelectMany(r => r.sharedMaterials).ToArray();
            var secondMaterials = second.GetComponentsInChildren<MeshRenderer>()
                .SelectMany(r => r.sharedMaterials).ToArray();
            Assert.That(firstMaterials, Is.EqualTo(secondMaterials));
            Assert.That(firstMeshes.Intersect(secondMeshes), Is.Empty);
            if (deactivateBeforeDestroy)
            {
                first.gameObject.SetActive(false);
                Assert.That(firstMeshes.All(m => m), Is.True, "Pooling must retain meshes until destruction");
            }
            Object.DestroyImmediate(first.gameObject);
            Assert.That(firstMeshes.All(m => !m), Is.True);
            Assert.That(secondMeshes.All(m => m), Is.True);
            Assert.That(firstMaterials.All(m => m), Is.True);
            Assert.That(secondMaterials.All(m => m), Is.True);
        }

        [Test]
        public void LegacyVanWrapperBuildsDeliveryTruck()
        {
            var root = CityVehicleModel.Build("#437699", null, true);
            roots.Add(root.gameObject);
            Assert.That(root.GetComponent<CityVehicleRig>().Kind, Is.EqualTo(CityVehicleKind.SmallTruck));
        }

        [TestCase(CityVehicleKind.Sedan, 1.85f, .88f, 1.49f)]
        [TestCase(CityVehicleKind.Taxi, 1.85f, .88f, 1.49f)]
        [TestCase(CityVehicleKind.Luxury, 1.94f, .88f, 1.55f)]
        [TestCase(CityVehicleKind.Sports, 1.94f, .69f, 1.15f)]
        public void CarDoorsMeetBodyShoulderAndRoofWithoutStaticPassageFill(CityVehicleKind kind,
            float width, float belt, float roof)
        {
            var root = Build(kind);
            var rig = root.GetComponent<CityVehicleRig>();
            var hull = root.GetComponentsInChildren<MeshFilter>()
                .Where(f => f.name.StartsWith("SculptedCarHull")).ToArray();
            var hullPoints = hull.SelectMany(f => f.sharedMesh.vertices.Select(f.transform.TransformPoint)).ToArray();
            var opaquePoints = hull.Where(f =>
                f.GetComponent<MeshRenderer>().sharedMaterial != CityVehicleSurface.Glass)
                .SelectMany(f => f.sharedMesh.vertices.Select(f.transform.TransformPoint)).ToArray();
            float half = width * .5f;
            Assert.That(opaquePoints.Any(p => Mathf.Abs(p.x) > half * .8f && p.z > -.85f && p.z < .15f &&
                p.y > .34f && p.y < roof - .08f), Is.False, "No static plate may conceal the doorway");
            foreach (int side in new[] { -1, 1 })
            {
                var door = side > 0 ? rig.RightDoors[0] : rig.LeftDoors[0];
                var points = door.GetComponent<MeshFilter>().sharedMesh.vertices.Select(door.TransformPoint).ToArray();
                foreach (float z in new[] { -.88f, .18f })
                {
                    for (int band = 0; band <= 4; band++)
                    {
                        float t = band / 4f;
                        var edge = new Vector3(side * half * (.955f + .045f * Mathf.Sin(t * Mathf.PI * .5f)),
                            Mathf.Lerp(.3f, belt, t), z);
                        AssertVertex(points, edge, "Door paint edge");
                        AssertVertex(hullPoints, edge, "Matching body edge");
                    }
                    var sill = new Vector3(side * half * .9f, belt + .035f, z);
                    var header = new Vector3(side * half * .77f, roof - .045f, z);
                    AssertVertex(points, sill, "Door glass lower edge");
                    AssertVertex(hullPoints, sill, "Adjacent glass lower edge");
                    AssertVertex(points, header, "Door glass roof edge");
                    AssertVertex(hullPoints, header, "Adjacent glass roof edge");
                }
                var rear = new Vector3(side * half, belt, -.88f);
                var localRear = door.InverseTransformPoint(rear);
                rig.SetDoorOpen(1, side);
                Assert.That(side * door.TransformPoint(localRear).x, Is.GreaterThan(half + .4f));
                rig.SetDoorOpen(0, side);
                Assert.That(Vector3.Distance(door.TransformPoint(localRear), rear), Is.LessThan(.00001f));
            }
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                Assert.That(filter.sharedMesh.vertices.All(Finite), Is.True, filter.name);
                Assert.That(filter.sharedMesh.normals.All(Finite), Is.True, filter.name);
            }
        }

        static void AssertVertex(Vector3[] points, Vector3 target, string label)
        {
            Assert.That(points.Min(p => Vector3.Distance(p, target)), Is.LessThan(.00001f), label + " " + target);
        }

        static bool Finite(Vector3 point) =>
            !float.IsNaN(point.x) && !float.IsInfinity(point.x) &&
            !float.IsNaN(point.y) && !float.IsInfinity(point.y) &&
            !float.IsNaN(point.z) && !float.IsInfinity(point.z);

        Transform Build(CityVehicleKind kind)
        {
            var model = CityVehicleModel.Build(kind, "#437699", null);
            roots.Add(model.gameObject);
            return model;
        }

        static Bounds Bounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            var result = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) result.Encapsulate(renderer.bounds);
            return result;
        }
    }
}
