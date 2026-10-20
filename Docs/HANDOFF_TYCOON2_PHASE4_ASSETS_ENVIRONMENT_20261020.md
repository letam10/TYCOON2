# TYCOON2 Phase 4 handoff — assets and environment

Date: 2026-10-20
Base: `f1cc27e16e0a3a479c08aecbd47b8c562efab645` (`feat: add phase3 interior cutaway camera controls`)
Scope: deterministic biome placement, shared water/shoreline queries, runtime catalog integration, and asset provenance.

## Delivered

- Added `CityBiomeVegetation` with fixed biome IDs `forest`, `farm`, and `coastal`, stable FNV-1a placement from biome/seed/sample index, horizontal clearance filtering, finite-input rejection, deterministic yaw/scale/category, and no Unity object creation.
- Added `CityWaterSurface` with a shared water baseline, finite deterministic water height, shoreline blend, and vertical-only floating-object snap.
- Added `CityAssetProvenance` with fail-closed validation for source URL, immutable version/commit, license, local path, import profile/policy, positive scale, finite pivot, collider policy, and LOD policy.
- Connected `WorldVegetationCatalog` to the shared biome IDs so runtime profiles and the Phase 4 contract cannot drift.
- Connected `CityCoastWaterMesh` to the shared `CityWaterSurface.WaterBaseline`; displacement remains owned by the existing deterministic wave function.
- Added `Docs/Art/PHASE4_ASSET_PROVENANCE_20261020.md`. Existing ez-tree generated outputs remain individually allowlisted; the raw `ASSET/Downloaded/` tree is not added to the Player.

## Verification

- Focused vegetation contract after runtime integration: `4/4 Passed`, Unity exit `0`; evidence: `work/tycoon2-plan-20261020/phase4-assets-environment/editmode-vegetation-integration-results.xml`.
- Focused water/provenance contract after fixing the shoreline assertion: `6/6 Passed`, Unity exit `0`; evidence: `work/tycoon2-plan-20261020/phase4-assets-environment/editmode-water-integration-results.xml`.
- Windows build after integration: `Succeeded`, `117180543` bytes, `0` errors, `34` warnings; evidence: `work/tycoon2-plan-20261020/phase4-assets-environment/Player/build-report.json` and `build.log`.
- Player world-features smoke on that build: `passed=true`, RTX 4060 Laptop GPU, 29 interactions, 5928 checks, 78 screenshots, 0 runtime errors, 0 matching error lines in the log; evidence: `work/tycoon2-plan-20261020/phase4-assets-environment/world-features-final/world-features-report.json`.

## Boundaries

The focused tests prove deterministic contracts and the smoke proves broad regression coverage. They do not prove close-up visual quality of every hero asset, native headed 1080p/1440p layout, a measured 60 FPS budget, long-session stability, or complete Phase 5 map/HUD interaction acceptance. Those remain explicit Phase 7 gates. No new external GitHub asset was imported in this tranche; the provenance record covers the existing pinned ez-tree outputs and repository-owned water generators.

## Next tranche

Phase 5 must implement and verify the interactive map viewport state, zoom/pan/rotation/filter/tooltip behavior, POI icon categories, route highlighting, unlock state, label collision handling, minimap safety, and HUD safe-area layout. Preserve the stable POI IDs and the current save/economy contracts.
