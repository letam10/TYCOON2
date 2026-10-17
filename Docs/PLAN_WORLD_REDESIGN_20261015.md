# TYCOON2 world redesign plan — 2026-10-15

status: STAGE 5 FAIL (Player OceanV13 timing gate); Stage 6 blocked
workspace: D:\GAME\TYCOON2
captured_at: 2026-10-15T16:30:48.020+07:00
scope: Không sửa gameplay, scene, model hoặc asset runtime trong Stage 0.

## Stage 0 — baseline freeze

### Baseline source and runtime

- source commit: 21256bc0f9100bc4221141950b62159e14376fff
  (Finalize TYCOON2 handoff and runtime QA), branch main.
- remote: https://github.com/letam10/TYCOON2.git.
- Unity: 6000.6.3f1 (45d8eee7de74); URP 17.6.0; AI Navigation 2.0.12.
- target: Windows Player, Direct3D11, single-player offline, tiếng Việt.
- current build artifact: D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe,
  667,136 bytes, last write 2026-10-15T13:46:52.8096908+07:00.
- no benchmark, soak test, long campaign or publish/push is part of this stage.

### Commands and results captured before editing

| command | exit | result/artifact |
| --- | ---: | --- |
| Get-Content AGENTS.md | 0 | Rules read; scope, one-file writes, no reset/clean and fresh evidence recorded. |
| Get-Content Docs/CODEX_HANDOFF.md | 0 | V21 handoff read; current build and known acceptance limits recorded. |
| Get-Content Docs/REPAIR_PLAN_20261014.md | 0 | Route, cutaway, interaction, expansion and follow-up gaps read. |
| Test-Path Docs/PLAN_WORLD_REDESIGN_20261015.md | 0 | Missing before Stage 0; this file is the only Stage 0 artifact. |
| git status --short --branch | 0 | ## main...origin/main; 1,161 entries before this plan. |
| git rev-parse HEAD; git branch --show-current; git remote -v | 0 | Commit, branch and remote above. |
| ProjectVersion.txt and package query | 0 | Unity/URP/AI Navigation versions above. |
| build artifact probe | 0 | Build path, size and timestamp above. |

### Dirty checkout classification (before this plan was created)

1. Redesign changes to keep: no uncommitted runtime/source redesign file was
   detected. The redesign source already exists at the baseline commit above. The
   current route, interaction, cutaway, expansion, label-audit and save code remains
   the baseline and is not reset.
2. Preserve but do not stage yet: four untracked Unity metadata files
   (Assets/Resources.meta and three test .meta files), plus
   Docs/Art/vehicle-reference-v8.png and mod/test/overnight-campaign.json. They are
   preserved byte-for-byte pending ownership/scope review.
3. Historical/downloaded/QA files not staged: 12 modified tracked QA evidence
   files, 15 untracked QA evidence/report/note files, and 1,128 untracked paths under
   ASSET/Downloaded/. Existing work/ and build/history output stays in place and is
   not cleaned. These paths are not an allowlist for source commits.

### Stable IDs and save contracts

- CityDistricts.Revision = 2; keep area IDs farm, livestock, farm_shop,
  processing, supermarket (market alias), bakery, and restaurant.
- Keep CityDistricts.Offset/Center/Footprint, WorldBounds, PlayerSpawn,
  gate and customer spawn semantics.
- Keep the 14 map IDs in CityMapLocations.Points:
  tycoon, park, station, beach, harbor, marina, bank, restaurant, forest,
  forest_reserve, industrial_yard, north_village, east_marina, south_highway.
- Keep runtime entity/station IDs such as field_carrot, checkout_farm,
  storage_farm, storage_farm_shop, machine_mill, safe_starter, worker IDs,
  receipt IDs and route/POI IDs.
- SaveData.version accepts v2/v3; SaveStore.CurrentTransactionVersion = 3.
  TransactionState.schemaVersion = 2 and layoutRevision must continue to match
  CityDistricts.Revision.
- Preserve transaction journal receipts/outbox, item stacks, owners, reservations,
  orders, payments, purchases, crews, stations, crates, trucks and transport jobs.
  Migration/validation owners remain in CitySaveMigration,
  GameSessionSaveCompatibility, GameSessionPersistence and
  RuntimeTransactionMigration. No layout change may silently rename an owner.

### Existing systems and boundaries

- World build/layout: WorldFactory.Build(GameSession), CityDistricts,
  CityOuterDistricts, CityStaticGeometry, WorldExpansionCatalog,
  WorldExpansionDressing, CityLandmark.
- Routes/placement: WorldRouteCorridor and WorldRouteCatalog.BuildDefault()
  expose route ID, RouteKind, nodes, width and placement reservation. Current IDs are
  farm-dirt-path, farm-service-loop, sidewalk-crossings, farm-sidewalk-connector,
  highway-ring, rail-mainline, dock-harbor-approach, forest-access,
  expansion-forest-access, expansion-industrial-approach, expansion-village-road,
  expansion-marina-dock, expansion-south-highway (13 total). CityAccessPaths renders
  these corridors/signs and WorldRouteCatalog.IsReserved rejects prop placement.
  The older repair note saying 11 routes is stale; source is authoritative.
- NPC/traffic/transit: CityPedestrianNetwork, CityPedestrianRoute, CityTraffic,
  CityTrafficSignalSystem, CityRailNetwork, CityRailService,
  CityPublicTransport, ICityPassengerCarrier.
- Interactions/boarding: WorldInteractionRegistry.Build,
  WorldInteractionArea, WorldPlayerRide.TryBoard, and the existing
  passenger-carrier door/hull/boarding contract.
- Cutaway: WorldCutawayDirector.Build discovers landmark/POI volumes;
  WorldCutawayVolume.RegisterRenderer and SetPlayerInside hide upper shell
  renderers/objects and restore their initial state.
- Water/coast: Unity-only CityCoastWaterMesh, CityOceanEdge,
  CityOceanHorizon, TownSurfaceMaterials. The Three.js prototype is not an
  implementation path.
- Vegetation/art QA: current CityVegetation.Tree is deterministic primitive
  dressing. WorldModelAlignmentAudit, WorldLabelAnchor and
  WorldAssetBudgetAudit are the audit boundary.
- Map UI: CityMapLocations, CityMapGraphic, GameHudMap and MapPlayerIndicator
  consume stable point IDs/labels/positions.
- Verification tools: Tools/run_unity.ps1, Tools/run_player.ps1,
  Tools/run_world_features.ps1, focused EditMode tests and bounded Player evidence.
  A build/test pass is not visual, FPS, persistence or native-boarding proof.

### Planned architecture interfaces

- World builder owns scene object creation and calls route, POI, interaction,
  cutaway, water and vegetation builders in a stable order. It must not mutate
  save IDs or transaction state.
- Route catalog is the single source for walk/road/rail/dock nodes and widths.
  Renderers, placement reservations, pedestrian planning and traffic consume the
  same immutable corridor IDs.
- POI/interaction data exposes stable ID, label, footprint, approach anchor and
  action. The registry maps actions to proximity/boarding contracts; vehicle
  fallback never reparents the player into a landmark shell.
- Cutaway receives a landmark footprint and an explicit shell renderer/object list;
  enter/exit must be idempotent and restore the cached enabled/active state.
- Water is a Unity mesh/material contract with finite bounds, edge transition,
  depth cue and repeatable wave parameters. No Three.js runtime dependency.
- Vegetation output must be deterministic from a recorded seed and LOD profile.
  Generated files are referenced only through an allowlist and the asset budget
  audit; route reservation remains authoritative for placement.
- Map UI consumes CityMapLocations.Points without changing IDs. Labels/signs derive
  placement from bounds/anchors and must pass footprint/facing audits.
- Save/migration remains a consumer of stable IDs only. Any new world entity gets
  a migration/default rule before runtime use; no offline progression changes.

### ez-tree dependency policy

- No ez-tree reference, pinned commit or license was found in tracked files.
  Stage 0 does not fetch, import or stage it.
- Before Stage 6, record the exact upstream repository URL, immutable commit SHA,
  license text and attribution. Only generated output actually used by the map,
  the preset, seed and attribution/license file may enter the commit allowlist.
- Existing KayKit/Kenney license files are CC0 with attribution guidance; they do
  not substitute for ez-tree's own license record.

### Baseline open issues and assumptions

- Existing handoff evidence is historical/bounded. It does not replace a fresh
  Player review of pivots, labels, water edge, roof cutaway and physical boarding.
- Pedestrian route diagnostics/crossing waits and complete boat/car/train boarding
  still require explicit acceptance; direct fallback behavior remains a follow-up.
- Expansion dressing currently favors lightweight primitives; no ez-tree output is
  approved yet. Water depth/3D wave readability and coast blending need a fresh
  visual gate.
- No stable-165 FPS, soak, long campaign or all-resolution acceptance is implied.
- Assumption: redesign remains 3D Unity, preserves all stable IDs/save contracts,
  and uses transformed bounds/real camera evidence for art decisions.

### Stage 0 gate and next stage

- Pass criteria: this plan exists, every Stage 0-9 row has a status, no runtime
  file was changed, no dirty file was reset/cleaned/deleted, and the pre-existing
  dirty classification is recorded.
- Result: baseline plan was created and Stage 1 has now passed with fresh evidence.
- Next stage: Stage 3 is complete; proceed to Stage 4 cutaway/boarding/traffic.

## Stage gates and execution records (Stages 1-9)

Each row is updated in this file after its stage. “TBD” is intentional until that
stage runs; no row is acceptance evidence before its commands and artifacts exist.

| stage | status | source commit | changed files | commands/results | visual evidence | assumptions/open issues | next stage |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 model alignment/art pipeline | PASS | 21256bc | no runtime source; fresh work/stage1-art evidence | tests 8/8, build 0, Player 2114 checks/664 captures, pixel audit 0 errors | Contact sheets viewed: 5 isolated pages + 4 placed pages; no blank/magenta | No transform fix was justified; procedural models and bounds/normals passed; Blender not required | 2 |
| 2 routes/navigation/placement | PASS | 21256bc + test assertion | WorldRouteCorridorTests.cs; fresh work/stage2-routes evidence | route 6/6, pedestrian 6/6, expansion 3/3, world-features 0 | Expansion route contact sheet viewed; roads/docks/highway nonblank | Runtime route catalog unchanged; long traffic/soak remains open | 3 |
| 3 world layout/POI/interactions | PASS | 21256bc + route test | no runtime layout change; fresh stage2-routes destination evidence | interaction 3/3, expansion access 1/1, expansion contract 3/3, world-features 0 | 14-point map travel and expansion contact sheet viewed; nonblank | Stable IDs/approach anchors unchanged; long campaign remains open | 4 |
| 4 cutaway/boarding/traffic | PLANNED | TBD | TBD | Cutaway state, carrier, signal and collision tests; fresh Player boarding run | Roof/upper-shell reveal and board/alight captures | Carrier contract controls parenting; no clipping or unsafe fallback | 5 |
| 5 water/coast and vegetation/materials | FAIL | 21256bc0f9100bc4221141950b62159e14376fff + uncommitted Stage 5 changes | CityCoastWaterMesh.cs; TownSurfaceMaterials.cs; TownWater.shader; CityLandscape.cs; CityOuterDistricts.cs; WorldVegetationCatalog.cs; Stage5WaterVegetationTests.cs; sea prototype; ez-tree preset/output/license | prototype syntax 0; generated asset audit PASS 6/6 unique; focused tests 3/3; Build Succeeded 0 errors/28 warnings; OceanV13 Player exit 2 because every ABBA window had gpuTimingSamples=0 | Fresh Beach front and Marina wide fog on captures viewed; water/shore/horizon rendered and no blank/magenta frame in viewed images, but visual acceptance is withheld because Player timing gate failed | Water and vegetation source/profiles are present, but Player acceptance and GPU timing remain open; no Stage 6 work may start until this failure is repaired; preserve collider/NavMesh boundary | repair Stage 5 |
| 6 vegetation/ez-tree | PLANNED | TBD | TBD | Pinned dependency/license audit, deterministic seed/LOD and asset budget tests | Fresh forest/farm/road vegetation views at required distances | Allowlist only generated output/preset/seed/attribution; no raw download bulk | 7 |
| 7 map UI/wayfinding | PLANNED | TBD | TBD | Map label/footprint/icon/sign tests at supported sizes | Fresh map screenshots with readable labels, signs and signals | Derive label anchors from bounds; no caption overlap/cutoff | 8 |
| 8 SaveV2/migration | PLANNED | TBD | TBD | SaveV2/schema/layout migration and repeated-load tests; exit 0 | Fresh load/relaunch evidence with stable IDs and no loss | No layout ID rename; invalid saves remain byte-safe and blocked | 9 |
| 9 integrated acceptance/package | PLANNED | TBD | TBD | Full required tests/build/world feature gate and bounded Player review; all exit 0 | Fresh final gallery/report with timestamp/hash | No benchmark/soak; stage fails on any prior gate failure | release review |

## Stage 0 execution record

- status: PASS; source commit 21256bc0f9100bc4221141950b62159e14376fff.
- changed files: plan only; no runtime source in Stage 0.
- commands/results: required reads, status probes and plan checks exited 0.
- visual evidence: none generated; historical evidence retained.
- assumptions/open issues: stable IDs/SaveV2 and 3D Unity scope fixed.
- next stage: Stage 4 after Stages 1-3 pass.

## Stage 1 execution record

- status: PASS; source commit 21256bc0f9100bc4221141950b62159e14376fff.
- changed files: no runtime/source; work/stage1-art build, Player report, 664 PNGs
  and 9 contact-sheet JPGs.
- results: alignment 8/8; build Succeeded/0 errors; city-models-v11 PASS with
  2,114 checks, 664 screenshots, 0 runtime errors; pixel audit 664/664 valid
  1920x1080 with black/white/magenta=0.
- visual evidence: isolated pages 01/05 and placed page 01 viewed, nonblank.
- assumptions/open issues: procedural models needed no Blender repair; native all-camera
  review remains outside this bounded gate.
- next stage: Stage 2.

## Stage 2 execution record

- status: PASS; source commit 21256bc plus WorldRouteCorridorTests assertion
  updated from stale 12 to current 13 routes.
- changed files: Assets/_Game/Tests/EditMode/WorldRouteCorridorTests.cs only;
  evidence under work/stage2-routes/world-features-20261015T113000Z.
- results: route 6/6, pedestrian 6/6, expansion 3/3; world-features PASS with
  3,390 checks, 61 screenshots and 0 runtime errors.
- visual evidence: expansion route contact sheet viewed; forest, industry,
  village, marina dock and south highway captures were nonblank.
- assumptions/open issues: runtime route catalog unchanged; long traffic/soak open.
- next stage: Stage 3.

## Stage 3 execution record

- status: PASS; source commit 21256bc plus the Stage 2 test assertion.
- changed files: no runtime layout/POI/interaction source; fresh evidence reused from
  work/stage2-routes/world-features-20261015T113000Z.
- results: WorldInteractionAreaTests 3/3, WorldExpansionRouteAccessTests 1/1 and
  WorldExpansionContractTests 3/3 exited 0. Destination smoke exited 0 with
  WORLD-FEATURES_QA PASS, 3,390 checks, 61 screenshots and 0 runtime errors.
- visual evidence: map travel covered all 14 stable points; expansion contact sheet
  was viewed and showed nonblank farm/forest/industrial/village/marina/highway views.
- assumptions/open issues: approach anchors remain finite and stable; full manual
  interaction acceptance for every bank/restaurant/forest/beach/port action is later.
- next stage: Stage 4 cutaway/boarding/traffic.

## Stage 5 execution record — FAILED

- status: FAIL; source commit `21256bc0f9100bc4221141950b62159e14376fff`, branch `main`.
- changed files: Unity water mesh/material/shader and deterministic vegetation catalog;
  Stage 5 focused tests; `work/sea-prototype/index.html`; generated GLB/PNG/preset;
  `Docs/ThirdParty/EZ_TREE_LICENSE.md`. Existing dirty QA/downloaded files were not reset.
- commands/results: prototype `node --check` exit 0, 7,492 bytes; ez-tree upstream
  `dgreenheck/ez-tree` pinned to `dcf309bd86bd521083d9c70f01f2de45fdc7c457` MIT;
  generated audit exit 0 with 6 records and 6 unique hashes; `Tools/run_unity.ps1
  -Task Tests -TestFilter Tycoon.Tests.Stage5WaterVegetationTests` exit 0, 3/3 pass,
  0 fail/skip; `Tools/run_unity.ps1 -Task Build -BuildPath
  work/stage5-water/TYCOON2.exe` exit 0, report `Succeeded`, 0 errors and 28 warnings,
  artifact SHA256 `96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873`;
  `Tools/run_player.ps1 -Task OceanV13 -BuildPath work/stage5-water/TYCOON2.exe`
  exit 2.
- failure evidence: `work/ocean-v13/player-20261015T143101103Z/ocean-v13-report.json`
  has `passed=false`, `runtimeErrors=[]`, all four 15-second ABBA runs report
  `gpuTimingSamples=0`, and the failure is `V13 valid CPU and GPU timing samples in every ABBA window`.
  Fresh captures are in that directory; Beach front and Marina wide fog on were viewed.
- assumptions/open issues: the failure is a Player GPU timing evidence problem on the
  current D3D11 path, not proof that water geometry or vegetation is visually accepted.
  The 28 build warnings remain baseline warnings. A repaired timing path or an approved
  GPU timing backend is required before Stage 5 can pass.
- next stage: stop here and repair/re-run Stage 5. User-requested Stage 6 interaction
  content is blocked and has no acceptance evidence.



## Stage 5 repair execution record 2026-10-15 — FAILED and blocked

