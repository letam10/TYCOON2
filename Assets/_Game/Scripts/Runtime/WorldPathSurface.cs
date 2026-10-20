using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public sealed class WorldPathSurfaceRequest
    {
        public string Id { get; }
        public IReadOnlyList<Vector3> Points { get; }
        public float Width { get; }
        public string Surface { get; }
        public string Tint { get; }
        public float Bottom { get; }
        public float Top { get; }
        public int Priority { get; }

        public WorldPathSurfaceRequest(string id, IReadOnlyList<Vector3> points,
            float width, string surface, string tint, float bottom, float top, int priority)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Surface request id is required.", nameof(id));
            if (points == null || points.Count < 2)
                throw new ArgumentException("Surface request needs sampled points.", nameof(points));
            if (width <= 0 || float.IsNaN(width) || float.IsInfinity(width))
                throw new ArgumentOutOfRangeException(nameof(width));
            if (float.IsNaN(bottom) || float.IsInfinity(bottom) || float.IsNaN(top)
                || float.IsInfinity(top) || top <= bottom)
                throw new ArgumentOutOfRangeException(nameof(top));
            Id = id;
            Points = new List<Vector3>(points).AsReadOnly();
            Width = width;
            Surface = surface;
            Tint = tint;
            Bottom = bottom;
            Top = top;
            Priority = priority;
        }
    }

    public sealed class WorldPathSurfacePiece
    {
        public WorldPathSurfaceRequest Request { get; }
        public WorldStreetSurface Geometry { get; }

        public WorldPathSurfacePiece(WorldPathSurfaceRequest request, WorldStreetSurface geometry)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        }
    }

    public sealed class WorldPathSurfaceLayout
    {
        public IReadOnlyList<WorldPathSurfaceRequest> Requests { get; }
        public IReadOnlyList<WorldPathSurfacePiece> Pieces { get; }
        public IReadOnlyList<WorldStreetSurface> Blockers { get; }

        public WorldPathSurfaceLayout(IReadOnlyList<WorldPathSurfaceRequest> requests,
            IReadOnlyList<WorldPathSurfacePiece> pieces, IReadOnlyList<WorldStreetSurface> blockers)
        {
            Requests = new List<WorldPathSurfaceRequest>(requests).AsReadOnly();
            Pieces = new List<WorldPathSurfacePiece>(pieces).AsReadOnly();
            Blockers = new List<WorldStreetSurface>(blockers).AsReadOnly();
        }
    }

    public sealed class WorldPathSurfaceState : MonoBehaviour
    {
        public WorldPathSurfaceLayout Layout { get; private set; }

        public void Initialize(WorldPathSurfaceLayout layout)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        }
    }
}
