using UnityEngine;

namespace Tycoon
{
    public static class CoastalTownCrossing
    {
        public static void Build(Transform world)
        {
            var root = new GameObject("IndustrialForestCrosswalk").transform;
            root.SetParent(world, false);
            var geometry = new CityStaticGeometry(root);
            Vector3 walkDirection = new Vector3(39, 0, -22).normalized;
            Vector3 roadDirection = new Vector3(-20, 0, 30).normalized;
            Vector3 roadSide = Vector3.Cross(Vector3.up, roadDirection);
            float crossingLength = 7f / Mathf.Abs(Vector3.Dot(walkDirection, roadSide)) + 1.2f;
            int stripes = Mathf.FloorToInt(crossingLength / 1.2f);
            for (int index = 0; index < stripes; index++)
            {
                Vector3 position = CoastalTownLayout.ForestIndustryCrossing
                    + walkDirection * ((index - (stripes - 1) * .5f) * 1.2f);
                position.y = .062f;
                geometry.RotatedBox(position, new Vector3(3.2f, .008f, .45f),
                    Quaternion.LookRotation(walkDirection), "paving", "#ECE3C4");
            }
            geometry.Finish();
        }
    }
}
