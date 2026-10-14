using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public sealed class CityPublicTransportVehicle : MonoBehaviour, ICityPassengerCarrier
    {
        readonly List<CityPublicTransportPassenger> passengers = new();
        readonly RaycastHit[] safetyHits = new RaycastHit[24];
        GameSession game;
        CityLife life;
        CityTraffic traffic;
        CityPublicTransport transit;
        Transform door;
        Vector3 closedDoor;
        CityVehicleRig rig;
        float speed;
        Vector3[] route;
        int next;
        int capacity;
        float dwell;
        bool departing;
        float doorAmount;
        public bool AtStop { get; private set; }
        public bool DoorsOpen => AtStop && doorAmount > .95f;
        public int PassengerCount => passengers.Count;
        public int Boardings { get; private set; }
        public int Alightings { get; private set; }
        public Transform Hull => transform;
        public float Speed => speed;
        public Vector3 DoorInsideLocal { get; private set; }
        public Vector3 DoorOutside => transform.TransformPoint(DoorInsideLocal + Vector3.right * 2.1f)
            - Vector3.up * DoorInsideLocal.y;
        public static readonly Vector3[] Lane =
        {
            new(-22, 0, -46.55f), new(70, 0, -46.55f), new(103.55f, 0, -46.55f),
            new(103.55f, 0, 30.55f), new(70, 0, 30.55f), new(-22, 0, 30.55f),
            new(-106.55f, 0, 30.55f), new(-106.55f, 0, -46.55f)
        };

        public void Initialize(GameSession session, CityLife residents, int kind, int start)
        {
            game = session;
            life = residents;
            traffic = residents ? residents.GetComponent<CityTraffic>() : null;
            transit = GetComponentInParent<CityPublicTransport>();
            capacity = kind == 2 ? 1 : 2;
            DoorInsideLocal = kind == 2 ? new(.65f, .15f, -.35f) : new(.85f, .38f, 2.6f);
            door = CityPublicTransportModel.Build(kind, transform);
            rig = GetComponent<CityVehicleRig>();
            if (rig) DoorInsideLocal = rig.DoorInsideLocal;
            closedDoor = door.localPosition;
            transform.position = Lane[start];
            route = CityRoadRoute.Round(Lane);
            next = (System.Array.IndexOf(route, Lane[start]) + 1) % route.Length;
            transform.rotation = Quaternion.LookRotation((route[next] - transform.position).normalized);
            AtStop = true;
            dwell = 24;
        }

        void Update()
        {
            using var updateProbe = QaFrameProbe.Measure("CityPublicTransportVehicle.1.Update");

            if (!game || !game.CanSimulate || Time.deltaTime <= 0) return;
            float delta = Time.deltaTime;
            doorAmount = Mathf.MoveTowards(doorAmount, AtStop ? 1 : 0, delta * 1.5f);
            if (rig) rig.SetDoorOpen(Mathf.SmoothStep(0, 1, doorAmount));
            else door.localPosition = closedDoor + Vector3.back * doorAmount * .8f;
            passengers.RemoveAll(x => !x || !x.IsTravelling || !x.gameObject.activeInHierarchy);
            if (AtStop)
            {
                speed = 0;
                if (rig) rig.SetTravel(0, delta);
                dwell -= delta;
                if (DoorsOpen && !departing)
                {
                    foreach (var passenger in passengers)
                        if (passenger.Phase == CityPassengerPhase.Seated)
                        {
                            passenger.Disembark();
                        }
                    departing = true;
                }
                bool exiting = passengers.Exists(x => x.Phase == CityPassengerPhase.Disembarking);
                if (DoorsOpen && !exiting && dwell > 8 && passengers.Count < capacity && life)
                {
                    var actor = life.FindPassenger(DoorOutside, 12);
                    if (actor)
                    {
                        var passenger = actor.GetComponent<CityPublicTransportPassenger>();
                        if (!passenger) passenger = actor.gameObject.AddComponent<CityPublicTransportPassenger>();
                        int slot = passengers.Exists(x => Mathf.Abs(x.SeatLocal.z + 1.2f) < .1f) ? 1 : 0;
                        Vector3 seat = new(-.55f, DoorInsideLocal.y, -1.2f - slot * 1.1f);
                        if (capacity == 1) seat = new(.5f, .12f, -.7f);
                        if (passenger.Reserve(this, seat))
                        {
                            passengers.Add(passenger);
                        }
                    }
                }
                if (dwell > 0) return;
                foreach (var passenger in passengers) passenger.CancelApproach();
                if (passengers.Exists(x => x.Phase is CityPassengerPhase.Boarding or
                    CityPassengerPhase.Disembarking)) return;
                AtStop = false;
                return;
            }
            if (doorAmount > .01f) return;
            Vector3 target = route[next];
            Vector3 direction = (target - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, target);
            float desired = CityRailNetwork.SafeSpeed(distance, 6, 3.5f);
            if (Blocked(direction, speed * delta)) desired = 0;
            speed = Mathf.MoveTowards(speed, desired, delta * 3.5f);
            float step = speed * delta;
            if (Blocked(direction, step))
            {
                speed = 0;
                if (rig) rig.SetTravel(0, delta);
                return;
            }
            transform.position = Vector3.MoveTowards(transform.position, target, step);
            if (rig) rig.SetTravel(speed, delta);
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction), delta * 140);
            if ((transform.position - target).sqrMagnitude > .01f) return;
            bool stop = target == Lane[0] || target == Lane[1] || target == Lane[4] || target == Lane[5];
            next = (next + 1) % route.Length;
            if (!stop) return;
            AtStop = true;
            departing = false;
            dwell = 24;
        }

        public void RecordBoarding() => Boardings++;
        public void RecordAlighting() => Alightings++;

        bool Blocked(Vector3 direction, float step)
        {
            if (!traffic && life) traffic = life.GetComponent<CityTraffic>();
            if (traffic && traffic.BlocksPublicTransport(transform.position, direction, step)) return true;
            if (transit && transit.BlocksTraffic(transform.position, direction, step)) return true;
            Vector3 nose = transform.position + direction * (capacity == 1 ? 2 : 4.4f);
            int count = Physics.SphereCastNonAlloc(nose + Vector3.up * 1.25f, 1.05f,
                direction, safetyHits, step + .8f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (!safetyHits[i].collider.transform.IsChildOf(transform)) return true;
            return false;
        }

        void OnDisable()
        {
            foreach (var passenger in passengers)
                if (passenger) passenger.ReleaseCarrier();
            passengers.Clear();
        }
    }
}
