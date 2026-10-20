# TYCOON2 Phase 4 Contract — Assets and Environment

## Scope

Phase 4 improves the readable stylized semi-realistic PBR presentation while preserving gameplay contracts, stable identifiers, economy, save data, route ownership, and the current scene/build pipeline. Workers must edit only the files assigned below. Do not import arbitrary downloaded asset folders, change scenes, change save schemas, or alter existing gameplay IDs.

## Shared contracts

- Existing runtime builders remain the source of truth for generated environment content.
- New deterministic catalogs must derive placement from `(biomeId, seed, sampleIndex)` and must not use Unity global random state.
- Biome IDs are fixed strings: `forest`, `farm`, and `coastal`.
- Existing route and building clearance is authoritative; vegetation and props are decorative and must be rejected or skipped when their footprint overlaps route/building clearance.
- Water and shoreline height queries must remain deterministic and expose a single shared query path for floating props, boats, docks, and shoreline dressing.
- Asset provenance records use: source URL, source commit/version, license, local asset path, import profile, scale, pivot, collider policy, and LOD policy.
- No worker may start Unity, build, launch a Player, open a browser, create another worker, commit, or push.

## Worker A — biome vegetation catalog

Allowed files:

- `Assets/_Game/Scripts/Runtime/CityBiomeVegetation.cs`
- `Assets/_Game/Tests/EditMode/CityBiomeVegetationContractTests.cs`
- matching `.meta` files only when required

Required API:

- `public static class CityBiomeVegetation`
- `public const string ForestBiomeId = "forest"`
- `public const string FarmBiomeId = "farm"`
- `public const string CoastalBiomeId = "coastal"`
- `public readonly struct Placement`
- `public static IReadOnlyList<Placement> Generate(string biomeId, int seed, IReadOnlyList<Vector3> samples, float clearanceRadius)`
- `public static bool IsAllowedBiome(string biomeId)`

Placement must contain stable biome ID, deterministic index, position, yaw, scale, and category. The generator must be pure, deterministic for identical inputs, bounded by clearance, and avoid Unity object creation.

## Worker B — water, shoreline, and provenance

Allowed files:

- `Assets/_Game/Scripts/Runtime/CityWaterSurface.cs`
- `Assets/_Game/Scripts/Runtime/CityAssetProvenance.cs`
- `Assets/_Game/Tests/EditMode/CityWaterEnvironmentContractTests.cs`
- matching `.meta` files only when required

Required API:

- `public static class CityWaterSurface`
- `public static float SampleWaterHeight(Vector3 worldPosition)`
- `public static float SampleShorelineBlend(Vector3 worldPosition)`
- `public static Vector3 SnapFloatingObject(Vector3 worldPosition)`
- `public sealed class CityAssetProvenance`
- `public bool IsComplete()`

Water queries must be finite, deterministic, and use the current water baseline without changing gameplay physics. `SnapFloatingObject` adjusts only vertical position. Provenance validation must fail closed when URL, version, license, local path, or import policy is missing.

## Root integration and QA

The root agent owns integration, any scene/editor wiring, asset import records, focused EditMode tests, Windows build, Player smoke, evidence and handoff. Root may repair worker code only inside these assigned files and must preserve the explicit allowlist. Any visual/close-up/1080p/1440p/FPS acceptance remains separate from source/build/smoke gates.

## Validation gates

1. `node`/static inspection of worker files and contract signatures.
2. Focused EditMode tests for deterministic biome placement, clearance, water finite queries, floating height, and provenance fail-closed behavior.
3. Windows build with zero errors.
4. Existing world-features Player smoke with zero runtime errors.
5. Record limitations for visual close-up, native resolution, LOD frame time, and full Steam acceptance until Phase 7.
