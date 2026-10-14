# V10 frame pacing findings

## Evidence

- The visible profile is a 1920x1080 windowed Player on the RTX 4060 Laptop GPU,
  D3D11. Its settings snapshot requests `syncDisplay=true` and `fpsLimit=165`.
  The captured Player log identifies Unity 6000.6.3f1 and the graphics device.
- `GameSettingsStore.Apply` sets `QualitySettings.vSyncCount=1` and
  `Application.targetFrameRate=-1` when display sync is selected
  (`Assets/_Game/Scripts/Runtime/GameSettingsStore.cs:65-66`). The QA acceptance
  checks that exact pair, and separately checks `vSyncCount=0` with target 165
  (`Assets/_Game/Scripts/Runtime/QaCarrotSettingsAcceptance.cs:105,141`).
  This confirms the intended Unity-side configuration; settings.json alone does
  not prove the active driver profile or effective presentation behavior.
- The Player log reports refresh-time drift twice, then says VSync does not
  limit the frame rate and switches delta-time calculation to CPU-side timing
  (`work/city-expansion-20261008/ordinary-play-20261009T134351129Z/Player.log:29,33,35`).
  It records the 213.31 FPS result at line 47. This is direct evidence Unity's
  D3D11 runtime detected broken pacing for this run. It is not proof of a
  specific Unity defect or of an NVIDIA override.
- The paired report records 213.31 average FPS, P95 6.76 ms, a 783.60 ms worst
  frame, 14 frames over 16 ms, 7 over 33 ms, and 3 over 100 ms. Average reported
  CPU/GPU times are 4.15/3.49 ms; full-city average is 187.71 FPS
  (`ordinary-play-report.json:13-23`). The roughly 4.69 ms average frame time
  and sub-6 ms average CPU/GPU times show substantial average throughput above
  165 Hz's 6.06 ms budget. The 213 FPS average is therefore a present pacing
  failure, not evidence that rendering cannot reach 165. The first timeline
  sample is 783.60 ms at 0.43 s with CPU/GPU unavailable, consistent with a
  startup hitch. This single sample cannot establish its cause; it also cannot
  explain the persistent 213 FPS average.

## Interpretation and recommendation

- Unity's documented desktop behavior is to use `vSyncCount` for display
  synchronization; with VSync enabled, `targetFrameRate` is not the controlling
  cap. With VSync disabled, Unity's software `targetFrameRate` cap applies.
  Therefore setting both VSync and target 165 is not a reliable correction.
  The project already has the correct two modes.
- No source change is justified by this capture. First inspect the per-program
  NVIDIA profile for the exact shipped `TYCOON2.exe`, read-only, and record
  Vertical sync and Max Frame Rate. An app-level setting can be overridden by
  the driver; this capture does not report those profile values.
- Smallest controlled experiment: if the per-program profile forces VSync off
  or has a conflicting frame cap, set that profile to application-controlled
  VSync with no driver frame cap, then rerun the same visible build and exact
  1920x1080 windowed scenario. Log the effective Unity values, actual display
  refresh, frame-time distribution, and Player log. Keep quality, resolution,
  window position, driver profile, and workload fixed. A result near 165 FPS
  without the warnings would implicate the override; if the profile already
  defers to the application and the warnings/rate persist, the evidence points
  toward Unity's D3D11/windowed presentation path or display scheduling, not a
  rendering-throughput limit. This still needs a second run to establish
  repeatability.
- If the NVIDIA profile already defers to the application, leave it unchanged
  and compare a second visible run with the existing VSync-off, software-165
  mode. That checks whether the software cap can hold this workload near 165;
  it does not isolate the cause of failed VSync.
- If the immediate product requirement is a 165 FPS ceiling and display
  synchronization is not essential, the existing VSync-off/software-cap mode
  is the smallest workaround to measure. It is not equivalent to refresh-locked
  presentation and should be reported separately.

## Primary references

- Unity 6 `QualitySettings.vSyncCount`:
  https://docs.unity3d.com/6000.0/Documentation/ScriptReference/QualitySettings-vSyncCount.html
- Unity 6 `Application.targetFrameRate`:
  https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-targetFrameRate.html
- Unity 6 `Screen.currentResolution` and refresh-rate reporting:
  https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Screen-currentResolution.html
- NVIDIA Control Panel program settings, including application-level 3D overrides:
  https://www.nvidia.com/content/Control-Panel-Help/vLatest/en-us/mergedProjects/Manage_3D_Settings/Program_Settings.htm

## Remaining uncertainty

- The NVIDIA per-program profile, Windows presentation mode, and monitor/VRR
  state were not inspected. The Player's 165 Hz value is Unity-reported and the
  monitor mode was verified externally per the investigation contract.
- The log's recovery condition says to move the window; this capture does not
  test whether moving it restores VSync. Do not infer that it does.
- Metrics are from one diagnostic run, which includes startup and maximum-mod
  workload. A clean ordinary-play run without profiling is needed to characterize
  steady-state pacing and reproduce the anomaly.
