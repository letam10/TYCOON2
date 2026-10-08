using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class TruckLogistics
    {
        CargoPlayerArea takeArea;
        CargoPlayerArea placeArea;

        public IEnumerable<IPlayerInteractionArea> PlayerAreas()
        {
            if (State == null || game.Transactions.View.schemaVersion < 3) yield break;
            takeArea ??= new CargoPlayerArea(this, true);
            placeArea ??= new CargoPlayerArea(this, false);
            yield return takeArea;
            yield return placeArea;
        }

        public Vector3 PlayerCargoPoint(bool take)
        {
            return State == null ? Vector3.zero : TruckRoutes.Dock(State.current) +
                (take ? Vector3.left * 2.4f : Vector3.back * 3.1f);
        }

        public bool PlayerCargoAvailable => State != null && State.phase != "Travelling" &&
            game.CanSimulate && game.Player.Carry.Total == 0;

        InteractionResult PhysicalInteract(CargoDock dock, InteractionContext context)
        {
            if (game.Economy.CashInHand > 0)
                return InteractionResult.Reject("Gửi tiền vào két để mang hàng.");
            if (context.Carry.Total > 0)
            {
                var item = context.Carry.Snapshot()[0];
                bool packed = Act(TransactionKind.PackCrate, "player", "crate:" + System.Guid.NewGuid().ToString("N"),
                    "player", item.id, Mathf.Min(6, item.count));
                return packed ? new InteractionResult(true) : InteractionResult.Reject(Reason);
            }
            return InteractionResult.Reject("Lấy kiện bên trái bến • giao kiện ở phía sau xe.");
        }

        public InteractionResult HandlePlayerCargo(bool take)
        {
            if (!PlayerCargoAvailable) return InteractionResult.Waiting;
            var crates = game.Transactions.View.crates;
            CargoCrateState box;
            TransactionKind kind;
            if (take)
            {
                string holder = State.phase == "WaitingUnload" ? State.id : "dock:" + State.current;
                box = crates.FirstOrDefault(x => x.holder == holder);
                kind = TransactionKind.ClaimCrate;
            }
            else
            {
                box = crates.FirstOrDefault(x => x.holder == "player");
                kind = box?.task == "unload" ? TransactionKind.UnloadCrate : TransactionKind.LoadCrate;
            }
            if (box == null) return InteractionResult.Reject(take ? "Chưa có kiện để lấy." : "Tay chưa có kiện.");
            if (!Act(kind, "player", box.id)) return InteractionResult.Reject(Reason);
            game.Player.View?.Interact(take);
            game.Feedback?.ItemTransfer(game.Player.transform.position + Vector3.up, take);
            return new InteractionResult(true);
        }
    }

    public sealed class CargoPlayerArea : IPlayerInteractionArea
    {
        readonly TruckLogistics logistics;
        readonly bool take;
        float delay;
        public CargoPlayerArea(TruckLogistics logistics, bool take)
        {
            this.logistics = logistics;
            this.take = take;
        }
        public InteractionKind Kind => take ? InteractionKind.Pickup : InteractionKind.Drop;
        public Vector3 Center => logistics.PlayerCargoPoint(take);
        public bool Available => logistics && logistics.PlayerCargoAvailable;
        public bool Contains(Vector3 point) => Vector3.ProjectOnPlane(point - Center, Vector3.up).sqrMagnitude < .64f;
        public string Hint => take ? "Lấy kiện lên tay" : "Giao kiện vào xe / kho";
        public InteractionResult Perform(InteractionContext context, float delta)
        {
            if (!context.Player || !Contains(context.Player.transform.position)) return InteractionResult.Waiting;
            delay += delta;
            if (delay < .3f) return InteractionResult.Waiting;
            delay = 0;
            return logistics.HandlePlayerCargo(take);
        }
        public void Exit(EntityId actor) => delay = 0;
    }
}
