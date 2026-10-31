using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityMapPhotographyTests
    {
        [Test]
        public void TilesCoverWorldWithSharedEdgesAndNorthUpPixels()
        {
            Bounds world = CityDistricts.WorldBounds;
            var grid = new CityMapCaptureGrid(world, 4, 3, 1024);
            double area = 0;
            for (int index = 0; index < grid.TileCount; index++)
            {
                Bounds tile = grid.TileBounds(index);
                area += tile.size.x * (double)tile.size.z;
                RectInt pixels = grid.TilePixels(index);
                Rect uv = grid.TileUv(index);
                Assert.That(uv.xMin * grid.AtlasWidth, Is.EqualTo(pixels.xMin).Within(.001));
                Assert.That(uv.yMin * grid.AtlasHeight, Is.EqualTo(pixels.yMin).Within(.001));
                Assert.That(uv.width * grid.AtlasWidth, Is.EqualTo(pixels.width).Within(.001));
                if (index % grid.Columns != grid.Columns - 1)
                    Assert.That(tile.max.x, Is.EqualTo(grid.TileBounds(index + 1).min.x).Within(.001));
                if (index + grid.Columns < grid.TileCount)
                {
                    Assert.That(tile.max.z,
                        Is.EqualTo(grid.TileBounds(index + grid.Columns).min.z).Within(.001));
                    Assert.That(pixels.yMax, Is.EqualTo(grid.TilePixels(index + grid.Columns).yMin));
                }
            }
            Assert.That(area, Is.EqualTo(world.size.x * (double)world.size.z).Within(.1));
            Assert.That(grid.WorldToUv(world.min), Is.EqualTo(Vector2.zero));
            Assert.That(grid.WorldToUv(world.max), Is.EqualTo(Vector2.one));
            Vector3 point = new Vector3(-57.2f, world.center.y, 103.4f);
            Assert.That(Vector3.Distance(grid.UvToWorld(grid.WorldToUv(point)), point), Is.LessThan(.001f));
        }

        [TestCase(0f)]
        [TestCase(37f)]
        [TestCase(90f)]
        [TestCase(-145f)]
        public void MapProjectionRoundTripsAndDraggingMovesTheImageByTheRequestedFraction(float angle)
        {
            var sessionRoot = new GameObject("MapPhotoTestSession");
            sessionRoot.SetActive(false);
            var mapRoot = new GameObject("MapPhotoTest", typeof(RectTransform), typeof(CanvasRenderer));
            mapRoot.SetActive(false);
            try
            {
                var session = sessionRoot.AddComponent<GameSession>();
                var map = mapRoot.AddComponent<CityMapGraphic>();
                map.Initialize(session, false);
                map.ZoomBy(2f);
                map.RotateBy(angle);
                var point = new Vector3(50.3f, map.ViewBounds.center.y, 81.9f);
                Assert.That(Vector3.Distance(map.WorldAt(map.Position(point)), point), Is.LessThan(.001f));
                Vector2 before = map.Position(point);
                Vector2 drag = new Vector2(.1f, -.07f);
                map.Pan(drag);
                Vector2 after = map.Position(point);
                Assert.That(Vector2.Distance(after, before - drag), Is.LessThan(.00001f));
            }
            finally
            {
                Object.DestroyImmediate(mapRoot);
                Object.DestroyImmediate(sessionRoot);
            }
        }
    }
}
