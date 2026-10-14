using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Tycoon.Editor
{
    static class ModelImportPresentationSettings
    {
        public const float SmoothingAngle = 60f;

        static readonly PropertyInfo SmoothingProperty = typeof(ModelImporter).GetProperty(
            "normalSmoothingAngle",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        static readonly FieldInfo SmoothingField = typeof(ModelImporter).GetField(
            "normalSmoothingAngle",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static bool IsTargetModelPath(string path)
        {
            return path.StartsWith("Assets/_Game/Art/Library/Models/", System.StringComparison.Ordinal)
                || path.StartsWith("Assets/_Game/Art/Imported/Models/", System.StringComparison.Ordinal);
        }

        public static void Configure(ModelImporter importer)
        {
            if (!importer || !IsTargetModelPath(importer.assetPath))
                return;

            importer.importNormals = ModelImporterNormals.Calculate;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.weldVertices = true;
            SetSmoothingAngle(importer, SmoothingAngle);
        }

        static void SetSmoothingAngle(ModelImporter importer, float angle)
        {
            if (SmoothingProperty != null && SmoothingProperty.CanWrite)
            {
                SmoothingProperty.SetValue(importer, angle);
                return;
            }
            SmoothingField?.SetValue(importer, angle);
        }
    }

    sealed class ModelImportPresentationPostprocessor : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            ModelImportPresentationSettings.Configure((ModelImporter)assetImporter);
        }
    }
}
