using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class WorldExpansionCutawayTests
    {
        [Test]
        public void InteriorExpansionPoisRegisterCutawayShells()
        {
            GameCatalog previousCatalog = Art.Catalog;
            Art.Catalog = ScriptableObject.CreateInstance<GameCatalog>();
            var root = new GameObject("ExpansionCutawayWorld");
            try
            {
                Assert.DoesNotThrow(() => WorldExpansionDressing.Build(root.transform));
                Transform dressing = root.transform.Find("WorldExpansionDressing");
                Assert.That(dressing, Is.Not.Null);

                foreach (string id in new[] { "NorthVillage", "IndustrialYard", "EastMarina" })
                {
                    Transform poi = dressing.Find("Expansion_" + id);
                    Assert.That(poi, Is.Not.Null, id + " was not built");
                    WorldCutawayVolume volume = poi.GetComponentInChildren<WorldCutawayVolume>(true);
                    Assert.That(volume, Is.Not.Null, id + " has no cutaway volume");
                    Assert.That(volume.localFootprint.x, Is.GreaterThan(0f));
                    Assert.That(volume.localFootprint.z, Is.GreaterThan(0f));
                    Assert.That(volume.upperShellRenderers.Count, Is.GreaterThan(0),
                        id + " has no registered upper shell");
                }

                Transform forest = dressing.Find("Expansion_ForestReserve");
                Assert.That(forest.GetComponentInChildren<WorldCutawayVolume>(true), Is.Null);
            }
            finally
            {
                Art.Catalog = previousCatalog;
                Object.DestroyImmediate(root);
            }
        }
    }
}
