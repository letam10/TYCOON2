using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class RuntimeTransactions
    {
        internal static bool ReconcileWorld(TransactionState state,GameSession game)
        {
            bool changed=false;
            foreach(var station in game.Stations.Where(x=>x is not StationZone))
            {
                string id=station.Id;
                if(station is ProductionStation producer)
                {
                    var existing=state.stations.Find(x=>x.id==id);
                    if(existing!=null&&(existing.batchYield!=producer.Yield||existing.cycleSeconds!=producer.Interval))
                    {existing.batchYield=producer.Yield;existing.cycleSeconds=producer.Interval;changed=true;}
                }
                if(!state.owners.Any(x=>x.id==id))
                {
                    var inventory=station.Inventory??new Inventory(0);
                    var owner=new OwnerState{id=id,actor="simulation",location=station is StorageStation?station.AreaId:id,
                        kind=station is StorageStation?OwnerKind.Storage:station is MachineStation or CheckoutStation?OwnerKind.Machine:station is ConveyorStation?OwnerKind.Conveyor:OwnerKind.Station,
                        capacity=inventory.Capacity,singleItem=inventory.SingleItem,limits=inventory.Limits,writers=new(){"player"}};
                    if(station is CheckoutStation)owner.kind=OwnerKind.Counter;
                    if(station is ShelfStation shelf)owner.accepts=new(shelf.AllowedItems??Array.Empty<string>());
                    if(station is CheckoutStation checkout)owner.accepts=new(checkout.AcceptedItems);
                    if(station is MachineStation machine)owner.accepts=new(){machine.Recipe.output};
                    state.owners.Add(owner);
                    foreach(var item in inventory.Snapshot())state.stacks.Add(new ItemStackState{id="migrated:"+id+":"+item.id,item=item.id,quantity=item.count,owner=id,location=owner.kind==OwnerKind.Storage?owner.location+"/"+item.id:owner.location});
                    changed=true;
                }
                if(station is MachineStation machineStation&&!state.owners.Any(x=>x.id==id+"_input"))
                {
                    var input=machineStation.Input;
                    state.owners.Add(new OwnerState{id=id+"_input",actor="simulation",location=id+"_input",kind=OwnerKind.Machine,
                        capacity=input.Capacity,singleItem=input.SingleItem,limits=input.Limits,accepts=machineStation.Recipe.inputs.Select(x=>x.id).ToList(),writers=new(){"player"}});
                    foreach(var item in input.Snapshot())state.stacks.Add(new ItemStackState{id="migrated:"+id+"_input:"+item.id,item=item.id,quantity=item.count,owner=id+"_input",location=id+"_input"});
                    changed=true;
                }
                if(state.stations.Any(x=>x.id==id))continue;
                var progress=station.CaptureProgress();
                var runtime=new StationRuntimeState{id=id,area=station.AreaId,requirement=station.Requirement,level=progress.level,
                    workCount=progress.workCount,playerWorkCount=progress.playerWorkCount,input=station is MachineStation?id+"_input":id,output=id,
                    progress=progress,kind=station is MachineStation?"machine":station is ProductionStation?"producer":station is StorageStation?"storage":station is CargoDock?"dock":
                        station is ShelfStation?"shelf":station is CheckoutStation?"counter":station is TableStation?"table":station is ConveyorStation?"conveyor":station is PurchasePad?"purchase":"station",
                    definitionId=station is MachineStation m?m.Recipe.id:station is PurchasePad pad?pad.Upgrade.id:id,
                    item=(station as ProductionStation)?.ItemId,batchYield=(station as ProductionStation)?.Yield??1,cycleSeconds=(station as ProductionStation)?.Interval??0,running=progress.running,remaining=station is MachineStation?progress.remaining:0,batches=progress.batches,playerBatches=progress.playerBatches};
                state.stations.Add(runtime);changed=true;
            }
            changed |= UpgradeCounterOwners(state, game);
            if(!changed)return false;
            TransactionCore.NormalizeState(state);TransactionCore.Validate(state);
            return true;
        }

        internal static bool UpgradeCounterOwners(TransactionState state,GameSession game)
        {
            bool changed = false;
            foreach(var machine in game.Machines)
            {
                var runtime=state.stations.Find(x=>x.id==machine.Id);if(runtime==null)continue;
                changed |= !runtime.autonomous || !runtime.recipeOptions.SequenceEqual(machine.Options);
                runtime.autonomous=true;runtime.recipeOptions=new(machine.Options);
                var input=state.owners.Find(x=>x.id==runtime.input);var output=state.owners.Find(x=>x.id==runtime.output);
                foreach (var item in machine.Input.Limits)
                {
                    var limit = input.limits.Find(x => x.id == item.id);
                    if (limit == null)
                    {
                        input.limits.Add(new ItemAmount(item.id, item.count));
                        changed = true;
                    }
                    else if (limit.count < item.count)
                    {
                        limit.count = item.count;
                        changed = true;
                    }
                }
                foreach(string key in machine.Options)
                {
                    var definition = Definitions.Recipe(key);
                    if (!output.accepts.Contains(definition.output))
                    {
                        output.accepts.Add(definition.output);
                        changed = true;
                    }
                    foreach (var item in definition.inputs)
                    {
                        if (input.accepts.Contains(item.id)) continue;
                        input.accepts.Add(item.id);
                        changed = true;
                    }
                }
                changed |= runtime.requirement != machine.Requirement;
                runtime.requirement=machine.Requirement;
            }
            foreach(var producer in game.Producers)
            {
                var runtime=state.stations.Find(s=>s.id==producer.Id);if(runtime==null)continue;
                changed |= runtime.batchYield != producer.Yield || runtime.cycleSeconds != producer.Interval;
                runtime.batchYield=producer.Yield;runtime.cycleSeconds=producer.Interval;
            }
            foreach(var counter in game.Checkouts.Where(x=>x.Inventory!=null))
            {
                var owner=state.owners.Find(x=>x.id==counter.Id);
                if(owner==null)throw new InvalidDataException("Owner quầy không còn tồn tại: "+counter.Id);
                changed |= owner.capacity < counter.Inventory.Capacity;
                owner.capacity=Math.Max(owner.capacity,counter.Inventory.Capacity);
                foreach (string item in counter.AcceptedItems)
                {
                    if (owner.accepts.Contains(item)) continue;
                    owner.accepts.Add(item);
                    changed = true;
                }
            }
            // LoadGame phải checkpoint cấu hình mới trước khi store đọc lại save cũ.
            return changed;
        }
    }
}
