using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed class CityMapGraphic : MaskableGraphic
    {
        public bool Local;
        GameSession game;
        WorldPathSurfaceLayout pathSurfaces;
        MapViewportState viewport;
        CityMapCapture capture;
        string routeHighlightPoiId;
        public event Action<string> PoiTooltipChanged;
        public event Action<string> PoiSelectionChanged;
        public event Action<string> RouteHighlightChanged;
        public MapViewportState Viewport => viewport;
        public MapViewportState ViewportState => viewport;
        public string SelectedPoiId => viewport?.SelectedPoiId ?? string.Empty;
        public string TooltipPoiId { get; private set; }
        public string RouteHighlightPoiId => routeHighlightPoiId;
        public WorldPathSurfaceLayout PathSurfaces => pathSurfaces;
        public CityMapCapture Capture => capture;
        public override Texture mainTexture => capture && capture.Atlas ? capture.Atlas : Texture2D.whiteTexture;

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
                "residential" => "residential",
                "apartment" => "apartment",
                "commercial" => "commercial",
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
                "residential" => "N",
                "apartment" => "A",
                "commercial" => "K",
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
                "residential" => new Color32(202, 170, 132, 255),
                "apartment" => new Color32(183, 155, 215, 255),
                "commercial" => new Color32(244, 184, 94, 255),
                _ => new Color32(240, 222, 118, 255)
            };
        }

        public void Initialize(GameSession session, bool local)
        {
            game = session; Local = local; raycastTarget = false;
            viewport = new MapViewportState();
            if (Local && game && game.Player)
                viewport.FocusPlayer(game.Player.transform.position);
            else
                viewport.SetCenter(CityDistricts.WorldBounds.center);
            var pathState = game.GetComponentInChildren<WorldPathSurfaceState>(true);
            pathSurfaces = pathState ? pathState.Layout : null;
            if (Application.isPlaying)
            {
                capture = CityMapCapture.Ensure(game);
                capture.Changed += SnapshotChanged;
            }
        }

        void SnapshotChanged()
        {
            SetAllDirty();
        }

        public void RefreshSnapshot()
        {
            if (capture)
                capture.RequestRefresh();
        }

        protected override void OnDestroy()
        {
            if (capture)
                capture.Changed -= SnapshotChanged;
            base.OnDestroy();
        }

        public Bounds ViewBounds
        {
            get
            {
                float zoom = viewport == null ? 1 : Mathf.Max(.01f, viewport.Zoom);
                if (!Local)
                    return new Bounds(MapCenter(), CityDistricts.WorldBounds.size / zoom);
                float aspect = rectTransform.rect.width / Mathf.Max(1, rectTransform.rect.height);
                return new Bounds(MapCenter(), new(128 * aspect / zoom, 4, 128 / zoom));
            }
        }

        public Vector2 Position(Vector3 world)
        {
            var bounds = ViewBounds;
            Vector2 delta = new(world.x - bounds.center.x, world.z - bounds.center.z);
            delta = Rotate(delta, -(viewport?.RotationDegrees ?? 0));
            return new Vector2(delta.x / bounds.size.x, delta.y / bounds.size.z) + Vector2.one * .5f;
        }

        public Vector3 WorldAt(Vector2 normalized)
        {
            Bounds bounds = ViewBounds;
            Vector2 offset = normalized - Vector2.one * .5f;
            offset = new Vector2(offset.x * bounds.size.x, offset.y * bounds.size.z);
            offset = Rotate(offset, viewport?.RotationDegrees ?? 0);
            return bounds.center + new Vector3(offset.x, 0, offset.y);
        }

        public Vector2 Direction(Vector3 worldDirection)
        {
            Vector2 direction = new(worldDirection.x, worldDirection.z);
            if (direction.sqrMagnitude < .000001f)
                return Vector2.up;
            return Rotate(direction.normalized, -(viewport?.RotationDegrees ?? 0));
        }

        public void ZoomBy(float amount)
        {
            viewport?.ZoomBy(amount);
            SetVerticesDirty();
        }

        public void Pan(Vector2 normalizedDelta)
        {
            Bounds bounds = ViewBounds;
            Vector2 offset = new Vector2(normalizedDelta.x * bounds.size.x, normalizedDelta.y * bounds.size.z);
            viewport?.Pan(Rotate(offset, viewport?.RotationDegrees ?? 0));
            if (viewport != null)
            {
                Bounds world = CityDistricts.WorldBounds;
                Vector3 center = viewport.Center;
                viewport.SetCenter(new Vector3(Mathf.Clamp(center.x, world.min.x, world.max.x), center.y,
                    Mathf.Clamp(center.z, world.min.z, world.max.z)));
            }
            SetVerticesDirty();
        }

        public void RotateBy(float degrees)
        {
            viewport?.RotateBy(degrees);
            SetVerticesDirty();
        }

        public void ResetNorth()
        {
            viewport?.ResetNorth();
            SetVerticesDirty();
        }

        public void FocusPlayer()
        {
            if (game && game.Player)
                FocusPlayer(game.Player.transform.position);
        }

        public void FocusPlayer(Vector3 playerPosition)
        {
            viewport?.FocusPlayer(playerPosition);
            SetVerticesDirty();
        }

        public void ToggleKind(string kind)
        {
            viewport?.ToggleKind(kind);
            SetVerticesDirty();
        }

        public bool IsKindEnabled(string kind)
        {
            return viewport == null || viewport.IsKindEnabled(kind);
        }

        public bool IsPoiVisible(string id)
        {
            return IsKindEnabled(PoiKind(id));
        }

        public void SelectPoi(string id)
        {
            if (viewport == null)
                return;
            string selected = FindPoint(id) ? id : string.Empty;
            viewport.Select(selected);
            PoiSelectionChanged?.Invoke(viewport.SelectedPoiId);
            SetVerticesDirty();
        }

        public void SetSelectedPoi(string id)
        {
            SelectPoi(id);
        }

        public void SetTooltip(string id)
        {
            TooltipPoiId = FindPoint(id) ? id : string.Empty;
            PoiTooltipChanged?.Invoke(TooltipPoiId);
        }

        public string GetPoiTooltip(string id)
        {
            foreach (var point in CityMapLocations.Points)
                if (point.id == id)
                    return point.label + " • " + PoiKind(id);
            return string.Empty;
        }

        public void SetRouteHighlight(string id)
        {
            routeHighlightPoiId = FindPoint(id) ? id : string.Empty;
            RouteHighlightChanged?.Invoke(routeHighlightPoiId);
            SetVerticesDirty();
        }

        public void HighlightRoute(string id)
        {
            SetRouteHighlight(id);
        }

        public void ClearRouteHighlight()
        {
            SetRouteHighlight(string.Empty);
        }

        Vector3 MapCenter()
        {
            if (viewport != null && IsFinite(viewport.Center))
                return viewport.Center;
            if (Local && game && game.Player)
                return game.Player.transform.position;
            return CityDistricts.WorldBounds.center;
        }

        static Vector2 Rotate(Vector2 value, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cosine = Mathf.Cos(radians);
            float sine = Mathf.Sin(radians);
            return new Vector2(value.x * cosine - value.y * sine,
                value.x * sine + value.y * cosine);
        }

        static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }

        static bool FindPoint(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            foreach (var point in CityMapLocations.Points)
                if (point.id == id)
                    return true;
            return false;
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (!game || !game.Player) return;
            Rect background = rectTransform.rect;
            AddQuad(mesh, background.min, background.max, new Color32(31, 53, 48, 255));
            DrawCapturedTiles(mesh);
            DrawRouteHighlight(mesh);
            foreach (var point in CityMapLocations.Points)
            {
                if (!IsPoiVisible(point.id))
                    continue;
                PoiIcon(mesh, point.id, Position(point.position), Local ? 3 : 6,
                    point.id == SelectedPoiId);
            }
            var player = Position(game.Player.transform.position);
            Dot(mesh, player, 6, Color.white);
            var rect = rectTransform.rect;
            Vector2 center = rect.min + Vector2.Scale(player, rect.size);
            Vector3 forward = game.Player.transform.forward;
            Vector2 direction = Direction(forward);
            Vector2 side = new(-direction.y, direction.x);
            int first = mesh.currentVertCount;
            mesh.AddVert(center + direction * 12, Color.white, Vector2.zero);
            mesh.AddVert(center - direction * 5 + side * 6, Color.white, Vector2.zero);
            mesh.AddVert(center - direction * 5 - side * 6, Color.white, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
        }

        void DrawCapturedTiles(VertexHelper mesh)
        {
            if (!capture || !capture.Atlas || capture.Grid == null)
                return;
            Rect rect = rectTransform.rect;
            for (int index = 0; index < capture.Grid.TileCount; index++)
            {
                Bounds tile = capture.Grid.TileBounds(index);
                Rect uv = capture.Grid.TileUv(index);
                Vector2[] points =
                {
                    Position(tile.min),
                    Position(new Vector3(tile.min.x, 0, tile.max.z)),
                    Position(tile.max),
                    Position(new Vector3(tile.max.x, 0, tile.min.z))
                };
                Vector2[] coordinates =
                {
                    uv.min, new Vector2(uv.xMin, uv.yMax), uv.max, new Vector2(uv.xMax, uv.yMin)
                };
                int first = mesh.currentVertCount;
                for (int corner = 0; corner < points.Length; corner++)
                    mesh.AddVert(rect.min + Vector2.Scale(points[corner], rect.size), Color.white, coordinates[corner]);
                mesh.AddTriangle(first, first + 1, first + 2);
                mesh.AddTriangle(first, first + 2, first + 3);
            }
        }

        void DrawRouteHighlight(VertexHelper mesh)
        {
            if (string.IsNullOrEmpty(routeHighlightPoiId) || !game || !game.Player)
                return;
            foreach (var point in CityMapLocations.Points)
            {
                if (point.id != routeHighlightPoiId)
                    continue;
                RouteSegment(mesh, game.Player.transform.position, point.position, 3.5f,
                    new Color32(255, 239, 116, 240));
                return;
            }
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

        void PoiIcon(VertexHelper mesh, string id, Vector2 center, float scale, bool selected = false)
        {
            Color edge = new Color32(24, 56, 48, 225);
            Color tint = PoiColor(id);
            Circle(mesh, center, scale + 1.6f, edge);
            if (selected)
                Circle(mesh, center, scale + 3.6f, new Color32(255, 239, 116, 220));
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
                case "residential":
                    PixelPolygon(mesh, center, tint, new(-3, -1), new(0, 3), new(3, -1),
                        new(3, -4), new(-3, -4));
                    break;
                case "apartment":
                    PixelPolygon(mesh, center, tint, new(-2, -4), new(2, -4), new(2, 4),
                        new(-2, 4));
                    PixelPolygon(mesh, center + Vector2.up * 1.5f, edge,
                        new(-1, -1), new(1, -1), new(1, 1), new(-1, 1));
                    break;
                case "commercial":
                    PixelPolygon(mesh, center, tint, new(-4, -2), new(4, -2), new(3, 3),
                        new(-3, 3));
                    PixelPolygon(mesh, center + Vector2.down * 1.5f, edge,
                        new(-2, -1), new(2, -1), new(2, 1), new(-2, 1));
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
