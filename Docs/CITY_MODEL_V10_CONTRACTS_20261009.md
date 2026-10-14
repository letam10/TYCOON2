# City and models v10 — shared contracts

## Scope and invariants

- Unity 6000.6.3f1 / URP 17.6, C# runtime procedural scene in D:/GAME/TYCOON2.
- Preserve all IDs, save/transaction semantics, current unrelated dirty changes.
- No subagents may spawn. No publishing, commits, Unity process or Player launch by subagents.
- Files under about 300 lines, one statement per line; Vietnamese comments for complex logic.
- Root owns city expansion, rendering settings, QA angle capture and integration builds/tests.
- Existing city dimensions 282.842712 x 192.333044, center (8,0,22).
- New dimensions exactly twice each: 565.685424 x 384.666088. Preserve existing station coordinates.
- Isolated model acceptance: 8 angles; placed acceptance: 5 angles including overhead.

## Carry containers owner

- May edit InventoryStack.cs; create CarryContainer*.cs and matching .meta;
  create CarryContainerTests.cs and .meta; create Docs/CARRY_CONTAINERS_V10.md.
- Only fragile item IDs: egg, milk, bottled_milk, sauce, soy_sauce.
- All other goods remain directly held, including cheese, bread, cloth, corn, beef, wheat, wool, yarn.
- Containers only when InventoryStack.HeldLayout is true, for both player and workers.
- Keep individual pooled item objects in items; VisibleCount and VisibleItemId represent real items.
- Use open 2x3 compartment trays/crates: six units per layer; partial layer shows exact count.
- One egg/bottle means exactly one visible unit in a container, never fake filled containers.
- Upright fragile contents with bases supported on container floor, no intersecting dividers.
- Shared meshes/materials; cache container geometry by profile, no mesh creation per frame.
- Existing InventoryStack.Refresh is revision driven. Expose Top and RearExtent including containers.
- Dispose/reuse containers on count change, item switch, empty, disable/pooling; no transaction changes.
- Signature suggestion: CarryContainerLayout.TryLayout(Transform owner, IReadOnlyList<GameObject> items,
  out float top, out float rearExtent); caller owns persistent helper and calls Clear when inactive.
- Read actual APIs before implementing; no edits to HandItemModels, CarryItemLayout or CarryPresentation.
- Self-check structural Python/PowerShell checks and prepare meaningful NUnit tests for exact counts,
  supporting floor, all five fragile IDs, nonfragile exclusion, reuse and cleanup. Root runs Unity.

## Industrial model owner

- May edit only WorldDressing.cs Machine method; create IndustrialMachine*.cs and .meta;
  create IndustrialMachineModelTests.cs and .meta; create Docs/INDUSTRIAL_MODELS_V10.md.
- Replace generic machine body with distinct detailed functional silhouettes per recipe.
- API: public static GameObject IndustrialMachineModels.Build(MachineStation station, Transform parent).
- Return root at local zero; preserve station collider, IDs, InteractionPoint, inputs/outputs, status lamp,
  progress and CozyMachineMotion. Set station.Rotor to a functional rotor/paddle if appropriate.
- Cover mill/feedmill, cheesemaker, saucemaker/soyextractor/milkbottler, spinner/loom,
  breadmixer, oven/cakeoven and kitchen/cook_* families using actual recipe definitions.
- Machine floor is 2.45 x 2.12; keep body x within +/-1.1, z within +/-0.92, base y>=0,
  height<=2.35. Do not block front interaction or output/support trays.
- Real thickness, grounded feet, panels, vents, pipes/motors/control details; closed shells,
  no coplanar overlapping skins or negative scales. Surface materials shared, static parts batched.
- Keep animated rotor separate. No new per-part Update, lights or particles. Reuse current motion.
- Self-check all recipes instantiate, bounds finite/within footprint, rotor separate, collider unchanged,
  cached/shared materials. Root handles native Unity tests, image angles and visual iteration.

## Reporting

- Integration ownership 2026-10-09: industrial agent interrupted after its test compile failure;
  root now owns IndustrialMachine files and their tests/documentation through final Player acceptance.

- Finish whole assigned job, self-check then report once (max 15 lines): files, checks, open issues.
- Do not modify other agent files. Do not start Unity or touch QA/build-report.json.
