using UnityEngine;

namespace Tycoon
{
    // Vật thể chọn hành động theo state; hướng chuyển hàng được giữ suốt lần đứng gần.
    public sealed class ProximityTarget : IPlayerInteractionArea
    {
        public readonly Station Target;
        public readonly bool Cash;
        InteractionKind kind;
        bool directionChosen;
        float elapsed;
        public ProximityTarget(Station target,bool cash=false){Target=target;Cash=cash;}
        public InteractionKind Kind=>kind;
        public Vector3 Center=>Cash && Target is CheckoutStation c ? c.CollectionPoint :Target is CargoDock?Target.transform.position+Vector3.left*1.7f:Target.transform.position;
        public bool Available=>Target && Target.isActiveAndEnabled && Target.IsUnlocked;
        Vector3 Surface(Vector3 point)
        {
            if(Cash||Target is CargoDock)return Center;
            Vector3 size=Target is ProductionStation p&&!p.Animal?new(4,.2f,4):Target is MachineStation?new(2.65f,1,2.25f):
                Target is ShelfStation?new(1.4f,1,4.7f):Target is StorageStation?new(3.3f,1,2.5f):Target is CheckoutStation?new(2.6f,1,1.2f):new(2,1,2);
            return new Bounds(Center+Vector3.up*.5f,size).ClosestPoint(point);
        }
        public float Distance(Vector3 point){var surface=Surface(point);surface.y=point.y;return Vector3.Distance(point,surface);}
        public bool Contains(Vector3 point)
        {
            if(!Available||Mathf.Abs(point.y-Center.y)>1.2f||Distance(point)>1.2f)return false;
            Vector3 start=point+Vector3.up*.8f,end=Surface(point);end.y=start.y;
            var delta=end-start;
            foreach(var hit in Physics.RaycastAll(start,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(Target.transform)&&!hit.transform.GetComponentInParent<PlayerController>())return false;
            return true;
        }
        public InteractionResult Perform(InteractionContext context,float delta)
        {
            if(!Available)return InteractionResult.Waiting;
            if(Cash)kind=InteractionKind.Collect;
            else if(Target is CargoDock)kind=InteractionKind.Operate;
            else if(Target is ProductionStation p)
            {
                kind=!p.Animal?(p.Phase==3?InteractionKind.Pickup:InteractionKind.Operate):
                    context.Carry.Available(p.FeedItem)>0&&p.Feed<ProductionStation.MaximumFeed?InteractionKind.Drop:
                    p.Herd==0?InteractionKind.Restock:p.Cycle<=0&&p.Feed>0?InteractionKind.Pickup:InteractionKind.Operate;
            }
            else if(Target is MachineStation m)
            {
                if(m.Broken)kind=InteractionKind.Repair;
                else if(!directionChosen)
                {
                    bool oldInput=context.Carry.Total==0&&m.Input.Available(context.SelectedItem)>0&&!System.Array.Exists(m.Recipe.inputs,x=>x.id==context.SelectedItem);
                    kind=oldInput?InteractionKind.Pickup:context.Carry.Total>0&&System.Array.Exists(m.Recipe.inputs,x=>context.Carry.Count(x.id)>0)?InteractionKind.Drop:m.Inventory.Total>0?InteractionKind.Pickup:context.Carry.Total>0?InteractionKind.Drop:InteractionKind.Operate;
                    directionChosen=oldInput;
                }
            }
            else if(Target is CheckoutStation counter)kind=counter.FrontOrder==null&&context.Carry.Total>0?InteractionKind.Drop:InteractionKind.Serve;
            else if(Target is TableStation t)kind=t.Cleaning>0&&t.Occupant==null?InteractionKind.Operate:InteractionKind.Serve;
            else if(!directionChosen){kind=context.Carry.Total>0?InteractionKind.Drop:InteractionKind.Pickup;directionChosen=true;}
            elapsed+=delta;if(elapsed<.1f)return InteractionResult.Waiting;
            float step=elapsed;elapsed=0;return Target.Perform(kind,context,step);
        }
        public void Exit(EntityId actor){directionChosen=false;elapsed=0;Target?.EndInteraction(actor);}
    }
}
