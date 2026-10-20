using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityWaterEnvironmentContractTests
    {
        [Test]
        public void WaterQueriesAreFiniteAndDeterministic()
        {
            var points = new[]
            {
                new Vector3(-24f, 7f, -64f),
                new Vector3(0f, -3f, 0f),
                new Vector3(247f, 1f, 77f),
                new Vector3(1000f, 4f, -1000f)
            };

            foreach (Vector3 point in points)
            {
                float firstHeight = CityWaterSurface.SampleWaterHeight(point);
                float secondHeight = CityWaterSurface.SampleWaterHeight(point);
                float firstBlend = CityWaterSurface.SampleShorelineBlend(point);
                float secondBlend = CityWaterSurface.SampleShorelineBlend(point);

                Assert.That(float.IsNaN(firstHeight) || float.IsInfinity(firstHeight), Is.False);
                Assert.That(firstHeight, Is.EqualTo(secondHeight));
                Assert.That(firstBlend, Is.InRange(0f, 1f));
                Assert.That(firstBlend, Is.EqualTo(secondBlend));
            }
        }

        [Test]
        public void FloatingSnapChangesOnlyVerticalPosition()
        {
            Vector3 source = new Vector3(12.5f, 99f, -8.75f);
            Vector3 snapped = CityWaterSurface.SnapFloatingObject(source);

            Assert.That(snapped.x, Is.EqualTo(source.x));
            Assert.That(snapped.z, Is.EqualTo(source.z));
            Assert.That(snapped.y, Is.EqualTo(CityWaterSurface.SampleWaterHeight(source)));
            Assert.That(snapped.y, Is.Not.EqualTo(source.y));
        }

        [Test]
        public void NonFiniteWaterInputsFailClosedToBaseline()
        {
            float height = CityWaterSurface.SampleWaterHeight(
                new Vector3(float.NaN, 3f, float.PositiveInfinity));

            Assert.That(height, Is.EqualTo(CityWaterSurface.WaterBaseline));
            Assert.That(CityWaterSurface.SampleShorelineBlend(
                new Vector3(float.NaN, 3f, 2f)), Is.EqualTo(0f));
        }

        [Test]
        public void RuntimeVegetationCatalogUsesTheSharedBiomeIds()
        {
            Assert.That(WorldVegetationCatalog.FarmBiomeId,
                Is.EqualTo(CityBiomeVegetation.FarmBiomeId));
            Assert.That(WorldVegetationCatalog.ForestBiomeId,
                Is.EqualTo(CityBiomeVegetation.ForestBiomeId));
            Assert.That(WorldVegetationCatalog.CoastalBiomeId,
                Is.EqualTo(CityBiomeVegetation.CoastalBiomeId));
        }

        [Test]
        public void CoastMeshUsesTheSharedWaterBaseline()
        {
            Mesh mesh = CityCoastWaterMesh.BeachWater(20f, 33f, 77f);
            try
            {
                Assert.That(mesh.vertices[0].y,
                    Is.EqualTo(CityWaterSurface.WaterBaseline).Within(.0001f));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void ProvenanceFailsClosedForMissingRequiredMetadata()
        {
            CityAssetProvenance provenance = CompleteProvenance();
            Assert.That(provenance.IsComplete(), Is.True);

            string[] requiredTextFields =
            {
                nameof(CityAssetProvenance.sourceUrl),
                nameof(CityAssetProvenance.sourceVersion),
                nameof(CityAssetProvenance.license),
                nameof(CityAssetProvenance.localAssetPath),
                nameof(CityAssetProvenance.importProfile),
                nameof(CityAssetProvenance.colliderPolicy),
                nameof(CityAssetProvenance.lodPolicy)
            };

            foreach (string fieldName in requiredTextFields)
            {
                CityAssetProvenance missing = CompleteProvenance();
                SetTextField(missing, fieldName, " ");
                Assert.That(missing.IsComplete(), Is.False, fieldName);
            }

            CityAssetProvenance invalidScale = CompleteProvenance();
            invalidScale.scale = new Vector3(1f, 0f, 1f);
            Assert.That(invalidScale.IsComplete(), Is.False);
        }

        static CityAssetProvenance CompleteProvenance()
        {
            return new CityAssetProvenance
            {
                sourceUrl = "https://example.invalid/asset",
                sourceVersion = "v1.2.3",
                license = "MIT",
                localAssetPath = "Assets/_Game/Art/Imported/example.fbx",
                importProfile = "PBR_Standard",
                scale = Vector3.one,
                pivot = Vector3.zero,
                colliderPolicy = "BoxCollider",
                lodPolicy = "LOD0_LOD1_CrossFade"
            };
        }

        static void SetTextField(CityAssetProvenance provenance, string fieldName, string value)
        {
            switch (fieldName)
            {
                case nameof(CityAssetProvenance.sourceUrl):
                    provenance.sourceUrl = value;
                    break;
                case nameof(CityAssetProvenance.sourceVersion):
                    provenance.sourceVersion = value;
                    break;
                case nameof(CityAssetProvenance.license):
                    provenance.license = value;
                    break;
                case nameof(CityAssetProvenance.localAssetPath):
                    provenance.localAssetPath = value;
                    break;
                case nameof(CityAssetProvenance.importProfile):
                    provenance.importProfile = value;
                    break;
                case nameof(CityAssetProvenance.colliderPolicy):
                    provenance.colliderPolicy = value;
                    break;
                case nameof(CityAssetProvenance.lodPolicy):
                    provenance.lodPolicy = value;
                    break;
            }
        }
    }
}
