using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    [DefaultExecutionOrder(-60)]
    public sealed class ActorPhysicalMotion : MonoBehaviour
    {
        public NavMeshAgent Agent { get; private set; }
        public CharacterController Body { get; private set; }
        public float Speed { get; private set; }
        public float DistanceMoved { get; private set; }
        Vector3 previous;

        public static ActorPhysicalMotion Attach(NavMeshAgent agent)
        {
            var motion = agent.GetComponent<ActorPhysicalMotion>();
            if (!motion) motion = agent.gameObject.AddComponent<ActorPhysicalMotion>();
            motion.Initialize(agent);
            return motion;
        }

        public void Initialize(NavMeshAgent agent)
        {
            Agent = agent;
            Body = GetComponent<CharacterController>();
            if (!Body) Body = gameObject.AddComponent<CharacterController>();
            Configure(Body);
            Agent.updatePosition = false;
            Agent.updateRotation = false;
            previous = transform.position;
        }

        public static void Configure(CharacterController body)
        {
            body.height = 1.8f;
            body.radius = .3f;
            body.center = new Vector3(0, .91f, 0);
            body.stepOffset = .16f;
            body.skinWidth = .035f;
            body.slopeLimit = 45;
            body.minMoveDistance = 0;
        }

        void OnEnable()
        {
            previous = transform.position;
            Speed = 0;
        }

        void Update()
        {
            if (!Agent || !Body) return;
            if (!Agent.enabled)
            {
                Body.enabled = false;
                Speed = 0;
                previous = transform.position;
                return;
            }
            if (!Body.enabled) Body.enabled = true;
            if (!Agent.isOnNavMesh) return;
            var game = GameSession.Instance;
            if (Time.deltaTime <= 0 || game && !game.CanSimulate)
            {
                Agent.nextPosition = transform.position;
                Speed = 0;
                previous = transform.position;
                return;
            }
            // NavMesh chọn đường; capsule vật lý quyết định bước nào thực sự đi được.
            Vector3 requested = Agent.nextPosition - transform.position;
            requested.y = -2 * Time.deltaTime;
            ApplyStep(requested, Time.deltaTime);
            Agent.nextPosition = transform.position;
            Vector3 direction = transform.position - previous;
            direction.y = 0;
            if (direction.sqrMagnitude > .000001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(direction), Agent.angularSpeed * Time.deltaTime);
            previous = transform.position;
        }

        public void ApplyStep(Vector3 requested, float delta)
        {
            if (!Body || !Body.enabled || delta <= 0) return;
            Vector3 before = transform.position;
            Body.Move(requested);
            Vector3 travelled = transform.position - before;
            travelled.y = 0;
            DistanceMoved += travelled.magnitude;
            Speed = travelled.magnitude / delta;
        }

        public bool Warp(Vector3 point)
        {
            bool active = Body && Body.enabled;
            if (active) Body.enabled = false;
            bool moved = Agent && Agent.enabled && Agent.Warp(point);
            if (moved)
            {
                transform.position = Agent.nextPosition;
                previous = transform.position;
                Speed = 0;
            }
            if (active) Body.enabled = true;
            return moved;
        }

        public void Suspend()
        {
            if (Body) Body.enabled = false;
            if (Agent) Agent.enabled = false;
            Speed = 0;
        }
    }
}
