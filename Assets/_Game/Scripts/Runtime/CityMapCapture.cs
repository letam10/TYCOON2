using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tycoon
{
    public sealed class CityMapCapture : MonoBehaviour
    {
        GameSession game;
        bool refreshRequested = true;
        public Texture2D Atlas { get; private set; }
        public CityMapCaptureGrid Grid { get; private set; }
        public bool IsCapturing { get; private set; }
        public int Generation { get; private set; }
        public float MaximumProjectionErrorPixels { get; private set; }
        public event Action Changed;

        public static CityMapCapture Ensure(GameSession session)
        {
            var capture = session.GetComponent<CityMapCapture>();
            if (!capture)
                capture = session.gameObject.AddComponent<CityMapCapture>();
            capture.game = session;
            return capture;
        }

        public void RequestRefresh()
        {
            if (!IsCapturing)
                refreshRequested = true;
        }

        void Update()
        {
            if (!refreshRequested || IsCapturing || !game || !game.NavigationReady || game.IsRestoring)
                return;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;
            refreshRequested = false;
            StartCoroutine(CaptureTiles());
        }

        IEnumerator CaptureTiles()
        {
            IsCapturing = true;
            var grid = new CityMapCaptureGrid(CityDistricts.WorldBounds, 4, 3, 1024);
            var atlas = new Texture2D(grid.AtlasWidth, grid.AtlasHeight, TextureFormat.RGBA32, true, false);
            atlas.name = "CityMapAerialAtlas";
            atlas.wrapMode = TextureWrapMode.Clamp;
            atlas.filterMode = FilterMode.Trilinear;
            var cameraObject = new GameObject("CityMapCaptureCamera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.transform.rotation = Quaternion.Euler(90, 0, 0);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(49, 89, 100, 255);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.allowDynamicResolution = false;
            camera.useOcclusionCulling = false;
            camera.cullingMask = ~0;
            var additional = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            additional.renderPostProcessing = false;
            additional.renderShadows = false;
            additional.requiresDepthTexture = true;
            additional.requiresColorTexture = true;
            float highest = grid.Bounds.max.y;
            float lowest = grid.Bounds.min.y;
            foreach (Renderer renderer in FindObjectsByType<Renderer>())
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;
                highest = Mathf.Max(highest, renderer.bounds.max.y);
                lowest = Mathf.Min(lowest, renderer.bounds.min.y);
            }
            float altitude = highest + 1;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = Mathf.Max(2, altitude - lowest + 1);
            var target = RenderTexture.GetTemporary(grid.TileWidth, grid.TileHeight, 24,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            float maximumError = 0;
            try
            {
                for (int index = 0; index < grid.TileCount; index++)
                {
                    if (game.IsRestoring)
                    {
                        refreshRequested = true;
                        yield break;
                    }
                    Bounds tile = grid.TileBounds(index);
                    camera.transform.position = new Vector3(tile.center.x, altitude, tile.center.z);
                    camera.orthographicSize = tile.size.z * .5f;
                    camera.aspect = tile.size.x / tile.size.z;
                    camera.targetTexture = target;
                    maximumError = Mathf.Max(maximumError, ProjectionError(camera, tile, grid));
                    CaptureTile(camera, target, atlas, grid.TilePixels(index));
                    yield return null;
                }
                PadAtlasEdges(atlas);
                atlas.SetPixel(0, 0, Color.white);
                atlas.Apply(true, false);
                Texture2D previous = Atlas;
                Atlas = atlas;
                atlas = null;
                Grid = grid;
                MaximumProjectionErrorPixels = maximumError;
                Generation++;
                Changed?.Invoke();
                if (previous)
                    Destroy(previous);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(target);
                Destroy(cameraObject);
                if (atlas)
                    Destroy(atlas);
                IsCapturing = false;
            }
        }

        static void CaptureTile(Camera camera, RenderTexture target, Texture2D atlas, RectInt pixels)
        {
            RenderTexture previous = RenderTexture.active;
            try
            {
                using var visibility = new CityMapCaptureVisibility();
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                atlas.ReadPixels(new Rect(0, 0, pixels.width, pixels.height), pixels.x, pixels.y, false);
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        static float ProjectionError(Camera camera, Bounds tile, CityMapCaptureGrid grid)
        {
            float error = 0;
            for (int corner = 0; corner < 5; corner++)
            {
                Vector2 expected = corner == 4 ? Vector2.one * .5f
                    : new Vector2(corner % 2, corner / 2);
                Vector3 world = new Vector3(Mathf.Lerp(tile.min.x, tile.max.x, expected.x),
                    tile.center.y, Mathf.Lerp(tile.min.z, tile.max.z, expected.y));
                Vector3 actual = camera.WorldToViewportPoint(world);
                Vector2 delta = new Vector2((actual.x - expected.x) * grid.TileWidth,
                    (actual.y - expected.y) * grid.TileHeight);
                error = Mathf.Max(error, delta.magnitude);
            }
            return error;
        }

        static void PadAtlasEdges(Texture2D atlas)
        {
            // Lặp mép ảnh để bilinear không pha với nền trống bên ngoài world bounds.
            int right = atlas.width - 1;
            int top = atlas.height - 1;
            for (int x = 1; x < right; x++)
            {
                atlas.SetPixel(x, 0, atlas.GetPixel(x, 1));
                atlas.SetPixel(x, top, atlas.GetPixel(x, top - 1));
            }
            for (int y = 1; y < top; y++)
            {
                atlas.SetPixel(0, y, atlas.GetPixel(1, y));
                atlas.SetPixel(right, y, atlas.GetPixel(right - 1, y));
            }
        }

        void OnDestroy()
        {
            StopAllCoroutines();
            if (Atlas)
                Destroy(Atlas);
        }
    }
}
