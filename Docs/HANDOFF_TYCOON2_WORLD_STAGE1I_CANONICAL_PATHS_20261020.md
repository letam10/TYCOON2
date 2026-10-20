# Phase 1I - canonical sampled path consumers and closed bevel joins

Current checkpoint: catalog path visuals, route triggers, pedestrian graph,
reservation/projection, route clearance and map edges share sampled points.
Overall Phase 1 and the full redesign remain OPEN.
Source base: main 633482c82a3fb022873e1bea1e3a7637dbbe15a0, matching fetched origin.
Read work/tycoon2-plan-20261020/objective-recovered.md before continuing.
Original attachment and eleven reference images remain missing. Exact recovered
objective/provenance is unchanged; no scope reduction is implied.

## Shared architecture and changes

WorldRouteCorridor and WorldPublicApproach keep all original Id/Nodes/Width.
SampledNodes is copied, immutable, cached at construction and retains exact
anchors. Two-node routes retain the same immutable two-point list. Longer
routes use the existing bounded CreateSmoothPath with the single geometry
constant RouteSubdivisionsPerSpan=4. This does not establish true arc rounding.

CityAccessPaths consumes those samples, including public approaches formerly
sampled with 3 subdivisions. Signs keep Nodes[0]. Bespoke landmark approach
curves remain outside the catalog, but now use the same pavement polygon API.
WorldRouteGeometry.Segments overloads, CityPedestrianNetwork graph/classifiers/
restricted-corridor queries, catalog reservations/nearest points, diagnostics,
CityDressing clearance and WorldVegetationCatalog clearance consume samples.
Authored-node validation remains raw so invalid input cannot be hidden.
CityMapGraphic draws each sampled edge once and no longer adds a separate
quadratic overlay. Existing viewport clipping/POIs/map interaction are retained.

Diagnostics previously classified nearby fragments of a sampled crossing as
independent clearance overlaps. Real crossing/shared-endpoint neighborhoods
now classify nearby fragments of that junction consistently. Crossing radius
is the sum of half-widths / sin(crossing angle); shared endpoint radius is the
sum of half-widths. Collinear projection overlap is still rejected before this
classification. Regression tests cover a divided cross, collinear continuation,
near miss without crossing, and remote parallel overlap despite a real crossing.
This remains planar diagnostics, not grade-separated topology acceptance.

First fresh Player images exposed open seams between rotated path boxes. Root
repaired them before delivery: WorldStreetGeometry.BuildPavement accepts already
sampled points, unions segment rectangles plus bounded bevel triangles at joins
and loop closure, and emits non-overlapping convex pieces. The ordinary street
layout also reads SampledNodes. CityAccessPaths builds closed WorldStreetMesh
prisms from the pieces; CityStaticGeometry owns/merges/releases those meshes.
Catalog visual bottom/top remain .0005/.0555 m, bespoke paths 0/.05 m. Trigger
centers, rotations, widths, lengths and trigger policy are unchanged. No additional
blocking path collider is introduced. These bevels are not the final arc corners.

Root wrote the contract and shared APIs first, then three agents independently
changed path/map consumers, pedestrian consumers and sampling tests. Root reviewed
and integrated all changes, repaired diagnostics and visual seams, and ran Unity.
No economy/save schema, stable POI/route ID, camera control or authored node moved.

## Fresh evidence and limitations

Artifacts: work/tycoon2-plan-20261020/phase1i/. CONTRACT.md and acceptance.json are
committed; large build/XML/log/image outputs remain local and ignored.

- after.xml: 86 total, 84 passed, 2 failed. One test helper recognized only old
  sidewalk anchors; it now recognizes actual sampled sidewalk points. The other
  failure exposed fragmented-crossing diagnostics, repaired as described above.
- after-final.xml: 91/91 after those repairs, before the mesh seam repair.
- after-mesh.xml: 93/93, 0 failed/skipped, Unity exit 0. Started 07:00:36Z,
  finished 07:00:37Z on 2026-10-20. Includes 19 WorldRouteSamplingTests,
  22 WorldStreetSurfaceTests, 14 WorldRouteGeometryTests, corridor/pedestrian/
  traffic/transition/vegetation/path/access/minimap suites. Some filter names
  match no test in this checkout; the XML is authoritative for executed suites.
