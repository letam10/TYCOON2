using UnityEngine;

namespace Tycoon
{
    public sealed class MachineStation : Station
    {
        RecipeDefinition recipe;
        public string[] RecipeOptions;
        public string[] Options=>RecipeOptions??new[]{recipe.id};
        public RecipeDefinition Recipe {get=>Runtime==null?recipe:Definitions.Recipe(Runtime.definitionId);set=>recipe=value;}
        [System.NonSerialized] public Inventory Input=new(36);
        bool running,broken,repairPaid;float remaining,repairRemaining=5;int batches,playerBatches,repairFee=20;
        public bool Running {get=>Runtime?.running??running;set{GuardState();running=value;}}
        public bool Broken {get=>Runtime?.progress.broken??broken;set{GuardState();broken=value;}}
        public bool RepairPaid {get=>Runtime?.progress.repairPaid??repairPaid;set{GuardState();repairPaid=value;}}
        public float Remaining {get=>(float)(Runtime?.remaining??remaining);set{GuardState();remaining=value;}}
        public float RepairRemaining {get=>Runtime?.progress.repairRemaining??repairRemaining;set{GuardState();repairRemaining=value;}}
        public int Batches {get=>Runtime?.batches??batches;set{GuardState();batches=value;}}
        public int PlayerBatches=>Runtime?.playerBatches??playerBatches;
        public int RepairFee {get=>Runtime?.progress.repairFee??repairFee;set{GuardState();repairFee=value;}}
        void GuardState(){if(Runtime!=null)throw new System.InvalidOperationException("Machine chỉ được cập nhật qua transaction.");}
        public Transform Rotor;
        public MachinePhase Phase=>Runtime?.machinePhase??(Recipe==null?MachinePhase.WaitingInput:Running?MachinePhase.Operating:
            Inventory!=null&&Inventory.Count(Recipe.output)>0?MachinePhase.CompletedWaitingPickup:
            Recipe.CanMake(Input)&&Inventory!=null&&Inventory.FreeFor(Recipe.output)>=Recipe.yield?MachinePhase.Ready:MachinePhase.WaitingInput);
        public bool HasOperator=>Runtime!=null?!Runtime.autonomous&&!string.IsNullOrEmpty(Runtime.operatorId)&&Runtime.operatorUntil>Authority.Now:Time.time<operatorUntil;
        public string WaitReason
        {
            get
            {
                if(Broken)return "Máy đang hỏng • chờ sửa chữa.";
                if(Recipe==null)return "Sai recipe";
                foreach(var input in Recipe.inputs)if(Input.Available(input.id)<input.count)return "Thiếu "+Definitions.Item(input.id).label;
                if(Inventory!=null&&Inventory.FreeFor(Recipe.output)<Recipe.yield)return "Đầu ra đầy • lấy "+Definitions.Item(Recipe.output).label;
                return "Chờ nguyên liệu";
            }
        }
        public override string Prompt=>Broken?Label+" • máy hỏng, sửa "+RepairFee+" xu • "+RepairRemaining.ToString("0.0")+"s":Label+" • "+(Phase switch
        {
            MachinePhase.Ready=>"Sẵn sàng • tự chạy khi đủ nguyên liệu",
            MachinePhase.Operating=>"Đang chạy • "+Remaining.ToString("0.0")+"s • giữ đầu ra "+Inventory.ReservedSpace(Recipe.output),
            MachinePhase.CompletedWaitingPickup=>"Hoàn tất • chờ lấy "+Recipe?.label,
            _=>WaitReason
        });
        bool outputReserved;
        float tick;
        void Update()
        {
            if(!IsUnlocked || !GameSession.Instance.CanSimulate || Authority==null)return;
            tick+=Time.deltaTime;if(tick<.2f)return;float elapsed=tick;tick=0;
            if(Authority.TickMachine(this,elapsed)&&Rotor)Rotor.Rotate(Vector3.up,130*elapsed,Space.Self);
        }
        void Start()=>ConfigureInputLimits();
        public void ConfigureInputLimits(){if(recipe==null||Input.Authority!=null)return;var items=new System.Collections.Generic.Dictionary<string,int>();foreach(string id in Options)foreach(var i in Definitions.Recipe(id).inputs)items[i.id]=Mathf.Max(items.TryGetValue(i.id,out int old)?old:0,i.count);foreach(var i in items)Input.SetLimit(i.Key,Mathf.Max(i.Value,Input.Capacity/Mathf.Max(1,items.Count)));}
        public bool Operate(float delta,EntityId actorId)
        {
            if(!IsUnlocked||Broken||delta<=0||float.IsNaN(delta)||float.IsInfinity(delta)||Recipe==null||Inventory==null)return false;
            if(Authority!=null){bool worked=Authority.OperateMachine(this,delta,actorId);if(worked&&Rotor)Rotor.Rotate(Vector3.up,130*delta,Space.Self);return worked;}
            if(!Lease(actorId))return false;
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
                if(Inventory.AddIntoReservedSpace(Recipe.output,Recipe.yield))
                {outputReserved=false;Running=false;Batches++;WorkCount++;if(GameSession.Instance?.Player&&actorId==GameSession.Instance.Player.GetEntityId()){playerBatches++;PlayerWorkCount++;}}
            }
            return true;
        }
        public void BreakDown(int fee)
        {
            if(Broken||GameSession.Instance&&GameSession.Instance.Machines.Exists(x=>x&&x!=this&&x.Broken))return;
            if(Authority!=null){var command=Authority.Command(TransactionKind.BreakMachine,"simulation",Id);command.quantity=fee;Authority.TryExecute(command,out _);return;}
            Broken=true;RepairFee=Mathf.Clamp(fee,10,100);RepairRemaining=5;RepairPaid=false;
        }
        public bool Repair(Economy economy,float delta,EntityId actorId)
        {
            if(!IsUnlocked||!Broken||economy==null||delta<=0||float.IsNaN(delta)||float.IsInfinity(delta))return false;
            if(Authority!=null)return Authority.StationAction(TransactionKind.RepairMachine,this,actorId,delta);
            if(!Lease(actorId))return false;
            if(!RepairPaid){if(!economy.TrySpend(RepairFee))return false;RepairPaid=true;}
            RepairRemaining=Mathf.Max(0,RepairRemaining-delta);
            if(RepairRemaining==0){Broken=false;RepairPaid=false;}return true;
        }
        public override bool Work(Inventory carrier,float delta,EntityId actorId)
        {
            if(!IsUnlocked||carrier==null||Recipe==null||delta<=0)return false;
            ConfigureInputLimits();
            if(Broken)return false;
            foreach(var input in Recipe.inputs)if(carrier.Count(input.id)>0&&Inventory.Transfer(carrier,Input,input.id,1)>0)return true;
            if(Inventory.Count(Recipe.output)>0&&Inventory.Transfer(Inventory,carrier,Recipe.output,1)>0)return true;
            return Operate(delta,actorId);
        }
        public override bool Interact(PlayerController player,bool withdraw)=>player&&ContainsInteractionPoint(player.transform.position)&&Work(player.Carry,Time.deltaTime,player.GetEntityId());
        public override StationProgressSave CaptureProgress(){var s=base.CaptureProgress();s.remaining=Remaining;s.running=Running;s.batches=Batches;s.playerBatches=PlayerBatches;s.broken=Broken;s.repairPaid=RepairPaid;s.repairRemaining=RepairRemaining;s.repairFee=RepairFee;return s;}
        public override void RestoreProgress(StationProgressSave s)
        {
            if(s==null||s.remaining<0||s.repairRemaining<0||s.batches<0||s.playerBatches<0||s.playerBatches>s.batches)throw new System.IO.InvalidDataException("Trạng thái máy không hợp lệ");
            if(outputReserved&&Recipe!=null&&Inventory!=null)Inventory.ReleaseSpace(Recipe.output,Recipe.yield);
            outputReserved=false;
            base.RestoreProgress(s);Remaining=s.remaining;Running=s.running;Batches=s.batches;playerBatches=s.playerBatches;Broken=s.broken;RepairPaid=s.repairPaid;RepairRemaining=s.repairRemaining;RepairFee=s.repairFee;
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
        public Station Target;
        bool? previous;
        void Update() => RefreshVisibility();
        bool RequirementMet() => Target?GameSession.Instance.Economy.Has(Target.Requirement):AnyRequirements != null && AnyRequirements.Length > 0
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
    public sealed class LivestockPopulationView : MonoBehaviour
    {
        public GameObject[] Members=System.Array.Empty<GameObject>();
        ProductionStation[] producers=System.Array.Empty<ProductionStation>();
        public void Bind(ProductionStation[] value)=>producers=value??System.Array.Empty<ProductionStation>();
        void LateUpdate()
        {
            int population=0;foreach(var producer in producers)if(producer&&producer.IsUnlocked)population+=producer.Herd;
            population=Mathf.Min(population,Members.Length);
            for(int i=0;i<Members.Length;i++)if(Members[i]&&Members[i].activeSelf!=(i<population))Members[i].SetActive(i<population);
        }
    }
}


