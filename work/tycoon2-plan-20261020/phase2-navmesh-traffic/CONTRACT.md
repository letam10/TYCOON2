# TYCOON2 Phase 2 navigation, pedestrian, traffic and interaction contract

## Scope

This tranche advances the objective's Phase 2 only: make NavMesh, pedestrian,
traffic and hidden logical interactions consume the current world geometry
contracts. It must preserve gameplay economy, transaction state, SaveData
schema, stable POI/route/station IDs, camera pitch/yaw and boarding persistence.
No asset drop, map redesign, interior redesign or performance claim belongs to
this tranche.

## Current source contracts

- `WorldRouteCatalog.BuildDefault()` is the sole source of authored corridor
  IDs, route kind, width, reservation metadata and immutable `SampledNodes`.
- `WorldRouteCatalog.BuildPublicApproaches()` is the sole source of public
  approach samples. Existing route and approach IDs must not change.
- `WorldPathSurfaceCatalog` and `WorldPathSurfaceState` remain the source for
  generated walkable surface pieces and their live mesh/collider layout.
- `NavigationWorld` owns the generated `NavMeshSurface` build. `Navigation.Go`,
  `Navigation.Arrived`, `Navigation.Warp` and `Navigation.Suspend` remain the
  public agent boundary.
- `CityPedestrianNetwork.TryPlan` and `ValidateRoute` remain the public graph
  boundary. `CityPedestrianRoute` remains the only waypoint sequencer for an
  agent. `ActorPhysicalMotion` remains the transform bridge for agents.
- `CityTraffic` owns ordinary road vehicle transforms. `CityTrafficSignals`
  owns signal phases and stable signal IDs. A traffic vehicle may not be moved
  by a second component.
- `PlayerController` owns player transform/input. `PlayerInteractionSession`
  owns the 0.25 second dwell. `WorldInteractionArea` owns logical approach,
  stable ID, cooldown and action. `WorldPlayerRide` owns boarding, reparenting,
  safe exit and `PlayerRideSave` compatibility.

## Stable identity and data rules

Keep every existing `WorldRouteCorridor.Id`, `WorldPublicApproach.Id`,
`RouteKind`, POI `StableId`, `WorldInteractionArea.SaveKey`, signal ID,
station ID, item ID and transaction owner ID unchanged. Do not add a second
save authority or modify `TransactionState`, `SaveData`, `PlayerRideSave` or
the transaction command vocabulary in this phase. Dynamic carrier areas must
resolve through a stable interaction ID before a Player ride can be saved.

## Motion ownership rule

Exactly one component owns each transform in a frame:

1. Player: `PlayerController` and its `CharacterController`.
2. NavMesh actor: `NavMeshAgent` plus `ActorPhysicalMotion`.
3. Road vehicle: `CityTraffic`'s vehicle state and route step.
4. Rail/boat carrier: the existing carrier implementation.

Route planning, signal checks, diagnostics and QA may read transforms but may
not write them. Every movement distance must derive from the owning speed and
`deltaTime`; no duplicate speed multiplier or frame-independent teleport may be
introduced.

## Worker partitions

Worker A may inspect and change only `CityPedestrianNetwork.cs`,
`CityPedestrianRoute.cs`, `NavigationWorld.cs`, and one new focused EditMode
test file under `Assets/_Game/Tests/EditMode/`. It must preserve public method
signatures and prove endpoint sampling, NavMesh leg connectivity, restricted
corridor exclusion and route recovery behavior from source-level fixtures.

Worker B may inspect and change only `CityTraffic.cs`, `CityRoadRoute.cs`,
`CityTrafficSignals.cs`, and one new focused EditMode test file under
`Assets/_Game/Tests/Editor/`. It must replace independent route geometry only
where the current sampled route contract proves the mapping, preserve signal
IDs/offsets, and cover blocked/deadlock/signal behavior without changing
vehicle transform ownership.

Worker C may inspect and change only `WorldInteractionArea.cs`,
`PlayerInteraction.cs`, `ProximityTarget.cs`, and one new focused EditMode test
file under `Assets/_Game/Tests/Editor/`. It must keep the 0.25 second logical
dwell and stable save/boarding behavior, hide debug geometry by default, and
avoid changing interaction semantics for stations or transaction actions.

Workers may not edit the same source file, may not create agents, may not run
Unity/build/Player/browser/server/benchmark, and may not commit or push. If a
required change crosses partitions, stop and report the seam to `/root`.

## Root integration and gates

Root will review every diff, resolve cross-partition seams, add or edit the
single Phase 2 QA entrypoint if needed, then run the narrowest EditMode filter,
Windows build, focused Player navigation/traffic/interaction smoke and fresh
visual/log review. Success requires source/build/fixture evidence plus runtime
evidence for the exact scope; focused tests alone do not prove NavMesh,
boarding, visual or FPS acceptance. Only after a clean allowlist and remote-head
check may root stage, commit and push this tranche.
