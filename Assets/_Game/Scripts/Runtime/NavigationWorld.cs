using System.Collections;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed class NavigationWorld : MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return null;
            Physics.SyncTransforms();
            var surface = gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true; surface.voxelSize = .16f;
            surface.BuildNavMesh();
            GameSession.Instance.NavigationReady = NavMesh.SamplePosition(new Vector3(5, .1f, 2), out _, 3, NavMesh.AllAreas);
            if (!GameSession.Instance.NavigationReady) Debug.LogError("Không tạo được NavMesh.");
        }
    }
    public static class Navigation
    {
        public static NavMeshAgent Agent(GameObject root)
        {
            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = .48f; agent.height = 1.8f; agent.speed = 4.2f;
            agent.angularSpeed = 480; agent.acceleration = 18; agent.stoppingDistance = .12f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            return agent;
        }
        public static bool Go(NavMeshAgent agent, Vector3 point, bool forceRepath=false)
        {
            if(!agent||!agent.isOnNavMesh||!NavMesh.SamplePosition(point,out var hit,1.8f,NavMesh.AllAreas))return false;
            if(!forceRepath&&(agent.pathPending||agent.hasPath)&&Vector3.SqrMagnitude(agent.destination-hit.position)<.16f)return true;
            return agent.SetDestination(hit.position);
        }
        public static bool Arrived(NavMeshAgent agent) => agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance < .5f;
    }
}
