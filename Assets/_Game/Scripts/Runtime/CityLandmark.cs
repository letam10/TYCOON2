using UnityEngine;

namespace Tycoon
{
    // Gốc riêng để kiểm model nhiều hướng mà không nhân bản logic thành phố.
    public sealed class CityLandmark : MonoBehaviour
    {
        public string Id;
        public Vector3 Footprint;
        public Vector3 Approach => transform.TransformPoint(Id switch
        {
            "PoliceStation" => new Vector3(0, 0, -15),
            "FireStation" => new Vector3(0, 0, -17),
            "SeasideBar" => new Vector3(0, 0, -14),
            "EntertainmentHall" => new Vector3(0, 0, -17),
            "Playground" => new Vector3(0, 0, -18),
            "FoundersMonument" => new Vector3(0, 0, -15),
            "Harbor" => new Vector3(-25, 0, 0),
            "Beach" => new Vector3(-27, 0, 0),
            "CoffeeHouse" => new Vector3(0, 0, -15),
            "Bank" => new Vector3(0, 0, -19),
            "Hotel" => new Vector3(0, 0, -18),
            "Theater" => new Vector3(0, 0, -17),
            "Marina" => new Vector3(-25, 0, 0),
            _ => Vector3.zero
        });
    }
}
