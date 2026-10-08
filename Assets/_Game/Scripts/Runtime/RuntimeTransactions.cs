using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    // Component Unity chỉ đọc projection và gửi command; core là writer của tài sản/state.
    public sealed partial class RuntimeTransactions
    {
        readonly GameSession game;
        readonly TransactionCore core;
        readonly Dictionary<Inventory, string> inventories = new();
        readonly Dictionary<EntityId, string> actors = new();
        readonly GameplayTransactionStore store;
        TransactionState view;
        TransactionState stationView;
        readonly Dictionary<string, StationRuntimeState> stationsById = new(StringComparer.Ordinal);
        double origin;readonly double restoredTime;bool frozen;
        public bool Ready => core.Lifecycle == CoreLifecycle.Ready && !game.SaveBlocked;
        public long Revision => core.Revision;
        // Tính delta trước để load cùng frame không làm timestamp lùi do sai số dấu phẩy động.
        public double Now => frozen ? restoredTime : restoredTime + (Time.timeAsDouble - origin);
        internal void ResumeClock(){if(frozen){origin=Time.timeAsDouble;frozen=false;}}
        public string LastReason { get; private set; } = "";
        internal TransactionState View => view;
        public TransactionState Snapshot() => core.Snapshot();
        public RuntimeTransactions(GameSession game, TransactionState state, bool persist = true,
            FileStream existingLease = null)
        {
            this.game = game; origin = Time.timeAsDouble; restoredTime = state.simulationTime;frozen=game.IsRestoring;
            store = persist ? new GameplayTransactionStore(game, existingLease) : null;
            try{core = new TransactionCore(state, store, () => Now, true); view = core.RuntimeSnapshot();}
            catch{store?.Dispose();throw;}
            actors.Add(game.Player.GetEntityId(), "player");
            Bind(game.Player.Carry, "player"); game.Economy.Bind(this);
            foreach (var station in game.Stations.Where(x => x is not StationZone))
            {
                if (station.Inventory != null) Bind(station.Inventory, station.Id);
                if (station is MachineStation machine) Bind(machine.Input, station.Id + "_input");
            }
        }
        public TransactionCommand Command(TransactionKind kind, string actor, string target = null, string key = null, string effect = null)
        {
            key ??= Guid.NewGuid().ToString("N");
            return new() { kind = kind, actor = actor, target = target, key = key,
                effectId = effect ?? "effect:" + key, expectedRevision = Revision,
                workforceRevision = WorkforceRules.Version };
        }
        public TransactionReceipt Execute(TransactionCommand command)
        {
            using var executeTiming = QaCpuProbe.Measure(QaCpuProbe.Work.RuntimeExecute);
            if (!Ready) throw new TransactionRejectedException("recovery", "Gameplay chưa phục hồi xong.");
            try { return core.Execute(command); }
            finally { Refresh(); }
        }
        public bool TryExecute(TransactionCommand command, out int amount)
        {
            amount = 0; LastReason = "";
            try { amount = Execute(command).amount; return true; }
            catch (TransactionRejectedException error) { LastReason = error.Message.Substring(error.Code.Length+2); return false; }
            catch (IOException error) { game.BlockRecovery(error); LastReason = "Không ghi được giao dịch; gameplay đã dừng."; return false; }
        }
        internal StationRuntimeState Station(string id)
        {
            if (id == null) return null;
            if (!ReferenceEquals(stationView, view))
            {
                // Getter trạm được gọi liên tục bởi animation; cache chỉ đọc theo projection.
                stationsById.Clear();
                foreach (var station in view.stations) stationsById.Add(station.id, station);
                stationView = view;
            }
            return stationsById.TryGetValue(id, out var found) ? found : null;
        }
        internal OrderRuntimeState Order(long receipt) => view.orders.Find(x => x.id == OrderId(receipt));
        public static string OrderId(long receipt) => "order:" + receipt;
        public static string CustomerId(long receipt) => "customer:" + receipt;
        public static string WorkerOwner(string id) => "worker:" + id;
        public string Actor(EntityId actor) => actors.TryGetValue(actor, out var id) ? id : throw new TransactionRejectedException("authority", "Actor chưa đăng ký.");
        public string OwnerId(Inventory inventory) => inventories.TryGetValue(inventory, out var id) ? id : throw new TransactionRejectedException("owner", "Inventory chưa có owner.");
        string CarrierActor(Inventory from, Inventory to)
        {
            var a = view.owners.Find(x => x.id == OwnerId(from)); var b = view.owners.Find(x => x.id == OwnerId(to));
            return a.kind is OwnerKind.Player or OwnerKind.Worker ? a.actor : b.kind is OwnerKind.Player or OwnerKind.Worker ? b.actor : "simulation";
        }
        public int Transfer(Inventory from, Inventory to, string item, int requested, string key = null, string effect = null, string reservation = null)
        {
            int count = string.IsNullOrEmpty(reservation) ? Math.Min(requested, Math.Min(from.Available(item), to.FreeFor(item))) : requested;
            if (count <= 0) return 0;
            var source = view.owners.Find(x => x.id == OwnerId(from)); var destination = view.owners.Find(x => x.id == OwnerId(to));
            var kind = source.kind is OwnerKind.Player or OwnerKind.Worker ? TransactionKind.Place : destination.kind is OwnerKind.Player or OwnerKind.Worker ? TransactionKind.Take : TransactionKind.Transfer;
            var c = Command(kind, CarrierActor(from, to), key: key, effect: effect);
            c.source = source.id; c.destination = destination.id; c.item = item; c.quantity = count; c.reservation = reservation;
            return TryExecute(c, out int moved) ? moved : 0;
        }
        public string Reserve(Inventory from, Inventory to, string item, int count, EntityId actor, Inventory relay = null)
        {
            string id = "reservation:" + Guid.NewGuid().ToString("N");
            var c = Command(TransactionKind.Reserve, Actor(actor), id);
            c.source = OwnerId(from); c.destination = OwnerId(to); c.item = item; c.quantity = count;
            c.secondary = relay == null ? null : OwnerId(relay); c.expiresAt = Now + 120;
            return TryExecute(c, out _) ? id : null;
        }
        public bool Release(string reservation, EntityId actor)
        {
            if (string.IsNullOrEmpty(reservation)) return false;
            return TryExecute(Command(TransactionKind.Release, Actor(actor), reservation), out _);
        }
        public bool TickConveyor(ConveyorStation route)
        {
            if(!Ready||!route||!route.IsUnlocked||!route.Target||route.Sources==null)return false;
            var input=route.Target.Recipe.inputs.SingleOrDefault(x=>x.id==route.ItemId);
            if(input==null)return false;
            string relay=OwnerId(route.Inventory),destination=OwnerId(route.Target.Input);
            var active=view.reservations.FirstOrDefault(x=>x.status==ReservationStatus.Active&&x.item==route.ItemId&&(x.relay==relay||x.source==relay));
            if(active!=null)
            {
                string key="belt:"+active.id+":"+(active.source==relay?"deliver":"load");
                return active.source==relay
                    ?game.Machines.FirstOrDefault(x=>OwnerId(x.Input)==active.destination) is MachineStation destinationMachine&&Transfer(route.Inventory,destinationMachine.Input,route.ItemId,active.quantity,key,"effect:"+key,active.id)>0
                    :game.Stations.OfType<StorageStation>().FirstOrDefault(x=>OwnerId(x.Inventory)==active.source) is StorageStation source&&route.Inventory.FreeFor(route.ItemId)>=active.quantity&&
                        Transfer(source.Inventory,route.Inventory,route.ItemId,active.quantity,key,"effect:"+key,active.id)>0;
            }
            if(route.Inventory.Total>0)return false;
            if(route.Target.Broken)return false;
            foreach(var source in route.Sources)
            {
                if(!source||source.AvailableAboveReserve(route.ItemId)<input.count||route.Inventory.FreeFor(route.ItemId)<input.count||route.Target.Input.FreeFor(route.ItemId)<input.count)continue;
                string sequence=Revision.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string reservation="reservation:"+route.Id+":"+sequence;
                var command=Command(TransactionKind.Reserve,"simulation",reservation,"belt-reserve:"+route.Id+":"+sequence,"effect:belt-reserve:"+route.Id+":"+sequence);
                command.source=OwnerId(source.Inventory);command.destination=destination;command.secondary=relay;command.item=route.ItemId;
                command.quantity=input.count;command.expiresAt=double.MaxValue;
                if(!TryExecute(command,out _))return false;
                string key="belt:"+reservation+":load";
                return Transfer(source.Inventory,route.Inventory,route.ItemId,input.count,key,"effect:"+key,reservation)>0;
            }
            return false;
        }
        internal ReservationState Reservation(string id) => view.reservations.Find(x => x.id == id);
        public bool StationAction(TransactionKind kind, Station station, EntityId actor, float delta = 0, Inventory carrier = null)
        {
            var c = Command(kind, Actor(actor), station.Id); c.duration = delta;
            if(kind==TransactionKind.FeedProducer&&station is ProductionStation livestock)c.item=livestock.FeedItem;
            if (carrier != null) { c.source = OwnerId(carrier); c.destination = OwnerId(carrier); }
            return TryExecute(c, out _);
        }
        public bool TickProducer(ProductionStation station, float delta)
        {
            var p = Station(station.Id).progress;
            if (!station.Animal && p.phase != 2 || station.Animal && p.breeding <= 0 && (p.feed == 0 || p.herd == 0 || p.cycle == 0)) return false;
            var c = Command(TransactionKind.TickProducer, "simulation", station.Id); c.duration = delta;
            return TryExecute(c, out _);
        }
        public bool OperateMachine(MachineStation machine, float delta, EntityId actor)
        {
            if(Station(machine.Id).autonomous)return false;
            var m = Station(machine.Id); string actorId = Actor(actor);
            if (!m.running)
            {
                var start = Command(TransactionKind.StartMachine, actorId, machine.Id);
                start.secondary = "job:" + Guid.NewGuid().ToString("N");
                if (!TryExecute(start, out _)) return false;
                m = Station(machine.Id);
            }
            string family=machine.AreaId switch{"processing"=>"mill","supermarket"=>"market","bakery"=>"oven","restaurant"=>"kitchen",_=>machine.AreaId};
            var advance = Command(TransactionKind.AdvanceMachine, actorId, machine.Id);
            advance.secondary = m.jobId; advance.duration = delta * (1 + .15f * (game.Progression.AxisLevel(family,UpgradeAxis.Speed) - 1));
            if (!TryExecute(advance, out _)) return false;
            m = Station(machine.Id);
            if (m.remaining == 0)
            {
                var finish = Command(TransactionKind.CompleteMachine, actorId, machine.Id, effect: "finish:" + m.id + ":" + m.jobId);
                finish.secondary = m.jobId; return TryExecute(finish, out _);
            }
            return true;
        }
        public void ReleaseOperator(Station station, EntityId actor)
        {
            if (!Ready || !actors.TryGetValue(actor, out var id) || Station(station.Id)?.operatorId != id) return;
            TryExecute(Command(TransactionKind.ReleaseOperator, id, station.Id), out _);
        }
        public bool OperatedByOther(Station station, EntityId actor)
        { var m = Station(station.Id); return m != null && m.operatorId != null && m.operatorId != Actor(actor) && Now < m.operatorUntil; }
    }
}
