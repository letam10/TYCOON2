using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public enum CityPassengerPhase { Walking, Approaching, Boarding, Seated, Disembarking }

    public sealed class CityPublicTransportPassenger : MonoBehaviour
    {
        const float TransitMoveTimeout = 8f;
        NavMeshAgent agent;
        ActorPhysicalMotion motion;
        ActorView view;
        Transform originalParent;
        ICityPassengerCarrier vehicle;
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
        float transitSeconds;
        readonly RaycastHit[] movementHits = new RaycastHit[16];
        readonly Collider[] exitHits = new Collider[24];
        float walkingSpeed;
        bool HasCarrier => vehicle != null && vehicle.Hull;
        public CityPassengerPhase Phase { get; private set; }
        public bool IsTravelling => Phase != CityPassengerPhase.Walking;
        public bool CanBoard => !IsTravelling && Time.time >= cooldown;
        public int CompletedRides { get; private set; }
        public Vector3 SeatLocal => seat;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            motion = GetComponent<ActorPhysicalMotion>();
            view = GetComponentInChildren<ActorView>();
            originalParent = transform.parent;
        }

        public bool Reserve(ICityPassengerCarrier carrier, Vector3 seatPosition)
        {
            if (!agent) Awake();
            if (carrier == null || !carrier.Hull || !CanBoard || !agent || !agent.enabled || !agent.isOnNavMesh)
                return false;
            vehicle = carrier;
            carrierHull = carrier.Hull;
            rode = false;
            seat = seatPosition;
            transitSeconds = 0;
            Phase = CityPassengerPhase.Approaching;
            if (Navigation.Go(agent, carrier.DoorOutside)) return true;
            CancelApproach();
            return false;
        }

        public void CancelApproach()
        {
            if (Phase != CityPassengerPhase.Approaching) return;
            if (agent && agent.enabled && agent.isOnNavMesh) agent.ResetPath();
            if (agent) agent.GetComponent<CityPedestrianRoute>()?.Clear();
            Phase = CityPassengerPhase.Walking;
            vehicle = null;
            transitSeconds = 0;
            cooldown = Time.time + 8;
        }

        void Update()
        {
            using var updateProbe = QaFrameProbe.Measure("CityPublicTransportPassenger.1.Update");

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
            if (Phase is CityPassengerPhase.Boarding or CityPassengerPhase.Disembarking)
            {
                transitSeconds += delta;
                if (transitSeconds > TransitMoveTimeout)
                {
                    EmergencyRestore();
                    return;
                }
            }
            if (Phase == CityPassengerPhase.Approaching)
            {
                if (!HasCarrier || !vehicle.DoorsOpen)
                {
                    CancelApproach();
                    return;
                }
                if (view) view.SetMotion(Navigation.ActualSpeed(agent), false);
                if (!Navigation.Arrived(agent)) return;
                exit = GroundExit(vehicle.DoorOutside);
                Navigation.Suspend(agent);
                transform.SetParent(vehicle.Hull, true);
                Phase = CityPassengerPhase.Boarding;
                throughDoor = false;
                walkingSpeed = 0;
            }
            if (Phase == CityPassengerPhase.Boarding)
            {
                Vector3 target = HasCarrier ? vehicle.Hull.TransformPoint(
                    throughDoor ? seat : vehicle.DoorInsideLocal) : exit;
                if (!Move(target, delta)) return;
                if (!throughDoor)
                {
                    throughDoor = true;
                    return;
                }
                Phase = CityPassengerPhase.Seated;
                rode = true;
                transitSeconds = 0;
                if (HasCarrier) vehicle.RecordBoarding();
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
                if (!ClearExit(exit) || !Move(exit, delta)) return;
                RestoreOutside();
            }
        }

        bool Move(Vector3 point, float delta)
        {
            float distance = Vector3.Distance(transform.position, point);
            walkingSpeed = Mathf.MoveTowards(walkingSpeed, Mathf.Min(1.8f, distance * 5), delta * 5);
            Vector3 next = Vector3.MoveTowards(transform.position, point, delta * walkingSpeed);
            Vector3 direction = next - transform.position;
            if (direction.sqrMagnitude > .000001f)
            {
                int count = Physics.CapsuleCastNonAlloc(transform.position + Vector3.up * .36f,
                    transform.position + Vector3.up * 1.48f, .28f, direction.normalized,
                    movementHits, direction.magnitude, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    var hit = movementHits[i].collider.transform;
                    if (hit == transform || hit.IsChildOf(transform)
                        || carrierHull && (hit == carrierHull || hit.IsChildOf(carrierHull)))
                        continue;
                    walkingSpeed = 0;
                    if (view) view.SetMotion(0, false);
                    return false;
                }
            }
            transform.position = next;
            Vector3 facing = direction;
            facing.y = 0;
            if (facing.sqrMagnitude > .000001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(facing), delta * 360);
            if (view) view.SetMotion(direction.magnitude / Mathf.Max(.001f, delta), false);
            return (transform.position - point).sqrMagnitude < .0025f;
        }

        public void Disembark()
        {
            if (Phase != CityPassengerPhase.Seated || !HasCarrier || !vehicle.DoorsOpen) return;
            exit = GroundExit(vehicle.DoorOutside);
            doorPosition = vehicle.Hull.TransformPoint(vehicle.DoorInsideLocal);
            throughDoor = false;
            walkingSpeed = 0;
            transitSeconds = 0;
            Phase = CityPassengerPhase.Disembarking;
        }

        public bool ClearExit(Vector3 point)
        {
            if (!NavMesh.SamplePosition(point, out var hit, .45f, NavMesh.AllAreas)) return false;
            if (Mathf.Abs(hit.position.y - point.y) > .3f) return false;
            int count = Physics.OverlapCapsuleNonAlloc(point + Vector3.up * .36f,
                point + Vector3.up * 1.48f, .32f, exitHits, ~0, QueryTriggerInteraction.Ignore);
            if (count == exitHits.Length) return false;
            for (int i = 0; i < count; i++)
                if (exitHits[i].transform != transform
                    && !exitHits[i].transform.IsChildOf(transform))
                    return false;
            return true;
        }

        static Vector3 GroundExit(Vector3 point)
        {
            // Cửa giữ vị trí ngang; chân khách phải theo mặt vỉa hè thay vì cao độ đường xe.
            if (NavMesh.SamplePosition(point, out var hit, .45f, NavMesh.AllAreas)
                && Mathf.Abs(hit.position.y - point.y) <= .3f)
                point.y = hit.position.y;
            foreach (var surface in Physics.RaycastAll(point + Vector3.up * .35f, Vector3.down,
                .7f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (surface.normal.y < .8f || Mathf.Abs(surface.point.y - point.y) > .3f
                    || surface.collider.GetComponentInParent<ActorPhysicalMotion>()
                    || surface.collider.GetComponentInParent<CityVehicleRig>())
                    continue;
                point.y = Mathf.Max(point.y, surface.point.y + .015f);
            }
            return point;
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
                if (HasCarrier) vehicle.RecordAlighting();
            }
            vehicle = null;
            carrierHull = null;
            Phase = CityPassengerPhase.Walking;
            transitSeconds = 0;
            cooldown = Time.time + 35;
            if (view) view.SetMotion(0, false);
        }

        void EmergencyRestore()
        {
            ICityPassengerCarrier completedCarrier = vehicle;
            Vector3 target = completedCarrier != null && completedCarrier.Hull
                ? completedCarrier.DoorOutside
                : exit;
            if (target == Vector3.zero && carrierHull)
                target = carrierHull.TransformPoint(releaseExitLocal);
            transform.SetParent(originalParent, true);
            if (NavMesh.SamplePosition(target, out var hit, 2.5f, NavMesh.AllAreas))
                target = hit.position + Vector3.up * .05f;
            transform.position = target;
            if (agent)
            {
                agent.enabled = true;
                if (!agent.isOnNavMesh)
                    agent.enabled = false;
                else
                {
                    agent.nextPosition = transform.position;
                    agent.ResetPath();
                    agent.isStopped = false;
                }
            }
            if (motion && motion.Body) motion.Body.enabled = true;
            if (rode)
            {
                CompletedRides++;
                completedCarrier?.RecordAlighting();
            }
            vehicle = null;
            carrierHull = null;
            detachPending = false;
            throughDoor = false;
            walkingSpeed = 0;
            rode = false;
            Phase = CityPassengerPhase.Walking;
            transitSeconds = 0;
            cooldown = Time.time + 8;
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
            if (HasCarrier)
            {
                exit = GroundExit(vehicle.DoorOutside);
                doorPosition = vehicle.Hull.TransformPoint(vehicle.DoorInsideLocal);
            }
            if (carrierHull)
            {
                releaseDoorLocal = carrierHull.InverseTransformPoint(doorPosition);
                releaseExitLocal = carrierHull.InverseTransformPoint(exit);
            }
            detachPending = true;
            Phase = CityPassengerPhase.Disembarking;
            throughDoor = false;
            transitSeconds = 0;
            vehicle = null;
        }

        void OnDisable()
        {
            // Không đổi hierarchy trong OnDisable; tick sau khi bật lại mới tách khỏi xe.
            ReleaseCarrier();
        }
    }
}
