# TYCOON2 world tranche 1A — route catalog geometry foundation

## Status

- Tranche: 1A — shared street-network catalog and rotated segment geometry.
- Date: 2026-10-20.
- Branch: `main`.
- Stable POI IDs, economy, transaction authority and SaveData v2/v3 contracts are unchanged.
- Existing dirty and untracked worktree paths were preserved.

## Changes

1. `WorldRouteCatalog` now exposes a separate `BuildStreetNetwork()` catalog containing the
   22 legacy street segments that were previously hard-coded inside `CityStreetNetwork`.
2. Street-network route IDs, widths, node endpoints, route kind and placement reservation now
   live in the route catalog source file.
3. `WorldRouteGeometry` provides one segment representation with center, forward, rotation,
   length, width and oriented bounds.
4. `CityStreetNetwork.Bounds()` and `CityStreetNetwork.Build()` consume the catalog and the
   shared segment geometry. Road pavement, sidewalks and lane marks use the segment rotation.
5. `WorldRouteCatalog.IsReserved()` includes the street-network catalog while preserving the
   original 13 `BuildDefault()` corridor contract.
6. `WorldRouteGeometryTests` covers the new catalog count, route validation, diagonal rotation,
   oriented bounds, segment-to-bounds count and street reservation.

## Verification

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 `
  -Task Tests -TestFilter 'Tycoon.Tests.WorldRouteGeometryTests'
```

Result: Unity exited with code `0` after the initial compile error was repaired by renaming
an internal helper that conflicted with the `IsFinite` property. The initial failed run is
kept as diagnostic evidence; it is not used as a pass claim.

Scoped source checks:

- `git diff --check` was run; unrelated pre-existing imported `.meta` files contain whitespace
  warnings and were not edited or staged.
- No Windows build, Player launch, NavMesh bake, visual capture, benchmark or soak was run in
  this tranche.

## Remaining work

- Replace independent `CityAccessPaths.CreateSmoothPath` visual segments with the shared sampled
  geometry where appropriate.
- Add junction patches, bounded corner trimming and explicit overlap/self-intersection checks.
- Synchronize traffic lanes, pedestrian route sampling and NavMesh generation in Phase 2.
- Add runtime evidence for road/sidewalk continuity and collider alignment.

## Commit and push

The exact source commit is recorded after the scoped commit is created. Only the files listed
in this handoff are eligible for that commit; unrelated dirty and untracked paths remain out
of scope.
