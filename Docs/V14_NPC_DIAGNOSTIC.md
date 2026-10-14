# V14 capped NPC animation diagnostic

## Finding

The capped Player run demonstrates a failure of the current NPC animation acceptance predicate, but does not
demonstrate that NPCs actually failed to animate. It counted 68 NPCs that traveled over 3 m, yet counted no
animated NPCs. The separate long Player run counted 81 animated of 87 walked NPCs and passed the same predicate.
This makes a general broken rig, clip, or locomotion-state regression unlikely; the capped run's zero remains an
unresolved run-specific runtime failure, with background-window visibility/culling and sparse sampling both
credible explanations. Do not waive the failed gate based on the long run.

## Evidence

- `work/city-expansion-20261008/ordinary-play-20261010T073717087Z/npc-motion-evidence.json` reports
  92 observed, 68 walked, 0 animated, and `passed: false`; its ordinary-play report is only 60.19 seconds,
  `maximum mod diagnostic with live gameplay`, with no runtime errors. This is the capped stationary run.
- `work/city-expansion-20261008/ordinary-play-20261010T063058411Z/npc-motion-evidence.json` reports
  99 observed, 87 walked, 81 animated, and `passed: true`. The corresponding ordinary-play report covers
  935.49 seconds, reports the NPC animation check completed, and has no failures or runtime errors. Its
  1920x1080 captures are substantial; the capped run's `city-overview.png` and scenery captures are each
  27,260 bytes, consistent with the documented hidden-window capture problem, but not proof of renderer
  visibility for each NPC.
- `CityPlayReport.Update` calls `actors.Sample(game)` no more often than once per second
  (`Assets/_Game/Scripts/Runtime/CityPlayReport.cs:71-73`). The first observation only initializes the track
  and pose (`Assets/_Game/Scripts/Runtime/CityActorEvidence.cs:39-52`). Each later observation compares leg
  local rotations across the entire one-second interval and retains the largest single sample sum, not
  accumulated motion (`CityActorEvidence.cs:63-70`). A looping gait can return near its initial pose at that
  cadence. Tracks need >3 m distance, an eligible locomotion state, and a >12 degree sample to count animated;
  at least eight walked and animated tracks are required (`CityActorEvidence.cs:59-61, 74-86`). The report
  records none of the per-actor state, angle, or visibility gates, so the zero cannot identify which gate failed.
- `ActorView.Initialize` uses `AnimatorCullingMode.CullUpdateTransforms` (`Assets/_Game/Scripts/Runtime/ActorView.cs:18-24`).
  If a renderer is culled, this mode can leave bone transforms unrefreshed. `CityActorEvidence` neither records
  `SkinnedMeshRenderer.isVisible` nor forces visibility or animation (`CityActorEvidence.cs:32-70`). Thus a
  background/occluded NPC can be counted as walked while its sampled bones appear stationary.
- The ordinary locomotion path selects Walk/Run/CarryWalk based on speed and carry state and adjusts Animator
  speed (`ActorView.cs:36-45`); `Play` cross-fades when the requested state changes (`ActorView.cs:72-89`).
  Nothing in the inspected code establishes that this path failed in the capped run. The long-run result is
  positive live-Animator evidence, while the existing prefab clip test is only clip/rig coverage and cannot
  resolve capped Player visibility or cadence.
- The capped run used `displaySync: false`, `runtimeVSyncCount: 0`, and target 165 (`ordinary-play-report.json`);
  the long run used display sync and VSync 1. These reports also differ in duration and gameplay/camera evidence,
  so they are not a controlled comparison. The capped report's `walkedMetres: 0` alongside 5,706 m total NPC
  distance confirms stationary Player coverage, not stationary NPCs.

## Smallest next step

Repeat the capped 60-second workload in a visible Player window with the same executable, checkpoint, capped
settings, and stationary camera, and capture a frame showing a known walking NPC. In the same run, temporarily
record per-actor renderer visibility, Animator state hash/normalized time, eligible-state sample count, and leg
angle at rendered-frame cadence while preserving the current distance and 8-NPC acceptance thresholds. This
distinguishes (a) hidden/culled bones, (b) an Animator state issue, and (c) one-second phase aliasing. If visible
NPCs still fail, the smallest fix is to accumulate maximum or total leg motion per frame during eligible walking
samples; retain the existing acceptance thresholds and report each gate. Change culling only if the visible
controlled run proves that it suppresses needed animation.

## Conclusion boundary

Treat the capped result as a real acceptance failure requiring reproduction, not as proof of a gameplay defect
and not as ignorable background telemetry. The long run shows animation can pass during ordinary Player gameplay;
it does not establish capped stationary presentation. Background-window frame/GPU measurements remain unsuitable
as visible-game load evidence, and the available reports contain no per-NPC visibility or Animator diagnostics.
