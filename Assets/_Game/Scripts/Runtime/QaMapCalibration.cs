using System.Collections;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator VerifyMapPhotoPixels(CityMapCapture capture)
        {
            var positions = new[]
            {
                new Vector3(-220, 64, -180),
                new Vector3(170, 64, 130),
                new Vector3(380, 64, -100),
                new Vector3(-180, 64, 150)
            };
            var colors = new[] { Color.red, Color.green, Color.blue, Color.yellow };
            var markers = new GameObject[positions.Length];
            var materials = new Material[positions.Length];
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Check(shader, "Map calibration has a real render shader");
            try
            {
                for (int index = 0; index < markers.Length; index++)
                {
                    markers[index] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    markers[index].name = "QA_MapPixelMarker_" + index;
                    markers[index].transform.position = positions[index];
                    markers[index].transform.localScale = new Vector3(10, .1f, 10);
                    Destroy(markers[index].GetComponent<Collider>());
                    materials[index] = new Material(shader);
                    materials[index].SetColor("_BaseColor", colors[index]);
                    materials[index].SetColor("_EmissionColor", colors[index]);
                    materials[index].EnableKeyword("_EMISSION");
                    markers[index].GetComponent<Renderer>().sharedMaterial = materials[index];
                    if (index == 3)
                        markers[index].AddComponent<CharacterController>();
                }
                int generation = capture.Generation;
                capture.RequestRefresh();
                yield return CompletionWait(() => capture.Generation > generation && !capture.IsCapturing, 60,
                    "Calibration world markers rendered into aerial tiles");
                Bounds world = CityDistricts.WorldBounds;
                for (int index = 0; index < positions.Length; index++)
                {
                    // Tính pixel độc lập từ world bounds, không dùng hàm WorldToUv đang được kiểm.
                    float u = (positions[index].x - world.min.x) / world.size.x;
                    float v = (positions[index].z - world.min.z) / world.size.z;
                    int x = 1 + Mathf.FloorToInt(u * capture.Grid.Columns * capture.Grid.TileWidth);
                    int y = 1 + Mathf.FloorToInt(v * capture.Grid.Rows * capture.Grid.TileHeight);
                    Color pixel = capture.Atlas.GetPixel(x, y);
                    if (index == 3)
                    {
                        Check(!(pixel.r > .7f && pixel.g > .7f && pixel.b < .2f),
                            "Moving character renderers are excluded from the photo atlas");
                        Check(!markers[index].GetComponent<Renderer>().forceRenderingOff,
                            "Character rendering state is restored after the capture");
                        continue;
                    }
                    float selected = index == 0 ? pixel.r : index == 1 ? pixel.g : pixel.b;
                    float other = index == 0 ? Mathf.Max(pixel.g, pixel.b)
                        : index == 1 ? Mathf.Max(pixel.r, pixel.b) : Mathf.Max(pixel.r, pixel.g);
                    Check(selected > other + .3f,
                        "Rendered world marker lands at its exact atlas pixel: " + index);
                }
            }
            finally
            {
                for (int index = 0; index < markers.Length; index++)
                {
                    if (markers[index])
                        Destroy(markers[index]);
                    if (materials[index])
                        Destroy(materials[index]);
                }
            }
            yield return null;
            int cleanGeneration = capture.Generation;
            capture.RequestRefresh();
            yield return CompletionWait(() => capture.Generation > cleanGeneration && !capture.IsCapturing, 60,
                "Final atlas contains only the game world after removing calibration markers");
        }
    }
}
