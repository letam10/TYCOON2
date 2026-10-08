using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityMigrationTests
    {
        static SaveData Seed() => new() { transactionState = new TransactionState { layoutRevision = 1 } };

        static WorkerSave AddWorker(SaveData data, string upgrade, float x = -2, float z = 30)
        {
            var worker = new WorkerSave { id = upgrade + ":0", upgrade = upgrade, x = x, z = z };
            data.workers.Add(worker);
            data.transactionState.owners.Add(new OwnerState
            {
                id = "worker:" + worker.id,
                actor = "worker:" + worker.id,
                kind = OwnerKind.Worker,
                worker = JsonUtility.FromJson<WorkerSave>(JsonUtility.ToJson(worker))
            });
            return worker;
        }

        static void AssertPosition(WorkerSave worker, float x, float z)
        {
            Assert.That(worker.x, Is.EqualTo(x).Within(.0001));
            Assert.That(worker.z, Is.EqualTo(z).Within(.0001));
        }

        [TestCase("farm", -2, 30)]
        [TestCase("farm_shop", 7, 13)]
        [TestCase("processing", 48, 2)]
        [TestCase("supermarket", 27, 32)]
        [TestCase("bakery", 3, 52)]
        [TestCase("restaurant", -34.7f, 44)]
        public void LoaderFollowsItsOwnDockEvenWhenTruckIsElsewhere(string area, float x, float z)
        {
            var data = Seed();
            data.transactionState.truck = new TruckRuntimeState { current = "storage_processing" };
            var worker = AddWorker(data, "loader_" + area, x, z);
            data.crews.Add(new CrewState { id = worker.upgrade, role = "Loader", area = area });

            Assert.That(TownLayout.Migrate(data), Is.True);

            var expected = TruckRoutes.Dock("storage_" + area);
            AssertPosition(worker, expected.x, expected.z);
            Assert.That(JsonUtility.ToJson(data.transactionState.owners[0].worker),
                Is.EqualTo(JsonUtility.ToJson(worker)));
            Assert.That(TownLayout.Migrate(data), Is.False);
            AssertPosition(worker, expected.x, expected.z);
        }

        [TestCase("Cashier", "farm_shop")]
        [TestCase("Repairer", "processing")]
        [TestCase("AnimalWorker", "farm")]
        [TestCase("Waiter", "restaurant")]
        public void IdleWorkerUsesAuthoritativeCrewArea(string role, string area)
        {
            var data = Seed();
            var worker = AddWorker(data, "fixture_crew");
            data.crews.Add(new CrewState { id = worker.upgrade, role = role, area = "bakery" });
            data.transactionState.crews.Add(new CrewState { id = worker.upgrade, role = role, area = area });
            worker.source = "storage_bakery";
            worker.destination = "storage_bakery";

            TownLayout.Migrate(data);

            Vector3 delta = CityDistricts.Offset(area);
            AssertPosition(worker, -2 + delta.x, 30 + delta.z);
        }

        [TestCase("loader_farm", "farm")]
        [TestCase("processor", "processing")]
        public void MissingCrewProjectionUsesUpgradeDefinition(string upgrade, string area)
        {
            var data = Seed();
            var worker = AddWorker(data, upgrade);

            TownLayout.Migrate(data);

            Vector3 delta = CityDistricts.Offset(area);
            AssertPosition(worker, -2 + delta.x, 30 + delta.z);
        }

        [TestCase(1, "farm")]
        [TestCase(2, "processing")]
        [TestCase(0, "restaurant")]
        public void ActiveWorkerUsesPickupDeliveryOrWorkingStation(int phase, string area)
        {
            var data = Seed();
            var worker = AddWorker(data, "processor");
            worker.phase = phase;
            worker.source = "storage_farm";
            worker.destination = "machine_mill_input";
            worker.working = "table_1";
            data.transactionState.stations.Add(new StationRuntimeState
            {
                id = "machine_mill", input = "machine_mill_input", output = "machine_mill", area = "processing"
            });
            data.transactionState.stations.Add(new StationRuntimeState { id = "table_1", area = "restaurant" });

            TownLayout.Migrate(data);

            Vector3 delta = CityDistricts.Offset(area);
            AssertPosition(worker, -2 + delta.x, 30 + delta.z);
        }

        [Test]
        public void MigrationPreservesWorkerJobGoodsMoneyAndTransactionIdentity()
        {
            var data = Seed();
            var state = data.transactionState;
            var worker = AddWorker(data, "processor");
            worker.phase = 2;
            worker.destination = "storage_processing";
            worker.source = "storage_farm";
            worker.item = "wheat";
            worker.count = 3;
            worker.deliveries = 8;
            worker.tableReceipt = 23;
            worker.reservation = "reservation:kept";
            worker.carry.Add(new ItemAmount("wheat", 3));
            state.owners[0].worker = JsonUtility.FromJson<WorkerSave>(JsonUtility.ToJson(worker));
            state.money = 789;
            state.cashInHand = 123;
            state.cashInSafe = 456;
            state.revision = 37;
            state.stacks.Add(new ItemStackState
            {
                id = "kept", owner = state.owners[0].id, item = "wheat", quantity = 3
            });
            state.reservations.Add(new ReservationState
            {
                id = worker.reservation, holder = state.owners[0].actor, source = state.owners[0].id,
                destination = worker.destination, item = "wheat", quantity = 3, expiresAt = 1000,
                allocations = new List<StackAllocation> { new() { stack = "kept", quantity = 3 } }
            });
            state.receipts.Add(new TransactionReceipt
            {
                id = "receipt:kept", key = "command:kept", effectId = "effect:kept", revision = 37, amount = 3
            });
            state.outbox.Add(new TransactionEvent { id = "event:kept", receiptId = "receipt:kept" });
            string before = JsonUtility.ToJson(state);
            string workerBefore = JsonUtility.ToJson(worker);

            TownLayout.Migrate(data);

            AssertPosition(worker, 29, 10);
            Assert.That(JsonUtility.ToJson(state.owners[0].worker), Is.EqualTo(JsonUtility.ToJson(worker)));
            var restored = JsonUtility.FromJson<TransactionState>(JsonUtility.ToJson(state));
            restored.layoutRevision = 1;
            restored.owners[0].worker.x = -2;
            restored.owners[0].worker.z = 30;
            Assert.That(JsonUtility.ToJson(restored), Is.EqualTo(before));
            var restoredWorker = JsonUtility.FromJson<WorkerSave>(JsonUtility.ToJson(worker));
            restoredWorker.x = -2;
            restoredWorker.z = 30;
            Assert.That(JsonUtility.ToJson(restoredWorker), Is.EqualTo(workerBefore));
        }

        [TestCase(false, 0d)]
        [TestCase(false, .25d)]
        [TestCase(false, .75d)]
        [TestCase(true, .5d)]
        [TestCase(true, 1d)]
        public void TravellingTruckKeepsProgressAndDriverAcrossOutboundAndReturnTrips(bool returning, double fraction)
        {
            var data = Seed();
            var truck = new TruckRuntimeState
            {
                current = returning ? "storage_processing" : "storage_farm",
                source = "storage_farm", destination = "storage_processing",
                tripTarget = returning ? "storage_farm" : "storage_processing",
                trip = "effect:dispatch-kept", phase = "Travelling", repeat = true, completedTrips = 4,
                path = new List<RoutePoint> { new(-2, 30), new(-2, 0), new(48, 0) },
                distance = 80, travelled = fraction * 80
            };
            data.transactionState.truck = truck;
            var previous = TruckRoutes.Position(truck);
            var driver = AddWorker(data, "truck_bundle", previous.x + .5f, previous.z + 1.5f);

            TownLayout.Migrate(data);

            var route = TruckRoutes.Path(truck.current, truck.tripTarget);
            Assert.That(JsonUtility.ToJson(new PathSnapshot { points = truck.path }),
                Is.EqualTo(JsonUtility.ToJson(new PathSnapshot { points = route })));
            Assert.That(truck.distance, Is.EqualTo(Length(route)).Within(.00001));
            Assert.That(truck.travelled / truck.distance, Is.EqualTo(fraction).Within(.00001));
            Assert.That(truck.travelled, Is.LessThanOrEqualTo(truck.distance));
            Assert.That(truck.trip, Is.EqualTo("effect:dispatch-kept"));
            Assert.That(truck.phase, Is.EqualTo("Travelling"));
            Assert.That(truck.repeat, Is.True);
            Assert.That(truck.completedTrips, Is.EqualTo(4));
            var current = TruckRoutes.Position(truck);
            AssertPosition(driver, current.x + .5f, current.z + 1.5f);
            Assert.That(JsonUtility.ToJson(data.transactionState.owners[0].worker),
                Is.EqualTo(JsonUtility.ToJson(driver)));
            string migrated = JsonUtility.ToJson(data);
            Assert.That(TownLayout.Migrate(data), Is.False);
            Assert.That(JsonUtility.ToJson(data), Is.EqualTo(migrated));
        }

        [Test]
        public void ParkedDriverUsesCurrentTruckDockInsteadOfItsProcessingCrewArea()
        {
            var data = Seed();
            data.transactionState.truck = new TruckRuntimeState { current = "storage_farm" };
            var driver = AddWorker(data, "truck_bundle");

            TownLayout.Migrate(data);

            AssertPosition(driver, -35, 10);
        }

        [Test]
        public void MigratedTruckCompletesOnlyAfterRemainingNewRouteDistance()
        {
            var state = TransactionCoreTests.Seed();
            state.layoutRevision = 1;
            foreach (string area in new[] { "farm", "processing" })
            {
                string id = "storage_" + area;
                state.owners.Add(new OwnerState
                {
                    id = id, actor = "simulation", location = area, kind = OwnerKind.Storage, capacity = 72
                });
                state.stations.Add(new StationRuntimeState
                {
                    id = id, kind = "storage", area = area, input = id, output = id
                });
            }
            state.truck = new TruckRuntimeState
            {
                current = "storage_farm", source = "storage_farm", destination = "storage_processing",
                tripTarget = "storage_processing", trip = "dispatch:kept", phase = "Travelling",
                path = new List<RoutePoint> { new(-2, 30), new(48, 30) }, distance = 50, travelled = 25
            };
            state.owners.Add(new OwnerState
            {
                id = state.truck.id, actor = "simulation", location = state.truck.id, kind = OwnerKind.Truck
            });
            TownLayout.Migrate(new SaveData { transactionState = state });
            var core = new TransactionCore(state, null, () => 100);
            double remaining = state.truck.distance - state.truck.travelled;
            var tick = new TransactionCommand
            {
                kind = TransactionKind.TickTruck, actor = "simulation", secondary = "dispatch:kept",
                key = "tick:almost", effectId = "effect:almost", expectedRevision = core.Revision,
                duration = (remaining - 1) / (4 * WorkforceRules.Capability)
            };
            core.Execute(tick);
            Assert.That(core.Snapshot().truck.phase, Is.EqualTo("Travelling"));
            tick.key = "tick:arrive";
            tick.effectId = "effect:arrive";
            tick.expectedRevision = core.Revision;
            tick.duration = .5;
            core.Execute(tick);
            Assert.That(core.Snapshot().truck.phase, Is.EqualTo("WaitingUnload"));
            Assert.That(core.Snapshot().truck.current, Is.EqualTo("storage_processing"));
            TransactionCore.Validate(core.Snapshot());
        }

        [Serializable]
        sealed class PathSnapshot
        {
            public List<RoutePoint> points;
        }

        static double Length(List<RoutePoint> path)
        {
            double length = 0;
            for (int i = 1; i < path.Count; i++)
            {
                double x = path[i].x - path[i - 1].x;
                double z = path[i].z - path[i - 1].z;
                length += Math.Sqrt(x * x + z * z);
            }
            return length;
        }
    }
}
