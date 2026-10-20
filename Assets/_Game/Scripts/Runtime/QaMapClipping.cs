using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator MapClippingAcceptance()
        {
            Check(game.Player.CanControl && !game.Hud.MapOpen && Time.timeScale == 1,
                "Minimap clipping starts from normal gameplay");
            string originalSettings = JsonUtility.ToJson(GameSettingsStore.Current);
            int originalWidth = Screen.width;
            int originalHeight = Screen.height;
            FullScreenMode originalMode = Screen.fullScreenMode;
            try
            {
                foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) })
                {
                    var settings = GameSettingsStore.Current;
                    settings.resolutionWidth = size.x;
                    settings.resolutionHeight = size.y;
                    settings.fullscreen = false;
                    GameSettingsStore.SaveAndApply();
                    float deadline = Time.realtimeSinceStartup + 5;
                    while ((Screen.width != size.x || Screen.height != size.y)
                        && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    yield return new WaitForSecondsRealtime(.15f);
                    Canvas.ForceUpdateCanvases();
                    yield return new WaitForEndOfFrame();
                    string sample = "Minimap clipping " + size.x + "x" + size.y;
                    Check(Screen.width == size.x && Screen.height == size.y,
                        sample + " uses the requested native screen");

                    var minimap = SettingsObject<RectTransform>("Minimap");
                    var viewport = SettingsObject<RectTransform>("MinimapViewport");
                    var terrain = SettingsObject<CityMapGraphic>("MinimapTerrain");
                    var mask = viewport.GetComponent<RectMask2D>();
                    Check(viewport.parent == minimap && terrain.transform.parent == viewport,
                        sample + " keeps terrain inside its dedicated viewport");
                    Check(mask && mask.isActiveAndEnabled && mask.padding == Vector4.zero
                        && mask.softness == Vector2Int.zero,
                        sample + " has an active hard rectangular viewport mask");
                    Check(terrain.Local && terrain.maskable && !terrain.raycastTarget
                        && MaskUtilities.GetRectMaskForClippable(terrain) == mask
                        && terrain.canvasRenderer.hasRectClipping,
                        sample + " applies the viewport clip to the rendered local terrain");
                    Check(viewport.anchorMin == Vector2.zero && viewport.anchorMax == Vector2.one
                        && viewport.offsetMin == new Vector2(8, 30)
                        && viewport.offsetMax == new Vector2(-8, -8),
                        sample + " retains the frame and help insets");
                    Check(terrain.rectTransform.anchorMin == Vector2.zero
                        && terrain.rectTransform.anchorMax == Vector2.one
                        && terrain.rectTransform.offsetMin == Vector2.zero
                        && terrain.rectTransform.offsetMax == Vector2.zero,
                        sample + " fills the clipped viewport");

                    var viewportCorners = new Vector3[4];
                    var terrainCorners = new Vector3[4];
                    viewport.GetWorldCorners(viewportCorners);
                    terrain.rectTransform.GetWorldCorners(terrainCorners);
                    Check(viewport.rect.width > 0 && viewport.rect.height > 0,
                        sample + " has a nonempty viewport");
                    for (int index = 0; index < viewportCorners.Length; index++)
                    {
                        Vector3 corner = viewportCorners[index];
                        Check(Vector3.Distance(corner, terrainCorners[index]) < .5f,
                            sample + " terrain corner matches the mask " + index);
                        Check(corner.x >= 0 && corner.y >= 0
                            && corner.x <= Screen.width && corner.y <= Screen.height,
                            sample + " viewport corner stays on screen " + index);
                    }
                    foreach (string name in new[] { "MinimapHelp", "North" })
                    {
                        var label = SettingsObject<Text>(name);
                        Check(label.isActiveAndEnabled && label.transform.parent == minimap
                            && MaskUtilities.GetRectMaskForClippable(label) != mask,
                            sample + " keeps " + name + " outside terrain clipping");
                    }

                    string captureName = "minimap-clipped-" + size.x + ".png";
                    yield return Capture(captureName);
                    var header = new byte[24];
                    using var capture = File.OpenRead(Path.Combine(game.QaDirectory, captureName));
                    int read = capture.Read(header, 0, header.Length);
                    Check(read == header.Length && PngDimension(header, 16) == size.x
                        && PngDimension(header, 20) == size.y,
                        sample + " capture retains the native PNG dimensions");
                    FeatureClick("Minimap");
                    yield return null;
                    Check(game.Hud.MapOpen && Time.timeScale == 0 && !game.Player.CanControl,
                        sample + " minimap button opens the paused world map");
                    FeatureClick("CloseMap");
                    yield return null;
                    Check(!game.Hud.MapOpen && game.Player.CanControl && Time.timeScale == 1,
                        sample + " closing the world map restores gameplay control");
                }
            }
            finally
            {
                if (game.Hud.MapOpen)
                    game.Hud.CloseMap();
                JsonUtility.FromJsonOverwrite(originalSettings, GameSettingsStore.Current);
                GameSettingsStore.SaveAndApply();
                // Cấu hình 0 giữ kích thước hiện tại, nên phục hồi cửa sổ riêng.
                Screen.SetResolution(originalWidth, originalHeight, originalMode);
            }
            float restoreDeadline = Time.realtimeSinceStartup + 5;
            while ((Screen.width != originalWidth || Screen.height != originalHeight
                || Screen.fullScreenMode != originalMode)
                && Time.realtimeSinceStartup < restoreDeadline)
                yield return null;
            Check(JsonUtility.ToJson(GameSettingsStore.Current) == originalSettings,
                "Minimap clipping restores the original settings");
            Check(Screen.width == originalWidth && Screen.height == originalHeight
                && Screen.fullScreenMode == originalMode,
                "Minimap clipping restores the original native resolution and window mode");
            Check(!game.Hud.MapOpen && game.Player.CanControl && Time.timeScale == 1,
                "Minimap clipping restores normal gameplay");
        }
    }
}
