using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        float nextDiningDiagnostic;

        void RecordDiningNavigation()
        {
            if (Time.realtimeSinceStartup < nextDiningDiagnostic) return;
            nextDiningDiagnostic = Time.realtimeSinceStartup + 5;
            string[] lines = game.Restaurant.Diners.Select(diner =>
            {
                var agent = diner.Agent;
                var route = diner.GetComponent<CityPedestrianRoute>();
                bool active = agent && agent.enabled && agent.isOnNavMesh;
                string navigation = active
                    ? $"status={agent.pathStatus} remain={agent.remainingDistance:F2} stop={agent.isStopped}"
                    : "navigation suspended";
                return $"t={Time.time:F1} receipt={diner.Receipt} phase={diner.Phase} table={diner.Table.Id} "
                    + $"pos={diner.transform.position} final={route.FinalDestination} nodes={route.RemainingNodes} "
                    + navigation;
            }).ToArray();
            File.AppendAllLines(Path.Combine(game.QaDirectory, "dining-navigation.txt"), lines);
        }
    }
}
