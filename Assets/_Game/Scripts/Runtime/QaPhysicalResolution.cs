using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        [Serializable]
        sealed class PhysicalResolutionSample
        {
            public string screenshot, mode;
            public int requestedWidth, requestedHeight, attemptedWindowWidth, attemptedWindowHeight;
            public int windowWidth, windowHeight, renderedWidth, renderedHeight, pngWidth, pngHeight;
            public bool offscreen, nativeWindowValidated;
            public int visibleTexts, visibleButtons, visibleBubbles, visibleWorldLabels, playerCountLabels;
            public float widestWorldLabelPixels, renderScale;
            public List<string> failures = new();
            public List<PhysicalCanvasSample> canvases = new();
        }

        [Serializable]
        sealed class PhysicalCanvasSample
        {
            public string name;
            public float logicalWidth, logicalHeight, scale, pixelWidth, pixelHeight;
        }

        [Serializable]
        sealed class PhysicalResolutionReport
        {
            public string captureMethod = "URP SingleCameraRequest into a real RenderTexture; no image resizing";
            public List<PhysicalResolutionSample> samples = new();
        }

        PhysicalResolutionSample physicalResolutionCapture;

        IEnumerator PhysicalResolutionAcceptance()
        {
            var evidence = new PhysicalResolutionReport();
            int originalWidth = Screen.width;
            int originalHeight = Screen.height;
            var originalMode = Screen.fullScreenMode;
            try
            {
                foreach (var size in new[]
                {
                    new Vector2Int(1280, 720), new Vector2Int(1920, 1080),
                    new Vector2Int(2560, 1440), new Vector2Int(3840, 2160)
                })
                {
                    Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
                    yield return new WaitForSecondsRealtime(.6f);
                    var sample = new PhysicalResolutionSample
                    {
                        requestedWidth = size.x,
                        requestedHeight = size.y,
                        attemptedWindowWidth = Screen.width,
                        attemptedWindowHeight = Screen.height,
                        nativeWindowValidated = Screen.width == size.x && Screen.height == size.y,
                        screenshot = "physical-ui-" + size.x + ".png"
                    };
                    sample.offscreen = !sample.nativeWindowValidated;
                    sample.mode = sample.offscreen ? "offscreen-resolution" : "native-size-offscreen-capture";
                    if (sample.offscreen)
                    {
                        // Cửa sổ bị Windows giới hạn: dùng layout 16:9 rồi render đúng số pixel yêu cầu.
                        Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
                        yield return new WaitForSecondsRealtime(.6f);
                    }
                    sample.windowWidth = Screen.width;
                    sample.windowHeight = Screen.height;
                    if (Mathf.Abs((float)Screen.width / Screen.height - 16f / 9) > .002f)
                        sample.failures.Add("Native layout fixture is not 16:9");
                    game.Say("Cất hàng để lấy tiền");
                    yield return null;
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    evidence.samples.Add(sample);
                    physicalResolutionCapture = sample;
                    string path = Path.Combine(game.QaDirectory, sample.screenshot);
                    CapturePhysicalFrame(path, size.x, size.y);
                    physicalResolutionCapture = null;
                    using (var input = File.OpenRead(path))
                    {
                        var header = new byte[24];
                        input.Read(header, 0, header.Length);
                        sample.pngWidth = PngDimension(header, 16);
                        sample.pngHeight = PngDimension(header, 20);
                    }
                    if (sample.pngWidth != size.x || sample.pngHeight != size.y)
                        sample.failures.Add("Encoded PNG dimensions do not match the request");
                    report.screenshots.Add(sample.screenshot);
                }
            }
            finally
            {
                physicalResolutionCapture = null;
                Screen.SetResolution(originalWidth, originalHeight, originalMode);
                File.WriteAllText(Path.Combine(game.QaDirectory, "physical-resolutions.json"),
                    JsonUtility.ToJson(evidence, true));
            }
            yield return new WaitForSecondsRealtime(.6f);
            foreach (var sample in evidence.samples)
                Check(sample.failures.Count == 0,
                    "physical resolution " + sample.requestedWidth + "x" + sample.requestedHeight + " " +
                    sample.mode + ": " + string.Join("; ", sample.failures));
        }

        static int PngDimension(byte[] bytes, int offset) =>
            bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3];

        void MeasurePhysicalResolution(Camera camera, Canvas[] canvases, int width, int height)
        {
            var sample = physicalResolutionCapture;
            sample.renderedWidth = camera.targetTexture.width;
            sample.renderedHeight = camera.targetTexture.height;
            sample.renderScale = UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset.renderScale;
            if (sample.renderedWidth != width || sample.renderedHeight != height ||
                !Mathf.Approximately(sample.renderScale, 1))
                sample.failures.Add("Render target or URP render scale differs from requested native pixels");
            foreach (var canvas in canvases)
            {
                var rect = (RectTransform)canvas.transform;
                var measured = new PhysicalCanvasSample
                {
                    name = canvas.name,
                    logicalWidth = rect.rect.width,
                    logicalHeight = rect.rect.height,
                    scale = canvas.scaleFactor,
                    pixelWidth = rect.rect.width * canvas.scaleFactor,
                    pixelHeight = rect.rect.height * canvas.scaleFactor
                };
                sample.canvases.Add(measured);
                if (Mathf.Abs(measured.pixelWidth - width) > 2 || Mathf.Abs(measured.pixelHeight - height) > 2)
                    sample.failures.Add(canvas.name + " canvas does not cover the render target");
                foreach (var text in canvas.GetComponentsInChildren<Text>())
                {
                    if (!text.isActiveAndEnabled || text.color.a <= .01f || string.IsNullOrWhiteSpace(text.text))
                        continue;
                    sample.visibleTexts++;
                    if (text.fontSize > 40 || text.resizeTextForBestFit && text.resizeTextMaxSize > 40)
                        sample.failures.Add(text.name + " font exceeds 40 logical pixels");
                    if (!text.GetComponentInParent<OrderBubbleLayer>())
                        MeasurePhysicalRect(text.rectTransform, camera, width, height, text.name, sample);
                }
                foreach (var button in canvas.GetComponentsInChildren<Button>())
                {
                    if (!button.isActiveAndEnabled) continue;
                    sample.visibleButtons++;
                    MeasurePhysicalRect((RectTransform)button.transform, camera, width, height, button.name, sample);
                }
                if (!canvas.GetComponent<OrderBubbleLayer>()) continue;
                foreach (RectTransform bubble in canvas.transform)
                {
                    if (!bubble.gameObject.activeInHierarchy) continue;
                    sample.visibleBubbles++;
                    MeasurePhysicalRect(bubble, camera, width, height, "OrderBubble", sample);
                }
            }
            if (sample.visibleTexts == 0 || sample.visibleButtons < 5 || sample.canvases.Count == 0)
                sample.failures.Add("HUD texts or five quick controls are missing");
            MeasurePhysicalWorldLabels(camera, width, height, sample);
        }

        static void MeasurePhysicalRect(RectTransform rect, Camera camera, int width, int height,
            string name, PhysicalResolutionSample sample)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            if (rect.rect.width <= 0 || rect.rect.height <= 0)
                sample.failures.Add(name + " has an empty rectangle");
            foreach (var corner in corners)
            {
                Vector3 pixel = camera.WorldToScreenPoint(corner);
                if (pixel.z > 0 && pixel.x >= -2 && pixel.y >= -2 && pixel.x <= width + 2 && pixel.y <= height + 2)
                    continue;
                sample.failures.Add(name + " outside render bounds: " + pixel);
                break;
            }
        }

        void MeasurePhysicalWorldLabels(Camera camera, int width, int height, PhysicalResolutionSample sample)
        {
            foreach (var label in FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            {
                var renderer = label.GetComponent<Renderer>();
                if (!renderer || !renderer.enabled || string.IsNullOrWhiteSpace(label.text)) continue;
                var bounds = renderer.localBounds;
                Vector3 center = camera.WorldToViewportPoint(renderer.transform.TransformPoint(bounds.center));
                bool playerLabel = label.transform.IsChildOf(game.Player.transform);
                if (center.z <= 0 || center.x < 0 || center.x > 1 || center.y < 0 || center.y > 1)
                {
                    if (playerLabel) sample.failures.Add("Player count label is outside the render");
                    continue;
                }
                Vector2 minimum = new(float.PositiveInfinity, float.PositiveInfinity);
                Vector2 maximum = new(float.NegativeInfinity, float.NegativeInfinity);
                for (int i = 0; i < 8; i++)
                {
                    var sign = new Vector3((i & 1) == 0 ? -1 : 1,
                        (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1);
                    Vector3 local = bounds.center + Vector3.Scale(bounds.extents, sign);
                    Vector3 point = camera.WorldToScreenPoint(renderer.transform.TransformPoint(local));
                    minimum = Vector2.Min(minimum, point);
                    maximum = Vector2.Max(maximum, point);
                }
                float pixels = maximum.x - minimum.x;
                sample.visibleWorldLabels++;
                sample.widestWorldLabelPixels = Mathf.Max(sample.widestWorldLabelPixels, pixels);
                if (pixels > width * .16f) sample.failures.Add(label.text + " world label exceeds 16% render width");
                if (!playerLabel) continue;
                sample.playerCountLabels++;
                if (minimum.x < 0 || minimum.y < 0 || maximum.x > width || maximum.y > height)
                    sample.failures.Add("Player count label is clipped by the render bounds");
            }
            if (game.Player.HasCarry && sample.playerCountLabels == 0)
                sample.failures.Add("Carried goods or cash have no visible count label");
        }
    }
}
