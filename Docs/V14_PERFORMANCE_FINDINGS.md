# V14 performance findings

## Current acceptance

Stable 165 FPS throughout the city is not proven and the goal remains active.
The 165 Hz display reported by Unity does not establish actual presentation cadence.
Hardware telemetry from a hidden-window Player does not establish visible-game utilization.

## Uninstrumented long V13 gameplay

Source: work/city-expansion-20261008/ordinary-play-20261010T063058411Z.
186748 frames across 935.489 seconds, 39 workers and 19 customers.
Average engine FPS 199.626; P95 6.914 ms, P99 9.183 ms, worst 603.454 ms.
Functional gameplay passed but stable165FrameBudget is false.
There were 85 frames above 16.67 ms, 16 above 33.33 ms, and three above 100 ms.
CPU/GPU mean frame durations were 4.974/1.906 ms from 900 valid timing samples.

Checkpoint operations copied transaction snapshots in roughly 19-31 ms during the tour.
Several hitch frame numbers coincide with these checkpoint frame numbers.
This does not explain the separate 603 ms and 376 ms spikes.

## Instrumented stationary V13 diagnosis

Source: work/city-expansion-20261008/ordinary-play-20261010T074248593Z.
12153 frames across 60.258 seconds; instrumentation is not an FPS acceptance comparison.
Native Update scripts averaged 2.362 ms/frame; LateUpdate scripts averaged 0.895 ms/frame.
Native AI update averaged 0.436 ms/frame; native Main Thread averaged 4.932 ms/frame.
ActorPhysicalMotion.Update averaged 0.799 ms/frame, including its nested scopes.
CapsuleMove averaged 0.576 ms/frame and WorkerAgent.Update averaged 0.309 ms/frame.
Nested scope totals overlap; do not add them to reconstruct a frame duration.
The checkpoint transaction snapshot measured 27.510 ms; total SaveGame.Checkpoint was 34.726 ms.
The next native PlayerLoop sample measured 45.699 ms and Update scripts 40.048 ms.
This supports optimizing the measured snapshot path without removing collision or NPC simulation.
Unity FrameTiming yielded no valid CPU/GPU timing pair in this stationary run.

## Hardware sample at software 165 cap

REJECTED for rendered gameplay acceptance: direct inspection found scenery-farm.png entirely black.
The counters below describe the process/device at that time; do not use them as gaming hardware values.

Source: work/fps-hardware-20261010/samples-20261010T073717069Z.
26 observations over 36.056 seconds after a 15-second warm-up.
CPU game mean 7.73% (5.31-10.02), normalized to all 16 logical processors.
Whole-system CPU mean 21.38% (12-44); GPU whole-device mean 4.50% (3-7).
GPU graphics clock 1.890 GHz and GPU temperature 48 C in every observed sample.
CPU actual boost clocks and CPU temperature are unavailable.
Windows reports 3801 MHz, which is not a directly measured CPU boost clock.
The simultaneous Player averaged 163.697 FPS with a 192.297 ms worst frame.
It was stationary, rendered in a hidden window, and failed NPC animation evidence.
These numbers do not establish hardware load during fully rendered gameplay near 165 FPS.

## Changes and verification

Typed history copying completed; native Checkpoint tests 24/24 passed, including 9 new fixture cases.
Candidate IL2CPP build exited 0. Visible stationary V14 T081545725Z passed with 20 nonblank captures.
It averaged 162.836 FPS; P95 6.186 ms, P99 7.661 ms; stable165FrameBudget remains false.
NPC evidence observed 65 walked and 10 animated actors, passing the existing acceptance rule.
Checkpoint snapshots measured 11.237/8.789 ms; full checkpoint work measured 16.091/10.934 ms.
These are measured operation costs, not a matched proof of whole-game FPS improvement.
Validated short hardware scope and numbers are in work/fps-hardware-20261010/RENDERED_MEASUREMENT_081545.md.
Capture validation rejected 20/20 black captures and accepted 42/42 rendered V13 tour captures.
The launcher now performs this nonblank capture check after the Player exits.

Typed history copying is assigned under
Docs/History/LEGACY_DOCS_20261011.md (SOURCE: V14_PERFORMANCE_NPC_CONTRACT.md).
Keep all receipt and event data and independent mutable objects; no history pruning or schema change.
Run native checkpoint tests, then build an IL2CPP candidate and compare its actual checkpoint timings.
Retain raw hitches. A faster checkpoint alone does not establish stable 165 FPS.
NPC evidence diagnosis is documented separately in Docs/V14_NPC_DIAGNOSTIC.md.
Visible-window presentation, sustained thermal behavior, and CPU sensors still require verification.

## Live V14 tour and hardware segment

Tour: work/city-expansion-20261008/ordinary-play-20261010T085522987Z, PID 35148, launcher session 67861.
Normal window, 1080p, maximum city, live simulation, default display synchronization.
The exact existing process completed with exit 0; it was not restarted after observation timeouts.
The final report passed functional gameplay, 937.574 seconds, 172600 frames and 456 interactions.
39 workers and 23 customers were present; the player walked 4645.634 metres.
Engine mean 184.092 FPS, P95 8.819 ms, P99 12.261 ms, worst 587.419 ms; stable165 false.
There were 318 frames above 16.67 ms, 47 above 33.33 ms and six above 100 ms.
Mean CPU/GPU frame times were 5.675/2.115 ms across 924 valid timing samples.
All 42 captures passed the nonblank gate; farm and carrot stack images were inspected directly.

Hardware: work/fps-hardware-20261010/samples-20261010T085522643Z.
128 samples over 180.583 seconds after warm-up; this covers only the beginning of the tour.
CPU game mean 17.44% (6.00-24.19); CPU total mean 61.63% (26-100).
GPU total mean 75.71% (20-98), graphics clock 2.467 GHz (1.890-2.535).
GPU temperature mean 77.39 C (66-84), power mean 73.03 W (25.53-91.84).
Total CPU/GPU readings include other applications and must not be attributed entirely to the game.
Do not combine these readings with the stationary software-cap FPS as if they were one configuration.
The tour log reports broken IDXGISwapChain timestamps and CPU timestamp fallback.
Consequently engine FPS alone does not verify physical 165 Hz presentation.

The sensor audit found installed HWiNFO but no active sensor interface for genuine CPU temperature/clock.
Typed null fields remain correct; the Windows 3801 MHz field is not effective-clock evidence.
Tools/measure_game_hardware.ps1 now requires executable, scope and output folder below work/.
It records PID, executable, sample UTC bounds, requested duration and hardware identity.
Python packaging tests passed 3/3; the V14 source whitelist contains 2059 existing files.
No V14 ZIP is published or called final; frozen V13 ZIPs remain unchanged.

Full native V14 suite passed 896/896, zero failed/skipped, native process exit 0.
Evidence: work/city-model-v14-20261010/full-tests-final.xml and full-tests-final.log.
Compared 303 packaged V13 runtime CSharp files with current bytes; only CheckpointSnapshot.cs changed.
Model generator source therefore remains covered by the inherited V13/V11 captures within that scope.
The tool verification collected 11 samples over 15.404 seconds, same PID, near the end of the process.
GPU mean 35.27%, graphics 2.150 GHz, temperature 58.45 C (56-62); it is a separate short window.
Raw: work/fps-hardware-20261010/samples-20261010T091027029Z.
Tour snapshot costs ranged 10.866-29.835 ms and checkpoint totals 14.016-33.613 ms, excluding final save.
This exceeds the 6.061 ms budget; checkpoint optimization has not established stable 165 FPS.
