# Phase 1F — route validator regression repair

Status: diagnostics verified; overall Phase 1 and the full redesign remain open.
Source base: `f1fcc291848fdf6390c2a3edd752ab70501dc910` on `main`.

## Recovered objective

The attachment `977c990b-dba2-432f-aedb-93abb62fe160/goal-objective.md`
is missing. Its complete original output was recovered verbatim from this
chat's first successful read, timestamp `2026-10-20T02:44:57.995Z`, ordinal 256.
The durable copy is `work/tycoon2-plan-20261020/objective-recovered.md`, SHA256
`3D1486C87F2E3904566D11758880411388BC9ADD44082378300561A14ABDA037`.
It preserves every original phase and the referenced image paths. The missing
reference images have not been recreated or substituted.

The scope still includes shared route geometry, NavMesh/pedestrian/traffic,
interaction visibility, interiors/cutaway, fixed pitch/yaw camera, assets and
licenses, interactive map/HUD, save compatibility and short 1080p/1440p
performance acceptance. This checkpoint does not reduce that objective.

## Defects reproduced and fixed

1. An open four-node X-shaped polyline was accepted because the validator
   unconditionally skipped its first/last edge pair. Removed that skip; valid
   shared endpoints still follow the existing shared-endpoint exemption.
2. Parallel overlapping corridors sharing a start or end were classified as
   valid shared endpoints. Projection overlap is now checked first, so a
   20 m edge containing a 10 m edge is rejected in either endpoint order.
3. Crossing diagnostics reported the mean of segment centers. They now reuse
   the actual intersection computation in `WorldRouteGeometry`. The asymmetric
   regression requires intersection `(0, 0, -2)`; the old result was about
   5.006 m away.

Only validators, diagnostics and their tests changed. Route IDs/nodes, mesh
builders, economy, save schemas and camera/input policy did not change.

## Fresh evidence

Evidence root: `work/tycoon2-plan-20261020/phase1f/`.

- `before.xml`: one X-polyline regression, failed; Unity exit 2.
- `before-regressions.xml`: 17 total, 13 passed, 4 failed; Unity exit 2.
  Failures are the X route, both collinear endpoint orders and the crossing
  coordinate. Timestamp: `2026-10-20 05:41:14Z`.
- `after.xml`: 36 total, 36 passed, zero failed/skipped; Unity exit 0,
  `2026-10-20 05:44:51Z` to `05:44:52Z`.
  Suites: WorldRouteGeometryTests (14), WorldRouteCorridorTests (6),
  Stage6RouteTransitionTests (3), Stage3RouteTrafficContractTests (5),
  CityPedestrianPublicRouteTests (2), CityPedestrianRouteContractTests (6).
- `Player/build-report.json`: Succeeded at `2026-10-20T05:46:17.1934367Z`,
  117112895 bytes, 37.061 s, 0 errors and 34 warnings. Unity exit 0.
  The editor log also contains a licensing access-token message; it did not
  prevent the successful build. Do not call the entire editor log error-free.
- `world-features/world-features-report.json`: passed true, empty failure,
  3657 checks, 30 interactions, 126.63995 distance walked, 61 PNG captures.
  Started `05:46:44.7931029Z`, finished `05:47:14.7643388Z`, RTX 4060 Laptop GPU.
  All 11 expansion-anchor checks report PathComplete. Player exit 0 and log
  ends with WORLD-FEATURES_QA PASS. Exception/assertion scan found zero matches.
- Runtime assembly SHA256:
  `C21C732320CA48A9EBE8B2A996849F1FA28FE751C7C709D0CF93057339CBEEFF`.

Unity was launched with batchmode/nographics, projectPath set to this checkout,
EditMode testFilter set to the six suites above, and isolated testResults and
logFile paths. Build used `Tycoon.Editor.ProjectBuilder.BuildWindows`,
`-buildTarget StandaloneWindows64` and `--build-output` set to
`work/tycoon2-plan-20261020/phase1f/Player/TYCOON2.exe`.

Player command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_world_features.ps1 -BuildPath 'work\tycoon2-plan-20261020\phase1f\Player\TYCOON2.exe' -OutputPath 'work\tycoon2-plan-20261020\phase1f\world-features'
```

## Visual gate: open

Opened two of the 61 fresh captures: `minimap-1920.png` and
`cutaway-StationCutaway.png`. Both render, but thin route-like geometry spills
outside the lower-left minimap onto world/HUD. Opened the prior Phase 1D
`minimap-1920.png` and confirmed the same artifact was present before this
validator patch. This is an observed visual failure, not visual acceptance.
`CityMapGraphic` is a MaskableGraphic; `GameHud.BuildMap` in `GameHudMap.cs` should be checked
for viewport clipping. The root cause has not yet been proven.

Other Phase 1 work still needs regression and close-up evidence: false junctions
at collinear continuations, sidewalks through interior crossings, continuous
joins/corner geometry, overlap removal and rail/highway/dock visual boundaries.
The new diagnostics are planar and do not yet establish grade-separated route
semantics or detect every possible collinear self-overlap.

## Resume order

1. Read the recovered objective and this handoff. Preserve the unrelated dirty
   asset/meta/QA files. No reset/clean/stash or broad staging.
2. Reproduce and repair the observed map spill and remaining street junction
   geometry. Use targeted regressions, fresh build/captures and image review.
3. Keep Phase 1 open until its full geometry gate is proved, then implement
   Phase 2 from the original objective. Do not substitute passing legacy
   traffic/pedestrian tests for actual shared sampled route data.
4. Continue remaining phases sequentially. Current evidence is not full suite,
   old-save migration acceptance, all-interior acceptance, FPS or Steam proof.

The current test/build/Player outputs are isolated; the delivery executable and
pre-existing QA reports were preserved by these commands.
