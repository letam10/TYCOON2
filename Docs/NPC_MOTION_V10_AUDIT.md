# NPC motion evidence audit: ordinary Player v10

## Finding

The run does **not** establish that NPC walk animations failed. It establishes that this report's sampled
bone-motion predicate returned zero. Current source shows a strong measurement blind spot: actor poses are
sampled only once per second, while the verdict depends on the angular difference between consecutive sampled
local rotations. A looping walk can return to the same or nearby phase at those intervals, making real gait
read as little or no movement. The report records no per-actor state, speed, bone count, Animator phase, or
measured angle, so phase aliasing is plausible, not proven as the sole cause.

## Evidence

- `CityPlayReport.Update` calls `actors.Sample(game)` on a one-second cadence.
- `CityActorEvidence.Sample` initializes each track's current pose then skips measurement for that first
  observation. Later it adds angular deltas only when `ActualSpeed > .4` and `ActorView.State` is Walk, Run,
  or CarryWalk. `Save` counts a track animated only if distance exceeds 3 m, a qualifying state was seen,
  and the **largest single sample's summed angle** exceeds 12 degrees.
- The failed run `work/city-expansion-20261008/ordinary-play-20261009T131318597Z` reports 98 observed,
  86 walked and 0 animated NPCs, with no runtime errors. This proves locomotion/path coverage, not gait failure.
- `CharacterWalkingTests.WalkingClipActuallyMovesLegBonesOnCurrentPrefab` checks the same name filter,
  requires at least two bones and more than 12 degrees between phases 0.12 and 0.62 of a looping clip.
  The saved EditMode results at `work/items-vehicles-20261009/editmode-results.xml` show all five cases
  passed (player Walk/CarryWalk and customer Walk variants). This supports valid bindings and animated clips,
  but samples clips directly and does not prove ordinary Player Animator state/culling behavior.
- Earlier ordinary Player outputs in the same city QA series recorded 49/55, 60/63 and 78/90 animated/walked
  NPCs (`evidence-final-20261008/fresh`, `ordinary-play-20261008T072739868Z`, and
  `ordinary-play-20261008T060110701Z`). Thus the current zero is a run-specific evidence regression, not a
  demonstrated missing rig/clip regression.
- `ActorView.Initialize` sets `Animator.cullingMode = CullUpdateTransforms`. Unity may therefore leave bone
  transforms unrefreshed for culled renderers. Whether visibility/occlusion explains this run is unknown:
  the evidence does not record renderer visibility or camera distance. Do not infer that hidden NPCs must
  keep advancing their poses.

## Minimal next step

Add temporary, low-volume diagnostic counters to the existing evidence path: for each observed actor record
bone count; count samples with speed above threshold; count samples with an eligible state; count eligible
samples with any nonzero angle; record maximum and mean angle; include Animator current state hash/normalized
time and renderer visibility. Sample leg transforms every rendered frame (or accumulate maximum motion
between the existing one-second report writes), while retaining one-second file/report output. This separates
state/controller failure, culling, and sample-phase aliasing without requiring hidden NPCs to animate.

## Surgical fix and meaningful acceptance

Replace the one-second pose-delta measurement with a per-frame accumulator for actors that are both moving
and in a walking state; keep distance and pass thresholds unchanged. Do not change `ActorView` clips or
culling policy based on this report alone. Add a focused test that advances a looping walk clip at a cadence
that aliases its phase and shows the current sampler can miss the threshold while a per-frame accumulator
passes. Then rerun ordinary Player and require at least eight walked NPCs to have qualifying runtime state
and accumulated leg motion above 12 degrees. Include counts for each gate so a future zero identifies the
failed predicate. Keep the existing EditMode prefab test as rig/clip coverage; it is not runtime acceptance.

## Scope

Read-only audit of code and saved evidence; no Unity/Player run, code/test/asset/save changes, or publication.
