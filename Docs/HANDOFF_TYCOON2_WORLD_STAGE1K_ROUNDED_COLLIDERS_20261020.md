# TYCOON2 — Phase 1K rounded routes and collider cooking handoff

Updated: 2026-10-20

Status: Phase 1K focused gates passed. The full world redesign remains open.

Source base before this tranche: `1872c1cd61b90eb11ddc1c11e810569a42e969fb`,
matching `origin/main` before the tranche. The recovered complete objective is
`work/tycoon2-plan-20261020/objective-recovered.md`; the original attachment
path remains unavailable. Preserve the 101 pre-existing tracked dirty files and
all unrelated downloaded/untracked assets. No reset, clean, stash, or broad
stage was used.

## Delivered in this tranche

`WorldRouteCorner` stores an authored anchor, tangent entry/exit, circle center,
radius and signed sweep. `WorldRouteRounding` creates bounded circular fillets
for finite horizontal routes, preserves exact open endpoints, rounds closed-loop
closing corners, samples even arc segments with maximum sagitta below 0.02 m,
and retains the existing bounded Hermite sampler for graded routes.

`WorldRouteCatalog` keeps all authored IDs, raw nodes, widths, kinds and
reservation flags. The four street-ring IDs now use exact cyclic sections from
the shared `highway-ring` sampled loop, including the authored reverse directions
for east and south. Public five-argument corridor and approach constructors keep
their public contract; the internal canonical-sample overload copies immutable
samples.

`WorldStreetGeometry` adds bounded finite join patches at sampled bends and
coincident endpoints across street IDs. Pavement and sidewalk clipping remains
height-aware and metadata-preserving. Generated outlines are quantized through a
deterministic convex hull. Near-duplicate float vertices within 0.0001 m are
collapsed, and quantized fragments at or below 0.00001 m² are discarded so Unity
MeshCollider does not silently drop degenerate triangles.

`WorldRouteGeometry.FindJunctions` uses sampled edges, skips adjacent edges from
the same route including closed-loop adjacency, aggregates one logical bend at
the real arc midpoint, and preserves grade-separated routes. Public approach
classification now uses sampled width and matching height. Authored shared-anchor
neighborhoods remain valid junctions after rounding. Traffic signal IDs and
cycle offsets are unchanged; poses project to the sampled ring with a 6.2 m
shoulder distance.

`QaRoundedRoutes` and `WorldRoundedIntegrationTests` cover live arc support,
circle radius and chord error, ring sections, traffic shoulder distance, public
approach classification, and every current generated path polygon against an
actual cooked MeshCollider. The existing QA pipeline now runs rounded-route
acceptance before path ownership and map checks.

## Fresh evidence

- EditMode affected run: `after-area-filter.xml`, 137 total, 137 passed, 0
  failed, 0 skipped, Unity exit 0, 2026-10-20 10:02:47–10:02:51Z.
- Focused collider regression: `collider-repro-after-area.xml`, 1/1 passed,
  after reproducing two float-quantized degenerate polygon failures.
- Windows build: `phase1k/Player/build-report.json`, `Succeeded`,
  117169759 bytes, 20.3673146 seconds, 0 errors, 34 warnings, built
  2026-10-20 10:07:19.2769184Z.
- Player QA: `world-features-after-area-filter/world-features-report.json`,
  passed true, 5928 checks, 1 interaction, 0 runtime errors, 78 PNGs,
  2026-10-20 10:08:41.2740614–10:09:16.3026984Z, Player exit 0 and
  `WORLD-FEATURES_QA PASS` in the fresh log.
- Rounded manifest: 62 logical corners, 270 arc edges, maximum radius error
  0.00003528595 m, maximum measured chord error 0.0194773674 m, 108 live
  pavement/sidewalk support samples.
- Sampled route manifest: 355 matched route colliders and 355 live colliders.
- Street surface manifest: 508 collider samples.
- Path ownership manifest: 854 live polygon collider samples, maximum path
  overlap 0.0008839662 m² and maximum street overlap 0.0000049942 m². These
  are below the 0.005 m² oracle; they are not claimed to be exact zero.
- Fresh images opened: rounded ring southwest/northwest/northeast/southeast,
  rounded industrial, rounded marina, path-owner views, street junction views,
  sampled-route views, map-2560 and clipped minimaps. They were nonblank and
  nonmagenta; the rounded ring and sidewalks were visually continuous.
- The 101 pre-existing tracked dirty-file hashes still match
  `phase1g/dirty-before.json`. Unity processes are closed and
  `ProjectSettings/TimeManager.asset` has no diff.

## Reproduction

Unity: `C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe`.

The affected test filter is the union of `WorldRoundedIntegrationTests`,
`WorldRouteRoundingTests`, path ownership, route sampling, route geometry,
route corridor, street surface, pedestrian, traffic, expansion, vegetation and
minimap contracts. The authoritative result is the XML, not the wrapper text.

Build command:

```text
-batchmode -nographics -quit -buildTarget StandaloneWindows64
-projectPath D:/GAME/TYCOON2
-executeMethod Tycoon.Editor.ProjectBuilder.BuildWindows
--build-output work/tycoon2-plan-20261020/phase1k/Player/TYCOON2.exe
-logFile work/tycoon2-plan-20261020/phase1k/build-after-area-filter.log
```

Player command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_world_features.ps1 `
  -BuildPath 'work\tycoon2-plan-20261020\phase1k\Player\TYCOON2.exe' `
  -OutputPath 'work\tycoon2-plan-20261020\phase1k\world-features-after-area-filter'
```

## Remaining scope and acceptance boundary

This handoff proves the bounded Phase 1K geometry/collider tranche only. It does
not prove the complete objective, native Steam performance, old-save matrix,
all-district visual acceptance, every capsule traversal, or full Phase 2–7.

Still open: complete NavMesh exclusions and connectivity, traffic alignment and
signals across all corridors, hidden 0.25-second interaction regions, hero
interiors and cutaways, camera clipping/fade behavior, PBR and licensed asset
catalogues, seeded biomes and LOD/clearance, shoreline and water polish,
interactive map zoom/pan/rotation/filter/tooltips/gamepad, additive save
migration, and short 1080p/1440p performance evidence. The authored highway /
sidewalk separation and marina detailing need later visual work even though the
rounded geometry gate is now green.

Next authorized tranche: stage only the Phase 1K source, tests, runtime QA,
contract and this handoff file; commit and push after the exact allowlist and
remote-head verification. Do not stage downloaded assets, old QA evidence,
Unity-generated unrelated metadata, or the 101 pre-existing dirty files.
