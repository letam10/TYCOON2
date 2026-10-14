using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator ShadowStabilitySequence(Transform board, UniversalRenderPipelineAsset pipeline)
        {
            var camera = game.CameraRig;
            var lens = camera.GetComponent<Camera>();
            int count = pipeline.shadowCascadeCount;
            int resolution = pipeline.mainLightShadowmapResolution;
            float range = pipeline.shadowDistance;
            float split = pipeline.cascade2Split;
            float border = pipeline.cascadeBorder;
            var corners = new[] { new Vector3(-5, 0, -4), new Vector3(5, 0, -4),
                new Vector3(5, 0, 4), new Vector3(-5, 0, 4) };
            var csv = new StringBuilder("frame,variant,elapsedSeconds,x0,y0,x1,y1,x2,y2,x3,y3\n");
            try
            {
                foreach (string variant in new[] { "current", "legacy", "single", "singleHigh", "nearSplit" })
                {
                    bool current = variant == "current";
                    pipeline.shadowCascadeCount = current ? count : variant is "legacy" or "nearSplit" ? 2 : 1;
                    pipeline.mainLightShadowmapResolution = current ? resolution : variant == "singleHigh" ? 4096 : 2048;
                    pipeline.shadowDistance = current ? range : variant == "singleHigh" ? 70 : 45;
                    pipeline.cascadeBorder = current ? border : variant == "singleHigh" ? .25f : .1077586f;
                    pipeline.cascade2Split = current ? split : variant == "nearSplit" ? .6f : .25f;
                    camera.Distance = 12;
                    camera.FocusOffset = Vector3.up;
                    camera.Snap();
                    for (int i = 0; i < 8; i++) yield return null;
                    double start = Time.realtimeSinceStartupAsDouble;
                    for (int frame = 0; frame <= 60; frame++)
                    {
                        float phase = frame / 60f;
                        camera.Distance = Mathf.Lerp(12, 27, Mathf.Sin(phase * Mathf.PI));
                        camera.FocusOffset = new Vector3(Mathf.Sin(phase * Mathf.PI * 2) * 1.5f, 1, 0);
                        camera.Snap();
                        string name = $"stability-{variant}-{frame:000}.png";
                        yield return CaptureShadowScreen(name);
                        csv.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2:F6}", name, variant,
                            Time.realtimeSinceStartupAsDouble - start);
                        foreach (var corner in corners)
                        {
                            Vector3 screen = lens.WorldToScreenPoint(board.position + corner);
                            csv.AppendFormat(CultureInfo.InvariantCulture, ",{0:F6},{1:F6}",
                                screen.x, Screen.height - 1 - screen.y);
                        }
                        csv.Append('\n');
                    }
                }
                File.WriteAllText(Path.Combine(game.QaDirectory, "shadow-stability.csv"), csv.ToString());
            }
            finally
            {
                pipeline.shadowCascadeCount = count;
                pipeline.mainLightShadowmapResolution = resolution;
                pipeline.shadowDistance = range;
                pipeline.cascade2Split = split;
                pipeline.cascadeBorder = border;
            }
        }
    }
}
