# TYCOON2 repair plan and handoff 2026-10-14

## Scope and invariants

- Keep the current 3D map, `CityDistricts.Revision`, area IDs, save slots and migration rules.
- Keep the semi-realistic material and lighting direction already used by the project.
- Do not stage downloaded assets, old QA evidence or unrelated dirty files.
- Functional QA is bounded; no FPS benchmark, soak test or long campaign run is part of this handoff.

## Implemented in this pass

1. `WorldRouteCorridor` and `WorldRouteCatalog` define continuous farm, sidewalk, highway, rail, dock and forest routes.
   Placement reservations are segment based, so scenery is rejected on the route while fixed landmark terminals remain usable.
2. `WorldCutawayVolume` and `WorldCutawayDirector` reveal landmark interiors by hiding upper shell renderers while the player is inside.
   Renderer and object state is restored on exit and optional hysteresis is supported.
3. `CityTrafficSignalSystem` adds green, amber, red and walk phases and `CityTraffic` stops cars at signal states.
4. `WorldFactory` wires cutaway and signal systems into the existing world build without changing save identifiers.
5. Focused EditMode tests cover route continuity, reservation distance, copied nodes and cutaway state restoration.

## Verification completed

- `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.WorldRouteCorridorTests` exited 0.
- `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.WorldCutawayVolumeTests` exited 0.
- `Tools/run_unity.ps1 -Task Build` exited 0 and produced `Builds/Windows/TYCOON2.exe`.
- `Tools/run_world_features.ps1 -BuildPath Builds/Windows/TYCOON2.exe` exited 0 with `WORLD-FEATURES_QA PASS`.

## Follow-up slices

- Add visible Player galleries for eight-direction model pivots, label anchors, interiors and water edge readability.
- Replace remaining direct pedestrian fallback with `TryPlan` diagnostics and route-aware crossing waits.
- Add player boarding areas for boat, car and train using the existing passenger-carrier contract.
- Add road-name signs, map labels, crossing lights and interactable bank, restaurant, forest, beach and taxi volumes.
- Recheck materials, water transparency, roof shell grouping and collision after each visual slice.

## Delivery

The Windows build is ready for manual testing at `D:\GAME\TYCOON2\Builds\Windows`.
The commit contains only this pass source, tests and handoff plan; downloaded libraries and prior evidence remain untouched.

## Continuation verification 2026-10-15

1. `CityPedestrianNetwork.TryPlan` now reuses an exact corridor anchor for an endpoint,
   preventing duplicate zero length waypoints. `CityPedestrianRoute` validates each NavMesh leg
   and keeps a failure reason for callers.
2. `WorldModelAlignmentAudit` and `WorldLabelAnchor` provide bounded checks for renderer bounds,
   grounded pivots, finite transforms and label footprint or facing. The focused audit tests pass.
3. `WorldInteractionRegistry` exposes visit, swim, vessel, train and vehicle interaction areas.
   Fallback rides do not reparent the player into a landmark shell when no passenger carrier exists.
4. The map adds marina, bank, restaurant and forest points. Marina now targets its paved approach,
   which is a valid NavMesh destination; the other points retain their stable IDs and labels.
5. `CityAccessPaths` draws farm dirt, sidewalk, highway, dock and forest corridors with Vietnamese
   route signs. Route corridor triggers remain non blocking so props are not placed on their footprint.

Fresh bounded evidence:\n+
- `Tycoon.Tests.CityPedestrianRouteContractTests` exited 0 after the duplicate-anchor fix.
- `Tycoon.Tests.WorldModelAlignmentAuditTests` exited 0.
- `Tools/run_unity.ps1 -Task Build` exited 0 and refreshed `Builds/Windows/TYCOON2.exe`.
- `Tools/run_world_features.ps1 -BuildPath Builds/Windows/TYCOON2.exe
  -OutputPath work/world-features-current-20261015-rerun` exited 0 with `WORLD-FEATURES_QA PASS`.

The smoke run is functional evidence, not a replacement for a fresh Player visual pass. Manual
acceptance should still inspect model pivots, labels, water readability, roof cutaways and boarding
from the player camera. No benchmark, soak test or long campaign run was performed.

## Continuation verification 2026-10-15 expansion and animation

