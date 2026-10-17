using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        static readonly string[] Stage7SmokeIds =
        {
            "farm", "residential_housing", "park", "bank", "commercial_market",
            "restaurant", "industrial_yard", "station", "harbor", "marina",
            "forest_reserve", "beach_swim", "highway_service_area"
        };

        IEnumerator Stage7PoiAccessAcceptance()
        {
            yield return WaitForStage7World();
            Check(WorldPoiAccessCatalog.Validate(out string reason), reason);
            WorldPoiAccessMarker[] allMarkers = FindObjectsByType<WorldPoiAccessMarker>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var markers = allMarkers.ToDictionary(marker => marker.StableId, StringComparer.Ordinal);
            Check(markers.Count == WorldPoiAccessCatalog.BuildDefault().Count,
                "Stage7 marker count matches catalog");
            foreach (WorldPoiAccessDefinition definition in WorldPoiAccessCatalog.BuildDefault())
            {
                Check(markers.TryGetValue(definition.StableId, out WorldPoiAccessMarker marker),
                    definition.StableId + " marker exists");
                Check(marker.Interaction != null && marker.Interaction.StableId == definition.StableId,
                    definition.StableId + " interaction stable ID");
                Check(marker.CollisionBoundary.size.x > 0f && marker.CollisionBoundary.size.z > 0f,
                    definition.StableId + " collision boundary");
            }

            foreach (string id in Stage7SmokeIds)
            {
                if (!markers.TryGetValue(id, out WorldPoiAccessMarker marker))
                    throw new Exception("Missing Stage7 smoke marker " + id);
                yield return VisitStage7Poi(marker, CaptureName(id));
            }

            yield return ExerciseDynamicTaxi();
            Check(game.RuntimeErrors.Count == 0, "Stage7 Player runtime errors remain empty");
        }

        IEnumerator WaitForStage7World()
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while (game && (!game.NavigationReady || game.IsRestoring))
            {
                if (Time.realtimeSinceStartup > deadline)
                    throw new Exception("Stage7 world did not become ready");
                yield return null;
            }
            yield return new WaitForSeconds(.5f);
        }

        IEnumerator VisitStage7Poi(WorldPoiAccessMarker marker, string captureName)
        {
            int useBefore = marker.Interaction.UseCount;
            Vector3 target = marker.ApproachAnchor;
            if (!NavMesh.SamplePosition(target, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                throw new Exception("Stage7 approach is not on NavMesh: " + marker.StableId);
            var path = new NavMeshPath();
            bool complete = NavMesh.CalculatePath(
                game.Player.transform.position, hit.position, NavMesh.AllAreas, path)
                && path.status == NavMeshPathStatus.PathComplete;
            Check(complete, marker.StableId + " NavMesh route complete");
            yield return Travel(hit.position);
            Check(marker.Interaction.Contains(game.Player.transform.position),
                marker.StableId + " capsule reaches interaction area");

            if (marker.Definition.Action is WorldInteractionAction.BoardTrain
                or WorldInteractionAction.BoardVessel)
            {
                ICityPassengerCarrier carrier = marker.Interaction.CarrierSource as ICityPassengerCarrier;
                if (carrier == null)
                    throw new Exception("Stage7 carrier missing: " + marker.StableId);
                yield return WaitForDoors(carrier, marker.StableId);
            }

            yield return new WaitForSeconds(.35f);
            if (marker.Interaction.UseCount == useBefore)
            {
                InteractionResult result = marker.Interaction.Perform(
                    new InteractionContext(game.Player, "carrot"), .35f);
                if (!result.Worked && marker.Interaction.UseCount == useBefore)
                    Check(false, marker.StableId + " enter interaction: " + result.Reason);
            }
            Check(marker.Interaction.UseCount > useBefore,
                marker.StableId + " enter interaction");
            yield return new WaitForSeconds(marker.Interaction.Action is
                WorldInteractionAction.BoardTrain or WorldInteractionAction.BoardVessel
                ? marker.Interaction.RideSeconds + 1f : .25f);
            Check(game.Player.CanControl, marker.StableId + " exit returns player control");
            marker.Exit(game.Player.GetEntityId());
            yield return CaptureStage7(captureName);
        }

        IEnumerator WaitForDoors(ICityPassengerCarrier carrier, string id)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!carrier.DoorsOpen)
            {
                if (Time.realtimeSinceStartup > deadline)
                    throw new Exception("Stage7 carrier doors did not open: " + id);
                yield return null;
            }
        }

        IEnumerator ExerciseDynamicTaxi()
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            WorldInteractionArea area = null;
            while (!area)
            {
                area = FindObjectsByType<WorldInteractionArea>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(candidate => candidate
                        && candidate.Action == WorldInteractionAction.CallTaxi
                        && candidate.CarrierSource is ICityPassengerCarrier);
                if (area || Time.realtimeSinceStartup > deadline) break;
                yield return null;
            }
            Check(area != null, "dynamic taxi interaction area exists");
            if (!area) yield break;
            ICityPassengerCarrier carrier = area.CarrierSource as ICityPassengerCarrier;
            yield return WaitForDoors(carrier, "taxi");
            if (!NavMesh.SamplePosition(area.ApproachPoint, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                throw new Exception("Taxi approach is not on NavMesh");
            yield return Travel(hit.position);
            Check(area.Contains(game.Player.transform.position), "taxi capsule reaches interaction area");
            InteractionResult result = area.Perform(new InteractionContext(game.Player, "carrot"), .35f);
            Check(result.Worked, "taxi boarding interaction: " + result.Reason);
            yield return new WaitForSeconds(area.RideSeconds + 1f);
            Check(game.Player.CanControl, "taxi alighting returns player control");
        }

        IEnumerator CaptureStage7(string name)
        {
            Keys();
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(game.QaDirectory, name + ".png");
            QaRenderedFrame.Write(path);
            report.screenshots.Add(name + ".png");
            yield return null;
        }

        static string CaptureName(string id)
        {
            return id switch
            {
                "bank" or "restaurant" => "stage7-interior-" + id,
                "residential_housing" => "stage7-residential",
                "industrial_yard" => "stage7-industrial",
                "station" => "stage7-station",
                "harbor" or "marina" => "stage7-harbor-" + id,
                "forest_reserve" => "stage7-forest-reserve",
                "beach_swim" => "stage7-beach-swim",
                "highway_service_area" => "stage7-highway-service",
                _ => "stage7-" + id
            };
        }
    }
}
