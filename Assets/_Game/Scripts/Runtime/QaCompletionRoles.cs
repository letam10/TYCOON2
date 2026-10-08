using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        [Serializable]
        sealed class CompletionLivestockFixture
        {
            public string station;
            public int population;
            public float timeoutSeconds;
        }

        IEnumerator CompletionRoleFlows()
        {
            yield return CompletionReset(new[] { "farmer" });
            var farm = game.StorageFor("farm");
            int carrots = farm.Inventory.Count("carrot");
            foreach (var producer in game.Producers.Where(p => !p.Animal)) producer.enabled = true;
            yield return CompletionWait(() => farm.Inventory.Count("carrot") > carrots, 90,
                "Farmer sows waters harvests and stores actual carrots");

            yield return CompletionReset(new[] { "animal_worker" }, "machine_feedmill");
            Time.timeScale = 0;
            var fixture = JsonUtility.FromJson<CompletionLivestockFixture>(File.ReadAllText(
                GameSession.Argument("--qa-livestock-fixture", "")));
            var animal = game.Producers.First(p => p.Id == fixture.station);
            var state = game.Transactions.Snapshot();
            var data = game.CaptureSaveData();
            foreach (var producer in game.Producers)
            {
                if (producer != animal) producer.Requirement = "qa:inactive";
                state.stations.First(s => s.id == producer.Id).requirement = producer.Requirement;
            }
            var livestock = state.stations.First(s => s.id == animal.Id).progress;
            livestock.herd = fixture.population;
            livestock.feed = 0;
            livestock.breeding = 0;
            CompletionWriteFixture(state, data);
            game.Commerce.enabled = true;
            game.Restaurant.enabled = true;
            yield return RecoveryReady();
            game.Commerce.enabled = false;
            game.Restaurant.enabled = false;
            game.Player.CanControl = false;
            animal.enabled = true;
            Time.timeScale = 3;
            farm = game.StorageFor("farm");
            bool restocked = false;
            yield return CompletionWait(() =>
            {
                restocked |= animal.Herd > 0;
                return farm.Inventory.Count("beef") > 0;
            }, fixture.timeoutSeconds, "Animal worker restocks zero herd and delivers real meat");
            Check(restocked, "A real population exists before meat is harvested");
            Check(animal.WorkCount > 0 && game.Workers.Any(w => w.Deliveries > 0),
                "Animal care completes production and storage delivery");

            yield return CompletionReset(new[] { "transport_bakery" }, "machine_breadmixer");
            var bakery = game.StorageFor("bakery");
            var processing = game.StorageFor("processing");
            int flour = bakery.Inventory.Available("flour");
            Check(game.Transactions.Transfer(bakery.Inventory, processing.Inventory, "flour", flour) == flour,
                "Cross area fixture keeps flour in the Processing owner");
            yield return CompletionWait(() => bakery.Inventory.Count("bread_dough") > 0, 120,
                "Transport worker brings cross area inputs and stores mixer output");
            Check(game.Workers.Single(w => w.Role == "Transporter").Deliveries >= 4,
                "Transport completes distinct input and output deliveries");

            yield return CompletionReset(new[] { "cook", "waiter" }, "machine_kitchen");
            var diner = CompletionDiner("corn_soup");
            var table = diner.Table;
            yield return CompletionWait(() => diner.Phase == 2, 85,
                "Cook and waiter prepare and serve the correct table before patience expires");
            Check(diner.Basket.Count("corn_soup") == 1, "Diner owns exactly the requested dish");
            yield return CompletionWait(() => table.Cleaning > 0, 10, "Eating creates a dirty table");
            Check(table.Occupant == null, "Diner releases table only after eating");
            yield return CompletionWait(() => table.Cleaning == 0, 20, "Waiter cleans before table reuse");
            var bank = game.Checkouts.First(c => c.ShopId == "restaurant");
            Check(bank.Cash > 0, "Restaurant crew leaves payment at the collection point");
            long money = game.Economy.Money;
            game.Player.CanControl = true;
            yield return Travel(bank.CollectionPoint);
            yield return CompletionWait(() => game.Economy.Money > money, 5,
                "Only player collection moves restaurant payment into the wallet");
            game.Player.CanControl = false;
        }

        DinerAgent CompletionDiner(string item)
        {
            var table = game.Tables.First(t => t.IsUnlocked && !t.Occupant && t.Cleaning == 0);
            var root = new GameObject("CompletionDiner");
            root.transform.SetParent(game.transform);
            root.transform.position = table.Seat;
            Art.Model("customer", Vector3.zero, root.transform).AddComponent<ActorView>();
            var diner = root.AddComponent<DinerAgent>();
            diner.Initialize();
            var data = new DinerSave
            {
                receipt = game.NextReceipt++, table = table.Id, item = item, phase = 1,
                remaining = 90, price = game.ItemPrice(item), x = table.Seat.x, z = table.Seat.z
            };
            game.Transactions.BindDiner(diner, data, true);
            diner.Begin(table, data);
            game.Restaurant.Diners.Add(diner);
            return diner;
        }
    }
}
