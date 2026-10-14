using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public sealed class CityRailTrain : MonoBehaviour, ICityPassengerCarrier
    {
        readonly List<CityPublicTransportPassenger> passengers = new();
        readonly RaycastHit[] safetyHits = new RaycastHit[16];
        GameSession game;
        CityLife life;
        CityVehicleRig rig;
        float target;
        float speed;
        float dwell;
        float doorAmount;
        bool returning;
        bool unloading;
        public int Track { get; private set; }
        public bool AtStation { get; private set; }
        public bool DoorsOpen => AtStation && doorAmount > .95f;
        public Transform Hull => transform;
        public int Boardings { get; private set; }
        public int Alightings { get; private set; }
        public int PassengerCount => passengers.Count;
        public float Travelled { get; private set; }
        public float Speed => speed;
        public Vector3 DoorInsideLocal => new(-rig.DoorInsideLocal.x, rig.DoorInsideLocal.y,
            rig.DoorInsideLocal.z);
        public Vector3 DoorOutside
        {
            get
            {
                Vector3 point = transform.TransformPoint(DoorInsideLocal + Vector3.left * 2.2f);
                point.y = CityRailNetwork.PlatformHeight;
                return point;
            }
        }

        public void Initialize(GameSession session, CityLife residents, int track)
        {
            game = session;
            life = residents;
            Track = track;
            rig = GetComponent<CityVehicleRig>();
            float start = track == 0 ? CityRailNetwork.WestEnd : CityRailNetwork.EastEnd;
            transform.SetPositionAndRotation(new(start, .185f, CityRailNetwork.Tracks[track]),
                Quaternion.LookRotation(track == 0 ? Vector3.right : Vector3.left));
            target = CityRailNetwork.StationX;
        }

        void Update()
        {
            using var updateProbe = QaFrameProbe.Measure("CityRailTrain.1.Update");

            if (!game || !game.CanSimulate || Time.deltaTime <= 0 || !rig) return;
            float delta = Time.deltaTime;
            passengers.RemoveAll(x => !x || !x.IsTravelling || !x.gameObject.activeInHierarchy);
            doorAmount = Mathf.MoveTowards(doorAmount, AtStation ? 1 : 0, delta * 1.6f);
            rig.SetDoorOpen(Mathf.SmoothStep(0, 1, doorAmount), -1);
            if (AtStation)
            {
                StationTick(delta);
                rig.SetTravel(0, delta);
                return;
            }
            if (doorAmount > .001f)
            {
                rig.SetTravel(0, delta);
                return;
            }
            if (dwell > 0)
            {
                dwell -= delta;
                return;
            }
            Vector3 from = transform.position;
            Vector3 destination = new(target, from.y, from.z);
            float distance = Mathf.Abs(target - from.x);
            Vector3 direction = (destination - from).normalized;
            float desired = Blocked(direction, speed * delta) ? 0 :
                CityRailNetwork.SafeSpeed(distance, 9, 2.8f);
            speed = Mathf.MoveTowards(speed, desired, 2.8f * delta);
            if (desired == 0 && Blocked(direction, 0)) speed = 0;
            transform.position = Vector3.MoveTowards(from, destination, speed * delta);
            float travelled = Vector3.Distance(from, transform.position);
            Travelled += travelled;
            float signedSpeed = Vector3.Dot(transform.position - from, transform.forward) / Mathf.Max(delta, .001f);
            rig.SetTravel(signedSpeed, delta);
            if (Mathf.Abs(transform.position.x - target) > .035f) return;
            speed = 0;
            if (Mathf.Abs(target - CityRailNetwork.StationX) < .1f)
            {
                AtStation = true;
                unloading = false;
                dwell = 26;
            }
            else
            {
                target = CityRailNetwork.StationX;
                returning = !returning;
                dwell = 2;
            }
        }

        void StationTick(float delta)
        {
            dwell -= delta;
            if (!DoorsOpen) return;
            if (!unloading)
            {
                foreach (var passenger in passengers)
                    if (passenger.Phase == CityPassengerPhase.Seated) passenger.Disembark();
                unloading = true;
            }
            bool exiting = passengers.Exists(x => x.Phase == CityPassengerPhase.Disembarking);
            if (!exiting && dwell > 8 && passengers.Count < 2 && life)
            {
                var actor = life.FindPassenger(DoorOutside, 6);
                if (actor)
                {
                    var passenger = actor.GetComponent<CityPublicTransportPassenger>();
                    if (!passenger) passenger = actor.gameObject.AddComponent<CityPublicTransportPassenger>();
                    Vector3 seat = new(-.55f, .6f, -11.5f - passengers.Count * 1.2f);
                    if (passenger.Reserve(this, seat)) passengers.Add(passenger);
                }
            }
            if (dwell > 0) return;
            foreach (var passenger in passengers) passenger.CancelApproach();
            if (passengers.Exists(x => x.Phase is CityPassengerPhase.Boarding or
                CityPassengerPhase.Disembarking)) return;
            AtStation = false;
            bool east = Track == 0 ? !returning : returning;
            target = east ? CityRailNetwork.EastEnd : CityRailNetwork.WestEnd;
        }

        bool Blocked(Vector3 direction, float step)
        {
            if (direction.sqrMagnitude < .1f) return false;
            float lead = Vector3.Dot(direction, transform.forward) >= 0 ? 5 : 24.7f;
            Vector3 probe = transform.position + direction * lead;
            int count = Physics.SphereCastNonAlloc(probe + Vector3.up * 1.1f, 1.1f,
                direction, safetyHits, Mathf.Max(.8f, step + .7f), ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = safetyHits[i].collider;
                if (!hit.transform.IsChildOf(transform)) return true;
            }
            return false;
        }

        public void RecordBoarding() => Boardings++;
        public void RecordAlighting() => Alightings++;

        void OnDisable()
        {
            foreach (var passenger in passengers)
                if (passenger) passenger.ReleaseCarrier();
            passengers.Clear();
        }
    }
}
