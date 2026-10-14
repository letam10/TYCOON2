# V12 frame pacing review

## Verified configuration and behavior

- The active full IL2CPP tour is PID 2280, run folder
  `work/city-expansion-20261008/ordinary-play-20261010T050540824Z`. Its Player.log
  records the D3D11 warning at lines 44-45: Unity detected refresh-rate time drift,
  then declared VSync broken because it was not limiting frame rate properly.
  Unity switches delta-time calculation to CPU-side timestamps until the window moves.
- This warning is direct runtime evidence that Unity's current D3D11 presentation
  path rejected refresh-based timing. It is not evidence that the application asked
  for VSync off, nor does it identify whether the trigger is Unity, Windows window
  scheduling, the display/VRR state, or a driver override.
- `GameSettingsStore.Apply` expresses display-sync mode as `vSyncCount=1` and
  `targetFrameRate=-1` (`Assets/_Game/Scripts/Runtime/GameSettingsStore.cs:65-66`).
  That configuration is an intent, not proof that presented frames are synchronized.
  `CityPlayReport` records the requested `syncDisplay` and a Unity-reported refresh
  rate; neither field measures scan-out or presentation cadence.
- The launch script always sets D3D11, GPU index 0, 1920x1080 windowed mode and
  `--qa-city-play`; it does not pass `--qa-benchmark`. It writes a per-run settings
  file only for `-ShowFps` or `-Manual165`. The active run has no settings.json, so
  `GameSettingsData` defaults apply: sync enabled and 165 FPS configured. Its full
  report is not yet present while the tour is running.
- `--qa-benchmark` is the only inspected path that overrides settings to VSync off
  and unlimited target rate. It is absent from this run's launcher arguments.
- Moving the window is Unity's own stated recovery condition. No evidence yet shows
  that moving this window clears the warning or restores refresh-locked presentation.

## Minimal next experiment

After the current tour exits, do one fresh, matched ordinary-play capture with the
same IL2CPP executable, checkpoint, 1920x1080 windowed mode, workload, and default
settings. Do not pass `-ShowFps`, `-Manual165`, or `--qa-benchmark`. Keep the window
stationary for the initial segment, record the warning and first-segment pacing, then
move the window once as the log instructs and record the second segment separately.
Do not alter window size, fullscreen mode, display mode, driver profile, or workload
during this run.

Record the exact time of the move and compare complete frame timestamps before and
after it, while retaining every hitch. Check the report's `displaySync`, configured
limit, and reported refresh alongside average FPS, frame-time percentiles, worst frame,
and counts over 16/33/100 ms. Preserve the warning text and timing in Player.log.
If moving the window clears the warning and pacing becomes refresh-limited, the result
supports a window/presentation-state trigger; it still does not identify which layer
caused it. If the warning persists or pacing does not change, the move is not a remedy
in this setup. Then inspect the exact executable's NVIDIA per-program VSync and frame
limit settings read-only, and separately compare with the existing `-Manual165`
software-cap mode. Do not change the FPS acceptance gate or claim VSync success from
settings/report fields alone.

## Limits

- The active run has not yet produced its final report, so no FPS outcome for this
  warning can be stated here.
- No presentation timestamps, compositor telemetry, VRR state, or per-program NVIDIA
  profile values were available in the reviewed files.
- Existing Unity API references document requested controls and display properties;
  they cannot establish actual scan-out cadence. See
  `Docs/V10_FRAME_PACING_FINDINGS.md` for the prior review and official reference URLs.
