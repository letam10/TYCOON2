using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tycoon
{
    public static class WorldHydrologyTerrain
    {
        struct Sample
        {
            public Vector3 Position;
            public float Water, Sand, Coast, Beach;
        }

        static readonly Dictionary<Vector2Int, Sample> samples = new();

        public static Mesh Sand(Bounds bounds) => Build(bounds, true);
        public static Mesh River() => Build(CityDistricts.WorldBounds, false);

        static Mesh Build(Bounds bounds, bool sand)
        {
            var cells = new HashSet<Vector2Int>();
            var river = WorldHydrologyLayout.River;
            for (int index = 1; index < river.Count; index++)
            {
                Bounds area = new(river[index - 1].Left, Vector3.zero);
                area.Encapsulate(river[index - 1].Right);
                area.Encapsulate(river[index].Left);
                area.Encapsulate(river[index].Right);
                area.Expand((WorldHydrologyLayout.BankWidth + 2) * 2);
                AddCells(area.min.x, area.max.x, area.min.z, area.max.z);
            }
            if (sand)
            {
                for (float z = bounds.min.z - 3; z < bounds.max.z + 3; z++)
                {
                    float x0 = WorldHydrologyLayout.CoastX(z);
                    float x1 = WorldHydrologyLayout.CoastX(z + 1);
                    AddCells(Mathf.Min(x0, x1) - WorldHydrologyLayout.BankWidth - 1,
                        Mathf.Max(x0, x1) + 1, z, z + 1);
                }
                AddCells(212, 250, 10, 144);
            }
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var indices = new List<int>();
            foreach (Vector2Int cell in cells)
            {
                Sample a = Get(cell);
                Sample b = Get(cell + Vector2Int.right);
                Sample c = Get(cell + Vector2Int.one);
                Sample d = Get(cell + Vector2Int.up);
                Triangle(a, b, c);
                Triangle(a, c, d);
            }
            var mesh = new Mesh
            {
                name = sand ? "UnifiedBeachAndRiverBanks" : "ClippedRiverEstuary",
                indexFormat = IndexFormat.UInt32
            };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (!sand)
            {
                Bounds displaced = mesh.bounds;
                displaced.Expand(new Vector3(0, .15f, 0));
                mesh.bounds = displaced;
            }
            return mesh;

            void AddCells(float left, float right, float bottom, float top)
            {
                for (int z = Mathf.FloorToInt(bottom); z < Mathf.CeilToInt(top); z++)
                    for (int x = Mathf.FloorToInt(left); x < Mathf.CeilToInt(right); x++)
                        cells.Add(new Vector2Int(x, z));
            }

            void Triangle(Sample a, Sample b, Sample c)
            {
                var polygon = new List<Sample> { a, b, c };
                polygon = Clip(polygon, sand ? 0 : 1);
                polygon = Clip(polygon, sand ? 2 : 3);
                if (polygon.Count < 3)
                    return;
                int first = vertices.Count;
                foreach (Sample point in polygon)
                {
                    Vector3 position = point.Position;
                    float depth = Mathf.Max(0, point.Water);
                    if (sand)
                    {
                        float dry = Mathf.Lerp(-.004f, .0275f, Mathf.SmoothStep(0, 1, point.Beach));
                        float wet = 1 - Mathf.Clamp01(-point.Water / WorldHydrologyLayout.BankWidth);
                        position.y = Mathf.Lerp(dry, WorldHydrologyLayout.WaterLevel, wet);
                    }
                    else
                        position.y = WorldHydrologyLayout.WaterLevel;
                    vertices.Add(position);
                    uv.Add(sand ? new Vector2(position.x, position.z) : new Vector2(depth, 0));
                }
                for (int corner = 1; corner < polygon.Count - 1; corner++)
                {
                    int second = first + corner;
                    int third = first + corner + 1;
                    Vector3 normal = Vector3.Cross(vertices[second] - vertices[first], vertices[third] - vertices[first]);
                    if (normal.sqrMagnitude < .00000001f)
                        continue;
                    indices.Add(first);
                    indices.Add(normal.y >= 0 ? second : third);
                    indices.Add(normal.y >= 0 ? third : second);
                }
            }
        }

        static Sample Get(Vector2Int key)
        {
            if (samples.TryGetValue(key, out Sample value))
                return value;
            Vector3 point = new(key.x, 0, key.y);
            float water = WorldHydrologyLayout.SignedWaterDistance(point);
            float beach = Mathf.Min(point.x - 213, Mathf.Min(point.z - 11, 143 - point.z));
            value = new Sample
            {
                Position = point,
                Water = water,
                Sand = Mathf.Max(WorldHydrologyLayout.BankWidth + water, beach),
                Coast = WorldHydrologyLayout.CoastX(point.z) - point.x,
                Beach = beach
            };
            samples.Add(key, value);
            return value;
        }

        static List<Sample> Clip(List<Sample> input, int field)
        {
            var output = new List<Sample>();
            if (input.Count == 0)
                return output;
            Sample previous = input[input.Count - 1];
            float previousValue = Value(previous, field);
            foreach (Sample current in input)
            {
                float currentValue = Value(current, field);
                if ((currentValue >= 0) != (previousValue >= 0))
                {
                    float amount = previousValue / (previousValue - currentValue);
                    output.Add(new Sample
                    {
                        Position = Vector3.Lerp(previous.Position, current.Position, amount),
                        Water = Mathf.Lerp(previous.Water, current.Water, amount),
                        Sand = Mathf.Lerp(previous.Sand, current.Sand, amount),
                        Coast = Mathf.Lerp(previous.Coast, current.Coast, amount),
                        Beach = Mathf.Lerp(previous.Beach, current.Beach, amount)
                    });
                }
                if (currentValue >= 0)
                    output.Add(current);
                previous = current;
                previousValue = currentValue;
            }
            return output;
        }

        static float Value(Sample point, int field)
        {
            return field switch
            {
                0 => -point.Water,
                1 => point.Water,
                2 => point.Sand,
                _ => point.Coast
            };
        }
    }
}
