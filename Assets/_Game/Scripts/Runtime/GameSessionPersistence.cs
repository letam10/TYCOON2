using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class GameSession
    {
        public void LoadGame()
        {
            FileStream recoveryLease = null;
            try
            {
                if (!File.Exists(SavePath)) return;
                Transactions?.Detach();
                Transactions = null;
                // Giữ khóa trước khi đọc/migrate; chuyển nguyên handle sang store để tránh khoảng hở.
                recoveryLease = GameplayTransactionStore.AcquireLease(SavePath);
                var data = SaveStore.Read(SavePath);
                if (data == null) return;
                IsRestoring=Application.isPlaying;Player.CanControl=false;
                bool worldChanged = PhysicalCashMigration.Upgrade(data);
                worldChanged |= TownLayout.Migrate(data);
                worldChanged |= RemoveRetiredPurchasePads(data);
                worldChanged |= EnsureWorldProjection(data);
                worldChanged |= WorkforceMigration.Apply(data);
                if(data.transactionState!=null)
                {
                    worldChanged|=RuntimeTransactions.ReconcileWorld(data.transactionState,this);
                    if (worldChanged) RuntimeTransactions.Project(data.transactionState, data);
                }
                ValidateSaveOwners(data);
                if (worldChanged && data.transactionState != null) SaveStore.Write(SavePath, data);
                Player.StopInteraction();
                foreach(var worker in Workers){worker.gameObject.SetActive(false);Destroy(worker.gameObject);}
                Workers.Clear();Commerce?.ResetForLoad();Restaurant?.ResetForLoad();
                foreach(var counter in Checkouts){counter.Queue.Clear();counter.Cash=0;}
                foreach(var table in Tables)table.Occupant=null;
                Economy.Restore(data.money, data.revenue, data.transactions, data.unlocked, data.pendingCash, data.receipts, data.losses,data.cashCollected);
                Economy.RestoreCash(data.cashInHand,data.cashInSafe);
                Purchases=data.purchases; CrewStates=data.crews; Events=data.events ?? new EventState();
                PendingCustomers=data.customers; PendingWorkers=data.workers;
                BakerySales = data.bakerySales; RestaurantMeals = data.restaurantMeals;
                PendingDiners = data.diners;
                NextReceipt = Math.Max(1, data.nextReceipt);
                foreach(long receipt in data.receipts)NextReceipt=Math.Max(NextReceipt,receipt+1);
                foreach(var loss in data.losses)NextReceipt=Math.Max(NextReceipt,loss.receipt+1);
                foreach(var customer in data.customers)NextReceipt=Math.Max(NextReceipt,customer.receipt+1);
                foreach(var diner in data.diners)NextReceipt=Math.Max(NextReceipt,diner.receipt+1);
                foreach (var inventory in data.inventories)
                {
                    if (inventory.id == "player") { Player.Carry.Restore(inventory.items); continue; }
                    var station = Stations.Find(x => x.Id == inventory.id);
                    station?.Inventory?.Restore(inventory.items);
                    if (inventory.id.EndsWith("_input") && Stations.Find(x => x.Id + "_input" == inventory.id) is MachineStation machine) machine.Input.Restore(inventory.items);
                }
                // Save v2 giữ hàng trên đúng chủ sở hữu; không hoàn hàng khách vào kho.
                foreach(var progress in data.stationStates) Stations.Find(x=>x.Id==progress.id)?.RestoreProgress(progress);
                foreach (var cash in data.cash) if (Stations.Find(x => x.Id == cash.id) is CheckoutStation checkout) checkout.Cash = cash.amount;
                if(Player.Controller)Player.Controller.enabled = false;
                Player.transform.position = new Vector3(data.playerX, .05f, data.playerZ);
                if(Player.Controller)Player.Controller.enabled = true;
                ApplyProgression();
                SaveBlocked=false;Player.CanControl=false;
                if (data.transactionState != null)
                {
                    InitializeTransactions(data.transactionState, existingLease: recoveryLease);
                    recoveryLease = null;
                }
                CameraRig?.Snap();
                UpgradeModelView.RefreshAll(this,false);
                foreach (var station in Stations)
                    if (station is PurchasePad pad) pad.RestoreAfterLoad();
                SaveBlocked=false;
                Player.CanControl=!IsRestoring;
                Say("Đã tải trò chơi");
            }
            catch (Exception error) { BlockRecovery(error); }
            finally { recoveryLease?.Dispose(); }
        }
        bool EnsureWorldProjection(SaveData data)
        {
            bool changed=false;
            foreach(var station in Stations.Where(x=>x is not StationZone))
            {
                if(station.Inventory!=null&&!data.inventories.Exists(x=>x.id==station.Id))
                {data.inventories.Add(new InventorySave(station.Id,new Inventory(station.Inventory.Capacity,station.Inventory.SingleItem)));changed=true;}
                if(station is MachineStation machine&&!data.inventories.Exists(x=>x.id==station.Id+"_input"))
                {data.inventories.Add(new InventorySave(station.Id+"_input",new Inventory(machine.Input.Capacity,machine.Input.SingleItem)));changed=true;}
                if(!data.stationStates.Exists(x=>x.id==station.Id)){data.stationStates.Add(station.CaptureProgress());changed=true;}
                if(station is CheckoutStation checkout&&!data.cash.Exists(x=>x.id==checkout.Id))
                {data.cash.Add(new CashSave{id=checkout.Id,amount=0});changed=true;}
            }
            return changed;
        }
        void ValidateSaveOwners(SaveData data)
        {
            if(data.transactionState!=null)
            {
                TransactionCore.Validate(data.transactionState);
                foreach(var state in data.transactionState.stations)
                    if(!Stations.Exists(x=>x is not StationZone&&x.Id==state.id))throw new InvalidDataException("Station reference không còn tồn tại: "+state.id);
                if(data.transactionState.money!=data.money||data.transactionState.cashInHand!=data.cashInHand||data.transactionState.cashInSafe!=data.cashInSafe)throw new InvalidDataException("Core money không khớp save projection.");
            }
            if(data.transit.Count>0)throw new InvalidDataException("Transit prototype không có owner; không tự trả hàng về kho.");
            var owners=new Dictionary<string,Inventory>{{"player",Player.Carry}};
            var stations=new Dictionary<string,Station>();
            foreach(var station in Stations)
            {
                if(station is StationZone)continue;
                if(string.IsNullOrEmpty(station.Id)||!stations.TryAdd(station.Id,station))throw new InvalidDataException("Stable ID trạm bị thiếu hoặc lặp.");
                if(station.Inventory!=null&&!owners.TryAdd(station.Id,station.Inventory))throw new InvalidDataException("Owner inventory bị lặp.");
                if(station is MachineStation machine&&!owners.TryAdd(station.Id+"_input",machine.Input))throw new InvalidDataException("Owner đầu vào máy bị lặp.");
            }
            if(data.inventories.Count!=owners.Count)throw new InvalidDataException("Save thiếu owner inventory.");
            foreach(var inventory in data.inventories)
            {
                if(!owners.TryGetValue(inventory.id,out var target))throw new InvalidDataException("Owner không còn tồn tại: "+inventory.id);
                new Inventory(int.MaxValue,target.SingleItem).Restore(inventory.items);
            }
            var ids=new HashSet<string>();
            if(data.stationStates.Count!=stations.Count)throw new InvalidDataException("Save thiếu trạng thái trạm.");
            foreach(var state in data.stationStates)
                if(state==null||!stations.ContainsKey(state.id)||!ids.Add(state.id)||float.IsNaN(state.remaining)||float.IsInfinity(state.remaining))
                    throw new InvalidDataException("Trạng thái trạm không hợp lệ.");
            foreach(var cash in data.cash)if(!Checkouts.Exists(c=>c.Id==cash.id))throw new InvalidDataException("Quầy tiền không còn tồn tại.");
            var receipts=new HashSet<long>();
            foreach(var customer in data.customers)
            {
                if(customer==null||customer.receipt<=0||customer.receipt==long.MaxValue||!receipts.Add(customer.receipt)||
                    !Checkouts.Exists(c=>c.Id==customer.lane&&c.ShopId==customer.shop))
                    throw new InvalidDataException("Owner khách không hợp lệ.");
                new Inventory(int.MaxValue).Restore(customer.basket);
            }
            foreach(var diner in data.diners)
            {
                if(diner==null||diner.receipt<=0||diner.receipt==long.MaxValue||!receipts.Add(diner.receipt)||!Tables.Exists(t=>t.Id==diner.table))
                    throw new InvalidDataException("Owner khách bàn không hợp lệ.");
                new Inventory(int.MaxValue).Restore(diner.basket);
            }
            ids.Clear();
            foreach(var worker in data.workers)
            {
                if(worker==null||string.IsNullOrEmpty(worker.id)||!ids.Add(worker.id)||!data.crews.Exists(c=>c.id==worker.upgrade)||
                    worker.phase<0||worker.phase>2||worker.count<0||
                    (worker.phase>0&&(!stations.ContainsKey(worker.destination?.Replace("_input","")??"")||
                    (worker.phase==1&&!stations.ContainsKey(worker.source??"")))))
                    throw new InvalidDataException("Owner nhân viên hoặc tuyến vận chuyển không hợp lệ.");
                new Inventory(int.MaxValue,true).Restore(worker.carry);
            }
            if(data.nextReceipt==long.MaxValue||float.IsNaN(data.playerX)||float.IsNaN(data.playerZ)||
                float.IsInfinity(data.playerX)||float.IsInfinity(data.playerZ))
                throw new InvalidDataException("Vị trí hoặc số biên nhận không hợp lệ.");
        }
    }
}
