# City repair contracts 11 October 2026

## Authoritative failed run

`work/city-expansion-20261008/ordinary-play-20261010T160928100Z`:
Player exit 2, 377 NavMesh point queries pass, route timeout `(206,0,-123)`,
94 observed NPCs, 84 moved, 0 animated, all 17 captures black.
The Player was launched with WindowStyle Hidden. No Unity or Player process remains alive.

## Capture implementation owner

Allowed files: new `Assets/_Game/Scripts/Runtime/QaRenderedFrame.cs` and meta,
`Assets/_Game/Scripts/Runtime/QaCapture.cs`, and `Docs/CITY_CAPTURE_REPAIR_20261011.md`.

Shared API, namespace Tycoon:\n+`internal static class QaRenderedFrame`
`internal static void Write(string path, int width = 0, int height = 0,
    System.Action<Camera, Canvas[], int, int> inspect = null)`

Extract the existing proven QaDriver.CapturePhysicalFrame URP render request and canvas restoration
into this module. QaDriver delegates to it, preserving its physicalResolutionCapture hook.
Main agent will update CityPlayMovement and CityPlayMotion call sites after the module is ready.
Do not launch Unity or Player while another worker might hold the project; main owns test/build.
No GPU readback async redesign, no gameplay or frame gate changes, no native UI operations.

## Navigation and animation review owner

Read-only source and failed artifact investigation. Only may create
`Docs/CITY_NAV_NPC_REPAIR_REVIEW_20261011.md`.
Find exact route length/deadline cause and whether NPC animation failure is explained by hidden
window culling or by a gameplay defect. Identify the narrowest truthful correction and tests.
Do not edit runtime/tests, launch Unity/Player, or spawn another agent.

## Integration criteria

Preserve current unrelated dirty files and save IDs. Source file size below about 300 lines.
Compile and full EditMode tests, current Player build, bounded correctness tour and nonblank
pixel validator. Model galleries still need current 8-direction and 5-angle evidence.
User cancelled stable 165 FPS benchmarking; no 165 success requirement or benchmark rerun.
