using System;
using System.Collections.Generic;

namespace Tycoon
{
    public enum OwnerKind { Player, Worker, Customer, Storage, Counter, Machine, Conveyor, Station, Escrow, Crate, Truck }
    public enum MachinePhase { WaitingInput, Ready, Operating, CompletedWaitingPickup }
    public enum TransactionKind
    {
        Take, Place, Transfer, Reserve, Release, Split, Merge, CreateOrder, DeliverOrder,
        CompleteOrder, FailOrder, CreatePayment, CollectPayment, ContributePurchase, CompletePurchase,
        StartMachine, AdvanceMachine, CompleteMachine, AcknowledgeEvent,
        RegisterOwner, OperateProducer, TickProducer, HarvestProducer, FeedProducer,
        ReleaseOperator, BreakMachine, RepairMachine, UpgradeCrew, CleanTable, AdvanceDiner, Checkpoint, RestockProducer,
        GrantAssistance, SelectRecipe, PackCrate, ClaimCrate, LoadCrate, UnloadCrate, SelectTruckRoute, DispatchTruck, TickTruck
    }
    public enum OrderStatus { Open, Complete, Failed }
    public enum ReservationStatus { Active, Used, Released, Expired }
    public enum CoreLifecycle { Ready, RecoveryFailed }

    // Chỉ TransactionCore giữ bản authoritative; mọi bản trả ra ngoài đều là bản sao.
    [Serializable] public sealed class OwnerState
    {
        internal OwnerState ShallowCopy()=>(OwnerState)MemberwiseClone();
        public string id, actor, location;
        public OwnerKind kind;
        public int capacity;
        public bool singleItem;
        public List<string> writers = new();
        public List<ItemAmount> limits = new();
        public List<string> accepts = new();
        public CustomerSave customer;
        public DinerSave diner;
        public WorkerSave worker;
    }
    [Serializable] public sealed class ItemStackState
    {
        internal ItemStackState ShallowCopy()=>(ItemStackState)MemberwiseClone();
        public string id, item, owner, location;
        public int quantity, definitionVersion = 1;
    }
    [Serializable] public sealed class ReservationState
    {
        internal ReservationState ShallowCopy()=>(ReservationState)MemberwiseClone();
        public string id, holder, source, destination, item;
        public string relay;
        public int quantity;
        public double expiresAt;
        public ReservationStatus status;
        public List<StackAllocation> allocations = new();
    }
    [Serializable] public sealed class StackAllocation { public string stack; public int quantity; }
    [Serializable] public sealed class OrderRuntimeState
    {
        internal OrderRuntimeState ShallowCopy()=>(OrderRuntimeState)MemberwiseClone();
        public string id, customer, counter;
        public double deadline;
        public OrderStatus status;
        public List<OrderLine> lines = new();
        public string table;
        public int dinerPhase;
        public double eatingRemaining;
    }
    [Serializable] public sealed class PaymentState
    {
        internal PaymentState ShallowCopy()=>(PaymentState)MemberwiseClone();
        public string id, order, counter;
        public int amount;
        public bool collected;
    }
    [Serializable] public sealed class PurchaseRuntimeState
    {
        internal PurchaseRuntimeState ShallowCopy()=>(PurchaseRuntimeState)MemberwiseClone();
        public string id, definitionId;
        public int definitionVersion = 1, contributed;
        public bool complete;
    }
    [Serializable] public sealed class StationRuntimeState
    {
        internal StationRuntimeState ShallowCopy()=>(StationRuntimeState)MemberwiseClone();
        public string id, definitionId, input, output, jobId, reservationId, operatorId, escrow;
        public int definitionVersion = 1, level = 1, workCount, playerWorkCount, batches, playerBatches;
        public int batchYield=1;
        public float cycleSeconds;
        public MachinePhase machinePhase;
        public double remaining;
        public bool running;
        public bool autonomous, manualRecipe, legacyInput;
        public string lastInputActor;
        public List<string> recipeOptions=new();
        public RecipeBatchSnapshot batch;
        public string kind = "machine", item, area, requirement;
        public double operatorUntil;
        public StationProgressSave progress = new();
    }
    [Serializable] public sealed class TransactionReceipt
    {
        public string id, key, fingerprint, effectId, effectFingerprint, eventId;
        public long revision;
        public int amount;
    }
    [Serializable] public sealed class TransactionEvent
    {
        internal TransactionEvent ShallowCopy()=>(TransactionEvent)MemberwiseClone();
        public string id, receiptId, effectId, kind;
        public long revision;
        public List<string> consumers = new();
    }
    [Serializable] public sealed class TransactionState
    {
        internal TransactionState ShallowCopy()=>(TransactionState)MemberwiseClone();
        public int schemaVersion = 2, catalogVersion = Definitions.Version;
        public long revision;
        public int money, revenue, cashCollected, assistedCash;
        public bool assisted;
        public int contentVersion;
        public double simulationTime;
        public int legacyRevenue, legacyTransactions;
        public List<long> legacyPaid = new();
        public List<LossRecord> legacyLosses = new();
        public List<CashSave> legacyCash = new();
        public List<OwnerState> owners = new();
        public List<ItemStackState> stacks = new();
        public List<ReservationState> reservations = new();
        public List<OrderRuntimeState> orders = new();
        public List<PaymentState> payments = new();
        public List<PurchaseRuntimeState> purchases = new();
        public List<CrewState> crews = new();
        public List<StationRuntimeState> stations = new();
        public List<string> jobIds = new();
        public List<ConsumerState> consumers = new();
        public List<string> unlocked = new();
        public List<TransactionReceipt> receipts = new();
        public List<TransactionEvent> outbox = new();
        public List<CargoCrateState> crates=new();
        // JsonUtility có thể tạo object rỗng cho reference null; list rỗng biểu diễn chưa mua xe.
        public List<TruckRuntimeState> trucks=new();
        public TruckRuntimeState truck {get=>trucks!=null&&trucks.Count>0?trucks[0]:null;set=>trucks=value==null?new():new(){value};}
        public List<ManualTransportJob> transportJobs=new();
        public string carryOrigin,carryBatch;
        public string carryDestination;
        public int carryDelivered;
    }
    [Serializable] public sealed class TransactionCommand
    {
        public string key, effectId, actor;
        public TransactionKind kind;
        public long expectedRevision;
        public string source, destination, item, target, secondary, reservation;
        public int quantity;
        public double duration, expiresAt;
        public List<OrderLine> lines = new();
        public OwnerState owner;
        public List<RoutePoint> path=new();
    }
    [Serializable] public sealed class ConsumerState { public string id; public int appliedEvents; }
    public sealed class TransactionRejectedException : InvalidOperationException
    {
        public string Code { get; }
        public TransactionRejectedException(string code, string message) : base(code + ": " + message) { Code = code; }
    }
}
