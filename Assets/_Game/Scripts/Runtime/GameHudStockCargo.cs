using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace Tycoon
{
    public sealed partial class GameHud : MonoBehaviour
    {
        void OpenStock()
        {
            if (!game.CanSimulate) return;
            ShowScreen(ScreenMode.Stock);
            foreach (Transform child in stockContent)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            stockRows.Clear();
            Button first = null;
            int row = 0;
            foreach (var storage in game.Stations.OfType<StorageStation>().Where(x => x.IsUnlocked))
            foreach (var item in Definitions.Items.Where(x => game.Progression.CanProduce(x.id)
                || storage.Inventory.Count(x.id) > 0))
            {
                var line = Panel("StockRow", stockContent, Card);
                Rect((RectTransform)line.transform, new(0, 1), new(1, 1), new(0, -row * 96 - 88), new(0, -row * 96));
                var select = line.AddComponent<Button>();
                StyleButton(select);
                string sku = item.id;
                select.onClick.AddListener(() =>
                {
                    game.Player.StopInteraction(); game.SelectedItem = System.Array.FindIndex(Definitions.Items,
                        x => x.id == sku);
                    CloseStock();
                }
                );
                var label = Text("StockRowText", line.transform, "", 22, TextAnchor.MiddleLeft, Ink);
                Inset(label.rectTransform, 18);
                stockRows.Add((storage, sku, label));
                first ??= select;
                row++;
            }
            if (row == 0)
            {
                var empty = Text("EmptyStock", stockContent, "Chưa có kho hoặc mặt hàng đã mở.", 24,
                    TextAnchor.MiddleCenter, Muted);
                Rect(empty.rectTransform, new(0, 1), new(1, 1), new(0, -120), Vector2.zero);
            }
            ((RectTransform)stockContent).sizeDelta = new(0, Mathf.Max(120, row * 96));
            stockContent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 1;
            RefreshStockCounts();
            Select(first ? first : stockClose);
        }
        void RefreshStockCounts()
        {
            foreach (var row in stockRows)
            SetText(row.label, AreaLabel(row.storage.AreaId) + " • " + Definitions.Item(row.item).label +
            "  " + row.storage.Inventory.Count(row.item) + "\nGiữ chỗ: " + row.storage.Inventory.Reserved(row.item) +
            " • Đang đến: " + row.storage.Inventory.ReservedSpace(row.item) + " • Sức chứa kho: "
                + row.storage.Inventory.Capacity);
        }
        void CloseStock()
        {
            ShowScreen(ScreenMode.Play);
        }
        void OpenCargo()
        {
            if (!game.CanSimulate) return;
            ShowScreen(ScreenMode.Cargo);
            RefreshCargo();
            Select(game.Transactions?.View.truck != null ? cargoActions[0] : cargoClose);
        }
        void CloseCargo()
        {
            ShowScreen(ScreenMode.Play);
        }
        void RefreshCargo()
        {
            var truck = game.Transactions?.View.truck;
            foreach (var button in cargoActions) button.interactable = truck != null && game.CanSimulate;
            SetText(cargoText, truck == null ?
                "Xe tải + tài xế: mở khu Chế biến, kho cấp 3 và tự chở hàng 30 chuyến.\n" +
                "Góp tiền tại điểm mua xe bên kho Chế biến." :
            "Nguồn: " + CargoWarehouse(truck.source) + " → Đích: " + CargoWarehouse(truck.destination) + "\n" +
            (truck.phase switch
            {
                "Idle" => "Đỗ tại bến", "Loading" => "Đang chất hàng", "Travelling" => "Đang vận chuyển",
                    _ => "Chờ dỡ hàng"
            }
            ) +
            " • Tự động: " + (truck.repeat ? "BẬT" : "TẮT") + "\n" + game.Logistics.Reason + "\n" +
            string.Join(" • ", game.Transactions.View.crates.Where(x => x.holder == truck.id).Select(x =>
            Definitions.Item(x.item).label + " ×"
                + game.Transactions.View.stacks.Where(s => s.owner == x.id).Sum(s => s.quantity))));
        }
        string CargoWarehouse(string id) => AreaLabel(game.Stations.Find(x => x.Id == id)?.AreaId ?? id);
        void CycleCargo(bool source)
        {
            var truck = game.Transactions?.View.truck;
            if (truck == null) return;
            var options = game.Stations.OfType<StorageStation>().Where(x => x.IsUnlocked).Select(x => x.Id).ToArray();
            if (options.Length < 2) return;
            int index = System.Array.IndexOf(options, source ? truck.source : truck.destination);
            for (int i = 1; i <= options.Length; i++)
            {
                string id = options[(index + i) % options.Length];
                if (id == (source ? truck.destination : truck.source)) continue;
                game.Logistics.Select(source ? id : truck.source, source ? truck.destination : id, truck.repeat);
                break;
            }
            RefreshCargo();
        }
    }
}
