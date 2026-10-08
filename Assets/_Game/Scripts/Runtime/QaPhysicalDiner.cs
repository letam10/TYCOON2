using System.Collections;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator PhysicalDinerAcceptance()
        {
            var baseline = game.Transactions.Snapshot();
            PhysicalCarryFixture(baseline, null, 0);
            game.Commerce.enabled = false;
            game.Restaurant.enabled = false;
            foreach (var worker in game.Workers) worker.enabled = false;
            var table = game.Tables.First(x => x.IsUnlocked && !x.Occupant && x.Cleaning <= 0);
            var actor = new GameObject("QA_Diner_ActualOrder");
            actor.transform.SetParent(game.Restaurant.transform, false);
            actor.transform.position = table.Seat;
            Art.Model("customer", Vector3.zero, actor.transform).AddComponent<ActorView>();
            var diner = actor.AddComponent<DinerAgent>();
            diner.Initialize();
            game.Restaurant.Diners.Add(diner);
            diner.Begin(table, null);
            yield return TownWait(() => diner.Phase == 1, 8, "diner sits and requests item");
            Check(diner.PatienceLeft > 0 && diner.Basket.Total == 0,
                "physical diner: actual order has patience and empty basket");
            game.CameraRig.Target = actor.transform;
            game.CameraRig.Distance = 12;
            game.CameraRig.Snap();
            yield return Capture("diner-actual-request.png");
            var order = game.Transactions.Snapshot();
            PhysicalCarryFixture(order, diner.WantedItem, 1);
            yield return Travel(Near(table));
            yield return TownWait(() => diner.Phase == 2, 8, "player delivers requested diner meal");
            Check(game.Player.Carry.Total == 0 && diner.Basket.Total == 1,
                "physical diner: successful delivery moves real 3D item to basket");
            yield return Capture("diner-actual-delivered.png");
            yield return TownWait(() => diner.Phase == 3, 10, "diner finishes eating and settles");
            Check(game.Checkouts.First(x => x.ShopId == "restaurant").Cash > 0,
                "physical diner: completed order leaves cash at checkout");
            yield return Capture("diner-actual-completed.png");
            game.Restaurant.Recycle(diner);
            game.Commerce.enabled = true;
            game.Restaurant.enabled = true;
            game.CameraRig.Target = game.Player.transform;
            game.LoadGame();
            yield return TownWait(() => game.CanSimulate, 8, "restore checkpoint after actual diner fixture");
            Check(!game.SaveBlocked, "physical diner: restores original checkpoint");
        }
    }
}
