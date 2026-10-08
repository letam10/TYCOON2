using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public sealed class CityTraffic : MonoBehaviour
    {
        sealed class Car
        {
            public Transform Root;
            public Vector3[] Route;
            public int Next;
            public float Speed;
            public int Lane;
        }

        readonly List<Car> cars = new();
        GameSession game;
        CityLife life;
        CityPublicTransport publicTransport;
        public int VehicleCount => cars.Count;

        void Start()
        {
            game = GameSession.Instance;
            life = GetComponent<CityLife>();
            publicTransport = GetComponentInChildren<CityPublicTransport>();
            string[] colors = { "#6E9C98", "#C59C75", "#7F94B6", "#A1828D" };
            for (int i = 0; i < colors.Length; i++)
            {
                float side = i % 2 == 0 ? 1.55f : -1.55f;
                Vector3[] route =
                {
                    new(-105 + side, 0, -45 + side), new(102 - side, 0, -45 + side),
                    new(102 - side, 0, 29 - side), new(-105 + side, 0, 29 - side)
                };
                if (i % 2 == 1) System.Array.Reverse(route);
                var root = CityVehicleModel.Build(colors[i], transform, i == 3);
                root.position = route[i];
                cars.Add(new Car
                {
                    Root = root, Route = route, Next = (i + 1) % 4, Speed = 5.5f + i * .4f, Lane = i % 2
                });
            }
        }

        void Update()
        {
            if (!game || !game.CanSimulate || Time.deltaTime <= 0) return;
            foreach (var car in cars)
            {
                Vector3 delta = car.Route[car.Next] - car.Root.position;
                if (delta.sqrMagnitude < .1f)
                {
                    car.Next = (car.Next + 1) % car.Route.Length;
                    continue;
                }
                Vector3 forward = delta.normalized;
                float step = car.Speed * Time.deltaTime;
                if (PeopleAhead(car, forward, step) || CarAhead(car, step) ||
                    publicTransport && publicTransport.BlocksTraffic(car.Root.position, forward, step)) continue;
                car.Root.position = Vector3.MoveTowards(car.Root.position, car.Route[car.Next], step);
                car.Root.rotation = Quaternion.RotateTowards(car.Root.rotation,
                    Quaternion.LookRotation(forward), 180 * Time.deltaTime);
            }
        }

        public bool BlocksPublicTransport(Vector3 point, Vector3 forward, float step)
        {
            foreach (var car in cars)
            {
                Vector3 offset = car.Root.position - point;
                float along = Vector3.Dot(offset, forward);
                if (along > 0 && along < 9 + step &&
                    (offset - forward * along).sqrMagnitude < 2.7f * 2.7f) return true;
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
                if (other != car && CityTrafficSafety.CarAhead(car.Root.position, car.Route, car.Next, car.Lane,
                    other.Root.position, other.Next, other.Lane, step)) return true;
            return false;
        }
    }
}
