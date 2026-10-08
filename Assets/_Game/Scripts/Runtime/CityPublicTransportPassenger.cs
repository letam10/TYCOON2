using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public enum CityPassengerPhase { Walking, Approaching, Boarding, Seated, Disembarking }

    public sealed class CityPublicTransportPassenger : MonoBehaviour
    {
        NavMeshAgent agent;
        ActorPhysicalMotion motion;
        ActorView view;
        Transform originalParent;
        CityPublicTransportVehicle vehicle;
        Vector3 exit;
        Vector3 seat;
        Vector3 doorPosition;
        float cooldown;
        bool throughDoor;
        bool rode;
        bool detachPending;
        Vector3 releaseDoorLocal;
        Vector3 releaseExitLocal;
        Transform carrierHull;
        readonly RaycastHit[] movementHits = new RaycastHit[16];
        public CityPassengerPhase Phase { get; private set; }
        public bool IsTravelling => Phase != CityPassengerPhase.Walking;
        public bool CanBoard => !IsTravelling && Time.time >= cooldown;
        public int CompletedRides { get; private set; }

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            motion = GetComponent<ActorPhysicalMotion>();
            view = GetComponentInChildren<ActorView>();
            originalParent = transform.parent;
        }

        public bool Reserve(CityPublicTransportVehicle carrier, Vector3 seatPosition)
        {
            if (!agent) Awake();
            if (!CanBoard || !agent || !agent.enabled || !agent.isOnNavMesh) return false;
            vehicle = carrier;
            carrierHull = carrier.transform;
            rode = false;
            seat = seatPosition;
            Phase = CityPassengerPhase.Approaching;
            if (Navigation.Go(agent, carrier.DoorOutside)) return true;
            CancelApproach();
            return false;
        }

        public void CancelApproach()
        {
            if (Phase != CityPassengerPhase.Approaching) return;
            if (agent && agent.enabled && agent.isOnNavMesh) agent.ResetPath();
            agent.GetComponent<CityPedestrianRoute>()?.Clear();
            Phase = CityPassengerPhase.Walking;
            vehicle = null;
            cooldown = Time.time + 8;
        }

        void Update()
        {
            if (GameSession.Instance && !GameSession.Instance.CanSimulate || Time.deltaTime <= 0) return;
            Tick(Time.deltaTime);
        }

        public void Tick(float delta)
        {
            if (delta <= 0 || !IsTravelling || !gameObject.activeInHierarchy) return;
            if (detachPending)
            {
                if (carrierHull)
                {
                    doorPosition = carrierHull.TransformPoint(releaseDoorLocal);
                    exit = carrierHull.TransformPoint(releaseExitLocal);
                }
                transform.SetParent(originalParent, true);
                detachPending = false;
            }
            if (Phase == CityPassengerPhase.Approaching)
            {
                if (!vehicle || !vehicle.DoorsOpen)
                {
                    CancelApproach();
                    return;
                }
                if (view) view.SetMotion(Navigation.ActualSpeed(agent), false);
                if (!Navigation.Arrived(agent)) return;
                exit = vehicle.DoorOutside;
                Navigation.Suspend(agent);
                transform.SetParent(vehicle.transform, true);
                Phase = CityPassengerPhase.Boarding;
                throughDoor = false;
            }
            if (Phase == CityPassengerPhase.Boarding)
            {
                Vector3 target = vehicle ? vehicle.transform.TransformPoint(
                    throughDoor ? seat : vehicle.DoorInsideLocal) : exit;
                if (view) view.SetMotion(1.8f, false);
                if (!Move(target, delta)) return;
                if (!throughDoor)
                {
                    throughDoor = true;
                    return;
                }
                Phase = CityPassengerPhase.Seated;
                rode = true;
                if (vehicle) vehicle.RecordBoarding();
                transform.localRotation = Quaternion.identity;
                if (view) view.Play("Sit");
            }
            else if (Phase == CityPassengerPhase.Seated)
            {
                if (view) view.Play("Sit");
            }
            else if (Phase == CityPassengerPhase.Disembarking)
            {
                if (!throughDoor)
                {
                    if (!Move(doorPosition, delta)) return;
                    throughDoor = true;
                }
                if (view) view.SetMotion(1.8f, false);
                if (!ClearExit(exit) || !Move(exit, delta)) return;
                RestoreOutside();
            }
        }

        bool Move(Vector3 point, float delta)
        {
            Vector3 next = Vector3.MoveTowards(transform.position, point, delta * 1.8f);
            Vector3 direction = next - transform.position;
            if (direction.sqrMagnitude > .000001f)
            {
                int count = Physics.CapsuleCastNonAlloc(transform.position + Vector3.up * .36f,
                    transform.position + Vector3.up * 1.48f, .28f, direction.normalized,
                    movementHits, direction.magnitude, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    var hit = movementHits[i].collider.transform;
                    if (hit.IsChildOf(transform) || carrierHull && hit.IsChildOf(carrierHull)) continue;
                    return false;
                }
            }
            transform.position = next;
            return (transform.position - point).sqrMagnitude < .0025f;
        }

        public void Disembark()
        {
            if (Phase != CityPassengerPhase.Seated || !vehicle || !vehicle.DoorsOpen) return;
            exit = vehicle.DoorOutside;
            doorPosition = vehicle.transform.TransformPoint(vehicle.DoorInsideLocal);
            throughDoor = false;
            Phase = CityPassengerPhase.Disembarking;
        }

        public bool ClearExit(Vector3 point)
        {
            if (!NavMesh.SamplePosition(point, out var hit, .45f, NavMesh.AllAreas)) return false;
            if (Mathf.Abs(hit.position.y - point.y) > .3f) return false;
            foreach (var collider in Physics.OverlapCapsule(point + Vector3.up * .36f,
                point + Vector3.up * 1.48f, .32f, ~0, QueryTriggerInteraction.Ignore))
                if (collider.transform != transform && !collider.transform.IsChildOf(transform)) return false;
            return true;
        }

        void RestoreOutside()
        {
            // Chỉ bật capsule khi đã bước ra khỏi cửa và vùng đứng thực sự trống.
            if (!ClearExit(transform.position)) return;
            transform.SetParent(originalParent, true);
            agent.enabled = true;
            if (!agent.isOnNavMesh)
            {
                agent.enabled = false;
                return;
            }
            agent.nextPosition = transform.position;
            agent.ResetPath();
            agent.isStopped = false;
            if (motion && motion.Body) motion.Body.enabled = true;
            if (rode)
            {
                CompletedRides++;
                if (vehicle) vehicle.RecordAlighting();
            }
            vehicle = null;
            carrierHull = null;
            Phase = CityPassengerPhase.Walking;
            cooldown = Time.time + 35;
            if (view) view.SetMotion(0, false);
        }

        public void ReleaseCarrier()
        {
            if (Phase == CityPassengerPhase.Approaching)
            {
                CancelApproach();
                return;
            }
            if (!IsTravelling) return;
            rode |= Phase == CityPassengerPhase.Seated;
            if (vehicle)
            {
                exit = vehicle.DoorOutside;
                doorPosition = vehicle.transform.TransformPoint(vehicle.DoorInsideLocal);
            }
            if (carrierHull)
            {
                releaseDoorLocal = carrierHull.InverseTransformPoint(doorPosition);
                releaseExitLocal = carrierHull.InverseTransformPoint(exit);
            }
            detachPending = true;
            Phase = CityPassengerPhase.Disembarking;
            throughDoor = false;
            vehicle = null;
        }

        void OnDisable()
        {
            // Không đổi hierarchy trong OnDisable; tick sau khi bật lại mới tách khỏi xe.
            ReleaseCarrier();
        }
    }
}
