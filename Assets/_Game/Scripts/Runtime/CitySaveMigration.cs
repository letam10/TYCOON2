using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public static class CitySaveMigration
    {
        public static void Apply(SaveData data)
        {
            Vector3? truckDelta = MigrateTruck(data.transactionState);
            Vector3 playerDelta = PositionOffset(new(data.playerX, 0, data.playerZ));
            data.playerX += playerDelta.x;
            data.playerZ += playerDelta.z;
            foreach (var customer in data.customers)
            {
                Vector3 delta = CityDistricts.Offset(customer.shop);
                customer.x += delta.x;
                customer.z += delta.z;
                var exit = CityDistricts.CustomerSpawn((int)(customer.receipt % 3));
                customer.exitX = exit.x;
                customer.exitZ = exit.z;
            }
            foreach (var diner in data.diners)
            {
                var delta = CityDistricts.Offset("restaurant");
                diner.x += delta.x;
                diner.z += delta.z;
            }
            foreach (var worker in data.workers)
            {
                var delta = CityWorkerMigration.Offset(data, worker, truckDelta);
                worker.x += delta.x;
                worker.z += delta.z;
            }
            var state = data.transactionState;
            if (state == null) return;
            state.layoutRevision = CityDistricts.Revision;
            foreach (var owner in state.owners)
            {
                if (owner.customer?.receipt > 0)
                {
                    var customer = data.customers.Find(x => x.receipt == owner.customer.receipt);
                    if (customer != null) owner.customer = TransactionCore.Copy(customer);
                }
                if (!string.IsNullOrEmpty(owner.worker?.id))
                {
                    var worker = data.workers.Find(x => x.id == owner.worker.id);
                    if (worker != null) owner.worker = TransactionCore.Copy(worker);
                }
                if (owner.diner?.receipt > 0)
                {
                    var diner = data.diners.Find(x => x.receipt == owner.diner.receipt);
                    if (diner != null) owner.diner = TransactionCore.Copy(diner);
                }
            }
        }

        static Vector3? MigrateTruck(TransactionState state)
        {
            var truck = state?.truck;
            if (truck?.phase != "Travelling" || truck.path.Count < 2) return null;
            Vector3 previous = TruckRoutes.Position(truck);
            double oldLength = truck.distance > 0 ? truck.distance : Length(truck.path);
            double fraction = oldLength > 0 ? truck.travelled / oldLength : 0;
            string destination = !string.IsNullOrEmpty(truck.tripTarget) ? truck.tripTarget :
                truck.current != truck.source ? truck.source : truck.destination;
            truck.path = TruckRoutes.Path(truck.current, destination);
            // TickTruck dùng distance để kết thúc chuyến; phải đổi cùng travelled.
            truck.distance = Length(truck.path);
            truck.travelled = System.Math.Clamp(fraction, 0, 1) * truck.distance;
            return TruckRoutes.Position(truck) - previous;
        }

        public static Vector3 PositionOffset(Vector3 point)
        {
            if (point.x < -26 && point.z >= 8 && point.z <= 44) return CityDistricts.Offset("farm");
            string[] areas = { "farm", "farm_shop", "processing", "supermarket", "bakery", "restaurant" };
            Vector3[] centers =
            {
                new(-15, 0, 15), new(7, 0, 6), new(36, 0, 7),
                new(40, 0, 33), new(11, 0, 38), new(-20, 0, 39)
            };
            int nearest = Enumerable.Range(0, areas.Length)
                .OrderBy(i => (centers[i] - point).sqrMagnitude).First();
            return CityDistricts.Offset(areas[nearest]);
        }

        static double Length(System.Collections.Generic.List<RoutePoint> points)
        {
            double total = 0;
            for (int i = 1; i < points.Count; i++)
            {
                double x = points[i].x - points[i - 1].x;
                double z = points[i].z - points[i - 1].z;
                total += System.Math.Sqrt(x * x + z * z);
            }
            return total;
        }
    }
}
