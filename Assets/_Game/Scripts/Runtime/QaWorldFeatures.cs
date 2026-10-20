using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator WorldFeaturesAcceptance()
        {
            yield return CompletionWait(() => game.NavigationReady && !game.IsRestoring, 30, "Map: gameplay ready");
            CityTrafficSignalVisual signalVisual = game.GetComponentInChildren<CityTrafficSignalVisual>(true);
            Check(signalVisual && signalVisual.LampCount
                == CityTrafficSignalSystem.Definitions.Count * 3,
                "Traffic signals expose dynamic red amber green lamps");
            Check(Vector3.Distance(game.Player.transform.position, CityDistricts.PlayerSpawn) < .4f,
                "New game starts directly in front of the starter TYCOON counter");
            yield return StreetSurfaceAcceptance();
            yield return SampledRouteAcceptance();
            yield return FeatureUiAcceptance();
            yield return MapClippingAcceptance();
            yield return Capture("minimap-1920.png");
            var minimap = SettingsObject<RectTransform>("Minimap");
            var carry = SettingsObject<RectTransform>("Carry");
            Vector3[] mapCorners = new Vector3[4], carryCorners = new Vector3[4];
            minimap.GetWorldCorners(mapCorners); carry.GetWorldCorners(carryCorners);
            Check(mapCorners[0].x >= 0 && mapCorners[0].y >= 0 && mapCorners[2].y < carryCorners[0].y,
                "Minimap stays bottom left without covering the carry HUD");
            yield return FeatureKey(Key.M);
            Check(game.Hud.MapOpen && Time.timeScale == 0 && !game.Player.CanControl,
                "M opens a paused full map and blocks player interaction");
            Vector3 paused = game.Player.transform.position;
            Keys(Key.W); yield return new WaitForSecondsRealtime(.15f); Keys();
            Check(game.Player.transform.position == paused, "Movement input cannot move player while map is open");
            foreach (var size in new[] { new Vector2Int(1280, 720), new(1920, 1080), new(2560, 1440) })
            {
                var settings = GameSettingsStore.Current;
                settings.resolutionWidth = size.x; settings.resolutionHeight = size.y;
                GameSettingsStore.SaveAndApply(); yield return new WaitForSecondsRealtime(.6f);
                Canvas.ForceUpdateCanvases();
                foreach (var button in SettingsObject<RectTransform>("WorldMapPanel").GetComponentsInChildren<Button>())
                {
                    button.GetComponent<RectTransform>().GetWorldCorners(mapCorners);
                    Check(mapCorners.All(x => x.x >= -1 && x.y >= -1 && x.x <= Screen.width + 1 && x.y <= Screen.height + 1),
                        "Map control stays inside native screen " + size + " " + button.name);
                }
                yield return Capture("map-" + size.x + ".png");
            }
            GameSettingsStore.Current.resolutionWidth = 1920; GameSettingsStore.Current.resolutionHeight = 1080;
            GameSettingsStore.SaveAndApply(); yield return new WaitForSecondsRealtime(.6f);
            yield return FeatureKey(Key.M);
            Check(!game.Hud.MapOpen && game.Player.CanControl && Time.timeScale == 1,
                "Second M closes map and restores control");
            game.Hud.OpenSettings(); FeatureClick("SettingsMapButton"); yield return null;
            Check(game.Hud.MapOpen, "Settings opens the same full map");
            FeatureClick("CloseMap"); yield return null;
            Check(game.Hud.SettingsOpen && Time.timeScale == 0, "Closing settings map returns to settings paused");
            FeatureClick("CloseSettings"); yield return null;
            for (int i = 0; i < CityMapLocations.Points.Length; i++)
            {
                game.Hud.OpenMap();
                FeatureClick("MapTravel_" + CityMapLocations.Points[i].id); yield return null;
                Check(!game.Hud.MapOpen && game.Player.CanControl,
                    "Map travels to unblocked destination " + CityMapLocations.Points[i].id);
                Check(Vector3.Distance(game.Player.transform.position, CityMapLocations.Points[i].position) < 3.5f,
                    "Map lands at the chosen special point " + CityMapLocations.Points[i].id);
                if (CityMapLocations.Points[i].id.Contains("_"))
                {
                    yield return new WaitForSecondsRealtime(.2f);
                    yield return Capture("expansion-" + CityMapLocations.Points[i].id + ".png");
                    yield return CaptureExpansionCenter(CityMapLocations.Points[i].id);
                }
            }
            yield return CutawayPictures();
            CheckExpansionNavigation();
            yield return CoastalWaterPictures();
            yield return FeatureBindingsAcceptance();
            yield return FeatureSlotsAcceptance();
            Check(game.RuntimeErrors.Count == 0, "Map, bindings and save slots have no runtime errors");
        }

        IEnumerator CaptureExpansionCenter(string id)
        {
            string poiId = id switch
            {
                "forest_reserve" => "ForestReserve",
                "industrial_yard" => "IndustrialYard",
                "north_village" => "NorthVillage",
                "east_marina" => "EastMarina",
                "south_highway" => "SouthHighway",
                _ => string.Empty
            };
            WorldExpansionPoi poi = WorldExpansionCatalog.BuildDefault()
                .First(candidate => candidate.Id == poiId);
            Transform originalTarget = game.CameraRig.Target;
            float originalYaw = game.CameraRig.Yaw;
            float originalDistance = game.CameraRig.Distance;
            Vector3 originalPlayerPosition = game.Player.transform.position;
            CharacterController playerController = game.Player.Controller;
            bool controllerEnabled = playerController && playerController.enabled;
            var focus = new GameObject("QaExpansionFocus");
            try
            {
                // Đưa player ra khỏi chiều cao footprint để Director không ẩn shell trong ảnh POI.
                if (playerController) playerController.enabled = false;
                game.Player.transform.position = originalPlayerPosition + Vector3.up * 9.5f;
                foreach (WorldCutawayVolume volume in game.GetComponentsInChildren<WorldCutawayVolume>(true))
                    if (volume && volume.name is "ExpansionCutaway" or "VillageCutaway")
                        volume.SetPlayerInside(false);
                focus.transform.position = poi.Center + Vector3.up * .5f;
                game.CameraRig.Target = focus.transform;
                game.CameraRig.Yaw = 45;
                game.CameraRig.Distance = 125;
                game.CameraRig.Snap();
                yield return null;
                yield return new WaitForEndOfFrame();
                Vector3 cameraPosition = Camera.main ? Camera.main.transform.position : Vector3.zero;
                Vector3 targetPosition = game.CameraRig.Target
                    ? game.CameraRig.Target.position
                    : Vector3.zero;
                string diagnostic = string.Join("\n", new[]
                {
                    "expectedId=" + id,
                    "poiId=" + poi.Id,
                    "poiCenter=" + poi.Center,
                    "focus=" + focus.transform.position,
                    "target=" + (game.CameraRig.Target ? game.CameraRig.Target.name : "null"),
                    "targetPosition=" + targetPosition,
                    "cameraPosition=" + cameraPosition,
                    "yaw=" + game.CameraRig.Yaw,
                    "distance=" + game.CameraRig.Distance,
                    "playerPosition=" + game.Player.transform.position
                });
                Transform poiRoot = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(candidate => candidate && candidate.name == "Expansion_" + poi.Id);
                if (poiRoot)
                {
                    Renderer[] renderers = poiRoot.GetComponentsInChildren<Renderer>(true);
                    Collider[] colliders = poiRoot.GetComponentsInChildren<Collider>(true);
                    Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(poiRoot.position, Vector3.zero);
                    for (int index = 1; index < renderers.Length; index++)
                        bounds.Encapsulate(renderers[index].bounds);
                    string[] materials = renderers
                        .SelectMany(renderer => renderer.sharedMaterials ?? System.Array.Empty<Material>())
                        .Where(material => material)
                        .Select(material => material.name)
                        .Distinct()
                        .OrderBy(name => name)
                        .ToArray();
                    diagnostic += "\nroot=" + poiRoot.name
                        + "\nrootPosition=" + poiRoot.position
                        + "\nrendererCount=" + renderers.Length
                        + "\ncolliderCount=" + colliders.Length
                        + "\nrenderBounds=" + bounds
                        + "\nmaterials=" + string.Join(",", materials);
                    Transform models = poiRoot.Find("ExpansionModels");
                    diagnostic += "\nmodelRoot=" + (models ? models.name : "null")
                        + "\nmodelChildCount=" + (models ? models.childCount.ToString() : "0");
                }
                else
                {
                    diagnostic += "\nroot=null";
                }
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(game.QaDirectory, "expansion-center-" + id + ".camera.txt"),
                    diagnostic);
                yield return Capture("expansion-center-" + id + ".png");
            }
            finally
            {
                game.Player.transform.position = originalPlayerPosition;
                if (playerController) playerController.enabled = controllerEnabled;
                game.CameraRig.Target = originalTarget;
                game.CameraRig.Yaw = originalYaw;
                game.CameraRig.Distance = originalDistance;
                game.CameraRig.Snap();
                Destroy(focus);
            }
        }

        void CheckExpansionNavigation()
        {
            var path = new NavMeshPath();
            foreach (WorldExpansionPoi poi in WorldExpansionCatalog.BuildDefault())
                foreach (Vector3 anchor in poi.RouteAnchors)
                {
                    bool sampled = NavMesh.SamplePosition(anchor, out var hit, 2f, NavMesh.AllAreas);
                    bool calculated = sampled && NavMesh.CalculatePath(game.Player.transform.position, hit.position,
                        NavMesh.AllAreas, path);
                    bool reachable = calculated && path.status == NavMeshPathStatus.PathComplete;
                    string status = sampled ? path.status.ToString() : "SampleMissing";
                    string distance = sampled ? hit.distance.ToString("0.00") : "-";
                    Check(reachable, "Expansion route reaches " + poi.Id + " " + anchor
                        + " sampleDistance=" + distance + " pathStatus=" + status);
                }
        }

        IEnumerator CutawayPictures()
        {
            VerifyLandmarkCutawayCoverage();
            WorldCutawayVolume[] volumes = game.GetComponentsInChildren<WorldCutawayVolume>(true)
                .Where(volume => volume && (volume.name == "StationCutaway" || volume.name == "VillageCutaway"))
                .ToArray();
            Check(volumes.Length >= 2, "Station and village cutaway volumes are registered");
            Vector3 playerPosition = game.Player.transform.position;
            Transform originalTarget = game.CameraRig.Target;
            float originalYaw = game.CameraRig.Yaw;
            float originalDistance = game.CameraRig.Distance;
            foreach (WorldCutawayVolume volume in volumes)
            {
                game.Player.transform.position = volume.transform.position + Vector3.up * .05f;
                volume.SetPlayerInside(true);
                yield return null;
                Check(volume.IsRevealed, "Cutaway reveals " + volume.name + " while player is inside");
                Check(volume.upperShellRenderers.Any(renderer => renderer),
                    "Cutaway has registered upper shell renderers " + volume.name);
                Check(volume.upperShellRenderers.Where(renderer => renderer)
                    .All(renderer => !renderer.enabled), "Cutaway hides upper shell " + volume.name);
                var focus = new GameObject("QaCutawayFocus");
                focus.transform.position = volume.transform.position + Vector3.up * .5f;
                game.CameraRig.Target = focus.transform;
                game.CameraRig.Yaw = 45;
                game.CameraRig.Distance = volume.name == "VillageCutaway" ? 70 : 22;
                game.CameraRig.Snap();
                yield return new WaitForSecondsRealtime(.2f);
                yield return Capture("cutaway-" + volume.name + ".png");
                game.Player.transform.position = playerPosition;
                volume.SetPlayerInside(false);
                yield return null;
                Check(!volume.IsRevealed, "Cutaway restores upper shell " + volume.name);
                Destroy(focus);
            }
            game.CameraRig.Target = originalTarget;
            game.CameraRig.Yaw = originalYaw;
            game.CameraRig.Distance = originalDistance;
            game.CameraRig.Snap();
            game.Player.transform.position = playerPosition;
        }

        void VerifyLandmarkCutawayCoverage()
        {
            CityLandmark[] landmarks = game.GetComponentsInChildren<CityLandmark>(true)
                .Where(landmark => landmark && landmark.Footprint.x > 0 && landmark.Footprint.z > 0)
                .ToArray();
            Check(landmarks.Length >= 13, "Every placed landmark participates in cutaway coverage");
            foreach (CityLandmark landmark in landmarks)
            {
                WorldCutawayVolume volume = landmark.GetComponent<WorldCutawayVolume>();
                Check(volume, "Cutaway volume exists for landmark " + landmark.Id);
                Check(volume && volume.upperShellRenderers.Any(renderer => renderer),
                    "Cutaway shell exists for landmark " + landmark.Id);
            }

            foreach (string id in new[] { "Harbor", "Marina", "Bank" })
            {
                CityLandmark landmark = landmarks.SingleOrDefault(item => item.Id == id);
                Check(landmark && landmark.GetComponent<WorldCutawayVolume>(),
                    "Interactive landmark has cutaway volume " + id);
                if (id is "Harbor" or "Marina")
                {
                    WorldInteractionArea area = landmark
                        ? landmark.GetComponent<WorldInteractionArea>()
                        : null;
                    Check(area && area.Action == WorldInteractionAction.BoardVessel,
                        "Dock has vessel boarding interaction " + id);
                    ICityPassengerCarrier carrier = area?.CarrierSource as ICityPassengerCarrier;
                    Check(carrier != null && carrier.Hull,
                        "Dock interaction owns a vessel carrier " + id);
                    Check(area && carrier != null
                        && Vector3.Distance(area.Center, carrier.DoorOutside) < .1f,
                        "Dock interaction is aligned to vessel door " + id);
                }
            }

            WorldInteractionArea[] trainAreas = game.GetComponentsInChildren<WorldInteractionArea>(true)
                .Where(area => area && area.Action == WorldInteractionAction.BoardTrain)
                .ToArray();
            Check(trainAreas.Any(), "Passenger train has a boarding interaction area");

            WorldCutawayVolume[] expansionVolumes = game.GetComponentsInChildren<WorldCutawayVolume>(true)
                .Where(volume => volume && volume.name == "ExpansionCutaway")
                .ToArray();
            Check(expansionVolumes.Length > 0, "Expansion districts have cutaway volumes");
        }

        IEnumerator FeatureKey(Key key)
        {
            Keys(key); yield return null; yield return null;
            Keys(); yield return null;
        }

        void FeatureClick(string name)
        {
            var button = SettingsObject<Button>(name);
            Check(button.isActiveAndEnabled && button.interactable, "UI button is reachable " + name);
            SubmitSettingsButton(name);
        }

        IEnumerator CoastalWaterPictures()
        {
            var beach = game.GetComponentsInChildren<CityLandmark>().Single(x => x.Id == "Beach");
            var ocean = beach.GetComponentsInChildren<MeshRenderer>()
                .Select(x => x.sharedMaterial).First(x => x.shader.name == "TYCOON/Town Water");
            Color tint = ocean.GetColor("_BaseColor");
            Check(tint.g > tint.r && tint.b > tint.r && ocean.shader.isSupported,
                "Coast uses supported blue-green water shader");
            Check(beach.GetComponentsInChildren<MeshRenderer>().Any(x => x.sharedMaterial.name.Contains("sand:")),
                "Beach uses sand material with a wet shore band");
            Transform original = game.CameraRig.Target;
            float yaw = game.CameraRig.Yaw, distance = game.CameraRig.Distance;
            var focus = new GameObject("QaCoastFocus");
            focus.transform.position = beach.transform.position + Vector3.right * 14;
            game.CameraRig.Target = focus.transform; game.CameraRig.Yaw = 90; game.CameraRig.Distance = 27;
            game.CameraRig.Snap();
            yield return new WaitForSecondsRealtime(.2f);
            yield return Capture("clear-water-a.png");
            yield return new WaitForSecondsRealtime(.4f);
            yield return Capture("clear-water-b.png");
            focus.transform.position = beach.transform.position + Vector3.right * 3;
            game.CameraRig.Yaw = 70; game.CameraRig.Snap();
            yield return Capture("beach-context.png");
            game.CameraRig.Target = original; game.CameraRig.Yaw = yaw; game.CameraRig.Distance = distance;
            game.CameraRig.Snap(); Destroy(focus);
        }
    }
}
