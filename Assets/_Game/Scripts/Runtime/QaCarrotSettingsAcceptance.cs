using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator CarrotSettingsAcceptance()
        {
            yield return PhysicalSavedAcceptance(false);
            string initial = JsonUtility.ToJson(GameSettingsStore.Current);
            var baseline = game.Transactions.Snapshot();
            Time.timeScale = 0;
            game.Player.CanControl = false;
            game.Player.transform.rotation = Quaternion.Euler(0, 200, 0);
            game.CameraRig.Distance = 6.5f;
            game.CameraRig.FocusOffset = new Vector3(0, .65f, .35f);
            game.CameraRig.Snap();
            foreach (int count in new[] { 1, 6, 7, 24, 48 })
            {
                PhysicalCarryFixture(baseline, "carrot", count);
                yield return null;
                yield return null;
                var stack = game.Player.GetComponentsInChildren<InventoryStack>()
                    .First(x => x.Inventory == game.Player.Carry);
                var models = stack.GetComponentsInChildren<MeshFilter>()
                    .Where(x => x.sharedMesh.name == "HandMesh_carrot").ToArray();
                var layout = CarryItemLayout.For("carrot");
                Check(stack.VisibleCount == count && models.Length == count, "carrot: exact visible count " + count);
                Check(layout.Columns == 2 && layout.Rows == 3, "carrot: two by three per layer");
                Check(Mathf.Abs(layout.Scale / .84f - .9f) < .00001f, "carrot: carry scale reduced by ten percent");
                Check(models.All(x => Quaternion.Angle(x.transform.localRotation, layout.Rotation) < .01f),
                    "carrot: every item has identical horizontal direction " + count);
                Check(models.All(x => Vector3.Dot(x.transform.localRotation * Vector3.up, Vector3.left) > .999f),
                    "carrot: broad leafy ends point left " + count);
                var bounds = layout.Bounds(models[0].sharedMesh);
                float layerStep = bounds.size.y + layout.Spacing.y;
                float baseY = models.Min(x => x.transform.localPosition.y);
                foreach (var model in models)
                {
                    var upper = model.transform.localPosition;
                    if (upper.y < baseY + layerStep * .5f) continue;
                    Check(models.Any(x => Mathf.Abs(x.transform.localPosition.x - upper.x) < .001f
                        && Mathf.Abs(x.transform.localPosition.z - upper.z) < .001f
                        && Mathf.Abs(x.transform.localPosition.y - upper.y + layerStep) < .001f),
                        "carrot: upper layer has a supporting item " + count);
                }
                yield return Capture("carrot-new-" + count + ".png");
            }
            Time.timeScale = 1;
            yield return SettingsPlayerAcceptance();
            JsonUtility.FromJsonOverwrite(initial, GameSettingsStore.Current);
            GameSettingsStore.SaveAndApply();
            game.LoadGame();
            yield return TownWait(() => game.CanSimulate, 8, "restore gameplay after carrot settings QA");
            Check(!game.SaveBlocked, "settings: independent from gameplay save recovery");
        }

        IEnumerator SettingsPlayerAcceptance()
        {
            PressActiveTextButton("Cài đặt");
            yield return null;
            Check(game.Hud.SettingsOpen && Time.timeScale == 0 && !game.Hud.AllowsPlayerControl,
                "settings: HUD button pauses simulation and interaction");
            Check(!game.Hud.GetComponentsInChildren<RectTransform>(true)
                .First(x => x.name == "Toast").gameObject.activeInHierarchy,
                "settings: gameplay toast cannot cover settings controls");
            Check(GameSettingsStore.DisplayRefreshHz > 0, "settings: real display refresh detected");
            yield return SettingsGraphicsAcceptance();
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080),
                new Vector2Int(2560, 1440) })
            {
                Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(.6f);
                Canvas.ForceUpdateCanvases();
                Check(Screen.width == size.x && Screen.height == size.y, "settings: native UI size " + size);
                foreach (var input in SettingsObject<RectTransform>("SettingsPanel")
                    .GetComponentsInChildren<Selectable>())
                {
                    var corners = new Vector3[4];
                    ((RectTransform)input.transform).GetWorldCorners(corners);
                    Check(corners.All(x => x.x >= -1 && x.y >= -1 && x.x <= Screen.width + 1
                        && x.y <= Screen.height + 1), "settings: input fits " + input.name + " " + size);
                }
                yield return Capture("settings-vi-" + size.x + ".png");
            }
            // 4K render thật ngoài màn hình; không dùng ảnh này làm phép đo FPS native 4K.
            CapturePhysicalFrame(Path.Combine(game.QaDirectory, "settings-vi-3840.png"), 3840, 2160);
            report.screenshots.Add("settings-vi-3840.png");
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(.6f);
            SettingsObject<Dropdown>("LanguageDropdown").value = (int)GameLanguage.English;
            SettingsObject<Toggle>("FpsToggle").isOn = true;
            SettingsObject<Toggle>("VSyncToggle").isOn = true;
            SubmitSettingsButton("ApplySettings");
            yield return null;
            Check(GameSettingsStore.Current.language == GameLanguage.English && GameSettingsStore.Current.showFps,
                "settings: UI applies English and FPS toggle");
            Check(QualitySettings.vSyncCount == 1 && Application.targetFrameRate == -1,
                "settings: desktop VSync follows actual display");
            if (game.Hud.SettingsOpen)
            {
                yield return Capture("settings-en.png");
                SubmitSettingsButton("CloseSettings");
            }
            yield return new WaitForSecondsRealtime(.8f);
            Check(SettingsObject<RectTransform>("FpsOverlay").gameObject.activeInHierarchy,
                "settings: FPS overlay actually visible");
            var readout = SettingsObject<Text>("FpsReadout");
            Check(readout.text.ToLowerInvariant().Contains("fps") && readout.text.ToLowerInvariant().Contains("ms")
                && readout.text.ToLowerInvariant().Contains("hz"), "settings: measured FPS frame time and display Hz");
            Check(GameLocalization.Translate("Cà rốt") == "Carrot", "settings: item name translated without ID change");
            Check(!game.Hud.GetComponentsInChildren<Text>().Any(x => x.text.Contains("TIỀN TRÊN TAY")),
                "settings: existing cash HUD changes language");
            yield return Capture("fps-enabled-en.png");
            string persisted = JsonUtility.ToJson(GameSettingsStore.Current);
            GameSettingsStore.Current.language = GameLanguage.Vietnamese;
            GameSettingsStore.Current.showFps = false;
            GameSettingsStore.Initialize(true, game.QaDirectory);
            Check(JsonUtility.ToJson(GameSettingsStore.Current) == persisted,
                "settings: private configuration restores exact saved values");
            Check(File.Exists(Path.Combine(game.QaDirectory, "settings.json")),
                "settings: QA preference file isolated from user preferences");
            PressActiveTextButton("Settings");
            yield return null;
            SettingsObject<Toggle>("FpsToggle").isOn = false;
            SettingsObject<Toggle>("VSyncToggle").isOn = false;
            SettingsObject<Dropdown>("LanguageDropdown").value = (int)GameLanguage.Vietnamese;
            SubmitSettingsButton("ApplySettings");
            if (game.Hud.SettingsOpen)
                SubmitSettingsButton("CloseSettings");
            yield return null;
            Check(!SettingsObject<RectTransform>("FpsOverlay").gameObject.activeInHierarchy,
                "settings: FPS overlay hides completely");
            Check(QualitySettings.vSyncCount == 0 && Application.targetFrameRate == 165,
                "settings: manual 165 cap applies when VSync disabled");
            Check(GameLocalization.Translate("Cà rốt") == "Cà rốt", "settings: Vietnamese restored");
            game.Hud.TogglePause();
            PressActiveTextButton("Cài đặt");
            yield return null;
            game.Hud.TogglePause();
            Check(!game.Hud.SettingsOpen && Time.timeScale == 0, "settings: Esc returns to original pause menu");
            game.Hud.TogglePause();
            Check(Time.timeScale == 1 && game.Hud.AllowsPlayerControl, "settings: closing pause restores play");
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            File.WriteAllText(Path.Combine(game.QaDirectory, "settings-runtime-facts.json"),
                JsonUtility.ToJson(new SettingsFacts
                {
                    displayHz = GameSettingsStore.DisplayRefreshHz,
                    frameCap = Application.targetFrameRate,
                    vSync = QualitySettings.vSyncCount,
                    renderScale = pipeline ? pipeline.renderScale : 0,
                    shadowDistance = pipeline ? pipeline.shadowDistance : 0,
                    msaa = pipeline ? pipeline.msaaSampleCount : 0,
                    fpsReadout = readout.text
                }, true));
        }

        T SettingsObject<T>(string name) where T : Component
        {
            return game.Hud.GetComponentsInChildren<T>(true).Single(x => x.gameObject.name == name);
        }

        IEnumerator SettingsGraphicsAcceptance()
        {
            var balanced = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            float shadowDistance = balanced.shadowDistance;
            int shadowResolution = balanced.mainLightShadowmapResolution;
            int msaa = balanced.msaaSampleCount;
            foreach (var quality in new[] { GameGraphicsPreset.Low, GameGraphicsPreset.High,
                GameGraphicsPreset.Balanced })
            {
                float scale = quality == GameGraphicsPreset.Low ? .85f : 1;
                SettingsObject<Dropdown>("QualityDropdown").value = (int)quality;
                SettingsObject<Slider>("RenderScaleSlider").value = scale;
                SubmitSettingsButton("ApplySettings");
                yield return null;
                yield return null;
                var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                Check(pipeline && (pipeline.hideFlags & HideFlags.DontSave) != 0,
                    "settings: graphics use a private runtime pipeline " + quality);
                Check(GameSettingsStore.Current.quality == quality && Mathf.Abs(pipeline.renderScale - scale) < .001f,
                    "settings: quality and render scale apply from UI " + quality);
                Check(pipeline.msaaSampleCount == (quality == GameGraphicsPreset.Low ? 1
                    : quality == GameGraphicsPreset.High ? 4 : msaa), "settings: MSAA applies " + quality);
                Check(Mathf.Abs(pipeline.shadowDistance - (quality == GameGraphicsPreset.Low ? 45
                    : quality == GameGraphicsPreset.High ? 90 : shadowDistance)) < .001f,
                    "settings: shadow distance applies " + quality);
                Check(pipeline.shadowCascadeCount == 1, "settings: stable single shadow cascade " + quality);
                Check(pipeline.mainLightShadowmapResolution == (quality == GameGraphicsPreset.Low ? 1024
                    : quality == GameGraphicsPreset.High ? 4096 : shadowResolution),
                    "settings: shadow resolution applies " + quality);
            }
        }

        void PressActiveTextButton(string label)
        {
            var button = game.Hud.GetComponentsInChildren<Button>().First(x => x.interactable
                && x.GetComponentInChildren<Text>() && x.GetComponentInChildren<Text>().text == label);
            ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }

        void SubmitSettingsButton(string name)
        {
            ExecuteEvents.Execute(SettingsObject<Button>(name).gameObject,
                new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }

        [System.Serializable]
        sealed class SettingsFacts
        {
            public double displayHz;
            public int frameCap, vSync, msaa;
            public float renderScale, shadowDistance;
            public string fpsReadout;
        }
    }
}
