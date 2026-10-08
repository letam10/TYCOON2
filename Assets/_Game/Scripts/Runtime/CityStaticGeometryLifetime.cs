using UnityEngine;

namespace Tycoon
{
    public sealed class CityStaticGeometryLifetime : MonoBehaviour
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
