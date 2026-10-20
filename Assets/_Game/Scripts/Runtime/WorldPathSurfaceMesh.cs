using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tycoon
{
    public static class WorldPathSurfaceMesh
    {
        public static void Build(Transform root, WorldPathSurfaceLayout layout)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));

            var batches = new Dictionary<(string Surface, string Tint, float Bottom, float Top),
                List<WorldPathSurfacePiece>>();
            var keys = new List<(string Surface, string Tint, float Bottom, float Top)>();
            foreach (WorldPathSurfacePiece piece in layout.Pieces)
            {
                WorldPathSurfaceRequest request = piece.Request;
                var key = (request.Surface, request.Tint, request.Bottom, request.Top);
                if (!batches.TryGetValue(key, out List<WorldPathSurfacePiece> batch))
                {
                    batch = new List<WorldPathSurfacePiece>();
                    batches.Add(key, batch);
                    keys.Add(key);
                }
                batch.Add(piece);
            }

            var owner = root.GetComponent<WorldStreetMeshOwner>();
            if (owner == null)
                owner = root.gameObject.AddComponent<WorldStreetMeshOwner>();
            for (int batchIndex = 0; batchIndex < keys.Count; batchIndex++)
            {
                var key = keys[batchIndex];
                List<WorldPathSurfacePiece> batch = batches[key];
                var pieces = new List<CombineInstance>(batch.Count);
                Mesh combined = null;
                try
                {
                    int vertexCount = 0;
                    foreach (WorldPathSurfacePiece piece in batch)
                    {
                        Mesh prism = WorldStreetMesh.CreatePrism(piece.Geometry, key.Bottom, key.Top);
                        vertexCount += prism.vertexCount;
                        pieces.Add(new CombineInstance
                        {
                            mesh = prism,
                            transform = Matrix4x4.identity
                        });
                    }
                    combined = new Mesh { name = "PathSurfaceMesh_" + batchIndex };
                    owner.Register(combined);
                    if (vertexCount > ushort.MaxValue)
                        combined.indexFormat = IndexFormat.UInt32;
                    combined.CombineMeshes(pieces.ToArray(), true, true, false);
                    combined.RecalculateBounds();
                }
                finally
                {
                    foreach (CombineInstance piece in pieces)
                    {
                        if (Application.isPlaying)
                            UnityEngine.Object.Destroy(piece.mesh);
                        else
                            UnityEngine.Object.DestroyImmediate(piece.mesh);
                    }
                }

                // Cùng tọa độ gốc và cùng mesh để hình ảnh, mặt đi bộ luôn khớp nhau.
                var part = new GameObject("PathSurface_" + batchIndex);
                part.transform.SetParent(root, false);
                part.AddComponent<MeshFilter>().sharedMesh = combined;
                var renderer = part.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = TownSurfaceMaterials.Get(key.Surface, key.Tint);
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                part.AddComponent<MeshCollider>().sharedMesh = combined;
            }
        }
    }
}
