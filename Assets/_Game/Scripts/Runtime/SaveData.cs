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
        public TransactionState transactionState;
        public int transactionVersion;
        public int contentVersion;
        public int version = 2;
        public int money;
        public int revenue;
        public int cashCollected;
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
    [Serializable] public sealed class DinerSave { public string table,item; public long receipt; public int phase; public float remaining,x,z; public int price; public List<ItemAmount> basket = new(); }

    public static class SaveStore
    {
        public const int CurrentTransactionVersion = 2;
        public static void Write(string path, SaveData data, Action<CommitBoundary> fault = null)
        {
            Validate(data);
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string temporary = path + ".tmp";
            try
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, true));
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                fault?.Invoke(CommitBoundary.TemporaryFlushed);
                // Replace không truyền backup path: ghi nguyên tử mà không tạo .bak.
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                fault?.Invoke(CommitBoundary.Replaced);
            }
            finally { if(File.Exists(temporary))File.Delete(temporary); }
        }
        public static SaveData Read(string path)
        {
            if (!File.Exists(path)) return null;
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            // Unity có thể tạo inline class rỗng khi JSON thiếu field; marker phân biệt save v2 cũ.
            if(data!=null&&data.transactionVersion==0)data.transactionState=null;
            if(data?.transactionVersion==1&&data.transactionState!=null)
            {
                data.transactionState=TransactionCore.NormalizeState(data.transactionState);
                data.transactionVersion=CurrentTransactionVersion;
            }
            if (data == null || data.version != 2 || data.money < 0 || data.inventories == null || data.unlocked == null)
                throw new InvalidDataException("Save không hợp lệ hoặc phiên bản chưa hỗ trợ.");
            data=GameplayTransactionStore.Recover(path,data);
            Validate(data);
            return data;
        }
        static void Validate(SaveData data)
        {
            if(data==null||data.version!=2||data.money<0||data.pendingCash<0||data.revenue<0||data.cashCollected<0||data.transactions<0||
                data.inventories==null||data.receipts==null||data.losses==null||data.cash==null||
                data.customers==null||data.diners==null||data.workers==null||data.stationStates==null||
                data.purchases==null||data.crews==null||data.unlocked==null||data.transit==null)
                throw new InvalidDataException("Save v2 không hợp lệ.");
            var ids=new HashSet<string>();
            foreach(var inventory in data.inventories)
            {
                if(inventory==null||string.IsNullOrEmpty(inventory.id)||inventory.items==null||!ids.Add(inventory.id))
                    throw new InvalidDataException("Owner inventory bị thiếu hoặc lặp.");
                new Inventory(int.MaxValue).Restore(inventory.items);
            }
            var receipts=new HashSet<long>();
            foreach(long receipt in data.receipts)
                if(receipt<=0||receipt==long.MaxValue||!receipts.Add(receipt))throw new InvalidDataException("Biên nhận bị lặp hoặc không hợp lệ.");
            foreach(var loss in data.losses)
            {
                if(loss==null||loss.receipt<=0||loss.receipt==long.MaxValue||loss.goods==null||!receipts.Add(loss.receipt))
                    throw new InvalidDataException("Biên nhận thanh toán và thất thoát mâu thuẫn.");
                new Inventory(int.MaxValue).Restore(loss.goods);
            }
            ids.Clear();long cash=0;
            foreach(var counter in data.cash)
            {
                if(counter==null||string.IsNullOrEmpty(counter.id)||counter.amount<0||!ids.Add(counter.id))
                    throw new InvalidDataException("Quầy tiền bị thiếu hoặc lặp.");
                cash+=counter.amount;
            }
            if(cash!=data.pendingCash)throw new InvalidDataException("Tiền tại quầy không khớp sổ tiền chờ thu.");
            if(data.transactionVersion is <0 or >CurrentTransactionVersion)throw new InvalidDataException("Transaction save version chưa hỗ trợ.");
            if(data.transactionVersion==CurrentTransactionVersion&&data.transactionState==null)throw new InvalidDataException("Save v2 thiếu transaction state.");
            if (data.transactionVersion==CurrentTransactionVersion) TransactionCore.Validate(data.transactionState);
        }
    }
}
