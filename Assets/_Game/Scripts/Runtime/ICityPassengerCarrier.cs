using UnityEngine;

namespace Tycoon
{
    public interface ICityPassengerCarrier
    {
        Transform Hull { get; }
        bool DoorsOpen { get; }
        Vector3 DoorInsideLocal { get; }
        Vector3 DoorOutside { get; }
        void RecordBoarding();
        void RecordAlighting();
    }
}
