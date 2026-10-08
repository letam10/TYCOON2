# Workforce, pedestrian routes, transport and placement

Latest user steering on 08/10 supersedes the earlier 78-worker target.

## Root ownership

- Root edits held/stored item orientation, 2x3 layer layout, ground gravel shader,
  buildings/props/colliders, WorldFactory, root QA integration and final build/Player evidence.
- Root integrates the route/transport entry points listed below after both agents finish.
- Do not launch Unity/Player or spawn subagents. Keep new files ~300 lines / 120 columns.

## Workforce agent ownership and API

- May edit Definitions.cs, CarryLimits.cs, WorkerAgent.cs, WorkerTransport.cs, WorkerTransport partials,
  GameSessionWorkers.cs, GameHudCrew.cs, TransactionMaximum*.cs, TransactionCrews*.cs,
  GameSessionPersistence.cs and new Workforce*.cs runtime/tests and metas.
- No terrain, vehicle, hand item, navigation or shared QA file edits.
- Expose WorkforceRules.MaximumActive = 39 and ExpectedActive(TransactionState state).
- Max workforce is exactly half the previous 78, with all 26 professions still represented.
- Define stable per-profession limits from catalog order, balanced 1/2 workers at maximum.
- Actual progression/mods use these limits; never hardcode identical maxima for all professions.
- Effective worker capability increases 1.5x (capacity, productivity, and safe locomotion).
- Player capacity and economic prices/yields remain unchanged.
- Saving/loading/mod repeat must not apply the multiplier or reduction again.
- Old staffed saves must preserve all goods, active reservations, station progress and dedup IDs.
- Retiring workers with cargo may finish depositing before despawning; never delete cargo/owners
  with live reservations. Serialize retirement and prevent assigning new jobs to retiring workers.
- Expose bounded retirement count/state so root can verify old saves settle to 39 active workers.
- Add meaningful migration/count/capability/idempotency tests. Root runs Unity and real Player.

## Routes and public transport agent ownership and API

- May edit CityLife.cs, CityTraffic.cs, CityTrafficSafety.cs, CityVehicleModel.cs,
  CommerceDirector.cs, OrderBubbleCustomerAgent.cs, OrderBubbleDiner.cs, RestaurantDirector.cs,
  NavigationWorld.cs and new CityPedestrian*.cs, CityPublicTransport*.cs runtime/tests and metas.
- No workforce, terrain, item, architecture, WorldFactory or shared QA edits.
- Expose CityPublicTransport.Build(GameSession game, Transform world).
- Navigation.Agent/Go/Arrived/Warp/Suspend retain existing public signatures.
- Route regular NPC travel via a small explicit connected sidewalk/main-road corridor network,
  with short NavMesh connectors to exact work/queue destinations and real physics collision.
- Arrival means the final destination, not an intermediate corridor node.
- Avoid player using a non-carving cylindrical NavMeshObstacle; pause/load/pool lifecycle safe.
- Do not teleport actors through geometry to hide path failures or disable their normal capsule.
- Add bus, passenger coach and taxi on fixed road lanes with designated stops, dwell, door animation,
  and actual NPC walk to door, boarding, seated ride and disembarkation.
- Disable locomotion collision only while boarding/seated; restore it when safely outside the door.
- Use actual existing character models/rigs/clips. Do not edit imported character assets.
- Keep vehicle geometry batched/shared and passenger count bounded; public transit is presentation,
  never creates inventory, sales or progress. State must not leave orphan agents after load/pooling.
- Add route/final-arrival and transport lifecycle tests; root checks actual Player boarding/paths.

## Root QA changes after integration

- Ordinary full-city target becomes 39 active workers, preserving 1x/no-benchmark gameplay and FPS gate.
- Snapshot provenance must identify the changed workforce design; do not reuse the 78-worker report
  as final evidence. Root validates cargo conservation and retirement on the legacy checkpoint.
- Ground stones are shader gravel, with no LandscapeRock mesh/collider in natural terrain.
- Every carried layer has exactly 2 rows x 3 columns; storage items rest on their support plane,
  using rotated mesh bounds, and elongated produce lies horizontally.
- Actual building footprints, walls, counters and furniture require matching solid colliders.
