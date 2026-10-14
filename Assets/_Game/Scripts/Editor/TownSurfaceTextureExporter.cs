using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tycoon.Editor
{
    public static class TownSurfaceTextureExporter
    {
        const string OutputRoot = "Assets/_Game/Art/Library/Textures/Generated";
        static readonly string[] Surfaces =
        {
            "grass", "soil", "sand", "asphalt", "stone", "wood", "brick", "roof"
        };

        static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        [MenuItem("TYCOON/Export Surface Texture Library")]
        public static void Export()
        {
            string absoluteRoot = Path.Combine(ProjectRoot, OutputRoot);
            Directory.CreateDirectory(absoluteRoot);
            for (int index = 0; index < Surfaces.Length; index++)
                ExportSurface(Surfaces[index], absoluteRoot);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("SURFACE_TEXTURE_EXPORT PASS " + Surfaces.Length + " surfaces");
        }

        static void ExportSurface(string surface, string absoluteRoot)
        {
            Texture2D source = TownSurfaceTexture.Get(surface);
            Texture2D copy = Readback(source);
            string absolutePath = Path.Combine(absoluteRoot, "TownDetail_" + surface + ".png");
            File.WriteAllBytes(absolutePath, copy.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(copy);

            string assetPath = OutputRoot + "/TownDetail_" + surface + ".png";
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (!importer)
                throw new InvalidOperationException("Texture import failed: " + assetPath);
            importer.sRGBTexture = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        static Texture2D Readback(Texture2D source)
        {
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, true, false);
            copy.SetPixels32(source.GetPixels32());
            copy.Apply(true, false);
            return copy;
        }
    }
}
