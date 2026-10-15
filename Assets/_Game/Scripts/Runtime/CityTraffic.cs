using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public sealed class CityTraffic : MonoBehaviour
    {
        enum BlockReason
        {
            None,
            Signal,
            Actor,
            Car,
            PublicTransport
        }

        sealed class Car
        {
            public Transform Root;
            public Vector3[] Route;
            public int Next;
            public float Speed;
            public float Velocity;
            public CityVehicleRig Rig;
            public CityRoadVehiclePassengers Passengers;
            public float BlockedSeconds;
            public bool IsBlocked;
            public BlockReason Reason;
            public bool DiagnosticReported;
        }

        const float DiagnosticThresholdSeconds = 3f;
        readonly List<Car> cars = new();
        GameSession game;
        CityLife life;
        CityPublicTransport publicTransport;
        CityTrafficSignalSystem signals;
        bool deadlockEpisodeActive;
        [System.NonSerialized] public int StuckVehicleCount;
        [System.NonSerialized] public int DeadlockCount;
        [System.NonSerialized] public string LastDiagnostic;
        public bool HasTrafficDeadlock => deadlockEpisodeActive;
        public int VehicleCount => cars.Count;

        void Start()
        {
            game = GameSession.Instance;
            life = GetComponent<CityLife>();
            publicTransport = GetComponentInChildren<CityPublicTransport>();
            signals = GetComponentInParent<CityTrafficSignalSystem>()
                ?? GetComponentInChildren<CityTrafficSignalSystem>();
            string[] colors = { "#6E9C98", "#C5B391", "#6A8BAD", "#A1ACAB", "#314A64", "#BD5148" };
            CityVehicleKind[] kinds = { CityVehicleKind.Sedan, CityVehicleKind.SmallTruck,
                CityVehicleKind.CargoTruck, CityVehicleKind.Tanker, CityVehicleKind.Luxury, CityVehicleKind.Sports };
            for (int i = 0; i < colors.Length; i++)
            {
                // Làn bên phải theo hướng xe chạy để cửa hành khách luôn hướng ra vỉa hè.
                float side = i % 2 == 0 ? -1.55f : 1.55f;
                Vector3[] route =
                {
                    new(-105 + side, 0, -45 + side), new(-22, 0, -45 + side), new(70, 0, -45 + side),
                    new(102 - side, 0, -45 + side), new(102 - side, 0, 29 - side),
                    new(70, 0, 29 - side), new(-22, 0, 29 - side), new(-105 + side, 0, 29 - side)
                };
                if (i % 2 == 1) System.Array.Reverse(route);
                route = CityRoadRoute.Round(route);
                var root = CityVehicleModel.Build(kinds[i], colors[i], transform);
                root.position = CityRoadRoute.Spawn(route, (i / 2) / 3f, out int next);
                root.rotation = Quaternion.LookRotation((route[next] - root.position).normalized);
                var rig = root.GetComponent<CityVehicleRig>();
                var passengerService = root.gameObject.AddComponent<CityRoadVehiclePassengers>();
                passengerService.Initialize(game, life, rig);
                cars.Add(new Car
                {
                    Root = root, Route = route, Next = next, Speed = 5.4f + i * .3f,
                    Rig = rig, Passengers = passengerService
                });
            }
        }

        void Update()
        {
            using var updateProbe = QaFrameProbe.Measure("CityTraffic.1.Update");

            if (!game || !game.CanSimulate || Time.deltaTime <= 0) return;
            for (int vehicleIndex = 0; vehicleIndex < cars.Count; vehicleIndex++)
            {
                var car = cars[vehicleIndex];
                if (car.Passengers.Tick(Time.deltaTime))
                {
                    ResetBlockedEpisode(car);
                    car.Velocity = 0;
                    if (car.Rig) car.Rig.SetTravel(0, Time.deltaTime);
                    continue;
                }
                Vector3 delta = car.Route[car.Next] - car.Root.position;
                if (delta.sqrMagnitude < .1f)
                {
                    if (Mathf.Abs(car.Route[car.Next].x + 22) < .1f ||
                        Mathf.Abs(car.Route[car.Next].x - 70) < .1f) car.Passengers.BeginStop();
                    car.Next = (car.Next + 1) % car.Route.Length;
                    ResetBlockedEpisode(car);
                    continue;
                }
                Vector3 forward = delta.normalized;
                float step = car.Velocity * Time.deltaTime;
                bool blockedByActor = PeopleAhead(car, forward, step);
                bool blockedByCar = CarAhead(car, step);
                bool blockedBySignal = signals && signals.ShouldStopVehicle(car.Root.position);
                bool blockedByPublicTransport = publicTransport &&
                    publicTransport.BlocksTraffic(car.Root.position, forward, step);
                bool blocked = blockedByActor || blockedByCar || blockedBySignal || blockedByPublicTransport;
                car.Velocity = Mathf.MoveTowards(car.Velocity, blocked ? 0 : car.Speed, 3.8f * Time.deltaTime);
                if (blocked)
                {
                    car.IsBlocked = true;
                    car.BlockedSeconds += Time.deltaTime;
                    car.Reason = blockedBySignal ? BlockReason.Signal :
                        blockedByActor ? BlockReason.Actor :
                        blockedByCar ? BlockReason.Car : BlockReason.PublicTransport;
                    car.Velocity = 0;
                    if (car.Rig) car.Rig.SetTravel(0, Time.deltaTime);
                    continue;
                }
                ResetBlockedEpisode(car);
                step = car.Velocity * Time.deltaTime;
                car.Root.position = Vector3.MoveTowards(car.Root.position, car.Route[car.Next], step);
                if (car.Rig) car.Rig.SetTravel(car.Velocity, Time.deltaTime);
                car.Root.rotation = Quaternion.RotateTowards(car.Root.rotation,
                    Quaternion.LookRotation(forward), 180 * Time.deltaTime);
            }
            UpdateDiagnostics();
        }

        void UpdateDiagnostics()
        {
            int blockedCars = 0;
            for (int i = 0; i < cars.Count; i++)
                if (cars[i].IsBlocked) blockedCars++;
            if (blockedCars < 2) deadlockEpisodeActive = false;
            for (int i = 0; i < cars.Count; i++)
            {
                var car = cars[i];
                if (!car.IsBlocked || car.BlockedSeconds < DiagnosticThresholdSeconds ||
                    car.DiagnosticReported) continue;
                car.DiagnosticReported = true;
                if (blockedCars > 1)
                {
                    if (deadlockEpisodeActive) continue;
                    deadlockEpisodeActive = true;
                    DeadlockCount++;
                    LastDiagnostic = "Vehicle " + i + " deadlock: " + GetReasonText(car.Reason);
                    continue;
                }
                StuckVehicleCount++;
                LastDiagnostic = "Vehicle " + i + " stuck: " + GetReasonText(car.Reason);
            }
        }

        void ResetBlockedEpisode(Car car)
        {
            car.BlockedSeconds = 0;
            car.IsBlocked = false;
            car.Reason = BlockReason.None;
            car.DiagnosticReported = false;
        }

        static string GetReasonText(BlockReason reason)
        {
            switch (reason)
            {
                case BlockReason.Signal: return "signal";
                case BlockReason.Actor: return "actor";
                case BlockReason.Car: return "car";
                case BlockReason.PublicTransport: return "public transport";
                default: return "traffic";
            }
        }

        public bool BlocksPublicTransport(Vector3 point, Vector3 forward, float step)
        {
            foreach (var car in cars)
            {
                if (CityTrafficSafety.VehicleAhead(point, forward, step,
                    car.Root.position, car.Root.forward)) return true;
            }
            return false;
        }

        bool PeopleAhead(Car car, Vector3 forward, float step)
        {
            Vector3 position = car.Root.position;
            if (Blocks(game.Player)) return true;
            foreach (var worker in game.Workers)
                if (Blocks(worker)) return true;
            if (game.Commerce)
                foreach (var customer in game.Commerce.Customers)
                    if (Blocks(customer)) return true;
            if (game.Restaurant)
                foreach (var diner in game.Restaurant.Diners)
                    if (Blocks(diner)) return true;
            return life && life.BlocksTraffic(position, forward, step);

            bool Blocks(Component actor) => actor && actor.gameObject.activeInHierarchy &&
                CityTrafficSafety.ActorAhead(position, forward, step, actor.transform.position);
        }

        bool CarAhead(Car car, float step)
        {
            foreach (var other in cars)
            {
                if (other == car) continue;
                if (CityTrafficSafety.VehicleAhead(car.Root.position,
                    (car.Route[car.Next] - car.Root.position).normalized, step,
                    other.Root.position, other.Root.forward)) return true;
            }
            return false;
        }
    }
}
