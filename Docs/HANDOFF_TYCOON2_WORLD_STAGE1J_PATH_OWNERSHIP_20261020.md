# Phase 1J — deterministic path ownership and matching collision

Status: scoped Phase 1J gates passed. Overall Phase 1 and the full redesign are OPEN.
Source base: main e34125d46a747270b1798f5c377221449e37155a, matching fetched origin/main.
Read work/tycoon2-plan-20261020/objective-recovered.md before continuing. Original
attachment and eleven reference images are still missing; the recovered complete
objective was reread. No later requirement has been removed or marked complete.

## Final implementation

WorldPathSurfaceRequest is an immutable copy of an already-sampled path, width,
stable request ID, material/tint, top/bottom and priority. Layout keeps all requests,
selected polygon pieces and protected street footprints. WorldPathSurfaceState
exposes the same live layout to rendering, collision and map consumers.

WorldPathSurfaceCatalog includes 12 nonrail catalog routes and four public
approaches. CityAccessPaths collects these and the existing nonzero bespoke
landmark legs before rendering. Actual world has 28 requests, including 12
landmark legs. Landmark IDs use landmark:<existingId>:<leg>; existing gameplay,
POI, station, route, save and trigger IDs were not renamed. Existing authored
points, sampled points, path widths, quadratic landmark bends and trigger poses
are preserved. Rail visuals retain their separate existing builder.

WorldPathSurfaceGeometry sorts priority descending then ordinal ID. Protected
street pavement and sidewalk polygons win first, followed by Dock 400, Highway
300, Sidewalk/landmark 200 and FarmPath 100. WorldStreetGeometry.SubtractSurfaces
subtracts earlier owners at the exact same source base height and preserves
route/segment/side metadata with deterministic piece indices. Fully hidden
requests remain in the layout: sidewalk-crossings is currently fully covered by
protected street geometry and therefore has no separate path polygons.

This is flat geometry ownership. Different source heights do not subtract and
sloped sources are rejected; this does not implement graded road intersections.

WorldPathSurfaceMesh groups pieces by material/tint/top/bottom, combines closed
prisms and gives each identity-pose child the exact same Mesh for MeshFilter and
nontrigger MeshCollider. Temporary meshes are destroyed, combined meshes are
registered in WorldStreetMeshOwner. No static batching changes mesh identity.
The old route/landmark triggers remain separate from walkable collision.

CityMapGraphic draws generated street polygons and reads the exact live path
layout for nonrail path polygons. Its ancestor RectMask2D, pins/captions/POI
behavior and existing rail/fallback drawing remain. This does not implement the
later interactive zoom/pan/rotation/filter/tooltip map redesign.

## Root integration repairs from fresh failures

- after-integration.xml: 108 total, 106 passed, 2 failed. Current catalog exposed
  a tiny reflex vertex after converting clipped double coordinates to Unity
  floats; the mesh lifecycle test also found Editor cleanup was not invoked.
- WorldStreetMeshOwner now has ExecuteAlways, matching the existing dining mesh
  lifetime pattern. after-diagnostics.xml: 15 total, 14 passed; cleanup passed,
  only the polygon diagnostic remained. Exact cross was -1.65135134011507E-07 m2
  on expansion-village-road segment 21.
- Generated float outlines now receive a deterministic monotone-chain convex
  hull before mesh output. Input clipping still rejects genuinely concave
  external polygons; an independent new regression checks that restriction.
- after-repair.xml: 109 total, 108 passed. A 1.5832483768463135E-07 m2 sliver
  remained. Minimum generated area is now 1E-06 m2 (one square millimetre), so
  numerically degenerate fragments do not become unstable collider triangles.
- QaStreetSurfaces selects CityStreets explicitly now that another mesh owner
  exists. QaPathSurfaceOwnership is wired into WorldFeaturesAcceptance and
  requires both map consumers, exact mesh/pose, per-polygon live collider
  coverage and independent double-precision intersection checks.

## Fresh final gates

Evidence root: work/tycoon2-plan-20261020/phase1j/. CONTRACT.md and acceptance.json
are committed; large build/log/XML/PNG outputs remain local and ignored.

- after-degenerate.xml: 109/109 passed, 0 failed/skipped, Unity exit 0.
  Start 2026-10-20 07:44:27Z, end 07:44:29Z. This includes 16 WorldPathSurface
  cases plus the prior 93 route/street/sampling/pedestrian/access/vegetation/map
  cases. XML is authoritative; this is a focused run, not the full regression.
