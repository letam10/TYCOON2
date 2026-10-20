# TYCOON2 Phase 4 asset provenance

This manifest records the environment assets eligible for the Phase 4 runtime. It does not authorize the raw `ASSET/Downloaded/` tree to ship in the Unity Player.

## Generated vegetation

- Generator: `dgreenheck/ez-tree`
- Source URL: https://github.com/dgreenheck/ez-tree
- Pinned commit: `dcf309bd86bd521083d9c70f01f2de45fdc7c457`
- License: MIT; upstream license URL is recorded in `Docs/ThirdParty/EZ_TREE_LICENSE.md`.
- Package version: `@dgreenheck/ez-tree` `1.1.0`
- Reproducible input: `Assets/_Game/Art/Generated/Vegetation/ez-tree-presets.json`, seed `20261015`, fixed profile seeds and LOD distances.
- Runtime output allowlist: `farm-tree.glb`, `farm-tree-albedo.png`, `forest-tree.glb`, `forest-tree-albedo.png`, `coastal-tree.glb`, and `coastal-tree-albedo.png` under `Assets/_Game/Art/Generated/Vegetation/`.
- Import profile: Unity model import with generated material assignment, scale verified by the existing vegetation contract, root pivot at the generated model origin, and LOD thresholds from the preset.
- Collider policy: runtime tree placement uses bounded trunk/root colliders; foliage has no per-leaf collider.
- LOD policy: each biome keeps the preset LOD count and ordered near/far distances validated by `Stage6VegetationEzTreeTests`.

## Procedural water, shoreline, and environment materials

- Source: repository-owned deterministic generators `CityCoastWaterMesh`, `CityWaterSurface`, `CityOceanHorizon`, and `CityCoastModels`.
- License/provenance: project source; no external download is required for the water mesh or shoreline geometry.
- Scale/pivot: world-space generator units; water baseline is `CityWaterSurface.WaterBaseline`; generated meshes are placed through the owning `CityStaticGeometry` transform.
- Material policy: existing `water`, `ocean`, `sand`, `wood`, and `stone` material keys remain authoritative; imported textures must carry their own URL, version, license, compression, mipmap, and filter record before use.
- Collider policy: water visual meshes do not become walkable colliders; harbor and shoreline navigation boundaries remain explicit modifiers/colliders owned by the coastal model.
- LOD policy: distant ocean context is a non-colliding horizon mesh; close coastal geometry remains the detailed sampled mesh.

## Required checks for future external assets

Every new external model or texture must add a provenance entry with source URL, immutable version or commit, license, local path, import profile, measured scale, pivot, material handling, collider policy, and LOD policy. A missing field is a rejection condition. Asset folders copied wholesale from a download archive remain outside the runtime allowlist until their individual outputs and metadata are validated.
