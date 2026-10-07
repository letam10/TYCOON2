using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        [Serializable] sealed class BalanceFixture
        {
            public int randomSeed=74014,maximumMinutes=300,targetMinimumMinutes=180,targetMaximumMinutes=240,traceSeconds=60,startingWallet;
            public float timeScale=12;
        }
        [Serializable] sealed class LayoutReport{public List<string> checkedPoints=new(),failures=new();}
        StationZone FinalZone(Station station,InteractionKind kind)=>game.Stations.OfType<StationZone>().First(x=>x.Target==station&&x.Kind==kind);
        Vector3 FinalPoint(Station station,InteractionKind kind)
        {
            var zone=game.Stations.OfType<StationZone>().FirstOrDefault(x=>x.Target==station&&x.Kind==kind);
            if(zone)return zone.Center;
            // QA theo đúng bề mặt tương tác của town hiện tại, kể cả quầy thu tiền.
            return kind==InteractionKind.Collect&&station is CheckoutStation counter?counter.CollectionPoint:Near(station);
        }
        IEnumerator FinalLayout()
        {
            yield return new WaitForSeconds(1);Check(game.NavigationReady,"final layout NavMesh ready");
            var layout=new LayoutReport();
            void Point(string id,Vector3 point)
            {
                var path=new NavMeshPath();
                if(!NavMesh.SamplePosition(point,out var hit,.7f,NavMesh.AllAreas)||
                    !NavMesh.CalculatePath(game.Player.transform.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)
                    layout.failures.Add(id+" unreachable at "+point);
                else layout.checkedPoints.Add(id+" "+point);
            }
            foreach(var station in game.Stations)
            {
                if(station is ConveyorStation)continue;
                Point(station.Id,station.InteractionPoint);
                if(station is not (StationZone or PurchasePad)){Point(station.Id+" work",station.WorkPoint);Point(station.Id+" wait",station.WaitingPoint);}
                if(station is TableStation table)Point(table.Id+" seat",table.Seat);
            }
            foreach(var counter in game.Checkouts.Where(c=>c.ShopId!="restaurant"))for(int i=0;i<30;i++)Point(counter.Id+" queue "+i,counter.QueuePoint(i));
            var areas=game.Stations.Where(s=>s is StationZone or PurchasePad).ToArray();
            for(int i=0;i<areas.Length;i++)for(int j=i+1;j<areas.Length;j++)
                if(Vector2.Distance(new(areas[i].InteractionPoint.x,areas[i].InteractionPoint.z),new(areas[j].InteractionPoint.x,areas[j].InteractionPoint.z))<areas[i].InteractionRadius+areas[j].InteractionRadius+.05f)
                    layout.failures.Add("Overlapping zones: "+areas[i].Id+" / "+areas[j].Id);
            File.WriteAllText(Path.Combine(game.QaDirectory,"layout-details.json"),JsonUtility.ToJson(layout,true));
            Check(layout.failures.Count==0,"all future footprints, work/wait/queue points and separated zones reachable: "+string.Join("; ",layout.failures));
        }
        IEnumerator FinalVisual()
        {
            if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--qa-load"))
            {
                Check(File.Exists(game.SavePath)&&!game.SaveBlocked,"relaunch loads the requested Save v2 file");
                var saved=SaveStore.Read(game.SavePath);
                Check(game.Economy.Money==saved.money&&game.Economy.CashCollected==saved.cashCollected,"relaunch restores wallet and collected cash before QA input");
                Check(saved.unlocked.All(game.Economy.Has),"relaunch restores every purchased route");
            }
            yield return new WaitForSeconds(1);
            foreach(string state in new[]{"Idle","Walk","Carry","Pickup","Drop","Operate","Farming","AnimalCare","Cashier","Cooking","Serving","Cleaning"})
                Check(game.Player.View.Animator.HasState(0,Animator.StringToHash(state)),"final animation "+state);
            yield return Capture("01-final-starter.png");
            game.CameraRig.Overview=true;yield return new WaitForSeconds(1);yield return Capture("02-final-layout.png");game.CameraRig.Overview=false;
            if(game.Economy.Money==0&&!game.Economy.Has("farm_shop"))
            {
                yield return FinalGetItem("carrot",2);yield return FinalStash();
                var pickup=FinalZone(game.Storage,InteractionKind.Pickup);
                yield return WalkTo(pickup.Center+Vector3.left*1.7f);
                Check(game.Player.Carry.Total==0,"walking through pickup zone never takes an unintended item");
                yield return Travel(pickup.Center);yield return FinalWait(()=>game.Player.Carry.Total>0,3,"pickup after stopping");
                Check(game.Player.Carry.Count("carrot")>0,"stopping inside pickup zone takes owned stock");
                yield return FinalGetItem("carrot",3);
                yield return Capture("03-carrot-carry.png");
                var counter=game.Checkouts.First(x=>x.ShopId=="farm");yield return Travel(FinalZone(counter,InteractionKind.Serve).Center);
                yield return new WaitForSeconds(8);yield return Capture("03-carry-and-service.png");
                yield return Travel(counter.CashZone.Center);yield return new WaitForSeconds(.5f);
                Check(game.Economy.CashCollected>0,"visual new game real farming, sale and player collection");
                yield return Capture("04-collected-wallet.png");
            }
        }
        IEnumerator FinalProgression()
        {
            yield return new WaitForSeconds(1);
            var fixturePath=GameSession.Argument("--qa-fixture",Path.Combine(Directory.GetParent(Application.dataPath).FullName,"mod","test","stage14-balance.json"));
            var settings=JsonUtility.FromJson<BalanceFixture>(File.ReadAllText(fixturePath));
            finalResume=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--qa-stage14-resume");
            if(finalResume)
            {
                finalSourceRun=Path.GetDirectoryName(game.SavePath);
                var previous=JsonUtility.FromJson<QaReport>(File.ReadAllText(Path.Combine(finalSourceRun,"stage14-progression-report.json")));
                Check(previous.checks.Contains("Stage14 new game starts with zero money and no grants"),"resume legitimate Save v2 from verified zero-money run, no state grants");
                File.WriteAllText(Path.Combine(game.QaDirectory,"save-lineage.txt"),"Nguồn: "+game.SavePath+"\nVí lúc bắt đầu chuỗi: 0\nSimulation time: "+game.Transactions.Now+"\nKhông thay money/items/progression của save.");
            }
            else Check(game.Economy.Money==0&&game.Economy.Revenue==0&&game.Economy.Unlocked.Count==0,"Stage14 new game starts with zero money and no grants");
            UnityEngine.Random.InitState(settings.randomSeed);Time.timeScale=settings.timeScale;
            yield return FinalPlayToRestaurant(settings);
        }
    }
}
