using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public sealed class CityRailService : MonoBehaviour
    {
        readonly List<CityRailTrain> trains = new();
        GameSession game;
        CityLife life;
        public IReadOnlyList<CityRailTrain> Trains => trains;

        public static void Build(GameSession game, Transform world)
        {
            CityRailStation.Build(world);
            var root = TownModelParts.Root("CityRailService", Vector3.zero, world);
            var service = root.gameObject.AddComponent<CityRailService>();
            service.game = game;
            service.life = world.GetComponent<CityLife>();
        }

        IEnumerator Start()
        {
            while (game && (!game.NavigationReady || !game.CanSimulate)) yield return null;
            if (!game) yield break;
            for (int track = 0; track < 2; track++)
            {
                var model = CityRailVehicleModel.Build(transform, track == 0 ? "#4D7F6A" : "#5A789B");
                model.name = "PassengerTrain_" + track;
                var train = model.gameObject.AddComponent<CityRailTrain>();
                train.Initialize(game, life, track);
                trains.Add(train);
                yield return null;
            }
        }
    }
}
