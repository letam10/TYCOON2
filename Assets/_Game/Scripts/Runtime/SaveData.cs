using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tycoon
{
    [Serializable]
    public sealed class InventorySave
    {
        public string id;
        public List<ItemAmount> items;
        public InventorySave(string id, Inventory inventory) { this.id = id; items = inventory.Snapshot(); }
    }

    [Serializable]
    public sealed class ProductionSave
    {
        public string id;
        public bool running;
        public float remaining;
        public int batches;
    }

    [Serializable]
    public sealed class SaveData
    {
        public int version = 2;
        public int money;
        public int revenue;
        public int transactions;
        public long nextReceipt = 1;
        public string savedAt;
        public float playerX;
        public float playerZ;
        public List<string> unlocked = new();
        public List<InventorySave> inventories = new();
        public List<ProductionSave> production = new();
        public List<ItemAmount> transit = new();
        public int businessStage;
        public int restaurantMeals;
        public int bakerySales;
        public int pendingCash;
        public List<CashSave> cash = new();
        public List<DinerSave> diners = new();
        public List<CustomerSave> customers = new();
        public List<WorkerSave> workers = new();
        public List<StationProgressSave> stationStates = new();
        public List<PurchaseProgress> purchases = new();
        public List<CrewState> crews = new();
        public List<long> receipts = new();
        public List<LossRecord> losses = new();
        public EventState events = new();
    }
    [Serializable] public sealed class CashSave { public string id; public int amount; }
    [Serializable] public sealed class DinerSave { public string table; public long receipt; public int phase; public float remaining,x,z; public int price; public List<ItemAmount> basket = new(); }

    public static class SaveStore
    {
        public static void Write(string path, SaveData data)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
            // Replace không truyền backup path: ghi nguyên tử mà không tạo .bak.
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
        public static SaveData Read(string path)
        {
            if (!File.Exists(path)) return null;
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            if (data == null || data.version != 2 || data.money < 0 || data.inventories == null || data.unlocked == null)
                throw new InvalidDataException("Save không hợp lệ hoặc phiên bản chưa hỗ trợ.");
            return data;
        }
    }
}
