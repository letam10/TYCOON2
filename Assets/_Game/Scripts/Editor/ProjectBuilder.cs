using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Tycoon.Editor
{
    public static class ProjectBuilder
    {
        const string ArtRoot = "Assets/_Game/Art";
        const string ScenePath = "Assets/_Game/Scenes/Tycoon.unity";
        static string Root => Directory.GetParent(Application.dataPath).FullName;

        [MenuItem("TYCOON/Prepare Assets and Scene")]
        public static void CreateScene()
        {
            ConfigureAssets();
            ConfigureProject();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("TYCOON2");
            root.AddComponent<GameSession>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("TYCOON2 scene and catalog created.");
        }

        [MenuItem("TYCOON/Build Windows")]
        public static void BuildWindows()
        {
            // Build baseline từ scene hiện có, không tạo lại scene hoặc cấu hình art.
            if (!File.Exists(ScenePath)) throw new FileNotFoundException("Không có scene TYCOON2", ScenePath);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = Path.Combine(Root, "Builds/Windows/TYCOON2.exe"),
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            var summary = new BuildAudit {
                result = report.summary.result.ToString(), bytes = report.summary.totalSize,
                seconds = report.summary.totalTime.TotalSeconds, errors = report.summary.totalErrors,
                warnings = report.summary.totalWarnings, builtUtc = DateTime.UtcNow.ToString("o")
            };
            File.WriteAllText(Path.Combine(Root, "QA/build-report.json"), JsonUtility.ToJson(summary, true));
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build Windows failed: " + report.summary.result);
            Debug.Log("Windows build succeeded: " + summary.bytes + " bytes.");
        }

        static void ConfigureProject()
        {
            PlayerSettings.companyName = "TamStudio";
            PlayerSettings.productName = "TYCOON2";
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.enableFrameTimingStats = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Standalone, ManagedStrippingLevel.Low);
            QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, false);
            QualitySettings.vSyncCount = 0; QualitySettings.shadowDistance = 45;
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            if (pipeline)
            {
                GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
                pipeline.msaaSampleCount = 2; pipeline.renderScale = 1;
                pipeline.shadowDistance = 45; pipeline.supportsHDR = true;
                EditorUtility.SetDirty(pipeline);
            }
            var stripping = GraphicsSettings.GetRenderPipelineSettings<URPShaderStrippingSetting>();
            if (stripping != null) stripping.stripUnusedPostProcessingVariants = false;
        }

        static void ConfigureAssets()
        {
            AssetDatabase.Refresh();
            var json = File.ReadAllText(Path.Combine(Root, "QA/asset-audit.json"));
            var audit = JsonUtility.FromJson<AssetAudit>("{\"entries\":" + json + "}");
            var entries = new List<ModelEntry>();
            var importAudit = new List<ModelImportAudit>();
            foreach (var asset in audit.entries)
            {
                string path = ArtRoot + "/Imported/Models/" + asset.key + ".fbx";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) throw new InvalidOperationException("Không tìm thấy FBX importer: " + path);
                importer.importAnimation = asset.bones > 0;
                importer.animationType = asset.bones > 0 ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.optimizeGameObjects = false;
                importer.importBlendShapes = false; importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.isReadable = true; importer.addCollider = false;
                foreach (var sourceMaterial in asset.materials)
                {
                    string safeName = new string(sourceMaterial.name.Select(x => char.IsLetterOrDigit(x) || x == '_' ? x : '_').ToArray());
                    string materialPath = ArtRoot + "/Materials/" + asset.key + "_" + safeName + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (!material)
                    {
                        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        AssetDatabase.CreateAsset(material, materialPath);
                    }
                    material.name = asset.key + "_" + safeName;
                    material.shader = Shader.Find("Universal Render Pipeline/Lit");
                    // Blender audit ghi màu linear; property Color của Unity nhận giá trị sRGB.
                    var baseColor = new Color(sourceMaterial.color[0], sourceMaterial.color[1], sourceMaterial.color[2], sourceMaterial.color[3]).gamma;
                    if (asset.key == "player" && sourceMaterial.name == "LightBlue") baseColor = Art.Hex("#3DAD92");
                    if (asset.key == "player" && sourceMaterial.name == "Purple") baseColor = Art.Hex("#345C59");
                    material.SetColor("_BaseColor", baseColor);
                    if (!string.IsNullOrEmpty(sourceMaterial.texture))
                    {
                        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(sourceMaterial.texture));
                        material.SetColor("_BaseColor", Color.white);
                    }
                    material.SetFloat("_Smoothness", sourceMaterial.name.ToLower().Contains("metal") ? .38f : .22f);
                    material.enableInstancing = true;
                    if (sourceMaterial.name == "BrandDecal")
                    {
                        string brandPath = ArtRoot + "/Imported/brand.png";
                        File.Copy(Path.Combine(Root, "ASSET/Generated/brand.png"), Path.Combine(Root, brandPath), true);
                        AssetDatabase.ImportAsset(brandPath);
                        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(brandPath));
                        material.SetColor("_BaseColor", Color.white);
                        material.SetFloat("_AlphaClip", 1); material.SetFloat("_Cutoff", .1f);
                        material.EnableKeyword("_ALPHATEST_ON"); material.renderQueue = (int)RenderQueue.AlphaTest;
                    }
                    EditorUtility.SetDirty(material);
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceMaterial.name), material);
                }
                importer.SaveAndReimport();
                if (asset.bones > 0)
                {
                    var clips = importer.defaultClipAnimations;
                    foreach (var clip in clips)
                    {
                        string shortName = clip.name.Split('|').Last();
                        clip.name = shortName;
                        clip.loopTime = shortName is "Idle" or "Walk" or "WalkSlow" or "Run" or "Carry" or "CarryWalk" or "Sit";
                        clip.loopPose = clip.loopTime; clip.lockRootRotation = true;
                        clip.lockRootHeightY = true; clip.lockRootPositionXZ = true;
                    }
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                }
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var wrapper = new GameObject(asset.key);
                var visual = PrefabUtility.InstantiatePrefab(model) as GameObject;
                visual.transform.SetParent(wrapper.transform, false);
                var animator = visual.GetComponentInChildren<Animator>();
                var animationClips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(x => !x.name.StartsWith("__")).ToArray();
                if (asset.bones > 0)
                {
                    if (!animator) throw new Exception("Missing Animator: " + asset.key);
                    animator.applyRootMotion = false;
                    animator.runtimeAnimatorController = Controller(asset.key, animationClips);
                }
                var renderers = wrapper.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) throw new Exception("No renderer: " + asset.key);
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                    foreach (var material in renderer.sharedMaterials)
                        if (!material || !material.shader || material.shader.name.Contains("Error")) throw new Exception("Invalid material: " + asset.key);
                }
                string prefabPath = ArtRoot + "/Imported/Models/" + asset.key + ".prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);
                entries.Add(new ModelEntry(asset.key, prefab){size=bounds.size});
                importAudit.Add(new ModelImportAudit {
                    key = asset.key, clips = animationClips.Select(x => x.name).ToArray(),
                    bounds = new[] { bounds.size.x, bounds.size.y, bounds.size.z },
                    renderers = renderers.Length, materialSlots = renderers.Sum(x => x.sharedMaterials.Length),
                    validAvatar = animator != null && animator.avatar != null && animator.avatar.isValid
                });
                UnityEngine.Object.DestroyImmediate(wrapper);
            }
            string catalogPath = "Assets/_Game/Resources/GameCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(catalogPath);
            if (!catalog)
            {
                catalog = ScriptableObject.CreateInstance<GameCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }
            catalog.models = entries.ToArray();
            catalog.font = AssetDatabase.LoadAssetAtPath<Font>(ArtRoot + "/Imported/NotoSans-Regular.ttf");
            catalog.brandTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtRoot + "/Imported/brand.png");
            string profilePath = ArtRoot + "/Materials/GameLighting.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (!profile)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
                var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.Neutral);
                tone.name = "Tonemapping"; AssetDatabase.AddObjectToAsset(tone, profile);
                var grade = profile.Add<ColorAdjustments>(true);
                grade.saturation.Override(-4); grade.contrast.Override(5);
                grade.name = "ColorAdjustments"; AssetDatabase.AddObjectToAsset(grade, profile);
            }
            catalog.lightingProfile = profile;
            if (!catalog.font || !catalog.brandTexture) throw new Exception("Missing UI assets.");
            EditorUtility.SetDirty(catalog);
            File.WriteAllText(Path.Combine(Root, "QA/unity-import-audit.json"), JsonUtility.ToJson(new ModelImportReport { models = importAudit.ToArray() }, true));
        }

        static AnimatorController Controller(string key, AnimationClip[] clips)
        {
            string path = ArtRoot + "/Imported/Models/" + key + ".controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            var controller = existing ? existing : AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            foreach (var state in machine.states) machine.RemoveState(state.state);
            foreach (var clip in clips)
            {
                string name = clip.name.Split('|').Last();
                var state = machine.AddState(name); state.motion = clip;
                if (name == "Idle") machine.defaultState = state;
            }
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(machine);
            return controller;
        }
        [Serializable] sealed class AssetAudit { public AssetEntry[] entries; }
        [Serializable] sealed class AssetEntry { public string key; public int bones; public MaterialEntry[] materials; }
        [Serializable] sealed class MaterialEntry { public string name, texture; public float[] color; }
        [Serializable] sealed class ModelImportReport { public ModelImportAudit[] models; }
        [Serializable] sealed class ModelImportAudit { public string key; public string[] clips; public float[] bounds; public int renderers; public int materialSlots; public bool validAvatar; }
        [Serializable] sealed class BuildAudit { public string result, builtUtc; public ulong bytes; public double seconds; public int errors, warnings; }
    }
}
