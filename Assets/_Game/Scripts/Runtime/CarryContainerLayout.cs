using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public sealed class CarryContainerLayout
    {
        readonly List<GameObject> layers = new();
        public int ActiveCount { get; private set; }

        public bool TryLayout(Transform owner, IReadOnlyList<GameObject> items, out float top, out float rearExtent)
        {
            top = 0;
            rearExtent = 0;
            if (items.Count == 0 || !CarryContainerProfile.Supports(items[0].name))
            {
                Clear();
                return false;
            }
            string id = items[0].name;
            for (int i = 1; i < items.Count; i++)
                if (items[i].name != id)
                {
                    Clear();
                    return false;
                }
            var profile = new CarryContainerProfile(id, items[0].GetComponent<MeshFilter>().sharedMesh);
            var mesh = CarryContainerGeometry.Get(id, profile);
            var material = CarryContainerGeometry.Material(id);
            int count = (items.Count + CarryContainerProfile.Slots - 1) / CarryContainerProfile.Slots;
            while (layers.Count < count)
            {
                var layer = new GameObject("CarryContainer_" + layers.Count);
                layer.transform.SetParent(owner, false);
                layer.AddComponent<MeshFilter>();
                layer.AddComponent<MeshRenderer>();
                layers.Add(layer);
            }
            for (int i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                layer.SetActive(i < count);
                if (i >= count) continue;
                layer.transform.localPosition = Vector3.up * (i * profile.LayerHeight);
                layer.GetComponent<MeshFilter>().sharedMesh = mesh;
                layer.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i].transform;
                item.localRotation = Quaternion.identity;
                item.localScale = Vector3.one * profile.Scale;
                item.localPosition = profile.Position(i);
            }
            ActiveCount = count;
            top = count * profile.LayerHeight;
            rearExtent = profile.Depth * .5f;
            return true;
        }

        public void Clear()
        {
            // Giữ khay rỗng ẩn tại owner để lần nhặt sau tái dùng; hủy cùng owner khi despawn.
            for (int i = 0; i < layers.Count; i++)
                if (layers[i]) layers[i].SetActive(false);
            ActiveCount = 0;
        }
    }
}
