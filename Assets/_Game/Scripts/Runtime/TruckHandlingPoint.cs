using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed partial class TruckLogistics
    {
        Vector3 HandlingPoint(WorkerAgent worker, Vector3 dock, bool pickup)
        {
            Vector3 point = dock + (pickup ? Vector3.left * 1.8f : Vector3.back * 2);
            var hull = Vehicle ? Vehicle.GetComponent<Collider>() : null;
            if (hull)
            {
                var body = worker.GetComponent<CharacterController>();
                float radius = Mathf.Max(worker.Agent.radius,
                    NavMesh.GetSettingsByID(worker.Agent.agentTypeID).agentRadius);
                float clearance = Mathf.Max(radius, body ? body.radius : 0) + (body ? body.skinWidth : 0);
                var bounds = hull.bounds;
                // Khi xe quay ngang, điểm bốc cũ nằm trong thân xe; lấy mép collider cộng bán kính người.
                if (pickup) point.x = Mathf.Min(point.x, bounds.min.x - clearance);
                else point.z = Mathf.Min(point.z, bounds.min.z - clearance);
            }
            // Dùng cùng điểm NavMesh để đi và xét tới nơi, tránh đứng ngoài xe mà vẫn chờ vô hạn.
            return NavMesh.SamplePosition(point, out var hit, 1.8f, worker.Agent.areaMask)
                ? hit.position : point;
        }
    }
}
