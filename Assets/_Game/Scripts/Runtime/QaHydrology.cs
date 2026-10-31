using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator HydrologyAcceptance()
        {
            yield return CompletionWait(() => game.NavigationReady && !game.IsRestoring, 60,
                "Hydrology world and navigation ready");
            yield return MapPhotographyAcceptance();
            CityMapCapture photographs = game.GetComponent<CityMapCapture>();
            foreach (Vector3 point in new[] { new Vector3(239, 0, 143), new(243, 0, 146), new(245, 0, 151) })
            {
                Vector2 uv = photographs.Grid.WorldToUv(point);
                int x = 1 + Mathf.FloorToInt(uv.x * (photographs.Atlas.width - 2));
                int y = 1 + Mathf.FloorToInt(uv.y * (photographs.Atlas.height - 2));
                Color pixel = photographs.Atlas.GetPixel(x, y);
                Check(pixel.b > pixel.r + .02f, "Estuary photograph contains water, not a floating sand patch: " + point);
            }
            Check(game.GetComponentsInChildren<Unity.AI.Navigation.NavMeshSurface>().Length == 1,
                "One world NavMesh owns both town and relocated expansion geometry");
            foreach (Vector3 point in new[] { new Vector3(350, 0, 77), new(400, 0, -215), new(500, 0, 300) })
            {
                Check(WorldHydrologyLayout.IsOcean(point), "Former grass strip is open ocean: " + point);
                Check(!NavMesh.SamplePosition(point, out _, 1, NavMesh.AllAreas),
                    "Open sea has no walkable NavMesh: " + point);
            }
            var river = WorldHydrologyLayout.River;
            int checkedSections = 0;
            for (int index = 12; index < river.Count - 8; index += 23)
            {
                Vector3 point = river[index].Center;
                if (!CityDistricts.WorldBounds.Contains(point)
                    || WorldHydrologyNavigation.IsRouteCrossing(point, 3))
                    continue;
                Check(!NavMesh.SamplePosition(point, out _, 1, NavMesh.AllAreas),
                    "River channel is not walkable away from bridges: " + index + " " + point);
                checkedSections++;
            }
            Check(checkedSections >= 5, "Water exclusion sampled along the upstream and town river");
            HydrologyPath(game.Player.transform.position, CoastalTownLayout.IndustrialVisit, "inland industry");
            HydrologyPath(game.Player.transform.position, CoastalTownLayout.MarinaEntry, "coastal marina");
            var boat = game.GetComponentsInChildren<WorldExpansionBoat>()
                .First(value => value.transform.parent.parent.name == "Expansion_EastMarina");
            HydrologyPath(CoastalTownLayout.MarinaEntry, boat.DoorOutside, "marina boarding pier");

            WorldRouteCatalog.TryGet("forest-access", out WorldRouteCorridor crossing);
            HydrologyPlace(crossing.Nodes[0]);
            Vector3 before = game.Player.transform.position;
            yield return Travel(crossing.Nodes[1]);
            yield return Capture("hydrology-river-bridge.png");
            yield return Travel(crossing.Nodes[crossing.Nodes.Count - 1]);
            Check(Vector3.Distance(before, game.Player.transform.position) > 20,
                "Player physically crosses the river bridge through normal movement input");
            yield return Capture("hydrology-river-crossing.png");

            HydrologyPlace(CoastalTownLayout.IndustrialVisit);
            yield return Capture("hydrology-industrial-inland.png");
            HydrologyPlace(CoastalTownLayout.MarinaEntry);
            int boardings = boat.Boardings;
            int alightings = boat.Alightings;
            yield return Travel(boat.DoorOutside);
            yield return CompletionWait(() => boat.Boardings > boardings, 5,
                "Walking to the boat triggers the normal automatic boarding interaction");
            yield return CompletionWait(() => boat.Alightings > alightings && game.Player.CanControl, 20,
                "Normal ride returns the player to the pier with control restored");
            Check(Vector2.Distance(new Vector2(game.Player.transform.position.x, game.Player.transform.position.z),
                new Vector2(boat.DoorOutside.x, boat.DoorOutside.z)) < .5f,
                "Player walks from the marina quay up the ramp to the boarding point");
            yield return Capture("hydrology-marina-pier.png");

            float coast = WorldHydrologyLayout.CoastX(77);
            HydrologyPlace(new Vector3(coast - 7, 0, 77));
            yield return Travel(new Vector3(coast - 2, 0, 77));
            yield return Capture("hydrology-coast-t0.png");
            yield return new WaitForSecondsRealtime(1);
            yield return Capture("hydrology-coast-t1.png");
            HydrologyPlace(WorldHydrologyLayout.Mouth + new Vector3(-5, 0, -19));
            float distance = game.CameraRig.Distance;
            game.CameraRig.Zoom(-100);
            game.CameraRig.Snap();
            yield return Capture("hydrology-estuary.png");
            game.CameraRig.Distance = distance;
            game.CameraRig.Snap();
            Check(game.CameraRig.Pitch == 55 && game.CameraRig.Yaw == 40,
                "Hydrology inspection preserves the gameplay camera angles");
            Check(game.RuntimeErrors.Count == 0, "Hydrology Player has no runtime errors");
        }

        void HydrologyPath(Vector3 start, Vector3 target, string label)
        {
            Check(NavMesh.SamplePosition(start, out NavMeshHit from, 3, NavMesh.AllAreas),
                "Navigation source exists for " + label);
            Check(NavMesh.SamplePosition(target, out NavMeshHit to, 3, NavMesh.AllAreas),
                "Navigation destination exists for " + label);
            var path = new NavMeshPath();
            bool complete = NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path)
                && path.status == NavMeshPathStatus.PathComplete;
            if (!complete)
                HydrologyNavigationDiagnostic(label, from.position, to.position, path);
            Check(complete, "Complete land/bridge path to " + label);
        }

        void HydrologyPlace(Vector3 point)
        {
            Check(NavMesh.SamplePosition(point, out NavMeshHit hit, 3, NavMesh.AllAreas),
                "Grounded capture position: " + point);
            var controller = game.Player.Controller;
            controller.enabled = false;
            game.Player.transform.position = hit.position + Vector3.up * .05f;
            controller.enabled = true;
            game.CameraRig.Snap();
        }
    }
}