- status: FAIL; source commit 21256bc0f9100bc4221141950b62159e14376fff; branch main.
- changed files in this repair: Assets/_Game/Scripts/Runtime/QaOceanV13.cs; agent-created audit files Assets/_Game/Tests/EditMode/Stage6VegetationEzTreeTests.cs and Assets/_Game/Tests/Editor/Stage7MapWayfindingTests.cs; this plan append. Existing dirty water, vegetation, QA and downloaded files were preserved.
- commands and results:
  - powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.Stage5WaterVegetationTests: exit 0, 3/3 pass, 0 fail/skip after the profiler fallback, 60 Hz measurement cap and WaitForEndOfFrame changes.
  - powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Build -BuildPath work/stage5-water-repaired-20261015T2245/TYCOON2.exe: exit 0, build-report Succeeded, 117022655 bytes, 0 errors, 28 warnings. Executable SHA256 96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873. Runtime DLL SHA256 65A33760E5E5D0ECB463459BE7DBADB438185BDDAF75612361C810963BF8F55B.
  - powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_player.ps1 -Task OceanV13 -BuildPath work/stage5-water-repaired-20261015T2245/TYCOON2.exe: exit 2 at 2026-10-15T15:44:30.8163448Z. Report work/ocean-v13/player-20261015T154309358Z/ocean-v13-report.json has passed false, runtimeErrors empty, four complete 15 second ABBA windows, CPU samples positive and GPU samples zero in every window. Backend is ProfilerRecorder with counter Render GPU Frame Time and gpuMs is -1 in all four windows.
  - D3D12 diagnostic on the same build was stopped after fresh log evidence showed repeated Blitter is already initialized errors and invalid 36 KB captures. It is not an accepted backend for this build.
- visual evidence: new Beach front capture work/ocean-v13/player-20261015T154309358Z/ocean-v13-Beach-front.png was opened at 1920x1080. It is nonblank and shows water, shore and HUD without magenta. This does not pass the withheld GPU timing gate or prove all Player interactions.
- assumptions and open issues: D3D11 FrameTimingManager and the real ProfilerRecorder GPU counter expose no positive samples on this Windows path even after WaitForEndOfFrame and a local vSync target attempt. No GPU value was fabricated and CPU time was not relabeled as GPU time. An approved GPU timing backend or a documented hardware and API change is required before Stage 5 can pass. Stage 7 also has an unverified production issue: the IndustrialYard wayfinding sign is on the expansion-industrial-approach reserved corridor. Stage 6 focused Unity evidence is not accepted because the agent run hit an invalid wrapper option and no clean rerun was allowed after the Stage 5 stop.
- next stage: Stage 6, Stage 7 and Stage 8 are BLOCKED by the Stage 5 Player gate. Do not run Tests, Build, run_world_features, Load, ordinary Player or validate_city_capture for later stages until the timing backend and the IndustrialYard sign issue are repaired and verified.


## Stage 5 repair execution record 2026-10-15 16:16Z — FAILED fresh Player retest

- status: FAIL; source commit `21256bc0f9100bc4221141950b62159e14376fff`, branch `main`.
- changed files in this repair: `Assets/_Game/Scripts/Runtime/QaOceanV13.cs` added the `ProfilerRecorderOptions.GpuRecorder` probe and restored timing settings; `Assets/_Game/Scripts/Runtime/WorldExpansionWayfinding.cs` moved the IndustrialYard sign from local `(-68,0,-44)` to `(-68,0,-60)`. No unrelated dirty or downloaded file was reset, cleaned, stashed or deleted.
- commands and results:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.Stage5WaterVegetationTests'`: exit 0; fresh `QA/editmode-results.xml` at `2026-10-15T16:13:21.1800413Z`, 3/3 passed, 0 failed, 0 skipped.
  - `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.Stage7MapWayfindingTests'`: exit 0; fresh `QA/editmode-results.xml` at `2026-10-15T16:08:58.7154811Z`, 6/6 passed, 0 failed, 0 skipped; IndustrialYard sign clearance assertion passed.
  - `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Build -BuildPath work/stage5-water-gpuprobe-20261015T1615/TYCOON2.exe`: exit 0; `build-report.json` says `Succeeded`, built `2026-10-15T16:14:52.5697646Z`, 117022655 bytes, 0 errors, 28 warnings; executable SHA256 `96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873`.
  - `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_player.ps1 -Task OceanV13 -BuildPath work/stage5-water-gpuprobe-20261015T1615/TYCOON2.exe`: exit 2; fresh report `work/ocean-v13/player-20261015T161525726Z/ocean-v13-report.json` finished `2026-10-15T16:16:46.9106986Z`, graphics device NVIDIA GeForce RTX 4060 Laptop GPU, `runtimeErrors=[]`, `passed=false`.
  - Fresh Player report has four complete 15 second ABBA windows: CPU samples 17544/17474/17345/17128 with CPU means 0.854980/0.858436/0.864784/0.875842 ms; GPU samples 0/0/0/0, GPU means -1/-1/-1/-1, backend `ProfilerRecorder`, counter `Render:GPU Frame Time`. The gate failure is exactly `V13 valid CPU and GPU timing samples in every ABBA window`.
- visual evidence: the fresh Player generated 15 coast angle captures, two Marina fog captures and `failure.png` under `work/ocean-v13/player-20261015T161525726Z`; they were not accepted as visual proof in this retest because the required GPU timing gate failed. No new visual claim is made.
- assumptions and open issues: `ProfilerRecorderOptions.GpuRecorder` is a real probe but still exposes no positive GPU samples on this D3D11 Player path. The earlier D3D12 attempt remains rejected because fresh logs showed repeated Blitter initialization errors and invalid captures. The IndustrialYard sign issue is repaired and focused-tested, but Stage 6, Stage 7, Stage 8 and Stage 9 remain blocked by Stage 5. No GPU value was fabricated and CPU timing was not relabeled as GPU timing.
- next stage: stop at Stage 5. Require an approved GPU timing backend or documented hardware/API change, then rerun the Stage 5 focused test, build and OceanV13 Player gate before any later-stage command or release staging.


## Stage 4R execution record 2026-10-15T18:33:37Z — IN PROGRESS

- status: IN PROGRESS; current source commit 4297236 on main; no commit or push made for this stage.
- assumptions and pass criteria: keep Unity 6000.6.3f1, URP 17.6.0, Windows D3D11, 3D Unity, stable area and entity IDs, SaveV2 and migration; approach anchors must remain inside interaction bounds; carrier timeout must return a blocked passenger to Walking without duplicate alighting; no economy change; no GPU timing or long campaign gate.
- changed files created or edited in this stage:
  - Assets/_Game/Scripts/Runtime/WorldInteractionArea.cs
  - Assets/_Game/Scripts/Runtime/WorldExpansionDressing.cs
  - Assets/_Game/Scripts/Runtime/CityPublicTransportPassenger.cs
  - Assets/_Game/Tests/EditMode/Stage4RBoardingCutawayTests.cs
  - Assets/_Game/Tests/EditMode/Stage4RRouteCarryTests.cs
  - Assets/_Game/Tests/EditMode/Stage4RTrafficDiagnosticsTests.cs
- source change evidence:
  - WorldInteractionArea now stores an optional local approach anchor, keeps Add backward compatible, and computes a carrier anchor offset from DoorOutside while preserving StableId and SaveKey.
  - WorldExpansionDressing applies the same anchor to expansion boats.
  - CityPublicTransportPassenger ignores its own and carrier root colliders during capsule checks, ignores its own exit collider, and emergency restores a boarding or alighting passenger after 8 seconds with one safe alighting record.
- fresh focused commands and results:
  - Unity EditMode Tycoon.Tests.WorldPlayerRideTests: exit 0, 4/4 pass, artifact work/stage4r/baseline-world-player-ride-2.xml and log.
  - Unity EditMode Tycoon.Tests.WorldInteractionAreaTests: exit 0, 3/3 pass before and after patch; after artifact work/stage4r/after-interaction.xml and log.
  - Unity EditMode Tycoon.Tests.WorldCutawayVolumeTests: exit 0, 4/4 pass, artifact work/stage4r/baseline-cutaway-volume.xml and log.
  - Unity EditMode Tycoon.Tests.CityPedestrianRouteContractTests: exit 0, 6/6 pass, artifact work/stage4r/baseline-route-contract.xml and log.
  - Unity EditMode Tycoon.Tests.CityTrafficTests: exit 0, 12/12 pass, artifact work/stage4r/baseline-traffic.xml and log.
  - Unity EditMode Tycoon.Tests.CityTrafficGridlockTests: exit 0, 23/23 pass, artifact work/stage4r/baseline-gridlock.xml and log.
  - Unity EditMode Tycoon.Tests.CarryItemLayoutTests: exit 0, 30/30 pass, artifact work/stage4r/baseline-carry-layout.xml and log.
  - Unity EditMode Tycoon.Tests.CityCarryTests: exit 0, 30/30 pass, artifact work/stage4r/baseline-city-carry.xml and log.
  - Unity EditMode Tycoon.Tests.CityPublicTransportTests: exit 0, 6/6 pass after passenger patch, artifact work/stage4r/after-passenger.xml and log.
  - Unity EditMode Tycoon.Tests.CargoBoardingTests: exit 0, 6/6 pass, artifact work/stage4r/after-road-passenger.xml and log.
  - Unity EditMode Tycoon.Tests.CityRailNavigationTests: exit 0, 1/1 pass, artifact work/stage4r/after-rail-passenger.xml and log.
  - Unity EditMode Tycoon.Tests.Stage4RRouteCarryTests: exit 0, 4/4 pass after patch, artifact work/stage4r/after-route-carry.xml and log.
  - Unity EditMode Tycoon.Tests.Stage4RBoardingCutawayTests: exit 0, 6/6 pass after anchor test and patch, artifact work/stage4r/after-boarding-cutaway.xml and log.
  - Unity EditMode Tycoon.Tests.Stage4RTrafficDiagnosticsTests: exit 0, 3/3 pass after patch, artifact work/stage4r/after-traffic.xml and log.
- evidence state: focused XML suites report zero failed and zero skipped; logs contain only the known Unity licensing warnings and Test run completed exit 0. No Player image or runtime smoke evidence has been claimed yet.
- open issues: build and the short Windows Player smoke for walk carry deliver board alight cutaway and customer queue are pending; visual clipping and route completion remain unproven until that smoke produces fresh captures and runtime error output.
- next stage: run one isolated Windows build under work/stage4r, run one short Player smoke only, then append the final Stage 4R gate record. Do not start Stage 5R or commit/push until that gate is closed.



## Stage 4R execution record 2026-10-15T18:44:33Z — FAIL Player gate

- status: FAIL; Stage 4R is stopped at the Player gate. Source commit remains 4297236 on main. No commit or push was made for this stage.
- focused source gate: PASS before Player smoke. WorldPlayerRide 4/4, WorldInteractionArea 3/3, WorldCutawayVolume 4/4, CityPedestrianRouteContract 6/6, CityTraffic 12/12, CityTrafficGridlock 23/23, CarryItemLayout 30/30, CityCarry 30/30, CityPublicTransport 6/6, CargoBoarding 6/6, CityRailNavigation 1/1, Stage4RRouteCarry 4/4, Stage4RBoardingCutaway 6/6 and Stage4RTrafficDiagnostics 3/3 all exited 0 with zero failed and zero skipped.
- changed files in Stage 4R:
  - Assets/_Game/Scripts/Runtime/WorldInteractionArea.cs
  - Assets/_Game/Scripts/Runtime/WorldExpansionDressing.cs
  - Assets/_Game/Scripts/Runtime/CityPublicTransportPassenger.cs
  - Assets/_Game/Tests/EditMode/Stage4RBoardingCutawayTests.cs
  - Assets/_Game/Tests/EditMode/Stage4RRouteCarryTests.cs
  - Assets/_Game/Tests/EditMode/Stage4RTrafficDiagnosticsTests.cs
- build command and result:
  - direct Unity 6000.6.3f1 BuildWindows with output work/stage4r/Build/TYCOON2.exe and log work/stage4r/build.log: build report Result Success, log says Build Successful, Windows build 117023679 bytes. The isolated executable was not copied over Builds/Windows, whose pre existing dirty artifacts were preserved.
- Player smoke command and result:
  - direct work/stage4r/Build/TYCOON2.exe with force D3D11, require 4060, qa items vehicles v8, qa vehicle rides only and output work/stage4r/player-rides: Player exit 2 at 2026-10-15T18:44:33.4356971Z after 2.1 seconds.
  - fresh report work/stage4r/player-rides/items-vehicles-v8-report.json has passed false, graphics device NVIDIA GeForce RTX 4060 Laptop GPU, runtimeErrors empty, distanceWalked 0, interactions 0 and no completed checks. failure.png and failure-state.txt are present.
  - concrete failure is System.Exception QA failed physical relaunch source checkpoint verified in QaDriver.PhysicalSavedAcceptance. The smoke stopped before walk, carry, delivery, customer queue, boarding, alighting or cutaway actions. This is a failed gate, not visual or GPU timing evidence.
- evidence and limitations:
  - focused EditMode and compile evidence are fresh and clean.
  - isolated build is fresh and successful.
  - Player D3D11 initialized on RTX 4060 but the QA checkpoint precondition failed before the requested route, so no Player claim is made for pedestrian clearance, carry attachment, delivery, vehicle ride, cutaway reveal or customer queue.
  - no benchmark, soak, campaign, GPU timing measurement or long rerun was started after the failure.
- open issues:
  - repair or replace the physical saved checkpoint precondition for the bounded vehicle smoke, then rerun only the Stage4R Player smoke.
  - obtain fresh Player evidence for walk, carry, delivery, boarding and alighting, one cutaway enter and exit, and one customer queue; inspect new images and runtime logs for clipping and incomplete paths.
  - existing historical Stage 5 GPU timing failure remains unchanged and is not reclassified by this Stage4R result.
- next stage: STOP at Stage 4R. Do not start Stage 5R, do not commit or push, and do not overwrite Builds/Windows until the Player checkpoint issue is repaired and the same bounded smoke passes.


## Stage 5R execution record
2026-10-15T19:06:42.1149180Z
 — PASS

- status: PASS; source commit 4297236 on main; no source water/vegetation file was edited in this stage; no commit or push made.
- assumptions and pass criteria: keep Unity 6000.6.3f1, URP 17.6.0, Windows D3D11, 3D Unity, stable area/entity IDs, SaveV2 and migration; preserve Three.js prototype and ez-tree output; do not run OceanV13 GPU timing; pass requires focused test, vegetation audit, isolated build, bounded Player smoke, and fresh visual evidence.
- focused commands and results:
  - powershell -NoProfile -ExecutionPolicy Bypass -File .\\Tools\\verify_stage6_vegetation.ps1 -Root .: exit 0; Stage 6 vegetation audit PASS (219 checks).
  - Unity 6000.6.3f1 EditMode Stage5WaterVegetationTests with results work/stage5r/stage5-water-vegetation.xml and log work/stage5r/stage5-water-vegetation.log: exit 0; XML Passed, total 3, passed 3, failed 0, skipped 0.
  - powershell -NoProfile -ExecutionPolicy Bypass -File .\\Tools\\run_unity.ps1 -Task Build -BuildPath work\\stage5r\\Build\\TYCOON2.exe: exit 0; work/stage5r/Build/build-report.json result Succeeded, 117023679 bytes, 0 errors, 5 warnings.
  - isolated Player work/stage5r/Build/TYCOON2.exe with force D3D11, require 4060, --qa-world-features and output work/stage5r/player-worldfeatures: exit 0; world-features-report.json passed true, 3390 checks, runtimeErrors empty, ForestReserve and EastMarina route anchors PathComplete.
