# TYCOON2 world tranche 1D — sidewalk trimming and route diagnostics

## Status

- Tranche: 1D — junction sidewalk endpoint trimming and self-intersection validation.
- Date: 2026-10-20.
- Branch: `main`.
- Parent tranche: `41a7deb`.
- Stable POI IDs, economy, transaction authority and SaveData v2/v3 contracts are unchanged.

## Changes

1. `WorldRouteGeometry.TrimAtJunctions` shortens sidewalk segments at shared junction
   endpoints while preserving route width and orientation.
2. `CityStreetNetwork` now uses trimmed sidewalk geometry while keeping road pavement and
   lane marks on the full segment.
3. `WorldRouteGeometry.ValidateNoUnexpectedSelfIntersections` rejects non-adjacent
   self-intersections while allowing ordinary connected turns and closed-loop endpoints.
4. EditMode coverage verifies sidewalk trimming and the default route catalog's self-intersection
   contract.

## Verification

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 `
  -Task Tests -TestFilter 'Tycoon.Tests.WorldRouteGeometryTests'
```

The first run after the edit found compiler error `CS0841` because the junction list was
declared after its use. The declaration was moved before the builder loop, and the fresh rerun
completed with Unity exit code `0`.

No Windows build, Player launch, NavMesh bake, visual capture, benchmark or soak was run.

## Remaining Phase 1 gate

- Extend diagnostics to cross-route overlap classification and rail/highway/dock boundary cases.
- Run a fresh Windows build and Player capture for road, sidewalk and collider alignment.
- Keep pedestrian route, traffic lane and NavMesh synchronization in Phase 2.
