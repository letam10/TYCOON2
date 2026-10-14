using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed partial class TruckLogistics
    {
        CityVehicleRig cargoRig;
        Vector3 previousTruckPosition;
        bool truckMotionInitialized;

        Transform BuildTruck()
        {
            var root = new GameObject("CargoTruck").transform;
            root.SetParent(transform, false);
            var collision = root.gameObject.AddComponent<BoxCollider>();
            collision.center = new(0, 1, 0);
            collision.size = new(2.1f, 2, 4.5f);
            var obstacle = root.gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = collision.center;
            obstacle.size = collision.size;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
            var model = new GameObject("DetailedOpenCargoBody").transform;
            model.SetParent(root, false);
            // Giữ kích thước bến cũ; thùng hở giúp nhìn rõ từng kiện đã chất.
            model.localScale = Vector3.one * .8f;
            cargoRig = model.gameObject.AddComponent<CityVehicleRig>();
            cargoRig.Kind = CityVehicleKind.SmallTruck;
            CityVehicleGeometryTrucks.Build(model, cargoRig, "#79A7A0", true);
            foreach (var collider in model.GetComponentsInChildren<Collider>()) collider.enabled = false;
            foreach (var navObstacle in model.GetComponentsInChildren<NavMeshObstacle>()) navObstacle.enabled = false;
            return root;
        }

        void AnimateTruckWheels()
        {
            float delta = Time.deltaTime;
            if (cargoRig && delta > 0 && truckMotionInitialized)
            {
                float speed = Vector3.Dot(Vehicle.position - previousTruckPosition, Vehicle.forward) / delta;
                cargoRig.SetTravel(speed / .8f, delta);
            }
            previousTruckPosition = Vehicle.position;
            truckMotionInitialized = true;
        }
    }
}
