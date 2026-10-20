using System.Collections.Generic;
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
        [Min(.05f)] public float CollisionRadius = .25f;
        [Min(0f)] public float CollisionPadding = .25f;
        [Min(.1f)] public float MinimumClearDistance = 1.5f;
        public LayerMask CollisionMask = ~0;
        public LayerMask OcclusionMask = ~0;
        public bool HideOccluders = true;
        public const float MinimumDistance = 12, MaximumDistance = 27;
        Vector3 velocity, previousTargetPosition, lookAhead;
        Transform followedTarget;
        bool overviewLens;
        bool savedFog;
        float savedFar;
        readonly Dictionary<Renderer, bool> occludedRenderers =
            new Dictionary<Renderer, bool>();

        void UpdateOverviewLens()
        {
            if (overviewLens == Overview) return;
            var lens = GetComponent<Camera>();
            overviewLens = Overview;
            if (Overview)
            {
                savedFar = lens.farClipPlane;
                savedFog = RenderSettings.fog;
                float overviewDistance = CityCameraFraming.OverviewDistance(
                    lens, Quaternion.Euler(Pitch, Yaw, 0));
                float boundsRadius = CityDistricts.WorldBounds.extents.magnitude;
                lens.farClipPlane = Mathf.Max(savedFar, overviewDistance + boundsRadius * 1.25f);
                RenderSettings.fog = false;
            }
            else
            {
                lens.farClipPlane = savedFar;
                RenderSettings.fog = savedFog;
            }
        }
        void Awake() => GetComponent<Camera>().orthographic = false;
        void LateUpdate()
        {
            using var lateUpdateProbe = QaFrameProbe.Measure("FollowCamera.1.LateUpdate");
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
            UpdateOverviewLens();
            var focus = Overview ? CityDistricts.WorldCenter : Target.position + FocusOffset + lookAhead;
            float desiredDistance = Overview ? CityCameraFraming.OverviewDistance(GetComponent<Camera>(), rotation) : Distance;
            float distance = ResolveDistance(focus, rotation, desiredDistance);
            var destination = focus - rotation * Vector3.forward * distance;
            transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, Damping, Mathf.Infinity, delta);
            transform.rotation = rotation;
            if (!Overview && HideOccluders)
                UpdateOcclusion(focus, transform.position);
            else
                RestoreOccluders();
        }
        public void Snap()
        {
            if (!Target) return;
            followedTarget = Target; previousTargetPosition = Target.position; lookAhead = Vector3.zero;
            var rotation = Quaternion.Euler(Pitch, Yaw, 0);
            UpdateOverviewLens();
            transform.rotation = rotation;
            var focus = Overview ? CityDistricts.WorldCenter : Target.position + FocusOffset;
            float desiredDistance = Overview ? CityCameraFraming.OverviewDistance(GetComponent<Camera>(), rotation) : Distance;
            float distance = ResolveDistance(focus, rotation, desiredDistance);
            transform.position = focus - rotation * Vector3.forward * distance;
            GetComponent<Camera>().orthographic = false;
            velocity = Vector3.zero;
            if (!Overview && HideOccluders)
                UpdateOcclusion(focus, transform.position);
            else
                RestoreOccluders();
        }

        float ResolveDistance(Vector3 focus, Quaternion rotation, float desiredDistance)
        {
            if (Overview || !float.IsFinite(desiredDistance) || desiredDistance <= 0f)
                return desiredDistance;

            Vector3 direction = -(rotation * Vector3.forward);
            RaycastHit[] hits = Physics.SphereCastAll(
                focus,
                Mathf.Max(.05f, CollisionRadius),
                direction,
                desiredDistance,
                CollisionMask,
                QueryTriggerInteraction.Ignore);
            float nearest = desiredDistance;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || IsTargetCollider(hit.collider))
                    continue;
                if (hit.distance > .01f)
                    nearest = Mathf.Min(nearest, hit.distance);
            }

            return CityCameraFraming.ResolveDistance(
                desiredDistance,
                nearest,
                CollisionPadding,
                MinimumClearDistance);
        }

        void UpdateOcclusion(Vector3 focus, Vector3 cameraPosition)
        {
            RestoreOccluders();
            Vector3 offset = cameraPosition - focus;
            float distance = offset.magnitude;
            if (distance <= .01f)
                return;

            Vector3 direction = offset / distance;
            RaycastHit[] hits = Physics.RaycastAll(
                focus,
                direction,
                distance,
                OcclusionMask,
                QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || IsTargetCollider(hit.collider))
                    continue;

                Renderer renderer = hit.collider.GetComponentInParent<Renderer>();
                if (renderer == null || occludedRenderers.ContainsKey(renderer))
                    continue;

                occludedRenderers.Add(renderer, renderer.enabled);
                renderer.enabled = false;
            }
        }

        void RestoreOccluders()
        {
            foreach (KeyValuePair<Renderer, bool> entry in occludedRenderers)
            {
                if (entry.Key != null)
                    entry.Key.enabled = entry.Value;
            }

            occludedRenderers.Clear();
        }

        bool IsTargetCollider(Collider collider)
        {
            if (collider == null || !Target)
                return false;
            return collider.transform == Target || collider.transform.IsChildOf(Target);
        }
    }
}
