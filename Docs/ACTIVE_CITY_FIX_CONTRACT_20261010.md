# Active city Player fix contract

## Shared facts

- Unity 6000.6.3f1, URP, Windows Player, D3D11.
- Current ordinary tour reports a stuck walk at `(206, 0, -123)` and 17 black captures.
- Do not change the 165 FPS gate or run a benchmark. Existing Player evidence is correctness-only.
- Preserve unrelated dirty files and keep each edited source file below 300 lines where practical.

## Agent A: navigation and NPC evidence

- Allowed files: `Assets/_Game/Scripts/Runtime/CityPlayMovement.cs`,
  `Assets/_Game/Scripts/Runtime/CityPlayNavigation.cs`,
  `Assets/_Game/Scripts/Runtime/CityActorEvidence.cs`, and narrowly scoped tests/docs.
- Goal: make the ordinary tour handle the valid expanded-city edge route without masking a real
  collision, and make the NPC walking/leg-motion evidence reflect active animated agents.
- Do not edit capture code, build scripts, save/economy code, or unrelated art.
- Self-check: run the smallest relevant EditMode tests if available and report exact files,
  evidence, and any unresolved route/NPC condition.

## Agent B: visual capture evidence

- Allowed files: `Assets/_Game/Scripts/Runtime/CityPlayMovement.cs`,
  `Assets/_Game/Scripts/Runtime/CityPlayPresentation.cs`, `Assets/_Game/Scripts/Runtime/QaCapture.cs`,
  and narrowly scoped capture validator/docs. If file overlap with Agent A occurs, do not edit
  that file; report the conflict instead.
- Goal: make Player screenshot capture produce a non-black PNG in the current Windows Player while
  preserving UI-inclusive end-of-frame ordering and writing no source into build output.
- Do not alter gameplay, navigation, save, or FPS measurement semantics.
- Self-check: inspect the resulting PNG dimensions and sampled pixel diversity with the existing
  validator or a bounded local capture check; report exact files and open limitations.

## Main integration gate

- Main agent will resolve any overlap, run `Tools/run_unity.ps1 -Task Tests`, build the current
  Player, and run one bounded ordinary tour. Stable 165 FPS is not a completion criterion.
