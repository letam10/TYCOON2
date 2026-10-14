# City capture repair 11 October 2026

`QaRenderedFrame.Write` now owns the proven URP `SingleCameraRequest` capture path formerly embedded in `QaDriver`.
It renders into a temporary ARGB32 sRGB `RenderTexture`, reads that target directly, and writes the requested PNG
dimensions.
The capture temporarily projects enabled screen-space overlay canvases through the main camera so UI geometry is
included in the render request.

`QaDriver.CapturePhysicalFrame` remains the compatibility hook for all existing QA routines.
When physical-resolution acceptance is active, it supplies `MeasurePhysicalResolution` through the
`Action<Camera, Canvas[], int, int>` callback. The callback runs after the PNG is encoded, while the
requested camera target and canvas projection are still available for measurement.

The `finally` block restores the camera with `CopyFrom`, restores canvas render mode, camera, plane distance,
scale, and reference pixels, restores every captured `RectTransform`, returns the previous active render texture,
and releases temporary Unity objects.
The module does not change gameplay, save handling, movement, motion, frame gates, or native window operations.

Verification for this extraction is intentionally source-only. Unity and Player execution remain with the main agent
under the repair contract.
