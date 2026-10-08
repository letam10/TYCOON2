using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tycoon
{
    public static partial class HandItemModels
    {
        public const float VisibleSize = .5f;
        static readonly Dictionary<string, Mesh> meshes = new();
        public static bool Supports(string id) => id == "cash" || id == "crate" || Definitions.Item(id) != null;

        public static GameObject Build(string id, Transform parent)
        {
            if (!Supports(id)) throw new System.ArgumentException("Không có mẫu vật phẩm: " + id);
            var root = new GameObject(id);
            root.transform.SetParent(parent, false);
            if (!meshes.TryGetValue(id, out var mesh) || !mesh)
            {
                var shapes = new GameObject("Shapes").transform;
                BuildShapes(id, shapes);
                mesh = Bake(id, shapes);
                meshes[id] = mesh;
                HandItemSurfaceAtlas.Apply();
                Dispose(shapes.gameObject);
            }
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = HandItemSurfaceAtlas.Material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            return root;
        }

        static Transform Part(Transform root, string name, PrimitiveType kind, Vector3 at, Vector3 size,
            string color, string surface = "food", Vector3 angle = default)
        {
            var part = new GameObject(name);
            part.transform.SetParent(root, false);
            part.transform.localPosition = at;
            part.transform.localScale = size;
            part.transform.localRotation = Quaternion.Euler(angle);
            part.AddComponent<MeshFilter>().sharedMesh = HandItemMeshPrimitives.Get(kind, name, color, surface);
            part.AddComponent<MeshRenderer>().sharedMaterial = HandItemSurfaceAtlas.Material;
            return part.transform;
        }

        static void Box(Transform t, string name, Vector3 p, Vector3 s, string color,
            string surface = "paper", Vector3 angle = default) =>
            Part(t, name, PrimitiveType.Cube, p, s, color, surface, angle);
        static void Ball(Transform t, string name, Vector3 p, Vector3 s, string color) =>
            Part(t, name, PrimitiveType.Sphere, p, s, color);
        static void Disc(Transform t, string name, Vector3 p, Vector3 s, string color, string surface = "food") =>
            Part(t, name, PrimitiveType.Cylinder, p, s, color, surface);

        static Mesh Bake(string id, Transform root)
        {
            var parts = new List<CombineInstance>();
            var bounds = new Bounds();
            bool first = true;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (first) bounds = renderer.bounds;
                else bounds.Encapsulate(renderer.bounds);
                first = false;
                parts.Add(new CombineInstance
                {
                    mesh = filter.sharedMesh,
                    transform = root.worldToLocalMatrix * filter.transform.localToWorldMatrix
                });
            }
            float scale = CarryItemLayout.ModelSize(id) / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            Vector3 center = new(bounds.center.x, bounds.min.y, bounds.center.z);
            var normalize = Matrix4x4.Scale(Vector3.one * scale) * Matrix4x4.Translate(-center);
            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                part.transform = normalize * part.transform;
                parts[i] = part;
            }
            var result = new Mesh { name = "HandMesh_" + id };
            result.CombineMeshes(parts.ToArray(), true, true);
            result.RecalculateBounds();
            return result;
        }

        static void Dispose(Object obj)
        {
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }

        static void BuildShapes(string id, Transform t)
        {
            switch (id)
            {
                case "carrot": Carrot(t);
                    break;
                case "tomato": Tomato(t);
                    break;
                case "wheat": Wheat(t);
                    break;
                case "corn": Corn(t);
                    break;
                case "soybean": Soybean(t);
                    break;
                case "milk": Milk(t);
                    break;
                case "egg": Egg(t);
                    break;
                case "flour": Sack(t, false);
                    break;
                case "animal_feed": Sack(t, true);
                    break;
                case "cheese": Cheese(t);
                    break;
                case "sauce": Bottle(t, "#A93D2F", "#638348", false);
                    break;
                case "soy_sauce": Bottle(t, "#44291E", "#CB9B4D", true);
                    break;
                case "bottled_milk": Bottle(t, "#F1EAD7", "#4C7D9B", false);
                    break;
                case "bread": Bread(t);
                    break;
                case "cake": Cake(t);
                    break;
                case "beef": Beef(t);
                    break;
                case "wool": Wool(t);
                    break;
                case "yarn": Yarn(t);
                    break;
                case "cloth": Cloth(t);
                    break;
                case "bread_dough": Dough(t);
                    break;
                case "cake_batter": Batter(t);
                    break;
                case "cash": Cash(t);
                    break;
                case "crate": Crate(t);
                    break;
                default: Dish(t, id);
                    break;
            }
        }
    }
}
