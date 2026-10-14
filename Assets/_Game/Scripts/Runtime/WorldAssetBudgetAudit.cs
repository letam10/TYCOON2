using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public sealed class WorldAssetBudgetRecord
    {
        public string Name;
        public bool Passed;
        public string Error;
        public int VertexCount;
        public int MaterialCount;
        public long TexturePixelCount;
        public int TextureCount;
        public int MaxTextureWidth;
        public int MaxTextureHeight;
        public int RendererCount;
        public int MissingRendererCount;
        public int MissingMaterialCount;
        public int MissingMeshCount;
        public int NonFiniteBoundsCount;
        public bool HasRenderer;
        public bool BoundsFinite;
        public int MaxVertices;
        public int MaxMaterials;
        public int MaxTexturePixels;

        public int Vertices => VertexCount;
        public int Materials => MaterialCount;
        public long TexturePixels => TexturePixelCount;
        public int MeshVertexCount => VertexCount;
        public int SharedMaterialCount => MaterialCount;
        public long TexturePixelTotal => TexturePixelCount;
        public int TextureWidth => MaxTextureWidth;
        public int TextureHeight => MaxTextureHeight;
        public bool MissingRenderer => MissingRendererCount > 0;
        public bool MissingMaterial => MissingMaterialCount > 0;
        public bool NonFiniteBounds => NonFiniteBoundsCount > 0;
        public bool HasNonFiniteBounds => NonFiniteBounds;

        public WorldAssetBudgetRecord(string name)
        {
            Name = name ?? string.Empty;
            Error = string.Empty;
            BoundsFinite = true;
        }
    }

    public static class WorldAssetBudgetAudit
    {
        public static WorldAssetBudgetRecord Inspect(GameObject root, int maxVertices,
            int maxMaterials, int maxTexturePixels)
        {
            string name = root ? root.name : "<null-root>";
            var record = new WorldAssetBudgetRecord(name)
            {
                MaxVertices = maxVertices,
                MaxMaterials = maxMaterials,
                MaxTexturePixels = maxTexturePixels
            };
            var errors = new List<string>();
            ValidateLimit(maxVertices, "vertex", errors);
            ValidateLimit(maxMaterials, "material", errors);
            ValidateLimit(maxTexturePixels, "texture pixel", errors);

            if (!root)
            {
                record.MissingRendererCount = 1;
                errors.Add("root is null");
                errors.Add("renderer is missing");
                return Finish(record, errors);
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            record.RendererCount = renderers.Length;
            record.HasRenderer = renderers.Length > 0;
            if (renderers.Length == 0)
            {
                record.MissingRendererCount = 1;
                errors.Add("renderer is missing");
            }

            // Gộp texture theo instance id để không đếm lặp texture dùng chung.
            var textures = new HashSet<Texture>();
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (!renderer)
                {
                    record.MissingRendererCount++;
                    continue;
                }

                Mesh mesh = FindMesh(renderer);
                if (!mesh)
                {
                    record.MissingMeshCount++;
                }
                else
                {
                    record.VertexCount += mesh.vertexCount;
                    if (!IsFinite(mesh.bounds))
                    {
                        record.NonFiniteBoundsCount++;
                    }
                    else if (!IsFinite(renderer.bounds))
                    {
                        record.NonFiniteBoundsCount++;
                    }
                }

                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    record.MissingMaterialCount++;
                    continue;
                }

                record.MaterialCount += materials.Length;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (!material)
                    {
                        record.MissingMaterialCount++;
                        continue;
                    }
                    AddMaterialTextures(material, textures, record);
                }
            }

            record.BoundsFinite = record.NonFiniteBoundsCount == 0;
            if (record.MissingMaterialCount > 0)
            {
                errors.Add("material is missing on " + record.MissingMaterialCount + " slot(s)");
            }
            if (record.MissingMeshCount > 0)
            {
                errors.Add("mesh is missing on " + record.MissingMeshCount + " renderer(s)");
            }
            if (record.NonFiniteBoundsCount > 0)
            {
                errors.Add("renderer bounds are non-finite on " + record.NonFiniteBoundsCount +
                    " renderer(s)");
            }
            if (record.VertexCount > maxVertices)
            {
                errors.Add("vertex budget exceeded: " + record.VertexCount + "/" + maxVertices);
            }
            if (record.MaterialCount > maxMaterials)
            {
                errors.Add("material budget exceeded: " + record.MaterialCount + "/" + maxMaterials);
            }
            if (record.TexturePixelCount > maxTexturePixels)
            {
                errors.Add("texture pixel budget exceeded: " + record.TexturePixelCount + "/" +
                    maxTexturePixels);
            }
            return Finish(record, errors);
        }

        public static bool Validate(IEnumerable<WorldAssetBudgetRecord> records, out string summary)
        {
            if (records == null)
            {
                summary = "World asset budget failed: records are null.";
                return false;
            }

            int total = 0;
            int failed = 0;
            var failures = new List<string>();
            foreach (WorldAssetBudgetRecord record in records)
            {
                total++;
                if (record == null)
                {
                    failed++;
                    failures.Add("<null-record>");
                    continue;
                }
                if (record.Passed) continue;
                failed++;
                string label = string.IsNullOrEmpty(record.Name) ? "<unnamed>" : record.Name;
                string reason = string.IsNullOrEmpty(record.Error) ? "unspecified failure" : record.Error;
                failures.Add(label + ": " + reason);
            }

            if (total == 0)
            {
                summary = "World asset budget failed: no records were supplied.";
                return false;
            }
            if (failed == 0)
            {
                summary = "World asset budget passed (" + total + " records).";
                return true;
            }

            summary = "World asset budget failed (" + failed + "/" + total + "): " +
                string.Join(" | ", failures);
            return false;
        }

        static WorldAssetBudgetRecord Finish(WorldAssetBudgetRecord record, List<string> errors)
        {
            record.Error = string.Join("; ", errors);
            record.Passed = errors.Count == 0;
            return record;
        }

        static void ValidateLimit(int value, string label, List<string> errors)
        {
            if (value < 0) errors.Add(label + " budget must be non-negative");
        }

        static Mesh FindMesh(Renderer renderer)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter && filter.sharedMesh) return filter.sharedMesh;
            var skinned = renderer as SkinnedMeshRenderer;
            return skinned ? skinned.sharedMesh : null;
        }

        static void AddMaterialTextures(Material material, HashSet<Texture> textures,
            WorldAssetBudgetRecord record)
        {
            string[] properties = material.GetTexturePropertyNames();
            for (int index = 0; index < properties.Length; index++)
            {
                Texture texture = material.GetTexture(properties[index]);
                if (!texture || !textures.Add(texture)) continue;
                record.TextureCount++;
                record.MaxTextureWidth = Mathf.Max(record.MaxTextureWidth, texture.width);
                record.MaxTextureHeight = Mathf.Max(record.MaxTextureHeight, texture.height);
                record.TexturePixelCount += (long)texture.width * texture.height;
            }
        }

        static bool IsFinite(Bounds bounds)
        {
            return IsFinite(bounds.center) && IsFinite(bounds.extents);
        }

        static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
