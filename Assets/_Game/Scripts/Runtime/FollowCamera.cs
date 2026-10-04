using UnityEngine;

namespace Tycoon
{
    [RequireComponent(typeof(Camera))]
    public sealed class FollowCamera : MonoBehaviour
    {
        public Transform Target;
        public float Pitch = 55;
        public float Yaw = 40;
        public float Distance = 24;
        public Vector3 FocusOffset=new(2,.8f,3);
        public bool Overview;
        public float Damping = .18f;
        Vector3 velocity;
        void Awake() => GetComponent<Camera>().orthographic = false;
        void LateUpdate() => Follow(Time.deltaTime);
        public void Follow(float delta)
        {
            if (!Target || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
            var rotation = Quaternion.Euler(Pitch, Yaw, 0);
            var focus = Overview ? new Vector3(5, .1f, 20) : Target.position + FocusOffset;
            float distance = Overview ? 95 : Distance;
            var destination = focus - rotation * Vector3.forward * distance;
            transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, Damping, Mathf.Infinity, delta);
            transform.rotation = rotation;
        }
        public void Snap()
        {
            if (!Target) return;
            var rotation = Quaternion.Euler(Pitch, Yaw, 0);
            transform.rotation = rotation;
            var focus = Overview ? new Vector3(5, .1f, 20) : Target.position + FocusOffset;
            transform.position = focus - rotation * Vector3.forward * (Overview ? 95 : Distance);
            GetComponent<Camera>().orthographic = false;
            velocity = Vector3.zero;
        }
    }
}
