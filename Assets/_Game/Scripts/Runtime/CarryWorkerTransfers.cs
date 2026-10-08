using UnityEngine;

namespace Tycoon
{
    public sealed class CarryWorkerTransfers : MonoBehaviour
    {
        public WorkerAgent Worker;
        Inventory observed;
        int count;
        string itemId;
        float nextAnimation;

        void LateUpdate()
        {
            if (!Worker) return;
            var inventory = Worker.Carry;
            if (!ReferenceEquals(observed, inventory))
            {
                observed = inventory;
                count = inventory.Total;
                itemId = count > 0 ? inventory.Snapshot()[0].id : null;
                return;
            }
            int total = inventory.Total;
            if (total == count) return;
            bool taking = total > count;
            if (taking) itemId = inventory.Snapshot()[0].id;
            if (GameSession.Instance && GameSession.Instance.CanSimulate && Time.time >= nextAnimation &&
                !string.IsNullOrEmpty(itemId))
            {
                Vector3 station = Worker.Agent && Worker.Agent.enabled && Worker.Agent.isOnNavMesh
                    ? Worker.Agent.destination : Worker.transform.position + Worker.transform.forward;
                station.y = transform.position.y;
                CarryTransferVisual.Play(itemId, taking ? station : transform.position,
                    taking ? transform.position : station, GameSession.Instance.transform);
                nextAnimation = Time.time + .15f;
            }
            count = total;
        }
    }
}
