# TYCOON2 Phase 0 baseline — 2026-10-20

## Scope
This is a read-only baseline for the sequential world redesign plan. No reset, clean, stash, overwrite, build, Player launch, benchmark, or runtime test was performed in this baseline step.

## Checkout and remote
- Project: D:\\GAME\\TYCOON2
- Branch: main
- HEAD: f538b06db0155e352c563412e39c918050cc414a
- Remote: https://github.com/letam10/TYCOON2.git
- origin/main...HEAD (behind/ahead): 0	1
- Remote fetch/fast-forward check: origin/main fetched; local branch was already up to date relative to the fetched remote and remains one commit ahead.

## Worktree preservation
- Modified tracked paths at baseline: 101
- Untracked paths at baseline: 1159
- Existing dirty and untracked paths are preserved and are outside this tranche unless explicitly listed in a later stage manifest.

## Architecture contracts to preserve
- WorldRouteCatalog in Assets/_Game/Scripts/Runtime/WorldRouteCorridor.cs remains the sole source for stable route IDs, route kind, nodes, width and reserved corridors.
- Mesh, walkable surface, interaction trigger and blocking collider remain separate responsibilities.
- Stable POI/entity IDs, economy, transaction authority, SaveData v2/v3 compatibility and transaction version 3 are unchanged.
- Route geometry, pedestrian route, traffic route, NavMesh evidence and map projection must derive from the same sampled route data before a stage can pass.

## Current route evidence
- CityAccessPaths already consumes WorldRouteCatalog and uses rotated route colliders.
- CityStreetNetwork still builds a separate hand-authored rectangular street bounds set.
- CityRoadRoute and CityTraffic still own traffic lane sampling/rounding independently of the world corridor catalog.
- WorldRouteDiagnostics and focused EditMode tests exist; current baseline does not rerun them.

## Gate state
- Phase 0: baseline captured.
- Phase 1 route/sidewalk unified generator: not started in this baseline.
- Phase 2 NavMesh/pedestrian/traffic synchronization: not started.
- Phase 3 interior/cutaway/camera: not started.
- Phase 4 asset/environment: not started.
- Phase 5 map/HUD: not started.
- Phase 6 save compatibility: preserved; no schema change proposed.
- Phase 7 full verification/Steam target: not run.

## Next action
Inspect the complete route catalog and existing focused tests, then make the smallest contract-first Phase 1 change. Each stage will have its own manifest, focused tests, build/runtime evidence as applicable, handoff note, scoped commit, and push receipt.
