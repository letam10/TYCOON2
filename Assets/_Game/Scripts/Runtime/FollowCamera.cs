using UnityEngine;

namespace Tycoon
{
    public sealed class FollowCamera : MonoBehaviour
    {
        public Transform Target;
        public float Pitch = 55;
        public float Yaw = 40;
        public float Distance = 24;
        public Vector3 FocusOffset=new(2,.8f,3);
        public bool Overview;
        Vector3 velocity;
        void LateUpdate()
        {
            if (!Target) return;
            var rotation = Quaternion.Euler(Pitch, Yaw, 0);
            var focus = Overview ? new Vector3(5, .1f, 20) : Target.position + FocusOffset;
            float distance = Overview ? 95 : Distance;
            var destination = focus - rotation * Vector3.forward * distance;
            transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, .22f);
            transform.rotation = rotation;
        }
        public void Snap()
        {
            if (!Target) return;
            var rotation = Quaternion.Euler(Pitch, Yaw, 0);
            transform.rotation = rotation;
            transform.position = Target.position + FocusOffset - rotation * Vector3.forward * Distance;
            velocity = Vector3.zero;
        }
    }
}
