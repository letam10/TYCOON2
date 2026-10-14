using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public sealed class ActorAnimationSmoothing : MonoBehaviour
    {
        const float DefaultTransition = .12f;
        const float MotionThreshold = .08f;
        const float Acceleration = 10f;
        const float Deceleration = 14f;
        const float MinimumLocomotionSpeed = .01f;
        const float FallbackDeltaTime = 1f / 60f;
        const float MaximumDeltaTime = .05f;
        const string SpeedParameterName = "Speed";
        static readonly string[] KnownStates = { "Idle", "Walk", "WalkSlow",
            "Run", "Carry", "CarryWalk" };

        readonly Dictionary<string, int> stateHashes = new();
        readonly Dictionary<string, float> clipLengths = new();
        Animator animator;
        float locomotionSpeed = 1f;
        float targetSpeed;
        float currentSpeed;
        bool grounded;
        bool hasSpeedParameter;
        string currentState;
        string actionState;

        public float CurrentSpeed => currentSpeed;

        public void ClearAction()
        {
            if (actionState == null) return;
            actionState = null;
            ApplyLocomotionState();
        }

        public void Initialize(Animator animator, float locomotionSpeed = 1f)
        {
            this.animator = animator;
            this.locomotionSpeed = Mathf.Max(MinimumLocomotionSpeed, locomotionSpeed);
            targetSpeed = 0;
            currentSpeed = 0;
            grounded = false;
            currentState = null;
            actionState = null;
            stateHashes.Clear();
            clipLengths.Clear();
            hasSpeedParameter = false;

            if (!this.animator) return;
            this.animator.applyRootMotion = false;
            this.animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            CacheAnimatorData();
        }

        public void SetMotion(Vector3 planarVelocity, bool grounded)
        {
            this.grounded = grounded;
            Vector3 planar = new Vector3(planarVelocity.x, 0, planarVelocity.z);
            targetSpeed = grounded ? planar.magnitude : 0;
            // SetMotion được gọi theo nhịp actor; mỗi lần gọi chỉ tiến một bước mượt.
            Advance(Time.deltaTime > 0 ? Time.deltaTime : FallbackDeltaTime);
            ApplyLocomotionState();
        }

        public void SetAction(string state, float transition = DefaultTransition)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                actionState = null;
                ApplyLocomotionState();
                return;
            }
            if (!animator) return;
            if (!TryGetStateHash(state, out _)) return;
            if (actionState == state && currentState == state) return;
            if (TransitionTo(state, transition)) actionState = state;
        }

        void CacheAnimatorData()
        {
            foreach (var parameter in animator.parameters)
                if (parameter.name == SpeedParameterName
                    && parameter.type == AnimatorControllerParameterType.Float)
                    hasSpeedParameter = true;

            foreach (string state in KnownStates) TryGetStateHash(state, out _);
            if (!animator.runtimeAnimatorController) return;
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (clip) clipLengths[clip.name] = Mathf.Max(.001f, clip.length);

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            foreach (var pair in stateHashes)
                if (pair.Value == info.shortNameHash)
                {
                    currentState = pair.Key;
                    break;
                }
        }

        void Advance(float deltaTime)
        {
            float rate = targetSpeed > currentSpeed ? Acceleration : Deceleration;
            float step = Mathf.Min(MaximumDeltaTime, Mathf.Max(0, deltaTime));
            float blend = 1 - Mathf.Exp(-rate * step);
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, blend);
            if (Mathf.Abs(currentSpeed - targetSpeed) < .001f) currentSpeed = targetSpeed;
            ApplyAnimator(deltaTime);
        }

        void ApplyAnimator(float deltaTime)
        {
            if (!animator) return;
            animator.applyRootMotion = false;
            float desiredPlaybackSpeed = actionState == null ? LocomotionPlaybackSpeed() : 1f;
            float blend = 1 - Mathf.Exp(-8f * Mathf.Max(0, deltaTime));
            animator.speed = Mathf.Lerp(animator.speed, desiredPlaybackSpeed, blend);
            if (hasSpeedParameter) animator.SetFloat(SpeedParameterName, currentSpeed / locomotionSpeed);
        }

        float LocomotionPlaybackSpeed()
        {
            if (currentSpeed <= MotionThreshold) return 1f;
            return Mathf.Clamp(currentSpeed / locomotionSpeed, .35f, 1.9f);
        }

        void ApplyLocomotionState(float transition = DefaultTransition)
        {
            if (!animator || actionState != null) return;
            string preferred = "Idle";
            if (grounded && currentSpeed > MotionThreshold)
                preferred = currentSpeed >= locomotionSpeed * 1.25f ? "Run" : "Walk";
            string state = ResolveLocomotionState(preferred);
            if (state != null) TransitionTo(state, transition);
        }

        string ResolveLocomotionState(string preferred)
        {
            if (TryGetStateHash(preferred, out _)) return preferred;
            if (preferred == "Run" && TryGetStateHash("Walk", out _)) return "Walk";
            if (preferred != "Idle" && TryGetStateHash("WalkSlow", out _)) return "WalkSlow";
            return preferred == "Idle" ? null : TryGetStateHash("Idle", out _) ? "Idle" : null;
        }

        bool TryGetStateHash(string state, out int hash)
        {
            hash = 0;
            if (!animator || string.IsNullOrEmpty(state)) return false;
            if (stateHashes.TryGetValue(state, out hash)) return true;
            hash = Animator.StringToHash(state);
            if (!animator.HasState(0, hash))
            {
                hash = 0;
                return false;
            }
            stateHashes[state] = hash;
            return true;
        }

        bool TransitionTo(string state, float transition)
        {
            if (!TryGetStateHash(state, out int hash)) return false;
            if (currentState == state) return true;
            float offset = TransitionOffset(state);
            animator.CrossFadeInFixedTime(hash, Mathf.Max(0, transition), 0, offset);
            currentState = state;
            return true;
        }

        float TransitionOffset(string state)
        {
            if (!IsLocomotion(currentState) || !IsLocomotion(state)) return 0;
            if (!clipLengths.TryGetValue(state, out float length)) return 0;
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            return Mathf.Repeat(info.normalizedTime, 1) * length;
        }

        static bool IsLocomotion(string state)
        {
            return state is "Idle" or "Walk" or "WalkSlow" or "Run" or "Carry" or "CarryWalk";
        }
    }
}
