# V11 pacing review contract

## Goal and ownership

Read code and existing Player evidence to distinguish software cap pacing from display synchronization.
May create only Docs/V11_PACING_FINDINGS.md. Runtime, tests, settings, saves and build outputs are read only.
Root owns model integration, Player execution and validation. Another agent owns cargo boarding.
Do not run Unity or Player, spawn children, edit other files, publish or change Windows or NVIDIA settings.

## Existing interfaces

- GameSettingsData.syncDisplay defaults true; fpsLimit defaults165.
- GameSettingsStore.Apply sets vSyncCount1/targetFrameRate minus1 when syncDisplay is true.
- Otherwise vSyncCount0/targetFrameRate fpsLimit.
- Tools/run_city_play.ps1 Manual165 writes private QA settings with syncDisplay false.
- ShowFps without Manual165 writes private QA settings with syncDisplay true.
- Current display refresh reported165Hz; Windows Player1080p/D3D11/RTX4060 Laptop.
- Simulation remains1x,39workers and24customers for loaded maximum diagnostics.
- ActorView already uses CullUpdateTransforms; do not recommend adding it as if missing.

## Evidence and required output

- Diagnostic work/city-expansion-20261008/ordinary-play-20261010T015709724Z passed377points.
- Report9646frames/62.294574s,154.844946FPS,P956.894806ms/P999.054301ms.
- Native marker mean MainThread6.22ms and WaitForTargetFPS0.71ms; GPU2.073725ms.
- Marker scopes may overlap. Do not add overlapping markers or exclude frames to inflateFPS.
- Check source usage, official Unity documentation and remaining CPU bottlenecks.
- Recommend a minimal matched comparison using existing switches and a concrete next edit only if needed.
- Clearly separate measured facts, inference and evidence still required for stable165FPS.
- Self check git diff --no-index --check; report ONCE at most15lines.
