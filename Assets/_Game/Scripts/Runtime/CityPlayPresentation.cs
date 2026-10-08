using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class CityPlaySession
    {
        [Serializable]
        sealed class SceneryFact
        {
            public string name;
            public Vector3 center, size;
        }

        [Serializable]
        sealed class SceneryFacts
        {
            public SceneryFact[] colliders;
            public int renderers, residents, vehicles;
            public string captureMode = "Presentation camera after ordinary FPS recording has stopped";
        }

        IEnumerator CaptureCityPresentation()
        {
            var scenery = game.transform.Find("World/CityScenery");
            if (!scenery) scenery = FindObjectsByType<Transform>()
                .FirstOrDefault(x => x.name == "CityScenery");
            if (!scenery) throw new InvalidOperationException("Không tìm thấy cảnh thành phố");
            var facts = new SceneryFacts
            {
                colliders = scenery.GetComponentsInChildren<Collider>().Select(x => new SceneryFact
                {
                    name = x.name,
                    center = x.bounds.center,
                    size = x.bounds.size
                }).ToArray(),
                renderers = scenery.GetComponentsInChildren<MeshRenderer>().Length,
                residents = FindFirstObjectByType<CityLife>()?.ResidentCount ?? 0,
                vehicles = FindFirstObjectByType<CityTraffic>()?.VehicleCount ?? 0
            };
            File.WriteAllText(Path.Combine(game.QaDirectory, "city-scenery-facts.json"),
                JsonUtility.ToJson(facts, true));
            var camera = Camera.main;
            var follow = camera.GetComponent<FollowCamera>();
            bool enabledBefore = follow.enabled;
            float clipBefore = camera.farClipPlane;
            follow.enabled = false;
            camera.farClipPlane = 700;
            Quaternion rotation = Quaternion.Euler(55, 40, 0);
            camera.transform.rotation = rotation;
            camera.transform.position = CityDistricts.WorldCenter + Vector3.up * 2
                - rotation * Vector3.forward * 330;
            var bubbles = FindAnyObjectByType<OrderBubbleLayer>();
            bool bubblesActive = bubbles && bubbles.gameObject.activeSelf;
            if (bubbles) bubbles.gameObject.SetActive(false);
            yield return Capture("city-overview");
            if (bubbles) bubbles.gameObject.SetActive(bubblesActive);
            yield return Scene("scenery-farm", new(-91, 0, -10), 48);
            yield return Scene("scenery-park", CityDistricts.ParkCenter, 42);
            yield return Scene("scenery-restaurant", new(-38, 0, 73), 46);
            var transit = FindFirstObjectByType<CityPublicTransport>();
            if (transit)
            {
                foreach (var vehicle in transit.Vehicles)
                    yield return Scene("transit-" + vehicle.name, vehicle.transform.position, 18);
            }
            camera.farClipPlane = clipBefore;
            follow.enabled = enabledBefore;
            follow.Snap();

            IEnumerator Scene(string name, Vector3 focus, float distance)
            {
                camera.transform.position = focus + Vector3.up * 1.5f - rotation * Vector3.forward * distance;
                yield return Capture(name);
            }
        }
    }
}
