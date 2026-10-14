# V14 requirement evidence audit

Scope: current V14 source and candidate at Unity 6000.6.3f1 / URP 17.6.
The live tour `work/city-expansion-20261008/ordinary-play-20261010T085522987Z`
was active during this audit (PID 35148). Its report had not been written yet.
No V14 acceptance item is called complete solely from code or an older package.

| Requirement | Existing evidence and exact scope | Status / remaining acceptance |
| --- | --- | --- |
| Fragile-only carry containers and exact occupancy | `Assets/_Game/Scripts/Runtime/CarryContainerProfile.cs` and `CarryContainerLayout.cs` implement six slots per tray. `Assets/_Game/Tests/Editor/CarryContainerTests.cs:15-109` covers egg, milk, bottled milk, sauce and soy sauce at 1, 2, 5, 6, 7, 13, 48; checks exact item/tray counts, upright orientation, clear bounds, supported layers, total occupancy and stored goods without trays. | Source + tests support the rule; V14 test XML is not a reliable completed count (see below). Need run/retain V14 test result after live tour. No current Player proof of carried/stored container presentation. |
| 27 carry models and stack layout | Frozen V13 evidence has 81 `carry-*.png` in each `evidence-final-20261008/function` and `.../visual` (27 SKUs × 3 counts, 1/24/48). `Assets/_Game/Tests/Editor/CityCarryTests.cs` checks each SKU at 48, non-overlapping meshes, 3×2 horizontal footprint and aligned support layers. `Docs/CITY_EXPANSION_20261008.md` documents layout. | Strong V13 static capture + source coverage; not a V14 gallery. Need generate/review current V14 27-SKU evidence at required counts and gameplay stack. |
| Eight isolated and five placed model views | No V14 gallery/index for the requested eight isolated and five placed views found under `work/city-model-v14-20261010`. | Missing. Create a V14 indexed set with source/build identity, views and nonblank image validation; inspect actual rendered views. |
| Industrial models | V13 carry sheet includes crate and goods, but no V14 industrial-model review/index found. | Missing as acceptance evidence. Confirm requested industrial set, then capture and review those models in V14. |
| Doubled city, districts, landmarks and transport | V13 `Docs/CITY_EXPANSION_20261008.md` records map dimensions 200×136 → 282.84×192.33 m, districts and transport. Frozen `evidence-final-20261008/fresh` includes overview, district and landmark/transit captures; its ordinary-play report lists 20 screenshots, 39 workers, 18 customers, 60.19 s, mean 162.836 FPS, P95 6.186 ms, P99 7.661 ms, worst 193.442 ms, and `stable165FrameBudget:false`. | Useful V13 static and short Player evidence only. Not proof for the V14 candidate or complete current city. Finish live V14 tour; verify every requested district/landmark/route in V14 captures and report. |
| Collisions and NPC behavior | V13 fresh mobility/NPC JSON and screenshots are indexed in `evidence-final-20261008/evidence-files.json`. `Docs/V14_NPC_DIAGNOSTIC.md` distinguishes passing long V13 animation evidence (81/87 animated) from the separate capped failure (0 animated) and requests visible controlled reproduction. | Not closed. V14 live tour must report collision recovery and visible NPC gait; do not waive the capped-run failure from older evidence. |
| Cash, save, UI, upgrades and settings | V13 function captures include `02-earned-cash.png`, `03-mod-two-options.png`, `04-upgrade-completion.png`; fresh includes `02-sale-cash.png`, `03-upgrade-feedback.png`. V13 city report records recovery, transaction IDs and saved map. Live V14 tour folder currently has `city-restore.json`, `settings.json`, save and journal, but no completion report yet. | V13 workflows are not V14 proof. Finish V14 Player checks for cash in hand/bank, persistence and transaction replay, HUD/UI, upgrades and settings; retain report and screenshots. |
| V14 targeted tests and candidate build | `Docs/V14_PERFORMANCE_FINDINGS.md` records 24/24 checkpoint tests (9 new cases) and a successful IL2CPP candidate build. `work/city-model-v14-20261010/Candidate/build-report.json` says Succeeded, 0 errors, 28 warnings, built 2026-10-10 08:13:12Z. `checkpoint-tests.xml` says Passed, but parsing its top-level testcase list yields zero cases; use the cited test summary, not an inferred count from this XML. | Build and targeted checkpoint scope only. Does not establish full V14 suite or Player acceptance. Capture a complete current test result after the tour. |
| Real Player performance / display | V14 short Normal run `work/city-expansion-20261008/ordinary-play-20261010T081545725Z`: 20 nonblank captures, 60.19 s, 162.836 average FPS, P95 6.186 ms, P99 7.661 ms, worst 193.442 ms; functional PASS, stable 165-frame budget false. V13 long run and stationary diagnostic are described in `Docs/V14_PERFORMANCE_FINDINGS.md`; hidden-window hardware measurement is explicitly rejected. | V14 short run is a real rendered Player sample, not sustained thermal acceptance. Need complete live run, retain full frame/hitch/timing logs, visible-window sustained thermal measurement and valid CPU sensor if available. 165 Hz display metadata does not prove scanout cadence; physical display acceptance remains unproven. |
| Matched delivery | V13 frozen packages and manifest: `work/city-expansion-20261008/Delivery-final-20261008/{Windows.zip,Source.zip,Evidence.zip,manifest.json}`. The historical `V13_DELIVERY_CONTRACT.md` is preserved in `Docs/History/LEGACY_DOCS_20261011.md`. | Explicitly stale for V14. Rebuild matched Windows/source/evidence packages from the accepted same revision, verify hashes/manifest and ensure evidence references the shipped candidate. Do not claim delivery complete. |

## Acceptance gates still open

1. Let the live V14 ordinary-play tour finish, then audit its final report, captures,
   timeline, hitches, Player log and checkpoint against the named candidate/build.
2. Run the complete current V14 Editor suite and preserve the machine-readable result;
   the 24 checkpoint tests are targeted coverage only.
3. Produce and visually review the missing V14 27-SKU, eight isolated/five placed,
   industrial model, city/landmark and transit evidence with an index and provenance.
4. Close visible NPC/collision, cash/save/UI/upgrades/settings workflows on the V14 Player.
5. Measure sustained visible gameplay and thermal behavior; report unavailable sensors
   as unavailable. Do not infer per-core boost or physical scanout from software counters.
6. Create and verify matched V14 Windows, source and evidence delivery artifacts.

Evidence boundary: V13 reports/captures/packages are explicitly labeled as frozen
historical evidence; tests/builds prove only their recorded scope; screenshots are
static visual evidence unless tied to a recorded live workflow; no source inspection
or gallery replaces in-game functional validation.
