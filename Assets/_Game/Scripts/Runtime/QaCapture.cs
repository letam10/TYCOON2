using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        sealed class CaptureRectState
        {
            public readonly RectTransform rect;
            readonly Vector2 anchorMin, anchorMax, pivot, size;
            readonly Vector3 position, scale;
            readonly Quaternion rotation;
            readonly bool active;

            public CaptureRectState(RectTransform value)
            {
                rect = value;
                anchorMin = rect.anchorMin;
                anchorMax = rect.anchorMax;
                pivot = rect.pivot;
                size = rect.sizeDelta;
                position = rect.anchoredPosition3D;
                scale = rect.localScale;
                rotation = rect.localRotation;
                active = rect.gameObject.activeSelf;
            }

            public void Restore()
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.pivot = pivot;
                rect.sizeDelta = size;
                rect.anchoredPosition3D = position;
                rect.localScale = scale;
                rect.localRotation = rotation;
                rect.gameObject.SetActive(active);
            }
        }

        sealed class CaptureCanvasState
        {
            public readonly Canvas canvas;
            public readonly CanvasScaler scaler;
            public readonly Vector2 logicalSize;
            public readonly float scale, pixelsPerUnit;
            public readonly bool scalerEnabled;
            readonly Camera camera;
            readonly float distance;

            public CaptureCanvasState(Canvas value)
            {
                canvas = value;
                scaler = value.GetComponent<CanvasScaler>();
                logicalSize = ((RectTransform)value.transform).rect.size;
                camera = value.worldCamera;
                distance = value.planeDistance;
                scale = value.scaleFactor;
                pixelsPerUnit = value.referencePixelsPerUnit;
                scalerEnabled = scaler && scaler.enabled;
            }

            public void Restore()
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = camera;
                canvas.planeDistance = distance;
                if (scaler) scaler.enabled = scalerEnabled;
                canvas.scaleFactor = scale;
                canvas.referencePixelsPerUnit = pixelsPerUnit;
            }
        }

        IEnumerator Capture(string name)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) yield break;
            yield return null;
            CapturePhysicalFrame(Path.Combine(game.QaDirectory, name));
            report.screenshots.Add(name);
        }

        void CapturePhysicalFrame(string path, int width = 0, int height = 0)
        {
            width = width > 0 ? width : Screen.width;
            height = height > 0 ? height : Screen.height;
            var camera = Camera.main;
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(x => x.enabled && x.renderMode == RenderMode.ScreenSpaceOverlay)
                .Select(x => new CaptureCanvasState(x)).ToArray();
            var rectangles = canvases.SelectMany(x => x.canvas.GetComponentsInChildren<RectTransform>(true))
                .Distinct().Select(x => new CaptureRectState(x)).ToArray();
            var cameraState = new GameObject("QA_CaptureCameraState");
            cameraState.SetActive(false);
            cameraState.hideFlags = HideFlags.HideAndDontSave;
            var savedCamera = cameraState.AddComponent<Camera>();
            savedCamera.CopyFrom(camera);
            var previousActive = RenderTexture.active;
            var target = RenderTexture.GetTemporary(width, height, 24,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                // URP chỉ gán target trong SubmitRenderRequest; canvas cần target trước khi dựng geometry.
                camera.targetTexture = target;
                camera.rect = new Rect(0, 0, 1, 1);
                camera.aspect = (float)width / height;
                camera.allowDynamicResolution = false;
                foreach (var state in canvases)
                {
                    if (state.scaler) state.scaler.enabled = false;
                    state.canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    state.canvas.worldCamera = camera;
                    state.canvas.planeDistance = Mathf.Max(.5f, camera.nearClipPlane + .01f);
                    state.canvas.scaleFactor = CaptureScale(state, width, height);
                    state.canvas.referencePixelsPerUnit = state.pixelsPerUnit;
                }
                Canvas.ForceUpdateCanvases();
                foreach (var state in canvases)
                {
                    var layer = state.canvas.GetComponent<OrderBubbleLayer>();
                    if (layer) layer.RefreshProjection(camera, new Rect(0, 0, width, height));
                }
                Canvas.ForceUpdateCanvases();
                var request = new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest
                {
                    destination = target
                };
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
                if (physicalResolutionCapture != null)
                    MeasurePhysicalResolution(camera, canvases.Select(x => x.canvas).ToArray(), width, height);
            }
            finally
            {
                // CopyFrom giữ cả trạng thái aspect tự động, tránh khóa aspect sau lần chụp đầu.
                camera.CopyFrom(savedCamera);
                foreach (var state in canvases) state.Restore();
                foreach (var state in rectangles) state.Restore();
                Canvas.ForceUpdateCanvases();
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                Destroy(pixels);
                Destroy(cameraState);
            }
        }

        static float CaptureScale(CaptureCanvasState state, int width, int height)
        {
            var scaler = state.scaler;
            if (!scaler || !state.scalerEnabled ||
                scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) return state.scale;
            // Công thức uGUI CanvasScaler.HandleScaleWithScreenSize, dùng kích thước render thật.
            float x = width / scaler.referenceResolution.x;
            float y = height / scaler.referenceResolution.y;
            return scaler.screenMatchMode switch
            {
                CanvasScaler.ScreenMatchMode.Expand => Mathf.Min(x, y),
                CanvasScaler.ScreenMatchMode.Shrink => Mathf.Max(x, y),
                _ => Mathf.Pow(2, Mathf.Lerp(Mathf.Log(x, 2), Mathf.Log(y, 2), scaler.matchWidthOrHeight))
            };
        }

    }
}
