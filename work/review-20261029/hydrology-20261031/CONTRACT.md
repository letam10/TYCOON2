# Hydrology implementation contract — 2026-10-31

User selected relocation inland / new shoreline. Keep camera, player character and Farm rules.
Source base remains 3a3ad93 with verified uncommitted photographic map and atomic-save changes.

## Fixed placement and IDs
IndustrialYard center=(-200,0,315), footprint=(180,4,140); entry=(-280,0,265), visit=(-145,0,345).
ForestReserve ends at x=-330; industry begins at x=-290: 40m valley for the upstream river.
NorthVillage begins at x=-80; industry ends at x=-110: 30m separation.
EastMarina center=(230,0,-215), footprint=(180,4,110), landside entry=(180,0,-230).
Its z range [-270,-160] leaves 5m to SouthHighway and 4m to Harbor. Main quay ends at x=218;
piers run along z=-247 and -188; boarding point is derived from the boat door, not an invented marker.
Keep existing stable IDs. Relocated positions/approaches/map/navigation read CoastalTownLayout.

## Module interfaces
CoastalTownLayout.cs: static centers/footprints/entry/visit/exit and IReadOnlyList<Vector3>
IndustrialRoad, IndustrialWalk, MarinaDock, MarinaWalk. No references back to WorldRouteCatalog.
WorldHydrologyLayout.cs: static CoastX(z), River (IReadOnlyList<RiverSection>), IsWater(Vector3),
ShoreDistance(Vector3), Mouth, WaterLevel. RiverSection stores Center, Left, Right, HalfWidth.
WorldHydrologyMeshes.cs: Ocean(Bounds), CoastBank(Bounds), RiverWater(), RiverBanks(): Mesh.
WorldHydrologyWorld.cs: BuildOcean(Transform), BuildRiver(Transform), water material ownership,
shore barriers/NavMesh exclusions. All source meshes have explicit lifetime ownership.
WorldHydrologyBridges.cs: Build(world) from the same route samples where they intersect river water.
TownWater.shader: optional UV shore-distance channel; uniform default preserves other materials.
WorldHydrologyTests.cs and QaHydrology.cs: geography, water/land navigation, moved POI access,
rendered atlas/shore/river/estuary/bridges, same gameplay camera and no runtime errors.

## Geometry
Ocean covers every point east of CoastX through far horizon. CoastX retains the current beach
shore at x=247+existing small offset between z=11 and 143; quay shore at x=225 at Harbor/Marina,
x=218 at the new EastMarina; other stretches blend smoothly.
River begins north of world through x≈-310 valley, bends south of bank/monument footprints,
passes existing central z≈103 channel, and ends at CoastX(151), z=151. Width grows downstream.
River is never skipped because of placement reservations. It excludes decorations instead.
Water level is shared .0525m. Last river-bank vertices use CoastX at their z coordinates.
Render/collider navigation coverage derive from these samples; no imagery patch can substitute geometry.
No whole rectangular marina floor over water: landside quay + real pier colliders and rails.

## Acceptance
No land strip east of sea inside world; river source-to-mouth continuous; moved industry dry;
marina quay supported, piers walkable, open water not walkable. Native photos before and after,
full-map recapture and zoom/rotation preserved. Focused tests and fresh Player navigation/save gates.
Map artifacts from 29/10 remain pre-hydrology evidence. No full redesign / Steam acceptance claim.
Workers authorized by user but no spawn/followup tools currently exposed; root implements modules.
