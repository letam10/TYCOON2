# V17 capture review: QA images and ordinary-play frame recording

## Scope and source evidence

This is a read-only review of `ordinary-play-20261010T114554647Z` and the
current `CityPlaySession` capture path. It does not rerun Unity, Player, or a
benchmark. The run report is a 1920x1080 Direct3D11 Player sample with
141,632 recorded frames over 881.634 s. It reports 160.647 FPS average,
P95 7.076 ms, P99 9.365 ms, worst 431.648 ms, 64 frames over 16 ms,
9 over 33 ms, and 2 over 100 ms; `stable165FrameBudget` is false.
Source: `ordinary-play-report.json` and `ordinary-play-hitches.csv` in the run
directory named by the V17 contract.

## Actual screenshot paths

1. `CityPlayMovement.Capture` stops input, waits 0.25 s, yields
   `WaitForEndOfFrame`, then calls
   `ScreenCapture.CaptureScreenshot(Path.Combine(game.QaDirectory, name +
   ".png"))`. It appends the filename and yields once more. This is the
   ordinary gameplay QA path for carried produce, sale cash, upgrade feedback,
   district, stack, park, and landmark images.
2. `CityPlayMotion.CaptureCarryMotion` is a separate post-measurement path.
   It walks a live 48-item carry route and, when its 0.1 s schedule is due,
   yields `WaitForEndOfFrame` and calls the same `CaptureScreenshot` into
   `carry-motion-frames/frame-NNNN.png`. It records capture timestamps, then
   writes a text file stating nominal 10 FPS and that the sequence is outside
   FPS measurement. The final close-up uses the ordinary `Capture` helper.
3. `CityPlaySession.PlayAndCapture` sets `recording = false` immediately after
   `Play()` returns, writes the city profile, and only then runs
   `CaptureCityPresentation`. The presentation code disables follow-camera
   control, changes camera/fog/bubbles for gallery views, and restores them
   after capture. Therefore overview/scenery/transit images are not gameplay
   frames from the FPS interval.
4. `CityPlayReport` records frame deltas while `recording` is true and writes
   the ordinary report/timeline/hitch files at finish. No screenshot name,
   PNG encode duration, file-write duration, or capture call timestamp is
   joined to a frame number in these files.

The run's `capture-validity.json` reports 44 images, all accepted as nonblank,
with 1920x1080 dimensions. That proves readable static evidence for this
validator's sample checks. It does not prove continuous rendering, display
scanout, or that a capture did not delay a neighboring frame.

## Can the current evidence attribute a hitch to screenshots?

No. The two >100 ms hitches occur at report frames 3 (431.648 ms, 4.008 s)
and 4 (106.395 ms, 4.113 s). The native long-frame collector reports nearby
sample frames 5 and 5 at 4.114 s, plus later samples at 68.350, 70.620,
70.710, 102.980, 102.995, 507.313, 759.377, and 843.506 s. The operation
CSV has no screenshot operation at those points. This frame-number mismatch is
expected from separate collectors and must not be treated as same-frame proof.

The large gameplay capture set is scheduled after waits and route actions, but
the report does not persist an event timestamp for each PNG request. The
current data therefore supports only a plausible cost: a 1920x1080 screenshot
request and PNG/file work can consume CPU/GPU or I/O time. It does not establish
that the 431 ms, 106 ms, or any other hitch was caused by a screenshot. The
report also cannot distinguish GPU readback, PNG encoding, filesystem latency,
or unrelated startup/scene work for a capture without per-call timings.

## Safe next implementation (proposal, not applied here)

Keep the existing capture mode for visual QA, but instrument each request with
an explicit event record: monotonic start/end timestamps, `Time.frameCount`,
requested path, width/height, whether it is gameplay or post-recording gallery,
and success/error text. Record the event before the call and after the next
frame, and include file length plus `File.Exists` after the write. Join events
to frame/hitch logs only by the recorded frame and timestamp; preserve all
slow frames. Run one matched ordinary-play comparison with capture enabled and
one with captures disabled, using the same route, build, resolution, save, and
duration. This is the minimum evidence needed before attributing cost.

If a later implementation needs less main-thread blocking, evaluate a staged
pipeline rather than changing QA semantics in place:
- On the Unity main/render thread, request a readback into a resource whose
  lifetime extends until the callback completes. `AsyncGPUReadback` is the
  official API family for asynchronous GPU-to-CPU copies; inspect
  `AsyncGPUReadback.Request` and its `Request.hasError`/`GetData<T>` contract
  for the exact Unity version before coding.
- Keep `Texture2D`, `RenderTexture`, `AsyncGPUReadbackRequest`, and graphics
  resource disposal on the Unity thread unless the versioned API explicitly
  documents otherwise. Do not pass a live Unity object to a worker thread.
  Copy returned bytes into ordinary managed/native data only after the request
  is complete, then encode and write from a bounded worker queue if profiling
  proves that this is safe.
- `Texture2D.EncodeToPNG` is a CPU encode of readable texture data. Verify the
  exact Unity 6 page/API availability before using it; do not assume that an
  arbitrary `NativeArray` or a texture created with an unreadable format can
  be encoded. Keep a copy or ownership token until encoding finishes.
- Preserve `WaitForEndOfFrame` ordering for UI-inclusive captures. Test the
  player camera orientation and image dimensions in windowed and fullscreen
  modes; screen-space UI, dynamic resolution, render scale, and platform
  orientation can change the image or dimensions. The existing validator's
  1920x1080 result covers this run only.
- Report errors as QA failures with path, frame, and exception/`hasError`
  details. Treat a missing file, zero-byte file, failed readback, and PNG
  encode exception as distinct failures. Never silently drop a frame to make
  the FPS report look better.

## Official API references checked

- Unity 6.0 `ScreenCapture.CaptureScreenshot`:
  https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ScreenCapture.CaptureScreenshot.html
- Unity 6.0 `AsyncGPUReadback`:
  https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.AsyncGPUReadback.html
- Unity 6.0 `AsyncGPUReadback.Request`:
  https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.AsyncGPUReadback.Request.html

The Unity 6.0 pages above were reachable during this review. The versioned
`Texture2D.EncodeToPNG` page URL used in older documentation returned 404 in
this check, so no claim is made here about a Unity 6 overload or thread-safety
guarantee for PNG encoding. Resolve that API against the installed Unity 6.3
documentation before implementation.

## Open issues

- There is no per-capture timing/error record in the V17 run, so screenshot
  cost remains unmeasured and attribution remains open.
- The capture-validity artifact checks static nonblank samples only; it does not
  validate UI state, orientation across modes, or presentation cadence.
- No async readback/encoding change should be merged until the matched A/B run
  and resource-lifetime/error-path checks are available.
