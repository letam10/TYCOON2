using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class Stage7PoiAccessTests
    {
        // Giả định: catalog dùng tọa độ world khi world root ở gốc; vùng mở rộng thêm bán kính tương tác.
        // Tiêu chí đạt: dữ liệu ổn định, đủ nhóm POI và marker giữ đúng hợp đồng truy cập/collision.
        static readonly string[][] CoverageGroups =
        {
            new[] { "bank" },
            new[] { "restaurant", "dine" },
            new[] { "farm", "agri" },
            new[] { "industrial", "industry" },
            new[] { "station" },
            new[] { "rail-platform", "rail_platform", "railplatform", "platform" },
            new[] { "harbor", "port" },
            new[] { "marina" },
            new[] { "forest-reserve", "forest_reserve", "forestreserve" },
            new[] { "beach" },
            new[] { "highway-service", "highway_service", "highwayservice", "service" },
            new[] { "community" },
            new[] { "residential", "residence", "housing" }
        };

        GameObject world;

        [TearDown]
        public void TearDown()
        {
            if (world)
                Object.DestroyImmediate(world);
        }

        [Test]
        public void DefaultCatalogHasStableFiniteAccessDefinitions()
        {
            Assert.That(WorldPoiAccessCatalog.Validate(out string reason), Is.True, reason);
            IReadOnlyList<WorldPoiAccessDefinition> entries = WorldPoiAccessCatalog.BuildDefault();
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.Count, Is.GreaterThanOrEqualTo(CoverageGroups.Length));

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (WorldPoiAccessDefinition entry in entries)
            {
                Assert.That(ids.Add(entry.StableId), Is.True, "duplicate stable id");
                Assert.That(entry.StableId, Is.Not.Null.And.Not.Empty);
                Assert.That(entry.Label, Is.Not.Null.And.Not.Empty, entry.StableId);
                Assert.That(IsFinite(entry.Center), Is.True, entry.StableId);
                Assert.That(IsFinite(entry.Footprint), Is.True, entry.StableId);
                Assert.That(entry.Footprint.x, Is.GreaterThan(0f), entry.StableId);
                Assert.That(entry.Footprint.y, Is.GreaterThan(0f), entry.StableId);
                Assert.That(entry.Footprint.z, Is.GreaterThan(0f), entry.StableId);
                Assert.That(IsFinite(entry.ApproachAnchor), Is.True, entry.StableId);
                Assert.That(IsFinite(entry.ExitAnchor), Is.True, entry.StableId);
                Assert.That(Enum.IsDefined(typeof(WorldInteractionAction), entry.Action), Is.True, entry.StableId);
                Assert.That(entry.InteractionRadius, Is.GreaterThan(0f), entry.StableId);
                Assert.That(IsFinite(entry.InteractionRadius), Is.True, entry.StableId);
                Assert.That(IsInsideExpanded(entry.Center, entry.Footprint, entry.ApproachAnchor,
                    entry.InteractionRadius), Is.True, entry.StableId + " approach");
                Assert.That(IsInsideExpanded(entry.Center, entry.Footprint, entry.ExitAnchor,
                    entry.InteractionRadius), Is.True, entry.StableId + " exit");
            }

            foreach (string[] group in CoverageGroups)
                Assert.That(HasCoverage(entries, group), Is.True, string.Join("/", group));
        }

        [Test]
        public void BuilderCreatesStage7MarkersWithInteractionAndCollisionContracts()
        {
            world = new GameObject("Stage7PoiWorld");
            WorldPoiAccessBuilder.Build(world.transform);

            Transform accessRoot = world.transform.Find("Stage7PoiAccess");
            Assert.That(accessRoot, Is.Not.Null);
            WorldPoiAccessMarker[] markers = accessRoot.GetComponentsInChildren<WorldPoiAccessMarker>(true);
            IReadOnlyList<WorldPoiAccessDefinition> entries = WorldPoiAccessCatalog.BuildDefault();
            Assert.That(markers.Length, Is.EqualTo(entries.Count));

            var definitions = new Dictionary<string, WorldPoiAccessDefinition>(StringComparer.Ordinal);
            foreach (WorldPoiAccessDefinition entry in entries)
                definitions.Add(entry.StableId, entry);

            foreach (WorldPoiAccessMarker marker in markers)
            {
                Assert.That(marker, Is.Not.Null);
                Assert.That(marker.Definition.StableId, Is.Not.Null.And.Not.Empty);
                Assert.That(definitions.ContainsKey(marker.StableId), Is.True, marker.StableId);
                Assert.That(marker.StableId, Is.EqualTo(marker.Definition.StableId));
                Assert.That(marker.Interaction, Is.Not.Null, marker.StableId + " interaction");
                Assert.That(marker.Interaction.StableId, Is.EqualTo(marker.StableId));
                Assert.That(marker.Interaction.Action, Is.EqualTo(marker.Definition.Action));
                Assert.That(marker.Interaction.SaveKey,
                    Is.EqualTo("world.interaction." + marker.StableId));
                Assert.That(IsFinite(marker.Interaction.ApproachPoint), Is.True, marker.StableId + " approach");
                Assert.That(IsFinite(marker.Interaction.ExitPoint), Is.True, marker.StableId + " exit");

                Bounds boundary = marker.CollisionBoundary;
                Assert.That(IsFinite(boundary.center), Is.True, marker.StableId + " collision center");
                Assert.That(IsFinite(boundary.size), Is.True, marker.StableId + " collision size");
                Assert.That(boundary.size.x, Is.GreaterThan(0f), marker.StableId + " collision width");
                Assert.That(boundary.size.y, Is.GreaterThan(0f), marker.StableId + " collision height");
                Assert.That(boundary.size.z, Is.GreaterThan(0f), marker.StableId + " collision depth");
            }
        }

        [Test]
        public void CatalogIdsAndSaveKeysRemainStableAcrossBuilds()
        {
            IReadOnlyList<WorldPoiAccessDefinition> first = WorldPoiAccessCatalog.BuildDefault();
            IReadOnlyList<WorldPoiAccessDefinition> second = WorldPoiAccessCatalog.BuildDefault();
            Assert.That(second.Count, Is.EqualTo(first.Count));
            for (int index = 0; index < first.Count; index++)
            {
                Assert.That(second[index].StableId, Is.EqualTo(first[index].StableId));
                Assert.That("world.interaction." + second[index].StableId,
                    Is.EqualTo("world.interaction." + first[index].StableId));
            }
        }

        static bool HasCoverage(IReadOnlyList<WorldPoiAccessDefinition> entries, string[] tokens)
        {
            foreach (WorldPoiAccessDefinition entry in entries)
            {
                string text = (entry.StableId + " " + entry.Label + " " + entry.RouteId).ToLowerInvariant();
                foreach (string token in tokens)
                    if (text.Contains(token))
                        return true;
            }
            return false;
        }

        static bool IsInsideExpanded(Vector3 center, Vector3 footprint, Vector3 point, float radius)
        {
            Vector3 half = footprint * .5f + Vector3.one * radius;
            Vector3 delta = point - center;
            return Mathf.Abs(delta.x) <= half.x
                && Mathf.Abs(delta.y) <= half.y
                && Mathf.Abs(delta.z) <= half.z;
        }

        static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        static bool IsFinite(Bounds value)
        {
            return IsFinite(value.center) && IsFinite(value.size);
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
