using UnityEngine;

namespace Tycoon
{
    public sealed class MachineStation : Station
    {
        public RecipeDefinition Recipe;
        [System.NonSerialized] public Inventory Input=new(36);
        public bool Running,Broken,RepairPaid;
        public float Remaining,RepairRemaining=8;
        public int Batches,RepairFee=20;
        public Transform Rotor;
        public override string Prompt=>Broken?Label+" • sửa "+RepairFee+" xu • "+RepairRemaining.ToString("0.0")+"s":Label+" • "+(Running?"Đứng vận hành • "+Remaining.ToString("0.0")+"s":"Đưa nguyên liệu / vận hành / lấy hàng");
        bool outputReserved;
        void Start()=>ConfigureInputLimits();
        public void ConfigureInputLimits(){if(Recipe!=null&&Recipe.inputs.Length>0)foreach(var item in Recipe.inputs)Input.SetLimit(item.id,Mathf.Max(item.count,Input.Capacity/Recipe.inputs.Length));}
        public bool Operate(float delta,EntityId actorId)
        {
            if(!IsUnlocked||Broken||delta<=0||float.IsNaN(delta)||float.IsInfinity(delta)||Recipe==null||Inventory==null||!Lease(actorId))return false;
            ConfigureInputLimits();
            if(!Running)
            {
                if(Inventory.FreeFor(Recipe.output)<Recipe.yield||!Recipe.CanMake(Input))return false;
                foreach(var input in Recipe.inputs)if(Input.Available(input.id)<input.count)return false;
                if(!Inventory.TryReserveSpace(Recipe.output,Recipe.yield))return false;
                if(!Recipe.Consume(Input)){Inventory.ReleaseSpace(Recipe.output,Recipe.yield);return false;}
                outputReserved=true;Running=true;Remaining=Recipe.seconds;
            }
            Remaining=Mathf.Max(0,Remaining-delta*(1+.15f*(Level-1)));
            if(Rotor)Rotor.Rotate(Vector3.up,130*delta,Space.Self);
            if(Remaining==0)
            {
                if(Inventory.AddIntoReservedSpace(Recipe.output,Recipe.yield)){outputReserved=false;Running=false;Batches++;WorkCount++;}
            }
            return true;
        }
        public void BreakDown(int fee){if(Broken)return;Broken=true;RepairFee=Mathf.Clamp(fee,10,100);RepairRemaining=8;RepairPaid=false;}
        public bool Repair(Economy economy,float delta,EntityId actorId)
        {
            if(!IsUnlocked||!Broken||economy==null||delta<=0||float.IsNaN(delta)||float.IsInfinity(delta)||!Lease(actorId))return false;
            if(!RepairPaid){if(!economy.TrySpend(RepairFee))return false;RepairPaid=true;}
            RepairRemaining=Mathf.Max(0,RepairRemaining-delta);
            if(RepairRemaining==0){Broken=false;RepairPaid=false;}return true;
        }
        public override bool Work(Inventory carrier,float delta,EntityId actorId)
        {
            if(!IsUnlocked||carrier==null||Recipe==null||delta<=0)return false;
            ConfigureInputLimits();
            if(Broken)return Repair(GameSession.Instance.Economy,delta,actorId);
            foreach(var input in Recipe.inputs)if(carrier.Count(input.id)>0&&Inventory.Transfer(carrier,Input,input.id,1)>0)return true;
            if(Inventory.Count(Recipe.output)>0&&Inventory.Transfer(Inventory,carrier,Recipe.output,1)>0)return true;
            return Operate(delta,actorId);
        }
        public override bool Interact(PlayerController player,bool withdraw)=>player&&ContainsInteractionPoint(player.transform.position)&&Work(player.Carry,Time.deltaTime,player.GetEntityId());
        public override StationProgressSave CaptureProgress(){var s=base.CaptureProgress();s.remaining=Remaining;s.running=Running;s.batches=Batches;s.broken=Broken;s.repairPaid=RepairPaid;s.repairRemaining=RepairRemaining;s.repairFee=RepairFee;return s;}
        public override void RestoreProgress(StationProgressSave s)
        {
            if(s==null||s.remaining<0||s.repairRemaining<0||s.batches<0)throw new System.IO.InvalidDataException("Trạng thái máy không hợp lệ");
            if(outputReserved&&Recipe!=null&&Inventory!=null)Inventory.ReleaseSpace(Recipe.output,Recipe.yield);
            outputReserved=false;
            base.RestoreProgress(s);Remaining=s.remaining;Running=s.running;Batches=s.batches;Broken=s.broken;RepairPaid=s.repairPaid;RepairRemaining=s.repairRemaining;RepairFee=s.repairFee;
            ConfigureInputLimits();
            if(Running)
            {
                if(Recipe==null||Inventory==null||!Inventory.TryReserveSpace(Recipe.output,Recipe.yield))throw new System.IO.InvalidDataException("Đầu ra máy không đủ chỗ cho mẻ đang chạy");
                outputReserved=true;
            }
        }
    }
    public sealed class UnlockVisual : MonoBehaviour
    {
        public string Requirement;
        public string[] AnyRequirements;
        bool? previous;
        void Update() => RefreshVisibility();
        bool RequirementMet() => AnyRequirements != null && AnyRequirements.Length > 0
            ? System.Array.Exists(AnyRequirements, id => GameSession.Instance.Economy.Has(id))
            : GameSession.Instance.Economy.Has(Requirement);
        public void RefreshVisibility()
        {
            if (!GameSession.Instance) return;
            bool visible = true;
            foreach (var gate in GetComponentsInParent<UnlockVisual>()) visible &= gate.RequirementMet();
            if (visible == previous) return;
            // Gate cha không bật lại phần vẫn bị khóa bởi gate con.
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                if (renderer.GetComponentInParent<UnlockVisual>() == this) renderer.enabled = visible && !renderer.name.EndsWith("Collision");
            foreach (var collider in GetComponentsInChildren<Collider>(true))
                if (collider.GetComponentInParent<UnlockVisual>() == this) collider.enabled = visible;
            previous = visible;
        }
    }
    public sealed class AnimalMotion : MonoBehaviour
    {
        public Vector3 Origin;
        public float Phase;
        public float Radius=.18f;
        ActorView view;
        void Start(){Origin=transform.localPosition;view=GetComponent<ActorView>();}
        void Update()
        {
            float angle=Time.time*.65f+Phase;
            Vector3 offset=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*Radius;
            Vector3 direction=new Vector3(Mathf.Cos(angle),0,-Mathf.Sin(angle));
            transform.localPosition=Origin+offset;
            transform.localRotation=Quaternion.Slerp(transform.localRotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-2.5f*Time.deltaTime));
            if(view){view.SetMotion(.5f,false);view.Animator.speed=.6f;}
        }
    }
}


