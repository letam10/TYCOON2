# Phase 1F route geometry audit and repair contract

The full objective is preserved in ../objective-recovered.md. This tranche
addresses unfinished Phase 1 geometry before Phase 2 navigation changes.
The recovered text came verbatim from the first successful attachment read
in this chat session, timestamp 2026-10-20T02:44:57.995Z, ordinal 256.

## Shared foundations already on disk

- WorldRouteCatalog.BuildDefault(): 13 stable WorldRouteCorridor entries.
- WorldRouteCatalog.BuildStreetNetwork(): 22 street entries.
- BuildPublicApproaches(): industrial-sidewalk-approach,
  village-sidewalk-approach, marina-sidewalk-approach, south-service-sidewalk.
- WorldRouteSegment: Start, End, Center, Forward, Rotation, Length, Width.
- WorldRouteGeometry.Segments(route), FindJunctions(routes),
  TrimAtJunctions(segment, junctions, trimDistance),
  ValidateNoUnexpectedSelfIntersections(route, out failure).
- WorldRouteDiagnostics.FindCrossRouteOverlaps(routes, tolerance),
  ValidateCrossRouteOverlaps(routes, out failure, tolerance),
  ValidateRouteKindBoundaries(routes, out failure, tolerance).

## Ownership and invariants

Root alone owns all source/test/doc edits and any Unity execution.
Both audit agents are read-only and return findings in their final reply.
No servers, browsers, editors, builds, extra agents, messages or file edits.
Preserve stable IDs, node data, economy, SaveData and existing dirty files.
Existing Unity C# modules are the entrypoints. HTML/DOM selectors and dt
scaling are not applicable: this audit adds no animation/update loop.
Do not prescribe a new stack or manufacture passing evidence.

## Independent audits

1. Diagnostics audit: overlap categories, segment clearance, grade separation,
   open/closed self intersections, degenerate inputs and actual test coverage.
   Return concrete failing geometry examples and file/line references.
2. Generator audit: interior street crossings, sidewalk trimming, junction
   continuity, mesh/collider correspondence and bounded curve sampling.
   Return concrete failure examples and the smallest relevant correction.

## Gate

Reproduce a concrete defect before editing source, verify new regression
fails before the fix, and rerun the relevant existing suites after the fix.
Build/runtime evidence must be fresh for any changed rendered geometry.
Existing screenshots or source assertions alone do not prove visual quality.
