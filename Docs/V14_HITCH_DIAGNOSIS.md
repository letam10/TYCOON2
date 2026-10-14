# V14 hitch diagnosis

Evidence: `work/city-expansion-20261008/ordinary-play-20261010T085522987Z/`.
This 937.57 s ordinary Player run passed functional checks, averaged 184.09 FPS,
and did not meet stable 165 FPS: 318 frames exceeded 16 ms, 47 exceeded 33 ms,
6 exceeded 100 ms, and the worst frame was 587.419 ms. Recorded mean CPU/GPU
frame timings were 5.675/2.115 ms across 924 samples; they are sparse averages,
not timings for the hitch frames.

## Frame attribution

- Startup restore/QA: hitch frames 23 (440.968 ms) and 24 (66.748 ms) occur at
  9.109/9.162 s. Operation timings place `CityPlay.Restore.Snapshot`,
  `CityPlay.Restore.Receipts`, and the enclosing `CityPlay.ResumeCity` on frame
  23. This is startup/restore instrumentation, not ordinary gameplay. The
  enclosing scope reports 3401.639 ms across coroutine work; it cannot be
  treated as one frame's CPU cost or added to its nested scopes.
- Live-play but unattributed: frames 1934, 2757–3251, 12724, 24342, 25627–28,
  26210–12, 34246–34408, 89973, 90985, 161510–161756, 165367, 171004, and
  171855 have no same-frame operation record. They occur after startup, during
  active play, but these files do not identify their cause. In particular,
  frame 89973 at 528.920 s (587.419 ms) and frame 165367 at 907.605 s
  (369.609 ms) do not coincide with a recorded checkpoint.
- Checkpoint/autosave: checkpoint timings recur about every 60 s. Frames 8484,
  25800, 143352, and 165605 have hitch samples of 35.203, 44.632, 43.000, and
  45.400 ms respectively, aligned in time with `SaveGame.Checkpoint` timings
  of 24.013, 24.571, 31.755, and 33.613 ms. This supports checkpoint work as
  a contributor to these moderate hitches, not as an explanation for all
  spikes. The hitch recorder writes `Time.frameCount - 1`, while operation
  timings write `Time.frameCount`; compare timestamps and allow a one-frame
  label offset.

## Candidate and next experiment

The measured CPU candidate is the synchronous transaction snapshot taken
before the checkpoint writer starts. `RuntimeTransactions.Checkpoint` calls
`TransactionCore.CaptureCheckpoint` under its state lock; the deep copy walks
the transaction state's reference fields and lists. Its recorded cost ranges
from 10.866 ms (frame 16942) to 29.835 ms (frame 165605), with late samples
higher than early ones but no steady monotonic rise. The enclosing checkpoint
cost is 14.016–33.613 ms. Nested `TransactionSnapshot`, `CaptureWorld`,
`CopyWorld`, `Projection`, and `SaveGame.Checkpoint` scopes overlap; do not sum
them. The timing evidence does not include transaction item counts, so growth
is a plausible cost driver, not proven causality.

Smallest next experiment: use the planned short Normal-window native CPU
profile on the same ordinary-play route and capture thread stacks across a
checkpoint and any >100 ms hitch. Check whether the checkpoint's synchronous
deep copy occupies the main thread, then inspect the 587/370 ms hitch stacks
separately. Keep the existing 165 FPS cap and thresholds. The Player log says
DXGI frame statistics are broken and delta time falls back to CPU-side
timestamps; this limits frame-presentation/GPU attribution. The recorded GPU
average and device name alone do not establish utilization or thermal cause.

No autosave timing names the six >100 ms frames. Treat those spikes as
unexplained until a frame-correlated CPU profile identifies the work; do not
select a fix from this timing set alone.
