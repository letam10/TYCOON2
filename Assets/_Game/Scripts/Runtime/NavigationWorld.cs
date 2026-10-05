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
            // Bake footprint của cả công trình tương lai; unlock không khiến NPC đi xuyên props mới.
            var colliders=GetComponentsInChildren<Collider>(true);var enabledBefore=new bool[colliders.Length];
            for(int i=0;i<colliders.Length;i++){enabledBefore[i]=colliders[i].enabled;colliders[i].enabled=true;}
            var surface = gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true; surface.voxelSize = .16f;
            try{Physics.SyncTransforms();surface.BuildNavMesh();}
            finally{for(int i=0;i<colliders.Length;i++)colliders[i].enabled=enabledBefore[i];}
            GameSession.Instance.NavigationReady = NavMesh.SamplePosition(new Vector3(5, .1f, 2), out _, 3, NavMesh.AllAreas);
            if (!GameSession.Instance.NavigationReady) Debug.LogError("Không tạo được NavMesh.");
        }
    }
    public static class Navigation
    {
        public static NavMeshAgent Agent(GameObject root,bool customerRecovery=false)
        {
            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = .38f; agent.height = 1.8f; agent.speed = 4.2f;
            agent.angularSpeed = 480; agent.acceleration = 18; agent.stoppingDistance = .12f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            if(customerRecovery)root.AddComponent<CustomerNavigationRecovery>();
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
    public sealed class CustomerNavigationRecovery:MonoBehaviour
    {
        NavMeshAgent agent;Vector3 previous;float stuck,resumeAt;int retries;
        public int Repaths {get;private set;}
        void Awake(){agent=GetComponent<NavMeshAgent>();previous=transform.position;}
        void OnEnable(){previous=transform.position;stuck=resumeAt=0;retries=0;if(agent&&agent.isOnNavMesh)agent.isStopped=false;}
        void Update()
        {
            if(!agent||!agent.isOnNavMesh||!GameSession.Instance||!GameSession.Instance.CanSimulate)return;
            if(resumeAt>0){if(Time.time<resumeAt)return;resumeAt=0;agent.isStopped=false;}
            if(!agent.hasPath||agent.remainingDistance<.6f){stuck=0;previous=transform.position;return;}
            float moved=(transform.position-previous).sqrMagnitude;previous=transform.position;
            if(moved>.0004f){stuck=0;retries=0;return;}
            stuck+=Time.deltaTime;if(stuck<3)return;stuck=0;
            if(retries<3){Navigation.Go(agent,agent.destination,true);retries++;Repaths++;}
            else{agent.isStopped=true;resumeAt=Time.time+5;retries=0;}
        }
    }
}
