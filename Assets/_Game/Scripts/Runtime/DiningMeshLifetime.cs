using UnityEngine;

namespace Tycoon
{
    [ExecuteAlways]
    public sealed class DiningMeshLifetime : MonoBehaviour
    {
        public Mesh[] Meshes;

        void OnDestroy()
        {
            if (Meshes == null) return;
            foreach (var mesh in Meshes)
            {
                if (!mesh) continue;
                if (Application.isPlaying) Destroy(mesh);
                else DestroyImmediate(mesh);
            }
        }
    }
}
