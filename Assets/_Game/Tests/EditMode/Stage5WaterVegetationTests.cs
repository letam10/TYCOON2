using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class Stage5WaterVegetationTests
    {
        [Test]
        public void BeachWaterHasLayeredDisplacementAndAFlushShoreEdge()
        {
            Mesh mesh = CityCoastWaterMesh.BeachWater(20, 33, 77);
            try
            {
                int columns = Mathf.CeilToInt(20 / 2f);
                int rows = Mathf.CeilToInt(33 / 2f);
                float minimum = mesh.vertices.Min(vertex => vertex.y);
                float maximum = mesh.vertices.Max(vertex => vertex.y);
                Assert.That(maximum - minimum, Is.GreaterThan(.01f));
                for (int row = 0; row <= rows; row++)
                    Assert.That(mesh.vertices[row * (columns + 1)].y,
                        Is.EqualTo(.0525f).Within(.0001f));
                Assert.That(CityCoastWaterMesh.LayeredDisplacement(2, 4),
                    Is.EqualTo(CityCoastWaterMesh.LayeredDisplacement(2, 4)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void VegetationProfilesAreDeterministicAndKeepCorridorClearance()
        {
            Assert.That(WorldVegetationCatalog.Profiles, Has.Count.EqualTo(3));
            foreach (WorldVegetationProfile profile in WorldVegetationCatalog.Profiles)
            {
                Assert.That(profile.FarLodDistance, Is.GreaterThan(profile.NearLodDistance));
                Assert.That(profile.LodCount, Is.GreaterThanOrEqualTo(3));
                Assert.That(profile.TargetDensity, Is.InRange(0, 1));
                Assert.That(WorldVegetationCatalog.StableSpecies(profile.Id, Vector3.one),
                    Is.EqualTo(WorldVegetationCatalog.StableSpecies(profile.Id, Vector3.one)));
            }

            var routes = WorldRouteCatalog.BuildDefault();
            Vector3 highway = routes.Single(route => route.Id == "highway-ring").Nodes[0];
            Assert.That(WorldVegetationCatalog.IsInsideCorridor(highway, routes, 0), Is.True);
            Assert.That(WorldVegetationCatalog.IsInsideCorridor(new Vector3(1000, 0, 1000), routes, 0),
                Is.False);
        }

        [Test]
        public void GeneratedVegetationAllowlistHasPinnedMetadataAndSmallOutputs()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath,
                "_Game/Art/Generated/Vegetation"));
            string presetPath = Path.Combine(root, "ez-tree-presets.json");
            Assert.That(File.Exists(presetPath), Is.True, presetPath);
            PresetFile preset = JsonUtility.FromJson<PresetFile>(File.ReadAllText(presetPath));
            Assert.That(preset.pinnedCommit,
                Is.EqualTo("dcf309bd86bd521083d9c70f01f2de45fdc7c457"));
            Assert.That(preset.profiles, Has.Length.EqualTo(3));

            foreach (PresetProfile profile in preset.profiles)
            {
                foreach (string output in profile.outputs)
                {
                    string outputPath = Path.Combine(root, output);
                    Assert.That(File.Exists(outputPath), Is.True, outputPath);
                    Assert.That(new FileInfo(outputPath).Length, Is.LessThan(15360));
                }
            }
        }

        [Serializable]
        sealed class PresetFile
        {
            public string pinnedCommit;
            public PresetProfile[] profiles;
        }

        [Serializable]
        sealed class PresetProfile
        {
            public string[] outputs;
        }
    }
}