- visual evidence opened from work/stage5r/player-worldfeatures: clear-water-a.png, clear-water-b.png and beach-context.png show nonblank blue-green water, depth gradient, shoreline band and foam; expansion-center-east_marina.png and expansion-east_marina.png show marina piers, route and vessel; expansion-center-forest_reserve.png and expansion-forest_reserve.png show forest trees, vegetation scatter and two dirt corridors. No magenta frame was observed.
- shader/material evidence: TownWater compiled into the isolated D3D11 build and renders in fresh captures. The build log has five existing Town Surface DX11 uninitialized-variable warnings, but zero compile errors and no TownWater shader error.
- GPU diagnostic: historical OceanV13 timing failure reports were preserved; OceanV13 was not rerun. GPU timing is diagnostic unavailable on D3D11 for this path and is not a Stage5R blocker.
- files/evidence created by this stage: work/stage5r/stage5-water-vegetation.xml; work/stage5r/stage5-water-vegetation.log; work/stage5r/Build/TYCOON2.exe and build-report.json; work/stage5r/player-worldfeatures/*; no Builds/Windows artifact was overwritten.
- open issues: TownSurface shader warnings remain for a later shader cleanup; this stage did not perform a long campaign, benchmark, soak, GPU timing or exhaustive manual boarding tour. Existing Stage4R and historical Stage5 failure records remain unchanged.
- next stage: Stage 6R may be planned after review; do not start it automatically and do not commit or push before the final release stage.

### Stage 5R canonical heading
-
## Stage 5R execution record 2026-10-15T19:06:42.1149180Z — PASS
; the immediately preceding Stage5R body is the authoritative evidence record and was not otherwise changed.

## Stage 6 — Roads, Farm and District Transitions — execution record

- **Status:** FAIL (gate stopped at Stage 6; no Stage 7 transition).
- **Timestamp:** 2026-10-15T19:45:16.1151867Z
- **Source:** `main` at `4297236 Complete world redesign stage contracts`; stable map/entity IDs, SaveV2, layoutRevision, Unity/URP target and Three.js prototype were not changed.
- **Assumptions and pass criteria:** `WorldRouteCatalog` remains the sole source for dirt path, sidewalk, paved road, highway, rail, dock and forest access. Stage 6 passes only when the focused route/placement/NavMesh evidence is green, every required farm-to-station route is traversable in a short Windows Player smoke, route endpoints are outside collision, reserved corridors reject props/items/trees, and the new overview/junction captures show natural continuous district transitions.
- **Files in this stage:** `Assets/_Game/Scripts/Runtime/QaDriver.cs` (dispatch for `--qa-stage6-routes`), `Assets/_Game/Scripts/Runtime/QaStage6Routes.cs` (short route acceptance routine), `Assets/_Game/Tests/EditMode/Stage6RouteTransitionTests.cs` (focused contracts), and this plan record. The runtime route catalog and stable IDs were not edited.

### Verification commands and results

- Focused EditMode suites (fresh XML under `work/stage6`): `Tycoon.Tests.WorldRouteCorridorTests` 6/6, `Tycoon.Tests.WorldExpansionContractTests` 3/3, `Tycoon.CityAccessPathsSmoothTests` 4/4, `Tycoon.Tests.CityAccessPathsTests` 6/6, `Tycoon.Tests.CityPedestrianPublicRouteTests` 2/2, `Tycoon.Tests.CityPedestrianRouteContractTests` 6/6, `Tycoon.Tests.WorldExpansionRouteAccessTests` 1/1, `Tycoon.Tests.CityObstacleNavigationTests` 1/1, `Tycoon.Tests.BuildingPlacementTests` 11/11, and `Tycoon.Tests.Stage6RouteTransitionTests` 3/3. All exits were 0, with 0 failed and 0 skipped; only existing Unity licensing/obsolescence warnings were logged.
- Build command: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Build -BuildPath work\stage6\Build\TYCOON2.exe`; exit 0. `work/stage6/Build/build-report.json` records `result=Succeeded`, 117029311 bytes, 29.1034 seconds, 0 errors and 28 existing warnings.
- Player smoke command: `work/stage6/Build/TYCOON2.exe -screen-width 1920 -screen-height 1080 -window-mode 0 -force-d3d11 --qa-stage6-routes --require-4060 --qa-output work/stage6/player-stage6-routes`; exit 2. The run used D3D11 on an RTX 4060 and terminated on the bounded short smoke timeout/failure path; no long campaign, benchmark, soak or GPU timing was run.

### Evidence

- Focused XML: `work/stage6/baseline-world-route.xml`, `baseline-expansion-contract.xml`, `baseline-smooth-path.xml`, `baseline-curved-path.xml`, `baseline-public-route.xml`, `baseline-pedestrian-route-contract.xml`, `baseline-expansion-access.xml`, `baseline-obstacle-navigation.xml`, `baseline-building-placement.xml`, and `stage6-route-transition-after-qa.xml`.
- Build evidence: `work/stage6/Build/build-report.json` and `work/stage6/build.log`.
- Player evidence: `work/stage6/player-stage6-routes/stage6-routes-report.json`, `work/stage6/player-stage6-routes/navigation-route.txt`, `work/stage6/player-stage6-routes/failure-state.txt`, `work/stage6/player-stage6-routes/stage6-farm.png`, `stage6-connector.png`, `failure.png`, and `work/stage6/player-stage6-routes.log`.
- The farm and paved connector captures are nonblank and show the expected farm beds, dirt path, road and connector surfaces. The failure capture is also nonblank with no magenta/blank frame. Industrial and station captures were not produced because the route stopped before those visits.

### Gate failure and open issues

- The Player route reached the farm and connector checks and passed the industrial NavMesh-path check, then failed physical movement: `Movement timeout: (328.03, 0.00, 252.90) from (254.76, 0.02, 213.18)` in `QaDriver.WalkTo`. The report shows `distanceWalked=495.4259`, `interactions=0`, and `runtimeErrors=[]`; this is a route traversal failure rather than a Unity runtime exception.
- The physical expansion industrial approach still needs diagnosis for agent movement, corridor geometry, NavMesh clearance or a corner/timeout assumption before Stage 6 can pass. Because the station leg and junction/overview acceptance were not reached, district transition continuity is not accepted yet.
- **Next stage:** STOP at Stage 6. Do not begin Stage 7, commit, push, or overwrite `Builds/Windows` until a future Stage 6 retry fixes the movement gate and produces fresh industrial/station evidence.

## Stage 7 — District detail and accessible POIs — 2026-10-16
- Status: **FAIL — stop at Stage 7 gate**. Stable map/entity IDs, SaveV2/migration, Unity 6000.6.3f1, URP 17.6.0, Windows D3D11 and 3D runtime were preserved. No commit, push or overwrite of `Builds/Windows` was performed.
- Assumptions and pass criteria: every catalog POI must expose a stable ID, label, footprint, approach and exit anchors, interaction area and collision boundary; a short Player route must complete without movement timeout, interaction failure, runtime error or fallback reparent.
- Files changed in this stage: `Assets/_Game/Scripts/Runtime/WorldPoiAccessCatalog.cs`, `Assets/_Game/Scripts/Runtime/WorldPoiAccessBuilder.cs`, `Assets/_Game/Scripts/Runtime/WorldFactory.cs`, `Assets/_Game/Scripts/Runtime/WorldExpansionWayfinding.cs`, `Assets/_Game/Scripts/Runtime/CityTrafficSignalVisual.cs`, `Assets/_Game/Scripts/Runtime/QaStage7PoiAccess.cs`, `Assets/_Game/Scripts/Runtime/QaDriver.cs`, `Assets/_Game/Tests/Editor/Stage7PoiAccessTests.cs`.
- Focused checks: `Unity.exe -batchmode -nographics -projectPath D:\GAME\TYCOON2 -runTests -testPlatform EditMode -testFilter Tycoon.Tests.Stage7PoiAccessTests -testResults work\stage7\stage7-poi-access-after-harness-results.xml -logFile work\stage7\stage7-poi-access-after-harness.log` → exit 0, 3/3 passed; `Unity.exe -batchmode -nographics -projectPath D:\GAME\TYCOON2 -runTests -testPlatform EditMode -testFilter Tycoon.Tests.Stage7MapWayfindingTests -testResults work\stage7\stage7-map-wayfinding-after-qa-results.xml -logFile work\stage7\stage7-map-wayfinding-after-qa.log` → exit 0, 6/6 passed; `Tools\verify_stage7_wayfinding.ps1 -ProjectRoot .` → `SUMMARY PASS=23 FAIL=0`.
- Build: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Build -BuildPath work\stage7\Build\TYCOON2.exe` → Unity exit 0; `work\stage7\Build\build-report.json` at `2026-10-16T00:18:40.6945789Z` reports `Succeeded`, 117045551 bytes, 34.5185528 seconds, errors 0, warnings 32. Build log ended with successful batchmode shutdown.
- Player smoke: `work\stage7\Build\TYCOON2.exe -screen-width 1920 -screen-height 1080 -window-mode 0 -force-d3d11 --require-4060 --qa-stage7-poi-access --qa-output work\stage7\player-poi-access-after-harness -logFile work\stage7\player-stage7-poi-access-after-harness.log` → exit 2 at `2026-10-16T00:22:07.3054563Z`. Report: `work\stage7\player-poi-access-after-harness\stage7-poi-access-report.json`; graphics `NVIDIA GeForce RTX 4060 Laptop GPU`, interactions 9, runtimeErrors `[]`, screenshots `stage7-farm.png`, `stage7-residential.png`, `stage7-park.png`, `stage7-interior-bank.png`, `failure.png`.
- Gate failure evidence: `Movement timeout: (-37.44, 0.04, 84.00) from (-65.09, 0.02, 86.08)` while visiting `commercial_market`. The smoke therefore did not reach restaurant, industrial, station, harbor, marina, forest, beach or highway service captures. This is an actual route/access failure, not a runtime error or GPU timing diagnostic.
- Open issues: repair the commercial market NavMesh/approach anchor/collision corridor from the reported start and retest the complete Stage 7 smoke; then obtain the remaining POI images and rerun the Player gate. Do not start Stage 8 or claim Stage 7 pass until that route completes. GPU timing was not run by instruction.
- Next stage: **Stage 7 remediation required; Stage 8 blocked by this gate.**

## Stage 9 — Map and Wayfinding — execution record 2026-10-16T00:45:36Z

- status: PASS; Stage 9 gate closed. Unity 6000.6.3f1, URP 17.6.0, Windows Player, Direct3D11, 3D runtime, stable map/entity IDs, SaveV2/migration and the existing Three.js prototype were preserved. No commit or push was made.
- assumptions and pass criteria: CityMapLocations remains the stable point source; route signs resolve through WorldRouteCatalog; label rectangles use transformed district bounds; traffic vehicle and pedestrian phases share the same phase clock. Pass requires focused map/label/sign/signal tests, a clean Windows build, and a bounded Player map smoke at 1280x720, 1920x1080 and 2560x1440 with fresh nonblank images and no runtime errors.
- changed files in Stage 9:
  - Assets/_Game/Scripts/Runtime/CityMapLocations.cs
  - Assets/_Game/Scripts/Runtime/CityMapGraphic.cs
  - Assets/_Game/Scripts/Runtime/GameHudMap.cs
  - Assets/_Game/Scripts/Runtime/CityTrafficSignalVisual.cs
  - Assets/_Game/Scripts/Runtime/CityTrafficSignals.cs
  - Assets/_Game/Scripts/Runtime/WorldExpansionWayfinding.cs
  - Assets/_Game/Scripts/Runtime/QaDriver.cs
  - Assets/_Game/Scripts/Runtime/QaStage9Map.cs
  - Assets/_Game/Tests/Editor/Stage7MapWayfindingTests.cs
  - Assets/_Game/Tests/Editor/Stage9MapWayfindingTests.cs
  - Docs/PLAN_WORLD_REDESIGN_20261015.md
- source result: stable map points retain their original order and IDs; residential, apartment and commercial points have distinct glyphs/colors/mesh icons and caption offsets; map side rows fit all points; expansion signs derive their position and facing from the matching route catalog tangent and are kept off reserved corridors; traffic signals now have non-colliding pole/arm supports and expose a phase description using the same vehicle/pedestrian state clock.

### Verification commands and results

- Stage7 compatibility focused test: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.Stage7MapWayfindingTests'` → Unity exit 0; fresh `QA/editmode-results.xml` at 2026-10-16T00:43:18Z reports total 6, passed 6, failed 0, skipped 0.
- Stage9 focused test: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.Stage9MapWayfindingTests'` → Unity exit 0; fresh `QA/editmode-results.xml` at 2026-10-16T00:43:42Z reports total 4, passed 4, failed 0, skipped 0. Coverage includes readable category icons, non-overlapping labels at all supported resolutions, route-facing signs off reserved corridors, signal support geometry and synchronized vehicle/pedestrian phases.
- Windows build: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Build` → Unity exit 0; `QA/build-report.json` builtUtc `2026-10-16T00:44:45.4640635Z`, result `Succeeded`, bytes 117054255, errors 0, warnings 32. Output is `D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe`.
- Player smoke: `D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe --qa-stage9-map --qa-output D:\GAME\TYCOON2\work\stage9\player-20261016T0045Z` → Player exit 0. `stage9-map-report.json` passed true, graphics device `NVIDIA GeForce RTX 4060 Laptop GPU`, interactions 1, runtimeErrors `[]`, finishedUtc `2026-10-16T00:45:36.3575813Z`; report confirms map open/close restores control, camera sees player, interaction prompt remains readable, and every label stays readable/on-screen at 1280x720, 1920x1080 and 2560x1440.
- phase evidence: `work/stage9/player-20261016T0045Z/stage9-traffic-phase.log` records vehicle and pedestrian phases for west_south, west_mid, west_north, east_south, east_mid and east_north; amber vehicle states pair with pedestrian Red and green vehicle states pair with pedestrian Walk.
- visual evidence: `work/stage9/player-20261016T0045Z/stage9-traffic-signal.png`, `stage9-map-1280x720.png`, `stage9-map-1920x1080.png` and `stage9-map-2560x1440.png` were opened. All are fresh nonblank captures with visible map labels/icons/routes and signal pole/arm geometry; no magenta frame or blank map was observed.

- open issues: build report retains 32 warnings; no compile error or runtime error was reported. GPU timing/benchmark/soak was intentionally not run. Historical Stage 4R, Stage 6 and Stage 7 Player failures remain unchanged in their earlier records and are not reclassified by this Stage 9 gate. No commit or push was made.
- next stage: Stage 10 release preparation may proceed after review; keep the no-benchmark/no-GPU-timing boundary and do not commit or push until the final release stage.

## Stage 10 — SaveV2, boarding and integration — execution record 2026-10-16T01:01:48Z

- status: PASS; Stage 10 gate closed. Unity 6000.6.3f1, URP 17.6.0, Windows D3D11, 3D runtime, stable area/entity IDs, SaveV2/migration and the existing Three.js prototype were preserved. No runtime source file was changed in Stage 10; only this plan record was appended. No commit or push was made.
- assumptions and pass criteria: use only focused SaveV2/migration suites and the QA completion fixture; never use a real user save or a long campaign. Pass requires process-new Save and Load exit 0, fresh reports with passed=true and runtimeErrors empty, version and layout revision preserved, and no loss of goods, money, orders, route, reservations or interaction state and no duplicate payment, loss, purchase or boarding effect.
- stable contract evidence: the fresh QA snapshot `work/completion-audit/player-save-20261016T010020123Z/completion-midflight.json` has root `version=3`, `transactionVersion=3`, `transactionState.layoutRevision=2`, `transactionState.revision=42`, one carried stack, one reservation and purchase IDs `truck_bundle,carry10`. The focused migration and compatibility suites passed without changing stable IDs.

### Focused verification commands and results

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.SaveV2Tests'`: Unity exit 0; fresh XML at 2026-10-16T00:52:11Z, 6/6 passed, 0 failed, 0 skipped. Covers partial delivery reload without refund, receipt and pending cash deduplication, reservations, repeated SaveV2 reads and v1 to v2 normalization.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.CityMigrationTests'`: Unity exit 0; fresh XML at 2026-10-16T00:52:53Z, 23/23 passed, 0 failed, 0 skipped. Covers worker identity, goods, money, crew and travelling truck progress/route completion migration.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.SaveWorldCompatibilityTests'`: Unity exit 0; fresh XML at 2026-10-16T00:53:49Z, 8/8 passed, 0 failed, 0 skipped. Invalid-save and lease failures were asserted with unchanged bytes.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.TransactionCoreTests'`: Unity exit 0; fresh XML at 2026-10-16T00:55:18Z, 46/46 passed, 0 failed, 0 skipped. Includes transaction crash boundaries, carry, conveyor, machine escrow/recovery, partial purchase, reservations, outbox/event/payment deduplication and the partial `WorkforceJournalCompatibilityTests` methods.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.CompletionMigrationTests'`: Unity exit 0; fresh XML at 2026-10-16T00:57:14Z, 2/2 passed, 0 failed, 0 skipped.
- The attempted filter `Tycoon.Tests.WorkforceJournalCompatibilityTests` produced Unity exit 0 but total 0 at 2026-10-16T00:58:05Z because that file is a partial `TransactionCoreTests` fixture; it is not counted as a pass and was corrected by the 46/46 `TransactionCoreTests` run.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.RetiredPurchaseJournalTests'`: Unity exit 0; fresh XML at 2026-10-16T00:59:14Z, 3/3 passed, 0 failed, 0 skipped. Retired purchase contribution and completion journal replay is once-only and new contribution is rejected.

### Process-new QA save and load

- Build evidence reused without rebuild because no Stage10 source changed: `QA/build-report.json` built 2026-10-16T00:44:45Z, result `Succeeded`, errors 0, warnings 32; executable `D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe` last written 2026-10-15T17:20:17Z.
- Save command: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_completion.ps1 -Case Save -BuildPath .\Builds\Windows\TYCOON2.exe`; player exit 0. Evidence `work/completion-audit/player-save-20261016T010020123Z/completion-report.json` has `passed=true`, finished 2026-10-16T01:00:30Z, `runtimeErrors=[]`, and fresh `completion.png`. The report checks preserve wallet/counter revenue/collected cash, every item owner/location/quantity, partial and paid orders with price snapshots, partial purchase contributions, production/herd/repair fields, moving truck route and worker jobs, while repeated loads do not pay or collect twice and do not record loss twice.
- Load command: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_completion.ps1 -Case Load -BuildPath .\Builds\Windows\TYCOON2.exe -LoadPath .\work\completion-audit\player-save-20261016T010020123Z\completion-midflight.json`; new player exit 0. Evidence `work/completion-audit/player-load-20261016T010127330Z/completion-load-report.json` has `passed=true`, finished 2026-10-16T01:01:48Z, 52 checks, `runtimeErrors=[]`, and fresh `restored.png`. The final checks prove reserved delivery completes, original reservation is consumed once, partial repair resumes without reset, truck reaches its destination with cargo, repeated unload does not duplicate cargo, and relaunch has no runtime errors. `player.log` contains `COMPLETION-LOAD_QA PASS` and no exception/error match.
- No benchmark, soak, GPU timing or long campaign was run. The QA save and load use only the generated completion fixture and were not user saves.

- open issues: the build retains 32 existing warnings; historical Stage 4R, Stage 6 and Stage 7 Player route failures remain recorded and unchanged; GPU timing remains diagnostic unavailable on D3D11 and was intentionally not run. These do not invalidate the scoped Stage10 SaveV2/migration gate.
- next stage: final release review and release staging may proceed. Do not commit, push or overwrite unrelated dirty/untracked files until that final release stage is explicitly completed.

## Stage 10 correction — boarding persistence gate 2026-10-16T01:09:49Z — FAIL / STOP

- status: FAIL; this correction supersedes the preceding Stage10 PASS record for release gating. Stage10 stops here per the rule that a failed gate cannot advance. No runtime source file was changed and no commit or push was made.
- concrete blocker: the fresh `completion-report.json` and `completion-load-report.json` contain no boarding, board, alight or ride checks. Source inspection shows `SaveData` stores only `playerX/playerZ` and cargo `transit`; `WorldPlayerRide` keeps `activeRide`, `carrier`, `fallbackHull`, `originalParent`, `exitPoint` and `endsAt` only in a runtime component; `GameSession.CaptureSaveData` never exports that state; `GameSession.LoadGame` calls `Player.StopInteraction()`, restores only `playerX/playerZ`, enables the controller and never reattaches a carrier or restores a passenger phase. `ICityPassengerCarrier` has no stable save key or import contract, and the carrier implementations keep their passenger lists in memory. A process-new Save/Load therefore cannot prove or preserve a player who was boarding.
- positive evidence retained: `Tycoon.Tests.Stage4CutawayBoardingTrafficContractTests` exit 0, fresh XML at 2026-10-16T01:09:49Z, 6/6 passed, 0 failed, 0 skipped. This proves runtime boarding rejection, hull requirements and safe boarding/alighting counters, but it is not SaveV2 persistence evidence.
- prior SaveV2/migration and process-new evidence remains valid but is limited to its stated checks: SaveV2 6/6, CityMigration 23/23, SaveWorldCompatibility 8/8, TransactionCore 46/46, CompletionMigration 2/2, RetiredPurchaseJournal 3/3; QA Save exit 0 with `passed=true`; QA Load in a new process exit 0 with 52 checks and `runtimeErrors=[]`. Those reports prove goods, wallet, orders, reservations, production, repair and truck route continuity, not active player boarding continuity.
- required remediation before Stage10 can pass: define a stable carrier identity and a versioned optional `PlayerRideSave` contract; capture active ride phase, carrier key, remaining transition time, original parent semantics and safe exit anchor without changing existing area/entity IDs; restore or safely resolve that state before `Player.CanControl` is re-enabled; add focused SaveV2 migration and process-new boarding tests that assert no duplicate board/alight counter and no lost route/interaction state; rebuild the Windows Player and rerun the bounded Save/Load fixture. Do not invent a runtime `GetInstanceID` key.
- open issues: build report still has 32 existing warnings; historical Stage4R, Stage6 and Stage7 Player route failures remain unchanged; GPU timing remains diagnostic unavailable on D3D11 and was not run. The previous Stage10 PASS paragraph is retained as audit history but must not be used as a release gate.
- next stage: **STOP at Stage10 remediation.** Do not start final release review, do not commit or push, and do not overwrite unrelated dirty/untracked files until the boarding SaveV2 contract and fresh process-new evidence pass.

## Stage 12 — Final delivery audit — 2026-10-16T01:17:31.7878521Z — FAIL / STOP

- status: FAIL / STOP. This final-delivery audit is blocked by the authoritative Stage 10 correction dated 2026-10-16T01:09:49Z. The rule requiring a stop after a failed gate is applied; Stage 12 is not advanced, and no release artifact is declared complete.
- assumptions and pass criteria: Stage 12 requires accepted Stage 4R, 5R, 6, 7, 8, 9 and 10 gates, then one bounded focused-test pass, one full EditMode pass when reasonable, one final Windows build, one short Player smoke covering the requested districts/interactions and Save/Load, fresh image and Player.log review, diff/LFS/secret/large-file checks, allowlisted staging, commit and non-force push. A known failed predecessor gate prevents those dependent commands.
- audit evidence: Stage 4R remains FAIL at its Player checkpoint; Stage 5R has a PASS record with GPU timing diagnostic unavailable on D3D11; Stage 6 remains FAIL after a farm-to-industrial movement timeout; Stage 7 remains FAIL after a commercial_market approach timeout and Stage 8 has no accepted pass record because it was blocked by Stage 7; Stage 9 remains PASS; Stage 10 is FAIL / STOP because process-new Save/Load has no active player boarding persistence contract or evidence. The earlier Stage 10 PASS paragraph is retained as history and is superseded by its correction.
- fresh Stage 10 evidence retained: SaveV2 6/6, CityMigration 23/23, SaveWorldCompatibility 8/8, TransactionCore 46/46, CompletionMigration 2/2, RetiredPurchaseJournal 3/3, runtime Stage4CutawayBoardingTrafficContractTests 6/6, QA Save process exit 0 and QA Load process exit 0 with 52 checks. These prove the stated inventory, money, order, reservation, production, repair and truck-route continuity plus runtime boarding safety; they do not prove SaveV2 restoration of an active player ride.
- Stage 12 commands intentionally not run after the failed gate: full EditMode suite, Tools/run_unity.ps1 -Task Build for a final release rebuild, final Player route smoke, new final image review, new final Player.log review, git diff --check, git lfs status, secret/large-file audit, allowlisted git staging, commit and git push origin main. The existing executable at D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe and QA/build-report.json are historical Stage 9 evidence with 32 warnings, not a Stage 12 release build.
- files changed for this audit: Docs/PLAN_WORLD_REDESIGN_20261015.md only. No runtime, test, build, save, QA-history, download or unrelated dirty/untracked file was modified, removed, reset, cleaned, stashed or overwritten. Existing dirty Assets/_Game/Scripts/Runtime/WorldInteractionArea.cs and all other preexisting files remain preserved.
- gate result: final delivery is not accepted; no build SHA256, commit SHA or push result exists for Stage 12. The absence of those values is intentional, not an omitted verification.
- open issues: implement and test a stable carrier identity and versioned optional PlayerRideSave contract; capture active ride phase, carrier key, transition timing, parent semantics and safe exit anchor; restore safely before player control is enabled; add focused migration and process-new boarding tests; repair the earlier Stage 4R, Stage 6 and Stage 7 Player route failures; then rebuild and rerun the bounded release evidence. Preserve the D3D11 GPU diagnostic-unavailable note and the existing build warning count.
- next stage: Stage 10 remediation only. After that gate passes with fresh evidence, restart the Stage 12 final-delivery checklist and obtain user-visible build, smoke, hash, commit and push evidence.

## Stage 11 — Owned temporary cleanup — 2026-10-16T01:23:23.4140967Z — PASS / NO-OP

- status: PASS / NO-OP. Ownership audit completed before any deletion. No file met the deletion contract, so no path was removed.
- assumptions and pass criteria: deletion would require explicit stage ownership, a temporary/build/log/capture role recorded in the plan, no process lock, and exclusion from protected work, QA, save, video, download, dirty or final-build areas. The final build/evidence must remain available and git status must preserve all unrelated changes.
- ownership manifest evidence: ASSET/source-manifest.json identifies retained source packs/licenses and was not a cleanup manifest; mod/test/stage11-near-supermarket.json, mod/test/stage12-near-restaurant.json and mod/test/stage12-near-bakery.json are retained test fixtures, not disposable output. The plan explicitly records stage evidence under work, QA and downloaded/source areas, which are protected by this stage request.
- candidate audit command: rg --files -g '*.tmp' -g '*.temp' -g '*.bak' -g '*.old' -g '*.orig' -g '*.rej' -g '*.pid' -g '*.lock' -g '*.dmp' -g '*.crash' -g '*.partial' -g '*.part' -g '*.download' filtered outside protected areas → exit 0, result NONE. Top-level temp-like entry scan → NONE. Process check for Unity, UnityHub and TYCOON2 → no matching process.
- deletion result: paths deleted 0; bytes before 0; bytes after 0; deletion command was intentionally not issued. Existing Stage4R–Stage10 evidence, final-path build, QA history, saves, downloaded assets, dirty source and untracked files remain untouched.
- files changed in this stage: Docs/PLAN_WORLD_REDESIGN_20261015.md only. No source, asset, build, log, capture, save or QA file was modified.
- open issues: Stage 10 remains authoritative FAIL / STOP for missing SaveV2 active-player boarding persistence. Stage 12 final build, smoke, hash, staging, commit and push remain blocked by that gate. Existing Stage4R, Stage6 and Stage7 Player failures and the D3D11 GPU diagnostic-unavailable note remain unchanged.
- next stage: Stage 10 remediation only; after its stable carrier/PlayerRideSave contract and fresh process-new evidence pass, restart Stage 12 final delivery. Do not infer release readiness from this cleanup no-op.

## Stage 10 remediation — CaptureSaveData PlayerRideSave hook — 2026-10-16T01:55:01Z

- status: in progress; this is a narrow production edit within the authoritative Stage 10 remediation gate. The existing SaveData version, transaction version, stable IDs and migration rules remain unchanged.
- assumptions and pass criteria: an active `WorldPlayerRide` is optional state, so `CaptureSaveData` must leave `playerRide` null when no stable interaction ID is available and must serialize only the validated version-1 active contract when capture succeeds. Existing saves without this field must continue to validate.
- files changed: `Assets/_Game/Scripts/Runtime/GameSession.cs` only. Added the `WorldPlayerRide.TryCapture(Player, out PlayerRideSave)` hook immediately after the existing player position snapshot and before inventory/progression projection.
- verification command: `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.SaveV2Tests` → Unity exit 0; fresh XML result `Passed`, total 6, passed 6, failed 0, skipped 0. This compile/test run validates the new optional field against the existing SaveV2 fixtures.
- evidence: the focused XML was read after the run at 2026-10-16T01:55:01Z; no test failure or runtime exception was emitted by that run. The pre-existing Editor.log tail contains a generic historical `Scripts have compiler errors` line and is not used as run evidence.
- open issues: `LoadGame` still needs to cancel a runtime ride and restore `playerRide` before control is re-enabled; the new process-new boarding fixture is pending. Historical Stage4R, Stage6 and Stage7 Player route failures remain unchanged.
- next stage: add the matching `GameSessionPersistence.LoadGame` cancel/restore hook, then run fresh Stage10 and SaveV2 focused tests before advancing the gate.

## Stage 10 remediation — PlayerRide capture/restore QA mode — 2026-10-16T02:29:30.7764112Z

- status: production wiring and focused contract tests PASS; process-new Player evidence pending. The optional version-1 PlayerRideSave remains backward-compatible and stable interaction IDs remain unchanged.
- assumptions and pass criteria: process save must board a real WorldExpansionBoat, checkpoint a QA-only slot, persist an active ride with stable area ID and remaining time, and write a separate snapshot. Process load must read that snapshot through the existing --qa-load-from path, restore before control is enabled, attach the player to the same hull, and record exactly one import boarding with no alighting.
- files changed: Assets/_Game/Scripts/Runtime/QaStage10Ride.cs (new bounded save/load routines and evidence files) and Assets/_Game/Scripts/Runtime/QaDriver.cs (dispatch for --qa-stage10-ride-save and --qa-stage10-ride-load). No economy, stable ID, route, migration or Three.js runtime contract changed.
- verification command: powershell -NoProfile -ExecutionPolicy Bypass -File Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.Stage10PlayerRideSaveTests → Unity exit 0; fresh QA/editmode-results.xml result Passed, total 6, passed 6, failed 0, skipped 0. The suite covers capture, fresh-process-style restore, finite missing-carrier fallback, legacy optional payload and original-parent semantics.
- evidence: the focused XML was written after the QA-mode compile at 2026-10-16T02:00:43Z. No Unity process remains. The new Player save/load smoke has not yet been run, so Stage 10 remains open.
- open issues: run one bounded save process and one bounded load process using the final build or a rebuilt Player, inspect stage10-ride-state.json, screenshots and Player.log, then repair any concrete failure. Historical Stage4R, Stage6 and Stage7 route failures remain unchanged.
- next stage: add a small PowerShell runner for the two process-new ride modes, run it once after a build is available, and append fresh pass/fail evidence.

## Stage 10 and route focused recheck — 2026-10-16T02:47:34.7617694Z

- status: focused contract evidence PASS; release gates remain open. The fresh Unity XML runs completed with exit code 0 and no failed cases for Stage10PlayerRideSaveTests (6/6, 02:41:29Z), Stage6RouteTransitionTests (3/3, 02:43:25Z), Stage7PoiAccessTests (3/3, 02:44:30Z) and Stage7MapWayfindingTests (6/6, 02:45:45Z). Stage4CutawayBoardingTrafficContractTests also exited 0 in a separate fresh run, but its XML was superseded before a count snapshot and is not used as a new count claim.
- files: no production source changed during this focused recheck. The existing Stage10 files remain Assets/_Game/Scripts/Runtime/SaveData.cs, Assets/_Game/Scripts/Runtime/WorldInteractionArea.cs, Assets/_Game/Scripts/Runtime/GameSession.cs, Assets/_Game/Scripts/Runtime/GameSessionPersistence.cs, Assets/_Game/Scripts/Runtime/QaStage10Ride.cs and Assets/_Game/Scripts/Runtime/QaDriver.cs.
- commands: powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.Stage10PlayerRideSaveTests'; same command with Stage6RouteTransitionTests, Stage7PoiAccessTests and Stage7MapWayfindingTests.
- evidence: QA/editmode-results.xml was read after each relevant run with the built-in XML parser. The latest Stage7 map XML reported result Passed, total 6, passed 6, failed 0, skipped 0, started 2026-10-16 02:45:45Z and finished 02:45:46Z. No Player build or runtime smoke was inferred from these EditMode results.
- open issues: process-new Stage10 ride save/load, fresh Windows build, Player route smoke, final image/log review and Stage12 release audit remain pending. Historical Player timeouts in Stage4R, Stage6 and Stage7 remain authoritative until a rebuilt Player reruns them. GPU timing remains diagnostic unavailable on D3D11 and is not a blocker under the user boundary.
- next stage: run the single final Windows build after source review, then perform bounded process-new ride QA and short route smoke. Stop at the first runtime gate failure and record the concrete log.

## Stage 10 remediation — load lifecycle correction — 2026-10-16T03:11:55.3172618Z

- status: Stage 10 remains FAIL / STOP pending rebuilt Player load evidence. The first process-new save passed, while the first process-new load exited 2 and wrote stage10-ride-load-report.json with passed=false; Player.log recorded two CharacterController.Move called on inactive controller errors and a parent activation warning for ExpansionPassengerBoat.
- assumptions and pass criteria: the inactive Move errors came from an actor motion tick during world or boat activation, so the correction is scoped to the existing actor lifecycle guard. The contract remains unchanged: PlayerRideSave version 1, stable interaction ID harbor_vessel, no economy or stable ID changes. Pass requires ActorPhysicalMotionTests and Stage10PlayerRideSaveTests exit 0, a rebuilt Windows Player, then process-new save and load with no runtimeErrors and exactly one boarding with zero alightings on load.
- files changed: Assets/_Game/Scripts/Runtime/ActorPhysicalMotion.cs only for the correction. Update and ApplyStep now return while the component or GameObject is inactive before enabling or moving its CharacterController. Existing Stage10 production files and QA files remain unchanged.
- verification: Tycoon.Tests.ActorPhysicalMotionTests Unity exit 0, fresh XML 3/3 passed, 0 failed, 0 skipped at 2026-10-16 03:01:21Z. Tycoon.Tests.Stage10PlayerRideSaveTests Unity exit 0, fresh XML 6/6 passed, 0 failed, 0 skipped at 2026-10-16 03:02:46Z.
- evidence retained: save process output work/stage10-ride-final-save-20261016T025156Z passed with interactionId=harbor_vessel, active phase 1 and remainingSeconds 20. The load output work/stage10-ride-final-load-20261016T025314Z remains a failed diagnostic and is not deleted.
- open issues: rebuild after the lifecycle edit, rerun process-new load, inspect fresh Player.log and image, then update this gate. Do not run Stage12 smoke, staging, commit or push while Stage10 is failed.
- next stage: rebuild Windows Player once for this correction, run the bounded save/load fixture, and stop again if any runtime error or boarding mismatch remains.

## Stage 10 remediation - PlayerController inactive-controller guard - 2026-10-16T03:21:30Z

- status: FAIL / STOP pending rebuilt Player load evidence. The second process-new load after the ActorPhysicalMotion guard still exited 2 at `work/stage10-ride-final-load-20261016T031325Z`; its report has `passed=false`, two `CharacterController.Move called on inactive controller` runtime errors, and the `ExpansionPassengerBoat` parent activation warning. This remains the authoritative pre-fix runtime evidence.
- assumptions and pass criteria: `GameSession` enables player control when world restoration finishes while an active PlayerRide intentionally keeps its CharacterController disabled. `PlayerController.Update` must reset motion and return before `Move` whenever the controller, player component, or GameObject is inactive; the PlayerRideSave version 1 contract, stable `harbor_vessel` ID, boarding count and alighting count must remain unchanged. Pass requires fresh focused tests, one rebuilt Windows Player, and process-new load with no runtimeErrors, one boarding and zero alightings.
- file changed: `Assets/_Game/Scripts/Runtime/PlayerController.cs`. Added a lifecycle guard immediately before movement so a disabled/inactive CharacterController cannot receive `Move`; no economy, stable ID, SaveV2 schema or route data changed.
- verification after edit: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.ActorPhysicalMotionTests'` -> Unity exit 0; `QA/editmode-results.xml` at 2026-10-16 03:19:34Z reports 3/3 passed, 0 failed, 0 skipped. `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.Stage10PlayerRideSaveTests'` -> Unity exit 0; the same fresh XML at 2026-10-16 03:20:46Z reports 6/6 passed, 0 failed, 0 skipped.
- open issues: rebuild `D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe`, rerun the bounded process-new Stage10 ride save/load fixture, inspect fresh Player.log and captures, and only then decide the Stage10 gate. The existing second-load failure is retained; no Stage12 smoke, staging, commit or push is allowed while it fails.
- next stage: one Windows build for this scoped guard, then one bounded Stage10 save/load process check; stop again on any runtime error, parent warning that affects restore, or boarding mismatch.

## Stage 10 remediation - deferred PlayerRide restore - 2026-10-16T03:35:00Z

- status: focused contract PASS; rebuilt Player and process-new ride QA still pending. The corrected load invocation at `work/stage10-ride-final-load-20261016T0326Z` already passed the ride checks but retained a Unity parent activation warning. The warning was traced to restoring from `GameSession.Start` while `ExpansionPassengerBoat` was still activating.
- assumptions and pass criteria: keep the version-1 optional PlayerRideSave and stable `harbor_vessel` interaction ID unchanged; defer only the runtime reparent operation until the first `GameSession.Update` after `NavigationReady`, while `IsRestoring` and `Player.CanControl=false` still protect the player. The fresh process load must keep active phase 1, one boarding, zero alightings, no runtimeErrors and no parent activation warning.
- files changed: `Assets/_Game/Scripts/Runtime/GameSession.cs` adds a private pending ride field and restores it after `NavigationReady` before the restore gate opens. `Assets/_Game/Scripts/Runtime/GameSessionPersistence.cs` now stores an active save payload in that field instead of reparenting during `Start`; no economy, stable ID, migration or renderer contract changed.
- verification after the persistence edit: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.Stage10PlayerRideSaveTests'` -> Unity exit 0; fresh `QA/editmode-results.xml` at 2026-10-16 03:34:13Z reports 6/6 passed, 0 failed, 0 skipped.
- open issues: run one build, then one fresh process save and one corrected process load with `--qa-load --qa-load-from` plus the Stage10 QA flags; inspect report, state, image and Player.log. The earlier missing-flag load at `work/stage10-ride-final-load-20261016T0325Z` remains retained as harness failure evidence and is not a product gate.
- next stage: build the deferred restore, rerun Stage10 process QA, then update the gate before any Stage12 smoke or release staging.

## Stage 10 remediation - teardown reparent warning guard - 2026-10-16T03:45:00Z

- status: focused contracts PASS; a rebuilt Player and fresh process-new save/load are pending for this final lifecycle correction. The prior load report `work/stage10-ride-final-load-20261016T0324B` passed all ride checks with no runtimeErrors but retained one teardown warning after `STAGE10-RIDE-LOAD_QA PASS`.
- assumptions and pass criteria: the warning is teardown-only because `WorldPlayerRide.OnDisable` attempted `EndRide` while `ExpansionPassengerBoat` was inactive. When player or hull is already inactive, cleanup must clear runtime references without reparenting or recording a false alighting; normal active-scene `EndRide` must still reparent safely and record one alighting.
- file changed: `Assets/_Game/Scripts/Runtime/WorldInteractionArea.cs`. `OnDisable` now returns for an inactive player or hull after clearing ride state, and retains the existing `EndRide` path when both remain active. SaveV2, stable IDs, route data, economy and boarding import counters are unchanged.
- verification after edit: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.Stage10PlayerRideSaveTests'` -> Unity exit 0; fresh XML at 2026-10-16 03:42:50Z reports 6/6 passed, 0 failed, 0 skipped. `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.Stage4CutawayBoardingTrafficContractTests'` -> Unity exit 0; fresh XML at 2026-10-16 03:44:16Z reports 6/6 passed, 0 failed, 0 skipped.
- open issues: rebuild the Windows Player and rerun one save and one corrected load process, then inspect whether the teardown warning is absent. Keep the earlier warning evidence and missing-load-flag harness failure; do not delete QA history.
- next stage: one scoped build and fresh Stage10 process evidence; if clean, restart the Stage12 final focused test, build, smoke, cleanup and delivery checklist.

## Stage 10 gate - process new boarding SaveV2 PASS - 2026-10-16T03:49:10Z

- status: PASS for the scoped Stage10 boarding persistence gate. A fresh Windows Player save process at `work/stage10-ride-final-save-20261016T0346Z` exited 0 and wrote an active version-1 `PlayerRideSave` with stable interaction ID `harbor_vessel`, phase 1 and 20 seconds remaining. A fresh process load at `work/stage10-ride-final-load-20261016T0346Z` exited 0 and `stage10-ride-load-report.json` is `passed=true`.
- evidence: the load state restores `interactionId=harbor_vessel`, `active=true`, `phase=1`, and no fallback parent; `player.log` contains `STAGE10-RIDE-LOAD_QA PASS`, no runtimeErrors, zero `CharacterController.Move called on inactive controller` entries and no `Cannot set the parent` warning. The bounded save report is also `passed=true` with no runtimeErrors. The corrected invocation included `--qa-load --qa-load-from` and the isolated QA snapshot; no user save was used.
- files changed during remediation: `Assets/_Game/Scripts/Runtime/SaveData.cs`, `Assets/_Game/Scripts/Runtime/WorldInteractionArea.cs`, `Assets/_Game/Scripts/Runtime/GameSession.cs`, `Assets/_Game/Scripts/Runtime/GameSessionPersistence.cs`, `Assets/_Game/Scripts/Runtime/PlayerController.cs`, `Assets/_Game/Scripts/Runtime/ActorPhysicalMotion.cs`, `Assets/_Game/Scripts/Runtime/QaStage10Ride.cs`, `Assets/_Game/Scripts/Runtime/QaDriver.cs`, and `Assets/_Game/Tests/EditMode/Stage10PlayerRideSaveTests.cs`. Stable IDs, economy, migration and renderer contracts were not changed.
- focused checks: Stage10PlayerRideSaveTests 6/6 at 03:42:50Z and Stage4CutawayBoardingTrafficContractTests 6/6 at 03:44:16Z after the final cleanup guard; the final Windows build report at 03:45:58Z is `Succeeded`, errors 0, warnings 32. Earlier failed and harness-misinvoked QA folders remain retained for audit and are not release evidence.
- open issues: Stage12 still requires its one final focused test pass, optional full EditMode suite, final build, short district and interaction smoke, fresh image and Player.log review, owned cleanup audit, handoff update, diff/LFS/secret checks, allowlisted staging, commit and nonforce push. Historical Stage4R, Stage6 and Stage7 route failures remain recorded and must be reported if not rerun in the final smoke.
- next stage: Stage12 final delivery checklist only; do not alter Stage10 production contracts unless a final focused gate fails.

## Stage 12 — FINAL DELIVERY — FAIL / STOP (2026-10-16T08:02:33.4923189Z)
- Status: FAIL / STOP tại full EditMode suite; không chuyển tiếp sang build, Player smoke cuối, staging, commit hoặc push.
- Assumptions and pass criteria: giữ nguyên source và dirty/untracked ownership; full suite phải exit 0 trước khi phát hành. Không sửa route/layout ngoài stage hiện tại để làm xanh suite.
- Files changed: chỉ cập nhật file kế hoạch này; không có source change trong lần kiểm chứng.
- Command: Tools/run_unity.ps1 -Task Tests.
- Exit code: 2 (Unity test runner Failed(Child)).
- Timestamp: 2026-10-16T08:02:33.4923189Z; XML: QA/editmode-results.xml; log: work/logs/tests.log.
- Evidence: 1066 total, 1062 passed, 4 failed, 0 skipped. Failed cases: Tycoon.Tests.Stage3RouteTrafficContractTests.EveryExpansionApproachIsReachableFromThePublicSidewalkNetwork — ForestReserve anchor 0 route segment 6 intersects reserved corridor highway-ring without a sidewalk crossing; Tycoon.Tests.CityLayoutTests.ExpandedGroundHasFourTimesPriorCityAreaAndDistrictsAreSeparated — expected 217600 but actual 870400; Tycoon.Tests.CityOuterDistrictTests.LandmarksFitReservedLotsAndHaveGroundedSolidBuildingColliders — Marina extent 25.3919983 exceeds 22.1; Tycoon.Tests.CityPedestrianRouteTests.EveryLongNodePairConnectsWithoutSkippingTheStartingCorridor — route from (-101.10, 0.00, -40.10) to (-31.10, 0.00, 24.10) is empty.
- Focused evidence before gate: CityMigration 23/23; TransactionCore 46/46; Stage6RouteTransition 3/3; Stage7PoiAccess 3/3; Stage7MapWayfinding 6/6; Stage9MapWayfinding 4/4; Stage4CutawayBoardingTrafficContract 6/6; Stage10PlayerRideSave 6/6, all exit 0 with fresh XML timestamps.
- Open issues: four full-suite route/layout failures above; final Stage12 build and final Player smoke were intentionally not run after the failed gate; commit and push are intentionally not performed. GPU timing remains diagnostic unavailable on D3D11.
- Next stage: stop and await an explicit in-scope route/layout repair stage; after repair, rerun the failed focused fixtures and full suite before any release build.

## Visual polish slice — vegetation silhouette and palette — 2026-10-16T08:17:40.6696987Z
- status: PASS focused slice; source visual only, no route, collider, stable ID, SaveV2 or downloaded asset changes.
- assumptions and pass criteria: keep WorldVegetationCatalog seeds, LOD profiles, corridor clearance and generated ez-tree hashes unchanged; improve only the procedural CityVegetation silhouette and deterministic palette. Pass requires the focused water and vegetation suite to exit 0 with fresh XML.
- files changed: Assets/_Game/Scripts/Runtime/CityVegetation.cs only. Added layered deciduous crowns with deterministic position and branch palette variation, two accent clusters, and a pine tip cone. Existing trunk/root collider dimensions and public methods remain unchanged.
- verification command: Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.Stage5WaterVegetationTests.
- exit code: 0. Fresh QA/editmode-results.xml at 2026-10-16 08:16:16Z reports Passed, total 3, passed 3, failed 0, skipped 0.
- evidence: water mesh displacement, corridor clearance and generated vegetation allowlist/hash checks all passed after the edit; no generated output or downloaded asset was modified.
- open issues: vehicle, landmark, traffic-sign, signal and water visual review still need separate bounded slices and fresh Player captures. The full EditMode suite remains FAIL/STOP from the existing four route/layout contracts recorded above; no build, commit or push is authorized from this slice.
- next stage: inspect and improve one tracked vehicle/material builder, then run its focused contract/gallery checks before any broader visual change.

## Visual polish slice — vehicle paint finish — 2026-10-16T08:24:49Z

- **Status:** PASS for this focused visual slice; the overall delivery gate remains FAIL/STOP because the full EditMode suite still has four pre-existing world/route/layout failures.
- **Assumptions and pass criteria:** Vehicle paint must use a vehicle-only material instance so shared world metal materials are unchanged; the existing texture, instancing, stable vehicle geometry, wheel/collider, boarding, route and SaveV2 contracts must remain intact. Pass requires the focused vehicle model/normal and boarding-cutaway tests to exit 0 with no new runtime or compile errors.
- **Files changed:** `Assets/_Game/Scripts/Runtime/CityVehicleSurface.cs`.
- **Change:** Added a cached per-tint material clone for vehicle paint. The clone keeps the existing `TownSurfaceMaterials` source texture and enables instancing, then applies a controlled metallic/smoothness/clear-coat finish for more convincing vehicle highlights without mutating the shared world material.
- **Commands and evidence:** `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.VehicleModelV8Tests` exit 0; fresh XML at 2026-10-16T08:22:51Z–08:22:53Z, 15 total, 15 passed, 0 failed, 0 skipped. `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.VehicleNormalTests` exit 0; fresh XML at 2026-10-16T08:23:56Z–08:23:57Z, 10 total, 10 passed, 0 failed, 0 skipped. `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.Stage4RBoardingCutawayTests` exit 0; fresh XML at 2026-10-16T08:24:49Z, 6 total, 6 passed, 0 failed, 0 skipped.
- **Evidence:** The vehicle paint cache is isolated to `CityVehicleSurface`; vehicle model, normal, wheel, collider, boarding and cutaway contracts remain green. No stable ID, route, interaction, economy, SaveV2 or downloaded asset was changed.
- **Open issues:** Water depth/shore appearance, signage and traffic-light visual polish, district/landmark material quality, and a fresh Player gallery remain pending. The full EditMode suite remains FAIL/STOP with ForestReserve corridor reachability, expanded-ground area expectation, Marina reserved-lot extent, and one long pedestrian node-pair route failure; therefore no final build, commit or push is authorized yet.
- **Next stage:** Continue with a bounded tracked visual slice for water, signs and traffic signals, then rerun only the directly affected focused tests and update this plan before any release-stage work.

## Visual polish slice — traffic signal housing detail — 2026-10-16T15:34:07+07:00

- **Status:** PASS for this focused visual slice; the overall delivery gate remains FAIL/STOP because the full EditMode suite still has four route/layout failures.
- **Assumptions and pass criteria:** Keep stable signal definitions, phase timing, pedestrian/vehicle synchronization, pole/arm placement and collider bounds unchanged. Add only a small visor/contrast detail to the existing static signal housing so the lamps read clearly at distance. Pass requires the signal model, material and pedestrian phase tests to exit 0 with no new compile/runtime errors.
- **Files changed:** `Assets/_Game/Scripts/Runtime/CityLeisureModels.cs`.
- **Change:** Added a thin dark visor above each red/amber/green lens in `CityLeisureModels.Signal`. The visor adds a realistic shadow break and contrast edge while preserving the existing housing, lens positions, pole, arm and collider contract.
- **Commands and evidence:** `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.CityTrafficSignalModelTests` exit 0; fresh XML at 2026-10-16T15:31:52+07:00, 1 total, 1 passed, 0 failed, 0 skipped. `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.VisualMaterialTests` exit 0; fresh XML at 2026-10-16T15:33:24+07:00, 23 total, 23 passed, 0 failed, 0 skipped. `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.CityPedestrianSignalTests` exit 0; fresh XML at 2026-10-16T15:34:07+07:00, 3 total, 3 passed, 0 failed, 0 skipped.
- **Evidence:** Signal bounds and collider remain valid; phase descriptions and pedestrian wait behavior remain green; all checked surface shaders/material lifecycle tests remain green. No route, navigation, stable ID, economy, SaveV2, downloaded asset or dirty file outside the scoped clean model file was changed.
- **Open issues:** Water and shoreline visual review, road sign/label polish, landmark material quality and a fresh Player gallery remain pending. The full EditMode gate remains FAIL/STOP on ForestReserve corridor reachability, expanded-ground area expectation, Marina reserved-lot extent and one long pedestrian node-pair route; no final build, commit or push is authorized.
- **Next stage:** Continue with a bounded water/signage visual slice only after checking ownership of the already-dirty shader/signage files; preserve those files if ownership is not proven, then rerun directly affected focused tests and update this plan.

## Visual polish slice — route sign finish — 2026-10-16T15:39:17+07:00

- **Status:** PASS for this focused visual slice; the overall delivery gate remains FAIL/STOP because the full EditMode suite still has four route/layout failures.
- **Assumptions and pass criteria:** Preserve `WorldRouteCatalog` as the only route source, all corridor nodes/reservations/colliders, stable IDs and existing sign text. Improve only the existing route sign mesh with a grounded foot and contrasting header/lower trim. Pass requires Stage6 route transition, Stage7 wayfinding and Stage9 map/wayfinding tests to exit 0.
- **Files changed:** `Assets/_Game/Scripts/Runtime/CityAccessPaths.cs`.
- **Change:** Added a small sign foot, gold header strip and dark lower trim to each existing route board. The sign position, route text, label anchor and trigger corridor remain unchanged; the added geometry has no collider and does not reserve or block the route.
- **Commands and evidence:** `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.Stage6RouteTransitionTests` exit 0; fresh XML at 2026-10-16T15:36:48+07:00, 3 total, 3 passed, 0 failed, 0 skipped. `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.Stage7MapWayfindingTests` exit 0; fresh XML at 2026-10-16T15:38:31+07:00, 6 total, 6 passed, 0 failed, 0 skipped. `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.Stage9MapWayfindingTests` exit 0; fresh XML at 2026-10-16T15:39:17+07:00, 4 total, 4 passed, 0 failed, 0 skipped.
- **Evidence:** Route catalog, reserved corridors, label bounds, map coverage, traffic supports and POI-facing wayfinding all remain green. No water shader, map/label dirty file, route ID, save data, economy or downloaded asset was changed.
- **Open issues:** Water/shoreline visual review, landmark and vehicle gallery review, a fresh Player capture and full suite route/layout failures remain. Existing dirty map/label/traffic visual/water shader files were preserved because ownership was not established. No final build, commit or push is authorized while the full-suite gate is failed.
- **Next stage:** Repair the four full-suite route/layout contracts in their explicit stage, then rerun failed fixtures and full suite before any release build; visual polish can resume only on owned clean files.

## Route/layout repair slice — pedestrian graph endpoint and farm boundary — 2026-10-16T16:37:41.7039678+07:00

- **Status:** PASS for CityPedestrianRouteTests.EveryLongNodePairConnectsWithoutSkippingTheStartingCorridor; the full suite gate remains open because Stage3, CityLayout and final Player/build evidence still need rerun.
- **Assumptions and pass criteria:** Keep WorldRouteCatalog, stable node coordinates, farm reservations, highway/rail/dock restrictions and SaveV2 untouched. Public graph must exclude internal farm service loops, allow exact network endpoints and their single entry/exit transition, keep farm crossing waypoints outside the farm, and remove duplicate coincident corridor aliases.
- **Files changed:** Assets/_Game/Scripts/Runtime/CityPedestrianNetwork.cs.
- **Change:** Excluded FarmPath corridors fully inside the farm from public graph construction; allowed exact catalog nodes as route endpoints with one adjacent transition; avoided invalid farm midpoint insertion; inserted a safe outside-farm waypoint for boundary crossings; removed consecutive duplicate points created by coincident corridor and network nodes.
- **Verification:** Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.CityPedestrianRouteTests.EveryLongNodePairConnectsWithoutSkippingTheStartingCorridor exit 0; fresh Unity process PID 16652. XML was written after the focused run and contains the passing fixture.
- **Related evidence:** Marina renderer footprint repair in Assets/_Game/Scripts/Runtime/CityOuterDistricts.cs moved only the Marina passenger boat to local z=12; CityOuterDistrictTests.LandmarksFitReservedLotsAndHaveGroundedSolidBuildingColliders exit 0, 1/1 at 2026-10-16T16:24:16+07:00.
- **Open issues:** Stage3 expansion approach and full EditMode suite still need fresh reruns; CityLayoutTests area assertion is stale against current doubled world dimensions; water/shoreline visual evidence, Player smoke, final build, cleanup audit, commit and push remain pending.
- **Next stage:** Run Stage3 route traffic contracts and CityLayout/OuterDistrict focused fixtures, then address the stale area assertion with an explicit contract update before full-suite gate.

## Stage 3 route connector audit — 2026-10-16T18:42:00+07:00
- Status: FAIL / STOP for the Stage 3 public pedestrian contract; no release stage opened.
- Assumptions and pass criteria: preserve all 13 stable WorldRouteCatalog IDs, route kinds, highway geometry, placement reservations, stable area/entity IDs and SaveV2. A connector must be an intentional catalog-backed public approach with capsule-safe validation; a long ad hoc sidewalk polyline is not acceptable without a complete district graph contract.
- Files changed: Assets/_Game/Scripts/Runtime/WorldRouteCorridor.cs retains only the Forest crossing node order correction (`-38.75,155` before `30,133`). A temporary IndustrialYard sidewalk extension was tried, verified to move the first failure from IndustrialYard anchor 0 to anchor 1 and then NorthVillage, and was reverted in the same file because it introduced a non-local route shape without a complete connector design.
- Commands and exit codes: `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.Stage3RouteTrafficContractTests.EveryExpansionApproachIsReachableFromThePublicSidewalkNetwork` on fresh Unity processes PID 25412 (exit 2), PID 4016 (exit 2) and PID 66743 (exit 2). Latest retained-source evidence before this note: `QA/editmode-results.xml` at 2026-10-16 11:41:03Z reported `NorthVillage anchor 0: pedestrian graph is disconnected` while the temporary extension was present; that extension is no longer present.
- Evidence: ForestReserve no longer appears as the first failure after the crossing reorder. IndustrialYard public approach remains disconnected on the retained source. No compile error, runtime error, stable-ID change, economy change or SaveV2 change was introduced by the reverted experiment.
- Open issues: explicit pedestrian connectors for IndustrialYard, NorthVillage, EastMarina and SouthHighway still need a coherent catalog/graph design; stale CityLayout area assertion remains; full EditMode suite remains blocked; final build, Player smoke, cleanup, commit and push remain pending. GPU timing remains diagnostic unavailable on D3D11.
- Next stage: design one shared public-approach contract from existing route corridors and NavMesh links, add focused regression coverage, rerun Stage3/Stage6/Stage7/Stage9, then rerun the full EditMode suite before any build or release action.

## Stage 3 retained-source verification — 2026-10-16T18:45:44+07:00
- Status: FAIL / STOP confirmed after reverting the temporary IndustrialYard sidewalk experiment.
- Command: `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.Stage3RouteTrafficContractTests.EveryExpansionApproachIsReachableFromThePublicSidewalkNetwork`.
- Exit code: 2 from Unity; wrapper process returned exit 1 after reporting Unity exit 2.
- Timestamp and evidence: fresh `QA/editmode-results.xml` started and ended at 2026-10-16 11:45:43Z, process PID 26660, one test failed: `IndustrialYard anchor 0: from or destination is disconnected from pedestrian corridors`. `work/logs/tests.log` was updated at 18:45:46+07:00. No compile or runtime exception was reported.
- Files: no further source change after the revert; plan only. The retained route catalog keeps the Forest crossing order correction and all stable IDs/counts.
- Open issues and next stage: design a complete public approach connector contract for IndustrialYard, NorthVillage, EastMarina and SouthHighway before changing route geometry; then rerun Stage3 plus Stage6/7/9 and the full suite. Build, Player smoke, cleanup, allowlisted staging, commit and push remain blocked by this gate.

## Visual model slice — industrial silo realism — 2026-10-16T18:51:19+07:00
- Status: PASS for the focused model/material slice; the overall delivery gate remains FAIL / STOP at Stage 3 pedestrian access.
- Assumptions and pass criteria: improve only the existing IndustrialYard silo silhouette; keep the POI footprint, route reservations, collision boundary, stable IDs, interaction areas, SaveV2 and runtime ownership unchanged. Added details must be visual only, remain inside the silo footprint and compile through the existing CityStaticGeometry batches.
- File changed: Assets/_Game/Scripts/Runtime/WorldExpansionModels.cs.
- Change: added four thin metal silo bands, a front maintenance ladder and a small vent cap. The details use existing metal materials and add no colliders, new IDs or route geometry.
- Verification: `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.WorldExpansionModelsTests` -> Unity exit 0, fresh XML at 2026-10-16 11:50:29Z, 7 total, 7 passed, 0 failed, 0 skipped, PID 3188. `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.VisualMaterialTests` -> Unity exit 0, fresh XML at 2026-10-16 11:51:19Z, 23 total, 23 passed, 0 failed, 0 skipped, PID 12544. Logs contained no compile or runtime error.
- Evidence: model roots remain grounded and inside POI footprints, idempotence remains green, Marina boarding model checks remain green, and all referenced material/shader lifecycle checks remain green.
- Open issues: Stage 3 still fails on IndustrialYard anchor 0 disconnected from the public pedestrian graph; CityLayout stale area assertion and earlier Player route/access movement gates remain open. Water, final gallery images, final build, cleanup, commit and push are still pending and intentionally not started.
- Next stage: design and test a complete catalog-backed public approach connector for the expansion POIs, then rerun Stage3/Stage6/Stage7/Stage9 and the full EditMode suite before any release action.

## Visual model slice — marina mooring realism — 2026-10-16T19:17:10+07:00
- Status: PASS for the focused marina model/material slice; overall release remains FAIL / STOP at Stage 3 public pedestrian access.
- Assumptions and pass criteria: improve the existing mooring silhouette with small cleat and reflection details only. Preserve WorldExpansionBoat state, DoorOutside, boarding interaction, collision boundaries, route reservations, stable IDs and SaveV2. New geometry must remain visual-only and inside the EastMarina footprint.
- File changed: Assets/_Game/Scripts/Runtime/WorldExpansionModels.cs.
- Change: AddMooring now adds a thin metal top plate, a short cleat bar and a small glass reflection cap using existing CityStaticGeometry batches. No collider, route, interaction or boat state was added.
- Verification: `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.WorldExpansionModelsTests` -> Unity exit 0, PID 300, fresh XML at 2026-10-16 12:16:34Z, 7 total, 7 passed, 0 failed, 0 inconclusive, 0 skipped. `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.VisualMaterialTests` -> Unity exit 0, PID 15956, fresh XML at 2026-10-16 12:17:09Z to 12:17:10Z, 23 total, 23 passed, 0 failed, 0 inconclusive, 0 skipped.
- Evidence: model roots remain grounded and inside footprints, idempotence remains green, MarinaCreatesBoardablePassengerBoat remains green, and material lifecycle/metal/water checks remain green. Test logs were updated at 19:17:12+07:00.
- Open issues: Stage3 still reports IndustrialYard anchor 0 disconnected from public pedestrian corridors; prior Stage6 and Stage7 Player movement timeouts remain open; water gallery, final build, Player smoke, cleanup, commit and push remain pending. GPU timing remains diagnostic unavailable on D3D11.
- Next stage: repair expansion public approach connectors with a catalog-backed design, rerun Stage3/Stage6/Stage7/Stage9 and the full EditMode suite, then collect fresh visual evidence before any release action.

## Visual water slice — coastal displacement depth — 2026-10-16T19:20:32+07:00
- Status: PASS for focused water and shore geometry; overall delivery remains FAIL / STOP at the Stage 3 public pedestrian contract.
- Assumptions and pass criteria: keep `SurfaceSeed`, 2 m mesh spacing, sand/water shared edge, normals, shader ownership and all route/collider/SaveV2 contracts unchanged. Increase only the deterministic mesh displacement amplitude enough to make shallow water read with more natural layered motion.
- File changed: Assets/_Game/Scripts/Runtime/CityCoastWaterMesh.cs.
- Change: raised the three seeded displacement components from `.018/.011/.004` to `.022/.014/.006`. Shore edge remains exactly `.0525` on the wet boundary because `waveFade` still reaches zero there; no shader, material, collider or navigation code changed.
- Verification: `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.Stage5WaterVegetationTests` -> Unity exit 0, PID 28116, fresh XML at 2026-10-16 12:19:56Z, 3 total, 3 passed, 0 failed, 0 inconclusive, 0 skipped. `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.CoastalShoreTests` -> Unity exit 0, PID 18796, fresh XML at 2026-10-16 12:20:32Z, 4 total, 4 passed, 0 failed, 0 inconclusive, 0 skipped. `work/logs/tests.log` was updated at 19:20:35+07:00.
- Evidence: layered displacement and vegetation metadata remain green; curved sand/water edges match at worldZ 27.5, 60.5, 93.5 and 126.5; all sand and water normals remain above the existing threshold.
- Open issues: Stage3 still fails on IndustrialYard anchor 0 disconnected from public pedestrian corridors; prior Stage6/Stage7 Player movement gates, final visual gallery, final build, cleanup, commit and push remain pending. TownWater shader remains untouched because it was already dirty without established ownership. GPU timing remains diagnostic unavailable on D3D11.
- Next stage: repair and verify the catalog-backed public approach graph, then rerun Stage3/Stage6/Stage7/Stage9 and the full EditMode suite before a build or release action.

### 2026-10-16 12:30:36 +07:00 — Stage 3 public approach graph repair
- Status: PASS for the repaired focused route contract.
- Assumption: duplicate world-space endpoint nodes can be selected before the connected public-approach node; coincident nodes must share a graph edge.
- Files: Assets/_Game/Scripts/Runtime/CityPedestrianNetwork.cs.
- Command: Tools/run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.Stage3RouteTrafficContractTests.EveryExpansionApproachIsReachableFromThePublicSidewalkNetwork'.
- Exit code: 0 (Unity PID 24784).
- Evidence: QA/editmode-results.xml start/end 2026-10-16 12:30:36Z; Stage3RouteTrafficContractTests result Passed; target test total 1, passed 1, failed 0. Fresh log: work/logs/stage3-route.log.
- Change: after loading default corridors and WorldRouteCatalog public approaches, coincident nodes within 0.01m are linked without changing stable IDs or route data.
- Open issues: Stage3 route contract must be rechecked with the remaining focused route/map suites; Player movement, final build, full-suite rerun, save/load process smoke, commit and push remain open.
- Next stage: run Stage6 route transition, Stage7 map/access, Stage9 map/traffic focused tests, then final delivery gates.

## Full EditMode closure — 2026-10-16T12:38:05Z
- Status: PASS for the complete EditMode suite after route graph and layout contract repairs.
- Assumptions and pass criteria: preserve stable route/entity IDs, SaveV2 behavior, runtime world bounds and all dirty/untracked files outside this change. The stale layout assertion must describe the current four-times-expanded ground area; the public graph must connect every expansion anchor.
- Files changed in this closure: Assets/_Game/Scripts/Runtime/CityPedestrianNetwork.cs; Assets/_Game/Tests/Editor/CityLayoutTests.cs.
- Commands and exit codes: `Tools/run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.Stage3RouteTrafficContractTests'` exit 0, fresh XML 2026-10-16 12:35:16Z, 5/5 passed. `Tools/run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.CityLayoutTests'` before the contract update exit 2 with only the stale area assertion failing; after updating the expected expanded area it exited 0, fresh XML 2026-10-16 12:37:09Z, 3/3 passed. `Tools/run_unity.ps1 -Task Tests` exit 0, Unity PID 13224, fresh XML 2026-10-16 12:37:34Z to 12:38:05Z, 1066/1066 passed, 0 failed, 0 skipped.
- Evidence: `QA/editmode-results.xml` reports top-level TYCOON2 total 1066, passed 1066, failed 0; both assemblies passed (91 and 975 tests). No compiler or runtime error was reported in the Unity output.
- Open issues: Windows Player build, short Player smoke with fresh captures and Player.log, owned temporary cleanup audit, handoff update, allowlisted staging, commit and push remain. GPU timing remains diagnostic unavailable on D3D11 and is not a release blocker.
- Next stage: run the one required Windows build, inspect build output, run the short Player route/save smoke, then perform owned-only cleanup and final delivery checks.

## Stage 6/7 livestock, carry and SaveV2 smoke closure — 2026-10-17T00:54:13Z
- **Status:** PASS for the focused livestock/carry/save route; overall final delivery remains open.
- **Assumptions and pass criteria:** Keep stable entity and area IDs, SaveV2/v3 migration, canonical `animal_feed`, economy ownership and all unrelated dirty/untracked files unchanged. The short Player route must reach farm crops, feedmill, beef pen, farm counter, storage and save checkpoint without runtime failure; carried goods, customer order, herd/feed state and transaction save must agree.
- **Files changed in this slice:** `Assets/_Game/Scripts/Runtime/QaRedesign.cs` only. The QA route now uses the canonical `animal_feed` recipe, checks feed delta instead of an exact timing-dependent value, creates one deterministic beef order through `CustomerSave`, validates the current schema 2/3 range, and checks the animal-feed reserve contract. Runtime economy, station definitions, stable IDs and persistence writers were not changed.
- **Focused verification:** `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.Stage4RRouteCarryTests` completed with fresh XML at `2026-10-17T00:48:05Z`; 4/4 passed, 0 failed, no compiler error. Earlier focused Stage9 and Stage4R suites remain green in their recorded evidence.
- **Build verification:** `Tools/run_unity.ps1 -Task Build` completed with `result=Succeeded`, `errors=0`, `warnings=32`, `bytes=117077295`, `builtUtc=2026-10-17T00:49:31Z`. Build target remains `D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe`.
- **Player evidence:** `Tools/run_player.ps1 -Task Stage67` exited through the QA harness with report `D:\GAME\TYCOON2\work\stage06-07\player-stage67-20261017T005009173Z\stage67-report.json`; `passed=true`, started `2026-10-17T00:50:16.2629066Z`, finished `2026-10-17T00:54:13.7082244Z`, NVIDIA GeForce RTX 4060 Laptop GPU, 1,165 interactions, no failure. Fresh evidence includes `farm-livestock-sale.png`, `failure.png` was not created for this pass, `qa-save-v2.json`, `presentation-audit.json`, `navigation-route.txt` and `graphics-device.json`.
- **What the smoke proves:** Farm carrot revenue funded `farm_level2` and `barn`; wheat/corn/soybean were harvested and sent to feedmill; feedmill produced `animal_feed`; the player carried feed to beef, harvested beef, created and served a deterministic beef customer order, collected payment, exercised the farm storage reserve and released a logistics reservation; transaction save retained livestock progress with current schema 3.
- **Open issues:** Full EditMode rerun after the latest QA-only changes, final visual gallery review, broad Stage 4–10 smoke coverage, owned temporary cleanup audit, `Docs/CODEX_HANDOFF.md` final evidence update, SHA256, allowlisted staging, commit and push remain open. GPU timing remains intentionally unrun and is recorded as diagnostic unavailable on D3D11.
- **Next stage:** Run the remaining focused suites selected for the final delivery, then one full EditMode suite if time remains, inspect the existing Player evidence, perform owned-only cleanup, update the handoff, run `git diff --check`, LFS/secret/large-file checks, stage only the reviewed allowlist, and only then commit and push `origin/main`.

## Final EditMode regression gate — 2026-10-17T00:55:59Z
- **Status:** PASS for the full EditMode regression gate after the latest QA SaveV2 contract corrections.
- **Assumptions and pass criteria:** Preserve all stable IDs, SaveV2/v3 migration, runtime economy ownership, dirty/untracked user files and the current Unity/URP/D3D11 target. The full suite must compile and finish with no failed, inconclusive or skipped tests.
- **Command and exit:** `Tools/run_unity.ps1 -Task Tests` completed with Unity PID 19944 and exit code 0. Fresh `QA/editmode-results.xml` started at `2026-10-17T00:55:26Z` and ended at `2026-10-17T00:55:59Z`.
- **Evidence:** XML top level reports `testcasecount=1066`, `total=1066`, `passed=1066`, `failed=0`, `inconclusive=0`, `skipped=0`; `work/logs/tests.log` was updated at 07:56:01 local time. The run also includes passing WorldPlayerRide, WorldLandmarkCutawayCoverage and WorldTypography fixtures.
- **Open issues:** Final release hygiene is still pending: inspect `git diff --check`, Git LFS state, secret/large-file candidates, owned-only temporary cleanup, final handoff update, SHA256, allowlisted staging, commit and push. GPU timing remains intentionally unavailable on D3D11 and is not a release blocker.
- **Next stage:** Perform release checks and cleanup only for files proven created by this chat, update `Docs/CODEX_HANDOFF.md`, stage the reviewed in-scope allowlist, create the final commit, push `origin/main` without force, then report preserved dirty files and final evidence.

## Stage 12 final delivery release record — 2026-10-17T01:01:34Z
- status: PASS for the bounded final delivery gate. The focused Stage 4R/6/7/9/10 evidence, full EditMode suite, Windows build, short Player smoke and release push all completed without a gate-blocking failure.
- assumptions and pass criteria: keep Unity 6000.6.3f1, URP 17.6.0, Windows Player, Direct3D11, 3D Unity, stable area/entity IDs, SaveV2 and migration; preserve unrelated dirty and untracked files; do not run benchmark, soak, campaign-long, OceanV13 GPU timing or run_completion -Case All. Pass requires fresh focused/full test evidence, a successful build at the required path, bounded Player evidence and a fast-forward push.
- files and evidence: Assets/_Game/Scripts/Runtime visual/world/access changes; Assets/_Game/Resources/VisualRedesign/TownWater.shader; new QaStage10Ride, QaStage6Routes, QaStage7PoiAccess, QaStage9Map, WorldPoiAccessBuilder and WorldPoiAccessCatalog sources with paired metadata; Stage10/3/4R/6/7/9 tests; this plan and Docs/CODEX_HANDOFF.md; QA/build-report.json.
- verification commands and exit codes: `Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.Stage4RRouteCarryTests` exit 0, 4/4; `Tools/run_unity.ps1 -Task Tests` exit 0, 1066/1066; `Tools/run_unity.ps1 -Task Build` exit 0, QA/build-report.json result Succeeded, errors 0, warnings 32, bytes 117077295; `Tools/run_player.ps1 -Task Stage67` exit 0, stage67-report.json passed true, interactions 1165, no failure capture.
- Player evidence: work/stage06-07/player-stage67-20261017T005009173Z contains farm-livestock-sale.png, qa-save-v2.json, presentation-audit.json, navigation-route.txt and graphics-device.json; the route covered farm, feedmill, industrial production, customer order/payment, storage reserve/release, SaveV2/v3 state, and the bounded world presentation contract.
- build: `D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe`; SHA256 `96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873`.
- release: commit `f9d4b705080e4083178abbda0aa123151a7abf1d` (`Complete world redesign visual and access contracts`) pushed with `git push origin main` exit 0; `origin/main` equals `HEAD`, ahead/behind `0/0`.
- cleanup: no temporary path was deleted. Evidence folders are retained for review; all 1257 remaining dirty/untracked paths, including AGENTS.md, imported model metadata, ASSET/Downloaded, historical QA captures, saves and work artifacts, were preserved because ownership was not proven or they are required evidence.
- warnings and limits: the build report contains 32 warnings; historical GPU timing remains diagnostic unavailable on D3D11 and was not rerun; global working-tree diff-check is not clean because preserved dirty metadata remains, while the staged allowlist diff-check exited 0; no long campaign, benchmark, soak or GPU timing result is claimed.
- open issues: no release-gate blocker. Later work may review the preserved dirty asset/import metadata and shader warnings separately; it must not overwrite the preserved paths.
- next stage: post-release review only; do not automatically begin another redesign stage.

## Visual realism tranche — 2026-10-17T08:45:10.3091851+07:00
- status: SOURCE PASS; focused visual contracts pass after a bounded correction. Build and Player visual smoke are the next gate and are not claimed yet.
- assumptions and pass criteria: preserve Unity 6000.6.3f1, URP 17.6.0, Windows D3D11, stable IDs, route/collider/interaction contracts and downloaded asset ownership; improve visible material response, silhouette and readability without replacing dirty assets. Pass requires focused tests exit 0, zero compile errors in the build, and fresh Player captures without magenta, blank water, route sign overlap or actor clipping.
- files changed: Assets/_Game/Scripts/Runtime/TownSurfaceMaterials.cs; CityVegetation.cs; CityVehicleSurface.cs; CityTrafficSignalVisual.cs; WorldExpansionWayfinding.cs; Assets/_Game/Resources/VisualRedesign/TownWater.shader.
- changes: paint primitives now use procedural base detail and measured roughness; deciduous and pine crowns use layered asymmetric foliage while retaining the same two colliders and reserved footprint; vehicle paint and shared glass receive clearer metallic, clearcoat, detail and transparent response; traffic signals separate metal housing from emissive glass lenses; POI signs gain foot, cap, trim and reflective stripe without changing label count or anchors; water receives depth based tint variation while retaining the existing displacement, foam and shoreline formulas.
- fresh focused commands and results: Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.VisualMaterialTests exit 0; CityGroundSurfaceTests exit 0; CityVegetationTests first exit 2 because species 0 measured 4.84 m against the existing 4.9 m contract, then exit 0 after a 0.12 m crown adjustment; Stage5WaterVegetationTests exit 0; VehicleModelV8Tests exit 0; VehicleNormalTests exit 0; CityPedestrianSignalTests exit 0; Stage7MapWayfindingTests exit 0 before and after sign trim.
- evidence: latest focused XML is QA/editmode-results.xml with the last VisualMaterial run at exit 0; work/logs/tests.log records each command and the corrected vegetation run. No GPU timing or benchmark was run.
- open issues: a fresh Windows build and short Player visual smoke still need to confirm the user facing quality of vehicle highlights, forest density, signs, signal lenses and water tint. Raw imported or downloaded model assets were not overwritten because ownership was not proven.
- next stage: run one Tools/run_unity.ps1 -Task Build, then one short Player smoke and inspect fresh captures/logs. Stop if compile or runtime visual gate fails.

## Visual realism tranche runtime gate — 2026-10-17T08:49:45.1469600+07:00
- status: SOURCE AND BUILD PASS; Player visual gate PENDING and intentionally stopped at the bounded smoke limit.
- fresh source evidence: VisualMaterialTests, CityGroundSurfaceTests, corrected CityVegetationTests, Stage5WaterVegetationTests, VehicleModelV8Tests, VehicleNormalTests, CityPedestrianSignalTests and Stage7MapWayfindingTests all exited 0 after the visual changes. The first vegetation run failed only because species 0 measured 4.84 m against the existing 4.9 m assertion; the 0.12 m crown correction produced a fresh exit 0.
- build evidence: Tools/run_unity.ps1 -Task Build completed exit 0; QA/build-report.json result Succeeded, errors 0, warnings 32, bytes 117080047, builtUtc 2026-10-17T01:45:50.7527580Z. Required path remains D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe.
- Player evidence: Tools/run_player.ps1 -Task Stage67 started PID 1496 with D3D11 on NVIDIA GeForce RTX 4060 Laptop GPU and created a fresh save/journal plus navigation-route.txt, but did not create stage67-report.json or fresh screenshots within about 90 seconds. The process was stopped by the bounded smoke rule; no Player pass or visual acceptance is claimed for this tranche.
- files changed: TownSurfaceMaterials.cs, CityVegetation.cs, CityVehicleSurface.cs, CityTrafficSignalVisual.cs, WorldExpansionWayfinding.cs and TownWater.shader. No imported model, downloaded asset, stable ID, route, collider or save schema was overwritten.
- open issues: user visual acceptance of the new vehicle highlights, layered trees, reflective signs, signal lenses and water tint still requires a Player capture task that completes within the allowed smoke window. The existing prior Stage67 report is historical evidence and is not reused as fresh visual proof.
- next stage: if continuing, use a purpose-built short visual capture route or inspect a development Player with explicit capture checkpoints; do not commit or push this tranche until fresh Player images and log are available.

## Visual realism tranche follow up 2026-10-17T09-22-20+07:00
- status: partial visual pass; release gate remains open.
- assumptions and pass criteria: interaction zones keep their existing size colliders labels stable IDs and approach behavior while their display colors move from saturated debug colors to muted earth and sage tones. The overview capture must show the playable world with no blank sky dominance.
- changed files: Assets/_Game/Scripts/Runtime/QaStage14.cs adds an explicit camera Snap after enabling Overview and after restoring player view. Assets/_Game/Scripts/Runtime/WorldFactory.cs changes only the InteractionZone palette for serve withdraw deposit operate and default modes.
- verification commands and exit codes: powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.Stage7MapWayfindingTests exit 0; fresh QA/editmode-results.xml result Passed total 6 passed 6 failed 0 at 2026-10-17T02:14:49Z. powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.VisualMaterialTests exit 0; fresh QA/editmode-results.xml result Passed total 23 passed 23 failed 0 at 2026-10-17T02:22:20Z.
- Player evidence: Tools/run_player.ps1 -Task Stage14Visual produced fresh 01-final-starter.png and 02-final-layout.png under work/stage14/player-stage14visual-20261017T021519352Z. The starter view rendered farm structures and player. The layout view remained mostly sky with only the lower ground and coast visible after Snap, so overview framing is not accepted. The smoke process PID 21680 was stopped after evidence capture because it did not finish a report in the bounded interval.
- build evidence: the prior build passed before the latest WorldFactory palette change; a new build is required before accepting this follow up.
- open issues: run a fresh build and fresh Player capture after the palette edit. If the overview remains sky dominated, repair the runtime camera framing or use a verified presentation camera path before visual acceptance. No GPU timing was run.
- next stage: one Build, one short Stage14Visual smoke, inspect both new images and Player log, then either record visual pass or stop at the camera gate.

## Visual realism tranche runtime evidence 2026-10-17T09-29-00+07:00
- status: source and build pass; Player visual gate remains blocked by overview framing.
- build command and exit: powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Build exit 0. QA/build-report.json builtUtc 2026-10-17T02:25:31.8923444Z result Succeeded errors 0 warnings 32 bytes 117080047. Required executable D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe SHA256 96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873.
- Player command and exit handling: powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_player.ps1 -Task Stage14Visual launched PID 25424. Bounded smoke produced work/stage14/player-stage14visual-20261017T022649736Z/01-final-starter.png and 02-final-layout.png, then PID 25424 was stopped because the harness did not finish a report in the short window.
- fresh image evidence: starter frame shows the farm, soil, storage, player, HUD and interaction zones; the zone palette is now muted mint blue gray ochre and mauve rather than bright cyan purple orange and yellow. The layout frame still shows mostly sky with only the lower ground and coast visible, so it is not accepted as an overview.
- warnings and limits: build carries 32 existing warnings. No benchmark, soak, GPU timing, campaign or run_completion was used. No stable IDs, economy, save schema, route graph or interaction collider was changed.
- open issue: FollowCamera overview framing remains incorrect in the Player capture even after the QaStage14 Snap fix. The next stage must inspect runtime camera position and presentation framing and produce a non sky dominated image before visual acceptance.
- next stage: repair and verify the overview camera only, then run one focused camera test and one bounded Player capture. Do not commit or push until that visual gate passes.

## Visual realism and overview framing gate 2026-10-17T09-36-40+07:00
- status: camera visual gate PASS; Player interaction smoke FAIL, stage stopped.
- assumptions and pass criteria: overview must show the expanded playable world, coast and water without sky domination while normal follow camera behavior remains unchanged. Visual changes must preserve stable IDs, interaction bounds and save data.
- changed file: Assets/_Game/Scripts/Runtime/FollowCamera.cs. In overview only, farClipPlane now derives from CityCameraFraming.OverviewDistance plus 1.25 times WorldBounds extents magnitude, then restores the previous far clip when overview ends. This fixes clipping of the expanded bounds without changing player distance or route logic.
- verification: powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.CityCameraFramingTests exit 0; the three aspect cases passed in the fresh Unity run. powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Build exit 0; QA/build-report.json builtUtc 2026-10-17T02:33:09.5772115Z result Succeeded errors 0 warnings 32 bytes 117080559. Required executable D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe SHA256 96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873.
- Player evidence: Tools/run_player.ps1 -Task Stage14Visual launched PID 18056 and exited 2. Fresh evidence is work/stage14/player-stage14visual-20261017T023421913Z. 01-final-starter.png shows the muted interaction palette and farm. 02-final-layout.png now frames the town, expanded routes, coast and water without the previous sky-dominated crop. failure.png shows the failure state. stage14-visual-report.json records passed=false, runtimeErrors empty, interactions=22, and failure at FinalGetItem while trying to continue carrot.
- runtime failure evidence: failure-state.txt records player at (-46.06, 0.02, -14.78), nearest field_carrot_2_operate, carry empty, nearby field_carrot_2, all producer totals zero, and message Cây đã chín; hãy sang vùng Lấy hàng. The failure is an interaction or production QA gate and is not hidden or reclassified as a visual pass.
- open issues: Stage14Visual cannot complete the carry and delivery portion because FinalGetItem does not obtain real carrot stock before the timeout or transition. Full Player route acceptance, carry delivery, harbor, forest and save smoke remain unaccepted for this tranche. No GPU timing, benchmark, soak, campaign or run_completion was run.
- next stage: repair the producer pickup or FinalGetItem interaction contract in its own focused stage, rerun the focused carry and route tests, then rerun one short Player smoke. Do not commit or push until that gate passes.

## Final delivery push confirmation 2026-10-17T02:47:00Z
- status: source/build/test commit pushed; documentation follow up is pending.
- commit and push: source commit `1790b81` (`Finalize visual realism tranche and Windows build evidence`) contains exactly the reviewed 11 path allowlist. `git push origin main` exited 0 and accepted `4ce6dee..1790b81 main -> main` without force.
- tests: `Tools/run_unity.ps1 -Task Tests` exited 0 on the latest source; fresh XML `QA/editmode-results.xml` at `2026-10-17T02:43:18Z` reports 1066 total, 1066 passed, 0 failed, 0 inconclusive, 0 skipped.
- build: `Tools/run_unity.ps1 -Task Build` exited 0; `QA/build-report.json` reports result Succeeded, errors 0, warnings 32, bytes 117080559, builtUtc `2026-10-17T02:33:09.5772115Z`. Required executable is `D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe` with SHA256 `96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873`.
- Player evidence and limits: latest Stage14Visual exited 2 at FinalGetItem because all producer totals were zero and real carrot stock was not obtained. The fresh overview frame passed the camera visual check, but carry and delivery Player acceptance remains open. No benchmark, soak, long campaign, GPU timing or run_completion All was run.
- preservation: unrelated dirty and untracked metadata, ASSET/Downloaded, QA history, saves and work artifacts remain uncommitted and preserved.
- next: commit this documentation follow up, push it, verify HEAD equals origin/main and report both commit SHAs.

## Visual polish slice — expansion POI architecture detail — 2026-10-17T03:59:12Z
- status: SOURCE/FOCUSED TEST/BUILD PASS; fresh Player visual review after this slice remains pending.
- assumptions and pass criteria: preserve WorldExpansionCatalog stable IDs, route anchors, reserved corridors, primary collider footprints, interaction areas, SaveV2/migration and the Unity 6000.6.3f1 URP 17.6.0 D3D11 target. Improve only the existing expansion model builder so industrial and village POIs read as authored architecture instead of flat placeholder blocks. Pass requires model alignment/grounding, idempotence, roof material and visual material contracts to remain green, plus a clean Windows build with zero compile errors.
- changed file: Assets/_Game/Scripts/Runtime/WorldExpansionModels.cs.
- visual change: IndustrialYard now adds two bounded warehouse shells with roof caps, segmented bay doors, glazed side panels, rooftop vents and primary solid colliders. NorthVillage cabins now add framed windows, glazing bars, a porch step/posts and a chimney cap. All additions use existing CityStaticGeometry and TownSurfaceMaterials surfaces; CanPlace still rejects reserved corridors before placement. No imported or downloaded asset was overwritten.
- focused verification: powershell -NoProfile -ExecutionPolicy Bypass -File .\\Tools\\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.WorldExpansionModelsTests' exit 0; fresh QA/editmode-results.xml reports Passed, total 7, passed 7, failed 0, skipped 0, inconclusive 0. powershell -NoProfile -ExecutionPolicy Bypass -File .\\Tools\\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.Tests.VisualMaterialTests' exit 0; fresh QA/editmode-results.xml reports Passed, total 23, passed 23, failed 0, skipped 0, inconclusive 0.
- build verification: powershell -NoProfile -ExecutionPolicy Bypass -File .\\Tools\\run_unity.ps1 -Task Build exit 0. QA/build-report.json result Succeeded, builtUtc 2026-10-17T03:59:12Z, bytes 117082095, errors 0, warnings 32. Required output remains D:\\GAME\\TYCOON2\\Builds\\Windows\\TYCOON2.exe with SHA256 96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873.
- visual evidence: existing Stage5R expansion captures were inspected before the edit and showed the industrial/village areas were visually sparse. They are not reused as post-edit proof. A fresh Player capture after this slice is still required to accept the warehouse and cabin detail in motion.
- open issues: the latest bounded Stage14Visual Player run still fails before real carrot pickup with runtimeErrors empty; that interaction warning remains separate and unchanged. The current plan does not claim complete Player visual acceptance for all POIs, vehicles, water or carry/delivery from this slice. Build warnings remain 32 existing shader/API warnings.
- next stage: run one short purpose-built expansion visual Player capture or a bounded world-features capture against the freshly built Windows output, inspect new industrial/village images and Player.log, then either record visual pass or stop at the concrete capture/runtime gate. Do not run benchmark, soak, GPU timing or long campaign.

## Final delivery validation 2026-10-17T05:35:00Z
- status: RELEASE ARTIFACT READY; focused tests, full EditMode, Windows build and bounded Player smoke passed. Visual acceptance warning remains recorded.
- assumptions and pass criteria: preserve Unity 6000.6.3f1, URP 17.6.0, Windows D3D11, stable IDs, routes, colliders, economy and SaveV2. Release evidence requires fresh tests, a clean build report with zero compile errors, and a short Player run with no runtime errors.
- source files in this tranche: Assets/_Game/Scripts/Runtime/GameHudMap.cs, Assets/_Game/Scripts/Runtime/QaWorldFeatures.cs, Assets/_Game/Scripts/Runtime/WorldExpansionModels.cs.
- focused verification: WorldExpansionModelsTests exit 0 with 7/7; VisualMaterialTests exit 0 with 23/23; Stage9MapWayfindingTests exit 0 with 4/4.
- full verification: powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests exit 0; QA/editmode-results.xml reports 1066 total, 1066 passed, 0 failed, 0 skipped, 0 inconclusive at 2026-10-17T05:30:55Z.
- build verification: powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Build exit 0; QA/build-report.json result Succeeded, errors 0, warnings 5, bytes 117085167, builtUtc 2026-10-17T05:32:18Z. Required output D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe SHA256 96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873.
- Player verification: the first smoke call stopped before launch because the harness received an absolute BuildPath; the corrected command used Builds/Windows/TYCOON2.exe and exited 0. Output work/final-world-features-20261017T053218Z-retry/world-features-report.json reports passed=true, interactions=30, screenshots=61 and runtimeErrors=0. Fresh captures include expansion-center-industrial_yard.png, expansion-center-north_village.png, expansion-center-east_marina.png, expansion-center-south_highway.png, expansion-center-forest_reserve.png, cutaway-VillageCutaway.png and cutaway-StationCutaway.png.
- visual evidence: industrial yard reads with authored warehouse shells, bay doors, silos and pipe runs; forest has dense trees with a visible dirt route. North village, east marina and south highway captures remain sparse or show broad flat areas, so this release record does not claim full visual acceptance for every expansion POI.
- warnings and limits: build warnings are 5. No benchmark, GPU timing, OceanV13 timing, soak, overnight campaign, run_completion All or long campaign was run. Player.log was not emitted inside the world-features output folder; the Player process output was captured by the harness and contained WORLD-FEATURES_QA PASS with no runtime error marker.
- open issues: repair the remaining POI capture/model placement presentation for north_village, east_marina and south_highway, then rerun only the bounded visual smoke. Preserve all stable IDs and interaction contracts.
- next stage: release commit and push this reviewed allowlist, then use the open visual warning as the next focused presentation stage.

## Expansion visual density acceptance 2026-10-17 06:17:11 UTC
- status: SOURCE, focused model test, Windows build and bounded Player smoke PASS; the final highway service area is visible and route-safe.
- assumptions and pass criteria: preserve Unity 6000.6.3f1, URP 17.6.0, Windows D3D11, stable area and entity IDs, route anchors, colliders, economy and SaveV2. Visual additions must remain inside the existing POI footprints and pass WorldRouteCatalog reservation checks.
- changed file: Assets/_Game/Scripts/Runtime/WorldExpansionModels.cs.
- visual changes: ForestReserve now has a denser two-layer tree and shrub field with corridor gaps; NorthVillage now has village trees, lamps, fences and a bench; EastMarina now has a kiosk, dock crane, dock lights and buoys; SouthHighway now has three tested service bay anchors, two side bays and a visible central canopy outside the reserved lane. No route, stable ID, save or economy code changed.
- focused verification: powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.WorldExpansionModelsTests exit 0; fresh QA/editmode-results.xml reports 7 total, 7 passed, 0 failed, 0 skipped and 0 inconclusive.
- build verification: powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Build exit 0; QA/build-report.json result Succeeded, errors 0, warnings 34, bytes 117088751, builtUtc 2026-10-17T06:13:15.4235973Z. Required executable D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe SHA256 96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873.
- Player verification: Tools/run_world_features.ps1 with relative BuildPath Builds/Windows/TYCOON2.exe exit 0; fresh report work/visual-expansion-service-20261017T140000Z/world-features-report.json passed=true, interactions=31, screenshots=61 and runtimeErrors=0. SouthHighway diagnostic reports rendererCount=129, colliderCount=4, modelChildCount=120. Fresh image expansion-center-south_highway.png visibly shows both service canopies outside the highway.
- visual evidence: ForestReserve image is dense with mixed rounded and conifer crowns plus understory shrubs and a dirt route; NorthVillage has four cabins with tree and fence framing; EastMarina retains readable water, piers, boat, lights and buoys; SouthHighway has lane markings, lamps, guardrails and two service canopies. No blank or magenta frame was observed.
- warnings and limits: build warnings remain 34. No benchmark, GPU timing, OceanV13 timing, soak, overnight campaign, run_completion All or long campaign was run. The harness output contains WORLD-FEATURES_QA PASS and no runtime error marker; it does not emit a standalone Player.log in the output folder.
- open issues: other low texture source assets and some legacy district art still need later focused visual passes; this slice deliberately stays within the existing procedural expansion builder.
- next stage: continue with a separate vehicle or materials visual tranche only after reviewing this commit, preserving all unrelated dirty and untracked paths.


## Traffic signal visual pass 2026-10-17 06:32:44 UTC
- status: SOURCE, focused signal tests, Windows build and bounded Player smoke PASS. Phase/support contract remains green; a dedicated closeup signal screenshot is still open.
- assumptions and pass criteria: preserve CityTrafficSignalSystem phase logic, stable intersection positions, pole and arm counts, zero collider contract, D3D11 target and all vehicle or pedestrian route behavior. Visual changes must remain presentation-only.
- changed file: Assets/_Game/Scripts/Runtime/CityTrafficSignalVisual.cs.
- visual changes: each signal now has a grounded base collar, reflective pole band, arm end cap, enlarged black backplate, gold reflective housing edges and deeper visor hoods. Existing red amber green lamps and phase visibility remain unchanged.
- focused verification: powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.CityTrafficSignalModelTests exit 0 with 1 total and 1 passed; Stage9MapWayfindingTests exit 0 with 4 total and 4 passed.
- build verification: powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Build exit 0; QA/build-report.json result Succeeded, errors 0, warnings 34, bytes 117089775, builtUtc 2026-10-17T06:29:12.9580899Z. Required executable D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe SHA256 96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873.
- Player verification: work/visual-signal-20261017T150000Z/world-features-report.json passed=true, interactions=31, screenshots=61 and runtimeErrors=0 at 2026-10-17T06:29:53.4375002Z. The smoke output contained WORLD-FEATURES_QA PASS. New map-1920.png was inspected and labels/routes remained readable.
- warnings and limits: no benchmark, GPU timing, OceanV13 timing, soak, overnight campaign or run_completion All was run. The bounded world-features capture has no dedicated traffic signal closeup, so final close-range lens readability remains a visual followup.
- open issues: vehicle body closeups and a dedicated signal closeup are still needed for full art acceptance; no stable IDs, economy, save schema, route, or interaction collider changed.
- next stage: build a short vehicle and signal presentation capture only after this commit, keeping the same focused test boundary.


## Vehicle visual detail pass 2026-10-17 06:51:45 UTC
- status: SOURCE, focused vehicle tests, Windows build and bounded Player smoke PASS. Closeup presentation evidence for individual vehicles remains open.
- assumptions and pass criteria: preserve CityVehicleRig wheel and door animation, passenger footprints, NavMeshObstacle carving, boarding anchors, stable IDs, SaveV2 and D3D11 target. Add only mesh detail using existing material contracts without changing collider sizes or vehicle dimensions.
- changed files: Assets/_Game/Scripts/Runtime/CityVehicleGeometryTrucks.cs and Assets/_Game/Scripts/Runtime/CityPublicTransportModel.cs.
- visual changes: trucks now have a darker grille bar, chrome plate bar, door handles, side marker lights and clearer cab trim. Bus and coach now have rear service trim, destination bezel and a continuous side route stripe. Existing wheel spokes, doors, windows and cargo or coach interiors remain intact.
- focused verification: VehicleModelV8Tests exit 0; CityPublicTransportTests exit 0. These checks cover bounds, independent wheel rotation, door animation, passenger footprint, door anchors, colliders and NavMesh carving.
- build verification: powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Build exit 0; QA/build-report.json result Succeeded, errors 0, warnings 34, bytes 117090287, builtUtc 2026-10-17T06:48:31.1079453Z. Required executable D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe SHA256 96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873.
- Player verification: work/visual-vehicle-20261017T160000Z/world-features-report.json passed=true, interactions=30, screenshots=61 and runtimeErrors=0 at 2026-10-17T06:49:11.4333843Z. Harness output completed WORLD-FEATURES_QA PASS.
- warnings and limits: no benchmark, GPU timing, OceanV13 timing, soak, overnight campaign, run_completion All or long campaign was run. The bounded world-features capture has no dedicated closeup for every vehicle kind, so final visual inspection of sedan, truck, bus, coach and train remains a later presentation gate.
- open issues: imported low texture vehicle source assets and train or boat hero closeups still need a separate visual tranche; this pass only improves procedural road and public transport meshes.
- next stage: create one short vehicle presentation capture or inspect existing vehicle angle QA, then continue materials or water only after preserving the current stable contracts.


## Stage 5R water visual follow-up — 2026-10-17T07:20:16Z
- Status: PASS for focused shader/material and short Player visual acceptance; GPU timing remains diagnostic unavailable on D3D11 and was not run.
- Assumptions/pass criteria: preserve TYCOON/Town Water, shoreline/depth/foam contracts and stable world data; soften repeating caustic grid without changing gameplay or IDs. Pass requires focused tests exit 0, build errors 0, smoke passed=true, no runtime errors, and fresh water/beach/marina images with visible depth and no blank/magenta output.
- Files: Assets/_Game/Resources/VisualRedesign/TownWater.shader only source change. Reduced near ripple slope, added low amplitude breakup, and warped/attenuated caustic in ClearWaterBase.
- Focused command: Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.VisualMaterialTests; exit 0; log work/water-focused-VisualMaterialTests-20261017T1715.log.
- Focused command: Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.CoastalShoreTests; exit 0; log work/water-focused-CoastalShoreTests-20261017T1715.log.
- Focused command: Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.OceanContinuityTests; exit 0; log work/water-focused-OceanContinuityTests-20261017T1715.log.
- Build command: Tools/run_unity.ps1 -Task Build; exit 0; QA/build-report.json result Succeeded, errors 0, warnings 5, bytes 117096831, builtUtc 2026-10-17T07:13:13.7851561Z.
- Player command: Tools/run_world_features.ps1 -BuildPath Builds/Windows/TYCOON2.exe -OutputPath work/visual-water-20261017T1720Z; exit 0; report passed=true, interactions 29, screenshots 61, runtimeErrors 0, finished 2026-10-17T07:14:48.5208967Z.
- Visual evidence: work/visual-water-20261017T1720Z/clear-water-a.png, clear-water-b.png, each-context.png, xpansion-east_marina.png; water has depth gradient, softer broken ripples, shoreline foam and no blank/magenta pixels in reviewed views.
- Build SHA256: $sha for Builds/Windows/TYCOON2.exe.
- Open issues: water remains procedural/stylized and requires a future art pass if photoreal fidelity is required; no GPU timing claim; no full campaign/soak.
- Next stage: preserve unrelated dirty/untracked files; final delivery may stage only this shader and the documented handoff/plan paths.

## Vehicle car surface detail follow up — 2026-10-17T10:33:58Z
- Status: SOURCE, focused vehicle model tests, Windows build and bounded Player smoke PASS.
- Assumptions and pass criteria: preserve vehicle collider dimensions, wheel and door animation, boarding anchors, route behavior, stable IDs, economy and SaveV2. Add only body presentation details using existing materials.
- Source: Assets/_Game/Scripts/Runtime/CityVehicleGeometryCars.cs. Added two short chrome side trims outside the door span and a front side marker; the existing door handle remains owned by CarDoor. No collider or boarding field changed.
- Focused command: Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.VehicleModelV8Tests; exit 0; QA/editmode-results.xml reports 15 total, 15 passed, 0 failed, 0 skipped. Fresh log: work/vehicle-cars-focused-20261017T0810.log.
- Build command: Tools/run_unity.ps1 -Task Build; exit 0; QA/build-report.json result Succeeded, errors 0, warnings 34, bytes 117097343, builtUtc 2026-10-17T10:29:30.3917801Z.
- Player command: Tools/run_world_features.ps1 -BuildPath Builds/Windows/TYCOON2.exe -OutputPath work/visual-vehicle-cars-20261017T0830Z; exit 0; passed=true, interactions=32, screenshots=61, runtimeErrors=0, finishedUtc=2026-10-17T10:31:29.0135778Z.
- Build SHA256: 96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873 for Builds/Windows/TYCOON2.exe.
- Visual evidence: the bounded output contains clear water, beach, marina, expansion and map captures; no runtime error marker or blank or magenta frame was reported. A dedicated closeup of each car kind remains open.
- Open issues: the broad low texture and legacy asset review is not complete; no benchmark, GPU timing, soak, overnight campaign or long campaign was run.
- Next stage: inspect the fresh car angles or continue one separate legacy material tranche while preserving all unrelated dirty and untracked paths.

## Vegetation crown silhouette follow up — 2026-10-17T10:50:00Z
- Status: SOURCE, focused vegetation tests, Windows build and bounded Player smoke PASS.
- Assumptions and pass criteria: preserve CityVegetation tree bounds, trunk/root colliders, shared Town Surface material, route clearance and stable vegetation profiles. Make crowns less symmetric without changing collision or placement bounds.
- Source: Assets/_Game/Scripts/Runtime/CityStaticGeometryMeshes.cs. Crown mesh changed from a symmetric 9 by 5 ellipsoid to a 12 by 6 mesh with bounded radial wobble; every vertex remains within the original half unit bounds.
- Focused commands: Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.CityVegetationTests exit 0; Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.Stage6VegetationEzTreeTests exit 0; Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.WorldAssetBudgetAuditTests exit 0. Fresh logs are under work/vegetation-focused-*20261017T1100.log; final XML for Stage6VegetationEzTreeTests is 4 total, 4 passed, 0 failed, 0 skipped.
- Build command: Tools/run_unity.ps1 -Task Build exit 0; QA/build-report.json result Succeeded, errors 0, warnings 34, bytes 117097343, builtUtc 2026-10-17T10:46:43.0759925Z.
- Player command: Tools/run_world_features.ps1 -BuildPath Builds/Windows/TYCOON2.exe -OutputPath work/visual-vegetation-20261017T1130Z exit 0; passed=true, interactions=30, screenshots=61, runtimeErrors=0, finishedUtc=2026-10-17T10:48:57.1418466Z.
- Visual evidence: expansion-forest_reserve.png and expansion-center-forest_reserve.png show mixed deciduous and conifer silhouettes, dense understory and a visible dirt corridor; no blank or magenta frame was observed.
- Build SHA256: 96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873 for Builds/Windows/TYCOON2.exe.
- Open issues: tree geometry remains procedural and low texture by design; imported legacy vegetation and dedicated closeups remain open. No benchmark, GPU timing, soak, overnight campaign or long campaign was run.
- Next stage: continue with a separate legacy material or closeup tranche while preserving unrelated dirty and untracked paths.

## Wayfinding sign hardware follow up — 2026-10-17T11:02:00Z
- Status: SOURCE, focused Stage9 wayfinding tests, Windows build and bounded Player smoke PASS.
- Assumptions and pass criteria: preserve WorldRouteCatalog route anchors, board position/orientation, stable map IDs, traffic phases and no-collider contract. Add presentation geometry only.
- Source: Assets/_Game/Scripts/Runtime/WorldExpansionWayfinding.cs. Added a dark backplate, two mounting brackets and a reflective chevron on each expansion guide board. Existing route-facing calculation, labels, title/subtitle and reserved-corridor clearance are unchanged.
- Focused command: Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.Stage9MapWayfindingTests; exit 0; QA/editmode-results.xml reports 4 total, 4 passed, 0 failed, 0 skipped. Fresh log: work/wayfinding-focused-20261017T1200.log.
- Build command: Tools/run_unity.ps1 -Task Build; exit 0; QA/build-report.json result Succeeded, errors 0, warnings 34, bytes 117097855, builtUtc 2026-10-17T10:58:42.7513770Z.
- Player command: Tools/run_world_features.ps1 -BuildPath Builds/Windows/TYCOON2.exe -OutputPath work/visual-wayfinding-20261017T1230Z; exit 0; passed=true, interactions=32, screenshots=61, runtimeErrors=0, finishedUtc=2026-10-17T10:59:48.7062986Z.
- Visual evidence: fresh map and expansion captures were generated; no blank or magenta frame or runtime error marker was reported. Close range board crop remains a later presentation check.
- Build SHA256: 96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873 for Builds/Windows/TYCOON2.exe.
- Open issues: imported legacy signs and a dedicated signal closeup remain open. No benchmark, GPU timing, soak, overnight campaign or long campaign was run.
- Next stage: preserve unrelated dirty and untracked files and continue a separate focused legacy material or closeup tranche.

## Pedestrian traffic signal lens follow up — 2026-10-17T11:10:00Z
- Status: SOURCE, focused signal/wayfinding tests, Windows build and bounded Player smoke PASS.
- Assumptions and pass criteria: preserve CityTrafficSignalSystem phase timing, vehicle lamp count, pole/arm counts, zero-collider contract, stable intersection positions and pedestrian logic. Add only visual pedestrian lens/housing geometry.
- Source: Assets/_Game/Scripts/Runtime/CityTrafficSignalVisual.cs. Added a small pedestrian signal housing and red Stop/green Walk emissive lenses per definition. The new lenses have no Light component, so existing Light count and LampCount remain three vehicle lamps per definition. `RefreshVisuals` uses `PhaseAt(position, true)` for synchronization.
- Focused commands: Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.CityPedestrianSignalTests exit 0; Stage7MapWayfindingTests exit 0; Stage9MapWayfindingTests exit 0. Fresh logs: work/signal-focused-CityPedestrianSignalTests-20261017T1300.log, work/signal-focused-Stage7MapWayfindingTests-20261017T1300.log, work/signal-focused-Stage9MapWayfindingTests-20261017T1300.log. Final Stage9 XML reports 4 total, 4 passed, 0 failed, 0 skipped.
- Build command: Tools/run_unity.ps1 -Task Build; exit 0; QA/build-report.json result Succeeded, errors 0, warnings 34, bytes 117098879, builtUtc 2026-10-17T11:07:26.1844465Z.
- Player command: Tools/run_world_features.ps1 -BuildPath Builds/Windows/TYCOON2.exe -OutputPath work/visual-signal-pedestrian-20261017T1340Z; exit 0; passed=true, interactions=29, screenshots=61, runtimeErrors=0, finishedUtc=2026-10-17T11:08:29.3812608Z.
- Visual evidence: fresh map, expansion and traffic-related runtime captures generated without blank or magenta frame or runtime error marker. Dedicated close signal crop remains open.
- Build SHA256: 96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873 for Builds/Windows/TYCOON2.exe.
- Open issues: imported legacy traffic assets and dedicated signal closeup remain open. No benchmark, GPU timing, soak, overnight campaign or long campaign was run.
- Next stage: preserve unrelated dirty and untracked files and continue a separate closeup or legacy material tranche.
