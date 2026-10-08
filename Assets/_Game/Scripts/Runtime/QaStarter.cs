using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator Foundation()
        {
            yield return new WaitForSeconds(1);
            Check(game.Player.Carry.Total == 0, "clean carry");
            Check(game.Economy.Money == 0 && game.Economy.Transactions == 0, "clean economy");
            var view = game.Player.View;
            Check(view.Animator != null && view.Animator.avatar != null && view.Animator.avatar.isValid, "valid player rig");
            foreach (string state in new[] { "Idle", "Walk", "Run", "Carry", "CarryWalk", "Pickup", "Drop" })
                Check(view.Animator.HasState(0, Animator.StringToHash(state)), "animation " + state);
            foreach (var renderer in FindObjectsByType<Renderer>())
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null || material.shader.name.Contains("Error"))
                        throw new Exception("Invalid material: " + renderer.name);
                    report.materialSlotsChecked++;
                }
            Check(report.materialSlotsChecked > 0, "all rendered material slots valid");
            yield return Capture("01-foundation.png");
            var begin = game.Player.transform.position;
            Keys(Key.W, Key.LeftShift);
            yield return new WaitForSeconds(1.2f);
            Check(view.State == "Run", "keyboard sprint animation");
            Keys(); yield return new WaitForSeconds(.2f);
            Check((game.Player.transform.position - begin).magnitude > 5, "keyboard movement");
            begin = game.Player.transform.position;
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.right });
            yield return new WaitForSeconds(.8f);
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return new WaitForSeconds(.2f);
            Check(game.Player.transform.position.x - begin.x > 2, "gamepad movement");
            var carrot = game.Producers.Find(x => x.ItemId == "carrot");
            yield return Travel(carrot.InteractionPoint);
            float harvestDeadline = Time.realtimeSinceStartup + carrot.Interval * 3;
            while (carrot.Inventory.Count("carrot") < 6)
            {
                if (Time.realtimeSinceStartup > harvestDeadline) throw new Exception("Crop production did not replenish harvest stock.");
                yield return null;
            }
            Keys(Key.E); yield return new WaitForSeconds(2.0f); Keys();
            yield return new WaitForSeconds(.5f);
            Check(game.Player.Carry.Count("carrot") >= 6, "harvest through keyboard");
            Check(view.State == "Carry", "carry animation");
            var carryStack = game.Player.GetComponentInChildren<InventoryStack>();
            Check(carryStack.VisibleCount == game.Player.Carry.Total, "physical carry stack");
            yield return Capture("02-carry.png");
            int harvested = game.Player.Carry.Count("carrot");
            yield return Travel(game.Storage.InteractionPoint);
            Keys(Key.E); yield return new WaitForSeconds(harvested * .25f + .5f); Keys();
            yield return new WaitForSeconds(.3f);
            Check(game.Player.Carry.Total == 0, "drop all through keyboard");
            Check(game.Storage.Inventory.Count("carrot") == harvested, "storage conserves stock");
            Keys(Key.R); yield return new WaitForSeconds(.70f); Keys();
            yield return new WaitForSeconds(.2f);
            int picked = game.Player.Carry.Count("carrot");
            Check(picked > 0 && picked < harvested, "withdraw stock");
            var shelf = game.Shelves[0];
            yield return Travel(shelf.InteractionPoint);
            Keys(Key.E); yield return new WaitForSeconds(picked * .25f + .3f); Keys();
            yield return new WaitForSeconds(.3f);
            Check(game.Player.Carry.Total == 0 && shelf.Inventory.Count("carrot") == picked, "stock shelf");
            Check(game.Storage.Inventory.Count("carrot") + shelf.Inventory.Count("carrot") == harvested, "global transfer conservation");
            Check(game.Economy.Money == 0 && game.Economy.Transactions == 0, "no money from timers");
            Keys(Key.F5); yield return new WaitForSeconds(.2f); Keys();
            yield return new WaitForSeconds(.3f);
            var save = SaveStore.Read(game.SavePath);
            Check(save != null && save.money == 0, "save via F5");
            Check(save.inventories.Exists(x => x.id == "storage" && x.items.Exists(y => y.id == "carrot" && y.count == harvested - picked)), "saved warehouse stock");
            Check(!File.Exists(game.SavePath + ".bak") && !File.Exists(game.SavePath + ".tmp"), "atomic save no backup");
            yield return Capture("03-farm-shop.png");
            game.CameraRig.Overview = true; yield return new WaitForSeconds(1);
            yield return Capture("04-overview.png");
            report.playerHeight = Bounds(game.Player.gameObject).size.y;
            Check(report.playerHeight > 1.1f && report.playerHeight < 2.8f, "rig scale and motion bounds");
        }
        IEnumerator VisualInspection()
        {
            yield return new WaitForSeconds(1);
            Check(game.Economy.Money==0,"art inspection never grants money");
            yield return Capture("01-larger-farm.png");
            foreach(string key in new[]{"player","customer","customer_beach","user_cow","user_chicken"})
            {
                var actor=Art.Model(key,new Vector3(-38,.04f,4),game.transform);
                game.CameraRig.Target=actor.transform;game.CameraRig.FocusOffset=new Vector3(0,Art.ModelSize(key).y*.5f,0);game.CameraRig.Pitch=22;game.CameraRig.Yaw=180;game.CameraRig.Distance=key.Contains("chicken")?4:6;game.CameraRig.Snap();
                var animator=actor.GetComponentInChildren<Animator>();Check(animator&&animator.avatar&&animator.avatar.isValid,"valid rig "+key);
                var bones=actor.GetComponentInChildren<SkinnedMeshRenderer>().bones;
                foreach(string state in key.StartsWith("user_")?new[]{"Idle","Walk"}:new[]{"Idle","Walk","Run","Carry","CarryWalk","Pickup","Drop","Sit"})
                {
                    Check(animator.HasState(0,Animator.StringToHash(state)),key+" clip "+state);
                    animator.Play(state,0,0);yield return new WaitForSeconds(.2f);
                    var previous=new Vector3[bones.Length];for(int i=0;i<bones.Length;i++)previous[i]=bones[i].position;
                    float deadline=Time.realtimeSinceStartup+.7f;float maximum=0;
                    while(Time.realtimeSinceStartup<deadline)
                    {
                        yield return null;
                        for(int i=0;i<bones.Length;i++){maximum=Mathf.Max(maximum,Vector3.Distance(previous[i],bones[i].position));previous[i]=bones[i].position;}
                    }
                    Check(maximum<.4f,"smooth bones "+key+" "+state);
                    var bounds=Bounds(actor);Check(float.IsFinite(bounds.size.y)&&bounds.size.y>.25f&&bounds.size.y<3,"finite pose bounds "+key+" "+state);
                    if(state is "Idle" or "Walk" or "Carry" or "Sit")yield return Capture(key+"-"+state+".png");
                }
                Destroy(actor);
            }
            game.CameraRig.Target=game.Player.transform;game.CameraRig.FocusOffset=new Vector3(2,.8f,3);game.CameraRig.Pitch=55;game.CameraRig.Yaw=40;game.CameraRig.Distance=24;game.CameraRig.Snap();
            var animals=game.GetComponentsInChildren<AnimalMotion>();var starts=new Vector3[animals.Length];
            for(int i=0;i<animals.Length;i++)starts[i]=animals[i].transform.localPosition;
            yield return new WaitForSeconds(4);
            int moved=0;foreach(var animal in animals)
            {
                int index=System.Array.IndexOf(animals,animal);if(Vector3.Distance(starts[index],animal.transform.localPosition)>.05f)moved++;
                Check(Vector3.Distance(animal.Origin,animal.transform.localPosition)<.21f,"animal stays within its cell");
            }
            Check(moved>8,"farm animals really move");
            yield return Capture("02-animal-motion.png");
        }
    }
}
