using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public sealed class CityPublicTransportVehicle : MonoBehaviour
    {
        readonly List<CityPublicTransportPassenger> passengers = new();
        readonly RaycastHit[] safetyHits = new RaycastHit[24];
        GameSession game;
        CityLife life;
        CityTraffic traffic;
        Transform door;
        Vector3 closedDoor;
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
        public Vector3 DoorInsideLocal { get; private set; }
        public Vector3 DoorOutside => transform.TransformPoint(DoorInsideLocal + Vector3.right * 2.1f)
            - Vector3.up * DoorInsideLocal.y;
        public static readonly Vector3[] Lane =
        {
            new(-22, 0, -43.45f), new(70, 0, -43.45f), new(100.45f, 0, -43.45f),
            new(100.45f, 0, 27.45f), new(70, 0, 27.45f), new(-22, 0, 27.45f),
            new(-103.45f, 0, 27.45f), new(-103.45f, 0, -43.45f)
        };

        public void Initialize(GameSession session, CityLife residents, int kind, int start)
        {
            game = session;
            life = residents;
            traffic = residents ? residents.GetComponent<CityTraffic>() : null;
            capacity = kind == 2 ? 1 : 2;
            DoorInsideLocal = kind == 2 ? new(.65f, .15f, -.35f) : new(.85f, .38f, 2.6f);
            door = CityPublicTransportModel.Build(kind, transform);
            closedDoor = door.localPosition;
            transform.position = Lane[start];
            next = (start + 1) % Lane.Length;
            transform.rotation = Quaternion.LookRotation((Lane[next] - transform.position).normalized);
            AtStop = true;
            dwell = 24;
        }

        void Update()
        {
            if (!game || !game.CanSimulate || Time.deltaTime <= 0) return;
            float delta = Time.deltaTime;
            doorAmount = Mathf.MoveTowards(doorAmount, AtStop ? 1 : 0, delta * 1.5f);
            door.localPosition = closedDoor + Vector3.back * doorAmount * .8f;
            passengers.RemoveAll(x => !x || !x.IsTravelling || !x.gameObject.activeInHierarchy);
            if (AtStop)
            {
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
                        Vector3 seat = new(-.55f, DoorInsideLocal.y, -1.2f - passengers.Count * 1.1f);
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
            Vector3 target = Lane[next];
            Vector3 direction = (target - transform.position).normalized;
            float step = 6 * delta;
            if (Blocked(direction, step)) return;
            transform.position = Vector3.MoveTowards(transform.position, target, step);
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction), delta * 140);
            if ((transform.position - target).sqrMagnitude > .01f) return;
            bool stop = next is 0 or 1 or 4 or 5;
            next = (next + 1) % Lane.Length;
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
