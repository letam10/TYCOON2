using System.Linq;

namespace Tycoon
{
    public static class CarryLimits
    {
        public static int Player(TransactionState state)
        {
            int capacity = state.unlocked.Contains("carry24") ? 24 :
                state.unlocked.Contains("carry16") ? 16 : state.unlocked.Contains("carry10") ? 10 : 6;
            return state.schemaVersion >= PhysicalCashRules.Version ? capacity * 2 : capacity;
        }

        public static int Worker(CrewState crew, bool physical = true)
        {
            int capacity = crew.role == "Loader" ? 6 * crew.carryLevel :
                crew.carryLevel >= 3 ? 16 : crew.carryLevel == 2 ? 10 : 6;
            return physical ? capacity * 3 : capacity;
        }

        public static void Apply(TransactionState state)
        {
            foreach (var owner in state.owners)
            {
                if (owner.kind == OwnerKind.Player) owner.capacity = Player(state);
                if (owner.kind != OwnerKind.Worker) continue;
                var crew = state.crews.FirstOrDefault(x => x.id == owner.worker?.upgrade);
                if (crew != null) owner.capacity = Worker(crew, state.schemaVersion >= PhysicalCashRules.Version);
            }
        }
    }
}
