# V12 IL2CPP candidate contract

## Goal and existing state

- Windows Unity 6000.6.3f1, URP 17.6, RTX 4060 Laptop, 1080p; stable165 is still unmet.
- V12 Mono release has 876 native tests PASS and a live sixty-second Player PASS.
- Same-save Mono run: ordinary-play-20261010T043102004Z, 157.771 FPS, P95 7.586 ms.
- The bounding side is CPU; model, shaders, simulation, NPC collisions and saves remain unchanged.
- Toolchain inventory finds VS Community 18 and SDK 10.0.26100.0; Unity compatibility is unproven.
- Main may add Editor/OptimizedPlayerBuilder.cs and a build-only Il2Cpp switch to Tools/run_unity.ps1.
- BuildWindows candidate calls the existing ProjectBuilder.BuildWindows with IL2CPP selected temporarily.
- Always restore the previous scripting backend in finally; preserve all other project settings.
- Candidate output is work/city-model-v12-20261010/Il2CppCandidate/TYCOON2.exe.
- Do not overwrite the verified Mono release or V11 ZIP artifacts.

## Read-only AOT review assignment

- Review CheckpointSnapshot.cs and the transaction/save DTO reflection and serialization paths.
- Determine possible IL2CPP AOT/stripping concerns, distinguishing confirmed issues from hypotheses.
- Give exact source paths and concrete real-Player checks for money, goods, receipts and checkpoint isolation.
- Own only Docs/V12_AOT_REVIEW.md, fewer than 120 lines and 120 characters per line.
- Do not change runtime/editor code, settings, saves or packages; do not launch Unity/Player or subagents.
- Self-check report line lengths and git diff whitespace; report once in at most fifteen lines.

## Acceptance

- Successful compilation/build is necessary but not proof of save compatibility or performance.
- Run the same saved city, same manual165 settings, same diagnostic after building.
- Preserve failed runs and runtime errors; do not lower NPC, navigation or receipt gates.
- Only consider switching release after actual Player evidence and a full tour on the candidate.
- If the toolchain is incompatible, preserve Mono and report the exact build limitation.
