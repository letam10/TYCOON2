# Dining seating v9 — 2026-10-09

## Ownership and metadata

Geometry, table styles, counter seating, terrace and furniture tests are owned by root.
Seating uses `DiningFurniture.Seats[0]`: the marker is the hip center above the cushion,
with forward facing the table. No additional cushion or hip height is added.
`TableStation.Seat` remains the reachable approach point; all metadata moves with its table.

## Diner behavior

- Arrival still advances the existing order from phase 0 to phase 1 through the same transaction.
- `DinerSeatingPose` suspends NavMesh and the actor capsule while moving onto the chair.
- The actual Humanoid hips bone, or the current Generic rig's `Hips` transform, is aligned
  to the seat marker with a 0.45 second smooth position/rotation transition.
- Waiting and eating retain `ActorView.State = Sit`; walking updates do not interrupt the pose.
- Leaving blends back to the approach, restores NavMesh/capsule and resumes the existing exit route.
  Terminal feedback can pause that route until its original duration expires.
- A seated save records the approach position. Loading phases 1/2 immediately restores the chair pose.
- Pool disable cancels pending callbacks, restores animation culling and walking components;
  the next spawn places the actor on sampled NavMesh before `Begin` restores the saved phase.
- Table IDs, receipts, order count, deadlines, inventory and payment operations are unchanged.

## Verification

- Roslyn compiled the current runtime against the installed Unity 6000.6.3f1 references.
  Only existing obsolescence warnings outside the seating files were reported.
- Added 12 EditMode test cases in `DinerSeatingTests`: both actual diner prefabs, stable animation,
  moving/rotated anchors, model scale and offset, NavMesh/capsule suspension and restoration,
  pool cancellation, and save/pool round trips for all five diner phases.
- Test source compilation and whitespace/line checks are performed separately from Unity execution.
- Follow-up: the first Unity run passed all five pose-only cases, while seven native-navigation cases
  failed before seating at agent attachment. The test class now uses the existing
  `CityPedestrianPlayModeFixture`: native agents register in PlayMode. It bakes a real collider floor
  with `NavMeshSurface`, asserts a sampled approach and agent registration, then preserves all warp,
  capsule and lifecycle assertions. Root must rerun these cases to confirm the correction.
- Root must run the tests and actual Player checks: sitting/body contact from several angles,
  reachable approach paths for all six tables, servicing with carried goods, departure feedback,
  and saving/loading seated customers. Source compilation is not visual or gameplay acceptance.
