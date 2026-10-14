using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    [DefaultExecutionOrder(100)]
    public sealed class DinerSeatingPose : MonoBehaviour
    {
        public const float TransitionSeconds = .45f;
        public Transform Seat { get; private set; }
        public bool IsSeated => Seat && !leaving;
        public bool IsMoving => Seat && blend < 1;
        NavMeshAgent agent;
        ActorView view;
        Transform hips;
        AnimatorCullingMode culling;
        Vector3 origin;
        Quaternion originRotation;
        Vector3 returnPoint;
        System.Action released;
        float blend;
        bool leaving;

        public void Initialize(NavMeshAgent navigation, ActorView actor)
        {
            agent = navigation;
            view = actor;
            if (actor.Animator && actor.Animator.isHuman)
                hips = actor.Animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips) return;
            foreach (var bone in actor.GetComponentsInChildren<Transform>(true))
                if (bone.name == "Hips")
                {
                    hips = bone;
                    break;
                }
        }

        public void Sit(Transform anchor, Vector3 approach, bool immediate = false)
        {
            if (!anchor || !hips || Seat == anchor && !leaving) return;
            Seat = anchor;
            returnPoint = approach;
            leaving = false;
            blend = immediate ? 1 : 0;
            origin = transform.position;
            originRotation = transform.rotation;
            Navigation.Suspend(agent);
            if (view)
            {
                view.SetMotion(0, false);
                view.Work("Sit");
                if (view.Animator)
                {
                    culling = view.Animator.cullingMode;
                    view.Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    if (immediate)
                    {
                        view.Animator.Play("Sit", 0, 0);
                        view.Animator.Update(0);
                    }
                }
            }
            Advance(0);
        }

        public void Leave(System.Action onReleased)
        {
            if (leaving) return;
            if (!Seat)
            {
                onReleased?.Invoke();
                return;
            }
            leaving = true;
            blend = 0;
            origin = transform.position;
            released = onReleased;
            if (view) view.Play("Idle");
        }

        public static Vector3 RootForSeat(Transform actor, Transform hip, Transform seat)
        {
            Vector3 offset = actor.InverseTransformPoint(hip.position);
            return seat.position - seat.rotation * Vector3.Scale(offset, actor.lossyScale);
        }

        public void Advance(float delta)
        {
            if (!Seat || !float.IsFinite(delta)) return;
            blend = Mathf.Clamp01(blend + Mathf.Max(0, delta) / TransitionSeconds);
            float weight = Mathf.SmoothStep(0, 1, blend);
            if (leaving)
            {
                transform.position = Vector3.Lerp(origin, returnPoint, weight);
                if (blend < 1) return;
                var callback = released;
                RestoreAnimator();
                Seat = null;
                released = null;
                leaving = false;
                if (agent)
                {
                    agent.enabled = true;
                    Navigation.Warp(agent, returnPoint);
                    var body = GetComponent<CharacterController>();
                    if (body) body.enabled = true;
                }
                callback?.Invoke();
                return;
            }
            // Anchor đã là tâm hông trên đệm; không cộng thêm chiều cao chân hoặc đệm lần nữa.
            transform.rotation = Quaternion.Slerp(originRotation, Seat.rotation, weight);
            transform.position = Vector3.Lerp(origin, RootForSeat(transform, hips, Seat), weight);
            if (view) view.Work("Sit");
        }

        void LateUpdate()
        {
            using var lateUpdateProbe = QaFrameProbe.Measure("DinerSeatingPose.1.LateUpdate");
            if (Time.deltaTime <= 0 || GameSession.Instance && !GameSession.Instance.CanSimulate) return;
            Advance(Time.deltaTime);
        }

        void RestoreAnimator()
        {
            if (Seat && view && view.Animator) view.Animator.cullingMode = culling;
        }

        public void ResetForReuse()
        {
            RestoreAnimator();
            bool occupied = Seat;
            if (occupied) transform.position = returnPoint;
            Seat = null;
            released = null;
            leaving = false;
            blend = 0;
            if (agent)
            {
                agent.enabled = true;
                if (occupied && gameObject.activeInHierarchy) Navigation.Warp(agent, returnPoint);
                var body = GetComponent<CharacterController>();
                if (body) body.enabled = true;
            }
            if (occupied && view && gameObject.activeInHierarchy) view.Play("Idle");
        }

        void OnDisable() => ResetForReuse();
    }
}
