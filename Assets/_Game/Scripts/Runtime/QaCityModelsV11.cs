using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator CityModelsV11()
        {
            yield return PhysicalSavedAcceptance(false);
            yield return new WaitForSeconds(2);
            Time.timeScale = 0;
            game.Player.CanControl = false;
            foreach (var station in game.Stations.Where(x => x.IsUnlocked
                && x is StorageStation or ShelfStation or CheckoutStation or SafeStation or TableStation))
            {
                var bounds = ModelBounds(station.transform);
                float distance = Mathf.Clamp(bounds.size.magnitude * 1.8f, 6, 16);
                yield return CaptureModelAngles("world-model-" + station.Id, bounds.center, distance, false);
                yield return StandaloneWorldAngles(station.transform, station.Id);
            }
            var landmarks = game.GetComponentsInChildren<CityLandmark>();
            Check(landmarks.Length == 13, "V11 thirteen unique placed landmarks");
            Check(landmarks.Select(x => x.Id).Distinct().Count() == 13, "V11 no duplicate landmarks");
            foreach (var landmark in landmarks)
            {
                var bounds = ModelBounds(landmark.transform);
                float yaw = landmark.Id is "Harbor" or "Beach" or "Marina" ? 270
                    : landmark.Id == "CoffeeHouse" ? 0 : 180;
                yield return CaptureModelAngles("city-placed-" + landmark.Id, bounds.center,
                    bounds.size.magnitude * 1.8f, false, yaw);
                yield return StandaloneWorldAngles(landmark.transform, landmark.Id, "city-single-", yaw);
            }
            yield return PropPreview("Bench", g => CityProps.Bench(g, Vector3.zero));
            yield return PropPreview("Planter", g => CityProps.Planter(g, Vector3.zero, 2.4f));
            yield return PropPreview("Lamp", g => CityProps.Lamp(g, Vector3.zero));
            yield return PropPreview("Signal", g => CityLeisureModels.Signal(g, Vector3.zero));
            yield return PropPreview("Railing", g => CityLeisureModels.Railing(g,
                new Vector3(-2, 0, 0), new Vector3(2, 0, 0)));
            yield return PropPreview("FishingBoat", g => CityBoatModels.Fishing(g, Vector3.zero));
            yield return PropPreview("Yacht", g => CityBoatModels.Yacht(g, Vector3.zero));
            yield return PropPreview("CargoBoat", g => CityBoatModels.Cargo(g, Vector3.zero));
            yield return PropPreview("BeachUmbrella", g => CityBeachDetails.Umbrella(g, Vector3.zero));
            yield return PropPreview("BeachLounger", g => CityBeachDetails.Lounger(g, Vector3.zero));
            yield return RetailPreview("supply_crate");
            yield return CaptureModelAngles("city-placed-Park", CityDistricts.ParkCenter + Vector3.up, 35, false);
            yield return CaptureModelAngles("city-placed-StreetFurniture", new Vector3(-184, 1, -96), 14, false);
            yield return CaptureModelAngles("city-placed-TrafficSignal", new Vector3(-188.8f, 2, -38.8f),
                14, false);
            Time.timeScale = 1;
            yield return WaterDetailComparisonV11();
            game.Player.CanControl = true;
            Check(game.RuntimeErrors.Count == 0, "V11 no runtime errors after model and water checks");
        }

        IEnumerator PropPreview(string id, Action<CityStaticGeometry> build)
        {
            var board = new GameObject("PropInspection").transform;
            board.position = new Vector3(0, 0, 1200);
            var source = new GameObject(id).transform;
            source.SetParent(board, false);
            var geometry = new CityStaticGeometry(source);
            build(geometry);
            geometry.Finish();
            var bounds = ModelBounds(source);
            float side = Mathf.Max(12, bounds.size.magnitude * 3);
            Art.Box("PreviewGround", Vector3.down * .09f, new(side, .18f, side), "#9CA993", board);
            CheckModelGeometry(source, id + " isolated");
            yield return CaptureModelAngles("city-prop-" + id, bounds.center,
                Mathf.Clamp(bounds.size.magnitude * 1.8f, 4, 30));
            UnityEngine.Object.Destroy(board.gameObject);
            yield return null;
        }

        IEnumerator RetailPreview(string key)
        {
            var board = new GameObject("RetailInspection").transform;
            board.position = new Vector3(0, 0, 1200);
            var source = RetailPropModels.Build(key).transform;
            source.SetParent(board, false);
            var bounds = ModelBounds(source);
            Art.Box("PreviewGround", Vector3.down * .09f, new(12, .18f, 12), "#9CA993", board);
            CheckModelGeometry(source, key + " isolated");
            yield return CaptureModelAngles("retail-prop-" + key, bounds.center,
                Mathf.Clamp(bounds.size.magnitude * 1.8f, 4, 16));
            UnityEngine.Object.Destroy(board.gameObject);
            yield return null;
        }
    }
}
