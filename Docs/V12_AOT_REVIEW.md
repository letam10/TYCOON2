# V12 IL2CPP AOT and save review

## Scope and conclusion

- Read-only source review for the candidate described in `Docs/V12_IL2CPP_CONTRACT.md`.
- No IL2CPP build or Player run was performed; this review does not claim AOT compatibility.
- Source inspection finds no confirmed AOT defect. Two reflection-dependent operations need
  actual IL2CPP Player coverage before the backend can be considered safe.
- Existing Mono tests and Player evidence establish the baseline only; Editor reflection tests
  and successful compilation do not prove IL2CPP behavior.

## Relevant paths

- `Assets/_Game/Scripts/Runtime/CheckpointSnapshot.cs`: snapshot copier and checkpoint capture.
- `Assets/_Game/Scripts/Runtime/TransactionState.cs`: serializable transaction DTO graph;
  public fields include money, owners, goods stacks, payments, receipts and outbox events.
- `Assets/_Game/Scripts/Runtime/RuntimeTransactionProjection.cs`: copies a transaction state
  into `SaveData`, including cash, inventory, receipts, losses and customer baskets.
- `Assets/_Game/Scripts/Runtime/GameplayTransactionStore.cs`: snapshots before worker
  checkpoint serialization and compacts the durable save/journal.
- `Assets/_Game/Scripts/Runtime/SaveData.cs`: Unity `JsonUtility` save write/read, recovery,
  normalization and validation.
- `Assets/_Game/Scripts/Runtime/TransactionStore.cs`: `JsonUtility` serialization of the
  transaction envelope and deserialization of its payload.
- `Assets/_Game/Scripts/Runtime/TransactionCore.cs`: runtime transaction copies use
  `JsonUtility`; checkpoint path delegates to `CaptureCheckpoint`.
- `Assets/_Game/Tests/Editor/CheckpointSnapshotTests.cs` and
  `CheckpointStringListTests.cs`: deep-copy isolation and string-list semantics.
- `Assets/_Game/Tests/Editor/CheckpointRecoveryTests.cs` and
  `SaveWorldCompatibilityTests.cs`: journal recovery, round-trip and legacy save coverage.

## AOT and stripping assessment

- `CheckpointSnapshot` binds `object.MemberwiseClone` once through non-public reflection and
  `MethodInfo.CreateDelegate(Func<object, object>)`. A failure during static initialization
  would break snapshot calls. IL2CPP support for this exact open-instance delegate and retention
  of the reflected method must be proven in the built Player.
- This is a risk to verify, not a confirmed failure: the method name and declaring type are
  constant, and the runtime may preserve the method or implement it as an intrinsic.
- `ReferenceFields` reflects over public instance fields, caches `FieldInfo[]` by runtime type,
  then reads/writes nested references using `FieldInfo.GetValue` and `SetValue`. AOT stripping
  could affect field metadata if a DTO becomes reachable only through dynamic reflection.
- The transaction DTO graph is also statically named, `[Serializable]`, and passed to Unity
  `JsonUtility`; source review alone cannot establish how the candidate linker retains all
  metadata. Treat stripping as an unverified risk, not a confirmed issue.
- Arrays use `Array.Clone`, indexed `GetValue`/`SetValue`; other `IList` implementations use
  `Activator.CreateInstance(type, source.Count)` and `IList.Add`. These paths are reflective.
  Current transaction DTO fields predominantly use concrete `List<T>` and arrays; verify the
  actual traversed runtime types in the candidate before broadening the claim.
- Exact `List<string>` values take a direct `new List<string>(source)` branch. This avoids
  reflection for that hot path and preserves nulls, duplicates, ordering, and copy isolation
  by ordinary list-copy semantics. It does not cover subclasses or other generic lists.
- Generic `Copy<T>` has direct runtime call sites; Editor tests also invoke it via
  `MakeGenericMethod` reflection. Passing the latter under Mono is not evidence that an
  IL2CPP-only generic instantiation is available; confirm every Player call site executes.
- `JsonUtility.ToJson`/`FromJson` paths are separate from `CheckpointSnapshot`. Validate both
  transaction-envelope and full-save round trips under IL2CPP; successful reflection alone
  does not prove Unity serialization retained or restored every field.

## Candidate Player gates

1. Build the named candidate with IL2CPP; retain the build log and confirm the previous
   scripting backend is restored even if the build fails. Do not replace the Mono release.
2. Launch the candidate on the same saved city. Confirm startup/load succeeds without
   `MissingMethodException`, `TypeInitializationException`, reflection/serialization errors,
   or recovery warnings. Record the candidate path and Player log.
3. Before and after save/reload, compare exact money, cash-in-hand, safe cash, pending counter
   cash, and revenue. Perform a real purchase/payment and collect cash; verify amounts change
   once and persist once.
4. Compare physical goods by owner: player carry, storage, stations, customer baskets, and
   any active worker/cargo owners. Exercise take, deliver, and reload; verify goods neither
   disappear nor duplicate and customer-held goods stay customer-owned.
5. Compare receipt IDs, transaction revision, payment/receipt count, losses and pending
   events before and after checkpoint/reload. Retry one already-receipted action and confirm
   it does not pay or deliver twice.
6. Exercise checkpoint isolation while transactions continue: trigger a checkpoint, perform
   another transaction during background serialization if the path allows, wait for checkpoint
   drain/compaction, then reload. Confirm the saved snapshot is internally consistent and the
   journal tail is recovered exactly once.
7. Repeat save/load after a normal exit and a forced process close at an approved test point;
   confirm the existing recovery behavior and preserve logs/save copies for any failure.

## Evidence boundary

- `CheckpointSnapshotTests` covers runtime type, deep nested copies, large receipt histories,
  and mutations in both directions, but runs in the Editor test assembly.
- `CheckpointStringListTests` covers the exact `List<string>` branch, including nulls,
  duplicates, order and isolation; it does not test IL2CPP.
- `CheckpointRecoveryTests` and `SaveWorldCompatibilityTests` cover transaction recovery and
  persistence semantics under their test runtime. The contract's Mono Player PASS is useful
  baseline evidence, not candidate evidence.
- Approve a backend switch only after all candidate Player gates pass with the same save and
  settings. Do not infer save safety or performance from build success or Editor tests.
