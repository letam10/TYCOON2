using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed class CityMapGraphic : MaskableGraphic
    {
        public bool Local;
        GameSession game;
        Bounds[] roads, districts, landmarks;
        static readonly string[] areas = { "farm", "farm_shop", "processing", "supermarket", "bakery", "restaurant" };

        public static string PoiKind(string id)
        {
            return id switch
            {
                "tycoon" => "farm",
                "park" => "park",
                "station" => "rail",
                "beach" => "beach",
                "harbor" or "marina" or "east_marina" => "marina",
                "bank" => "civic",
                "restaurant" => "food",
                "forest" or "forest_reserve" => "forest",
                "industrial_yard" => "industrial",
                "north_village" => "village",
                "south_highway" => "highway",
                _ => "poi"
            };
        }

        public static string PoiGlyph(string id)
        {
            return PoiKind(id) switch
            {
                "farm" => "F",
                "park" => "P",
                "rail" => "R",
                "beach" => "B",
                "marina" => "M",
                "civic" => "C",
                "food" => "D",
                "forest" => "T",
                "industrial" => "I",
                "village" => "V",
                "highway" => "H",
                _ => "•"
            };
        }

        public static Color PoiColor(string id)
        {
            return PoiKind(id) switch
            {
                "farm" => new Color32(240, 206, 95, 255),
                "park" => new Color32(112, 204, 137, 255),
                "rail" => new Color32(174, 185, 216, 255),
                "beach" => new Color32(104, 203, 224, 255),
                "marina" => new Color32(105, 173, 213, 255),
                "civic" => new Color32(217, 165, 235, 255),
                "food" => new Color32(240, 156, 114, 255),
                "forest" => new Color32(60, 154, 104, 255),
                "industrial" => new Color32(228, 155, 103, 255),
                "village" => new Color32(230, 210, 164, 255),
                "highway" => new Color32(241, 236, 224, 255),
                _ => new Color32(240, 222, 118, 255)
            };
        }

        public void Initialize(GameSession session, bool local)
        {
            game = session; Local = local; raycastTarget = false;
            roads = CityStreetNetwork.Bounds().ToArray();
            districts = areas.Select(CityDistricts.Footprint).ToArray();
            var existingLandmarks = game.GetComponentsInChildren<CityLandmark>()
                .Select(x => new Bounds(x.transform.position, x.Footprint));
            var expansionLandmarks = WorldExpansionCatalog.BuildDefault()
                .Select(x => x.FootprintBounds);
            landmarks = existingLandmarks.Concat(expansionLandmarks).ToArray();
        }

        public Bounds ViewBounds
        {
            get
            {
                if (!Local || !game || !game.Player) return CityDistricts.WorldBounds;
                float aspect = rectTransform.rect.width / Mathf.Max(1, rectTransform.rect.height);
                return new Bounds(game.Player.transform.position, new(128 * aspect, 4, 128));
            }
        }

        public Vector2 Position(Vector3 world)
        {
            var bounds = ViewBounds;
            return new((world.x - bounds.min.x) / bounds.size.x, (world.z - bounds.min.z) / bounds.size.z);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (roads == null || !game || !game.Player) return;
            var bounds = ViewBounds;
            Quad(mesh, bounds, new Color32(91, 126, 91, 255));
            Quad(mesh, CoastalWaterBounds(), new Color32(57, 165, 181, 255));
            DrawCoastline(mesh);
            DrawRegionTints(mesh);
            foreach (var road in roads) Quad(mesh, road, new Color32(185, 185, 159, 255));
            foreach (var corridor in WorldRouteCatalog.BuildDefault())
            {
                for (int index = 1; index < corridor.Nodes.Count; index++)
                    RouteSegment(mesh, corridor.Nodes[index - 1], corridor.Nodes[index],
                        corridor.Width, RouteColor(corridor.Kind));
                for (int index = 1; index < corridor.Nodes.Count - 1; index++)
                    CurvedRoute(mesh, corridor.Nodes[index - 1], corridor.Nodes[index],
                        corridor.Nodes[index + 1], corridor.Width * .84f, RouteColor(corridor.Kind));
            }
            foreach (var district in districts) Quad(mesh, district, new Color32(211, 178, 116, 255));
            foreach (var landmark in landmarks) Quad(mesh, landmark, new Color32(126, 156, 165, 255));
            foreach (var point in CityMapLocations.Points)
                PoiIcon(mesh, point.id, Position(point.position), Local ? 3 : 6);
            var player = Position(game.Player.transform.position);
            Dot(mesh, player, 6, Color.white);
            var rect = rectTransform.rect;
            Vector2 center = rect.min + Vector2.Scale(player, rect.size);
            Vector3 forward = game.Player.transform.forward;
            Vector2 direction = new Vector2(forward.x, forward.z).normalized;
            Vector2 side = new(-direction.y, direction.x);
            int first = mesh.currentVertCount;
            mesh.AddVert(center + direction * 12, Color.white, Vector2.zero);
            mesh.AddVert(center - direction * 5 + side * 6, Color.white, Vector2.zero);
            mesh.AddVert(center - direction * 5 - side * 6, Color.white, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
        }

        static Bounds CoastalWaterBounds()
        {
            return new Bounds(new(530, 0, 22), new(526, 4, CityDistricts.GroundDepth));
        }

        void DrawCoastline(VertexHelper mesh)
        {
            Bounds bounds = CityDistricts.WorldBounds;
            float start = bounds.min.z;
            Vector3 previous = CoastPoint(start);
            for (int index = 1; index <= 32; index++)
            {
                float amount = (float)index / 32;
                float z = Mathf.Lerp(bounds.min.z, bounds.max.z, amount);
                Vector3 current = CoastPoint(z);
                RouteSegment(mesh, previous, current, 2.2f, new Color32(244, 221, 153, 235));
                previous = current;
            }
        }

        static Vector3 CoastPoint(float z)
        {
            Bounds water = CoastalWaterBounds();
            float x = water.min.x + Mathf.Sin((z + 14) * .037f) * 5.5f
                + Mathf.Sin((z - 42) * .083f) * 2.2f;
            return new Vector3(x, 0, z);
        }

        void DrawRegionTints(VertexHelper mesh)
        {
            Quad(mesh, new Bounds(new(30, 0, 133), new(92, 4, 76)),
                new Color32(53, 144, 92, 100));
            foreach (var poi in WorldExpansionCatalog.BuildDefault())
            {
                if (poi.Id == "ForestReserve")
                    Quad(mesh, poi.FootprintBounds, new Color32(53, 144, 92, 105));
                if (poi.Id == "IndustrialYard")
                    Quad(mesh, poi.FootprintBounds, new Color32(192, 135, 91, 112));
            }
        }

        void Quad(VertexHelper mesh, Bounds world, Color tint)
        {
            Vector2 min = Position(world.min), max = Position(world.max);
            min = Vector2.Max(Vector2.zero, min); max = Vector2.Min(Vector2.one, max);
            if (min.x >= max.x || min.y >= max.y) return;
            var rect = rectTransform.rect;
            AddQuad(mesh, rect.min + Vector2.Scale(min, rect.size), rect.min + Vector2.Scale(max, rect.size), tint);
        }

        void Dot(VertexHelper mesh, Vector2 point, float radius, Color tint)
        {
            if (point.x < 0 || point.y < 0 || point.x > 1 || point.y > 1) return;
            var rect = rectTransform.rect;
            Vector2 center = rect.min + Vector2.Scale(point, rect.size);
            AddQuad(mesh, center - Vector2.one * radius, center + Vector2.one * radius, tint);
        }

        void RouteSegment(VertexHelper mesh, Vector3 start, Vector3 end, float width, Color tint)
        {
            Vector3 delta = end - start;
            delta.y = 0;
            if (delta.sqrMagnitude < .001f) return;
            Vector3 side = Vector3.Cross(Vector3.up, delta).normalized * (width * .5f);
            AddPolygon(mesh, new[]
            {
                Position(start + side), Position(start - side),
                Position(end - side), Position(end + side)
            }, tint);
        }

        void CurvedRoute(VertexHelper mesh, Vector3 start, Vector3 control, Vector3 end, float width, Color tint)
        {
            const int steps = 5;
            Vector3 previous = start;
            for (int step = 1; step <= steps; step++)
            {
                float amount = (float)step / steps;
                float inverse = 1 - amount;
                Vector3 current = inverse * inverse * start + 2 * inverse * amount * control
                    + amount * amount * end;
                RouteSegment(mesh, previous, current, width, tint);
                previous = current;
            }
        }

        void PoiIcon(VertexHelper mesh, string id, Vector2 center, float scale)
        {
            Color edge = new Color32(24, 56, 48, 225);
            Color tint = PoiColor(id);
            Circle(mesh, center, scale + 1.6f, edge);
            switch (PoiKind(id))
            {
                case "farm":
                    PixelPolygon(mesh, center, tint, new(-3, -1), new(0, 3), new(3, -1),
                        new(3, -3), new(-3, -3));
                    break;
                case "park":
                case "forest":
                    PixelPolygon(mesh, center, tint, new(-3, -3), new(0, 3), new(3, -3));
                    PixelPolygon(mesh, center + Vector2.down * 1.5f, tint,
                        new(-2, -3), new(0, 2), new(2, -3));
                    break;
                case "rail":
                    PixelPolygon(mesh, center, tint, new(-3, -3), new(-1, -3), new(-1, 3),
                        new(1, 3), new(1, -3), new(3, -3), new(3, 3), new(-3, 3));
                    break;
                case "beach":
                case "marina":
                    PixelPolygon(mesh, center, tint, new(-4, -1), new(-2, 1), new(0, -1),
                        new(2, 1), new(4, -1), new(4, -3), new(-4, -3));
                    break;
                case "industrial":
                    PixelPolygon(mesh, center, tint, new(-3, -2), new(-2, -4), new(2, -4),
                        new(3, -2), new(3, 2), new(2, 4), new(-2, 4), new(-3, 2));
                    break;
                case "civic":
                    PixelPolygon(mesh, center, tint, new(0, 4), new(4, 0), new(0, -4),
                        new(-4, 0));
                    break;
                case "food":
                    Circle(mesh, center, scale * .65f, tint);
                    PixelPolygon(mesh, center + Vector2.up * 2, tint,
                        new(-3, 0), new(3, 0), new(2, 2), new(-2, 2));
                    break;
                case "village":
                    PixelPolygon(mesh, center, tint, new(-3, -3), new(0, 2), new(3, -3),
                        new(3, -4), new(-3, -4));
                    break;
                case "highway":
                    PixelPolygon(mesh, center, tint, new(-4, -2), new(4, -2), new(4, 2),
                        new(-4, 2));
                    break;
                default:
                    Circle(mesh, center, scale * .65f, tint);
                    break;
            }
        }

        void Circle(VertexHelper mesh, Vector2 center, float radius, Color tint)
        {
            const int sides = 8;
            Rect rect = rectTransform.rect;
            Vector2[] points = new Vector2[sides];
            float width = Mathf.Max(1, rect.width);
            float height = Mathf.Max(1, rect.height);
            for (int index = 0; index < sides; index++)
            {
                float angle = Mathf.PI * 2 * index / sides;
                points[index] = center + new Vector2(Mathf.Cos(angle) * radius / width,
                    Mathf.Sin(angle) * radius / height);
            }
            AddPolygon(mesh, points, tint);
        }

        void PixelPolygon(VertexHelper mesh, Vector2 center, Color tint, params Vector2[] offsets)
        {
            Rect rect = rectTransform.rect;
            float width = Mathf.Max(1, rect.width);
            float height = Mathf.Max(1, rect.height);
            Vector2[] points = new Vector2[offsets.Length];
            for (int index = 0; index < offsets.Length; index++)
                points[index] = center + new Vector2(offsets[index].x / width, offsets[index].y / height);
            AddPolygon(mesh, points, tint);
        }

        static Color RouteColor(RouteKind kind)
        {
            return kind switch
            {
                RouteKind.FarmPath => new Color32(151, 113, 73, 220),
                RouteKind.Highway => new Color32(73, 82, 85, 230),
                RouteKind.Rail => new Color32(83, 76, 70, 230),
                RouteKind.Dock => new Color32(161, 139, 104, 230),
                _ => new Color32(225, 222, 187, 220)
            };
        }

        void AddPolygon(VertexHelper mesh, Vector2[] points, Color tint)
        {
            int first = mesh.currentVertCount;
            for (int index = 0; index < points.Length; index++)
            {
                Rect rect = rectTransform.rect;
                Vector2 local = rect.min + Vector2.Scale(points[index], rect.size);
                mesh.AddVert(local, tint, Vector2.zero);
            }
            for (int index = 1; index < points.Length - 1; index++)
                mesh.AddTriangle(first, first + index, first + index + 1);
        }

        static void AddQuad(VertexHelper mesh, Vector2 min, Vector2 max, Color tint)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(new(min.x, min.y), tint, Vector2.zero);
            mesh.AddVert(new(min.x, max.y), tint, Vector2.zero);
            mesh.AddVert(new(max.x, max.y), tint, Vector2.zero);
            mesh.AddVert(new(max.x, min.y), tint, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2); mesh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
