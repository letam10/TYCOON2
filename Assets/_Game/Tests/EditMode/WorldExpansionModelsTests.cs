using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class WorldExpansionModelsTests
    {
        [Test]
        public void BuildIgnoresNullRoot()
        {
            WorldExpansionPoi poi = WorldExpansionCatalog.BuildDefault()[0];
            Assert.DoesNotThrow(() => WorldExpansionModels.Build(null, poi));
        }

        [Test]
        public void BuildCreatesOneModelRootPerPoi()
        {
            foreach (WorldExpansionPoi poi in WorldExpansionCatalog.BuildDefault())
            {
                GameObject root = new GameObject("ExpansionModelTest_" + poi.Id);
                root.transform.localPosition = poi.Center;
                try
                {
                    Assert.DoesNotThrow(() => WorldExpansionModels.Build(root.transform, poi));
                    Transform models = root.transform.Find("ExpansionModels");
                    Assert.That(models, Is.Not.Null, poi.Id + " did not create model root");
                    Assert.That(models.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThan(0),
                        poi.Id + " has no rendered geometry");
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void BuildIsIdempotentForExistingModelRoot()
        {
            WorldExpansionPoi poi = WorldExpansionCatalog.BuildDefault()[2];
            GameObject root = new GameObject("ExpansionModelIdempotenceTest");
            root.transform.localPosition = poi.Center;
            try
            {
                WorldExpansionModels.Build(root.transform, poi);
                WorldExpansionModels.Build(root.transform, poi);
                Assert.That(root.transform.childCount, Is.EqualTo(1));
                Assert.That(root.transform.Find("ExpansionModels"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void VillageRoofsUseSeparateRoofMaterialBatch()
        {
            WorldExpansionPoi poi = WorldExpansionCatalog.BuildDefault()[2];
            GameObject root = new GameObject("ExpansionRoofBatchTest");
            root.transform.localPosition = poi.Center;
            try
            {
                WorldExpansionModels.Build(root.transform, poi);
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                bool foundRoof = false;
                for (int index = 0; index < renderers.Length; index++)
                {
                    Material material = renderers[index].sharedMaterial;
                    if (material && material.name.ToLowerInvariant().Contains("roof"))
                    {
                        foundRoof = true;
                        break;
                    }
                }
                Assert.That(foundRoof, Is.True, "Village roof was not batched with roof material");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void WayfindingBuildCreatesOneSignSetPerPoi()
        {
            foreach (WorldExpansionPoi poi in WorldExpansionCatalog.BuildDefault())
            {
                GameObject root = new GameObject("ExpansionWayfindingTest_" + poi.Id);
                root.transform.localPosition = poi.Center;
                try
                {
                    WorldExpansionWayfinding.Build(root.transform, poi);
                    WorldExpansionWayfinding.Build(root.transform, poi);
                    Transform signs = root.transform.Find("Wayfinding");
                    Assert.That(signs, Is.Not.Null, poi.Id + " did not create wayfinding root");
                    Assert.That(signs.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThanOrEqualTo(3),
                        poi.Id + " has no complete sign set");
                    Assert.That(root.transform.childCount, Is.EqualTo(1),
                        poi.Id + " created duplicate wayfinding roots");
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void MarinaCreatesBoardablePassengerBoat()
        {
            WorldExpansionPoi poi = WorldExpansionCatalog.BuildDefault()[3];
            GameObject root = new GameObject("ExpansionBoatTest");
            root.transform.localPosition = poi.Center;
            try
            {
                WorldExpansionModels.Build(root.transform, poi);
                WorldExpansionBoat boat = root.GetComponentInChildren<WorldExpansionBoat>(true);
                Assert.That(boat, Is.Not.Null);
                Assert.That(boat.Hull, Is.EqualTo(boat.transform));
                Assert.That(boat.DoorsOpen, Is.True);
                Assert.That(boat.DoorOutside, Is.EqualTo(boat.transform.TransformPoint(
                    new Vector3(0, .2f, -5.2f))));
                Assert.That(float.IsNaN(boat.DoorOutside.x), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
