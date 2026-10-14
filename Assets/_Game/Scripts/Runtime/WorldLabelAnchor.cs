using UnityEngine;

namespace Tycoon
{
    [DisallowMultipleComponent]
    public sealed class WorldLabelAnchor : MonoBehaviour
    {
        [Tooltip("Bật khi nhãn được xoay theo camera qua BillboardLabel.")]
        public bool Billboard = true;

        [Tooltip("Root model dùng để parent nhãn; audit nhận footprint qua API tĩnh.")]
        public Transform ModelRoot;

        public bool HasBillboardComponent => GetComponent<BillboardLabel>() != null;

        public bool IsFacingOrBillboardConfigured => !Billboard || HasBillboardComponent;
    }
}
