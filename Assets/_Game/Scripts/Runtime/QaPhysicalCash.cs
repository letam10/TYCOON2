using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator PhysicalCashAcceptance()
        {
            yield return new WaitForSeconds(1);
            Check(game.NavigationReady, "physical: NavMesh ready");
            Check(game.Transactions.View.schemaVersion == 3, "physical: schema 3");
            Check(game.Player.Carry.Capacity == 12, "physical: starter capacity 12");
            Check(game.Economy.CashInHand == 0 && game.Economy.CashInSafe == 0, "physical: zero start");
            var safe = game.Stations.OfType<SafeStation>().Single();
            var plot = game.Producers.First(x => x.Id == "field_carrot");
            var pickup = game.Stations.OfType<StationZone>().Single(x => x.Target == plot && x.Kind == InteractionKind.Pickup);
            while (game.Player.Carry.Count("carrot") < 6)
            {
                if (plot.Phase != 3)
                {
                    yield return Travel(Near(plot));
                    yield return TownWait(() => plot.Phase == 3, 35, "grow carrots before harvest");
                }
                int before = game.Player.Carry.Count("carrot");
                yield return Travel(pickup.InteractionPoint);
                yield return TownWait(() => game.Player.Carry.Count("carrot") > before, 35,
                    "harvest carrots from pickup zone");
            }
            yield return Travel(pickup.InteractionPoint + Vector3.back * 2);
            yield return Capture("01-carrots-in-hands.png");
            var counter = game.Checkouts.First(x => x.ShopId == "farm");
            var customer = TownCustomer(counter, "carrot", Mathf.Min(2, game.Player.Carry.Count("carrot")), 90);
            yield return Travel(Near(counter));
            yield return TownWait(() => customer.Order.Paid && counter.Cash > 0, 30,
                "FIFO customer completes real order");
            if (game.Player.Carry.Total > 0) yield return TownDrop(game.Storage);
            Check(game.Player.Carry.Total == 0 && counter.Cash > 0, "physical: goods exchanged for counter cash");
            Check(game.Economy.CashInHand == 0, "physical: payment remains at counter until collected");
            yield return Travel(counter.CashZone.InteractionPoint);
            yield return TownWait(() => game.Economy.CashInHand > 0, 5, "collect cash onto hands");
            long earned = game.Economy.CashInHand;
            Check(game.Player.HasCarry, "physical: cash enables carry pose");
            yield return Capture("02-earned-cash.png");
            yield return Travel(pickup.InteractionPoint);
            yield return new WaitForSeconds(1);
            Check(game.Player.Carry.Total == 0 && game.Economy.CashInHand == earned,
                "physical: cash blocks harvesting goods");
            yield return Travel(safe.Deposit.Center);
            yield return TownWait(() => game.Economy.CashInHand == 0, 5, "deposit all earned cash");
            Check(game.Economy.CashInSafe == earned, "physical: safe receives exact total");
            int deposits = game.Transactions.View.outbox.Count(x => x.kind == nameof(TransactionKind.DepositCash));
            yield return new WaitForSeconds(1);
            Check(game.Transactions.View.outbox.Count(x => x.kind == nameof(TransactionKind.DepositCash)) == deposits,
                "physical: standing in safe zone does not repeat");
            yield return Travel(safe.Withdraw.Center);
            yield return TownWait(() => game.Economy.CashInHand == earned, 5, "withdraw all cash onto hands");
            Check(game.Economy.CashInSafe == 0, "physical: withdrawal clears safe");
            yield return Travel(safe.Withdraw.Center + Vector3.forward * 2);
            game.SaveGame();
            game.Commerce.enabled = true;
            game.Restaurant.enabled = true;
            game.LoadGame();
            yield return TownWait(() => game.CanSimulate, 8, "load restores all actors before controls");
            Check(!game.SaveBlocked && game.Economy.CashInHand == earned, "physical: save/load cash exactly");
            game.Commerce.enabled = false;
            yield return PhysicalModsAndPurchase(safe);
            yield return PhysicalCargoAcceptance();
            yield return PhysicalDinerAcceptance();
            yield return PhysicalVisualAcceptance();
            game.SaveGame();
            Check(game.RuntimeErrors.Count == 0, "physical: no runtime errors");
        }

        IEnumerator PhysicalModsAndPurchase(SafeStation safe)
        {
            long hand = game.Economy.CashInHand;
            long revenue = game.Economy.Revenue;
            int orders = game.Economy.Transactions;
            yield return ClickPresentationButton("Mod game");
            Check(game.Hud.IsModMenuOpen && !game.Hud.AllowsPlayerControl, "physical: mod menu blocks controls");
            yield return Capture("03-mod-two-options.png");
            yield return ClickPresentationButton("Mod tiền");
            Check(game.Economy.CashInHand == hand && game.Economy.CashInSafe == 999999,
                "physical: mod adds only 999999 to safe");
            Check(game.Economy.Revenue == revenue && game.Economy.Transactions == orders,
                "physical: cash mod preserves business statistics");
            yield return Travel(safe.Withdraw.Center);
            yield return TownWait(() => game.Economy.CashInSafe == 0, 5, "withdraw mod money");
            var pad = game.Stations.OfType<PurchasePad>().First(x => x.Upgrade.id == "carry10");
            long before = game.Economy.CashInHand;
            yield return Travel(pad.Center);
            yield return PhysicalUpgradeMotion();
            yield return TownWait(() => game.Economy.Has("carry10"), 8, "purchase carry upgrade using hand cash");
            yield return Capture("04-upgrade-completion.png");
            yield return Travel(pad.Center + Vector3.back * 2);
            Check(game.Player.Carry.Capacity >= 20, "physical: first carry tier doubles to 20");
            Check(game.Economy.CashInHand <= before - 100, "physical: purchase spends hand cash");
            yield return ClickPresentationButton("Mod game");
            yield return ClickPresentationButton("Mod xây dựng tối đa");
            yield return new WaitForSeconds(.8f);
            var maximum = game.Transactions.Snapshot();
            Check(Definitions.Upgrades.Where(x => x.kind != "legacy").All(x => maximum.unlocked.Contains(x.id)),
                "physical: every current upgrade maximized");
            Check(game.Player.Carry.Capacity == 48 && maximum.truck != null, "physical: max carry and truck");
            Check(!game.Stations.OfType<PurchasePad>().Any(x => x.Available), "physical: final pads have no interaction");
            int workers = game.Workers.Count;
            Check(workers == maximum.crews.Sum(x => x.count), "physical: all maximized crews are instantiated");
            yield return ClickPresentationButton("Mod game");
            yield return ClickPresentationButton("Mod xây dựng tối đa");
            yield return new WaitForSeconds(.3f);
            Check(game.Workers.Count == workers && game.Transactions.View.crews.Count == maximum.crews.Count,
                "physical: repeated maximum creates no duplicate crews");
            Check(game.Economy.Revenue == revenue && game.Economy.Transactions == orders,
                "physical: maximum preserves business statistics");
            game.SaveGame();
            game.Commerce.enabled = true;
            game.Restaurant.enabled = true;
            game.LoadGame();
            yield return TownWait(() => game.CanSimulate, 8, "load restores all actors before controls");
            Check(!game.SaveBlocked && game.Player.Carry.Capacity == 48 && game.Transactions.View.maximumBuilt,
                "physical: maximum persists across load");
            yield return Capture("05-maximum-world.png");
        }
    }
}
