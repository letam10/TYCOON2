using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed class CityLife : MonoBehaviour
    {
        sealed class Resident
        {
            public NavMeshAgent Agent;
            public ActorView View;
            public Vector3[] Stops;
            public int Next;
            public float Wait;
        }

        readonly List<Resident> residents = new();
        GameSession game;
        public int ResidentCount => residents.Count;

        public UnityEngine.AI.NavMeshAgent FindPassenger(Vector3 stop, float radius)
        {
            foreach (var resident in residents)
            {
                var actor = resident.Agent;
                if (!actor || !actor.enabled || !actor.gameObject.activeInHierarchy) continue;
                var passenger = actor.GetComponent<CityPublicTransportPassenger>();
                if (passenger && !passenger.CanBoard) continue;
                if ((actor.transform.position - stop).sqrMagnitude <= radius * radius) return actor;
            }
            return null;
        }

        public bool BlocksTraffic(Vector3 position, Vector3 forward, float step)
        {
            foreach (var resident in residents)
                if (resident.Agent && resident.Agent.enabled && resident.Agent.gameObject.activeInHierarchy &&
                    CityTrafficSafety.ActorAhead(position, forward, step, resident.Agent.transform.position))
                    return true;
            return false;
        }

        IEnumerator Start()
        {
            game = GameSession.Instance;
            while (!game || !game.NavigationReady || !game.CanSimulate) yield return null;
            gameObject.AddComponent<CityTraffic>();
            Vector3[][] walks =
            {
                new[] { new Vector3(-22, 0, -49.9f), new Vector3(-50, 0, -49.9f) },
                new[] { new Vector3(70, 0, -49.9f), new Vector3(89, 0, -49.9f) },
                new[] { new Vector3(70, 0, 33.9f), new Vector3(90, 0, 33.9f) },
                new[] { new Vector3(-22, 0, 33.9f), new Vector3(12, 0, 33.9f) },
                new[] { new Vector3(90, 0, 39), new Vector3(90, 0, 73) },
                new[] { new Vector3(88, 0, -30), new Vector3(88, 0, 3) },
                new[] { new Vector3(-22, 0, -32), new Vector3(12, 0, -32) },
                new[] { new Vector3(-95, 0, -12), new Vector3(-95, 0, 21) }
            };
            for (int i = 0; i < walks.Length; i++)
            {
                var stops = new List<Vector3>();
                foreach (var point in walks[i])
                    if (NavMesh.SamplePosition(point, out var hit, 4, NavMesh.AllAreas)) stops.Add(hit.position);
                if (stops.Count < 2) continue;
                var actor = new GameObject("CityResident_" + i);
                actor.transform.SetParent(transform, false);
                actor.transform.position = stops[0];
                var model = Art.Model(i % 2 == 0 ? "customer" : "customer_beach", Vector3.zero, actor.transform);
                var view = model.AddComponent<ActorView>();
                view.Initialize();
                var agent = Navigation.Agent(actor);
                agent.speed = 1.7f + i % 3 * .17f;
                residents.Add(new Resident { Agent = agent, View = view, Stops = stops.ToArray(), Next = 1 });
            }
        }

        void Update()
        {
            if (!game) return;
            foreach (var resident in residents)
            {
                var agent = resident.Agent;
                var passenger = agent ? agent.GetComponent<CityPublicTransportPassenger>() : null;
                if (passenger && passenger.IsTravelling) continue;
                if (!agent || !agent.isOnNavMesh) continue;
                agent.isStopped = !game.CanSimulate;
                if (!game.CanSimulate) continue;
                resident.View.SetMotion(Navigation.ActualSpeed(agent), false);
                if (Time.time < resident.Wait) continue;
                if (!agent.hasPath) Navigation.Go(agent, resident.Stops[resident.Next]);
                else if (Navigation.Arrived(agent))
                {
                    agent.ResetPath();
                    resident.Next = (resident.Next + 1) % resident.Stops.Length;
                    resident.Wait = Time.time + 2.5f;
                }
            }
        }
    }
}
