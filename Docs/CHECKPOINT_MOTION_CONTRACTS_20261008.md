# Checkpoint and collision continuation

## Ownership

- Root owns navigation, player/NPC collision, QA movement diagnostics, builds and Player runs.
- Save agent owns SaveData.cs, GameplayTransactionStore.cs, RuntimeTransactionBindings.cs,
  RuntimeTransactionProjection.cs and new Checkpoint*.cs runtime/test files only.
- No agent starts Unity or Player while root is investigating/building.

## Save invariants

- Existing schema 3 and all receipt/effect IDs, wallets, carry and progress remain compatible.
- Every command still writes a checksummed journal line and Flush(true) before publishing state.
- Exclusive lease remains held throughout any background work and until store disposal completes.
- Capture Unity world data and immutable transaction snapshot on the main thread only.
- Background checkpoint receives immutable SaveData and an absolute path; it cannot call GameSession,
  transforms, components, UI, or mutate the live TransactionCore/view.
- Compact JSON is compatible with current JsonUtility read and retains all existing fields.
- A completed snapshot is validated, flushed and atomically replaced before any journal pruning.
- Later commands must survive checkpoint completion; do not truncate the complete journal while new
  commands have been appended. Keeping the journal until synchronous shutdown is acceptable initially.
- Only one checkpoint runs at once. No unbounded Task queue. Errors are observed and reported.
- Dispose/quit/load waits for pending work before releasing the lease or reading/replacing the save.
- Recovery after interruption at either commit boundary and replay dedup must continue to pass.
- Fault injection tests must remain deterministic and synchronous.

## APIs allowed

- Keep public RuntimeTransactions.Checkpoint() and GameplayTransactionStore.Checkpoint(state).
- An internal drain/flush method or immutable encoded checkpoint helper may be added.
- Project may accept an optional explicitly owned snapshot to avoid a redundant deep copy.
- No changes to economic rules, auto-save interval, loss/receipt retention, FPS criteria or QA fixtures.

## Verification

- Add meaningful focused EditMode tests for concurrent journal commands during a checkpoint,
  pending disposal/reopen, replay/dedup and serialization round trip.
- Static checks: git diff --check and new code approximately <=300 lines / <=120 columns.
- Root executes Tools/run_unity.ps1 -Task Tests and actual ordinary Player including save/load.
