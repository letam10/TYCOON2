# Phase 1G minimap clipping contract

Base: main 11b74fbb28122da0cf99285f0a379470a866cad3.
Read objective-recovered.md and Phase 1F handoff. Full redesign remains open.
Existing Unity/uGUI stack and shared game contracts remain unchanged.

## Fixed hierarchy and ownership

- Root edits Assets/_Game/Scripts/Runtime/GameHudMap.cs only for the fix.
- PlayingHud/Minimap keeps its Image, Button and OpenMap callback.
- Minimap/MinimapViewport is a RectTransform with RectMask2D.
- Viewport anchors are zero to one; offsets (8,30) and (-8,-8).
- MinimapViewport/MinimapTerrain is the existing CityMapGraphic, Local true,
  stretches zero to one with zero offsets, remains non-raycastable.
- Create CanvasRenderer explicitly on MinimapTerrain before CityMapGraphic;
  the first masked fixture exposed a missing renderer during clipping teardown.
- MinimapHelp and North remain direct children of Minimap outside the mask.
- No world map hierarchy, route geometry, stable IDs, economy, saves or inputs change.
- Existing game clock owns simulation speed; no new dt or speed multiplication.
- HTML entry point and DOM selectors are not applicable to this Unity project.

## Independent files

1. map_clip_tests owns only Assets/_Game/Tests/Editor/MinimapViewportTests.cs.
   Implement NUnit EditMode regressions against real GameHud.BuildMap via a
   minimal inactive fixture/reflection, not source string tests. Check ancestor
   RectMask2D selection, exact insets/fill and labels outside clipping. Keep
   minimap click callback intact. Use installed uGUI MaskUtilities as authority.
2. map_clip_runtime owns only Assets/_Game/Scripts/Runtime/QaMapClipping.cs.
   Add partial QaDriver IEnumerator MapClippingAcceptance(). Root invokes it
   after FeatureUiAcceptance in WorldFeaturesAcceptance. At 1920x1080 and
   2560x1440 validate actual viewport/mask, terrain bounds, unclipped labels,
   click Minimap with FeatureClick and verify map/control restoration, capture
   minimap-clipped-1920.png and minimap-clipped-2560.png using Capture. Restore
   original resolution/settings. Use existing SettingsObject, Check, FeatureClick.
3. Root integrates caller, runs fresh failing/passing tests, builds Player,
   runs bounded smoke, opens captures, records handoff, commits/pushes allowlist.

## Agent limits and evidence

Agents read relevant dependencies, edit only their assigned file, no process
launches, Unity/browser/server, no nested agents, no messaging/followups. C# has
no meaningful node --check; root compiles with Unity. Return concrete changes
and limitations. Root waits at least 120000 ms and integrates directly.

No acceptance claim from structure alone: inspect before/after Player captures.
No whole Phase 1, FPS, all-interior or full save acceptance from this tranche.
