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

        public override string Prompt=>"Băng chuyền • "+Definitions.Item(ItemId)?.label+" • đang chuyển "+(Inventory?.Count(ItemId)??0);
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
