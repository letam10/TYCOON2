using System;
using UnityEngine;

namespace Tycoon
{
    public sealed class CityMapCaptureGrid
    {
        public Bounds Bounds { get; }
        public int Columns { get; }
        public int Rows { get; }
        public int TileWidth { get; }
        public int TileHeight { get; }
        public int TileCount => Columns * Rows;
        public int AtlasWidth => Columns * TileWidth + 2;
        public int AtlasHeight => Rows * TileHeight + 2;

        public CityMapCaptureGrid(Bounds bounds, int columns, int rows, int tileWidth)
        {
            if (bounds.size.x <= 0 || bounds.size.z <= 0 || columns < 1 || rows < 1 || tileWidth < 1)
                throw new ArgumentOutOfRangeException(nameof(bounds));
            Bounds = bounds;
            Columns = columns;
            Rows = rows;
            TileWidth = tileWidth;
            float cellAspect = (bounds.size.x / columns) / (bounds.size.z / rows);
            TileHeight = Mathf.Max(1, Mathf.RoundToInt(tileWidth / cellAspect));
        }

        public Bounds TileBounds(int index)
        {
            ValidateIndex(index);
            int column = index % Columns;
            int row = index / Columns;
            Vector3 minimum = Bounds.min;
            Vector3 maximum = Bounds.max;
            float left = Mathf.Lerp(minimum.x, maximum.x, column / (float)Columns);
            float right = Mathf.Lerp(minimum.x, maximum.x, (column + 1f) / Columns);
            float bottom = Mathf.Lerp(minimum.z, maximum.z, row / (float)Rows);
            float top = Mathf.Lerp(minimum.z, maximum.z, (row + 1f) / Rows);
            var result = new Bounds();
            result.SetMinMax(new Vector3(left, minimum.y, bottom), new Vector3(right, maximum.y, top));
            return result;
        }

        public RectInt TilePixels(int index)
        {
            ValidateIndex(index);
            return new RectInt(1 + index % Columns * TileWidth, 1 + index / Columns * TileHeight,
                TileWidth, TileHeight);
        }

        public Rect TileUv(int index)
        {
            RectInt pixels = TilePixels(index);
            return new Rect(pixels.x / (float)AtlasWidth, pixels.y / (float)AtlasHeight,
                pixels.width / (float)AtlasWidth, pixels.height / (float)AtlasHeight);
        }

        public Vector2 WorldToUv(Vector3 point)
        {
            return new Vector2((point.x - Bounds.min.x) / Bounds.size.x,
                (point.z - Bounds.min.z) / Bounds.size.z);
        }

        public Vector3 UvToWorld(Vector2 uv)
        {
            return new Vector3(Bounds.min.x + uv.x * Bounds.size.x, Bounds.center.y,
                Bounds.min.z + uv.y * Bounds.size.z);
        }

        void ValidateIndex(int index)
        {
            if (index < 0 || index >= TileCount)
                throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
