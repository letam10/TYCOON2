using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    // Nhận OnDestroy cả trong EditMode để thu hồi mesh tạo khi dựng/kiểm tra model.
    [ExecuteAlways]
    public sealed class CityVehicleRig : MonoBehaviour
    {
        public CityVehicleKind Kind { get; set; }
        public Vector3 DoorInsideLocal { get; set; }
        public Transform[] Wheels { get; set; } = Array.Empty<Transform>();
        public Transform[] LeftDoors { get; set; } = Array.Empty<Transform>();
        public Transform[] RightDoors { get; set; } = Array.Empty<Transform>();
        public float WheelRadius { get; set; } = .34f;

        readonly List<Mesh> ownedMeshes = new();
        readonly Dictionary<Transform, Vector3> closedPositions = new();
        readonly Dictionary<Transform, Quaternion> closedRotations = new();

        public void Own(Mesh mesh) => ownedMeshes.Add(mesh);

        public void SetTravel(float speed, float delta)
        {
            if (delta <= 0 || WheelRadius <= 0) return;
            float angle = speed * delta / WheelRadius * Mathf.Rad2Deg;
            foreach (var wheel in Wheels)
                if (wheel) wheel.Rotate(Vector3.right, angle, Space.Self);
        }

        public void SetDoorOpen(float amount, int side = 1)
        {
            amount = Mathf.Clamp01(amount);
            Move(LeftDoors, side < 0 ? amount : 0, -1);
            Move(RightDoors, side >= 0 ? amount : 0, 1);
        }

        void Move(Transform[] doors, float amount, int side)
        {
            bool sliding = Kind is CityVehicleKind.Bus or CityVehicleKind.Coach or CityVehicleKind.Train;
            foreach (var door in doors)
            {
                if (!door) continue;
                if (!closedPositions.ContainsKey(door))
                {
                    closedPositions.Add(door, door.localPosition);
                    closedRotations.Add(door, door.localRotation);
                }
                // Cửa trượt nhích ra khỏi vỏ trước khi lùi, cửa ô tô quay quanh bản lề trước.
                door.localPosition = closedPositions[door] + (sliding
                    ? new Vector3(side * .12f * amount, 0, -1.06f * amount) : Vector3.zero);
                door.localRotation = closedRotations[door] * Quaternion.Euler(0,
                    sliding ? 0 : -side * 68 * amount, 0);
            }
        }

        void OnDestroy()
        {
            foreach (var mesh in ownedMeshes)
            {
                if (!mesh) continue;
                if (Application.isPlaying) Destroy(mesh);
                else DestroyImmediate(mesh);
            }
            ownedMeshes.Clear();
        }
    }
}
