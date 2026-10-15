using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class Stage7MapWayfindingTests
    {
        static readonly string[] StablePointIds =
        {
            "tycoon", "park", "station", "beach", "harbor", "marina", "bank",
            "restaurant", "forest", "forest_reserve", "industrial_yard", "north_village",
            "east_marina", "south_highway"
        };

        static readonly Vector2Int[] CaptionResolutions =
        {
            new(1280, 720), new(1920, 1080), new(2560, 1440)
        };

        GameObject signalRoot;

        [TearDown]
        public void TearDown()
        {
            if (signalRoot)
                Object.DestroyImmediate(signalRoot);
        }

        [Test]
        public void MapPointsKeepStableCoverageAndFiniteCoordinates()
        {
            Assert.That(CityMapLocations.Points.Length, Is.EqualTo(StablePointIds.Length));
            var ids = new HashSet<string>();
            for (int index = 0; index < CityMapLocations.Points.Length; index++)
            {
                var point = CityMapLocations.Points[index];
                Assert.That(ids.Add(point.id), Is.True, "duplicate map point id " + point.id);
                Assert.That(point.id, Is.EqualTo(StablePointIds[index]));
                Assert.That(point.label, Is.Not.Empty, point.id);
                Assert.That(IsFinite(point.position), Is.True, point.id);
            }
        }

        [Test]
        public void MapPointIconsAreDistinctPerPoiKind()
        {
            var glyphs = new Dictionary<string, string>();
            var colors = new Dictionary<string, Color>();
            foreach (var point in CityMapLocations.Points)
            {
                string kind = CityMapGraphic.PoiKind(point.id);
                string glyph = CityMapGraphic.PoiGlyph(point.id);
                Color color = CityMapGraphic.PoiColor(point.id);
                Assert.That(kind, Is.Not.Empty, point.id);
                Assert.That(glyph, Is.Not.Empty, point.id);
                Assert.That(color.a, Is.GreaterThan(0f), point.id);
                if (glyphs.TryGetValue(kind, out string existingGlyph))
                    Assert.That(glyph, Is.EqualTo(existingGlyph), point.id);
                else
                    glyphs.Add(kind, glyph);
                if (colors.TryGetValue(kind, out Color existingColor))
                    Assert.That(color, Is.EqualTo(existingColor), point.id);
                else
                    colors.Add(kind, color);
            }

            Assert.That(glyphs.Count, Is.GreaterThanOrEqualTo(10));
            Assert.That(new HashSet<string>(glyphs.Values).Count, Is.EqualTo(glyphs.Count));
            Assert.That(new HashSet<Color>(colors.Values).Count, Is.EqualTo(colors.Count));
        }

        [Test]
        public void CaptionRectanglesRemainFiniteAtSupportedResolutions()
        {
            Bounds world = CityDistricts.WorldBounds;
            foreach (Vector2Int resolution in CaptionResolutions)
            {
                foreach (var point in CityMapLocations.Points)
                {
                    Vector2 normalized = new(
                        (point.position.x - world.min.x) / world.size.x,
                        (point.position.z - world.min.z) / world.size.z);
                    Vector2 pixel = Vector2.Scale(normalized, resolution);
                    Vector2 offset = CityMapLocations.CaptionOffset(point.id);
                    float width = CityMapLocations.CaptionWidth(point.id);
                    Rect rect = new(pixel.x + offset.x, pixel.y + offset.y - 9f, width, 18f);
                    Assert.That(IsFinite(rect), Is.True,
                        point.id + " at " + resolution.x + "x" + resolution.y);
                    Assert.That(rect.width, Is.GreaterThan(0f), point.id);
                    Assert.That(rect.height, Is.GreaterThan(0f), point.id);
                }
            }
        }

        [Test]
        public void RouteCatalogHasFiniteNodesWidthsAndPlacementReservations()
        {
            Assert.That(WorldRouteCatalog.ValidateContinuous(out string failure), Is.True, failure);
            IReadOnlyList<WorldRouteCorridor> routes = WorldRouteCatalog.BuildDefault();
            var routeIds = new HashSet<string>();
            bool hasReservation = false;
            foreach (WorldRouteCorridor route in routes)
            {
                Assert.That(routeIds.Add(route.Id), Is.True, route.Id);
                Assert.That(route.Nodes.Count, Is.GreaterThanOrEqualTo(2), route.Id);
                Assert.That(route.Width, Is.GreaterThan(0f), route.Id);
                Assert.That(IsFinite(route.Width), Is.True, route.Id);
                hasReservation |= route.ReservesPlacement;
                foreach (Vector3 node in route.Nodes)
                    Assert.That(IsFinite(node), Is.True, route.Id);
            }

            Assert.That(hasReservation, Is.True);
        }

        [Test]
        public void WayfindingSignsAndLabelsStayOffReservedRoutesAndFaceForward()
        {
            GameCatalog previousCatalog = Art.Catalog;
            GameCatalog testCatalog = ScriptableObject.CreateInstance<GameCatalog>();
            testCatalog.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Art.Catalog = testCatalog;
            try
            {
                foreach (WorldExpansionPoi poi in WorldExpansionCatalog.BuildDefault())
                {
                    GameObject root = new("Stage7Wayfinding_" + poi.Id);
                    root.transform.position = poi.Center;
                    try
                    {
                        WorldExpansionWayfinding.Build(root.transform, poi);
                        Transform wayfinding = root.transform.Find("Wayfinding");
                        Assert.That(wayfinding, Is.Not.Null, poi.Id);
                        Transform board = wayfinding.Find("GuideBoard");
                        Transform post = wayfinding.Find("GuidePost");
                        Assert.That(board, Is.Not.Null, poi.Id + " board");
                        Assert.That(post, Is.Not.Null, poi.Id + " post");
                        Assert.That(WorldRouteCatalog.IsReserved(board.position, 2f, out string route), Is.False,
                            poi.Id + " sign reserved by " + route);
                        Assert.That(post.position.y, Is.GreaterThan(root.transform.position.y));
                        TextMesh[] labels = wayfinding.GetComponentsInChildren<TextMesh>(true);
                        Assert.That(labels.Length, Is.EqualTo(2), poi.Id + " labels");
                        foreach (TextMesh label in labels)
                        {
                            Assert.That(label.transform.forward.sqrMagnitude, Is.GreaterThan(.000001f),
                                poi.Id + " label facing");
                            Assert.That(label.GetComponent<WorldLabelAnchor>(), Is.Not.Null,
                                poi.Id + " label anchor");
                        }
                    }
                    finally
                    {
                        Object.DestroyImmediate(root);
                    }
                }
            }
            finally
            {
                Art.Catalog = previousCatalog;
                Object.DestroyImmediate(testCatalog);
            }
        }

        [Test]
        public void SignalVisualHasPoleLampsAndPhaseSynchronizedState()
        {
            signalRoot = new GameObject("Stage7Signals");
            CityTrafficSignalSystem signals = CityTrafficSignalSystem.Build(signalRoot.transform);
            CityTrafficSignalVisual visual = signalRoot.GetComponentInChildren<CityTrafficSignalVisual>(true);
            Assert.That(signals, Is.Not.Null);
            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.LampCount, Is.EqualTo(CityTrafficSignalSystem.Definitions.Count * 3));
            Assert.That(signalRoot.GetComponentsInChildren<Light>(true).Length, Is.EqualTo(visual.LampCount));
            Assert.That(signalRoot.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThanOrEqualTo(visual.LampCount));
            MethodInfo update = typeof(CityTrafficSignalVisual).GetMethod(
                "Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(update, Is.Not.Null);
            update.Invoke(visual, null);

            for (int index = 0; index < CityTrafficSignalSystem.Definitions.Count; index++)
            {
                CityTrafficSignalDefinition definition = CityTrafficSignalSystem.Definitions[index];
                CitySignalPhase phase = signals.PhaseAt(definition.Position, false);
                Transform red = signalRoot.transform.Find("CityTrafficSignalVisuals/SignalLamp_" + index + "_Red");
                Transform amber = signalRoot.transform.Find("CityTrafficSignalVisuals/SignalLamp_" + index + "_Amber");
                Transform green = signalRoot.transform.Find("CityTrafficSignalVisuals/SignalLamp_" + index + "_Green");
                Assert.That(red, Is.Not.Null, definition.Id);
                Assert.That(amber, Is.Not.Null, definition.Id);
                Assert.That(green, Is.Not.Null, definition.Id);
                Assert.That(red.localPosition.y, Is.GreaterThan(amber.localPosition.y));
                Assert.That(amber.localPosition.y, Is.GreaterThan(green.localPosition.y));
                Assert.That(IsFinite(red.localPosition), Is.True, definition.Id);
                Assert.That(IsFinite(amber.localPosition), Is.True, definition.Id);
                Assert.That(IsFinite(green.localPosition), Is.True, definition.Id);
                Assert.That(red.GetComponent<Renderer>().enabled, Is.EqualTo(phase is CitySignalPhase.Red or CitySignalPhase.Walk));
                Assert.That(amber.GetComponent<Renderer>().enabled, Is.EqualTo(phase == CitySignalPhase.Amber));
                Assert.That(green.GetComponent<Renderer>().enabled, Is.EqualTo(phase == CitySignalPhase.Green));
            }
        }

        static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        static bool IsFinite(Rect value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.width) && IsFinite(value.height);
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
