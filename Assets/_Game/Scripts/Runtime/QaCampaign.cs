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
        IEnumerator Progression(bool resume = false)
        {
            if (!resume) yield return Vertical();
            else { yield return new WaitForSeconds(1); Check(game.Economy.Has("machine_upgrade"), "resume uses earned vertical save"); }
            foreach(string id in new[]{"farmer_2","barn","animal_worker","farm_upgrade","worker_upgrade","carry_upgrade","dairy","supermarket","restocker_market","cashier_market","bakery","cook_bakery","restaurant","cook","waiter"})
            {
                if(game.Economy.Has(id)) continue;
                yield return Earn(Definitions.Upgrade(id).cost);yield return Buy(id);
                if(id=="supermarket")
                {
                    float deadline=Time.realtimeSinceStartup+40;
                    while(game.Commerce.PeakCustomers<30&&Time.realtimeSinceStartup<deadline)yield return new WaitForSeconds(1);
                    Check(game.Commerce.PeakCustomers>=30,"30 customers active");
                    yield return Travel(game.Checkouts.Find(x=>x.ShopId=="market").InteractionPoint);yield return Capture("06-supermarket.png");
                }
            }
            Check(game.Workers.Exists(x=>x.Role=="Cook") && game.Workers.Exists(x=>x.Role=="Waiter"),"Cook and Waiter hired");
            yield return Travel(game.Checkouts.Find(x=>x.ShopId=="bakery").InteractionPoint);
            float end=Time.realtimeSinceStartup+300;
            while((game.RestaurantMeals<2||game.BakerySales<2||game.Machines.Exists(x=>x.Batches==0)) && Time.realtimeSinceStartup<end){Keys(Key.E);yield return null;}
            Keys();Check(game.RestaurantMeals>=2,"ordered meals cooked served eaten and paid");Check(game.BakerySales>=2,"bakery customers bought bread or cake");
            foreach(var machine in game.Machines)Check(machine.Batches>0,"recipe operated "+machine.Recipe.id);
            foreach(var shelf in game.Shelves)Check(shelf.Inventory.Total<=shelf.Inventory.Capacity,"shelf capacity "+shelf.Id);
            yield return Capture("07-bakery.png");yield return Travel(game.Tables[1].InteractionPoint);yield return Capture("08-restaurant.png");
            yield return Benchmark();
            Keys(Key.F5);yield return new WaitForSeconds(.2f);Keys();yield return new WaitForSeconds(.2f);
            var save=SaveStore.Read(game.SavePath);
            Check(save.businessStage==5&&save.restaurantMeals>=2&&save.bakerySales>=2,"saved full business progression");Check(save.pendingCash==SumSavedCash(save),"full pending cash conserved");
            game.CameraRig.Overview=true;yield return new WaitForSeconds(1);yield return Capture("09-all-businesses.png");
        }
        IEnumerator Benchmark()
        {
            yield return Travel(game.Checkouts.Find(x=>x.ShopId=="market").InteractionPoint);
            game.CameraRig.Overview=false;yield return new WaitForSeconds(2);
            var camera=Camera.main;var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){antiAliasing=2};target.Create();
            var previous=camera.targetTexture;camera.targetTexture=target;
            int renders=0;Action<UnityEngine.Rendering.ScriptableRenderContext,Camera> handler=(c,cam)=>{if(cam==camera)renders++;};
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering+=handler;
            yield return new WaitForSeconds(5);
            var times=new List<float>();var gpu=new List<double>();var cpu=new List<double>();var timing=new FrameTiming[1];ulong stamp=0;int maxCustomers=0,minCustomers=1000;
            float deadline=Time.realtimeSinceStartup+30;int beginRenders=renders;
            while(Time.realtimeSinceStartup<deadline)
            {
                FrameTimingManager.CaptureFrameTimings();yield return null;
                int customers=game.Commerce.Customers.Count+game.Restaurant.Diners.Count;maxCustomers=Mathf.Max(maxCustomers,customers);minCustomers=Mathf.Min(minCustomers,customers);
                if(customers<30)continue;times.Add(Time.unscaledDeltaTime*1000);
                if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].frameStartTimestamp!=stamp)
                {stamp=timing[0].frameStartTimestamp;if(timing[0].gpuFrameTime>0)gpu.Add(timing[0].gpuFrameTime);if(timing[0].cpuFrameTime>0)cpu.Add(timing[0].cpuFrameTime);}
            }
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=handler;camera.targetTexture=previous;target.Release();Destroy(target);
            Check(renders-beginRenders>300,"benchmark rendered actual camera frames");Check(times.Count>=300,"benchmark samples with thirty customers");
            times.Sort();float total=0;foreach(float t in times)total+=t;double gpuTotal=0,cpuTotal=0;foreach(double t in gpu)gpuTotal+=t;foreach(double t in cpu)cpuTotal+=t;
            var result=new PerformanceReport{graphicsDevice=SystemInfo.graphicsDeviceName,width=1920,height=1080,samples=times.Count,renderedFrames=renders-beginRenders,averageFps=1000*times.Count/total,p95Milliseconds=times[(int)(times.Count*.95f)],minimumCustomers=minCustomers,maximumCustomers=maxCustomers,gpuSamples=gpu.Count,gpuAverageMilliseconds=gpu.Count>0?gpuTotal/gpu.Count:0,cpuAverageMilliseconds=cpu.Count>0?cpuTotal/cpu.Count:0};
            File.WriteAllText(Path.Combine(game.QaDirectory,"performance.json"),JsonUtility.ToJson(result,true));Check(result.averageFps>=60,"1080p average sixty FPS with thirty customers");
        }
        [Serializable] public sealed class PerformanceReport
        {
            public string graphicsDevice;public int width,height,samples,renderedFrames,minimumCustomers,maximumCustomers,gpuSamples;public float averageFps,p95Milliseconds;public double gpuAverageMilliseconds,cpuAverageMilliseconds;
        }
        IEnumerator ManualEarn(int amount)
        {
            var source = game.Producers.Find(x => x.ItemId == "milk");
            var shelf = game.Shelves.Find(x => x.Accepts("milk"));
            var lane = game.Checkouts[0];
            float end = Time.realtimeSinceStartup + 200;
            while (game.Economy.Money < amount)
            {
                Check(Time.realtimeSinceStartup < end, "manual selling within deadline");
                yield return Travel(source.InteractionPoint);
                while (game.Player.Carry.Count("milk") < game.Player.Carry.Capacity && Time.realtimeSinceStartup < end) { Keys(Key.E); yield return null; }
                Keys();
                Check(game.Player.Carry.Count("milk") > 0, "manual harvest uses production inventory");
                if (report.screenshots.Count < 2) yield return Capture("02-tall-carry.png");
                yield return Travel(shelf.InteractionPoint);
                while (game.Player.Carry.Total > 0 && Time.realtimeSinceStartup < end) { Keys(Key.E); yield return null; }
                Keys();
                yield return Travel(lane.InteractionPoint);
                float wait = Time.realtimeSinceStartup + 20;
                Keys(Key.E);
                while ((lane.Queue.Count > 0 || shelf.Inventory.Total > 0) && Time.realtimeSinceStartup < wait) yield return null;
                yield return new WaitForSeconds(.5f); Keys();
                Check(game.Economy.Transactions > 0, "customer checkout paid");
            }
            yield return Capture("03-customer-checkout.png");
        }
        IEnumerator Earn(int amount)
        {
            var lane = game.Checkouts[0];
            yield return Travel(lane.InteractionPoint);
            float end = Time.realtimeSinceStartup + 240;
            int transactions=game.Economy.Transactions;float stalled=Time.realtimeSinceStartup;bool captured=false;
            while (game.Economy.Money < amount && Time.realtimeSinceStartup < end)
            {
                Keys(Key.E);
                if(game.Economy.Transactions!=transactions){transactions=game.Economy.Transactions;stalled=Time.realtimeSinceStartup;captured=false;}
                if(!captured && Time.realtimeSinceStartup-stalled>45){captured=true;WriteState("stalled-state.txt");yield return Capture("stalled.png");}
                yield return null;
            }
            Keys(); Check(game.Economy.Money >= amount, "earned " + amount + " from automation");
        }
        IEnumerator Buy(string id)
        {
            var pad = (PurchasePad)game.Stations.Find(x => x.Id == "pad_" + id);
            yield return Travel(pad.InteractionPoint);
            long money = game.Economy.Money;
            Keys(Key.E); yield return new WaitForSeconds(.35f); Keys();
            Check(game.Economy.Has(id), "purchased " + id);
            Check(game.Economy.Money == money - pad.Upgrade.cost, "exact cost " + id);
            yield return new WaitForSeconds(.2f);
            Keys(Key.F5);yield return new WaitForSeconds(.1f);Keys();
        }
        IEnumerator LoadCheck()
        {
            yield return new WaitForSeconds(1);
            var save = SaveStore.Read(game.SavePath);
            Check(save != null && game.Economy.Money == save.money, "restart loaded wallet");
            Check(game.Economy.PendingCash == SumCash(), "restart retained cash");
            foreach (string id in save.unlocked) Check(game.Economy.Has(id), "restart unlocked " + id);
            Check(game.NextReceipt >= save.nextReceipt, "receipt IDs advance across restart");
            foreach (var amount in save.transit) Check(game.Storage.Inventory.Count(amount.id) >= amount.count, "transit conserved " + amount.id);
            yield return Capture("restart-loaded.png");
        }
    }
}
