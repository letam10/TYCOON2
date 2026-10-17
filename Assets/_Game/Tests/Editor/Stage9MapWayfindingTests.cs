using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class Stage9MapWayfindingTests
    {
        static readonly Vector2Int[] Resolutions =
        {
            new(1280, 720), new(1920, 1080), new(2560, 1440)
        };

        static readonly string[] RequiredIds =
        {
            "tycoon", "residential", "apartment", "commercial", "bank", "restaurant",
            "industrial_yard", "station", "forest", "beach", "harbor", "south_highway"
        };

        GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root)
                Object.DestroyImmediate(root);
            Art.Catalog = null;
        }

        [Test]
        public void MapLabelsStayInsideAndDoNotOverlapAtSupportedResolutions()
        {
            Bounds world = CityDistricts.WorldBounds;
            foreach (Vector2Int resolution in Resolutions)
            {
                var rectangles = new List<Rect>();
                foreach (var point in CityMapLocations.Points)
                {
                    Vector2 normalized = new(
                        (point.position.x - world.min.x) / world.size.x,
                        (point.position.z - world.min.z) / world.size.z);
                    Vector2 pixel = Vector2.Scale(normalized, resolution);
                    Vector2 offset = CityMapLocations.CaptionOffset(point.id);
                    Rect rectangle = new(pixel.x + offset.x, pixel.y + offset.y - 9f,
                        CityMapLocations.CaptionWidth(point.id), 18f);
                    Assert.That(rectangle.xMin, Is.GreaterThanOrEqualTo(-8f), point.id);
                    Assert.That(rectangle.yMin, Is.GreaterThanOrEqualTo(-8f), point.id);
                    Assert.That(rectangle.xMax, Is.LessThanOrEqualTo(resolution.x + 8f), point.id);
                    Assert.That(rectangle.yMax, Is.LessThanOrEqualTo(resolution.y + 8f), point.id);
                    rectangles.Add(rectangle);
                }

                for (int left = 0; left < rectangles.Count; left++)
                    for (int right = left + 1; right < rectangles.Count; right++)
                        Assert.That(rectangles[left].Overlaps(rectangles[right]), Is.False,
                            CityMapLocations.Points[left].id + " overlaps "
                            + CityMapLocations.Points[right].id + " at " + resolution);
            }
        }

        [Test]
        public void MapHasReadableIconsForEveryStage9Category()
        {
            foreach (string id in RequiredIds)
            {
                Assert.That(CityMapGraphic.PoiKind(id), Is.Not.EqualTo("poi"), id);
                Assert.That(CityMapGraphic.PoiGlyph(id), Is.Not.EqualTo("•"), id);
                Assert.That(CityMapGraphic.PoiColor(id).a, Is.GreaterThan(.9f), id);
            }
        }

        [Test]
        public void WayfindingBoardsFollowRouteAndFacePoi()
        {
            GameCatalog previous = Art.Catalog;
            GameCatalog catalog = ScriptableObject.CreateInstance<GameCatalog>();
            catalog.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Art.Catalog = catalog;
            try
            {
                foreach (WorldExpansionPoi poi in WorldExpansionCatalog.BuildDefault())
                {
                    root = new GameObject("Stage9Wayfinding_" + poi.Id);
                    root.transform.position = poi.Center;
                    WorldExpansionWayfinding.Build(root.transform, poi);
                    Transform board = root.transform.Find("Wayfinding/GuideBoard");
                    Assert.That(board, Is.Not.Null, poi.Id);
                    Assert.That(WorldRouteCatalog.IsReserved(board.position, 2f, out string route),
                        Is.False, poi.Id + " reserved by " + route);
                    Vector3 toPoi = poi.Center - board.position;
                    toPoi.y = 0;
                    Assert.That(Vector3.Dot(board.forward, toPoi.normalized), Is.GreaterThan(.45f), poi.Id);
                    Assert.That(board.GetComponent<Collider>(), Is.Null, poi.Id);
                    Object.DestroyImmediate(root);
                    root = null;
                }
            }
            finally
            {
                Art.Catalog = previous;
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void TrafficSignalsKeepVehiclePedestrianPhasesAndPhysicalSupports()
        {
            root = new GameObject("Stage9Signals");
            CityTrafficSignalSystem signals = CityTrafficSignalSystem.Build(root.transform);
            CityTrafficSignalVisual visual = root.GetComponentInChildren<CityTrafficSignalVisual>(true);
            Assert.That(visual.PoleCount, Is.EqualTo(CityTrafficSignalSystem.Definitions.Count));
            Assert.That(visual.ArmCount, Is.EqualTo(CityTrafficSignalSystem.Definitions.Count));
            Assert.That(root.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(0));
            for (int index = 0; index < CityTrafficSignalSystem.Definitions.Count; index++)
            {
                CityTrafficSignalDefinition definition = CityTrafficSignalSystem.Definitions[index];
                CitySignalPhase vehicle = signals.PhaseAt(definition.Position, false);
                CitySignalPhase pedestrian = signals.PhaseAt(definition.Position, true);
                if (vehicle == CitySignalPhase.Amber)
                    Assert.That(pedestrian, Is.EqualTo(CitySignalPhase.Red), definition.Id);
                else
                    Assert.That(pedestrian, Is.EqualTo(CitySignalPhase.Walk), definition.Id);
                Assert.That(root.transform.Find("CityTrafficSignalVisuals/SignalPole_"
                    + index), Is.Not.Null, definition.Id);
            }
            Assert.That(signals.DescribePhases(), Does.Contain("vehicle="));
            Assert.That(signals.DescribePhases(), Does.Contain("pedestrian="));
        }
    }
}
