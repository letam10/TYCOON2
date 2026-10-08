using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed class CityPedestrianPlayerObstacle : MonoBehaviour
    {
        public static void Attach(GameObject player)
        {
            var obstacle = player.GetComponent<NavMeshObstacle>();
            if (!obstacle) obstacle = player.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Capsule;
            obstacle.center = new Vector3(0, .9f, 0);
            obstacle.radius = .55f;
            obstacle.height = 1.8f;
            obstacle.carving = false;
        }
    }
}
