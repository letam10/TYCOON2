using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed partial class CityPlaySession
    {
        [Serializable]
        sealed class NavigationReport
        {
            public int checkedPoints;
            public string[] blocked;
        }

        void CheckCityNavigation()
        {
            var blocked = new List<string>();
            int count = 0;
            var path = new NavMeshPath();
            foreach (var station in game.Stations.Where(x => x is not StationZone and not ConveyorStation))
            {
                Point(station.Id, Approach(station));
                if (station is PurchasePad or SafeStation) continue;
                Point(station.Id + ":work", station.WorkPoint);
                Point(station.Id + ":wait", station.WaitingPoint);
                if (station is TableStation table) Point(station.Id + ":seat", table.Seat);
                if (station is not CheckoutStation checkout) continue;
                Point(station.Id + ":cash", checkout.CollectionPoint);
                for (int index = 0; index < 15; index++)
                    Point(station.Id + ":queue:" + index, checkout.QueuePoint(index));
            }
            foreach (var safe in game.Stations.OfType<SafeStation>())
            {
                Point(safe.Id + ":deposit", safe.Deposit.Center);
                Point(safe.Id + ":withdraw", safe.Withdraw.Center);
            }
            File.WriteAllText(Path.Combine(game.QaDirectory, "city-navigation.json"),
                JsonUtility.ToJson(new NavigationReport { checkedPoints = count, blocked = blocked.ToArray() }, true));
            Check(blocked.Count == 0, "Đường tới " + count + " điểm tương tác và hàng chờ");

            void Point(string label, Vector3 point)
            {
                count++;
                bool reachable = NavMesh.SamplePosition(point, out var hit, 1, NavMesh.AllAreas) &&
                    NavMesh.CalculatePath(game.Player.transform.position, hit.position, NavMesh.AllAreas, path) &&
                    path.status == NavMeshPathStatus.PathComplete;
                if (!reachable) blocked.Add(label + " " + point);
            }
        }

        static Vector3 Approach(Station station) => station switch
        {
            PurchasePad => station.transform.position,
            CargoDock => station.transform.position + Vector3.left * 1.7f,
            ShelfStation => station.transform.position + Vector3.left * 1.4f,
            ProductionStation producer when !producer.Animal => station.transform.position + Vector3.back * 2.6f,
            CheckoutStation => station.transform.position + new Vector3(-1.7f, 0, -.3f),
            _ => station.transform.position + Vector3.back *
                (station is MachineStation or TableStation or ProductionStation ? 1.65f : 2)
        };
    }
}
