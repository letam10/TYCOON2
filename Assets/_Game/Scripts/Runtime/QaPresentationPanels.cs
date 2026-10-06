using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        void CheckPresentationMaterials()
        {
            foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach (var material in renderer.sharedMaterials)
                {
                    if (!material || !material.shader || material.shader.name.Contains("Error"))
                        throw new Exception("Invalid presentation material: " + renderer.name);
                    report.materialSlotsChecked++;
                }
            Check(report.materialSlotsChecked > 0, "all presentation material slots are valid");
        }
        Button VisibleButton(string name) => game.Hud.GetComponentsInChildren<Button>(true).First(x => x.name == name && x.gameObject.activeInHierarchy);
        void CheckVisibleButtons(string panel)
        {
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];
            var canvas = game.Hud.GetComponentInChildren<Canvas>();
            foreach (var button in game.Hud.GetComponentsInChildren<Button>())
            {
                // Nút trong danh sách cuộn có thể nằm ngoài viewport nhưng đã được mask.
                if (!button.gameObject.activeInHierarchy || button.GetComponentInParent<RectMask2D>() || button.GetComponentInParent<Mask>()) continue;
                button.GetComponent<RectTransform>().GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var p = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corner);
                    if (p.x < -3 || p.y < -3 || p.x > Screen.width + 3 || p.y > Screen.height + 3)
                        throw new Exception(panel + " button outside screen: " + button.name + " at " + p);
                }
            }
            Check(true, panel + " visible buttons fit " + Screen.width + "x" + Screen.height);
        }
        IEnumerator PresentationPanels()
        {
            CheckPresentationMaterials();
            float originalSpeed = Time.timeScale;
            foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1920,1080), new Vector2Int(2560,1440) })
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(.4f);
                game.Hud.TogglePause(); yield return null;
                Check(!game.Player.CanControl && Time.timeScale == 0, "pause blocks movement and simulation");
                CheckVisibleButtons("pause"); yield return Capture("menu-" + size.x + ".png");
                VisibleButton("Quản lý đội").onClick.Invoke(); yield return null;
                Check(!game.Player.CanControl, "crew modal keeps movement blocked");
                CheckVisibleButtons("crew"); yield return Capture("crew-" + size.x + ".png");
                VisibleButton("Quay lại").onClick.Invoke(); yield return null;
                game.Hud.TogglePause(); yield return null;
                Check(game.Player.CanControl && Time.timeScale > 0, "closing pause restores valid controls");
                VisibleButton("Xem kho / chọn hàng").onClick.Invoke(); yield return null;
                Check(!game.Player.CanControl, "stock modal blocks movement");
                CheckVisibleButtons("stock"); yield return Capture("stock-" + size.x + ".png");
                VisibleButton("Đóng").onClick.Invoke(); yield return null;
                Check(game.Player.CanControl, "closing stock restores valid controls");
                VisibleButton("Vận chuyển xe tải").onClick.Invoke(); yield return null;
                Check(!game.Player.CanControl, "cargo modal blocks movement");
                CheckVisibleButtons("cargo"); yield return Capture("cargo-" + size.x + ".png");
                VisibleButton("Đóng").onClick.Invoke(); yield return null;
                Check(game.Player.CanControl, "closing cargo restores valid controls");
            }
            Time.timeScale = originalSpeed; game.Player.StopInteraction();
            game.Hud.TogglePause(); game.SaveGame(); game.LoadGame();
            float restoreDeadline = Time.realtimeSinceStartup + 8;
            while (!game.CanSimulate)
            {
                if (Time.realtimeSinceStartup > restoreDeadline) throw new Exception("restore under pause timed out");
                yield return null;
            }
            yield return null;
            Check(!game.Player.CanControl && Time.timeScale == 0, "restore keeps pause input locked");
            game.Hud.TogglePause();
            Check(game.Player.CanControl && Time.timeScale == originalSpeed, "resume after restore returns to valid play input");
        }
    }
}
