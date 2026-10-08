using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public sealed class CityPublicTransport : MonoBehaviour
    {
        readonly List<CityPublicTransportVehicle> vehicles = new();
        GameSession game;
        Transform world;
        public IReadOnlyList<CityPublicTransportVehicle> Vehicles => vehicles;

        public static void Build(GameSession game, Transform world)
        {
            var root = new GameObject("CityPublicTransport");
            root.transform.SetParent(world, false);
            var transit = root.AddComponent<CityPublicTransport>();
            transit.game = game;
            transit.world = world;
        }

        IEnumerator Start()
        {
            while (game && (!game.NavigationReady || !game.CanSimulate)) yield return null;
            if (!game) yield break;
            var life = world.GetComponent<CityLife>();
            string[] names = { "CityBus", "PassengerCoach", "CityTaxi" };
            int[] starts = { 0, 4, 1 };
            for (int i = 0; i < names.Length; i++)
            {
                var root = new GameObject(names[i]);
                root.transform.SetParent(transform, false);
                var vehicle = root.AddComponent<CityPublicTransportVehicle>();
                vehicle.Initialize(game, life, i, starts[i]);
                vehicles.Add(vehicle);
            }
            foreach (int stop in new[] { 0, 1, 4, 5 }) BuildStop(stop);
        }

        public bool BlocksTraffic(Vector3 point, Vector3 forward, float step)
        {
            foreach (var vehicle in vehicles)
            {
                if (!vehicle) continue;
                Vector3 offset = vehicle.transform.position - point;
                float along = Vector3.Dot(offset, forward);
                if (along > 0 && along < 9 + step &&
                    (offset - forward * along).sqrMagnitude < 2.7f * 2.7f) return true;
            }
            return false;
        }

        void BuildStop(int index)
        {
            Vector3 forward = index < 4 ? Vector3.right : Vector3.left;
            Vector3 outside = Vector3.Cross(Vector3.up, forward);
            Vector3 at = CityPublicTransportVehicle.Lane[index] + outside * 6.4f;
            var root = TownModelParts.Root("TransitStop_" + index, at, transform);
            TownModelParts.Box("StopPost", new(0, 1.3f, 0), new(.12f, 2.6f, .12f),
                "metal", "#53666E", root);
            TownModelParts.Box("RouteSign", new(0, 2.35f, 0), new(.8f, .55f, .1f),
                "metal", "#377F91", root);
            TownModelParts.Box("BenchSeat", new(1.5f, .48f, 0), new(1.9f, .12f, .55f),
                "wood", "#A58260", root);
            TownModelParts.Box("BenchBack", new(1.5f, .85f, .22f), new(1.9f, .5f, .08f),
                "wood", "#A58260", root);
            for (int side = -1; side <= 1; side += 2)
                TownModelParts.Box("BenchLeg", new(1.5f + side * .65f, .23f, 0),
                    new(.12f, .46f, .45f), "metal", "#53666E", root);
            StaticBatchingUtility.Combine(root.gameObject);
        }
    }
}
