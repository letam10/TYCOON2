using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        [Serializable] sealed class HudCoverage
        { public int width, height; public float coveredPercent, widestWorldLabel; }
        [Serializable] sealed class HudCoverageReport
        { public List<HudCoverage> samples = new(); }
        readonly HudCoverageReport hudCoverage = new();
        void CheckDrawnPurchasePads()
        {
            var pads=game.Stations.OfType<PurchasePad>().ToArray();
            foreach(var pad in pads)
            {
                var icon=pad.GetComponentInChildren<PurchaseIconView>(true);
                var sprite=icon ? icon.GetComponent<SpriteRenderer>() : null;
                Check(sprite && sprite.sprite && sprite.sprite.texture, "drawn sprite bound to "+pad.Upgrade.id);
                Check(!pad.GetComponentInChildren<Animator>(true) &&
                    pad.GetComponentsInChildren<MeshFilter>(true).Where(f=>!f.GetComponent<TextMesh>()).All(f=>f.name is "PadBorder" or "PadFace"),
                    "purchase pad contains only its base mesh and drawn icon: "+pad.Upgrade.id);
            }
            Check(pads.Length>0,"all purchase pads checked for drawn icons");
        }
        void CheckCompactHud()
        {
            var canvas = game.Hud.GetComponentInChildren<Canvas>();
            var playing = canvas.transform.Find("SafeArea/PlayingHud");
            var rectangles = new List<UnityEngine.Rect>(); var corners = new Vector3[4];
            foreach (var image in playing.GetComponentsInChildren<Image>())
            {
                if (!image.isActiveAndEnabled || image.color.a < .12f) continue;
                image.rectTransform.GetWorldCorners(corners);
                Vector2 a = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[0]);
                Vector2 b = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[2]);
                rectangles.Add(UnityEngine.Rect.MinMaxRect(a.x,a.y,b.x,b.y));
            }
            // Đo hợp các vùng HUD thực sự vẽ, không cộng trùng icon nằm trong thẻ.
            int covered = 0;
            for (int y=0;y<90;y++) for(int x=0;x<160;x++)
            {
                var point = new Vector2((x+.5f)*Screen.width/160, (y+.5f)*Screen.height/90);
                if (rectangles.Any(r=>r.Contains(point))) covered++;
            }
            float percent = 100f*covered/(160*90), widest=0;
            foreach(var label in FindObjectsByType<BillboardLabel>())
            {
                var mesh=label.GetComponent<MeshRenderer>(); if(!mesh || !mesh.enabled)continue;
                var bounds=mesh.bounds;
                float distance=Vector3.Dot(bounds.center-Camera.main.transform.position,Camera.main.transform.forward);
                if(distance<=Camera.main.nearClipPlane)continue;
                float pixels=bounds.size.magnitude*Screen.height/(2*distance*Mathf.Tan(Camera.main.fieldOfView*.5f*Mathf.Deg2Rad));
                widest=Mathf.Max(widest,pixels);
            }
            hudCoverage.samples.Add(new(){width=Screen.width,height=Screen.height,coveredPercent=percent,widestWorldLabel=widest});
            File.WriteAllText(Path.Combine(game.QaDirectory,"hud-coverage.json"),JsonUtility.ToJson(hudCoverage,true));
            Check(percent <= 10, "default HUD covers at most 10% at "+Screen.width+"x"+Screen.height+": "+percent.ToString("0.00")+"%");
            Check(widest <= Mathf.Min(212,Screen.width*.151f), "world labels remain compact: "+widest.ToString("0.0")+"px");
            CheckVisibleButtons("default HUD");
        }
        void CheckPresentationMaterials()
        {
            foreach (var renderer in FindObjectsByType<Renderer>())
                foreach (var material in renderer.sharedMaterials)
                {
                    if (!material || !material.shader || material.shader.name.Contains("Error"))
                        throw new Exception("Invalid presentation material: " + renderer.name);
                    report.materialSlotsChecked++;
                }
            Check(report.materialSlotsChecked > 0, "all presentation material slots are valid");
        }
        Button VisibleButton(string name) => game.Hud.GetComponentsInChildren<Button>(true).First(x => x.name == name && x.gameObject.activeInHierarchy);
        IEnumerator ClickPresentationButton(string name)
        {
            var button=VisibleButton(name); Check(button.IsInteractable(), "UI button enabled: "+name);
            var canvas=button.GetComponentInParent<Canvas>();
            Vector2 position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,button.GetComponent<RectTransform>().TransformPoint(button.GetComponent<RectTransform>().rect.center));
            InputSystem.QueueStateEvent(mouse,new MouseState{position=position}); yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=position}.WithButton(MouseButton.Left)); yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=position}); yield return null;
        }
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
            if(DevelopmentAssistance.Enabled)
            {
                float speed=Time.timeScale;bool controls=game.Player.CanControl;
                Time.timeScale=0;game.Player.CanControl=false;game.Player.StopInteraction();yield return null;
                var before=game.Transactions.Snapshot();
                yield return ClickPresentationButton("Mod game");
                var after=game.Transactions.Snapshot();
                Check(after.money==before.money+999999,"mod click adds exactly 999999 wallet cash");
                after.money=before.money;after.revision=before.revision;after.receipts=before.receipts;after.outbox=before.outbox;after.consumers=before.consumers;
                Check(JsonUtility.ToJson(after)==JsonUtility.ToJson(before),"mod click preserves all other gameplay state");
                yield return new WaitForSecondsRealtime(.2f);
                var wallet=game.Hud.GetComponentsInChildren<Text>().First(t=>t.name=="Money");
                Check(wallet.text==game.Economy.Money.ToString("N0"),"mod wallet display matches authoritative balance");
                yield return Capture("mod-game-cash-only.png");
                Time.timeScale=speed;game.Player.CanControl=controls;
            }
            float originalSpeed = Time.timeScale;
            foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1920,1080), new Vector2Int(2560,1440) })
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(.4f);
                game.Hud.TogglePause(); yield return null;
                Check(!game.Player.CanControl && Time.timeScale == 0, "pause blocks movement and simulation");
                CheckVisibleButtons("pause"); yield return Capture("menu-" + size.x + ".png");
                yield return ClickPresentationButton("Quản lý đội");
                Check(!game.Player.CanControl, "crew modal keeps movement blocked");
                CheckVisibleButtons("crew"); yield return Capture("crew-" + size.x + ".png");
                yield return ClickPresentationButton("Quay lại");
                game.Hud.TogglePause(); yield return null;
                Check(game.Player.CanControl && Time.timeScale > 0, "closing pause restores valid controls");
                yield return ClickPresentationButton("Thông tin");
                Check(!game.Player.CanControl && Time.timeScale == 0,"business information pauses play");
                CheckVisibleButtons("business information");yield return Capture("information-"+size.x+".png");
                yield return ClickPresentationButton("Đóng");
                Check(game.Player.CanControl && Time.timeScale > 0,"closing business information resumes play");
                yield return ClickPresentationButton("Xem kho / chọn hàng");
                Check(!game.Player.CanControl, "stock modal blocks movement");
                CheckVisibleButtons("stock"); yield return Capture("stock-" + size.x + ".png");
                yield return ClickPresentationButton("Đóng");
                Check(game.Player.CanControl, "closing stock restores valid controls");
                yield return ClickPresentationButton("Vận chuyển xe tải");
                Check(!game.Player.CanControl, "cargo modal blocks movement");
                CheckVisibleButtons("cargo"); yield return Capture("cargo-" + size.x + ".png");
                yield return ClickPresentationButton("Đóng");
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
