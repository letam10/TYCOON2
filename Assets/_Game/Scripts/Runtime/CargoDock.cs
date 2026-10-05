using UnityEngine;
namespace Tycoon
{
    public sealed class CargoDock:Station
    {
        public string WarehouseId;
        public override string Prompt=>"Bến "+GameHud.AreaLabel(AreaId)+" • đóng / chất / dỡ thùng";
        public override InteractionResult Perform(InteractionKind kind,InteractionContext context,float delta)=>GameSession.Instance.Logistics.Interact(this,context);
        public override bool Interact(PlayerController player,bool withdraw)=>Perform(InteractionKind.Operate,new(player,Definitions.Items[GameSession.Instance.SelectedItem].id),Time.deltaTime).Worked;
    }
}
