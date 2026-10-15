using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class Stage6VegetationEzTreeTests
    {
        const string PinnedCommit = "dcf309bd86bd521083d9c70f01f2de45fdc7c457";
        const int MaximumOutputBytes = 15 * 1024;

        static readonly string[] ProfileIds = { "farm", "forest", "coastal" };
        static readonly int[] ProfileSeeds = { 20261032, 20261058, 20261086 };
        static readonly float[] NearDistances = { 28f, 34f, 30f };
        static readonly float[] FarDistances = { 86f, 118f, 102f };

        static readonly Dictionary<string, string> OutputHashes =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["farm-tree.glb"] =
                    "BD05BE578EB0792C8E7A17478741164967DD23D09C404302B9E276F237ECE4DC",
                ["farm-tree-albedo.png"] =
                    "1ACB65C568BE7D118360D4543E48684CDF23BEAD5CB192D7DF03753AB9B054B8",
                ["forest-tree.glb"] =
                    "BD38FB63FDBB4797CA3D9701D6F0E729017A5738A32E3D3CB47ED23509A4F514",
                ["forest-tree-albedo.png"] =
                    "034726990A3233F0BADE71DEE4EFDE53211B08EC9C3AE5C671AE150FC2AE8BE6",
                ["coastal-tree.glb"] =
                    "2736DB84E8076D11808377A87A595B6B1EE623969E8F04FB233D2962A6F2AB02",
                ["coastal-tree-albedo.png"] =
                    "77ADD85933A6E63EDACE00DE2BBCA53AB6459EE8F01C51EF789660BDEDA026EB"
            };

        [Test]
        public void ProfilesKeepStableSeedsAndOrderedLodDistances()
        {
            Assert.That(WorldVegetationCatalog.Seed, Is.EqualTo(20261015));
            Assert.That(WorldVegetationCatalog.Profiles, Has.Count.EqualTo(ProfileIds.Length));

            for (int index = 0; index < ProfileIds.Length; index++)
            {
                WorldVegetationProfile profile = WorldVegetationCatalog.Profiles[index];
                Assert.That(profile.Id, Is.EqualTo(ProfileIds[index]));
                Assert.That(profile.Seed, Is.EqualTo(ProfileSeeds[index]));
                Assert.That(profile.NearLodDistance, Is.EqualTo(NearDistances[index]));
                Assert.That(profile.FarLodDistance, Is.EqualTo(FarDistances[index]));
                Assert.That(profile.NearLodDistance, Is.LessThan(profile.FarLodDistance));
                Assert.That(profile.TargetDensity, Is.InRange(0f, 1f));
                Assert.That(profile.MinimumSpacing, Is.GreaterThan(0f));
                Assert.That(profile.CorridorClearance, Is.GreaterThan(0f));
                Assert.That(profile.LodCount, Is.GreaterThanOrEqualTo(3));

                Vector3 sample = new Vector3(index * 13.25f, 0f, -index * 7.5f);
                Assert.That(
                    WorldVegetationCatalog.StableSpecies(profile.Id, sample),
                    Is.EqualTo(WorldVegetationCatalog.StableSpecies(profile.Id, sample)));
                Assert.That(
                    WorldVegetationCatalog.StableValue(profile.Id, sample),
                    Is.EqualTo(WorldVegetationCatalog.StableValue(profile.Id, sample)));
            }
        }

        [Test]
        public void EzTreeAllowlistMatchesCatalogAndHashesSmallOutputs()
        {
            string generatedRoot = Path.Combine(
                Application.dataPath,
                "_Game/Art/Generated/Vegetation");
            string presetPath = Path.Combine(generatedRoot, "ez-tree-presets.json");

            Assert.That(File.Exists(presetPath), Is.True, presetPath);
            PresetFile preset = JsonUtility.FromJson<PresetFile>(File.ReadAllText(presetPath));

            Assert.That(preset, Is.Not.Null);
            Assert.That(preset.generator, Is.EqualTo("ez-tree"));
            Assert.That(preset.pinnedCommit, Is.EqualTo(PinnedCommit));
            Assert.That(preset.seed, Is.EqualTo(WorldVegetationCatalog.Seed));
            Assert.That(preset.profiles, Has.Length.EqualTo(ProfileIds.Length));

            for (int index = 0; index < preset.profiles.Length; index++)
            {
                PresetProfile presetProfile = preset.profiles[index];
                WorldVegetationProfile catalogProfile = WorldVegetationCatalog.Get(presetProfile.id);

                Assert.That(catalogProfile, Is.Not.Null, presetProfile.id);
                Assert.That(presetProfile.seed, Is.EqualTo(catalogProfile.Seed));
                Assert.That(presetProfile.lod, Is.Not.Null);
                Assert.That(presetProfile.lod.Length, Is.EqualTo(catalogProfile.LodCount));
                Assert.That(presetProfile.lod[0], Is.EqualTo(0f));
                Assert.That(presetProfile.lod[1], Is.EqualTo(catalogProfile.NearLodDistance));
                Assert.That(presetProfile.lod[2], Is.EqualTo(catalogProfile.FarLodDistance));

                for (int lodIndex = 1; lodIndex < presetProfile.lod.Length; lodIndex++)
                    Assert.That(
                        presetProfile.lod[lodIndex],
                        Is.GreaterThan(presetProfile.lod[lodIndex - 1]),
                        presetProfile.id + " LOD " + lodIndex);

                Assert.That(presetProfile.outputs, Has.Length.EqualTo(2));
                for (int outputIndex = 0; outputIndex < presetProfile.outputs.Length; outputIndex++)
                {
                    string output = presetProfile.outputs[outputIndex];
                    Assert.That(OutputHashes.ContainsKey(output), Is.True, output);
                    string outputPath = Path.Combine(generatedRoot, output);

                    Assert.That(File.Exists(outputPath), Is.True, outputPath);
                    Assert.That(
                        new FileInfo(outputPath).Length,
                        Is.LessThanOrEqualTo(MaximumOutputBytes),
                        outputPath);
                    Assert.That(Sha256(outputPath), Is.EqualTo(OutputHashes[output]), outputPath);
                }
            }

            string licensePath = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                "Docs/ThirdParty/EZ_TREE_LICENSE.md");
            Assert.That(File.Exists(licensePath), Is.True, licensePath);
            string license = File.ReadAllText(licensePath);
            Assert.That(license, Does.Contain(PinnedCommit));
            Assert.That(license, Does.Contain("MIT"));
        }

        [Test]
        public void VegetationPlacementRejectsRoutesByProfileClearance()
        {
            IReadOnlyList<WorldRouteCorridor> routes = WorldRouteCatalog.BuildDefault();
            Vector3 safePoint = new Vector3(520f, 0f, 40f);

            for (int profileIndex = 0; profileIndex < WorldVegetationCatalog.Profiles.Count; profileIndex++)
            {
                WorldVegetationProfile profile = WorldVegetationCatalog.Profiles[profileIndex];

                for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
                {
                    WorldRouteCorridor route = routes[routeIndex];
                    Vector3 midpoint = Vector3.Lerp(route.Nodes[0], route.Nodes[1], .5f);

                    Assert.That(
                        WorldVegetationCatalog.IsInsideCorridor(
                            midpoint, routes, profile.CorridorClearance),
                        Is.True,
                        profile.Id + " must reject " + route.Id);
                    Assert.That(
                        WorldRouteCatalog.IsReserved(
                            midpoint, profile.MinimumSpacing, out string reservedRoute),
                        Is.True,
                        profile.Id + " reservation " + route.Id);
                    Assert.That(reservedRoute, Is.EqualTo(route.Id));
                }

                Assert.That(
                    WorldVegetationCatalog.IsInsideCorridor(
                        safePoint, routes, profile.CorridorClearance),
                    Is.False,
                    profile.Id + " safe point");
                Assert.That(
                    WorldRouteCatalog.IsReserved(
                        safePoint, profile.MinimumSpacing, out string safeRoute),
                    Is.False,
                    profile.Id + " safe reservation");
                Assert.That(safeRoute, Is.Null);
            }
        }

        static string Sha256(string path)
        {
            using (SHA256 hash = SHA256.Create())
            {
                byte[] bytes = hash.ComputeHash(File.ReadAllBytes(path));
                return BitConverter.ToString(bytes).Replace("-", string.Empty);
            }
        }

        [Serializable]
        sealed class PresetFile
        {
            public string generator;
            public string pinnedCommit;
            public int seed;
            public PresetProfile[] profiles;
        }

        [Serializable]
        sealed class PresetProfile
        {
            public string id;
            public int seed;
            public float[] lod;
            public string[] outputs;
        }
    }
}
