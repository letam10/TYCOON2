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
            const string item = "carrot";
            var source = game.Producers.Find(x => x.IsUnlocked && x.ItemId == item);
            var shelf = game.Shelves.Find(x => x.Accepts(item));
            var lane = game.Checkouts[0];
            var safe = game.Stations.OfType<SafeStation>().FirstOrDefault();
            Check(source != null && shelf != null, "unlocked starter producer has a matching shelf");
            Check(safe != null, "cash safe is reachable for the manual sales loop");
            float previousTimeScale = Time.timeScale;
            Time.timeScale = 2f;
            game.Commerce.enabled = false;
            foreach (var customer in game.Commerce.Customers.ToArray()) game.Commerce.Recycle(customer);
            float end = Time.realtimeSinceStartup + 200;
            while (game.Economy.Money + game.Economy.CashInSafe < amount)
            {
                Check(Time.realtimeSinceStartup < end, "manual selling within deadline");
                if (game.Player.Carry.Total == 0)
                {
                    yield return Travel(source.InteractionPoint);
                    while (game.Player.Carry.Count(item) < game.Player.Carry.Capacity &&
                        Time.realtimeSinceStartup < end) { Keys(Key.E); yield return null; }
                    Keys();
                    Check(game.Player.Carry.Count(item) > 0, "manual harvest uses production inventory");
                    if (report.screenshots.Count < 2) yield return Capture("02-tall-carry.png");
                }
                yield return Travel(lane.InteractionPoint);
                Check(game.Player.Carry.Count(item) > 0, "manual checkout carries one carrot");
                while (game.Player.Carry.Total > 0 && game.Economy.Money + game.Economy.CashInSafe < amount &&
                    Time.realtimeSinceStartup < end)
                {
                    if (lane.Queue.Count == 0) CreateSingleCarrotQaCustomer(lane, game.NextReceipt++);
                    int transactionsBefore = game.Economy.Transactions;
                    float wait = Time.realtimeSinceStartup + 20;
                    Keys(Key.E);
                    while (lane.Queue.Count > 0 && Time.realtimeSinceStartup < wait)
                    {
                        Keys(Key.E);
                        yield return null;
                    }
                    Keys();
                    if (game.Economy.Transactions == transactionsBefore)
                    {
                        WriteState("checkout-failure-state.txt");
                        Check(false, "customer checkout paid");
                    }
                }
                if (game.Player.Carry.Total > 0 || lane.Queue.Count > 0) continue;
                Check(game.Player.Carry.Total == 0, "manual checkout empties the carried batch");
                long balanceBeforeCash = game.Economy.Money;
                yield return Travel(lane.CollectionPoint);
                float cashDeadline = Time.realtimeSinceStartup + 8;
                while (game.Economy.Money == balanceBeforeCash && Time.realtimeSinceStartup < cashDeadline)
                {
                    Keys(Key.E);
                    yield return null;
                }
                Keys();
                Check(game.Economy.Money > balanceBeforeCash, "player collects checkout cash");
                float depositDeadline = Time.realtimeSinceStartup + 8;
                yield return Travel(safe.Deposit.Center);
                while (game.Economy.CashInHand > 0 && Time.realtimeSinceStartup < depositDeadline)
                {
                    Keys(Key.E);
                    yield return null;
                }
                Keys();
                Check(game.Economy.CashInHand == 0, "player deposits cash before the next harvest");
            }
            yield return Travel(safe.Withdraw.Center);
            float withdrawDeadline = Time.realtimeSinceStartup + 8;
            while (game.Economy.Money < amount && Time.realtimeSinceStartup < withdrawDeadline)
            {
                Keys(Key.E);
                yield return null;
            }
            Keys();
            Check(game.Economy.Money >= amount, "player withdraws manual sales for the next upgrade");
            Time.timeScale = previousTimeScale;
            yield return Capture("03-customer-checkout.png");
        }
        CustomerAgent CreateSingleCarrotQaCustomer(CheckoutStation counter, long sequence)
        {
            var root = new GameObject("ManualEarnCustomer_" + sequence);
            root.transform.SetParent(game.Commerce.transform);
            root.transform.position = counter.QueuePoint(0);
            var model = Art.Model("customer", Vector3.zero, root.transform);
            model.AddComponent<ActorView>();
            var customer = root.AddComponent<CustomerAgent>();
            customer.Initialize();
            if (!UnityEngine.AI.NavMesh.SamplePosition(counter.QueuePoint(0), out var point, 3,
                UnityEngine.AI.NavMesh.AllAreas)) throw new Exception("Manual QA customer cannot reach farm queue.");
            var exit = TownLayout.Spawn(0);
            customer.Restore(new CustomerSave
            {
                receipt = sequence,
                shop = counter.ShopId,
                lane = counter.Id,
                phase = (int)CustomerAgent.State.Queue,
                remaining = OrderState.Patience,
                order = new List<OrderLine> { new("carrot", 1, game.ItemPrice("carrot")) },
                x = point.position.x,
                y = point.position.y,
                z = point.position.z,
                exitX = exit.x,
                exitY = exit.y,
                exitZ = exit.z
            });
            root.transform.position = point.position;
            customer.Agent.Warp(point.position);
            game.Commerce.Customers.Add(customer);
            return customer;
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
            float deadline = Time.realtimeSinceStartup + 8;
            while (!game.Economy.Has(id) && Time.realtimeSinceStartup < deadline)
            {
                Keys(Key.E);
                yield return null;
            }
            Keys();
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
