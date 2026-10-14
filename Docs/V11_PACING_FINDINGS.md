# V11 pacing findings

## Measured facts

- The referenced capture is `work/city-expansion-20261008/ordinary-play-20261010T015709724Z`.
  Its private settings specify `syncDisplay: false`, `fpsLimit: 165`, and 1920×1080.
  The report confirms 39 workers, 24 customers, 165 Hz reported display refresh, D3D11,
  and RTX 4060 Laptop GPU.
- The 62.295-second capture contains 9,646 frames: 154.845 average FPS, P95 6.895 ms,
  P99 9.054 ms, and 2,300.555 ms worst frame. Nine frames exceed 16 ms, four exceed
  33 ms, and one exceeds 100 ms. The report explicitly marks
  `stable165FrameBudget: false`. Mean sampled GPU time is 2.074 ms.
- Native marker means are about 6.22 ms for Main Thread, 6.21 ms for PlayerLoop,
  and 0.71 ms for WaitForTargetFPS. These scopes overlap: PlayerLoop is inside the
  Main Thread, and target waiting is not extra application work to add to those totals.
  A 0.71 ms mean wait alongside 154.8 FPS suggests the software cap is not spending
  much time sleeping on average; it does not prove the cap is misconfigured.
- Hitch samples show a few isolated expensive frames (including ~57–75 ms frames,
  plus a reported 2.3 s outlier). Their cause is not established by these aggregate
  reports. The low mean GPU time makes sustained GPU saturation unlikely in this
  capture; it does not rule out presentation or CPU spikes.
- Source defaults `syncDisplay` to true and `fpsLimit` to 165.
  `GameSettingsStore.Apply` selects VSync count 1 and target -1 when synchronized,
  or VSync count 0 and target 165 when manual. The launcher writes
  `syncDisplay:false` only with `-Manual165`; `-ShowFps` alone writes true.
  The default settings screen also selects display sync. The report above is explicitly
  the manual-cap case, regardless of its folder's “ordinary-play” name.
- `ActorView.Initialize` already sets `Animator.cullingMode` to
  `CullUpdateTransforms`. This is not a missing optimization.

## Interpretation

The existing evidence measures one software-cap configuration. It cannot compare the
cap to display synchronization. The mean is below 165 and the report rejects the stable
165 frame budget, so do not claim stable 165 FPS or infer that changing synchronization
will solve it. The low GPU mean and appreciable main-thread work suggest CPU-side work or
sporadic stalls deserve attention if the matched run reproduces them, but marker averages
do not identify a specific function to change. Do not sum nested markers or use excluded
frames to improve the reported rate.

The settings code expresses the intended modes clearly. No concrete source edit is
justified by this one capture.

## Minimal next action

Run two fresh captures from the same build, same restored checkpoint, and same Player
conditions, using the same normal live-play duration and maximum-city state:

1. Display synchronization: `Tools/run_city_play.ps1 -LoadPath <same-checkpoint>`
   (optionally add `-ShowFps` only if the overlay is desired; keep it consistent
   between both runs).
2. Manual cap: `Tools/run_city_play.ps1 -LoadPath <same-checkpoint> -Manual165`
   (again, keep `-ShowFps` consistent).

Verify the resulting private settings and report fields: 1920×1080, same display,
refresh, build, checkpoint, workers/customers, resolution and graphics settings;
`displaySync:true` versus false; and unchanged 165 software limit. Compare complete
run average FPS, frame-time percentiles, frames over 16/33/100 ms, GPU samples, and
native CPU markers. Repeat in reverse order (manual then sync) to reduce warm-up/order
bias. Avoid FPS overlay in both runs if possible because it adds measurement/UI work;
the launcher's private setting can keep it off while exercising either mode.

Choose the default from the two modes' repeatable frame pacing and presentation quality,
not from the nominal 165 Hz alone. If sync mode tracks refresh smoothly while manual mode
has uneven presentation, keep display sync as default. If sync mode has unexplained drops
but manual mode is repeatably better, inspect those frames and verify on the actual
display before changing default behavior. A source change is warranted only if the
matched evidence demonstrates an implementation defect or supports a clear user-facing
policy choice.

## Evidence still needed for stable 165

Stable 165 requires the full frame-time distribution under the intended acceptance
scenario, including sustained worst-case work and hitch causes. The current capture is
one 62-second manual-cap diagnostic; its P95/P99 are under 16 ms but it has outliers and
a 154.8 average. Neither the percentiles nor average alone establish continuous 165 Hz
delivery. Preserve all frames and report the same fixed interval for both modes.

## References

- Unity `QualitySettings.vSyncCount` API:
  https://docs.unity3d.com/6000.0/Documentation/ScriptReference/QualitySettings-vSyncCount.html
- Unity `Application.targetFrameRate` API:
  https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-targetFrameRate.html

The documentation lookup service returned HTTP 404 during this review, so the URLs above
are the intended official Unity references; their current contents were not independently
retrieved in this run.