- Numeric tests independently check curved samples and strip reservation outside
  old chords. Bevel tests verify a 78 m2 L union, exactly one cover at the former
  outer seam and inner overlap, no spike beyond the bevel, and closed-loop joins.
- PlayerFinal/build-report.json: Succeeded 2026-10-20T07:01:53.9284592Z,
  117137471 bytes, 29.2922049 s, 0 errors, 34 warnings, Unity exit 0. Existing
  obsolete API/licensing warnings remain; no claim that editor logs are clean.
- world-features-final/world-features-report.json: passed true, Player exit 0,
  4159 checks, 34 interactions, 69 PNGs, 0 runtime errors. Started
  07:02:37.4809135Z, finished 07:03:12.0420561Z, D3D11 RTX 4060 Laptop GPU.
  Exception/assertion/error scan has 0 matches. This is not a performance run.
- sampled-routes.json: 17 catalog paths/approaches, 104 authored anchors,
  362 sampled nodes, 337/337 live route trigger poses matched one-to-one.
  Public village graph trip retains 25 points. Rail visual geometry is handled
  separately and is explicitly excluded from this trigger count.
- Opened first farm/industrial/marina captures and map-1920. After mesh repair,
  opened all three fresh captures plus map-2560 and minimap-clipped-2560.
  Former transverse box seams in the farm/industrial views are repaired. Cross-
  category material overlaps/notches near the industrial highway/paving boundary
  and physical marina/rail detailing remain unfinished, not silently accepted as
  final road/boundary quality. These focus captures do not prove every district.
- Final runtime DLL SHA256:
  80325053D2C4A82F5169948B07490361A0A7CD6438C5EE2BFE3DAC14752F5393.

## Reproduction

Unity: C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe.
Launch hidden, one editor per checkout, isolated absolute outputs.

```text
-batchmode -nographics -projectPath D:\GAME\TYCOON2 -runTests -testPlatform EditMode
-testFilter Tycoon.Tests.WorldRouteSamplingTests|Tycoon.Tests.WorldRouteGeometryTests|Tycoon.Tests.WorldRouteCorridorTests|Tycoon.Tests.CityAccessPathsSmoothTests|Tycoon.Tests.CityAccessPathsTests|Tycoon.Tests.CityPedestrianPublicRouteTests|Tycoon.Tests.CityPedestrianRouteContractTests|Tycoon.Tests.Stage3RouteTrafficContractTests|Tycoon.Tests.Stage6RouteTransitionTests|Tycoon.Tests.WorldExpansionRouteAccessTests|Tycoon.Tests.Stage5WaterVegetationTests|Tycoon.Tests.Stage6VegetationEzTreeTests|Tycoon.Tests.WorldStreetSurfaceTests|Tycoon.Tests.MinimapViewportTests
-testResults <phase1i>\after-mesh.xml -logFile <phase1i>\after-mesh.log
```

No -quit for runTests. Build with -batchmode -nographics -quit -buildTarget
StandaloneWindows64 -executeMethod Tycoon.Editor.ProjectBuilder.BuildWindows,
-projectPath this checkout, --build-output <phase1i>\PlayerFinal\TYCOON2.exe,
-logFile <phase1i>\build-final.log.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_world_features.ps1 -BuildPath 'work\tycoon2-plan-20261020\phase1i\PlayerFinal\TYCOON2.exe' -OutputPath 'work\tycoon2-plan-20261020\phase1i\world-features-final'
```

## Preservation and next work

All 101 pre-existing tracked dirty files match phase1g/dirty-before.json SHA256.
All unrelated untracked assets/QA files remain. Default build and old QA reports
are preserved. Unity's own TimeManager.asset serialization change was restored
after Unity closed; git diff confirms it is clean. No Unity/Player remains.

Continue Phase 1: true bounded round corners, one material/ownership policy for
intersecting corridor categories, all seam/capsule traversal, rail/highway/dock
boundaries and physical track/traffic/map alignment. Bespoke landmark geometry
still needs authoritative catalog identity. FindJunctions retains authored-node
semantics; full grade separation is unresolved. Then Phase 2 NavMesh exclusion,
pedestrian/traffic behavior and interaction pad removal; later interiors/camera,
licensed asset/environment work, interactive map/HUD, old-save compatibility
and short 1080p/1440p performance acceptance retain their original scope.
No full regression, FPS, full old-save or Steam acceptance claim.
