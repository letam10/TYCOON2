# Building placement and collision contract

Latest user asks for precise walls, grounded furniture and richer houses.

## Ownership

- Building agent: TownModelParts.cs, TownArchitecture.cs, CityArchitecture.cs,
  CityStaticGeometry.cs, TownFixtures.cs, TownProps.cs, WorldDressing.cs,
  new CityBuildingDetails.cs / TownFurnitureCollision.cs and focused editor tests/metas.
- Root: WorldFactory.cs, all held/stored item files, ground/gravel shader,
  workforce transaction integration, QA, build and Player evidence.
- No edits to navigation, workforce, transit, imported characters, save IDs or unrelated files.

## Shared API

- Preserve TownModelParts.Box(name, point, size, surface, tint, parent, yaw = 0).
  May append optional bool solid = false; collider local center zero, size exact mesh size.
- Preserve all CityStaticGeometry methods. May add RotatedBox(point, size, rotation, surface, tint)
  for continuous sloped roof panels. It uses the existing material/chunk batches.
- Ground and business floors have top at y = 0.06, normal visible prop roots at y = 0.
- Existing business wall openings remain 4 metres wide at rear center/right-side center.
- Business walls must join plinth to cornice without gaps and block a 0.3-radius capsule.
- Roof and trim above head height need no collision; opaque walls, planters, counters and
  furniture at walking height need collision matching the visible footprint.
- Do not add broad colliders across working/queue/entry paths or render roofs over gameplay.
- Imported furniture collision uses renderer bounds transformed into its root local space.
- Decorative houses are solid, not enterable. Add foundation collision and coherent sloped roof,
  door/porch/window/shutter/gutter details with material variation, reusing batched geometry.
- Greenhouse planter beds must have physical collision and ground support.

## Verification

- Add meaningful tests: visible walls vs collision bounds, wall support/openings, grounded furniture,
  continuous roof bounds and no collision spanning the intended entry.
- Static checks: git diff --check, files ~300 lines, columns ~120, one statement per line.
- Agent does not launch Unity, Player or more subagents. Root runs tests/build/ordinary Player.
- Report once when done, at most 15 lines: changed files, checks, remaining issues.
