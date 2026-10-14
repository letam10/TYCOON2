# V14 checkpoint history copying

## Change

CheckpointSnapshot.cs dispatches sealed TransactionReceipt, TransactionEvent and their
concrete List types to typed helpers. Preallocated list loops clone entries directly,
without per-entry type inspection, reference-field lookup or reflection field access.
The existing cached MemberwiseClone delegate preserves every field. Event consumers
are copied separately, including empty lists; null remains null. Entries are never shared.
The generic fallback, public API, checkpoint gate and save schema stay unchanged.
Existing uncommitted delegate and string-list optimizations are preserved.

## Verification

CheckpointHistoryTests.cs adds nine cases: exact sealed DTO schema guards; every receipt
field; null and repeated entries; large longs; 20,000 events; null, empty and populated
consumers; null/empty history lists; two-way isolation; and actual TransactionCore capture
followed by a retry and live acknowledgement. New fields require deliberate schema review.
Existing tests cover 20,000 receipts and generic nested DTO/array copying.

Command: Tools/run_unity.ps1 -Task Tests -TestFilter Checkpoint
Unity 6000.6.3f1 native EditMode: 24/24 before and after, zero failures or skips, exit 0.
New history fixture: 9/9. Final XML: work/city-model-v14-20261010/checkpoint-tests.xml.
Owned files pass diff whitespace checks and remain below 300 lines and 120 columns.

## Remaining measurement

Player checkpoint latency must be remeasured using the same checkpoint and settings
before claiming improvement over the prior 19-31 ms. EditMode does not prove Player
frame pacing or IL2CPP acceptance. Main agent owns that measurement.
