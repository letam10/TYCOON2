using UnityEngine;
using Unity.AI.Navigation;

namespace Tycoon
{
    public static class CheckoutCashModels
    {
        // Điểm đứng trước thùng để capsule và NavMesh không chọn mặt thùng thay cho mặt đất.
        public static readonly Vector3 CollectionOffset = new(2.7f, 0, -.65f);

        public static GameObject Stand(Transform parent)
        {
            var stand = Art.Model("supply_crate", new Vector3(2.7f, 0, .6f), parent);
            stand.name = "CashStand";
            Vector3 size = RetailPropModels.Size("supply_crate");
            stand.transform.localScale = new Vector3(1.1f / size.x, .7f / size.y, .7f / size.z);
            var body = stand.AddComponent<BoxCollider>();
            body.center = Vector3.up * size.y * .5f;
            body.size = size;
            var navigation = stand.AddComponent<NavMeshModifier>();
            navigation.overrideArea = true;
            navigation.area = 1;
            return stand;
        }

        public static GameObject Bill(Transform parent)
        {
            var bill = HandItemModels.Build("cash", parent);
            bill.name = "CashBill";
            var bounds = bill.GetComponent<MeshFilter>().sharedMesh.bounds;
            bill.transform.localScale = Vector3.one * (.48f / bounds.size.x);
            return bill;
        }

        public static Vector3 Position(int index, GameObject bill)
        {
            float height = bill.GetComponent<MeshFilter>().sharedMesh.bounds.size.y * bill.transform.localScale.y;
            // Mesh tiền có đáy tại y0; tầng đầu chạm thùng, các tầng sau cách nhau một khe giấy nhỏ.
            return new Vector3(2.45f + index % 2 * .5f, .7f + index / 2 * (height + .012f), .6f);
        }
    }
}
