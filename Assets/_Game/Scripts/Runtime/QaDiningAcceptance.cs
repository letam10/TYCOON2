using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator DiningAcceptance()
        {
            Time.timeScale = 0;
            Check(game.Tables.Count == 6, "dining: existing six table owners preserved");
            foreach (var table in game.Tables)
            {
                var furniture = table.GetComponentInChildren<DiningFurniture>();
                Check(furniture && furniture.Seats.Length == 2, "dining: two seats " + table.Id);
                Check(Vector3.Distance(table.Seat, furniture.Approach.position) < .001f,
                    "dining: approach follows expanded district " + table.Id);
                Check(NavMesh.SamplePosition(table.Seat, out _, .5f, NavMesh.AllAreas),
                    "dining: approach on navigation " + table.Id);
                var path = new NavMeshPath();
                Check(NavMesh.CalculatePath(table.InteractionPoint, table.Seat, NavMesh.AllAreas, path)
                    && path.status == NavMeshPathStatus.PathComplete, "dining: service path " + table.Id);
                CheckModelGeometry(furniture.transform, table.Id);
            }
            var counter = game.GetComponentsInChildren<DiningFurniture>(true)
                .FirstOrDefault(x => x.Shape == DiningTableShape.Counter);
            Check(counter && counter.Seats.Length == 3, "dining: three counter stools");
            yield return CaptureModelAngles("dining-counter", ModelBounds(counter.transform).center, 9);
            Time.timeScale = 1;
            foreach (var shape in new[] { DiningTableShape.Round, DiningTableShape.Square })
            {
                DinerAgent diner = null;
                yield return TownWait(() =>
                {
                    RecordDiningNavigation();
                    diner = game.Restaurant.Diners.FirstOrDefault(x => x.Phase is 1 or 2
                        && x.Table.GetComponentInChildren<DiningFurniture>().Shape == shape
                        && x.GetComponent<DinerSeatingPose>().IsSeated
                        && !x.GetComponent<DinerSeatingPose>().IsMoving);
                    return diner;
                }, 120, "dining: real diner sits at " + shape);
                Time.timeScale = 0;
                var pose = diner.GetComponent<DinerSeatingPose>();
                var hips = diner.View.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == "Hips");
                Check(hips && Vector3.Distance(hips.position, pose.Seat.position) < .06f,
                    "dining: actual hip rests on cushion " + shape);
                Check(!diner.Agent.enabled, "dining: seated navigation suspended " + shape);
                yield return CaptureModelAngles("dining-customer-" + shape,
                    diner.Table.transform.position + Vector3.up * .8f, 7);
                long receipt = diner.Receipt;
                int phase = diner.Phase;
                game.SaveGame();
                game.LoadGame();
                Time.timeScale = 1;
                yield return TownWait(() => game.CanSimulate, 8, "dining: load seated customer " + shape);
                yield return TownWait(() => game.Restaurant.Diners.Any(x => x.Receipt == receipt),
                    8, "dining: seated receipt restored " + shape);
                var restored = game.Restaurant.Diners.First(x => x.Receipt == receipt);
                Check(restored.Phase == phase && restored.View.State == "Sit"
                    && restored.GetComponent<DinerSeatingPose>().IsSeated,
                    "dining: saved phase and sit pose restored " + shape);
                yield return CapturePassengerMotion("dining-motion-" + shape,
                    restored.Table.transform.position + Vector3.up * .8f);
            }
        }
    }
}
