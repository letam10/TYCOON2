# TYCOON2 world tranche 1B — junction discovery and road patches

## Status

- Tranche: 1B — shared junction detection and street intersection patches.
- Date: 2026-10-20.
- Branch: `main`.
- Parent source tranche: `cd79d3d`.
- Stable POI IDs, economy, transaction authority and SaveData v2/v3 contracts are unchanged.

## Changes

1. `WorldRouteGeometry.FindJunctions` detects route bends, shared endpoints and XZ segment
   intersections from the shared catalog.
2. Junctions are merged inside a deterministic tolerance and retain the largest connected route
   width for patch sizing.
3. `CityStreetNetwork.Build` creates a square pavement patch at every catalog junction before
   static batching, reducing visible seams between rotated road segments.
4. `WorldRouteGeometryTests` verifies finite junctions and the known main/west intersection.

## Verification

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 `
  -Task Tests -TestFilter 'Tycoon.Tests.WorldRouteGeometryTests'
```

Result: Unity exited with code `0` after the junction changes.

No Windows build, Player launch, NavMesh bake, visual capture, benchmark or soak was run.

## Remaining Phase 1 work

- Trim sidewalk segment ends at junctions and add explicit overlap/self-intersection diagnostics.
- Replace remaining independent access-path visual sampling where it duplicates catalog geometry.
- Verify road/sidewalk/collider continuity in a fresh Windows Player capture.
- Do not claim Phase 1 complete until the geometry checks and Player evidence are fresh.
