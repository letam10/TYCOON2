using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed class CityPedestrianRoute : MonoBehaviour
    {
        NavMeshAgent agent;
        readonly List<Vector3> points = new();
        int next;
        bool active;
        public Vector3 FinalDestination { get; private set; }
        public int RemainingNodes => active ? points.Count - next - 1 : 0;
        public bool HasDestination => active;
        public bool FinalArrived => active && next == points.Count - 1 &&
            Reached(agent, FinalDestination);

        void Awake() => agent = GetComponent<NavMeshAgent>();

        public static bool Reached(NavMeshAgent value, Vector3 point) =>
            value && value.enabled && value.isOnNavMesh && !value.pathPending &&
            value.pathStatus == NavMeshPathStatus.PathComplete && value.remainingDistance < .5f &&
            Vector3.ProjectOnPlane(value.transform.position - point, Vector3.up).sqrMagnitude < .36f;

        public bool Go(Vector3 point, bool force)
        {
            if (!agent) agent = GetComponent<NavMeshAgent>();
            if (!agent || !agent.enabled || !agent.isOnNavMesh) return false;
            // Một số caller repath bằng agent.destination: giữ nguyên đích cuối của hành trình.
            if (force && active && (point - agent.destination).sqrMagnitude < .16f)
                point = FinalDestination;
            if (!NavMesh.SamplePosition(point, out var final, 1.8f, agent.areaMask)) return false;
            if (active && (FinalDestination - final.position).sqrMagnitude < .16f)
                return !force && (agent.hasPath || agent.pathPending) || SetNext();
            FinalDestination = final.position;
            points.Clear();
            foreach (var node in CityPedestrianNetwork.Plan(transform.position, final.position))
            {
                if (!NavMesh.SamplePosition(node, out var hit, 2, agent.areaMask))
                {
                    active = false;
                    return false;
                }
                points.Add(hit.position);
            }
            next = 0;
            active = points.Count > 0;
            return active && SetNext();
        }

        bool SetNext()
        {
            bool accepted = agent.SetDestination(points[next]);
            if (GameSession.Instance && !GameSession.Instance.CanSimulate) agent.isStopped = true;
            return accepted;
        }

        void Update()
        {
            if (!active || !agent || !agent.enabled || !agent.isOnNavMesh ||
                GameSession.Instance && !GameSession.Instance.CanSimulate) return;
            if (next < points.Count - 1 && Reached(agent, points[next]))
            {
                next++;
                SetNext();
            }
        }

        public void Clear()
        {
            active = false;
            points.Clear();
            next = 0;
        }

        void OnDisable() => Clear();
    }
}
