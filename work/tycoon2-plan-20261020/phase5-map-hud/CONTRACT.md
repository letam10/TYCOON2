# TYCOON2 Phase 5 Contract — Interactive Map and HUD

## Scope

Phase 5 adds one deterministic viewport state for the existing map and connects it to the current `GameHud`/`CityMapGraphic` hierarchy. Stable POI IDs, `CityMapLocations.Points`, teleport rules, route catalog data, save/economy state, and existing map button names remain unchanged. Workers must edit only their assigned files and must not launch Unity, build, Player, browser, or another worker, and must not commit or push.

## Shared state contract

`MapViewportState` is the only owner of map center, zoom, pan, rotation, selected POI, and category filters. It is a plain deterministic runtime type and must not create Unity objects or read `GameSession`.

Required public API:

- `public sealed class MapViewportState`
- `public const float MinZoom = 0.65f`
- `public const float MaxZoom = 3.5f`
- `public Vector3 Center { get; }`
- `public float Zoom { get; }`
- `public float RotationDegrees { get; }`
- `public string SelectedPoiId { get; }`
- `public IReadOnlyCollection<string> EnabledKinds { get; }`
- `public void SetCenter(Vector3 worldPosition)`
- `public void FocusPlayer(Vector3 playerPosition)`
- `public void ZoomBy(float amount)`
- `public void Pan(Vector2 normalizedDelta)`
- `public void RotateBy(float degrees)`
- `public void ResetNorth()`
- `public void Select(string poiId)`
- `public void ToggleKind(string kind)`
- `public bool IsKindEnabled(string kind)`
- `public bool IsFiniteAndValid()`

Zoom is clamped to the fixed range; rotation is normalized to `[-180, 180)`; pan/center/zoom inputs with NaN or infinity are ignored or fail closed. Filter values are ordinal strings and an empty enabled set means all known kinds are visible. State changes must not mutate POI arrays.

## Worker A — viewport state

Allowed files:

- `Assets/_Game/Scripts/Runtime/MapViewportState.cs`
- `Assets/_Game/Tests/EditMode/MapViewportStateContractTests.cs`
- matching `.meta` files only when required

The tests must cover default state, zoom clamp, finite pan, rotation normalization, north reset, player focus, selection, filter toggling, invalid input, and preservation of stable POI IDs.

## Worker B — map and HUD integration

Allowed files:

- `Assets/_Game/Scripts/Runtime/CityMapGraphic.cs`
- `Assets/_Game/Scripts/Runtime/GameHudMap.cs`
- `Assets/_Game/Scripts/Runtime/MapPlayerIndicator.cs`
- `Assets/_Game/Scripts/Runtime/GameHudLayout.cs`
- `Assets/_Game/Tests/Editor/Phase5MapHudContractTests.cs`
- matching `.meta` files only when required

Integration requirements:

- Keep `CityMapGraphic.PoiKind`, `PoiGlyph`, `PoiColor`, `Initialize`, `Position`, and existing stable POI IDs source-compatible.
- The world map owns a `MapViewportState`; the minimap may use a separate local state initialized from player position and default zoom.
- Draw world geometry around state center and zoom; rotation affects viewport mapping only and never mutates world objects or player orientation.
- Existing map buttons and `CityMapLocations.Teleport` remain authoritative.
- Add filter/selection/tooltip/route-highlight hooks without hiding locked POIs or changing teleport eligibility.
- Input handling belongs in `GameHudMap`: wheel/buttons zoom, pointer drag pan, modifier/button rotation, north reset, player focus, and basic gamepad navigation. Do not alter `GameHud.UpdateNavigationInput` save/menu semantics.
- Keep minimap inside `safeArea`; map labels/buttons must remain clipped by the existing viewport and not cover the safe HUD panels.

## Root integration and QA

The root agent owns any scene/editor wiring, runtime smoke, focused tests, Windows build, evidence, handoff, allowlist staging, commit, and push. Root may repair worker files only within this contract. Existing Stage 7/9 map tests remain regression gates; visual close-up and performance acceptance remain separate Phase 7 evidence.

## Validation gates

1. Focused EditMode state and map/HUD contract tests.
2. Existing map caption, minimap viewport, save slot, and Stage 9 wayfinding tests.
3. Windows build with zero errors.
4. World-features Player smoke with map open/close, travel, bindings and save slots still passing.
5. Record that native headed 1080p/1440p overlap and 60 FPS remain Phase 7 until measured.
