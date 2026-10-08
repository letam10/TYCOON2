using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Tycoon
{
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(50)]
    public sealed class FollowCamera : MonoBehaviour
    {
        public Transform Target;
        public float Pitch = 55;
        public float Yaw = 40;
        public float Distance = 16;
        public Vector3 FocusOffset=new(.5f,.8f,1.3f);
        public bool Overview;
        public float Damping = .18f;
        public const float MinimumDistance = 12, MaximumDistance = 27;
        Vector3 velocity, previousTargetPosition, lookAhead;
        Transform followedTarget;
        void Awake() => GetComponent<Camera>().orthographic = false;
        void LateUpdate()
        {
            var game = GameSession.Instance;
            if (!Overview && game && game.Player && Target == game.Player.transform && game.Player.CanControl)
            {
                bool overUi = EventSystem.current && EventSystem.current.IsPointerOverGameObject();
                float wheel = Mouse.current?.scroll.ReadValue().y ?? 0;
                if (!overUi && Mathf.Abs(wheel) > .01f) Zoom(Mathf.Sign(wheel));
                float stick = Gamepad.current?.rightStick.ReadValue().y ?? 0;
                if (Mathf.Abs(stick) > .2f) Zoom(stick * Time.deltaTime * 5);
            }
            Follow(Time.deltaTime);
        }
        public void Zoom(float steps)
        {
            if (Overview || !float.IsFinite(steps) || !float.IsFinite(Distance)) return;
            Distance = Mathf.Clamp(Distance - steps * 1.5f, MinimumDistance, MaximumDistance);
        }
        public void Follow(float delta)
        {
            if (!Target || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
            if (followedTarget != Target) { followedTarget = Target; previousTargetPosition = Target.position; lookAhead = Vector3.zero; velocity = Vector3.zero; }
            Vector3 travelled = Target.position - previousTargetPosition; travelled.y = 0;
            previousTargetPosition = Target.position;
            if (!Overview && travelled.sqrMagnitude > 144) { Snap(); return; }
            // Nhìn trước một chút theo chuyển động, không đổi hướng WASD hay góc camera.
            Vector3 lead = Overview ? Vector3.zero : Vector3.ClampMagnitude(travelled / delta * .12f, .9f);
            lookAhead = Vector3.Lerp(lookAhead, lead, 1 - Mathf.Exp(-7 * delta));
            var rotation = Quaternion.Euler(Pitch, Yaw, 0);
            var focus = Overview ? new Vector3(5, .1f, 20) : Target.position + FocusOffset + lookAhead;
            float distance = Overview ? 95 : Distance;
            var destination = focus - rotation * Vector3.forward * distance;
            transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, Damping, Mathf.Infinity, delta);
            transform.rotation = rotation;
        }
        public void Snap()
        {
            if (!Target) return;
            followedTarget = Target; previousTargetPosition = Target.position; lookAhead = Vector3.zero;
            var rotation = Quaternion.Euler(Pitch, Yaw, 0);
            transform.rotation = rotation;
            var focus = Overview ? new Vector3(5, .1f, 20) : Target.position + FocusOffset;
            transform.position = focus - rotation * Vector3.forward * (Overview ? 95 : Distance);
            GetComponent<Camera>().orthographic = false;
            velocity = Vector3.zero;
        }
    }
}
