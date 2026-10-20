using System;
using System.Collections.Generic;

namespace Tycoon
{
    public static class WorldPathSurfaceGeometry
    {
        public static WorldPathSurfaceLayout Build(
            IReadOnlyList<WorldPathSurfaceRequest> requests,
            IReadOnlyList<WorldStreetSurface> blockers)
        {
            if (requests == null)
                throw new ArgumentNullException(nameof(requests));
            if (blockers == null)
                throw new ArgumentNullException(nameof(blockers));

            var ordered = new List<WorldPathSurfaceRequest>(requests.Count);
            var requestIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < requests.Count; index++)
            {
                WorldPathSurfaceRequest request = requests[index];
                if (request == null)
                    throw new ArgumentException("Path requests cannot contain null entries.", nameof(requests));
                if (!requestIds.Add(request.Id))
                    throw new ArgumentException("Duplicate path surface request id: " + request.Id, nameof(requests));
                ordered.Add(request);
            }
            ordered.Sort(CompareRequests);

            var pieces = new List<WorldPathSurfacePiece>();
            var owned = new List<WorldStreetSurface>();
            for (int index = 0; index < ordered.Count; index++)
            {
                WorldPathSurfaceRequest request = ordered[index];
                IReadOnlyList<WorldStreetSurface> available = WorldStreetGeometry.BuildPavement(
                    request.Id, request.Points, request.Width);
                available = WorldStreetGeometry.SubtractSurfaces(available, blockers);
                // Mặt ưu tiên cao giữ phần giao; mặt sau chỉ nhận diện tích còn trống.
                available = WorldStreetGeometry.SubtractSurfaces(available, owned);
                for (int pieceIndex = 0; pieceIndex < available.Count; pieceIndex++)
                {
                    WorldStreetSurface geometry = available[pieceIndex];
                    pieces.Add(new WorldPathSurfacePiece(request, geometry));
                    owned.Add(geometry);
                }
            }
            return new WorldPathSurfaceLayout(ordered, pieces, blockers);
        }

        static int CompareRequests(WorldPathSurfaceRequest first, WorldPathSurfaceRequest second)
        {
            int priority = second.Priority.CompareTo(first.Priority);
            return priority != 0 ? priority : StringComparer.Ordinal.Compare(first.Id, second.Id);
        }
    }
}
