using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public sealed class ActorView : MonoBehaviour
    {
        public Animator Animator;
        public string State { get; private set; }
        readonly Dictionary<string, float> clipLengths = new();
        readonly Dictionary<string, int> stateHashes = new();
        float interactionEnd;
        float movementSpeed;
        float turnLean;
        Quaternion originalRotation;
        Vector3 previousForward;
        bool poseReady;
        public void Initialize()
        {
            Animator = GetComponentInChildren<Animator>();
            if (Animator == null) return;
            Animator.applyRootMotion = false;
            Animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            if (Animator.runtimeAnimatorController)
                foreach (var clip in Animator.runtimeAnimatorController.animationClips)
                {
                    string name = clip.name;
                    int separator = name.LastIndexOf('|');
                    if (separator >= 0) name = name.Substring(separator + 1);
                    clipLengths[name] = clip.length;
                }
            originalRotation = transform.localRotation;
            previousForward = transform.parent ? transform.parent.forward : transform.forward;
            poseReady = true;
        }
        public void SetMotion(float speed, bool carry)
        {
            using var frameProbe = QaFrameProbe.Measure("ActorView.SetMotion");
            movementSpeed = speed;
            if (Time.time < interactionEnd && speed < .3f) return;
            bool moving = speed > (State is "Walk" or "Run" or "CarryWalk" ? .12f : .3f);
            Play(carry ? moving ? "CarryWalk" : "Carry" : speed > 5.5f ? "Run" : moving ? "Walk" : "Idle");
            if (!Animator) return;
            float rate = moving ? Mathf.Clamp(speed / (State == "Run" ? 6f : 3.3f), .35f, 1.9f) : 1;
            Animator.speed = Mathf.Lerp(Animator.speed, rate, 1 - Mathf.Exp(-8 * Time.deltaTime));
        }
        public void Interact(bool pickup)
        {
            interactionEnd = Time.time + .42f;
            if (Animator) Animator.speed = 1;
            Play(pickup ? "Pickup" : "Drop", true);
        }
        public void Work(string state)
        {
            using var frameProbe = QaFrameProbe.Measure("ActorView.Work");
            interactionEnd = Time.time + .22f;
            if (Animator) Animator.speed = 1;
            Play(state);
        }
        public static string WorkState(Station target, InteractionKind kind) => kind switch
        {
            InteractionKind.Serve => target is TableStation ? "Serving" : "Cashier",
            InteractionKind.Repair => "Operate",
            _ => target switch
            {
                ProductionStation producer => producer.Animal ? "AnimalCare" : "Farming",
                TableStation => "Cleaning",
                MachineStation machine => machine.AreaId is "bakery" or "restaurant" ? "Cooking" : "Operate",
                _ => "Operate"
            }
        };
        public void Play(string state, bool replayFinished = false)
        {
            using var frameProbe = QaFrameProbe.Measure("ActorView.Play");
            if (!Animator) return;
            if (state == State && (!replayFinished || Animator.IsInTransition(0)
                || Animator.GetCurrentAnimatorStateInfo(0).normalizedTime < .9f)) return;
            if (!stateHashes.TryGetValue(state, out int hash))
            {
                hash = Animator.StringToHash(state);
                if (!Animator.HasState(0, hash)) return;
                stateHashes[state] = hash;
            }
            float offset = 0;
            // Đổi tư thế cầm khi đang bước vẫn giữ nhịp chân, không giật về đầu clip.
            if (IsWalking(State) && IsWalking(state) && clipLengths.TryGetValue(state, out float length))
                offset = Mathf.Repeat(Animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1) * length;
            Animator.CrossFadeInFixedTime(hash, .14f, 0, offset);
            State = state;
        }

        static bool IsWalking(string state) => state is "Walk" or "Run" or "CarryWalk";

        void LateUpdate()
        {
            if (!poseReady || !transform.parent || Time.deltaTime <= 0) return;
            Vector3 forward = transform.parent.forward;
            float turn = Vector3.SignedAngle(previousForward, forward, Vector3.up);
            previousForward = forward;
            float target = movementSpeed > .3f ? Mathf.Clamp(-turn / Time.deltaTime * .016f, -3, 3) : 0;
            turnLean = Mathf.Lerp(turnLean, target, 1 - Mathf.Exp(-10 * Time.deltaTime));
            float lean = IsWalking(State) ? Mathf.Clamp(movementSpeed * .42f, 0, 2.4f) : 0;
            var rotation = originalRotation * Quaternion.Euler(lean, 0, turnLean);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, rotation,
                1 - Mathf.Exp(-10 * Time.deltaTime));
        }

        void OnEnable()
        {
            State = null;
            interactionEnd = 0;
            movementSpeed = turnLean = 0;
            previousForward = transform.parent ? transform.parent.forward : transform.forward;
        }
    }
}
