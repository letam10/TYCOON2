using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class WorkerAgent
    {
        public bool Retiring { get; private set; }
        public bool Retired { get; private set; }
        float retirementHandling;

        public void RequestRetirement()
        {
            Retiring = true;
        }

        void TickRetirement()
        {
            var game = GameSession.Instance;
            StopWorking();
            Reason = "Bàn giao hàng trước khi nghỉ";
            if (phase is 1 or 2)
            {
                Transport();
                return;
            }
            if (Role == "Loader" && game.Transactions != null && game.Logistics.HasCargo(this))
            {
                DepositRetiringCrate(game);
                return;
            }
            if (Carry.Total > 0)
            {
                BeginDrop(game.StorageFor(crew.area));
                return;
            }
            if (game.Transactions != null)
            {
                var state = game.Transactions.View;
                var owner = state.owners.Find(x => x.id == RuntimeTransactions.WorkerOwner(WorkerId));
                if (owner == null || WorkforceRules.HasObligations(state, owner)) return;
                if (!game.Transactions.RetireWorkforce(this)) return;
            }
            Retired = true;
            game.Workers.Remove(this);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        void DepositRetiringCrate(GameSession game)
        {
            var state = game.Transactions.View;
            string actor = game.Transactions.Actor(GetEntityId());
            var box = state.crates.FirstOrDefault(x => x.holder == actor);
            var truck = state.truck;
            if (box == null || truck == null || truck.phase == "Travelling" ||
                game.StorageFor(crew.area)?.Id != truck.current) return;
            Vector3 goal = TruckRoutes.Dock(truck.current) + Vector3.back * 2;
            if ((transform.position - goal).sqrMagnitude > 1.2f)
            {
                Navigation.Go(Agent, goal);
                return;
            }
            if (Agent.hasPath) Agent.ResetPath();
            retirementHandling += Time.deltaTime * WorkforceRules.Productivity(crew.speedLevel);
            if (retirementHandling < .7f) return;
            retirementHandling = 0;
            var kind = box.task == "unload" ? TransactionKind.UnloadCrate : TransactionKind.LoadCrate;
            if (game.Transactions.TryExecute(game.Transactions.Command(kind, actor, box.id), out _))
                View.Interact(false);
        }
    }
}
