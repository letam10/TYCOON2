using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class Phase5MapHudContractTests
    {
        static readonly string[] StablePointIds =
        {
            "tycoon", "park", "station", "beach", "harbor", "marina", "bank",
            "restaurant", "forest", "forest_reserve", "industrial_yard", "north_village",
            "east_marina", "south_highway", "residential", "apartment", "commercial"
        };

        [Test]
        public void StablePoiIdsAndTeleportSourceRemainUnchanged()
        {
            Assert.That(CityMapLocations.Points.Length, Is.EqualTo(StablePointIds.Length));
            var ids = new HashSet<string>();
            for (int index = 0; index < CityMapLocations.Points.Length; index++)
            {
                string id = CityMapLocations.Points[index].id;
                Assert.That(ids.Add(id), Is.True, "duplicate POI ID " + id);
                Assert.That(id, Is.EqualTo(StablePointIds[index]));
            }

            MethodInfo teleport = typeof(CityMapLocations).GetMethod("Teleport",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(teleport, Is.Not.Null);
            Assert.That(teleport.ReturnType, Is.EqualTo(typeof(bool)));
        }

        [Test]
        public void CityMapGraphicKeepsSourceCompatiblePoiAndPositionApi()
        {
            Assert.That(typeof(CityMapGraphic).GetMethod("PoiKind", new[] { typeof(string) }), Is.Not.Null);
            Assert.That(typeof(CityMapGraphic).GetMethod("PoiGlyph", new[] { typeof(string) }), Is.Not.Null);
            Assert.That(typeof(CityMapGraphic).GetMethod("PoiColor", new[] { typeof(string) }), Is.Not.Null);
            Assert.That(typeof(CityMapGraphic).GetMethod("Position", new[] { typeof(Vector3) }), Is.Not.Null);
            Assert.That(typeof(CityMapGraphic).GetMethod("Initialize",
                new[] { typeof(GameSession), typeof(bool) }), Is.Not.Null);
        }

        [Test]
        public void MapViewportStateIsPlainAndMapOwnsTheViewportHooks()
        {
            Assert.That(typeof(MapViewportState).IsClass, Is.True);
            Assert.That(typeof(UnityEngine.Object).IsAssignableFrom(typeof(MapViewportState)), Is.False);
            Assert.That(typeof(CityMapGraphic).GetProperty("Viewport"), Is.Not.Null);
            Assert.That(typeof(CityMapGraphic).GetProperty("SelectedPoiId"), Is.Not.Null);
            Assert.That(typeof(CityMapGraphic).GetMethod("ToggleKind", new[] { typeof(string) }), Is.Not.Null);
            Assert.That(typeof(CityMapGraphic).GetMethod("SelectPoi", new[] { typeof(string) }), Is.Not.Null);
            Assert.That(typeof(CityMapGraphic).GetMethod("SetTooltip", new[] { typeof(string) }), Is.Not.Null);
            Assert.That(typeof(CityMapGraphic).GetMethod("SetRouteHighlight", new[] { typeof(string) }), Is.Not.Null);
        }

        [Test]
        public void HudExposesDeterministicMapControlHooks()
        {
            Assert.That(typeof(GameHud).GetMethod("ZoomMap", new[] { typeof(float) }), Is.Not.Null);
            Assert.That(typeof(GameHud).GetMethod("PanMap", new[] { typeof(Vector2) }), Is.Not.Null);
            Assert.That(typeof(GameHud).GetMethod("RotateMap", new[] { typeof(float) }), Is.Not.Null);
            Assert.That(typeof(GameHud).GetMethod("ResetMapNorth"), Is.Not.Null);
            Assert.That(typeof(GameHud).GetMethod("FocusMapPlayer"), Is.Not.Null);
            Assert.That(typeof(GameHud).GetMethod("ToggleMapKind", new[] { typeof(string) }), Is.Not.Null);
            Assert.That(typeof(GameHud).GetMethod("SetMapTooltip", new[] { typeof(string) }), Is.Not.Null);
        }

        [Test]
        public void MapPlayerIndicatorRetainsInitializeCompatibility()
        {
            MethodInfo initialize = typeof(MapPlayerIndicator).GetMethod("Initialize",
                BindingFlags.Public | BindingFlags.Instance);
            Assert.That(initialize, Is.Not.Null);
            Assert.That(initialize.GetParameters()[0].ParameterType, Is.EqualTo(typeof(CityMapGraphic)));
            Assert.That(initialize.GetParameters()[1].ParameterType, Is.EqualTo(typeof(PlayerController)));
        }
    }
}
