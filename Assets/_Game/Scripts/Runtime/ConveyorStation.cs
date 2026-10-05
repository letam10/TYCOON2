using UnityEngine;

namespace Tycoon
{
    public sealed class ConveyorStation : Station
    {
        public StorageStation[] Sources;
        public MachineStation Target;
        public string ItemId;
        public GameObject VisualRoot;
        float nextTransfer;

        public string StoppedReason
        {
            get
            {
                if(!IsUnlocked)return "Chưa mở tuyến";
                if(!Target||!Target.IsUnlocked)return "Chờ mở máy nhận";
                if(Target.Broken)return "Máy hỏng • chờ sửa";
                int amount=System.Array.Find(Target.Recipe.inputs,x=>x.id==ItemId)?.count??1;
                if(Target.Input.FreeFor(ItemId)<amount)return "Đầu nhận đầy / đã giữ chỗ";
                if(Sources==null||!System.Array.Exists(Sources,x=>x&&x.AvailableAboveReserve(ItemId)>=amount))return "Thiếu hàng trên mức dự trữ";
                return "Đang cấp hàng";
            }
        }
        public override string Prompt=>Definitions.Item(ItemId)?.label+" • trên đường "+(Inventory?.Count(ItemId)??0)+" • "+StoppedReason;
        public override bool Interact(PlayerController player,bool withdraw)=>false;

        void Update()
        {
            var game=GameSession.Instance;
            if(!game||!game.CanSimulate)return;
            bool open=IsUnlocked;
            if(VisualRoot&&VisualRoot.activeSelf!=open)VisualRoot.SetActive(open);
            if(open&&game.Transactions!=null&&Time.time>=nextTransfer)
            {
                nextTransfer=Time.time+.25f;
                game.Transactions.TickConveyor(this);
            }
        }
    }
}
