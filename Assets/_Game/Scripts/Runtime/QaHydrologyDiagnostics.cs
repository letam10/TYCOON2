using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        void HydrologyNavigationDiagnostic(string label, Vector3 start, Vector3 target, NavMeshPath path)
        {
            var lines = new List<string>
            {
                label + " from=" + start + " target=" + target + " status=" + path.status
            };
            foreach (Vector3 corner in path.corners)
                lines.Add("corner=" + corner);
            if (path.corners.Length > 0)
            {
                Vector3 end = path.corners[path.corners.Length - 1];
                foreach (Collider obstacle in Physics.OverlapSphere(end, 3, ~0, QueryTriggerInteraction.Ignore))
                    lines.Add("frontier collider=" + obstacle.name + " center=" + obstacle.bounds.center
                        + " size=" + obstacle.bounds.size);
            }
            foreach (Vector3 probe in new[]
            {
                new Vector3(-195, 0, 130), new(-195, 0, 160), new(0, 0, 130), new(0, 0, 160),
                CoastalTownLayout.IndustrialEntry, CoastalTownLayout.IndustrialVisit
            })
            {
                if (!NavMesh.SamplePosition(probe, out NavMeshHit hit, 3, NavMesh.AllAreas))
                {
                    lines.Add("probe missing=" + probe);
                    continue;
                }
                var check = new NavMeshPath();
                NavMesh.CalculatePath(start, hit.position, NavMesh.AllAreas, check);
                lines.Add("probe=" + probe + " sampled=" + hit.position + " path=" + check.status);
            }
            var overlapping = game.GetComponentsInChildren<NavMeshSurface>()
                .Where(surface => surface.name == "WorldExpansionDressing").ToArray();
            try
            {
                foreach (NavMeshSurface surface in overlapping)
                    surface.RemoveData();
                var check = new NavMeshPath();
                NavMesh.CalculatePath(start, target, NavMesh.AllAreas, check);
                lines.Add("without overlapping expansion surface=" + check.status);
                foreach (Vector3 corner in check.corners)
                    lines.Add("primary corner=" + corner);
            }
            finally
            {
                foreach (NavMeshSurface surface in overlapping)
                    surface.AddData();
            }
            File.AppendAllLines(Path.Combine(game.QaDirectory, "hydrology-navigation.txt"), lines);
        }
    }
}
