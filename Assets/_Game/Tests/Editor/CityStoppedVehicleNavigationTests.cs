using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace Tycoon.Tests
{
    public sealed class CityStoppedVehicleNavigationTests : CityPedestrianPlayModeFixture
    {
        GameObject floor;
        Transform vehicle;

        protected override void CreateWorld()
        {
            floor = new GameObject("StoppedVehicleFloor", typeof(BoxCollider));
            floor.transform.position = Vector3.down * .1f;
            floor.GetComponent<BoxCollider>().size = new(40, .2f, 40);
            var surface = floor.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            Physics.SyncTransforms();
            surface.BuildNavMesh();
            vehicle = CityVehicleModel.Build(CityVehicleKind.CargoTruck, "#63899E", null);
        }

        protected override void ClearWorld()
        {
            if (vehicle) Object.DestroyImmediate(vehicle.gameObject);
            if (floor) Object.DestroyImmediate(floor);
        }

        [UnityTest]
        public IEnumerator PedestrianPathGoesAroundStationaryVehicleInsteadOfThroughItsHull()
        {
            yield return new WaitForSeconds(1);
            var path = new NavMeshPath();
            Assert.That(NavMesh.CalculatePath(new(-8, 0, 0), new(8, 0, 0), NavMesh.AllAreas, path), Is.True);
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
            Assert.That(path.corners.Any(x => Mathf.Abs(x.z) > 4.1f), Is.True,
                "A complete pedestrian path must avoid the stopped truck, not hit its collider forever.");
        }
    }
}
