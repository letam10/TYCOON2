using UnityEngine;

namespace Tycoon
{
    public readonly struct CarryItemLayout
    {
        public readonly int Rows;
        public readonly int Columns;
        public readonly float Scale;
        public readonly Quaternion Rotation;
        public readonly Vector3 Spacing;
        CarryItemLayout(float scale, Vector3 rotation)
        {
            Rows = 3;
            Columns = 2;
            Scale = scale;
            Rotation = Quaternion.Euler(rotation);
            Spacing = new Vector3(.018f, .012f, .018f);
        }

        public static CarryItemLayout For(string id) => id switch
        {
            "beef" => new(.77f, Vector3.zero),
            "carrot" => new(.756f, new Vector3(0, 0, 90)),
            "wheat" => new(.84f, new Vector3(90, 0, 90)),
            "corn" => new(.66f, new Vector3(0, 0, 90)),
            "sauce" or "soy_sauce" or "bottled_milk" => new(.65f, new Vector3(0, 0, 90)),
            "milk" => new(.48f, new Vector3(0, 0, 90)),
            "flour" or "animal_feed" => new(.62f, new Vector3(90, 0, 90)),
            "yarn" => new(.58f, new Vector3(0, 0, 90)),
            "egg" => new(.50f, new Vector3(0, 0, 90)),
            "bread" or "cheese" or "cloth" or "bread_dough" => new(.65f, Vector3.zero),
            "meal" or "soy_vegetables" or "pasta" or "beef_soy" or "egg_sandwich" or "corn_soup" =>
                new(.62f, Vector3.zero),
            _ => new(.42f, Vector3.zero)
        };

        public static float ModelSize(string id) => id switch
        {
            "beef" => .70f,
            "carrot" => .54f,
            "wheat" => .56f,
            "corn" => .52f,
            "egg" => .40f,
            "tomato" => .46f,
            _ => HandItemModels.VisibleSize
        };

        public Bounds Bounds(Mesh mesh)
        {
            return ItemStackBounds.Transform(mesh, Rotation, Scale);
        }

        public Vector3 Position(int index, int count, Bounds bounds)
        {
            int columns = Mathf.Min(Columns, count);
            int rows = Mathf.Min(Rows, Mathf.CeilToInt((float)count / Columns));
            int row = index / Columns % Rows;
            int layer = index / (Columns * Rows);
            float stepX = bounds.size.x + Spacing.x;
            float stepZ = bounds.size.z + Spacing.z;
            // Tầng trên đặt đúng cột dưới; mọi món đều giữ nguyên hướng.
            return new Vector3((index % Columns - (columns - 1) * .5f) * stepX,
                layer * (bounds.size.y + Spacing.y) - bounds.min.y,
                (row - (rows - 1) * .5f) * stepZ) - new Vector3(bounds.center.x, 0, bounds.center.z);
        }
    }

    public static class ItemStackBounds
    {
        public static Bounds Transform(Mesh mesh, Quaternion rotation, float scale)
        {
            var source = mesh.bounds;
            var matrix = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one * scale);
            Vector3 extents = source.extents;
            Vector3 x = matrix.MultiplyVector(new Vector3(extents.x, 0, 0));
            Vector3 y = matrix.MultiplyVector(new Vector3(0, extents.y, 0));
            Vector3 z = matrix.MultiplyVector(new Vector3(0, 0, extents.z));
            Vector3 size = new(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
                Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
            return new Bounds(matrix.MultiplyPoint3x4(source.center), size * 2);
        }

    }
}
