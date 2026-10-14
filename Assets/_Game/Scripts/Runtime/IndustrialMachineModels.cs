using UnityEngine;

namespace Tycoon
{
    public static class IndustrialMachineModels
    {
        public static GameObject Build(MachineStation station, Transform parent)
        {
            string recipe = station.Recipe.id;
            var root = TownModelParts.Root("Industrial_" + recipe, Vector3.zero, parent);
            var body = TownModelParts.Root("StaticEngineering", Vector3.zero, root);
            station.Rotor = null;
            switch (recipe)
            {
                case "mill":
                case "feedmill":
                    IndustrialMachineProcessing.Mill(body, root, station, recipe == "feedmill");
                    break;
                case "cheesemaker":
                case "saucemaker":
                case "soyextractor":
                case "milkbottler":
                    IndustrialMachineProcessing.Liquids(body, root, station, recipe);
                    break;
                case "spinner":
                case "loom":
                    IndustrialMachineTextiles.Build(body, root, station, recipe == "loom");
                    break;
                case "breadmixer":
                case "cakemixer":
                    IndustrialMachineBakery.Mixer(body, root, station, recipe == "cakemixer");
                    break;
                case "oven":
                case "cakeoven":
                    IndustrialMachineBakery.Oven(body, recipe == "cakeoven");
                    break;
                default:
                    IndustrialMachineKitchen.Build(body, recipe);
                    break;
            }
            // Chỉ gộp vỏ máy; trục quay và chi tiết CozyMachineMotion vẫn là node riêng.
            DiningMeshBatch.Combine(body);
            return root.gameObject;
        }
    }
}
