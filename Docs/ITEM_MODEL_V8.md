# Item models V8 — 2026-10-08

## Scope

All 27 catalog IDs plus `cash` and `crate` retain `HandItemModels.Build/Supports`.
The catalog ID for the soybean vegetable dish is `soy_vegetables`.
There is no `salad` item in the catalog.

## Geometry and surfaces

- Carrot retains the approved pointed root, four leaves only at its broad crown,
  ModelSize .54, carry scale .756, and uniform leftward root direction.
- Tomato has six shallow lobes, pointed calyx and curved stem.
- Wheat has alternating angled grains, fine stalks/awns and a round twine binding.
- Corn uses a tapered cob, seven rows of kernels and curved husks.
- Soy pods have seed bulges, a curved seam and a loose bean; egg has a 24-sided
  asymmetrical shell with subtle speckling.
- Churn has a continuous shoulder, metal lid, bottom rim and loop handles.
- Three bottle types have continuous bodies/shoulders/necks, screw closure rings,
  distinct contents/cap colors, label seals and print marks.
- Flour/feed sacks have gathered mouths, folds, tied twine and distinct labels.
- Cheese has a tapered wedge outline, rind and pores. Beef has an irregular flat
  cut, visible fat rim, curved veins and marbling in its albedo/normal atlas.
- Bread has a rounded loaf and curved scores. Cake has three sponge/cream layers,
  icing, piped rosettes and seeded strawberries.
- Wool uses irregular tufts/curls; yarn has a bobbin and individual thread rings;
  cloth uses curved folded edges and woven seams.
- Dough rests on a board; batter sits inside a hollow bowl with a swirl and whisk.
- Prepared dishes use a shaped ceramic plate or hollow bowl. Pasta is made of
  curved strands; beef soy has slices/scallion rings/sesame; meal has rice grains,
  carrot slices and broccoli; soup has corn/herbs/cream; sandwich has bread,
  lettuce, egg and tomato; soybean vegetables has greens, carrots and beans.
- Cash has layered rounded paper edges, band, portrait and ink. Crate has an open
  cargo cavity, floor, separated wood slats, four braces and nail heads.

Every item is baked to one shared mesh, submesh, renderer and shared URP Lit atlas
material. Baked meshes and mapped source shapes are cached for subsequent builds.
Bounds normalization uses actual transformed vertices for every item. Normal,
albedo and metallic/smoothness maps distinguish food, meat, shell, berry, fiber,
wood, ceramic, metal, glass and baked surfaces.

## Carry layout

Every SKU uses two columns and three rows per layer. Upper layers share the
same X/Z positions as their supporting lower slots. Long produce and containers
lie horizontally; prepared food stays upright. Spacing uses rotated bounds.
Wheat and sacks use their shallow depth as the vertical carry dimension.
Carrot remains exactly 10 percent smaller than its .84 baseline.

## Verification

- Source compilation against installed Unity 6000.6.3f1 reference assemblies:
  runtime item files and CarryItemLayout compiled successfully with Roslyn.
- Source compilation of those files plus CarryItemLayoutTests, CityCarryTests,
  CarryVisualTests, CarrotShapeTests and ItemModelV8Tests also passed.
- Updated existing layout assertions from mixed/staggered layouts to 2 by 3.
- New tests cover exact vertex bounds, finite complete mesh data, nonoverlapping
  slots at partial/full counts, vertically aligned support, upright dishes,
  curved pasta directions and an unobstructed crate interior.
- Static catalog dispatch, line lengths/file sizes and git diff whitespace checked.
- Unity EditMode execution, Player build, screenshots and actual visual acceptance
  are intentionally left to the main agent. Source compilation is not a runtime
  or visual pass. No save, inventory, character, vehicle or QA script was edited.
