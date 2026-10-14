# V13 ocean continuity

## Observed issue

- V12 ZIP smoke scenery-Marina.png shows a large rectangular background beside water.
- CityOceanEdge currently stops at WorldBounds.max.x+3, about293.84m.
- Presentation cameras use farClip1600 and fog=false, exposing the unfinished ocean edge.
- Ocean base tint#387E86 also differs from harbor/marina#346E7B.
- Gameplay ocean blockers are three physics colliders and NavMesh modifier volumes.
- Navigation uses PhysicsColliders; a decorative mesh need not enlarge navigation boundaries.

## Verification in progress

- Three regression cases request water past the playable boundary in far coastal views.
- Another case checks coastal tint continuity; the fifth preserves the three physical boundaries.
- V12 full-tour PID13428/session24768 finished PASS; all194505frames retained over880.977seconds.
- Current Windows/Source/Evidence V12 ZIPs remain immutable; they contain the old ocean edge.
- UI/presentation cadence still requires the user to unlock Windows.
- Native red:4FAIL/1PASS; sea ended early and tint differed while physical blockers were already correct.
- Implemented one decorative45vertex/64triangle ocean horizon; no collider or navigation volume added.
- Matched harbor/marina tint; fog now evaluated per pixel and skips shading when fully fogged.
- First full run882/883 exposed tiny material color round trip precision; compare with1e-5 tolerance.
- Native green883/883PASS,0skip at `work/city-model-v13-20261010/full-tests-green.xml`.
- First Player gallery passed but visual review exposed lateral gray gaps outside city bounds.
- Four added regression cases reproduced those gaps:4FAIL/5PASS in ocean-lateral-red.xml.
- Added two narrow water continuations and three grass context planes outside the city footprint.
- Six context meshes total270vertices/384triangles; no new collider, volume, shadow caster or playable land.
- Final native887/887PASS,0skip at `work/city-model-v13-20261010/full-tests-final.xml`.
- Final IL2CPP Player gallery: `work/ocean-v13/player-20261010T062809251Z`,PASS,0runtimeerrors.
- Reviewed all15coastal angles plus two fog toggles; gray lateral gaps and old short ocean edge are gone.
- ABBA whole scene GPU means off1.431/on1.618/on1.628/off1.461ms; main horizon difference about0.177ms.
- Each window measured15seconds after2seconds settle with5831or more valid GPU samples.
- The measured change toggles the main horizon, not all context meshes, and is not gameplay FPS or scanout.
- Final gameplay restoration tour PASS:186748frames/935.489seconds,39workers,456interactions,0runtimeerrors.
- Engine199.626FPS,P956.914ms/P999.183ms; desktop remains locked, stable165 remains unaccepted.
- Delivery preparation selected971evidence files and2047source files; ZIP verification follows separately.
