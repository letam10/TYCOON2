using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tycoon
{
    public static class Art
    {
        public static Vector3 ModelSize(string key)
        {
            foreach(var entry in Catalog.models)if(entry.key==key)return entry.size;
            if(TownArt.Supports(key))return TownArt.Size(key);
            return Vector3.one*.4f;
        }
        public static GameCatalog Catalog;
        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var color); return color; }
        public static Material Material(string hex) => TownSurfaceMaterials.Paint(hex);
        public static GameObject Box(string name, Vector3 position, Vector3 size, string color, Transform parent = null, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            if (!collider) RemoveCollider(go);
            return go;
        }
        public static GameObject Cylinder(string name, Vector3 position, Vector3 size, string color, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            RemoveCollider(go);
            return go;
        }
        static void RemoveCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            collider.enabled = false;
            if (Application.isPlaying) Object.Destroy(collider);
            else Object.DestroyImmediate(collider);
        }
        public static GameObject Model(string key, Vector3 position, Transform parent = null, float scale = 1, float yaw = 0)
        {
            var prefab = Catalog.Model(key);
            if (prefab == null && !TownArt.Supports(key)) throw new System.InvalidOperationException("Thiếu model: " + key);
            var go = prefab ? Object.Instantiate(prefab,parent) : TownArt.Build(key);
            if(!prefab)go.transform.SetParent(parent,false);
            go.name = key; go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = Vector3.one * scale;
            TownPropSurfaces.Apply(go, key);
            return go;
        }
        public static TextMesh Label(string text, Vector3 position, Transform parent, float size = .18f, string color = "#173F40", bool billboard = true)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false); go.transform.localPosition = position;
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = Catalog.font;
            mesh.fontSize = 128;
            mesh.characterSize = size * 30f / 128f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Hex(color);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = WorldTypography.Material(Catalog.font);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (billboard) go.AddComponent<BillboardLabel>().RefreshNow();
            return mesh;
        }
    }

    public sealed class ItemPool
    {
        readonly Transform root;
        readonly Dictionary<string, Stack<GameObject>> available = new();
        public int Created { get; private set; }
        public ItemPool(Transform parent)
        {
            var go = new GameObject("ItemVisualPool"); go.transform.SetParent(parent);
            root = go.transform;
        }
        public GameObject Take(string id, Transform parent)
        {
            if (!available.TryGetValue(id, out var stack)) available[id] = stack = new();
            // Actor cũ có thể bị hủy sau OnDisable khi load; bỏ model đã chết trong pool.
            GameObject go = null;
            while (stack.Count > 0 && !go) go = stack.Pop();
            if (!go) go = Create(id);
            go.transform.SetParent(parent, false); go.SetActive(true); return go;
        }
        GameObject Create(string id)
        {
            Created++;
            var go = HandItemModels.Build(id, root);
            go.name = id;
            return go;
        }
        public void Return(GameObject item, bool detach = true)
        {
            if (!item) return;
            string id = item.name;
            item.SetActive(false);
            if (detach) item.transform.SetParent(root, false);
            if (!available.TryGetValue(id, out var stack)) available[id] = stack = new();
            stack.Push(item);
        }
    }

}
