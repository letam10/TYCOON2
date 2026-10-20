# TYCOON2 world tranche 1C — shared sampled route geometry

## Status

- Tranche: 1C — centralized bounded spline sampling and segment construction.
- Date: 2026-10-20.
- Branch: `main`.
- Parent tranche: `1153a9e`.
- Stable POI IDs, economy, transaction authority and SaveData v2/v3 contracts are unchanged.

## Changes

1. `WorldRouteGeometry.CreateSmoothPath` now owns bounded Catmull-Rom sampling and endpoint
   preservation.
2. `WorldRouteGeometry.Segments(points, width)` owns segment length, center, width and rotation
   for sampled access paths.
3. `CityAccessPaths.CreateSmoothPath` remains as a compatibility wrapper for existing callers
   and tests.
4. Access path pavement and trigger colliders now use the shared segment representation instead
   of duplicating delta/rotation/length calculations.

## Verification

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 `
  -Task Tests -TestFilter 'Tycoon.Tests.WorldRouteGeometryTests'
```

Result: Unity exited with code `0` after the refactor.

No Windows build, Player launch, NavMesh bake, visual capture, benchmark or soak was run.

## Remaining Phase 1 work

- Add sidewalk endpoint trimming and explicit self-intersection/overlap diagnostics.
- Extend geometry tests to cover rail, highway and dock route boundaries.
- Run a fresh Windows build and Player visual capture for road, sidewalk and collider alignment.
- Keep traffic lanes, pedestrian route planning and NavMesh synchronization in Phase 2.
