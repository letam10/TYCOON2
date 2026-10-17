using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator Stage9MapAcceptance()
        {
            yield return CompletionWait(
                () => game.NavigationReady && !game.IsRestoring,
                30,
                "Stage 9 gameplay ready");

            CityTrafficSignalSystem signals =
                game.GetComponentInChildren<CityTrafficSignalSystem>(true);
            CityTrafficSignalVisual visual =
                game.GetComponentInChildren<CityTrafficSignalVisual>(true);
            Check(signals && visual, "Traffic signal system and visual exist");
            Check(visual.PoleCount == CityTrafficSignalSystem.Definitions.Count,
                "Every traffic signal has a pole");
            Check(visual.ArmCount == CityTrafficSignalSystem.Definitions.Count,
                "Every traffic signal has an arm");
            string phaseLog = signals.DescribePhases();
            File.WriteAllText(Path.Combine(game.QaDirectory, "stage9-traffic-phase.log"), phaseLog);
            Check(phaseLog.Contains("vehicle=") && phaseLog.Contains("pedestrian="),
                "Traffic phase log contains vehicle and pedestrian phases");
            yield return CaptureSignal();

            game.Hud.OpenMap();
            yield return null;
            Check(game.Hud.MapOpen && !game.Player.CanControl && Time.timeScale == 0,
                "Map opens and blocks player control");
            foreach (Vector2Int size in new[]
                { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) })
            {
                GameSettingsStore.Current.resolutionWidth = size.x;
                GameSettingsStore.Current.resolutionHeight = size.y;
                GameSettingsStore.SaveAndApply();
                yield return new WaitForSecondsRealtime(.45f);
                Canvas.ForceUpdateCanvases();
                RectTransform panel = SettingsObject<RectTransform>("WorldMapPanel");
                Check(panel && panel.gameObject.activeInHierarchy,
                    "Map panel visible at " + size.x + "x" + size.y);
                foreach (Text label in panel.GetComponentsInChildren<Text>(true)
                    .Where(candidate => candidate.name.StartsWith("MapLabel_")))
                {
                    Check(label.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(label.text),
                        "Map label readable " + label.name + " at " + size.x + "x" + size.y);
                    Vector3[] corners = new Vector3[4];
                    label.rectTransform.GetWorldCorners(corners);
                    Check(corners.All(corner => corner.x >= -2f && corner.y >= -2f
                        && corner.x <= Screen.width + 2f && corner.y <= Screen.height + 2f),
                        "Map label stays on screen " + label.name + " at " + size.x + "x" + size.y);
                }
                yield return Capture("stage9-map-" + size.x + "x" + size.y + ".png");
            }

            GameSettingsStore.Current.resolutionWidth = 1920;
            GameSettingsStore.Current.resolutionHeight = 1080;
            GameSettingsStore.SaveAndApply();
            yield return new WaitForSecondsRealtime(.45f);
            game.Hud.CloseMap();
            yield return null;
            Check(!game.Hud.MapOpen && game.Player.CanControl && Time.timeScale == 1,
                "Map closes and restores player control");

            Camera camera = Camera.main;
            Vector3 view = camera
                ? camera.WorldToViewportPoint(game.Player.transform.position + Vector3.up)
                : new Vector3(-1, -1, -1);
            Check(camera && view.z > 0f && view.x > 0f && view.x < 1f
                && view.y > 0f && view.y < 1f,
                "Camera sees player after map exit");
            Check(game.PlayerInteractionAreas().Any(), "Interaction area remains available");
            FieldInfo promptField = typeof(GameHud).GetField(
                "prompt", BindingFlags.Instance | BindingFlags.NonPublic);
            Text prompt = promptField?.GetValue(game.Hud) as Text;
            Check(prompt && prompt.isActiveAndEnabled && !string.IsNullOrWhiteSpace(prompt.text),
                "Interaction prompt is readable after map exit");
        }

        IEnumerator CaptureSignal()
        {
            Transform originalTarget = game.CameraRig.Target;
            float originalYaw = game.CameraRig.Yaw;
            float originalDistance = game.CameraRig.Distance;
            var focus = new GameObject("QaStage9SignalFocus");
            focus.transform.position =
                CityTrafficSignalSystem.Definitions[0].Position + Vector3.up * 2f;
            game.CameraRig.Target = focus.transform;
            game.CameraRig.Yaw = 45;
            game.CameraRig.Distance = 24;
            game.CameraRig.Snap();
            yield return new WaitForSecondsRealtime(.25f);
            yield return Capture("stage9-traffic-signal.png");
            game.CameraRig.Target = originalTarget;
            game.CameraRig.Yaw = originalYaw;
            game.CameraRig.Distance = originalDistance;
            game.CameraRig.Snap();
            Destroy(focus);
        }
    }
}
