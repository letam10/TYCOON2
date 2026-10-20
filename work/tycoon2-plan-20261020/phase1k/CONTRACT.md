# Phase 1K — bounded circular route corners and continuous street seams

Base main 1872c1cd61b90eb11ddc1c11e810569a42e969fb. Read ../objective-recovered.md.
Full objective and later phases remain OPEN. Preserve all 101 original tracked
dirty files and unrelated untracked assets. No reset, clean, stash or broad stage.

## Root foundation and fixed interfaces

Root writes this contract, WorldRouteCorner.cs and complete catalog integration
before spawning agents. Unity entry remains WorldFactory, no HTML/DOM/JS/dt change.
WorldRouteCorner readonly struct has AnchorIndex, Anchor, Entry, Exit, Center,
Radius, SweepRadians; constructor(int,Vector3,Vector3,Vector3,Vector3,float,float).
Evaluate(float fraction) returns exact Entry/Exit at 0/1, otherwise XZ circle.

New WorldRouteRounding static class fixed API:
- public const float MaximumChordError = .02f (metres).
- public static IReadOnlyList<WorldRouteCorner> Corners(IReadOnlyList<Vector3> anchors, float preferredRadius).
- public static IReadOnlyList<Vector3> Sample(IReadOnlyList<Vector3> anchors, float preferredRadius).
- public static IReadOnlyList<Vector3> Section(IReadOnlyList<Vector3> closedAnchors, float preferredRadius, int fromCorner, int toCorner).

Finite horizontal routes use circular fillets. Unit incoming/outgoing vectors
give signed turn theta from dot/cross. Tangent trim t=min(preferredRadius*tan(abs(theta)/2), incomingLength/2, outgoingLength/2).
Actual R=t/tan(abs(theta)/2). Thus adjacent trims cannot overlap. Preferred radius
for catalog paths is exactly Width. Center is Entry plus signed left normal*R.
Arc lies inside the local anchor triangle; preserve raw Nodes independently.
Use double math where helpful. Sample segments must meet R*(1-cos(abs(theta)/(2*N))) <= .02m.
Use even N>=2 so every arc midpoint is an exact canonical sample. Never replace
the arc with a quadratic approximation or arbitrary multiply. No zero edges.
Open paths keep exact endpoints. Closed paths round the closing corner too and
repeat their first actual sample exactly. Collinear or reversal corners have no
fillet; invalid/duplicate authored input must still be visible to raw validators.
Return immutable copies. Nonhorizontal routes retain the existing bounded
WorldRouteGeometry.CreateSmoothPath(..., RouteSubdivisionsPerSpan) behavior; no
grade/ramp policy change in this tranche. Two-node corridor/approach constructors
retain the original same-list representation unless catalog supplies a section.

Section returns the exact forward cyclic Sample subsequence from fromCorner's
arc midpoint to the next corner's arc midpoint. Only adjacent unique indices
are valid. Reject invalid/open input; do not resample/reapproximate endpoints.

Root catalog keeps all original Id/Nodes/Width/Kind. Internal six-argument
WorldRouteCorridor constructor accepts immutable canonical samples; public five
argument API remains. highway-ring supplies the shared closed samples. Four
street ring IDs use Sections of that same raw loop, same width:
- street-ring-west: 0->1.
- street-ring-north: 1->2.
- street-ring-east: reverse(2->3), matching authored direction.
- street-ring-south: reverse(3->0), matching authored direction.
SampledNodes geometry and derived signal poses change; no entity/POI/economy/save ID changes.

## Independent assignments

1. round_sampler owns only new Runtime/WorldRouteRounding.cs. Implement all three
fixed functions and constants. Read root corner type and catalog integration.
No other file edits, Unity/tests/build/Player/browser/server/Git mutations or
extra agents/messages. Return promptly; root owns all compilation/runtime.

2. round_surfaces owns only Runtime/WorldStreetGeometry.cs and WorldRouteGeometry.cs.
Maintain public APIs. Pavement and sidewalks must join both within sampled
curves and at coincident endpoints across different street IDs. Add bounded
join candidates and clip against roads/previous sidewalks; don't extend miter
spikes or create overlap, and don't drop sidewalk corner areas. Exact height
separation, route/segment/side/piece metadata, 1mm2 sliver rejection and quantized
convex output remain. FindJunctions should intersect sampled edges, skip same-
route adjacent edges including loop closure, aggregate logical bend markers
using actual rounded midpoint where possible, and preserve grade separation.
Synthetic 10m+10m perpendicular two-route L, road width4/sidewalk2 has pavement
78m2 and sidewalk74m2 with bounded bevel envelope. No runtime or other edits.

3. round_tests owns new EditMode/WorldRouteRoundingTests.cs and existing
WorldRouteSamplingTests.cs, WorldStreetSurfaceTests.cs, WorldRouteGeometryTests.cs.
Independently verify circle radius/center/tangents, half-edge trim, even midpoint,
chord error, local convex-hull containment, left/right, closed closure, short and
collinear/reversal/graded inputs, immutable copies, exact section recombination
and stable ring IDs/anchors. Update superseded planar Hermite/count assumptions
without weakening nonplanar legacy, reservation, finite/nozero, graph, collider,
overlap or area checks. Add numeric global L join and curved sidewalk seam tests.
Do not use expected=same implementation twice or source strings as proof.
No Unity/test execution or other edits. Return promptly when source is ready.

## Root integration and gates

Root reviews all changes and owns catalog, QA rounded-corner manifest/captures,
live sampled-trigger and polygon-collider checks, any navigation adaptation,
failure diagnosis/repairs, affected tests, isolated build and short world-features
Player, fresh image inspection, handoff/index/acceptance and exact allowlist
commit/nonforce push. No benchmark/soak. Preserve original gameplay camera yaw/pitch.
No full Phase 1/2, old-save matrix, 60FPS or Steam acceptance claim from this.

## Root repairs confirmed by affected EditMode gates

The first run passed 130/133. Industrial POI endpoint classification, authored
ring/industrial junction neighborhoods and the northeast signal shoulder needed
adaptation to actual rounded geometry. Public approach classification now uses
its sampled width and matching ground height. Authored shared-anchor junction
neighborhoods require both sampled surfaces within their summed half-widths;
remote collinear overlap remains rejected. Signal IDs/cycles stay fixed while
poses project to the sampled ring with 6.2m shoulder distance. A normal-component
correction handles a nearest-edge change at a concave sampled corner. Pedestrian
failure diagnostics include the failing coordinate and total waypoint count.
Added three independent regression tests for these repairs. Final affected run
after-shoulder.xml passed 136/136, 0 failed/skipped, Unity exit0, at
2026-10-20 08:27:57Z through 08:28:01Z. Native/build gates remain separate.
