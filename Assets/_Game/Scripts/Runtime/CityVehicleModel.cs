using UnityEngine;

namespace Tycoon
{
    public static class CityVehicleModel
    {
        public static Transform Build(string color, Transform parent, bool van) =>
            Build(van ? CityVehicleKind.SmallTruck : CityVehicleKind.Sedan, color, parent);

        public static Transform Build(CityVehicleKind kind, string color, Transform parent)
        {
            if (kind == CityVehicleKind.Train) return CityRailVehicleModel.Build(parent, color);
            var root = TownModelParts.Root("CityVehicle_" + kind, Vector3.zero, parent);
            if (kind is CityVehicleKind.Bus or CityVehicleKind.Coach)
                CityPublicTransportModel.Build(kind == CityVehicleKind.Bus ? 0 : 1, root);
            else
                BuildRoad(kind, color, root);
            return root;
        }

        public static CityVehicleRig BuildRoad(CityVehicleKind kind, string color, Transform root)
        {
            var rig = root.gameObject.AddComponent<CityVehicleRig>();
            rig.Kind = kind;
            if (kind is CityVehicleKind.SmallTruck or CityVehicleKind.CargoTruck or CityVehicleKind.Tanker)
                CityVehicleGeometryTrucks.Build(root, rig, color);
            else
                CityVehicleGeometryCars.Build(root, rig, color);
            rig.SetDoorOpen(0);
            return rig;
        }
    }
}
