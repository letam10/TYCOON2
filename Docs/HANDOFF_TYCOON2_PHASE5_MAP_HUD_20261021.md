# TYCOON2 Phase 5 handoff — interactive map and HUD

Date: 2026-10-21
Base: `4f28bbe2bcc3a7a1d25170a103d508195815b7da` (`feat: add phase4 assets environment contracts`)
Scope: map viewport state, interactive map controls, typed POI presentation, tooltip/selection/route hooks, and safe-area HUD integration.

## Delivered

- Added `MapViewportState` as the single deterministic owner for map center, zoom, pan, rotation, selected POI, and category filters.
- Fixed zoom range is `0.65..3.5`; rotation is normalized to `[-180, 180)`; non-finite state inputs are ignored and invalid state reports false.
- Connected `CityMapGraphic` to the viewport state while preserving `PoiKind`, `PoiGlyph`, `PoiColor`, `Initialize`, `Position`, `CityMapLocations.Points`, and teleport stable IDs.
- Added map APIs for zoom, normalized pan, rotation, north reset, player focus, kind filters, tooltip selection, and route highlight.
- Added map reset/player-focus controls, filter controls, tooltip text, pointer pan/rotate handling, wheel zoom, keyboard/gamepad navigation, and route highlight hooks in `GameHudMap`.
- Kept minimap labels outside the active `RectMask2D` and retained safe-area HUD placement through `GameHudLayout`.
- Added contract tests for state and map/HUD integration without changing save/economy or POI teleport authority.

## Verification

- `MapViewportStateContractTests`: `9/9 Passed`, Unity exit `0`.
- `Phase5MapHudContractTests`: `5/5 Passed`, Unity exit `0`.
- `CityMapCaptionLayoutTests`: `4/4 Passed`, Unity exit `0`.
- `MinimapViewportTests`: `3/3 Passed`, Unity exit `0`.
- `Stage9MapWayfindingTests`: `4/4 Passed`, Unity exit `0`.
- Windows build after Phase 5: `Succeeded`, `117229839` bytes, `0` errors, `34` warnings; evidence: `work/tycoon2-plan-20261020/phase5-map-hud/Player/build-report.json`.
- Player world-features smoke: `passed=true`, RTX 4060 Laptop GPU, 29 interactions, 6132 checks, 78 screenshots, 0 runtime errors, and zero matching error lines in the log; evidence: `work/tycoon2-plan-20261020/phase5-map-hud/world-features/world-features-report.json`.

## Boundaries

Focused tests prove the viewport contract and existing map/HUD regression behavior. The Player smoke proves broad runtime coverage but does not replace headed visual inspection at native 1080p/1440p, label overlap review at every window scale, measured 60 FPS sampling, or a long-session performance run. Those remain Phase 7 evidence. No save schema, economy value, stable POI ID, or teleport eligibility was changed.

## Next tranche

Phase 6 must audit additive save compatibility for interior/map/asset state, validate before save, preserve old save loading, and prove player position, cargo, money, POI progress, and slot isolation through load/save tests and Player smoke.
