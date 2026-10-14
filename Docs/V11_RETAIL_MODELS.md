# Retail and safe models V11

## Implemented

- RetailPropModels implements Supports, Size and Build for the four contract keys.
- Display cabinet: recessed metal plinth, continuous carcass, timber panels and an uninterrupted stock deck.
- Checkout: timber fascia, metal kick plate, rear cabinet fronts, drawers and attached handles.
- Warehouse rack: four continuous uprights, foot plates, end ties, rear panel and top canopy.
- Closed supply crate: backed plank seams, closed lid, steel bands and a two-runner pallet.
- Safe: beveled armored shell, gasket, inset door, hinges, dial, attached pull handle and manufacturer plate.
- All construction uses the shared TownModelParts bevel meshes and TownSurfaceMaterials surfaces.
- Static geometry is combined by material once per model key, then the combined meshes/materials are shared.
- Retail and safe model factories add no MonoBehaviour, lights, animation or colliders.
- SafeStation retains both original colliders, labels, deposit/withdraw offsets and interaction behavior.

## Dimensions and stock

| Model | Actual size XYZ | Stock plane |
| --- | --- | --- |
| storage_rack | 3 x 1.65 x .85 | Existing front stock stays at station y=.16 |
| display_counter | 1.4 x 1.04 x 4.7 | y=1.04 |
| checkout_counter | 2.2 x 1.1 x 1.1 | y=1.10 |
| supply_crate | 1.15 x .7 x 1.15 | Closed prop |

Checkout keeps the body within the original 2.1 x .9 footprint at x=.3.
The top overhang reaches x=-.8 to 1.4 and z=-.5 to .6 to support the full existing stock rectangle.
The safe plinth remains 1.6 x 1.3 and the body remains 1.3 x 1.05, with all door hardware facing +Z.
Storage is still placed at (0,.15,.65), so its foremost geometry is station z=.225.
The base rack has no middle shelves crossing the existing upgrade shelf planes.
No decorative products were added; inventory remains owned by InventoryStack.

## Integration requirements for the root agent

- Route the four keys through RetailPropModels before catalog prefabs in Art.Model and Art.ModelSize.
- Skip TownPropSurfaces.Apply for those factory models.
- Existing StationStockSurface trays for Shelf and Checkout occupy the same top planes as the new decks.
  Keep their support colliders and stock coordinates, but skip/hide those duplicate tray renderers.
- WorldDressing.Checkout currently places the POS screen and keyboard at y=1.0.
  Raise both to y=1.1 to sit on the new stock deck.
- WorldDressing.Storage still calls TownProps.Storage, whose old uprights and braces duplicate this rack.
  Remove the duplicate frame dressing while retaining a correctly grounded storage floor if needed.
- The existing warehouse decorative crates start at y=.15 while WarehousePallet reaches y=.16.
  If retaining that pallet, align crates at y=.16 during integration.

## Verification

- Added 13 EditMode cases covering grounded bounds, triangle validity, geometry budget,
  shared mesh/material lifetime, nine stock support samples per counter, upgrade shelf clearance,
  closed crate cover and safe orientation.
- Independently compiled the four new C# files with Roslyn against Unity and the current runtime/test references.
  No compiler errors; CS0436 warnings arise because the running editor had already imported the same new types.
- Runtime limits asserted by tests: 2 to 6 renderers and at most 6000 triangles per prop.
- Unity test execution, Player build, eight surrounding plus top views, placed angles and FPS are root-owned.
- The source checks do not establish visual acceptance or measured Player performance.
