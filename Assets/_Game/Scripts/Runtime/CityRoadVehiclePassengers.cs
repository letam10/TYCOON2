using UnityEngine;

namespace Tycoon
{
    public sealed class CityRoadVehiclePassengers : MonoBehaviour, ICityPassengerCarrier
    {
        GameSession game;
        CityLife life;
        CityVehicleRig rig;
        CityPublicTransportPassenger passenger;
        float dwell;
        float doorAmount;
        bool unloading;
        public Transform Hull => transform;
        public bool AtStop { get; private set; }
        public bool DoorsOpen => AtStop && doorAmount > .95f;
        public Vector3 DoorInsideLocal => rig.DoorInsideLocal;
        public Vector3 DoorOutside
        {
            get
            {
                Vector3 point = transform.TransformPoint(DoorInsideLocal + Vector3.right * 2.1f);
                point.y = 0;
                return point;
            }
        }
        public int Boardings { get; private set; }
        public int Alightings { get; private set; }
        public int PassengerCount => passenger && passenger.IsTravelling ? 1 : 0;

        public void Initialize(GameSession session, CityLife residents, CityVehicleRig presentation)
        {
            game = session;
            life = residents;
            rig = presentation;
        }

        public void BeginStop()
        {
            if (AtStop) return;
            AtStop = true;
            unloading = false;
            dwell = 18;
        }

        public bool Tick(float delta)
        {
            if (!game || !game.CanSimulate || delta <= 0 || !rig) return true;
            if (passenger && (!passenger.IsTravelling || !passenger.gameObject.activeInHierarchy)) passenger = null;
            doorAmount = Mathf.MoveTowards(doorAmount, AtStop ? 1 : 0, delta * 1.7f);
            rig.SetDoorOpen(Mathf.SmoothStep(0, 1, doorAmount));
            if (!AtStop) return doorAmount > .001f;
            dwell -= delta;
            if (!DoorsOpen) return true;
            if (!unloading)
            {
                if (passenger && passenger.Phase == CityPassengerPhase.Seated) passenger.Disembark();
                unloading = true;
            }
            if (!passenger && dwell > 8 && life)
            {
                var actor = life.FindPassenger(DoorOutside, BoardingSearchRadius(dwell));
                if (actor)
                {
                    var rider = actor.GetComponent<CityPublicTransportPassenger>();
                    if (!rider) rider = actor.gameObject.AddComponent<CityPublicTransportPassenger>();
                    Vector3 seat = new(-.4f, DoorInsideLocal.y, DoorInsideLocal.z - .2f);
                    if (rider.Reserve(this, seat)) passenger = rider;
                }
            }
            if (dwell > 0) return true;
            if (passenger)
            {
                passenger.CancelApproach();
                if (passenger.Phase is CityPassengerPhase.Boarding or CityPassengerPhase.Disembarking) return true;
            }
            AtStop = false;
            return true;
        }

        public void RecordBoarding() => Boardings++;
        public void RecordAlighting() => Alightings++;

        static float BoardingSearchRadius(float remainingDwell)
        {
            // Tốc độ cư dân thấp nhất là 1.7 m/s; dành 4 giây bước qua cửa và ngồi, không kéo dài lượt dừng.
            return Mathf.Min(CityPedestrianNetwork.LocalTrip, Mathf.Max(0, remainingDwell - 4) * 1.7f);
        }

        void OnDisable()
        {
            if (passenger) passenger.ReleaseCarrier();
        }
    }
}
