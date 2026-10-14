using UnityEngine;

namespace Tycoon
{
    public sealed partial class DinerAgent
    {
        DinerSeatingPose seating;
        Vector3 SavePosition => seating && seating.Seat && Table ? Table.Seat : transform.position;

        void InitializeSeating()
        {
            seating = gameObject.AddComponent<DinerSeatingPose>();
            seating.Initialize(Agent, View);
        }

        void SitAtTable(bool restored = false)
        {
            if (!seating || !Table) return;
            if (seating.IsSeated) return;
            var furniture = Table.GetComponentInChildren<DiningFurniture>();
            if (!furniture || furniture.Seats == null || furniture.Seats.Length == 0) return;
            seating.Sit(furniture.Seats[0], Table.Seat, restored);
        }

        void WalkAwayFromTable()
        {
            if (Agent) Navigation.Go(Agent, TownLayout.Spawn((int)(Receipt % 3)));
            if (HasTerminalFeedback && Agent && Agent.enabled && Agent.isOnNavMesh) Agent.isStopped = true;
        }

        void LeaveSeat()
        {
            if (seating) seating.Leave(WalkAwayFromTable);
            else WalkAwayFromTable();
        }
    }
}
