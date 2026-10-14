using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityCurbsideBoardingTests
    {
        GameObject root;

        [SetUp]
        public void CreateRoot()
        {
            root = new GameObject("CurbsideBoardingTest");
            root.SetActive(false);
        }

        [TearDown]
        public void ClearRoot() => Object.DestroyImmediate(root);

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(4)]
        [TestCase(5)]
        public void PublicVehicleDoorFacesSidewalkInsteadOfOppositeTraffic(int stop)
        {
            var vehicle = root.AddComponent<CityPublicTransportVehicle>();
            vehicle.Initialize(null, null, 0, stop);
            AssertCurbside(vehicle.transform.position, vehicle.transform.forward, vehicle.DoorOutside);
        }

        [Test]
        public void EveryRoadVehicleUsesCurbsideDoorsAtAllFourStops()
        {
            var traffic = root.AddComponent<CityTraffic>();
            typeof(CityTraffic).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(traffic, null);
            var cars = (IEnumerable)typeof(CityTraffic)
                .GetField("cars", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(traffic);
            int vehicles = 0;
            int stops = 0;
            foreach (var car in cars)
            {
                var type = car.GetType();
                var model = (Transform)type.GetField("Root").GetValue(car);
                var route = (Vector3[])type.GetField("Route").GetValue(car);
                var service = model.GetComponent<CityRoadVehiclePassengers>();
                vehicles++;
                for (int index = 0; index < route.Length; index++)
                {
                    Vector3 point = route[index];
                    if (Mathf.Abs(point.x + 22) > .1f && Mathf.Abs(point.x - 70) > .1f) continue;
                    model.position = point;
                    model.rotation = Quaternion.LookRotation(route[(index + 1) % route.Length] - point);
                    AssertCurbside(point, model.forward, service.DoorOutside);
                    stops++;
                }
            }
            Assert.That(vehicles, Is.EqualTo(6));
            Assert.That(stops, Is.EqualTo(24));
        }

        static void AssertCurbside(Vector3 position, Vector3 forward, Vector3 exit)
        {
            float roadCenter = position.z < 0 ? -45 : 29;
            float laneSide = Mathf.Sign(position.z - roadCenter);
            Assert.That((exit.z - roadCenter) * laneSide, Is.GreaterThan(4),
                "Door must end beyond the road pavement edge, not inside opposite traffic.");
            Assert.That(Vector3.Dot(exit - position, Vector3.Cross(Vector3.up, forward)),
                Is.GreaterThan(2), "Passenger uses the real right-hand door.");
        }
    }
}
