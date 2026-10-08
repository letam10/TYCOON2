using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityLayoutTests
    {
        [Test]
        public void GroundAreaIsDoubledAndDistrictsAreSeparated()
        {
            Assert.That(CityDistricts.GroundWidth * CityDistricts.GroundDepth, Is.EqualTo(200 * 136 * 2).Within(.1));
            Assert.That(CityDistricts.Footprint("farm").Intersects(CityDistricts.Footprint("restaurant")), Is.False);
            Assert.That(CityDistricts.Footprint("processing").Intersects(CityDistricts.Footprint("supermarket")), Is.False);
            Assert.That(CityDistricts.WorldBounds.Contains(CityDistricts.PlayerSpawn), Is.True);
        }

        [Test]
        public void AllTruckRouteSegmentsStayOnDefinedStreets()
        {
            string[] ids = { "storage_farm", "storage_farm_shop", "storage_processing",
                "storage_supermarket", "storage_bakery", "storage_restaurant" };
            var roads = CityStreetNetwork.Bounds().ToArray();
            foreach (string from in ids)
                foreach (string to in ids.Where(x => x != from))
                {
                    var path = TruckRoutes.Path(from, to);
                    for (int i = 1; i < path.Count; i++)
                    {
                        var a = new Vector3(path[i - 1].x, 0, path[i - 1].z);
                        var b = new Vector3(path[i].x, 0, path[i].z);
                        for (int n = 0; n <= 20; n++)
                            Assert.That(roads.Any(x => x.Contains(Vector3.Lerp(a, b, n / 20f))), Is.True,
                                from + " -> " + to + " segment " + i);
                    }
                }
        }

        [Test]
        public void MigrationMovesActorsOnceAndPreservesAssets()
        {
            var state = new TransactionState { layoutRevision = 1, cashInHand = 123, cashInSafe = 456 };
            state.stacks.Add(new ItemStackState { id = "kept", item = "carrot", owner = "player", quantity = 7 });
            var data = new SaveData { transactionState = state, playerX = -15.5f, playerZ = -3.5f };
            data.customers.Add(new CustomerSave { receipt = 5, shop = "farm", x = -13, z = 0 });
            data.diners.Add(new DinerSave { receipt = 8, x = -20, z = 39 });
            data.workers.Add(new WorkerSave { id = "kept-worker", x = -20, z = 39 });
            Assert.That(TownLayout.Migrate(data), Is.True);
            Assert.That(data.playerX, Is.EqualTo(CityDistricts.PlayerSpawn.x));
            Assert.That(data.playerZ, Is.EqualTo(CityDistricts.PlayerSpawn.z));
            Assert.That(data.customers[0].x, Is.EqualTo(-46));
            Assert.That(data.customers[0].exitX, Is.EqualTo(143));
            Assert.That(data.diners[0].x, Is.EqualTo(-52));
            Assert.That(data.workers[0].x, Is.EqualTo(-52));
            Assert.That(state.stacks.Single().quantity, Is.EqualTo(7));
            Assert.That(state.cashInHand, Is.EqualTo(123));
            Assert.That(state.cashInSafe, Is.EqualTo(456));
            Assert.That(TownLayout.Migrate(data), Is.False);
            Assert.That(data.playerX, Is.EqualTo(CityDistricts.PlayerSpawn.x));
        }
    }
}
