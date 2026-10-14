using UnityEngine;

namespace Tycoon
{
    [DisallowMultipleComponent]
    public sealed class WorldExpansionBoat : MonoBehaviour, ICityPassengerCarrier
    {
        public Transform Hull => transform;
        public bool DoorsOpen => true;
        public Vector3 DoorInsideLocal => new(0, 1.35f, -1.6f);
        public Vector3 DoorOutside => transform.TransformPoint(new Vector3(0, .2f, -5.2f));
        public int Boardings { get; private set; }
        public int Alightings { get; private set; }

        public static WorldExpansionBoat Build(Transform parent, Vector3 localPosition)
        {
            if (!parent)
                return null;
            WorldExpansionBoat existing = parent.GetComponentInChildren<WorldExpansionBoat>(true);
            if (existing)
                return existing;

            Transform root = new GameObject("ExpansionPassengerBoat").transform;
            root.SetParent(parent, false);
            root.localPosition = localPosition;
            WorldExpansionBoat boat = root.gameObject.AddComponent<WorldExpansionBoat>();
            CityStaticGeometry geometry = new CityStaticGeometry(root);
            CityBoatModels.Yacht(geometry, Vector3.zero);
            geometry.Finish();
            return boat;
        }

        public void RecordBoarding()
        {
            Boardings++;
        }

        public void RecordAlighting()
        {
            Alightings++;
        }
    }
}