- Player/build-report.json: Succeeded at 2026-10-20T07:45:34.5903736Z, 117156623
  bytes, 39.4503396 seconds, 0 errors, 34 warnings, Unity exit 0.
- world-features/world-features-report.json: passed true, Player exit 0,
  4987 checks, 35 interactions, 72 PNGs, 0 runtime errors. Started
  2026-10-20T07:46:10.3881817Z, finished 07:46:45.4592938Z.
- path-surface-ownership.json: 28 requests, 789 convex pieces, 6 mesh batches,
  789/789 live collider centroid samples matched. Both map consumers share the
  exact layout reference. Maximum measured path overlap after float quantization
  is 0.00025512068532407284 m2; maximum protected-street overlap is 0. The oracle
  threshold is 0.005 m2. Do not describe measured overlap as exact zero.
- sampled-routes.json: 17 routes/approaches, 337/337 live trigger poses matched,
  public pedestrian trip remains 25 points. Rail visual geometry is excluded
  from this trigger count.
- Opened all eight fresh images: path-owner-industrial, path-owner-marina,
  path-owner-ring, sampled-route-farm, sampled-route-industrial,
  sampled-route-marina, map-2560 and minimap-clipped-2560. None is blank/magenta;
  minimap remains inside its mask. These do not constitute every-district visual
  or complete junction acceptance. The highway/sidewalk authored separation,
  faceted corners and marina/dock rail detailing remain visible and open.
- WORLD-FEATURES_QA PASS is in the fresh log; exception/assertion/error scan has
  no matches. No benchmark, GPU timing, soak or long campaign was run.
- Runtime DLL SHA256:
  7E7A157DA090374F8D136DF6A6437D7BF79C4BAFECE40BAD89B9D00F1CDC63F3.

## Reproduction

Unity executable: C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe.
Run hidden and one Editor per checkout. Test arguments (no -quit):

```text
-batchmode -nographics -buildTarget StandaloneWindows64 -projectPath D:/GAME/TYCOON2
-runTests -testPlatform EditMode
-testFilter Tycoon.Tests.WorldPathSurfaceTests|Tycoon.Tests.WorldRouteSamplingTests|Tycoon.Tests.WorldRouteGeometryTests|Tycoon.Tests.WorldRouteCorridorTests|Tycoon.Tests.CityAccessPathsSmoothTests|Tycoon.Tests.CityAccessPathsTests|Tycoon.Tests.CityPedestrianPublicRouteTests|Tycoon.Tests.CityPedestrianRouteContractTests|Tycoon.Tests.Stage3RouteTrafficContractTests|Tycoon.Tests.Stage6RouteTransitionTests|Tycoon.Tests.WorldExpansionRouteAccessTests|Tycoon.Tests.Stage5WaterVegetationTests|Tycoon.Tests.Stage6VegetationEzTreeTests|Tycoon.Tests.WorldStreetSurfaceTests|Tycoon.Tests.MinimapViewportTests
-testResults <phase1j>/after-degenerate.xml -logFile <phase1j>/after-degenerate.log
```

Build uses -batchmode -nographics -quit -buildTarget StandaloneWindows64,
-projectPath this checkout, -executeMethod Tycoon.Editor.ProjectBuilder.BuildWindows,
--build-output <phase1j>/Player/TYCOON2.exe and -logFile <phase1j>/build.log.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_world_features.ps1 -BuildPath 'work\tycoon2-plan-20261020\phase1j\Player\TYCOON2.exe' -OutputPath 'work\tycoon2-plan-20261020\phase1j\world-features'
```

## Preservation and continuation

All 101 original dirty tracked files match phase1g/dirty-before.json SHA256.
Unrelated downloads, asset metadata, old QA evidence and saves remain preserved.
Default Builds/Windows and historical QA outputs were not overwritten. Unity's
TimeManager serialization delta was restored after processes closed; diff is
clean. No Unity/Player remains. Stage only the exact Phase 1J allowlist.

Next within Phase 1: implement true bounded round corners and continuous shared
junction boundaries; resolve authored highway/sidewalk gaps, marina rail/dock
physical detailing, full sampled traffic/rail alignment and actual capsule
traversal across every seam. Preserve all IDs/economy/save contracts. Phase 2
still includes NavMesh exclusions/connectivity, pedestrian signals, traffic and
hidden logical interactions; later phases still include real interiors, camera
occlusion, assets/PBR/biomes/water, interactive map/HUD, additive save compatibility
and short 1080p/1440p performance acceptance. No full old-save, stable 60 FPS or
Steam acceptance claim follows from this checkpoint.
