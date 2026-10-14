using UnityEngine;

namespace Tycoon
{
    public enum DiningTableShape { Round, Square, Counter }

    public sealed class DiningFurniture : MonoBehaviour
    {
        public DiningTableShape Shape;
        // Tâm hông trên đệm ghế; trục forward của anchor hướng về bàn.
        public Transform[] Seats;
        public Transform Approach;
        // Cao độ mặt bàn trong hệ tọa độ DiningFurniture.
        public float TableTop;
    }
}
