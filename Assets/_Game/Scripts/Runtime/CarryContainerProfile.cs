using UnityEngine;

namespace Tycoon
{
    public readonly struct CarryContainerProfile
    {
        public const int Slots = 6;
        public const float Floor = .012f;
        public const float Wall = .009f;
        public readonly float Scale;
        public readonly Bounds ItemBounds;
        public readonly float CellX;
        public readonly float CellZ;
        public readonly float LayerHeight;
        public float Width => CellX * 2 + Wall;
        public float Depth => CellZ * 3 + Wall;

        public static bool Supports(string id) =>
            id is "egg" or "milk" or "bottled_milk" or "sauce" or "soy_sauce";

        public CarryContainerProfile(string id, Mesh itemMesh)
        {
            // Giữ chai/trứng đứng thẳng, tám tầng vẫn nằm trong chiều cao mang hàng 1,8 m.
            Scale = Mathf.Min(CarryItemLayout.For(id).Scale, .20f / itemMesh.bounds.size.y);
            ItemBounds = ItemStackBounds.Transform(itemMesh, Quaternion.identity, Scale);
            CellX = ItemBounds.size.x + .024f;
            CellZ = ItemBounds.size.z + .024f;
            LayerHeight = Floor + ItemBounds.size.y + .008f;
        }

        public Vector3 Position(int index)
        {
            int cell = index % Slots;
            int layer = index / Slots;
            return new Vector3((cell % 2 - .5f) * CellX - ItemBounds.center.x,
                layer * LayerHeight + Floor - ItemBounds.min.y,
                (cell / 2 - 1) * CellZ - ItemBounds.center.z);
        }
    }
}
