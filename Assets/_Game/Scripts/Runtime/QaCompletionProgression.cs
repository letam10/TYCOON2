using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator CompletionProgression()
        {
            string path = GameSession.Argument("--qa-purchase-fixture", "");
            var fixture = JsonUtility.FromJson<MilestoneFixture>(File.ReadAllText(path));
            var evidence = new MilestoneEvidence();
            foreach (var row in fixture.cases)
            {
                Time.timeScale = 0;
                SeedMilestone(row);
                game.Milestone = 0;
                game.Commerce.enabled = true;
                game.Restaurant.enabled = true;
                foreach (var producer in game.Producers) producer.enabled = true;
                foreach (var machine in game.Machines) machine.enabled = true;
                yield return RecoveryReady();
                game.Commerce.enabled = false;
                game.Restaurant.enabled = false;
                game.Player.CanControl = true;
                Time.timeScale = fixture.timeScale;
                if (row.id == "supermarket")
                {
                    yield return CompletionWait(() => new[] { "mill", "cheesemaker", "saucemaker" }
                        .All(id => game.Progression.RecipeBatches(id) >= 50), 15,
                        "Three actual Processing recipes cross batch 49 to 50");
                }
                string item = row.id == "restaurant" ? "bread" : "carrot";
                int quantity = row.id == "restaurant" ? 1 : 2;
                if (item == "bread")
                {
                    var oven = game.Machines.First(m => m.Id == "machine_oven");
                    yield return CompletionWait(() => oven.Inventory.Available("bread") > 0, 15,
                        "Near threshold oven produces a real bread batch");
                    game.SelectedItem = Array.FindIndex(Definitions.Items, i => i.id == "bread");
                    yield return Travel(Near(oven));
                    yield return CompletionWait(() => game.Player.Carry.Count("bread") >= quantity, 5,
                        "Player picks bread from the actual oven output");
                }
                else yield return FinalGetItem(item, quantity);
                var counter = game.Checkouts.First(c => c.ShopId ==
                    (row.id == "restaurant" ? "bakery" : "farm"));
                var customer = TownCustomer(counter, item, quantity);
                int beforeSale = game.Economy.Money;
                int beforeCollected = game.Economy.CashCollected;
                yield return Travel(Near(counter));
                yield return CompletionWait(() => customer.Order.Paid, 12,
                    "Real player completes boundary order for " + row.id);
                Check(game.Economy.Money == beforeSale && counter.Cash > 0,
                    "Delivery leaves money at counter before " + row.id);
                yield return Travel(counter.CollectionPoint);
                yield return CompletionWait(() => game.Economy.Money > beforeSale, 5,
                    "Player collects the last boundary cash for " + row.id);
                int beforePurchase = game.Economy.Money;
                yield return FinalStash();
                var pad = game.Stations.OfType<PurchasePad>().First(p => p.Upgrade.id == row.id);
                Check(game.CanPurchase(pad.Upgrade, out _), "All progression gates passed for " + row.id);
                yield return Travel(pad.Center);
                yield return CompletionWait(() => game.Economy.Has(row.id), 20,
                    "Player completes purchase " + row.id);
                Check(game.Economy.Money == beforePurchase - pad.Upgrade.cost,
                    "Purchase spends exact price once for " + row.id);
                yield return WalkTo(pad.Center + Vector3.back * 2);
                evidence.results.Add(new MilestoneRow
                {
                    id = row.id,
                    seedWallet = row.wallet,
                    collected = game.Economy.CashCollected - beforeCollected,
                    beforePurchase = beforePurchase,
                    afterPurchase = game.Economy.Money,
                    purchased = true
                });
                game.SaveGame();
            }
            File.WriteAllText(Path.Combine(game.QaDirectory, "purchase-boundaries.json"),
                JsonUtility.ToJson(evidence, true));
        }
    }
}
