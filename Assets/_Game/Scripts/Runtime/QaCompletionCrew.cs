using System.Collections;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator CompletionCrews()
        {
            yield return CompletionProcessingCrew();
            yield return CompletionBakeryCrew();
            yield return CompletionMarketCrew();
            yield return CompletionCrewShortage();
            yield return CompletionCrewFullShelf();
        }

        IEnumerator CompletionProcessingCrew()
        {
            string[] machines = { "machine_mill", "machine_cheesemaker", "machine_saucemaker" };
            yield return CompletionReset(new[] { "processor" }, machines);
            var stock = game.StorageFor("processing");
            var targets = machines.Select(id => game.Machines.First(m => m.Id == id)).ToArray();
            var batches = targets.Select(m => m.Batches).ToArray();
            var outputs = targets.Select(m => stock.Inventory.Count(m.Recipe.output)).ToArray();
            Check(outputs.All(n => n == 0), "processing fixture has no pre-made recipe outputs");
            int expectedWorkers = 1;
            if (game.Economy.Money >= game.CrewUpgradeCost("processor", "count"))
            {
                Check(game.UpgradeCrew("processor", "count"), "second processor purchased through crew transaction");
                expectedWorkers = 2;
            }
            yield return CompletionWait(
                () => game.Workers.Count(w => w.UpgradeId == "processor") == expectedWorkers,
                5, "processing actors match purchased crew count");
            yield return CompletionWait(
                () => targets.Select((m, i) => m.Batches > batches[i] &&
                    stock.Inventory.Count(m.Recipe.output) >= outputs[i] + m.Recipe.yield).All(done => done),
                180, "processors load, produce and unload flour, cheese and sauce");
            foreach (var worker in game.Workers.Where(w => w.UpgradeId == "processor"))
            {
                Check(worker.Deliveries > 0, "processor participates in actual transport: " + worker.WorkerId);
                Check(worker.Agent.isOnNavMesh, "processor remains on NavMesh: " + worker.WorkerId);
            }
            Check(targets.All(m => m.PlayerBatches == 0), "processing batches belong to autonomous worker flow");
            TransactionCore.Validate(game.Transactions.Snapshot());
        }

        IEnumerator CompletionBakeryCrew()
        {
            string[] machines =
            {
                "machine_breadmixer", "machine_cakemixer", "machine_oven", "machine_cakeoven"
            };
            yield return CompletionReset(new[] { "cook_bakery" }, machines);
            var stock = game.StorageFor("bakery");
            var targets = machines.Select(id => game.Machines.First(m => m.Id == id)).ToArray();
            Check(new[] { "bread", "cake", "bread_dough", "cake_batter" }
                .All(item => stock.Inventory.Count(item) == 0), "bakery starts without finished goods or dough");
            yield return CompletionWait(
                () => targets.All(m => m.Batches > 0) && stock.Inventory.Count("bread") >= 3 &&
                    stock.Inventory.Count("cake") >= 2,
                240, "baker runs both mixers and both ovens and returns bread and cake to storage");
            var baker = game.Workers.Single(w => w.UpgradeId == "cook_bakery");
            Check(baker.Deliveries >= 10, "baker physically transports ingredients, dough and finished goods");
            Check(targets.All(m => m.PlayerBatches == 0), "bakery production requires no player operation");
            Check(baker.Agent.isOnNavMesh, "baker remains on NavMesh through both recipes");
            TransactionCore.Validate(game.Transactions.Snapshot());
        }

        IEnumerator CompletionMarketCrew()
        {
            yield return CompletionReset(new[] { "restocker_market", "cashier_market" });
            var shelf = game.Shelves.First(s => s.ShopId == "market" && s.Accepts("carrot"));
            var storage = game.StorageFor("supermarket");
            var holding = game.StorageFor("farm");
            int seeded = shelf.Inventory.Count("carrot");
            if (seeded > 0)
            {
                Check(game.Transactions.Transfer(shelf.Inventory, holding.Inventory, "carrot", seeded) == seeded,
                    "empty market carrot shelf through actual transfer");
            }
            int before = storage.Inventory.Count("carrot");
            Check(before > 0, "market storage supplies stocker acceptance");
            yield return CompletionWait(
                () => shelf.Inventory.Count("carrot") > 0 && storage.Inventory.Count("carrot") < before,
                60, "market stocker moves owned carrots from storage to shelf");
            var cashier = game.Workers.Single(w => w.UpgradeId == "cashier_market");
            yield return CompletionWait(() => cashier.Checkout != null, 5, "cashier chooses its real checkout");
            var counter = cashier.Checkout;
            var buyer = TownCustomer(counter, "carrot", 1);
            long receipt = buyer.Receipt;
            int money = game.Economy.Money;
            int collected = game.Economy.CashCollected;
            int total = CompletionItemTotal("carrot");
            yield return CompletionWait(() => buyer.Order.Paid, 80, "cashier fetches shelf goods and completes order");
            Check(buyer.Basket.Count("carrot") == 1, "customer owns the cashier-delivered carrot");
            Check(CompletionItemTotal("carrot") == total, "cashier delivery conserves total owned carrots");
            string order = RuntimeTransactions.OrderId(receipt);
            var payment = game.Transactions.View.payments.Single(p => p.order == order);
            int amount = payment.amount;
            Check(!payment.collected && counter.Cash == amount && amount > 0,
                "completed market order leaves exactly one uncollected counter payment");
            yield return new WaitForSeconds(2);
            Check(game.Economy.Money == money && game.Economy.CashCollected == collected,
                "market employees cannot collect counter cash");
            game.Player.CanControl = true;
            yield return Travel(counter.CollectionPoint);
            yield return CompletionWait(() => game.Economy.Money == money + amount, 5,
                "player proximity collects the market payment");
            game.Player.CanControl = false;
            Check(counter.Cash == 0 && game.Economy.CashCollected == collected + amount,
                "player collection clears counter and advances collected cash once");
            Check(game.Transactions.View.payments.Single(p => p.order == order).collected,
                "market payment collection is persisted in transaction state");
            TransactionCore.Validate(game.Transactions.Snapshot());
        }

        int CompletionItemTotal(string item)
        {
            return game.Transactions.View.stacks.Where(s => s.item == item).Sum(s => s.quantity);
        }
    }
}
