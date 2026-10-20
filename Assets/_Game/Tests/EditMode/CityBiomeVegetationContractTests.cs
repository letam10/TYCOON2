using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityBiomeVegetationContractTests
    {
        [Test]
        public void AllowedBiomeIdsAreFixed()
        {
            Assert.That(CityBiomeVegetation.IsAllowedBiome(CityBiomeVegetation.ForestBiomeId), Is.True);
            Assert.That(CityBiomeVegetation.IsAllowedBiome(CityBiomeVegetation.FarmBiomeId), Is.True);
            Assert.That(CityBiomeVegetation.IsAllowedBiome(CityBiomeVegetation.CoastalBiomeId), Is.True);
            Assert.That(CityBiomeVegetation.IsAllowedBiome("forest "), Is.False);
            Assert.That(CityBiomeVegetation.IsAllowedBiome("town"), Is.False);
            Assert.That(CityBiomeVegetation.IsAllowedBiome(null), Is.False);
        }

        [Test]
        public void GenerationIsDeterministicAndUsesStableSourceIndices()
        {
            IReadOnlyList<Vector3> samples = new[]
            {
                new Vector3(-4f, 0f, 2f),
                new Vector3(8f, 0f, 3f),
                new Vector3(14f, 0f, -5f)
            };

            IReadOnlyList<CityBiomeVegetation.Placement> first =
                CityBiomeVegetation.Generate(CityBiomeVegetation.ForestBiomeId, 73, samples, 1f);
            IReadOnlyList<CityBiomeVegetation.Placement> second =
                CityBiomeVegetation.Generate(CityBiomeVegetation.ForestBiomeId, 73, samples, 1f);

            Assert.That(first, Has.Count.EqualTo(samples.Count));
            Assert.That(second, Has.Count.EqualTo(first.Count));
            for (int index = 0; index < first.Count; index++)
            {
                Assert.That(second[index].BiomeId, Is.EqualTo(first[index].BiomeId));
                Assert.That(second[index].Index, Is.EqualTo(first[index].Index));
                Assert.That(second[index].Position, Is.EqualTo(first[index].Position));
                Assert.That(second[index].Yaw, Is.EqualTo(first[index].Yaw));
                Assert.That(second[index].Scale, Is.EqualTo(first[index].Scale));
                Assert.That(second[index].Category, Is.EqualTo(first[index].Category));
                Assert.That(first[index].Scale, Is.GreaterThan(0f));
                Assert.That(first[index].Yaw, Is.InRange(0f, 360f));
            }
        }

        [Test]
        public void ClearanceSkipsOverlappingSamplesInTheHorizontalPlane()
        {
            IReadOnlyList<Vector3> samples = new[]
            {
                new Vector3(0f, 100f, 0f),
                new Vector3(1f, -100f, 0f),
                new Vector3(4f, 0f, 0f)
            };

            IReadOnlyList<CityBiomeVegetation.Placement> placements =
                CityBiomeVegetation.Generate(CityBiomeVegetation.FarmBiomeId, 11, samples, 2f);

            Assert.That(placements, Has.Count.EqualTo(2));
            Assert.That(placements[0].Index, Is.EqualTo(0));
            Assert.That(placements[1].Index, Is.EqualTo(2));
            Assert.That(Vector2.Distance(
                    new Vector2(placements[0].Position.x, placements[0].Position.z),
                    new Vector2(placements[1].Position.x, placements[1].Position.z)),
                Is.GreaterThanOrEqualTo(2f));
        }

        [Test]
        public void InvalidInputProducesNoPlacementAndDoesNotCreateUnityObjects()
        {
            IReadOnlyList<Vector3> samples = new[]
            {
                new Vector3(float.NaN, 0f, 0f),
                new Vector3(float.PositiveInfinity, 0f, 0f)
            };

            Assert.That(
                CityBiomeVegetation.Generate("unknown", 1, samples, 1f),
                Is.Empty);
            Assert.That(
                CityBiomeVegetation.Generate(CityBiomeVegetation.CoastalBiomeId, 1, samples, 1f),
                Is.Empty);
            Assert.That(
                CityBiomeVegetation.Generate(CityBiomeVegetation.CoastalBiomeId, 1, null, 1f),
                Is.Empty);
        }
    }
}
