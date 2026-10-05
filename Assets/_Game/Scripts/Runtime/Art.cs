using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tycoon
{
    public static class Art
    {
        public static Vector3 ModelSize(string key)
        {
            foreach(var entry in Catalog.models)if(entry.key==key)return entry.size;
            return Vector3.one*.4f;
        }
        public static GameCatalog Catalog;
        static readonly Dictionary<string, Material> materials = new();
        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var color); return color; }
        public static Material Material(string hex)
        {
            if (materials.TryGetValue(hex, out var existing)) return existing;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Paint_" + hex };
            material.SetColor("_BaseColor", Hex(hex).linear);
            material.SetFloat("_Smoothness", .22f);
            material.enableInstancing = true;
            materials[hex] = material;
            return material;
        }
        public static GameObject Box(string name, Vector3 position, Vector3 size, string color, Transform parent = null, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            if (!collider) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }
        public static GameObject Cylinder(string name, Vector3 position, Vector3 size, string color, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            Object.Destroy(go.GetComponent<Collider>());
            return go;
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
            return go;
        }
        public static TextMesh Label(string text, Vector3 position, Transform parent, float size = .18f, string color = "#173F40", bool billboard = true)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false); go.transform.localPosition = position;
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text; mesh.font = Catalog.font; mesh.fontSize = 64;
            mesh.characterSize = size * 15f / 64f; mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center; mesh.color = Hex(color).linear;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Catalog.font.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            if (billboard) go.AddComponent<BillboardLabel>();
            return mesh;
        }
    }

    public sealed class BillboardLabel : MonoBehaviour
    {
        void LateUpdate() { if (Camera.main) transform.rotation = Camera.main.transform.rotation; }
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
            var go = stack.Count > 0 ? stack.Pop() : Create(id);
            go.transform.SetParent(parent, false); go.SetActive(true); return go;
        }
        GameObject Create(string id)
        {
            Created++;
            var go = Art.Model(Definitions.Item(id).model, Vector3.zero, root);
            go.name = id;
            return go;
        }
        public void Return(GameObject item, bool detach = true)
        {
            string id = item.name;
            item.SetActive(false);
            if (detach) item.transform.SetParent(root, false);
            if (!available.TryGetValue(id, out var stack)) available[id] = stack = new();
            stack.Push(item);
        }
    }

    public sealed class InventoryStack : MonoBehaviour
    {
        [System.NonSerialized] public Inventory Inventory;
        [System.NonSerialized] public ItemPool Pool;
        public int Columns = 3;
        public int Rows = 2;
        public int Maximum = 18;
        public float Spacing = .21f;
        public float Scale = .65f;
        public float LayerHeight = .24f;
        public int VisibleCount => items.Count;
        public string VisibleItemId(int index) => items[index].name;
        readonly List<GameObject> items = new();
        int revision = -1;
        Inventory displayedInventory;
        void LateUpdate() => Refresh();
        public void Refresh()
        {
            if (Inventory == null || Pool == null || ReferenceEquals(displayedInventory, Inventory) && revision == Inventory.Revision) return;
            displayedInventory = Inventory;
            revision = Inventory.Revision;
            foreach (var go in items) Pool.Return(go);
            items.Clear();
            float spacing=Spacing,height=0,layerHeight=0;
            foreach(var amount in Inventory.Snapshot())
            {var size=Art.ModelSize(Definitions.Item(amount.id).model);spacing=Mathf.Max(spacing,Mathf.Sqrt(size.x*size.x+size.z*size.z)*Scale+.025f);}
            foreach (var value in Inventory.Snapshot())
            {
                // Giỏ một loại phải hiển thị đủ hàng thật, kể cả sau nâng cấp hoặc load save.
                for (int count = 0; count < value.count && items.Count < (Inventory.SingleItem ? Inventory.Total : Maximum); count++)
                {
                    var go = Pool.Take(value.id, transform);
                    int index = items.Count;
                    int col = index % Columns, row = index / Columns % Rows, layer = index / (Columns * Rows);
                    if(index>0 && index%(Columns*Rows)==0){height+=layerHeight+.025f;layerHeight=0;}
                    layerHeight=Mathf.Max(layerHeight,Art.ModelSize(Definitions.Item(go.name).model).y*Scale);
                    go.transform.localPosition = new Vector3((col - (Columns - 1) * .5f) * spacing,height,(row - (Rows - 1) * .5f) * spacing);
                    go.transform.localRotation = Quaternion.Euler(0, index * 37 % 90, 0);
                    go.transform.localScale = Vector3.one * Scale;
                    items.Add(go);
                }
            }
        }
        void OnDisable()
        {
            // Parent đang bị tắt: giữ parent đến lần Take tiếp theo, tránh lỗi native hierarchy.
            if (Pool != null) foreach (var go in items) Pool.Return(go, false);
            items.Clear(); revision = -1;
        }
    }

    public sealed class ActorView : MonoBehaviour
    {
        public Animator Animator;
        public string State { get; private set; }
        float interactionEnd;
        public void Initialize()
        {
            Animator = GetComponentInChildren<Animator>();
            if (Animator != null) { Animator.applyRootMotion = false; Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; }
        }
        public void SetMotion(float speed, bool carry)
        {
            if (Time.time < interactionEnd) return;
            bool moving=speed>(State is "Walk" or "Run" or "CarryWalk"?.12f:.3f);
            Play(carry ? (moving ? "CarryWalk" : "Carry") : speed > 5.5f ? "Run" : moving ? "Walk" : "Idle");
            if (Animator)
            {float rate=moving?Mathf.Clamp(speed/(State=="Run"?6f:3.3f),.75f,1.6f):1;Animator.speed=Mathf.Lerp(Animator.speed,rate,1-Mathf.Exp(-8*Time.deltaTime));}
        }
        public void Interact(bool pickup)
        {
            interactionEnd = Time.time + .42f;
            if(Animator)Animator.speed=1;
            Play(pickup ? "Pickup" : "Drop");
        }
        public void Work(string state)
        {
            interactionEnd=Time.time+.22f;
            if(Animator)Animator.speed=1;
            Play(state);
        }
        public static string WorkState(Station target,InteractionKind kind)=>kind switch
        {
            InteractionKind.Serve=>target is TableStation?"Serving":"Cashier",
            InteractionKind.Repair=>"Operate",
            _=>target switch{ProductionStation p=>p.Animal?"AnimalCare":"Farming",TableStation=>"Cleaning",
                MachineStation m=>m.AreaId is "bakery" or "restaurant"?"Cooking":"Operate",_=>"Operate"}
        };
        public void Play(string state)
        {
            if (state == State || !Animator) return;
            int hash = Animator.StringToHash(state);
            if (!Animator.HasState(0, hash)) return;
            Animator.CrossFadeInFixedTime(hash, .14f);
            State = state;
        }
    }
}
