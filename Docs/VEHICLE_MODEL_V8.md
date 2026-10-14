# Vehicle models V8 — 2026-10-08

## Scope

Procedural models use the builtin generated reference `Docs/Art/vehicle-reference-v8.png`.
All construction is deterministic, forward +Z, root scale one, wheel rotation axis +X.
No source scene or imported mesh is needed. Existing Town surface textures provide shared
roughness and surface variation; clear URP glass is shared separately.

## Variants

- Sedan: curved crown hood/trunk, tapered cabin, separate sloped glass, open wheel arches.
- Taxi: 4.2 m body, yellow paint, roof sign, hinged passenger doors and visible seats.
- Luxury: 5.05 m body, tall chrome grille, plush seats, different proportions and larger rims.
- Sports: low cabin, wide stance, splitter, rear wing, side intake and exposed large wheels.
- Small truck: short cab, open arch, ribbed load bed and curved fabric canopy with tie cords.
- Cargo truck: long corrugated box, rear locks, three axles, side rails and cab steps.
- Tanker: cylindrical tank with rounded end rings, straps, catwalk, rails and rear ladder.
- City bus: 8.6 x 2.45 m collider, low floor, standing handrails, large glazing and route panel.
- Coach: same route footprint, higher roof/window sill, luggage bays, racks and plush seat rows.
- Train: rounded locomotive plus two passenger coaches, 12 rotating axle pivots, bogie frames,
  bellows, couplers, seats, roof vents, four sliding doors and three individual body colliders.

## Runtime API and ownership

`CityVehicleModel.Build(CityVehicleKind, string, Transform)` returns a model root with `CityVehicleRig`.
The previous bool overload is retained and maps van to SmallTruck.
`CityPublicTransportModel.Build(int, Transform)` adds the rig to the supplied root and returns
its actual right passenger door. Kinds 0/1/2 remain bus/coach/taxi.
`CityRailVehicleModel.Build(Transform, string)` returns the complete train with its rig.

Rig exposes Kind, DoorInsideLocal, Wheels, LeftDoors, RightDoors and WheelRadius.
SetTravel uses distance/radius to rotate wheel pivots. SetDoorOpen clamps input, opens only
one selected side, slides bus/coach/train doors and hinges road car/cab doors. Closing restores
recorded local positions/rotations exactly. Meshes and colliders are never rebuilt per frame.

Bus/coach entry foot is (1.12, .54, 2.6); taxi is (.865, .2, -.35).
Train entry foot is (1.3, .6, -10), wheel radius .38. Coach door openings are 1.04 m wide.
Train envelope is below 32 m long, 2.8 m wide and 3.6 m high, including steps/couplers.
All body NavMeshObstacles use carving=false. Passenger traversal belongs to the carrier owner.

Static hull geometry is grouped by material; each wheel/axle and each door uses one renderer.
The largest train uses ten hull renderers, twelve wheel renderers and four door renderers.
CityVehicleRig owns every generated mesh and destroys them when its root is destroyed.
Shared Town materials/textures and the glass material are reused across builds and remain shared.
Generated mesh data is compacted per material, so transparent bounds do not span unrelated hull parts.

## Verification

Added VehicleModelV8Tests for all ten kinds, distinct geometry, renderer limits, wheel motion,
public transport API/footprints, clamped open/closed doors, train dimensions and separate colliders,
shared material lifetime/owned mesh release, and compatibility of the legacy van wrapper.
Source reviewed for exact contract signatures and owned files checked with git diff --check.
Unity/EditMode/Player were deliberately not launched by the model agent; root owns those gates.
Visual acceptance requires the actual Player screenshots and passenger boarding flow.

## Closed car door repair — 2026-10-09

The Player taxi capture showed recessed doors, paint ending below the beltline and glass/header gaps.
Sedan, taxi, luxury and sports now use a dedicated sculpted car door. Its paint follows the same
cross-section as the body, reaches the beltline, then forms the shoulder up to the sloped window.
Door and static quarter windows share exact sill/header positions. The roof shell meets that header.
Body segments terminate exactly at z=-.88 and z=.18; opening either hinged door leaves a real passage.
The low rocker remains below the door and joins the existing floor; no static plate fills the opening.
Hood, fender and end-cap boundaries now share profiles and sampling positions, including sports arches.
Wheel transforms, passenger foot anchors, collider dimensions, shared materials and renderer counts stay fixed.

Added four focused cases checking both door edges against emitted body vertices, glass/roof joints,
absence of a static passage fill, outward opening/closing and finite model vertices/normals.
Standalone Roslyn compiled changed source/tests with zero errors against unstripped Unity references.
An offline probe checked 8,192 actual emitted hood/end-cap vertices for finiteness and 80 seam samples.
The probe substitutes reference identity for material lookup only; it does not run Unity or render images.
Complete EditMode and six-angle/open-door Player acceptance remain with root.
