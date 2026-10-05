using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public enum InteractionKind { Pickup, Drop, Operate, Serve, Collect, Purchase, Restock, Repair }
    public readonly struct InteractionResult
    {
        public readonly bool Worked;
        public readonly string Reason;
        public InteractionResult(bool worked,string reason=""){Worked=worked;Reason=reason??"";}
        public static InteractionResult Reject(string reason)=>new(false,reason);
        public static InteractionResult Success=>new(true);
        public static InteractionResult Waiting=>new(false);
    }
    public readonly struct InteractionContext
    {
        public readonly PlayerController Player;
        public readonly Inventory Carry;
        public readonly EntityId Actor;
        public readonly string SelectedItem;
        public InteractionContext(PlayerController player,string selectedItem)
        {Player=player;Carry=player.Carry;Actor=player.GetEntityId();SelectedItem=selectedItem;}
        public InteractionContext(Inventory carry,EntityId actor,string selectedItem)
        {Player=null;Carry=carry;Actor=actor;SelectedItem=selectedItem;}
    }
    public interface IPlayerInteractionTarget
    {
        InteractionResult Perform(InteractionKind kind,InteractionContext context,float delta);
        void EndInteraction(EntityId actor);
    }
    public interface IPlayerInteractionArea
    {
        InteractionKind Kind {get;}
        Vector3 Center {get;}
        bool Available {get;}
        bool Contains(Vector3 point);
        InteractionResult Perform(InteractionContext context,float delta);
        void Exit(EntityId actor);
    }
    // Chỉ vùng đang đứng được chạy; chuyển vùng hoặc mất điều khiển luôn nhả vùng cũ.
    public sealed class PlayerInteractionSession
    {
        public IPlayerInteractionArea Current {get;private set;}
        EntityId actor;
        float dwell;
        public InteractionResult Tick(IEnumerable<IPlayerInteractionArea> areas,InteractionContext context,Vector3 position,float delta)
        {
            if(delta<=0||float.IsNaN(delta)||float.IsInfinity(delta)){Stop();return InteractionResult.Waiting;}
            IPlayerInteractionArea nearest=null;float distance=float.PositiveInfinity;
            foreach(var area in areas)
                if(area!=null&&area.Contains(position)&&area.Available)
                {
                    var offset=position-area.Center;offset.y=0;
                    float square=area is ProximityTarget target?Mathf.Pow(target.Distance(position),2):offset.sqrMagnitude;
                    if(square<distance){distance=square;nearest=area;}
                }
            if(Current is ProximityTarget && Current.Available && Current.Contains(position)) nearest=Current;
            if(!ReferenceEquals(Current,nearest)||actor!=context.Actor){Stop();Current=nearest;actor=context.Actor;}
            if(Current is ProximityTarget){dwell+=delta;if(dwell<.25f)return InteractionResult.Waiting;}
            return Current==null?InteractionResult.Waiting:Current.Perform(context,delta);
        }
        public void Stop(){var previous=Current;Current=null;dwell=0;previous?.Exit(actor);}
    }
    public static class StationPlayerActions
    {
        static string Name(string id)=>Definitions.Item(id)?.label??id;
        public static InteractionResult CannotCarry(Inventory carry,string id)
        {
            if(carry.Total>carry.Count(id))return InteractionResult.Reject("Giỏ đang mang loại khác; hãy đặt hết hàng trước khi lấy "+Name(id)+".");
            return InteractionResult.Reject("Giỏ đầy ("+carry.Total+"/"+carry.Capacity+").");
        }
        public static bool Supports(Station target,InteractionKind kind)=>target switch
        {
            ProductionStation p=>kind is InteractionKind.Pickup or InteractionKind.Operate||p.Animal&&kind is InteractionKind.Drop or InteractionKind.Restock,
            MachineStation=>kind is InteractionKind.Pickup or InteractionKind.Drop or InteractionKind.Operate or InteractionKind.Repair,
            StorageStation or ShelfStation=>kind is InteractionKind.Pickup or InteractionKind.Drop,
            CheckoutStation=>kind is InteractionKind.Drop or InteractionKind.Serve or InteractionKind.Collect,
            TableStation=>kind is InteractionKind.Serve or InteractionKind.Operate,
            PurchasePad=>kind==InteractionKind.Purchase,
            _=>false
        };
        public static InteractionResult Execute(Station target,InteractionKind kind,InteractionContext context,float delta)
        {
            if(!target||!target.IsUnlocked)return InteractionResult.Reject("Khu vực chưa được mở.");
            if(context.Carry==null||delta<=0||float.IsNaN(delta)||float.IsInfinity(delta))return InteractionResult.Waiting;
            if(!Supports(target,kind))return InteractionResult.Reject("Vật thể này không hỗ trợ thao tác đã chọn.");
            var carry=context.Carry;
            if(kind==InteractionKind.Purchase&&target is PurchasePad pad)
            {
                var game=GameSession.Instance;
                if(!game.CanPurchase(pad.Upgrade,out string reason))return InteractionResult.Reject(reason);
                if(game.Economy.Money<=0)return InteractionResult.Reject("Không còn tiền để góp.");
                return new InteractionResult(pad.HoldToBuy(delta));
            }
            if(kind==InteractionKind.Collect&&target is CheckoutStation cash)
            {
                if(cash.Cash<=0)return InteractionResult.Reject("Quầy chưa có tiền cần thu.");
                return context.Player?new InteractionResult(cash.CollectCash(context.Player)>0):InteractionResult.Reject("Người chơi phải đứng tại vùng thu tiền.");
            }
            if(kind==InteractionKind.Serve)
            {
                if(target is CheckoutStation counter)
                {
                    var order=counter.FrontOrder;
                    if(order==null)return InteractionResult.Reject("Chưa có khách cần phục vụ.");
                    var source=carry.Total>0?carry:counter.Inventory;
                    if(source==null||source.Total==0)return InteractionResult.Reject("Giỏ và quầy đang không có hàng.");
                    bool wanted=false;foreach(var line in order.Lines)wanted|=line.Remaining>0&&source.Available(line.id)>0;
                    if(!wanted)return InteractionResult.Reject(carry.Total>0?"Đơn hàng không cần loại đang mang.":"Quầy chưa có loại hàng khách cần.");
                    return counter.Serve(source,context.Actor)?InteractionResult.Success:InteractionResult.Reject("Chờ khách tới đúng vị trí quầy.");
                }
                if(target is TableStation table)
                {
                    if(carry.Total==0)return InteractionResult.Reject("Giỏ đang trống.");
                    if(!table.NeedsMeal)return InteractionResult.Reject("Bàn chưa cần phục vụ.");
                    if(carry.Available(table.Occupant.WantedItem)==0)return InteractionResult.Reject(table.Label+" cần "+Name(table.Occupant.WantedItem)+".");
                    return new InteractionResult(table.DeliverMeal(carry));
                }
            }
            if(kind==InteractionKind.Operate)
            {
                if(target.IsOperatedByOther(context.Actor))return InteractionResult.Reject("Trạm đang có người vận hành.");
                if(target is ProductionStation production)return production.OperatePlayer(delta,context.Actor);
                if(target is MachineStation machine)
                {
                    if(machine.Broken)return InteractionResult.Reject("Máy hỏng; dừng gần máy để sửa.");
                    if(!machine.Running)
                    {
                        if(machine.Inventory.FreeFor(machine.Recipe.output)<machine.Recipe.yield)return InteractionResult.Reject("Đầu ra máy đầy; hãy lấy hàng.");
                        foreach(var item in machine.Recipe.inputs)if(machine.Input.Available(item.id)<item.count)return InteractionResult.Reject("Thiếu "+Name(item.id)+"; hãy đặt nguyên liệu vào máy.");
                    }
                    return InteractionResult.Reject(machine.Running?"Máy đang tự chạy • còn "+machine.Remaining.ToString("0.0")+"s":"Máy tự chạy khi đủ nguyên liệu.");
                }
                if(target is TableStation cleaning)
                    return cleaning.Cleaning>0&&cleaning.Occupant==null?new InteractionResult(cleaning.Work(carry,delta,context.Actor)):InteractionResult.Reject("Bàn chưa cần dọn.");
            }
            if(kind==InteractionKind.Repair&&target is MachineStation repair)
            {
                if(!repair.Broken)return InteractionResult.Reject("Máy chưa cần sửa.");
                if(!repair.RepairPaid&&GameSession.Instance.Economy.Money<repair.RepairFee)return InteractionResult.Reject("Không đủ tiền sửa máy.");
                return new InteractionResult(repair.Repair(GameSession.Instance.Economy,delta,context.Actor));
            }
            if(kind==InteractionKind.Restock&&target is ProductionStation livestock)
            {
                if(!livestock.Animal)return InteractionResult.Reject("Chỉ chuồng vật nuôi mới cần tái đàn.");
                return livestock.RestockPlayer(context.Actor);
            }
            if(kind==InteractionKind.Pickup)
            {
                string item=target is ProductionStation producer?producer.ItemId:context.SelectedItem;
                var pickup=target.Inventory;
                if(target is MachineStation output)
                {
                    string held=carry.Total>0?carry.Snapshot()[0].id:null;
                    if(held!=null&&output.Inventory.Available(held)>0)item=held;
                    else if(output.Inventory.Available(item)==0&&output.Inventory.Available(output.Recipe.output)>0)item=output.Recipe.output;
                    else if(output.Input.Available(item)>0&&output.Inventory.Available(item)==0&&!Array.Exists(output.Recipe.inputs,x=>x.id==item))pickup=output.Input;
                    else if(output.Inventory.Available(item)==0)item=output.Inventory.Total>0?output.Inventory.Snapshot()[0].id:output.Recipe.output;
                }
                if(target is ShelfStation shelf&&!shelf.Accepts(item))
                    return InteractionResult.Reject("Quầy không chứa "+Name(item)+"; nhấn Q / RB để chọn loại cần lấy.");
                if(string.IsNullOrEmpty(item)||Definitions.Item(item)==null)return InteractionResult.Reject("Chưa có hàng để lấy.");
                if(carry.FreeFor(item)==0)return CannotCarry(carry,item);
                if(target is ProductionStation harvest)return harvest.PickupPlayer(carry,delta,context.Actor);
                if(pickup==null||pickup.Available(item)==0)return InteractionResult.Reject("Chưa có "+Name(item)+" để lấy.");
                return new InteractionResult(Inventory.Transfer(pickup,carry,item,1)>0);
            }
            if(kind==InteractionKind.Drop)
            {
                if(carry.Total==0)return InteractionResult.Reject("Giỏ đang trống.");
                string item=carry.Snapshot()[0].id;
                if(target is ProductionStation animal)
                {
                    if(!animal.Animal||item!=animal.FeedItem)return InteractionResult.Reject("Chuồng chỉ nhận "+Definitions.Item(animal.FeedItem)?.label+" làm thức ăn.");
                    return animal.FeedPlayer(carry,context.Actor);
                }
                if(target is ShelfStation shelf&&!shelf.Accepts(item))return InteractionResult.Reject("Quầy không nhận "+Name(item)+".");
                if(target is CheckoutStation counter&&!counter.AcceptsItem(item))return InteractionResult.Reject("Quầy không nhận "+Name(item)+".");
                var destination=target.Inventory;
                if(target is MachineStation machine)
                {
                    if(!Array.Exists(machine.Recipe.inputs,x=>x.id==item))return InteractionResult.Reject("Máy không nhận "+Name(item)+".");
                    machine.ConfigureInputLimits();destination=machine.Input;
                }
                if(destination==null)return InteractionResult.Reject("Vị trí này không nhận hàng.");
                if(destination.FreeFor(item)==0)return InteractionResult.Reject("Vị trí chứa "+Name(item)+" đã đầy.");
                bool moved=Inventory.Transfer(carry,destination,item,1)>0;
                if(moved&&GameSession.Instance.Transactions==null)target.WorkCount++;
                return new InteractionResult(moved);
            }
            return InteractionResult.Reject("Vùng này chưa hỗ trợ thao tác.");
        }
    }
}
