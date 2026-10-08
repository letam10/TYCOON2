using System.Collections;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        void PhysicalCarryFixture(TransactionState baseline, string item, int count, long cash = 0)
        {
            // Fixture chỉ phục vụ ảnh từng model; không được tính là tiến trình chơi tự nhiên.
            var state = TransactionCore.Copy(baseline);
            state.stacks.RemoveAll(x => x.owner == "player");
            state.cashInHand = cash;
            if (count > 0)
            {
                var owner = state.owners.Single(x => x.id == "player");
                state.stacks.Add(new ItemStackState
                {
                    id = "qa:carry:" + item,
                    owner = owner.id,
                    location = owner.location,
                    item = item,
                    quantity = count
                });
            }
            TransactionCore.Validate(state);
            game.Transactions.Detach();
            game.InitializeTransactions(state, false);
            foreach (var worker in game.Workers)
                game.Transactions.BindWorker(worker, worker.Snapshot());
            foreach (var customer in game.Commerce.Customers)
                if (customer.Order != null && state.owners.Any(x =>
                    x.id == RuntimeTransactions.CustomerId(customer.Receipt)))
                    game.Transactions.BindCustomer(customer, customer.Snapshot(), false);
            foreach (var diner in game.Restaurant.Diners)
                if (state.owners.Any(x => x.id == RuntimeTransactions.CustomerId(diner.Receipt)))
                    game.Transactions.BindDiner(diner, diner.Snapshot(), false);
            // Fixture đổi trực tiếp ảnh chụp dữ liệu nên phải xóa cache model, giữ nguyên journal hợp lệ.
            foreach (var stack in game.Player.GetComponentsInChildren<InventoryStack>())
            {
                stack.enabled = false;
                stack.enabled = true;
            }
        }

        IEnumerator PhysicalVisualAcceptance()
        {
            yield return PhysicalActorCollisionAcceptance();
            game.Player.StopInteraction();
            game.Player.CanControl = false;
            var baseline = game.Transactions.Snapshot();
            Time.timeScale = 0;
            game.CameraRig.Distance = 8;
            game.CameraRig.FocusOffset = new Vector3(0, .6f, .3f);
            game.CameraRig.Snap();
            yield return PhysicalBubbleScreens();
            foreach (var definition in Definitions.Items)
            {
                foreach (int count in new[] { 1, 24, 48 })
                {
                    PhysicalCarryFixture(baseline, definition.id, count);
                    yield return null;
                    yield return null;
                    var stack = game.Player.GetComponentsInChildren<InventoryStack>()
                        .First(x => x.Inventory == game.Player.Carry);
                    Check(stack.VisibleCount == count, "physical model count: " + definition.id + " x" + count);
                    Check(ItemIconAtlas.Get(definition.id), "physical drawn icon: " + definition.id);
                    yield return Capture("carry-" + definition.id + "-" + count + ".png");
                }
            }
            PhysicalCarryFixture(baseline, null, 0, 5000000000L);
            yield return null;
            yield return Capture("cash-5000000000.png");
            Check(game.Economy.CashInHand == 5000000000L && game.Player.Carry.Total == 0,
                "physical: unlimited cash is 64 bit and exclusive");
            game.CameraRig.Distance = 18;
            game.CameraRig.FocusOffset = new Vector3(2, .8f, 3);
            game.CameraRig.Snap();
            yield return PhysicalResolutionAcceptance();
            // Trả lại checkpoint chơi trước các fixture hình ảnh.
            game.LoadGame();
            yield return TownWait(() => game.CanSimulate, 8, "restore actors after visual fixtures");
            Check(!game.SaveBlocked, "physical: restores saved world after model fixtures");
            game.Player.CanControl = true;
        }
    }
}
