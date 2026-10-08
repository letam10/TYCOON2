using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

namespace Tycoon
{
    public sealed partial class GameHud : MonoBehaviour
    {
        void RefreshPlayingHud()
        {
            SetText(cashInHand, game.Economy.CashInHand.ToString("N0"));
            SetText(cashInSafe, game.Economy.CashInSafe.ToString("N0"));
            string area = game.BusinessStage switch
            {
                5 => "Nhà hàng & tiệm bánh", 4 => "Tiệm bánh", 3 => "Siêu thị",
                2 => game.Economy.Has("mill") ? "Chế biến" : "Cửa hàng nông sản", _ => "Nông trại & cửa hàng"
            };
            SetText(progress, area + "\n" + game.Workers.Count + " nhân viên • " + game.Economy.Transactions + " đơn");
            var carried = game.Player.Carry.Snapshot();
            string carryText = game.Economy.CashInHand > 0
                ? "Đang cầm " + game.Economy.CashInHand.ToString("N0") + " xu\nĐặt vào két để mang hàng"
                : carried.Count == 0 && game.Player.HasCarry
                    ? "Đang mang thùng hàng\nĐặt thùng tại kho đích"
                    : (carried.Count == 0 ? "Tay trống" : Definitions.Item(carried[0].id).label) + "  " +
                        game.Player.Carry.Total + "/" + game.Player.Carry.Capacity +
                        "\nChọn: " + Definitions.Items[game.SelectedItem].label;
            SetText(carry, carryText);
            float capacity = game.Player.Carry.Capacity > 0
                ? (float)game.Player.Carry.Total / game.Player.Carry.Capacity : 0;
            carryFill.rectTransform.anchorMax = new(Mathf.Clamp01(capacity), 1);
            carryFill.enabled = capacity > 0;
            var active = game.Player.ActiveInteraction as ProximityTarget;
            bool canSelectRecipe = AllowsPlayerControl && game.CanSimulate
                && active?.Target is MachineStation machine && machine.Options.Length > 1;
            recipeButton.gameObject.SetActive(canSelectRecipe);
            var station = game.NearestStation(game.Player.transform.position);
            string reason = modOpen ? "Đóng menu mod để tiếp tục." :
            !game.CanSimulate ? "Gameplay đang tạm dừng • mở menu để kiểm tra lưu game." :
            !string.IsNullOrEmpty(game.Player.InteractionReason) ? game.Player.InteractionReason :
            active != null ? active.Cash ? "Thu tiền tại cọc tiền" : active.Target.Prompt :
            game.Player.ActiveInteraction is PurchasePad pad ? pad.Prompt :
            game.Player.ActiveInteraction is StationZone zone ? zone.Prompt :
            game.Player.ActiveInteraction is SafeInteractionArea safe ? safe.Hint :
            game.Player.ActiveInteraction is CargoPlayerArea cargo ? cargo.Hint :
            station ? station.Prompt : "Dừng 0,25 giây gần vật thể để bắt đầu";
            SetText(prompt, reason);
            prompt.color = Art.Hex(string.IsNullOrEmpty(game.Player.InteractionReason)
                && game.CanSimulate ? Ink : "#FFE5A4");
            SetText(help, Gamepad.current != null ?
            "Cần trái: di chuyển    RB: chọn hàng    Start: lưu    Back: menu" :
            "WASD: di chuyển    Dừng 0,25 giây: thao tác    Q: chọn hàng    F5: lưu    Esc: menu");
        }
        void RefreshStatus()
        {
            SetText(objective, game.ObjectiveText);
            SetText(finance, "Chưa thu  " + game.Economy.PendingCash.ToString("N0") + " xu\nDoanh thu  " +
            game.Economy.Revenue.ToString("N0") + " xu\nĐã thu  " + game.Economy.CashCollected.ToString("N0") +
            " xu\nThất thoát  " + game.Economy.LostItems + " món");
            var nearby = game.Stations.Where(x => x && x.IsUnlocked
                && x is not StationZone and not PurchasePad and not ConveyorStation)
            .OrderBy(x => (x.transform.position - game.Player.transform.position).sqrMagnitude).FirstOrDefault();
            string area = nearby?.AreaId ?? "farm";
            var storage = game.StorageFor(area);
            if (!storage || !storage.IsUnlocked)
            {
                area = "farm";
                storage = game.StorageFor(area);
            }
            stockLines.Clear();
            stockLines.AppendLine("KHO • " + AreaLabel(area));
            if (storage)
            {
                int rows = 0;
                foreach (var item in Definitions.Items.OrderByDescending(
                    x => x.id == Definitions.Items[game.SelectedItem].id))
                {
                    int total = storage.Inventory.Count(item.id), held = storage.Inventory.Reserved(item.id),
                        incoming = storage.Inventory.ReservedSpace(item.id);
                    if (total + held + incoming == 0 && item.id != Definitions.Items[game.SelectedItem].id) continue;
                    if (++rows > 3)
                    {
                        stockLines.AppendLine("Mở Kho để xem toàn bộ hàng");
                        break;
                    }
                    stockLines.Append(item.label).Append("  ").Append(total);
                    if (held + incoming > 0)
                        stockLines.Append(" • giữ ").Append(held).Append(" / đến ").Append(incoming);
                    stockLines.AppendLine();
                }
                stockLines.Append("Sức chứa  ").Append(storage.Inventory.Total)
                    .Append("/").Append(storage.Inventory.Capacity);
            }
            SetText(stock, stockLines.ToString());
            if (mode == ScreenMode.Crew)
            foreach (var row in crewStatuses)
            SetText(row.Value, string.Join(" • ", game.Workers.Where(x => x
                && x.UpgradeId == row.Key).Select(x => x.Reason).Distinct().Take(3)));
            if (mode == ScreenMode.Stock) RefreshStockCounts();
        }
        void CycleRecipe()
        {
            if (game.Player.ActiveInteraction is not ProximityTarget target
                || target.Target is not MachineStation machine || machine.Running) return;
            int i = System.Array.IndexOf(machine.Options, machine.Recipe.id);
            if (game.Transactions.SelectRecipe(machine, machine.Options[(i
                + 1) % machine.Options.Length])) game.Say("Chuẩn bị: " + machine.Recipe.label);
        }
    }
}
