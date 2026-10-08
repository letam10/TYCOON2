using UnityEngine;

namespace Tycoon
{
    internal static class CityWorkerMigration
    {
        internal static Vector3 Offset(SaveData data, WorkerSave worker, Vector3? truckDelta)
        {
            var state = data.transactionState;
            var crew = state?.crews.Find(x => x.id == worker.upgrade) ??
                data.crews.Find(x => x.id == worker.upgrade);
            if (crew == null && !string.IsNullOrEmpty(worker.upgrade))
            {
                var upgrade = Definitions.Upgrade(worker.upgrade);
                if (upgrade?.kind == "worker") crew = GameSession.CrewFor(upgrade);
            }
            if (crew?.role == "Driver" && state?.truck != null)
                return truckDelta ?? CityDistricts.Offset(Area(state, state.truck.current));

            // Loader làm việc ở bến của đội, dù bến cũ nằm sát khu khác.
            if (crew?.role == "Loader") return CityDistricts.Offset(crew.area);

            string target = worker.phase == 1 ? worker.source :
                worker.phase == 2 ? worker.destination : worker.working;
            string area = Area(state, target);
            if (string.IsNullOrEmpty(area)) area = crew?.area;
            return string.IsNullOrEmpty(area) ?
                CitySaveMigration.PositionOffset(new(worker.x, 0, worker.z)) : CityDistricts.Offset(area);
        }

        static string Area(TransactionState state, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var station = state?.stations.Find(x => x.id == id || x.input == id || x.output == id);
            if (!string.IsNullOrEmpty(station?.area)) return station.area;
            if (id.StartsWith("dock_") || id.StartsWith("dock:")) id = id.Substring(5);
            if (id.StartsWith("storage_")) return id.Substring(8);
            return null;
        }
    }
}
