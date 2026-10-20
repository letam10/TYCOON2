# Phase 1I - canonical sampled route consumers

Base: main 633482c82a3fb022873e1bea1e3a7637dbbe15a0.
Read ../objective-recovered.md. Overall Phase 1 and redesign remain OPEN.

## Shared data, written by root before delegation

WorldRouteCorridor and WorldPublicApproach retain Id, Nodes, Width and all
authored coordinates. Add public IReadOnlyList<Vector3> SampledNodes { get; }.
The immutable sampled collection is constructed once from the copied Nodes.
Two-anchor routes use the original immutable list. Routes with >=3 anchors
use WorldRouteGeometry.CreateSmoothPath(Nodes, 4). Every authored anchor is
retained exactly, and each sample stays inside its own span endpoint bounds.
No new sampler, per-consumer subdivisions, route ID, width or save field.
This consolidates the existing bounded sampler; it does not establish true
rounded corners, final junction geometry, grade separation or NavMesh proof.

WorldRouteGeometry.Segments(WorldRouteCorridor) and Segments(WorldPublicApproach)
read SampledNodes. Catalog nearest/reservation and public-node classification
read SampledNodes. Raw Nodes remain the authored identity/anchor contract.
There is no HTML/DOM/JS entrypoint in this Unity task. No dt/speed changes.

## Independent agent ownership

1. canonical_path_map: only CityAccessPaths.cs and CityMapGraphic.cs under
Assets/_Game/Scripts/Runtime. Catalog surfaces and route triggers use sampled
nodes. Map draws each sampled edge once; remove its independent CurvedRoute
overlay/helper only if made unused. Signs retain Nodes[0]. Preserve bespoke
landmark Pave paths and all clipping/viewport/POI/UI behavior.
2. canonical_pedestrian: only CityPedestrianNetwork.cs. Build corridor/approach
graph from SampledNodes, recognize these samples in sidewalk/designated-crossing
classification, and use samples for reserved-corridor distance/intersection.
Retain Nodes fixed graph, original anchor positions, endpoint/direct-trip/farm
policy, destination precision, validation and save/economy behavior. No new
pathfinding algorithm or unrelated formatting/refactor.
3. canonical_sampling_tests: only new EditMode/WorldRouteSamplingTests.cs.
Verify immutable/copy behavior, exact anchor retention, bounded finite samples,
two-point semantics, shared segment endpoints/length/width, nearest/reservation
at a genuinely curved span, public sample recognition, and a public graph trip
that follows intermediate samples rather than raw anchor chords. Independent
numeric expectations; do not assert implementation source strings. NUnit
namespace Tycoon.Tests, existing Tycoon.EditModeTests assembly.

Agents may only write assigned files and return results. They may read source.
No Unity/Player/server/browser launch, extra agents, messaging, commits or push.
node --check is not applicable to C#; root owns compilation and test gates.

## Root integration and gates

Root owns shared data, geometry overloads, diagnostics, route clearance in
dressing/vegetation, runtime QA and all integration fixes. Existing invalid
input diagnostics must still see authored nodes. Avoid silently hiding NaNs.
Root inspects every diff, runs bounded affected EditMode tests, builds one
isolated Windows Player, runs the short world-features smoke and opens fresh
route/map captures. No soak or benchmark. Preserve all existing dirty files,
default build and QA output; artifacts stay in this folder. Update handoff,
allowlist stage, diff check, commit and non-force push per saved objective.

Remaining scope includes bespoke landmark paths, physical track/traffic geometry,
fully rounded joins, seam/capsule traversal, Phase 2 navigation exclusion and all
later interior/camera/environment/map/save/performance work. Do not close goal.

## Root visual repair after first Player inspection

First Player passed logic checks but fresh farm/industrial images exposed open
seams between rectangular path pieces. Root must repair before delivery.
WorldStreetGeometry.BuildPavement(string id, IReadOnlyList<Vector3> points,
float width) accepts already sampled flat points without interpolation. It unions
segment rectangles and bounded bevel triangles at internal/closed-loop joins.
Existing street layout also uses SampledNodes. CityAccessPaths reuses this surface
and WorldStreetMesh.CreatePrism for catalog and bespoke path visuals. Trigger
poses/counts and existing surface heights stay unchanged. True arc rounding and
global cross-category intersection ownership remain separate unfinished work.
