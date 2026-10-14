# V13 ocean QA integration review

## Existing integration points

- `QaDriver.Start()` dispatches `--qa-city-models-v11` to `CityModelsV11()`.
- `CityModelsV11()` includes the three coast landmarks in its 13-landmark loop,
  then calls the unrelated prop galleries and `WaterDetailComparisonV11()`.
  Reusing that whole routine would make the focused QA unnecessarily long.
- `CaptureModelAngles(string prefix, Vector3 center, float distance,
  bool elevation = true, float yawOffset = 0)` already captures named front,
  right, rear, left, and top views when `elevation` is false. Each view calls
  `Capture(prefix + "-" + name + ".png")`.
- `Capture(string name)` writes under `game.QaDirectory` and appends the name to
  `report.screenshots`. Its guard skips captures on a null graphics device.
- `GameSession.Awake()` consumes `--qa-output`; `Start()` attaches `QaDriver`
  for ordinary QA flags. The runner currently provides no V13 task alias.
- `WaterDetailComparisonV11()` is a close timing template: set a material
  parameter, warm up, collect frame timing samples for 15 realtime seconds,
  then restore state in `finally`. It reports averages and frame percentiles.
  Its current ABBA variable is `_Detail`, so it does not measure the horizon.

## Minimal extension

Add a focused `IEnumerator OceanV13Acceptance()` to the existing `QaDriver`
partial class, with dispatch before the default campaign selection:

```csharp
if (Array.Exists(arguments, x => x == "--qa-ocean-v13"))
{
    mode = "ocean-v13";
    routines.Push(OceanV13Acceptance());
}
```

Reuse `CaptureModelAngles` directly for the three placed coast landmarks:

```csharp
IEnumerator CaptureCoast(string id)
{
    var landmark = game.GetComponentsInChildren<CityLandmark>()
        .Single(x => x.Id == id);
    var bounds = ModelBounds(landmark.transform);
    yield return CaptureModelAngles("ocean-v13-" + id, bounds.center,
        Mathf.Max(20f, bounds.size.magnitude * 1.8f), false, 270f);
}
```

Call it for `Harbor`, `Beach`, and `Marina`; this produces 15 screenshots:
`ocean-v13-{Harbor|Beach|Marina}-{front|right|rear|left|top}.png`.
The existing angle helper restores camera and fog state after each landmark.
Check that each target exists and assert all 15 names appear in
`report.screenshots` before the final standard `passed` report is written.

For the cost phase, extract the timing loop shape from
`WaterDetailComparisonV11()` into a small helper, for example
`IEnumerator MeasureHorizonRun(bool enabled, HorizonRun result)`. Keep the
existing beach camera (`yaw=235`, `pitch=42`, `distance=27`, focus `(252,.1,70)`)
and one rendered scene for every run. Freeze gameplay with `Time.timeScale=0`
and `game.Player.CanControl=false`; wait for navigation before the timing
sequence. Run horizon states in this exact order: off, on, on, off. For each
state, set `OceanVisualHorizon` active/inactive, allow a 2-second realtime
settle, then sample for 15 seconds with `FrameTimingManager` and
`Time.realtimeSinceStartupAsDouble`. Exclude image capture and settle frames
from the measured interval. Record measured duration and valid GPU sample count
as well as CPU/GPU mean milliseconds and frame count; zero valid GPU samples
must be explicit rather than interpreted as zero cost. Restore horizon state,
camera, time scale, and player control in `finally`.

Write one `ocean-v13-report.json` after the routine with the ordinary
`QaReport` outcome (`passed`, `runtimeErrors`, `screenshots`) plus a
`renderCostRuns` array containing each run's `horizonEnabled`, sequence index,
measured seconds, frames, `cpuMs`, `gpuMs`, and `timingSamples`. Keep the
existing final `QaDriver.Start()` report path so runtime errors and process
exit status remain consistent. This focused routine should not call
`CityModelsV11()` or `WaterDetailComparisonV11()` wholesale.

## Launch and acceptance scope

The existing direct Player invocation accepts a QA mode and output directory:

```powershell
& 'Builds/Windows/TYCOON2.exe' -force-d3d11 -force-device-index 0 `
  -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 `
  -logFile 'work/logs/player-ocean-v13.log' --qa-ocean-v13 `
  --require-4060 --qa-output 'work/ocean-v13/player'
```

Alternatively, add `OceanV13` to `Tools/run_player.ps1`'s `ValidateSet` and
task-to-flag map, using a unique timestamped `work/ocean-v13/player-*` output
directory. That is the smallest reusable scripted launch integration. Keep
the run visible on the target RTX 4060 laptop; the desktop is currently locked
and native input is unavailable, so this review defines the harness only and
does not claim a completed Player run.

Acceptance evidence is the final JSON (`passed=true`, empty `runtimeErrors`,
all 15 coast image names, and four ordered 15-second horizon measurements)
plus the corresponding PNGs and Player log. Review all 15 images for each
landmark's five views. This measures whole-scene frame/GPU timing with the
horizon toggled under one frozen gallery scene; it does not isolate GPU cost
to the horizon alone. It also does not establish gameplay FPS, 165 Hz
presentation, or performance during active simulation.
