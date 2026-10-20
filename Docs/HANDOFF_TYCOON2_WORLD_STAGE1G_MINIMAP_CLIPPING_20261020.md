# Phase 1G — minimap clipping repair

Status: minimap spill repaired and verified at native 1080p/1440p.
Overall Phase 1 and the complete world redesign remain open.
Source base: main `11b74fbb28122da0cf99285f0a379470a866cad3`.

## Objective and checkout

Read `work/tycoon2-plan-20261020/objective-recovered.md` before continuing.
The original attachment and its eleven reference images remain missing; the
complete objective was recovered from this chat's successful original read.
The recovered file hash and provenance are in the Phase 1F handoff.
This checkpoint continues that objective without reducing any later phase.

Fetched origin before implementation; HEAD and origin/main matched the base.
Captured SHA256 hashes of all 101 pre-existing tracked dirty files in
`work/tycoon2-plan-20261020/phase1g/dirty-before.json`.
After tests, build and Player, all 101 hashes still matched. Preserve them and
the unrelated untracked assets/QA files. No reset, clean, stash or broad staging.

## Root cause and implementation

`CityMapGraphic` emits route polygons and POI vertices outside its local view.
Its axis-aligned terrain quads are bounded, but route polygons/icons are not.
`GameHud.BuildMap` previously had no ancestor clip for the local map graphic.
The Phase 1F Player capture showed route strips across the carry HUD and world.

The installed uGUI source confirms that `MaskUtilities.GetRectMaskForClippable`
ignores a RectMask2D on the same GameObject as the clippable. The repair adds
an actual parent viewport rather than a mask on the graphic itself:

```text
PlayingHud/Minimap               existing Image, Button and OpenMap callback
  MinimapViewport               RectMask2D, insets 8/30/8/8 reference pixels
    MinimapTerrain              CanvasRenderer then CityMapGraphic, full stretch
  MinimapHelp                   existing label, outside terrain clipping
  North                         existing label, outside terrain clipping
```

The first masked fixture exposed a MissingComponentException for CanvasRenderer.
The final code creates it explicitly before adding CityMapGraphic. This is
covered by the fresh test run and by actual Player clipping activation.

Root changed `GameHudMap.cs` and inserted one call to `MapClippingAcceptance`
in `QaWorldFeatures.cs`. Independent agents authored only the assigned test
and QA modules after root wrote the shared phase contract; root reviewed,
integrated, repaired the framework/renderer issues and executed all gates.

Route geometry/catalog IDs, world map hierarchy, save/economy and controls did
not change. No interactive map zoom/pan/rotation implementation is claimed.

## Fresh verification

Evidence root: `work/tycoon2-plan-20261020/phase1g/`.
Machine-readable summary: `acceptance.json` in that directory.

1. Initial compile stopped because this NUnit package has no
   `NonParallelizable` attribute. Removed it from the new isolated fixture.
2. `before.xml`: 3 tests, 1 passed and 2 failed, Unity exit 2. The two clipping
   tests failed for the absent viewport/mask; the existing click/close behavior
   passed. `before-tests.log` records this valid regression reproduction.
3. First masked test run exposed missing CanvasRenderer; `after.xml` and
   `after-tests.log` intentionally retain that intermediate failure.
4. Final `after-renderer.xml`: 31/31 passed, zero failed/skipped, Unity exit 0,
   `2026-10-20 06:01:57Z` to `06:01:58Z`.
   Suites: WorldRouteGeometryTests 14, CityMapCaptionLayoutTests 4,
   MinimapViewportTests 3, Stage7MapWayfindingTests 6,
   Stage9MapWayfindingTests 4. This is a focused run, not the complete suite.
5. Windows build: Succeeded at `2026-10-20T06:02:53.0413086Z`, 117118015 bytes,
   28.647 s, 0 errors and 34 warnings, Unity exit 0. Existing obsolete API and
   licensing messages prevent calling the entire editor log error-free.
6. Player: `passed=true`, exit 0, 3705 checks, 30 interactions, 63 PNG captures,
   0 runtime errors and zero exception/assertion/error scan matches.
   Started `06:03:17Z`, finished `06:03:49Z`; NVIDIA RTX 4060 Laptop GPU, D3D11.
7. New runtime checks verify active ancestor mask, CanvasRenderer clipping,
   terrain/corner alignment, hard clipping/insets, labels outside the terrain
   mask, native resolution and PNG dimensions. Clicking the minimap opens the
   paused full map; closing it restores player control and time scale.
8. Opened the before capture and both fresh captures:
   `minimap-clipped-1920.png` and `minimap-clipped-2560.png` in world-features.
   The visible spill in Phase 1F is absent at both native sizes. Frame, help,
   north label and carry HUD remain visible. This visual verdict covers the
   minimap repair, not the entire game's art or geometry.

Runtime DLL SHA256:
`743543C87AE36DC65836DB917BEC1EB6EE3EBD05110F47C5747CA4ECA14E2B92`.

## Reproduction commands

Editor executable:
`C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe`.
Launch hidden and keep test/build output under the isolated phase directory.

```text
-batchmode -nographics -projectPath D:\GAME\TYCOON2 -runTests
-testPlatform EditMode
-testFilter Tycoon.Tests.MinimapViewportTests|Tycoon.Tests.CityMapCaptionLayoutTests|Tycoon.Tests.Stage7MapWayfindingTests|Tycoon.Tests.Stage9MapWayfindingTests|Tycoon.Tests.WorldRouteGeometryTests
-testResults <phase1g>\after-renderer.xml -logFile <phase1g>\after-renderer.log
```

Do not add `-quit` to the Test Framework command. Build arguments:

```text
-batchmode -nographics -quit -buildTarget StandaloneWindows64
-projectPath D:\GAME\TYCOON2 -executeMethod Tycoon.Editor.ProjectBuilder.BuildWindows
--build-output <phase1g>\Player\TYCOON2.exe -logFile <phase1g>\build.log
```

Player command from repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_world_features.ps1 -BuildPath 'work\tycoon2-plan-20261020\phase1g\Player\TYCOON2.exe' -OutputPath 'work\tycoon2-plan-20261020\phase1g\world-features'
```

The dedicated executable and local evidence remain in this checkout; the
default delivery executable and pre-existing QA reports were preserved.
Large build/capture outputs are local ignored evidence; the acceptance summary
and contract are committed with the source and handoff.

## Resume order and acceptance limits

Continue Phase 1 before claiming Phase 2 completion. The next bounded repair
is street junction geometry: false junctions at collinear continuations,
sidewalks through interior crossings, continuous joins/corners, overlap removal
and rail/highway/dock visual boundaries. Read the Phase 1F handoff for earlier
validator changes and planar diagnostic limitations.

The minimap currently still draws from its existing route representation.
Clipping does not establish one shared sampled polyline for every map, mesh,
collider, NavMesh, pedestrian and traffic consumer. That original contract is
still required. Passing legacy route tests is not proof of Phase 2 implementation.

Interiors/cutaway, camera visibility, licensed environment assets, full interactive
map/HUD, save compatibility and short multi-district 1080p/1440p performance
acceptance remain in the original sequential plan. This short smoke does not
prove 60 FPS, Steam readiness, full regression or all old-save migrations.