1. `WorldExpansionCatalog` defines five new POIs inside the doubled world bounds: forest reserve,
   industrial yard, north village, east marina and south highway. `WorldExpansionDressing` creates
   lightweight primitive dressing, labels, approach routes and interaction areas for each POI.
2. `CityDistricts.GroundWidth` and `GroundDepth` are doubled around the existing world center.
   Existing area IDs, spawn coordinates and save identifiers remain unchanged. The map now contains
   fourteen stable points and its right hand travel buttons use a six percent row height so labels fit.
3. `ActorAnimationSmoothing` centralizes locomotion, action crossfade and animator parameter updates.
   `ActorView` keeps root motion disabled and forwards movement, pickup, drop and work states to the
   smoothing component.
4. `WorldAssetBudgetAudit` counts mesh vertices, shared materials and unique texture pixels and
   rejects non finite bounds. The audit now checks mesh bounds before reading `Renderer.bounds`, so a
   deliberately invalid mesh is reported by the test without producing a native Unity worldAABB log.
5. Expansion ground has physics colliders for the current runtime NavMesh bake. Expansion map points
   use clear route anchors and a bounded expansion only teleport fallback so the five new destinations
   remain usable while their navigation surface is being refined.

Fresh bounded evidence from this continuation:\n
- `Tycoon.Tests.ActorAnimationSmoothingTests` exited 0.
- `Tycoon.Tests.WorldAssetBudgetAuditTests` exited 0 after the non finite bounds fix.
- `Tycoon.Tests.CityPedestrianRouteContractTests` exited 0.
- `Tycoon.Tests.WorldModelAlignmentAuditTests` exited 0.
- `Tycoon.Tests.WorldRouteCorridorTests` exited 0.
- `Tycoon.Tests.WorldCutawayVolumeTests` exited 0.
- `Tools/run_unity.ps1 -Task Build` exited 0 after the final map and NavMesh changes.
- `Tools/run_world_features.ps1 -BuildPath Builds/Windows/TYCOON2.exe
  -OutputPath work/world-features-current-20261015-expansion-finalpass` exited 0 with
  `WORLD-FEATURES_QA PASS`.

The build output is in `D:\GAME\TYCOON2\Builds\Windows`. This is a bounded functional smoke,
not a benchmark, soak test, performance campaign or replacement for fresh manual Player visual
acceptance. External downloaded models were not added in this slice; the new areas use the existing
low overhead primitive and material catalog so the texture and geometry budget stays predictable.

## Continuation verification 2026-10-15 route contracts

1. `WorldRouteCatalog` now includes five reserved expansion corridors for the forest reserve,
   industrial approach, north village road, east marina dock and south highway. The existing
   `CityAccessPaths` renderer and placement reservation logic consume the same catalog, so route
   signs, walkable corridors and no placement zones stay aligned.
2. `CityMapLocations.forest_reserve` now points to the forest approach anchor instead of the dense
   POI center. Industrial and east marina already target their clear approach anchors; village and
   south highway map points sit on their corridor nodes.
3. `WorldExpansionContractTests` verifies expanded bounds, finite approach anchors, unique reserved
   route IDs, continuous nodes and map destinations that land on expansion corridors.

Fresh bounded evidence:\n- `Tycoon.Tests.WorldExpansionContractTests` exited 0.
- `Tycoon.Tests.WorldRouteCorridorTests` exited 0 after the catalog grew to 11 routes.
- `Tycoon.Tests.CityPedestrianRouteContractTests` exited 0.
- `Tycoon.Tests.WorldModelAlignmentAuditTests` exited 0.
- `Tycoon.Tests.WorldCutawayVolumeTests` exited 0.
- `Tycoon.Tests.ActorAnimationSmoothingTests` exited 0.
- `Tycoon.Tests.WorldAssetBudgetAuditTests` exited 0.
- `Tools/run_unity.ps1 -Task Build` exited 0.
- `Tools/run_world_features.ps1 -BuildPath Builds/Windows/TYCOON2.exe
  -OutputPath work/world-features-current-20261015-expansion-routes` exited 0 with
  `WORLD-FEATURES_QA PASS`.

This remains bounded functional evidence. It does not replace a fresh Player camera review of
model pivots, label placement, water edge appearance, cutaway roof behavior or physical boarding.
No benchmark, soak test or long campaign run was performed.
