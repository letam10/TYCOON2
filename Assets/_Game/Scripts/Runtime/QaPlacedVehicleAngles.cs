using System.Collections;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator PlacedVehicleAngles(CityRailService rail, CityPublicTransport transit,
            CityRoadVehiclePassengers[] road)
        {
            float speed = Time.timeScale;
            Time.timeScale = 0;
            foreach (var train in rail.Trains)
                yield return Views(train.transform, "Train" + train.Track);
            foreach (var vehicle in transit.Vehicles)
                yield return Views(vehicle.transform, vehicle.name);
            foreach (var vehicle in road)
                yield return Views(vehicle.transform, vehicle.GetComponent<CityVehicleRig>().Kind.ToString());
            Time.timeScale = speed;

            IEnumerator Views(Transform root, string label)
            {
                Bounds bounds = ModelBounds(root);
                yield return CaptureModelAngles("vehicle-placed-" + label, bounds.center,
                    Mathf.Max(8, bounds.size.magnitude * 1.8f), false);
            }
        }
    }
}
