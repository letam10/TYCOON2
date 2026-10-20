# Phase 1H shared street surface geometry

Base main 231763d8e3368a0f34bf9efbab8cb6c1d4c033c1.
Read objective-recovered.md in full. Phase 1 and full redesign remain open.
Foundation WorldStreetSurface.cs is complete on disk before agent dispatch.

## Fixed interfaces

WorldStreetSurface immutable data:
RouteId string; SegmentIndex int; Side int (0 pavement, -1/+1 sidewalk);
PieceIndex int; Source WorldRouteSegment; Outline IReadOnlyList<Vector3>.
Constructor takes those six values in the same order.
Outline is a convex polygon with positive signed area in XZ, no repeated final
vertex, finite points and no consecutive duplicate/collinear vertices.
WorldStreetLayout.Pavements and .Sidewalks are readonly surface lists.
Constructor takes pavement list, then sidewalk list.

WorldStreetGeometry public static method:
WorldStreetLayout Build(IReadOnlyList<WorldRouteCorridor> routes, float sidewalkWidth = 1.8f).
WorldStreetMesh public static method:
Mesh CreatePrism(WorldStreetSurface surface, float bottom, float top).
bottom/top are offsets added to Outline point y, not absolute world height.
WorldStreetMeshOwner MonoBehaviour: public void Register(Mesh mesh); destroys
only its registered runtime meshes in OnDestroy. Root attaches one to CityStreets.

## Geometry contract

Input is the existing catalog's 22 flat streets; do not move nodes, widths,
IDs, route kinds or change economy/save/camera contracts.
For each segment create its oriented pavement rectangle from exact endpoints
and half width. Sidewalk rectangles use center offset +/- (roadWidth/2 + .9),
sidewalk half width .9, preserving the existing total width 1.8 m.
Emit pavement union without interior overlaps by subtracting previous input
road rectangles from each subsequent pavement rectangle.
Subtract every road footprint from each sidewalk rectangle, including road
intersections in the middle of a source segment. Then subtract previously
accepted sidewalk pieces so curb corners meet without coplanar overlap.
Use exact convex polygon difference: split a convex subject successively by
each CCW blocker half-plane; emit outside polygons and continue with the
inside remainder. Do not approximate a rotated polygon with its AABB or trim
by a fixed fraction/distance. Pieces remain convex. Remove zero area slivers.
Keep deterministic iteration and piece IDs. Intersections at different surface
heights must not remove each other's geometry. Current inputs are flat; slopes
and full rail/highway/dock semantics remain a later gate, not silently accepted.
No curved or other route catalog data is modified by this tranche.

## Ownership

1. street_surface_geometry owns only Runtime/WorldStreetGeometry.cs under
   Assets/_Game/Scripts. Implements full polygon difference and Build.
2. street_surface_mesh owns only Runtime/WorldStreetMesh.cs under that path.
   Implements CreatePrism and mesh owner. Top normals upward, bottom downward,
   sides outward, UVs in world metre coordinates, finite triangles/bounds.
   No materials, object creation beyond Mesh, or collider responsibility here.
3. street_surface_tests owns only Assets/_Game/Tests/EditMode/WorldStreetSurfaceTests.cs.
   NUnit fixture covers analytic crossing union area, no sidewalk-road overlap,
   no sidewalk-sidewalk overlap, collinear continuation coverage, rotated cases,
   deterministic results, current catalog coverage, different height isolation,
   mesh normals/bounds/triangle area and flat cap manifold coverage. Use an
   independent intersection/area oracle, not source-text tests or Build calling
   itself as the expected result. Also cover FindJunctions excluding pure
   collinear continuations while preserving T/X/right-angle shared joins.
4. Root integrates CityStreetNetwork: uses one shared layout for rendered
   surfaces and exact shared MeshCollider; no redundant square junction patches.
   Root fixes FindJunctions, updates stale endpoint test, implements runtime
   capture/validation, runs Unity tests/build/Player and image review.

Root uses pavement bottom .030/top .058 and sidewalk .045/.085 to preserve
existing top surfaces. Mesh local vertices carry catalog coordinates; surface
GameObjects have identity transform under CityStreets. Same mesh is assigned
to MeshFilter and MeshCollider. Root merges prisms into two material groups,
RoadPavement_Surface and CitySidewalk_Surface. The combined mesh is registered
with the owner; temporary prism meshes are destroyed. No subsequent static
batch rewrite may replace only the MeshFilter and break the shared reference.
Root owns materials; existing lane marks still use their source transforms.
Lane mark rendering follows existing source segment transforms for now.
Simulation speed remains with existing game clock; no dt/speed handling added.
HTML entry point and DOM IDs are not applicable to this Unity project.

## Agent boundaries and gates

Read dependencies but edit only the assigned file. No Unity/player/browser/
server launches, no nested agents, no send_message/followup_task/list_agents.
C# is compiled by root with Unity; node --check is not applicable to C#.
Return concrete source changes and limitations promptly. Root waits >=120000ms
and directly integrates and fixes QA. Do not claim any tests or visuals ran.

Root preserves all unrelated dirty files, obtains fresh XML/build/captures,
opens images and commits/pushes only the explicit tranche allowlist. No FPS,
whole-Phase-1, NavMesh/traffic final, save migration or Steam acceptance claim
without the corresponding original objective gate.
