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
        sealed class MapPhotoTileEvidence
        {
            public int index;
            public Bounds worldBounds;
            public Rect uv;
            public RectInt pixels;
            public string image;
        }

        [Serializable]
        sealed class MapPhotoEvidence
        {
            public string capturedUtc;
            public int columns, rows, atlasWidth, atlasHeight, generation;
            public float maximumProjectionErrorPixels;
            public Bounds worldBounds;
            public List<MapPhotoTileEvidence> tiles = new();
        }

        IEnumerator MapPhotographyAcceptance()
        {
            yield return CompletionWait(() => game.NavigationReady && !game.IsRestoring, 30,
                "Photographic map world ready");
            CityMapCapture capture = CityMapCapture.Ensure(game);
            yield return CompletionWait(() => capture.Atlas && !capture.IsCapturing, 60,
                "Initial world tiles captured");
            float pitch = game.CameraRig.Pitch;
            float yaw = game.CameraRig.Yaw;
            float distance = game.CameraRig.Distance;
            Vector3 player = game.Player.transform.position;
            int generation = capture.Generation;
            game.Hud.OpenMap();
            yield return CompletionWait(() => capture.Generation > generation && !capture.IsCapturing, 60,
                "Map refresh replaces the complete tile set");
            yield return VerifyMapPhotoPixels(capture);
            Check(capture.Grid.TileCount == 12, "Twelve orthographic tiles cover the world");
            Check(capture.MaximumProjectionErrorPixels < .5f, "Tile world projection error is below half a pixel");
            var map = SettingsObject<CityMapGraphic>("WorldMapTerrain");
            Check(map.mainTexture == capture.Atlas, "Full map renders the captured atlas");
            var mini = SettingsObject<CityMapGraphic>("MinimapTerrain");
            Check(mini.mainTexture == capture.Atlas, "Minimap shares the same captured world atlas");
            Check(MaskUtilities.GetRectMaskForClippable(map), "Full map clips photo tiles and labels at the viewport");
            var colors = new HashSet<Color32>();
            for (int y = 64; y < capture.Atlas.height; y += 128)
                for (int x = 64; x < capture.Atlas.width; x += 128)
                    colors.Add(capture.Atlas.GetPixel(x, y));
            Check(colors.Count > 20, "Atlas contains varied rendered world pixels");
            File.WriteAllBytes(Path.Combine(game.QaDirectory, "aerial-atlas.png"), capture.Atlas.EncodeToPNG());
            var evidence = new MapPhotoEvidence
            {
                capturedUtc = DateTime.UtcNow.ToString("o"),
                columns = capture.Grid.Columns,
                rows = capture.Grid.Rows,
                atlasWidth = capture.Atlas.width,
                atlasHeight = capture.Atlas.height,
                generation = capture.Generation,
                maximumProjectionErrorPixels = capture.MaximumProjectionErrorPixels,
                worldBounds = capture.Grid.Bounds
            };
            for (int index = 0; index < capture.Grid.TileCount; index++)
            {
                RectInt pixels = capture.Grid.TilePixels(index);
                string image = "aerial-tile-" + index.ToString("D2") + ".png";
                var tile = new Texture2D(pixels.width, pixels.height, TextureFormat.RGBA32, false);
                tile.SetPixels(capture.Atlas.GetPixels(pixels.x, pixels.y, pixels.width, pixels.height));
                tile.Apply();
                File.WriteAllBytes(Path.Combine(game.QaDirectory, image), tile.EncodeToPNG());
                Destroy(tile);
                evidence.tiles.Add(new MapPhotoTileEvidence
                {
                    index = index,
                    worldBounds = capture.Grid.TileBounds(index),
                    pixels = pixels,
                    uv = capture.Grid.TileUv(index),
                    image = image
                });
            }
            File.WriteAllText(Path.Combine(game.QaDirectory, "aerial-map-manifest.json"),
                JsonUtility.ToJson(evidence, true));
            yield return Capture("photo-map-full.png");
            game.Hud.FocusMapPlayer();
            game.Hud.ZoomMap(3);
            yield return Capture("photo-map-farm-zoom.png");
            Vector2 before = map.Position(player);
            Vector2 drag = new Vector2(.11f, -.07f);
            game.Hud.PanMap(drag);
            Check(Vector2.Distance(map.Position(player), before - drag) < .0001f,
                "Map drag moves image and player marker by the same viewport delta");
            yield return Capture("photo-map-pan.png");
            game.Hud.RotateMap(37);
            foreach (var point in CityMapLocations.Points)
            {
                Vector3 restored = map.WorldAt(map.Position(point.position));
                Check(Vector2.Distance(new Vector2(restored.x, restored.z),
                    new Vector2(point.position.x, point.position.z)) < .001f,
                    "Rotated photo and POI share world coordinates: " + point.id);
            }
            yield return Capture("photo-map-rotated.png");
            game.Hud.ResetMapNorth();
            game.Hud.CloseMap();
            Check(game.CameraRig.Pitch == pitch && game.CameraRig.Yaw == yaw && game.CameraRig.Distance == distance,
                "Aerial capture preserves gameplay camera configuration");
            Check(Vector3.Distance(game.Player.transform.position, player) < .01f,
                "Map inspection does not move the player");
            yield return Capture("photo-minimap-gameplay.png");
            Check(game.RuntimeErrors.Count == 0, "Photographic map has no runtime errors");
        }
    }
}
