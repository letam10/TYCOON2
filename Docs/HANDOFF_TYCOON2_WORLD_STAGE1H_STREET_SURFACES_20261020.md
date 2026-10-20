# Phase 1H — shared street surface polygons

Status: current 22-street pavement/sidewalk union and matching colliders verified.
Overall Phase 1 and full redesign remain OPEN.
Source base: main 231763d8e3368a0f34bf9efbab8cb6c1d4c033c1.
Read work/tycoon2-plan-20261020/objective-recovered.md before continuing.
The original attachment and eleven reference images remain missing; objective
provenance is recorded in the Phase 1F handoff. No scope reduction is implied.

## Source changes

WorldStreetSurface.cs defines immutable surface outlines and source route IDs.
WorldStreetGeometry.Build creates exact oriented road rectangles and sidewalk
strips, then subtracts convex polygons by successive half-plane splits.
Pavement pieces exclude earlier road footprints; sidewalk pieces exclude every
road footprint and previously accepted sidewalk pieces. This handles crossings
inside source segments and removes coplanar interior overlap at corners.
It uses double precision during clipping, removes degenerate points/polygons,
preserves winding and deterministic piece order, and converts final vertices
to Unity floats. Inputs at different exact heights do not subtract each other.
Non-flat segments are explicitly rejected rather than silently flattened.

WorldStreetMesh.CreatePrism builds closed top/bottom/side faces with correct
normals and metre UVs. CityStreetNetwork merges prisms into two material groups:
RoadPavement_Surface and CitySidewalk_Surface. Each has identity local transform
and uses the exact same Mesh for MeshFilter and MeshCollider. Runtime meshes are
owned and cleaned by WorldStreetMeshOwner. Temporary prism meshes are released.

Existing top heights are retained: pavement .058 m, sidewalk .085 m; bottoms
are .030/.045. Sidewalk width stays 1.8 m, offset roadWidth/2 + .9 m.
The prior square junction overlays and endpoint-only sidewalk trim are replaced
by the actual polygon union. FindJunctions no longer treats collinear touching
segments as junctions. The old endpoint test now uses the west connector with
real intersections at both ends, instead of a collinear ring continuation.

First fresh Player images exposed crossing center dashes at intersections.
Final lane mark placement skips the junction half-width plus the 1.4 m dash
half-length. All fresh tests/build/Player captures were repeated after this fix.
No catalog route IDs/nodes/widths, economy/save schemas or camera controls changed.

Root wrote the foundation and contract before three agents independently
implemented polygon geometry, prism meshes and tests. Root integrated, repaired
junction classification, implemented runtime QA and performed every Unity gate.

## Evidence

Root: work/tycoon2-plan-20261020/phase1h/.
acceptance.json is the committed machine-readable summary; bulky build/capture
evidence is local and ignored. PlayerFinal/TYCOON2.exe is this checkpoint build.

- before.xml: 34 total, 31 passed, 3 failed, Unity exit 2. All failures were
  false collinear junctions in either endpoint order or a straight polyline.
- after.xml: 48/48 passed before the visual lane mark adjustment.
- after-final.xml: 48/48 passed, zero failed/skipped, Unity exit 0,
  2026-10-20 06:30:19Z–06:30:20Z. Suites: WorldStreetSurfaceTests 20,
  WorldRouteGeometryTests 14, WorldRouteCorridorTests 6,
  MinimapViewportTests 3, Stage3RouteTrafficContractTests 5.
- Geometry tests independently check analytic union areas, oblique crossings,
  overlap area, grid coverage, collinear continuity, deterministic outlines,
  current catalog coverage, separate heights and closed outward prism faces.
- PlayerFinal/build-report.json: Succeeded at 2026-10-20T06:30:51.4762866Z,
  117131839 bytes, 22.942135 s, 0 errors, 34 warnings, Unity exit 0.
  Existing obsolete API/licensing messages remain; editor log is not error-free.
- world-features-final/world-features-report.json: passed true, exit 0,
  3815 checks, 33 interactions, 66 PNG captures, 0 runtime errors.
  Started 06:31:02.8303389Z, finished 06:31:35.0667901Z, D3D11 RTX 4060 Laptop.
  Exception/assertion/error scan: zero matches.
- street-surfaces.json: 22 source routes, 37 pavement pieces, 61 sidewalk
  pieces, 19290.5 square metres pavement, 8768.888671875 square metres sidewalk.
  98 independent collider ray samples hit the expected rendered top height.
- Opened all three first-run and all three final captures: street-surface-
  collinear.png, street-surface-tee.png and street-surface-cross.png. Final
  pictures show continuous asphalt, clipped sidewalk corners and no crossing
  center dashes. This is acceptance of those views, not every world district.

Final runtime DLL SHA256:
6AFE511F94AA1DCE099F0011FA122E5A0F6A03E178958F87A7450A1E23C010C1.

## Reproduction

Unity executable: C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe.
Launch hidden, one editor per checkout, with isolated absolute output paths.

```text
-batchmode -nographics -projectPath D:\GAME\TYCOON2 -runTests -testPlatform EditMode
-testFilter Tycoon.Tests.WorldStreetSurfaceTests|Tycoon.Tests.WorldRouteGeometryTests|Tycoon.Tests.WorldRouteCorridorTests|Tycoon.Tests.MinimapViewportTests|Tycoon.Tests.Stage3RouteTrafficContractTests
-testResults <phase1h>\after-final.xml -logFile <phase1h>\after-final.log
```

Do not add -quit to runTests. Build with -batchmode -nographics -quit
-buildTarget StandaloneWindows64 -executeMethod Tycoon.Editor.ProjectBuilder.BuildWindows,
projectPath this checkout, --build-output <phase1h>\PlayerFinal\TYCOON2.exe,
and -logFile <phase1h>\build-final.log.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_world_features.ps1 -BuildPath 'work\tycoon2-plan-20261020\phase1h\PlayerFinal\TYCOON2.exe' -OutputPath 'work\tycoon2-plan-20261020\phase1h\world-features-final'
```

## Preservation and next work

Fetched main at start; it matched the source base. All 101 pre-existing tracked
dirty files match the SHA256 snapshot from phase1g/dirty-before.json. Preserve
them and all unrelated untracked assets/QA files. Unity reserialized the clean
TimeManager.asset during testing; only that agent-generated formatting change
was restored. Default delivery executable and pre-existing QA reports preserved.

Do not close Phase 1 yet. Continue shared sampled curved routes across access
paths, mesh/collider, map and graph consumers; bounded rounded corner geometry,
complete seam/capsule traversal, and rail/highway/dock visual boundaries remain.
FindJunctions and older diagnostics remain planar; full grade-separated route
semantics are not established by the flat street generator's height isolation.

Then implement original Phase 2 NavMesh/pedestrian/traffic behavior. Current
NavMesh smoke is not proof that roads/water/buildings are excluded as required,
nor that every traffic/map consumer uses the final sampled source. Later
interiors/camera, licensed assets, interactive map/HUD, save compatibility and
short 1080p/1440p performance acceptance retain their original scope.
No FPS, full regression suite, complete old-save or Steam acceptance claim.
