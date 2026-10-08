using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    // Một writer, một khóa và một durable envelope cho toàn bộ thay đổi nghiệp vụ.
    public sealed partial class TransactionCore
    {
        readonly object gate = new();
        readonly ITransactionStore store;
        readonly Func<double> clock;
        readonly bool runtime;
        readonly bool replaying;
        bool legacyWorkforceReplay;
        readonly Dictionary<string,TransactionReceipt> keys=new(),effects=new();
        TransactionState state;
        public CoreLifecycle Lifecycle { get; private set; }
        public Action<CommitBoundary> Fault { get; set; }
        public TransactionCore(TransactionState seed, ITransactionStore store = null, Func<double> clock = null,
            bool runtime = false, bool replaying = false)
        {
            this.store = store;
            this.runtime = runtime;
            this.replaying = replaying;
            this.clock = clock ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d);
            state = NormalizeRecoveryState(
                Copy(store?.Read() ?? seed ?? throw new ArgumentNullException(nameof(seed))), !replaying);
            RefreshMachinePhases(state);
            Validate(state);
            IndexReceipts();
            if (store != null && store.Read() == null) store.Write(state);
        }
        public long Revision { get { lock (gate) return state.revision; } }
        public TransactionState Snapshot() { lock (gate) return Copy(state); }
        internal TransactionState RuntimeSnapshot(){lock(gate)return state.RuntimeCopy(false);}
        void IndexReceipts(){keys.Clear();effects.Clear();foreach(var r in state.receipts){keys.Add(r.key,r);effects.Add(r.effectId,r);}}
        internal static T Copy<T>(T value) => JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        public TransactionReceipt Execute(TransactionCommand command)
        {
            using var executeTiming = QaCpuProbe.Measure(QaCpuProbe.Work.CoreExecute);
            if (command == null) throw new ArgumentNullException(nameof(command));
            using var commandTiming = QaCpuProbe.Measure(command.kind);
            lock (gate)
            {
                Require(Lifecycle == CoreLifecycle.Ready, "recovery", "Core chưa phục hồi xong.");
                var c = Copy(command);
                Require(!string.IsNullOrWhiteSpace(c.key) && !string.IsNullOrWhiteSpace(c.effectId) && !string.IsNullOrWhiteSpace(c.actor), "identity", "Thiếu command, effect hoặc actor ID.");
                string fingerprint = Fingerprint(c, false), effectFingerprint = Fingerprint(c, true);
                keys.TryGetValue(c.key,out var receipt);
                if (receipt != null)
                { Require(receipt.fingerprint == fingerprint, "key-conflict", "Key đã dùng với payload khác."); return Copy(receipt); }
                effects.TryGetValue(c.effectId,out receipt);
                if (receipt != null)
                { Require(receipt.effectFingerprint == effectFingerprint, "effect-conflict", "Effect đã dùng với payload khác."); return Copy(receipt); }
                Require(c.expectedRevision == state.revision, "stale", "State version đã thay đổi.");
                TransactionState draft;
                using (QaCpuProbe.Measure(QaCpuProbe.Work.DraftCopy))
                    draft = runtime ? state.RuntimeCopy(true,c.kind==TransactionKind.AcknowledgeEvent?c.target:null,
                    c.kind is TransactionKind.GrantMaximum or TransactionKind.RegisterOwner or TransactionKind.CompletePurchase or TransactionKind.UpgradeCrew or TransactionKind.PackCrate or TransactionKind.ClaimCrate or TransactionKind.LoadCrate or TransactionKind.UnloadCrate) : Copy(state);
                // Mod chỉ đổi ví, không tranh thủ hết hạn đặt chỗ hoặc cập nhật pha máy.
                bool cashOnly = c.kind is TransactionKind.GrantModCash or TransactionKind.DepositCash or TransactionKind.WithdrawCash or TransactionKind.GrantMaximum;
                if(!cashOnly)Expire(draft, clock());
                PhysicalCashRules.Guard(draft, c);
                int amount;
                using (QaCpuProbe.Measure(QaCpuProbe.Work.Apply))
                    amount = Apply(draft, c);
                PhysicalCashRules.Validate(draft);
                if(!cashOnly)RefreshMachinePhases(draft);
                if(!cashOnly)draft.simulationTime = clock();
                draft.revision = checked(state.revision + 1);
                receipt = new TransactionReceipt {
                    id = "receipt:" + c.key, key = c.key, effectId = c.effectId,
                    fingerprint = fingerprint, effectFingerprint = effectFingerprint,
                    eventId = "event:" + c.key, revision = draft.revision, amount = amount, cashAmount = CashMoved(state, draft, c.kind)
                };
                draft.receipts.Add(receipt);
                draft.outbox.Add(new TransactionEvent { id = receipt.eventId, receiptId = receipt.id,
                    effectId = c.effectId, revision = draft.revision, kind = c.kind.ToString() });
                try
                {
                    if(!runtime)Validate(draft);
                    else Require(draft.money>=0&&draft.revenue>=0&&draft.cashCollected>=0&&draft.receipts.Count==draft.revision,"journal","Giao dịch runtime không hợp lệ.");
                    Fault?.Invoke(CommitBoundary.Prepared);
                    using (QaCpuProbe.Measure(QaCpuProbe.Work.StoreCommit))
                    {
                        if (store is ICommandTransactionStore journal)
                            journal.Write(draft, c);
                        else
                            store?.Write(draft);
                    }
                }
                catch
                {
                    // Lịch sử bất biến được dùng chung; rollback phần append khi chưa publish.
                    if(runtime){state.receipts.Remove(receipt);if(ReferenceEquals(state.outbox,draft.outbox))state.outbox.RemoveAt(state.outbox.Count-1);}
                    // Có thể lỗi sau replace nhưng trước trả lời: đọc lại receipt trước khi cho retry.
                    if(store!=null)
                        try { var recovered = store.Read(); Validate(recovered); state = Copy(recovered);IndexReceipts(); }
                        catch { Lifecycle = CoreLifecycle.RecoveryFailed; }
                    throw;
                }
                state = draft;
                keys.Add(receipt.key,receipt);effects.Add(receipt.effectId,receipt);
                Fault?.Invoke(CommitBoundary.Published);
                return Copy(receipt);
            }
        }
        public IReadOnlyList<TransactionEvent> PendingEvents(string consumer)
        { lock (gate) return state.outbox.Where(e => !e.consumers.Contains(consumer) && e.kind != nameof(TransactionKind.AcknowledgeEvent)).Select(Copy).ToArray(); }
        static string Fingerprint(TransactionCommand c, bool effect)
        {
            var payload = Copy(c);
            if (effect) { payload.key = null; payload.expectedRevision = 0; }
            string json=JsonUtility.ToJson(payload);
            // Command cũ không có path; giữ fingerprint cũ cho các giao dịch không gửi xe.
            if(payload.kind!=TransactionKind.DispatchTruck)json=json.Replace(",\"path\":[]","");
            json=json.Replace(",\"approachStep\":0","");
            return FileTransactionStore.Hash(WorkforceFingerprint(json));
        }
        int Apply(TransactionState s, TransactionCommand c)
        {
            Require(c.workforceRevision is >= 0 and <= 1, "version", "Phiên bản nhân viên không hỗ trợ.");
            legacyWorkforceReplay = replaying && c.workforceRevision == 0;
            switch (c.kind)
            {
                case TransactionKind.Take:
                case TransactionKind.Place:
                case TransactionKind.Transfer: return Move(s, c);
                case TransactionKind.Reserve: return Reserve(s, c);
                case TransactionKind.Release:
                    var reservation = Reservation(s, c.target);
                    Require(reservation.holder == c.actor, "authority", "Reservation thuộc actor khác.");
                    if (reservation.status != ReservationStatus.Active) return 0;
                    reservation.status = ReservationStatus.Released; return reservation.quantity;
                case TransactionKind.Split: return Split(s, c);
                case TransactionKind.Merge: return Merge(s, c);
                case TransactionKind.CreateOrder: return CreateOrder(s, c);
                case TransactionKind.DeliverOrder: return Deliver(s, c);
                case TransactionKind.CompleteOrder: return CompleteOrder(s, c);
                case TransactionKind.FailOrder: return FailOrder(s, c);
                case TransactionKind.CreatePayment: return CreatePayment(s, c);
                case TransactionKind.CollectPayment: return Collect(s, c);
                case TransactionKind.ContributePurchase: return Contribute(s, c);
                case TransactionKind.CompletePurchase: return CompletePurchase(s, c);
                case TransactionKind.StartMachine: return StartMachine(s, c);
                case TransactionKind.AdvanceMachine: return AdvanceMachine(s, c);
                case TransactionKind.CompleteMachine: return CompleteMachine(s, c);
                case TransactionKind.AcknowledgeEvent:
                    Require(c.actor == c.secondary && !string.IsNullOrWhiteSpace(c.secondary), "authority", "Sai consumer.");
                    var e = s.outbox.Find(x => x.id == c.target);
                    Require(e != null, "event", "Event không tồn tại.");
                    if (e.consumers.Contains(c.secondary)) return 0;
                    e.consumers.Add(c.secondary);
                    var consumer = s.consumers.Find(x => x.id == c.secondary);
                    if (consumer == null) { consumer = new ConsumerState { id = c.secondary }; s.consumers.Add(consumer); }
                    consumer.appliedEvents++; return 1;
                case TransactionKind.RegisterOwner:
                    Require(c.actor == "simulation" && c.owner != null, "authority", "Chỉ mô phỏng đăng ký owner.");
                    Require(!s.owners.Any(x => x.id == c.owner.id), "duplicate", "Owner đã tồn tại.");
                    s.owners.Add(Copy(c.owner));
                    if (c.owner.kind == OwnerKind.Worker)
                        foreach (var shared in s.owners.Where(x => x.kind is not (OwnerKind.Player or OwnerKind.Worker)))
                            if (!shared.writers.Contains(c.owner.actor)) shared.writers.Add(c.owner.actor);
                    return 1;
                case TransactionKind.OperateProducer: return OperateProducer(s, c);
                case TransactionKind.RestockProducer: return RestockProducer(s, c);
                case TransactionKind.GrantAssistance: return GrantAssistance(s, c);
                case TransactionKind.GrantModCash: return GrantModCash(s, c);
                case TransactionKind.DepositCash: return TransferSafe(s, c, true);
                case TransactionKind.WithdrawCash: return TransferSafe(s, c, false);
                case TransactionKind.GrantMaximum: return GrantMaximum(s, c);
                case WorkforceRules.RetireCommand: return RetireWorkforce(s, c);
                case TransactionKind.SelectRecipe:
                    var selected=Machine(s,c);Require(!selected.running && selected.recipeOptions.Contains(c.secondary),"recipe","Recipe không thuộc máy hoặc mẻ đang chạy.");
                    selected.definitionId=c.secondary;selected.definitionVersion=Definitions.Recipe(c.secondary).version;selected.manualRecipe=c.actor=="player";return 1;
                case TransactionKind.PackCrate:return PackCrate(s,c);
                case TransactionKind.ClaimCrate:return ClaimCrate(s,c);
                case TransactionKind.LoadCrate:return LoadCrate(s,c);
                case TransactionKind.UnloadCrate:return UnloadCrate(s,c);
                case TransactionKind.SelectTruckRoute:return SelectTruckRoute(s,c);
                case TransactionKind.DispatchTruck:return DispatchTruck(s,c);
                case TransactionKind.TickTruck:return TickTruck(s,c);
                case TransactionKind.TickProducer: return TickProducer(s, c);
                case TransactionKind.HarvestProducer: return HarvestProducer(s, c);
                case TransactionKind.FeedProducer: return FeedProducer(s, c);
                case TransactionKind.ReleaseOperator:
                    var station = Station(s, c.target);
                    if (station.operatorId != c.actor) return 0;
                    station.operatorId = null; station.operatorUntil = 0; return 1;
                case TransactionKind.BreakMachine:
                    Require(c.actor == "simulation", "authority", "Chỉ mô phỏng làm hỏng máy.");
                    var broken = Station(s, c.target); Require(broken.kind == "machine", "station", "Không phải máy.");
                    if (broken.progress.broken) return 0;
                    Require(!s.stations.Any(x=>x.kind=="machine"&&x.progress.broken), "breakdown", "Đã có một máy hỏng.");
                    broken.progress.broken = true; broken.progress.repairFee = Math.Clamp(c.quantity, 10, 100);
                    broken.progress.repairRemaining = 5;broken.progress.repairProgress=0;broken.progress.repairVersion=1;broken.progress.repairIncident++;broken.progress.playerOnlyRepair=true;broken.operatorId=null;broken.operatorUntil=0;broken.progress.repairPaid = false; return 1;
                case TransactionKind.RepairMachine: return RepairMachine(s, c);
                case TransactionKind.UpgradeCrew: return UpgradeCrew(s, c);
                case TransactionKind.CleanTable: return CleanTable(s, c);
                case TransactionKind.AdvanceDiner: return AdvanceDiner(s, c);
                case TransactionKind.Checkpoint:
                    Require(c.actor == "simulation", "authority", "Chỉ mô phỏng ghi checkpoint."); return 0;
                default: throw new TransactionRejectedException("kind", "Transaction không hỗ trợ.");
            }
        }
    }
}
