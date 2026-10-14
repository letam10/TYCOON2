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
            Check(Vector3.Distance(game.Player.transform.position, CityDistricts.PlayerSpawn) < .4f,
                "New game starts directly in front of the starter TYCOON counter");
            yield return FeatureUiAcceptance();
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
            }
            CheckExpansionNavigation();
            yield return CoastalWaterPictures();
            yield return FeatureBindingsAcceptance();
            yield return FeatureSlotsAcceptance();
            Check(game.RuntimeErrors.Count == 0, "Map, bindings and save slots have no runtime errors");
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
