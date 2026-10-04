using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator Redesign()
        {
            float limit=Time.realtimeSinceStartup+30;
            while(!game.NavigationReady){Check(Time.realtimeSinceStartup<limit,"Navigation startup");yield return null;}
            yield return new WaitForSeconds(1);
            Check(game.Economy.Money==0,"Fresh wallet is zero");
            Check(game.Player.Carry.Capacity==6&&game.Player.Carry.SingleItem,"Carry one SKU with six slots");
            Check(game.Producers.FindAll(x=>x.IsUnlocked&&x.ItemId=="carrot").Count==3,"Three free carrot plots");
            Check(!game.CanPurchase(Definitions.Upgrade("farmer"),out _),"No first hire before operational and upgrade gates");
            yield return Capture("01-starter.png");
            var crop=game.Producers.Find(x=>x.IsUnlocked&&x.ItemId=="carrot");
            yield return Travel(crop.InteractionPoint);
            limit=Time.realtimeSinceStartup+45;
            while(game.Player.Carry.Total<6){Check(Time.realtimeSinceStartup<limit,"Manual crop phases produce carry");yield return null;}
            yield return Capture("02-carry-six.png");
            var counter=game.Checkouts.Find(x=>x.ShopId=="farm"&&x.IsUnlocked);
            var service=game.Stations.Find(x=>x.Id==counter.Id+"_serve");
            yield return Travel(service?service.InteractionPoint:counter.InteractionPoint+Vector3.right*2);
            limit=Time.realtimeSinceStartup+100;
            while(game.Economy.Transactions==0){Check(Time.realtimeSinceStartup<limit,"Direct service completes a real order");yield return null;}
            Check(game.Economy.Money==0&&game.Economy.PendingCash>0,"Sale stays as physical cash");
            yield return Capture("03-first-sale.png");
            var collect=game.Stations.Find(x=>x.Id==counter.Id+"_cash");
            yield return Travel(collect?collect.InteractionPoint:counter.InteractionPoint);
            limit=Time.realtimeSinceStartup+10;
            while(game.Economy.Money==0){Check(Time.realtimeSinceStartup<limit,"Player collects cash");yield return null;}
            int balance=game.Economy.Money;
            var pad=game.Stations.Find(x=>x is PurchasePad p&&p.Upgrade.id=="barn");
            yield return Travel(pad.InteractionPoint);
            limit=Time.realtimeSinceStartup+15;
            while(game.Contribution("barn")==0){Check(Time.realtimeSinceStartup<limit,"Partial pad contributions");yield return null;}
            yield return Travel(pad.InteractionPoint+Vector3.left*3);
            int contributed=game.Contribution("barn");
            Check(contributed>0&&contributed<500&&!game.Economy.Has("barn"),"Partial purchase does not unlock");
            Check(game.Economy.Money+contributed==balance,"Wallet and contribution conserved");
            Keys(Key.F5);yield return null;Keys();yield return new WaitForSeconds(.2f);
            var saved=SaveStore.Read(game.SavePath);
            Check(saved.version==2&&saved.purchases.Exists(x=>x.id=="barn"&&x.paid==contributed),"Save v2 persists partial purchase");
            Check(saved.customers.Count>0&&saved.transit.Count==0,"Customers saved separately from warehouse");
            Check(game.RuntimeErrors.Count==0,"No runtime errors in starter loop");
            yield return Capture("04-partial-pad-save.png");
        }
    }
}
