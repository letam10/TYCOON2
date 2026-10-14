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
        public string LastFailure { get; private set; }
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
            LastFailure = null;
            if (!agent) agent = GetComponent<NavMeshAgent>();
            if (!agent || !agent.enabled || !agent.isOnNavMesh)
                return Fail("agent is not on NavMesh");
            // Một số caller repath bằng agent.destination: giữ nguyên đích cuối của hành trình.
            if (force && active && (point - agent.destination).sqrMagnitude < .16f)
                point = FinalDestination;
            if (!NavMesh.SamplePosition(point, out var final, 1.8f, agent.areaMask))
                return Fail("destination is outside NavMesh");
            if (active && (FinalDestination - final.position).sqrMagnitude < .16f)
                return !force && (agent.hasPath || agent.pathPending) || SetNext();

            FinalDestination = final.position;
            if (!CityPedestrianNetwork.TryPlan(transform.position, final.position, out var plan, out string failure))
                return Fail(failure);
            points.Clear();
            for (int index = 0; index < plan.Count; index++)
            {
                if (!NavMesh.SamplePosition(plan[index], out var hit, 2, agent.areaMask))
                    return Fail("route waypoint is outside NavMesh");
                points.Add(hit.position);
            }
            if (points.Count == 0)
                return Fail("route planner returned no waypoints");
            if (!ValidateNavMeshLegs())
                return Fail("route waypoint graph is disconnected on NavMesh");
            next = 0;
            active = true;
            return SetNext();
        }

        bool ValidateNavMeshLegs()
        {
            var path = new NavMeshPath();
            Vector3 previous = transform.position;
            for (int index = 0; index < points.Count; index++)
            {
                if (!NavMesh.CalculatePath(previous, points[index], agent.areaMask, path) ||
                    path.status != NavMeshPathStatus.PathComplete)
                    return false;
                previous = points[index];
            }
            return true;
        }

        bool SetNext()
        {
            if (next < 0 || next >= points.Count)
                return Fail("route waypoint index is invalid");
            bool accepted = agent.SetDestination(points[next]);
            if (!accepted)
                return Fail("NavMeshAgent rejected route waypoint");
            if (GameSession.Instance && !GameSession.Instance.CanSimulate) agent.isStopped = true;
            active = true;
            return true;
        }

        bool Fail(string reason)
        {
            LastFailure = string.IsNullOrWhiteSpace(reason) ? "pedestrian route failed" : reason;
            active = false;
            points.Clear();
            next = 0;
            if (agent && agent.enabled && agent.isOnNavMesh) agent.ResetPath();
            return false;
        }

        void Update()
        {
            using var updateProbe = QaFrameProbe.Measure("CityPedestrianRoute.1.Update");

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
            LastFailure = null;
        }

        void OnDisable() => Clear();
    }
}
