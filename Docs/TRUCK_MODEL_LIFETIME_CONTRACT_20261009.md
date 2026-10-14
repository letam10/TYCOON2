# Truck model lifetime repair

Workspace: D:/GAME/TYCOON2. Unity 6000.6.3f1, C#, existing transaction-authoritative logistics.

## Ownership

- Agent may edit TruckLogistics.cs and create TruckModelLifetimeTests.cs plus its Unity meta file.
- Root owns all passenger, QA, build, and delivery files. Never edit them or spawn another agent.
- Preserve unrelated dirty files and all transaction/data signatures.

## Invariants

- Dictionary `visuals` maps crate ID to a disposable GameObject presentation.
- A transaction-owned crate must rebuild its visual when a cached Unity object has been destroyed.
- Destroying a presentation must never create, transfer, or delete a transaction-owned crate/item.
- Keep one live presentation per ID, correct holder parent, and quantity labels after reconstruction.
- Player runtime currently throws in TruckLogistics.LateUpdate after QA fixture/save reload.
- Make a minimal repair to the stale-object lifetime and write a focused regression that fails first.
- Code stays below approximately 300 lines per file with one statement per line for new code.

## Verification

Use existing GameSession fixtures in Assets/_Game/Tests/Editor as needed.
Run Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.TruckModelLifetimeTests.
Root is not launching any Unity Editor while this focused test runs.
Save the failing result under work/items-vehicles-20261009/truck-lifetime-before.xml if reproducible.
Run the focused test after the repair, preserve result XML, and report once in at most 15 lines.
Do not build, publish, package, modify saves, or touch source assets outside the assigned files.
