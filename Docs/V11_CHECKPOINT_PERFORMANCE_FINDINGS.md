# V11 checkpoint performance findings

## Scope and evidence

This is a source review of the checkpoint path and existing reports only. No Unity, Player, or tests were run.

`SaveGame.Checkpoint` samples in `work/city-expansion-20261008/ordinary-play-20261010T015900930Z/operation-timings.csv` rise from 9.170–16.495 ms early to 55.080–61.990 ms later (18 samples). The second run, `ordinary-play-20261010T024906901Z/operation-timings.csv`, records 77.418 and 67.978 ms. The third, `ordinary-play-20261010T030724987Z/operation-timings.csv`, records 78.049 and 58.954 ms. These establish the aggregate main-thread operation cost in those runs. CPU JSON exports do not expose checkpoint-specific internal phase timings; they cannot establish which phase accounts for the increase. No speedup is claimed.

## Synchronous path and scaling

1. `GameSession.SaveGame()` measures `SaveGame.Checkpoint` around `Transactions.Checkpoint()`.
2. `RuntimeTransactions.Checkpoint()` calls `TransactionCore.CaptureCheckpoint()`. That method holds `gate` while `CheckpointSnapshot.Copy(state)` recursively clones the complete `TransactionState`. The recursive reflection walk visits every reference field and reconstructs lists and arrays. Cost scales with all state DTOs and nested references, including receipts, outbox events, orders and lines, stacks, reservations and allocations, owners, stations, and history lists. It also blocks transactions for the duration of this copy.
3. `GameplayTransactionStore.CheckpointOwned()` verifies revision and deep-copies `game.CaptureSaveData()` to detach live world DTOs. This work is synchronous. The copy traverses the save's current lists and nested DTOs.
4. `RuntimeTransactions.ProjectOwned()` adds the transaction snapshot and projects it into the save DTO. There are repeated linear searches (`Find`/`Where`) over owners, orders, payments, reservations, and output lists. Most directly proportional to inventory stacks, `Items(state, owner)` scans all stacks and groups by item each time it is called. It is called for failed orders, persisted inventories, workers, customers, and diners. Thus projection can approach `O(number of projected owners × stack count)` in addition to per-owner grouping; customer/order lookups also grow with owner and order counts.
5. `CheckpointWriter.Start(data)` queues `SaveStore.WriteCheckpoint` on `Task.Run`. Validation, JSON serialization, UTF-8 encoding, flush, and atomic replace are in the background, outside the measured synchronous `SaveGame.Checkpoint` call. They still consume CPU and can affect total process CPU, but the supplied operation timing does not attribute them to the calling frame.

The step order and complexity descriptions above are source-based inference from `CheckpointSnapshot.cs`, `RuntimeTransactionBindings.cs`, `GameplayTransactionStore.cs`, `RuntimeTransactionProjection.cs`, `CheckpointWriter.cs`, and `SaveData.cs`. The benchmark evidence is the aggregate timing above, not per-phase profiling.

## Smallest proposed optimization

In `Assets/_Game/Scripts/Runtime/RuntimeTransactionProjection.cs`, build a local index of `state.stacks` by `owner` once inside `ProjectSnapshot(...)`, then use it for each `Items(state, owner)` projection. Keep the existing per-owner `GroupBy(item)` and sum so item aggregation and encounter order remain the same. A dictionary from owner ID to that owner's stack list removes repeated full-stack scans while keeping the save schema, values, receipt IDs, locks, deep-copy boundary, and journal/recovery behavior unchanged. Keep the existing `Items(TransactionState state, string owner)` overload if other callers need it; add a private overload accepting the local owner index and route only projection calls through it.

This targets the clear repeated scan and is smaller than replacing the generic deep copier. It is a candidate, not a measured fix. If the index construction or grouping becomes the dominant cost, phase-level instrumentation and another Player run would be needed before further changes.

## Verification to require for a future implementation

- Add a projection equivalence test comparing every owner/item total and serialized `SaveData` from the indexed path with expected results for multiple owners, repeated item stacks, empty owners, and no stacks. Include customer and worker carries so all current projection call sites are covered.
- Preserve and run the existing checkpoint tests in `Assets/_Game/Tests/Editor/CheckpointRecoveryTests.cs`, especially `BackgroundCheckpointRetainsLaterJournalAndReceiptDedup`, `CheckpointCopiesRecipeArraysBeforeBackgroundSerialization`, `CompactCheckpointRoundTripsAllSaveFieldsThroughExistingReader`, background failure, and injected interruption at both replace boundaries.
- Preserve journal/recovery coverage in `Assets/_Game/Tests/Editor/CityJournalStreamTests.cs` and `Assets/_Game/Tests/Editor/TransactionRecoveryTests.cs`: committed later journal commands replay once, receipt dedup remains intact, and incomplete/invalid durable state is not weakened.
- Native project test command, when implementation is authorized: `powershell -NoProfile -ExecutionPolicy Bypass -File D:\GAME\TYCOON2\Tools\run_unity.ps1 -Task Tests`. A Player benchmark is still required to establish any performance change; do not infer one from EditMode tests.

## Open evidence gap

The current reports provide no per-phase checkpoint timings and do not correlate sample cost with stack/order/receipt counts at each checkpoint. The proposed stack index addresses a visible algorithmic multiplier, but the contribution of snapshot cloning versus projection has not been measured. Add narrowly scoped phase timing/count evidence in a separately authorized change if the first optimization does not explain the trend.
