using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public sealed class InventoryStack : MonoBehaviour
    {
        [System.NonSerialized] public Inventory Inventory;
        [System.NonSerialized] public ItemPool Pool;
        public int Columns = 3;
        public int Rows = 2;
        public int Maximum = 48;
        public float Spacing = .23f;
        public float Scale = .72f;
        public float LayerHeight = .23f;
        public bool HeldLayout;
        public bool ShowCount;
        public float MinimumCountHeight;
        public int VisibleCount => items.Count;
        public string VisibleItemId(int index) => items[index].name;
        public float Top { get; private set; }
        readonly List<GameObject> items = new();
        readonly List<string> desired = new();
        int revision = -1;
        Inventory displayedInventory;
        TextMesh badge;
        float previousLayout;

        void LateUpdate()
        {
            using var frameProbe = QaFrameProbe.Measure("InventoryStack.LateUpdate");
            Refresh();
        }

        public void Refresh()
        {
            using var frameProbe = QaFrameProbe.Measure("InventoryStack.Refresh");
            if (Inventory == null || Pool == null) return;
            float layout = Columns + Rows * 13 + Maximum * 37 + Spacing * 53 + Scale * 97 +
                LayerHeight * 137 + MinimumCountHeight * 173 + (ShowCount ? 199 : 0) + (HeldLayout ? 233 : 0);
            if (ReferenceEquals(displayedInventory, Inventory) && revision == Inventory.Revision &&
                Mathf.Approximately(layout, previousLayout)) return;
            displayedInventory = Inventory;
            revision = Inventory.Revision;
            previousLayout = layout;
            desired.Clear();
            foreach (var row in Inventory.Snapshot())
                for (int n = 0; n < row.count && desired.Count < Maximum; n++) desired.Add(row.id);
            // Giữ lại mesh đang đúng vị trí; chỉ lấy/trả pool phần số lượng thực sự đổi.
            for (int i = items.Count - 1; i >= 0; i--)
                if (i >= desired.Count || items[i].name != desired[i])
                {
                    Pool.Return(items[i]);
                    items.RemoveAt(i);
                }
            for (int i = 0; i < desired.Count; i++)
            {
                if (i >= items.Count || items[i].name != desired[i])
                    items.Insert(i, Pool.Take(desired[i], transform));
            }
            if (HeldLayout) LayoutHeld();
            else LayoutGrid();
            RefreshBadge();
        }

        void LayoutHeld()
        {
            Top = 0;
            if (items.Count == 0) return;
            var profile = CarryItemLayout.For(items[0].name);
            var bounds = profile.Bounds(items[0].GetComponent<MeshFilter>().sharedMesh);
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i].transform;
                item.localRotation = profile.Rotation;
                item.localScale = Vector3.one * profile.Scale;
                item.localPosition = profile.Position(i, items.Count, bounds);
                Top = Mathf.Max(Top, item.localPosition.y + bounds.max.y);
            }
        }

        void LayoutGrid()
        {
            Vector3 size = Vector3.zero;
            foreach (var item in items)
            {
                var rotation = StoredItemLayout.Rotation(item.name);
                var bounds = ItemStackBounds.Transform(item.GetComponent<MeshFilter>().sharedMesh, rotation, Scale);
                size = Vector3.Max(size, bounds.size);
            }
            float spacingX = Mathf.Max(Spacing, size.x + .018f);
            float spacingZ = Mathf.Max(Spacing, size.z + .018f);
            int columns = Mathf.Max(1, Columns);
            int rows = Mathf.Max(1, Rows);
            int usedColumns = Mathf.Min(columns, items.Count);
            int usedRows = Mathf.Min(rows, Mathf.CeilToInt((float)items.Count / columns));
            var supportedTop = new float[columns * rows];
            Top = 0;
            for (int i = 0; i < items.Count; i++)
            {
                int col = i % columns;
                int row = i / columns % rows;
                var item = items[i].transform;
                item.localRotation = StoredItemLayout.Rotation(item.name);
                item.localScale = Vector3.one * Scale;
                var bounds = ItemStackBounds.Transform(item.GetComponent<MeshFilter>().sharedMesh,
                    item.localRotation, Scale);
                int cell = i % supportedTop.Length;
                // Đáy đã xoay chạm bàn hoặc món phía dưới, kể cả kho có nhiều loại hàng.
                item.localPosition = new Vector3(
                    (col - (usedColumns - 1) * .5f) * spacingX - bounds.center.x,
                    supportedTop[cell] - bounds.min.y,
                    (row - (usedRows - 1) * .5f) * spacingZ - bounds.center.z);
                float top = item.localPosition.y + bounds.max.y;
                supportedTop[cell] = top + .006f;
                Top = Mathf.Max(Top, top);
            }
        }

        void RefreshBadge()
        {
            if (ShowCount && !badge && Art.Catalog)
                badge = Art.Label("", Vector3.zero, transform, .12f, "#FFFFFF");
            if (badge)
            {
                badge.gameObject.SetActive(ShowCount && Inventory.Total > 0);
                badge.text = Inventory.Total + "/" + Inventory.Capacity;
                badge.transform.localPosition = new Vector3(0, Mathf.Max(MinimumCountHeight, Top + .16f), 0);
                badge.GetComponent<BillboardLabel>()?.RefreshNow();
            }
        }

        void OnDisable()
        {
            if (Pool != null) foreach (var go in items) Pool.Return(go, false);
            items.Clear();
            revision = -1;
        }
    }
}
