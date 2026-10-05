using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Tycoon.Editor
{
    public static class AssetLibraryBuilder
    {
        const string ArtRoot = "Assets/_Game/Art/Library";
        static string Root => Directory.GetParent(Application.dataPath).FullName;
        [Serializable] public sealed class LibraryReport { public LibraryEntry[] entries; }
        [Serializable] public sealed class LibraryEntry
        {
            public string key, source, source_sha256, license, fingerprint, fbx, fbx_sha256;
            public int triangles;
            public float[] size;
            public LibraryMaterial[] materials;
        }
        [Serializable] public sealed class LibraryMaterial { public string name, texture; public float[] color; }
        [Serializable] sealed class ImportReport { public int models, triangles, renderers, materialSlots; public string[] keys; }
        static LibraryEntry[] Read() => JsonUtility.FromJson<LibraryReport>(File.ReadAllText(Path.Combine(Root, "Docs/asset-library-import.json"))).entries;

        [MenuItem("TYCOON/Import Local Asset Library")]
        public static void Import()
        {
            var entries = Read();
            Directory.CreateDirectory(Path.Combine(Root, ArtRoot, "Materials"));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var texture in entries.SelectMany(e => e.materials).Select(m => m.texture).Where(x => !string.IsNullOrEmpty(x)).Distinct())
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(texture);
                if (!importer.sRGBTexture || importer.mipmapEnabled || importer.filterMode != FilterMode.Point || importer.maxTextureSize != 1024)
                {
                    importer.sRGBTexture = true; importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Point; importer.maxTextureSize = 1024;
                    importer.SaveAndReimport();
                }
            }
            foreach (var entry in entries)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(entry.fbx);
                bool changed = importer.importAnimation || importer.addCollider || importer.isReadable || importer.importCameras || importer.importLights;
                importer.importAnimation = false; importer.animationType = ModelImporterAnimationType.None;
                importer.addCollider = false; importer.isReadable = false;
                importer.importCameras = false; importer.importLights = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                foreach(var obsolete in importer.GetExternalObjectMap().Keys.Where(id=>id.type==typeof(Material)&&!entry.materials.Any(m=>m.name==id.name)).ToArray())
                {importer.RemoveRemap(obsolete);changed=true;}
                foreach (var source in entry.materials)
                {
                    var material = Material(source,entry.key);
                    var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name);
                    if (!importer.GetExternalObjectMap().TryGetValue(id, out var mapped) || mapped != material)
                    { importer.AddRemap(id, material); changed = true; }
                }
                if (changed) importer.SaveAndReimport();
                string prefabPath = ArtRoot + "/Models/" + entry.key + ".prefab";
                // Giữ GUID và prefab đã đúng ở lần chạy lại, không tạo bản sao hoặc chỉnh nguồn.
                if (!AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) || changed)
                {
                    var wrapper = new GameObject(entry.key);
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(entry.fbx));
                    model.transform.SetParent(wrapper.transform, false);
                    PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);
                    UnityEngine.Object.DestroyImmediate(wrapper);
                }
            }
            ApplyCatalog(AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/_Game/Resources/GameCatalog.asset"));
            var used=new HashSet<string>(AssetDatabase.GetDependencies(entries.Select(e=>e.fbx).ToArray(),true),StringComparer.Ordinal);
            foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{ArtRoot+"/Materials"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                if(!used.Contains(path))AssetDatabase.DeleteAsset(path);
            }
            var report = new ImportReport { models = entries.Length, keys = entries.Select(e => e.key).ToArray() };
            foreach (var entry in entries)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArtRoot + "/Models/" + entry.key + ".prefab");
                if (prefab.GetComponentsInChildren<Collider>(true).Length != 0) throw new Exception("Visual có collider ngoài footprint: " + entry.key);
                var instance = UnityEngine.Object.Instantiate(prefab);
                var renderers = instance.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) throw new Exception("Model không có renderer: " + entry.key);
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                    foreach (var material in renderer.sharedMaterials)
                        if (!material || material.shader.name != "Universal Render Pipeline/Lit") throw new Exception("Material URP không hợp lệ: " + entry.key);
                    report.materialSlots += renderer.sharedMaterials.Length;
                }
                var expected = new Vector3(entry.size[0], entry.size[1], entry.size[2]);
                if ((bounds.size - expected).sqrMagnitude > .0001f || Mathf.Abs(bounds.min.y) > .005f)
                    throw new Exception("Bounds/pivot sai: " + entry.key + " " + bounds);
                report.renderers += renderers.Length; report.triangles += entry.triangles;
                UnityEngine.Object.DestroyImmediate(instance);
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Path.Combine(Root, "work/art-refresh"));
            File.WriteAllText(Path.Combine(Root, "work/art-refresh/unity-import.json"), JsonUtility.ToJson(report, true));
            Debug.Log("ASSET_LIBRARY_IMPORT PASS " + report.models + " models, " + report.triangles + " triangles.");
        }

        static Material Material(LibraryMaterial source,string modelKey)
        {
            string tint=modelKey.StartsWith("tree_")||modelKey is "garden_bush" or "crop_leaves"?"#D8F070":
                modelKey=="crop_soil"?"#9F6834":modelKey=="wheat_crop"?"#DDB352":modelKey=="farm_fence"?"#CDB386":"#FFFFFF";
            var color = new Color(source.color[0], source.color[1], source.color[2], source.color[3]).gamma*Art.Hex(tint);
            var texture = string.IsNullOrEmpty(source.texture) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(source.texture);
            if(modelKey=="garden_rock"){color=Art.Hex("#899987");texture=null;}
            string signature = color.r.ToString("R",CultureInfo.InvariantCulture)+","+color.g.ToString("R",CultureInfo.InvariantCulture)+","+color.b.ToString("R",CultureInfo.InvariantCulture)+"|"+(texture?source.texture:"");
            using var hash = SHA256.Create();
            string key = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(signature))).Replace("-", "").Substring(0, 16);
            string path = ArtRoot + "/Materials/Palette_" + key + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.GetColor("_BaseColor") != color || material.GetTexture("_BaseMap") != texture || !material.enableInstancing)
            {
                material.SetColor("_BaseColor", color); material.SetTexture("_BaseMap", texture);
                material.SetFloat("_Smoothness", .18f); material.enableInstancing = true;
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        public static void ApplyCatalog(GameCatalog catalog)
        {
            var entries = Read();
            var models = catalog.models.ToDictionary(x => x.key, StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArtRoot + "/Models/" + entry.key + ".prefab");
                if (!prefab) throw new Exception("Chạy Import Local Asset Library trước: " + entry.key);
                models[entry.key] = new ModelEntry(entry.key, prefab) { size = new Vector3(entry.size[0], entry.size[1], entry.size[2]) };
            }
            var merged = models.Values.OrderBy(x => x.key, StringComparer.Ordinal).ToArray();
            string[] keys = entries.Select(x => x.key).ToArray();
            if (catalog.models.Length != merged.Length || !catalog.models.Select(x => (x.key, x.prefab, x.size)).SequenceEqual(merged.Select(x => (x.key, x.prefab, x.size))) ||
                catalog.libraryKeys == null || !catalog.libraryKeys.SequenceEqual(keys))
            {
                catalog.models = merged; catalog.libraryKeys = keys;
                EditorUtility.SetDirty(catalog);
            }
        }
    }
}
