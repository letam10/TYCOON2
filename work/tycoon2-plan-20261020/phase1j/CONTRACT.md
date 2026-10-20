# Phase 1J - deterministic path surface ownership

Base main e34125d46a747270b1798f5c377221449e37155a. Read ../objective-recovered.md.
Original Phase 1 and full redesign remain OPEN. This fixes cross-category
overlap and matching walkable mesh geometry; true rounded corners, complete
rail/traffic/NavMesh behavior and later phases keep their original scope.

## Shared foundation written by root before agents

WorldPathSurface.cs defines immutable request/piece/layout and runtime state.
WorldPathSurfaceRequest constructor(string id, IReadOnlyList<Vector3> points,
float width, string surface, string tint, float bottom, float top, int priority).
Properties: Id, Points, Width, Surface, Tint, Bottom, Top, Priority.
Points are already sampled. Never interpolate them again. Id is unique per
request and is independent of polygon pieces, mesh batches and save IDs.
WorldPathSurfacePiece(request, geometry): Request and Geometry properties.
WorldPathSurfaceLayout(requests, pieces, blockers): Requests, Pieces, Blockers
immutable lists. Keep even fully occluded requests in Requests.
WorldPathSurfaceState : MonoBehaviour exposes Layout and Initialize(layout).

WorldPathSurfaceCatalog.BuildDefault() returns immutable catalog requests for
all non-rail default corridors and public approaches. Width, sampled coordinates,
IDs stay unchanged. All use bottom .0005/top .0555. Priority Dock=400,
Highway=300, Sidewalk=200, FarmPath=100; public approaches Sidewalk=200.
CreateLandmark(string landmarkId, int leg, IReadOnlyList<Vector3> points,
float width) returns Id=landmark:<landmarkId>:<leg>, paving/#C0B8A0, bottom 0,
top .05, priority 200. Do not rename any existing gameplay/POI/save ID.
BuildStreetBlockers() returns WorldStreetGeometry.Build(street catalog)
Pavements + Sidewalks. These are highest-priority protected footprints.

There is no HTML, DOM or JS entrypoint. Unity entry remains existing WorldFactory
and CityOuterDistricts -> CityAccessPaths.Build. No dt/speed/camera changes.

## Independent agent ownership

1. path_surface_geometry owns only WorldStreetGeometry.cs and new
WorldPathSurfaceGeometry.cs in Runtime. Add public static IReadOnlyList<
WorldStreetSurface> SubtractSurfaces(IReadOnlyList<WorldStreetSurface> subjects,
IReadOnlyList<WorldStreetSurface> blockers), reusing existing double clipping.
Preserve input metadata, convex CCW winding, unique deterministic piece indices
per (RouteId,SegmentIndex,Side), finite/nondegenerate output and input immutability.
Subtract only at identical base height (Source.Start.y). Do not flatten elevated
surfaces. AABB broad phase is allowed to avoid pointless split allocations.
WorldPathSurfaceGeometry.Build(IReadOnlyList<WorldPathSurfaceRequest> requests,
IReadOnlyList<WorldStreetSurface> blockers) returns layout. Sort descending
priority then Ordinal Id, reject duplicate request IDs, BuildPavement from already
sampled Points, clip street blockers then earlier owned surfaces at same height.
Every covered region has one owner; accepted pieces still cover the input union
outside blockers. Do not paper over errors by deleting intersecting requests.

2. path_surface_runtime owns only CityAccessPaths.cs and new
WorldPathSurfaceMesh.cs in Runtime. Collect all default + bespoke landmark
requests before rendering. Preserve bespoke quadratic path points/widths, signs
at raw Nodes[0], and every existing RouteCorridor/PedestrianLink trigger pose,
size and count. Give every bespoke leg its stable landmark request ID.
Build layout with default street blockers, add WorldPathSurfaceState and call
WorldPathSurfaceMesh.Build(Transform root, WorldPathSurfaceLayout layout).
Renderer batches by Surface/Tint/Bottom/Top, creates closed prisms, combines
temporary meshes, releases them and registers final meshes in existing
WorldStreetMeshOwner. Each PathSurface_<index> child has identity local pose,
MeshFilter+MeshRenderer+non-trigger MeshCollider sharing the exact same mesh.
Do not statically batch these renderers into a different mesh after assignment.
CityStaticGeometry may still own trigger creation/legacy sign helpers. Preserve
CreateCurvedPath/CreateSmoothPath public methods. Do not edit other consumers.

3. path_surface_tests owns only new EditMode/WorldPathSurfaceTests.cs. Write
independent numeric area/coverage tests for priority crossings, reverse-input
determinism, same-priority Ordinal tie, protection by street blockers, fully
occluded requests, partially clipped joins, duplicate-ID rejection, elevated
surfaces, preserved metadata/immutability and current catalog. Test renderer
shared meshes/pose/cleanup if bounded without Unity process launch. Use NUnit
Tycoon.Tests and independent polygon intersection/point tests, not source strings
or expected = same algorithm called twice. Root owns all compilation/runtime.

Agents may read; write only assigned files, one exec mutation per source file.
No Unity/Player/server/browser, extra agents, messages/followups, commit or push.
C# is not valid node --check input. Return promptly when source work is done.

## Root integration gates

Root writes foundation/catalog, reviews diffs, fixes integration and adds live QA.
Root alone runs affected EditMode tests, isolated Windows build and short native
world-features QA, opens fresh industrial/farm/marina/map captures and checks
actual shared mesh collider coverage. Preserve the 101 dirty tracked files and
all unrelated untracked assets. No reset/clean/stash, broad stage or benchmark.
If visual defects remain, repair and repeat fresh evidence. Update handoff and
machine acceptance, exact allowlist commit/non-force push per saved objective.
Do not claim Phase 1, whole goal, full old-save or 60 FPS acceptance from this.
