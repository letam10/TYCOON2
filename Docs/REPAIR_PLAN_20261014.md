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
