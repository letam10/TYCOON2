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
            Quad(mesh, new Bounds(new(530, 0, 22), new(526, 4, CityDistricts.GroundDepth)),
                new Color32(57, 165, 181, 255));
            foreach (var road in roads) Quad(mesh, road, new Color32(185, 185, 159, 255));
            foreach (var corridor in WorldRouteCatalog.BuildDefault())
            {
                for (int index = 1; index < corridor.Nodes.Count; index++)
                    RouteSegment(mesh, corridor.Nodes[index - 1], corridor.Nodes[index],
                        corridor.Width, RouteColor(corridor.Kind));
            }
            foreach (var district in districts) Quad(mesh, district, new Color32(211, 178, 116, 255));
            foreach (var landmark in landmarks) Quad(mesh, landmark, new Color32(126, 156, 165, 255));
            foreach (var point in CityMapLocations.Points)
                Dot(mesh, Position(point.position), Local ? 3 : 5, new Color32(240, 222, 118, 255));
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
