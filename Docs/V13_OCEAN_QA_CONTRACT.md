# V13 ocean QA review contract

## Current facts

- Workspace D:/GAME/TYCOON2, Unity6000.6.3f1, URP17.6, WindowsD3D11/RTX4060 Laptop.
- User requests better coastal water/models with low rendering cost and views from multiple directions.
- V12 archive smoke exposes empty camera background past the short ocean edge atx293.84.
- Desktop is locked; Computer Use inputs stopped and unlock question is pending.
- V12 full-tour24768 finished194505frames/880.977s,gameplayPASS;165cadence unverified.
- Root owns ocean geometry/shader/tests and compilation.

## Planned shared API

```csharp
public static class CityOceanHorizon
{
    public const string Tint = "#346E7B";
    public static void Build(Transform parent, Bounds bounds, float left);
}
```

- Decorative child renderer name: OceanVisualHorizon.
- Compact4x8quad grid:45vertices/64triangles; no new collider or NavMesh volume.
- Existing three ocean physical boundaries remain within WorldBounds+3.
- Horizon uses shared TownWater material matching harbor/marina tint.
- Root plans fragment fog evaluation and early fully-fogged output after checking local URP functions.

## Independent review and allowed output

- Goal: find minimal QA integration for15coast images and ABBA horizon render cost.
- Read QaDriver, QaItemVehicleAcceptance, QaModelAngles, QaWaterDetailV11, GameSession and launch scripts.
- May create/edit ONLY Docs/V13_OCEAN_QA_REVIEW.md; under300lines/120columns.
- Give exact functions and launch integration; recommend reusable capture/timing helpers.
- Desired report: passed,runtimeErrors,screenshots,renderCostRuns with off/on/on/off15s segments.
- Captures cover Harbor/Beach/Marina at front/right/rear/left/top.
- Gallery simulation frozen; compare whole-scene GPU timings across toggles.
- This QA does not establish gameplayFPS or165Hz presentation; record scope explicitly.
- Do not edit runtime/tests/other docs/settings; do not launch Unity/Player/compiler or expensive hashing.
- Do not spawn subagents. Report once<=15lines with file/checks/open questions.
- Self-check: file line/column limits and git diff --no-index --check against an empty baseline.

## File ownership for focused implementation

- Root owns CityOceanHorizon.cs/.meta, CityOceanEdge.cs and TownWater.shader.
- Ocean QA agent owns QaOceanV13.cs/.meta, QaDriver.cs dispatch only, Tools/run_player.ps1 task alias only.
- Runtime object to toggle is OceanVisualHorizon, exactly one MeshRenderer under CoastalHorizon.
- No shader or scene edits in QA agent scope. Do not launch Unity or Player processes from the agent.
- Main agent will run Unity tests, build, screenshots and frame timing after source integration.
- Use a distant Marina camera including the horizon for ABBA, instead of a close beach view hiding it.
- Add fog on/off wide-water screenshots outside measured intervals; 15 required landmark views stay unchanged.
- Keep all new source files under 300 lines and preserve existing unrelated dirty code.
