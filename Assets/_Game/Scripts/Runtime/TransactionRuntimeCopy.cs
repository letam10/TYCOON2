using System.Collections.Generic;
using System.Linq;

namespace Tycoon
{
    internal static class TransactionRuntimeCopy
    {
        // Không serialize lại toàn bộ lịch sử ở mỗi tick; chỉ copy phần state có thể thay đổi.
        internal static TransactionState RuntimeCopy(this TransactionState state,bool history,string acknowledged=null,bool ownerChanges=false)
        {
            var copy=state.ShallowCopy();
            copy.owners=ownerChanges?state.owners.Select(o=>{var x=o.ShallowCopy();x.writers=new(o.writers);return x;}).ToList():new(state.owners);
            copy.stacks=state.stacks.Select(x=>x.ShallowCopy()).ToList();
            copy.reservations=state.reservations.Select(r=>
            {
                if(r.status!=ReservationStatus.Active)return r;
                var x=r.ShallowCopy();x.allocations=r.allocations.Select(a=>new StackAllocation{stack=a.stack,quantity=a.quantity}).ToList();return x;
            }).ToList();
            copy.orders=state.orders.Select(o=>
            {
                if(o.status!=OrderStatus.Open&&o.dinerPhase!=2)return o;
                var x=o.ShallowCopy();x.lines=o.lines.Select(l=>l.Copy()).ToList();return x;
            }).ToList();
            copy.payments=state.payments.Select(p=>p.collected?p:p.ShallowCopy()).ToList();
            copy.purchases=state.purchases.Select(p=>p.complete?p:p.ShallowCopy()).ToList();
            copy.crews=state.crews.Select(c=>new CrewState{id=c.id,role=c.role,area=c.area,count=c.count,speedLevel=c.speedLevel,carryLevel=c.carryLevel}).ToList();
            copy.stations=state.stations.Select(m=>{var x=m.ShallowCopy();x.progress=m.progress.ShallowCopy();return x;}).ToList();
            copy.legacyCash=state.legacyCash.Select(c=>new CashSave{id=c.id,amount=c.amount}).ToList();
            copy.legacyPaid=new(state.legacyPaid);copy.legacyLosses=new(state.legacyLosses);
            copy.jobIds=new(state.jobIds);copy.unlocked=new(state.unlocked);
            copy.crates=state.crates.Select(x=>x.Copy()).ToList();copy.truck=state.truck?.Copy();copy.transportJobs=new(state.transportJobs);
            copy.consumers=state.consumers.Select(c=>new ConsumerState{id=c.id,appliedEvents=c.appliedEvents}).ToList();
            copy.receipts=history?state.receipts:new();
            copy.outbox=history?state.outbox:new();
            if(history&&acknowledged!=null)
                copy.outbox=state.outbox.Select(e=>{if(e.id!=acknowledged)return e;var x=e.ShallowCopy();x.consumers=new(e.consumers);return x;}).ToList();
            return copy;
        }
    }
}
