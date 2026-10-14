using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityMapCaptionLayoutTests
    {
        const float MapWidth = 800f;
        const float MapHeight = 544f;

        [Test]
        public void CentralMapCaptionsDoNotOverlapAtWorldMapScale()
        {
            string[] ids = { "tycoon", "park", "station", "restaurant", "forest" };
            for (int firstIndex = 0; firstIndex < ids.Length; firstIndex++)
            {
                Rect first = CaptionRect(ids[firstIndex]);
                for (int secondIndex = firstIndex + 1; secondIndex < ids.Length; secondIndex++)
                {
                    Rect second = CaptionRect(ids[secondIndex]);
                    Assert.That(first.Overlaps(second), Is.False,
                        ids[firstIndex] + " overlaps " + ids[secondIndex]);
                }
            }
        }

        [Test]
        public void EveryMapPointHasFiniteCaptionLayout()
        {
            foreach (var point in CityMapLocations.Points)
            {
                Vector2 offset = CityMapLocations.CaptionOffset(point.id);
                float width = CityMapLocations.CaptionWidth(point.id);
                Assert.That(float.IsNaN(offset.x) || float.IsInfinity(offset.x), Is.False, point.id);
                Assert.That(float.IsNaN(offset.y) || float.IsInfinity(offset.y), Is.False, point.id);
                Assert.That(width, Is.GreaterThan(0f), point.id);
            }
        }

        [Test]
        public void EveryMapPointHasStableTypedIcon()
        {
            foreach (var point in CityMapLocations.Points)
            {
                string kind = CityMapGraphic.PoiKind(point.id);
                string glyph = CityMapGraphic.PoiGlyph(point.id);
                Color color = CityMapGraphic.PoiColor(point.id);
                Assert.That(kind, Is.Not.Empty, point.id);
                Assert.That(glyph, Is.Not.Empty, point.id);
                Assert.That(CityMapGraphic.PoiKind(point.id), Is.EqualTo(kind), point.id);
                Assert.That(CityMapGraphic.PoiGlyph(point.id), Is.EqualTo(glyph), point.id);
                Assert.That(color.a, Is.GreaterThan(0f), point.id);
            }
        }

        [Test]
        public void ExpansionMapIconsUseDistinctAreaTypes()
        {
            Assert.That(CityMapGraphic.PoiKind("forest_reserve"), Is.EqualTo("forest"));
            Assert.That(CityMapGraphic.PoiKind("industrial_yard"), Is.EqualTo("industrial"));
            Assert.That(CityMapGraphic.PoiKind("south_highway"), Is.EqualTo("highway"));
            Assert.That(CityMapGraphic.PoiColor("forest_reserve"),
                Is.Not.EqualTo(CityMapGraphic.PoiColor("industrial_yard")));
        }

        static Rect CaptionRect(string id)
        {
            Vector3 position = FindPoint(id).position;
            Bounds bounds = CityDistricts.WorldBounds;
            Vector2 normalized = new(
                (position.x - bounds.min.x) / bounds.size.x,
                (position.z - bounds.min.z) / bounds.size.z);
            Vector2 pixel = Vector2.Scale(normalized, new Vector2(MapWidth, MapHeight));
            Vector2 offset = CityMapLocations.CaptionOffset(id);
            return new Rect(pixel.x + offset.x, pixel.y + offset.y - 9,
                CityMapLocations.CaptionWidth(id), 18);
        }

        static (string id, string label, Vector3 position) FindPoint(string id)
        {
            foreach (var point in CityMapLocations.Points)
                if (point.id == id) return point;
            Assert.Fail("Missing map point " + id);
            return default;
        }
    }
}
