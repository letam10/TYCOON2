using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class CityPlaySession
    {
        // Ghi hình riêng sau lượt đo; vẫn dùng input và nhặt hàng qua giao dịch thật.
        IEnumerator CaptureCarryMotion()
        {
            var plot = game.Producers.First(x => x.Id == "field_carrot");
            yield return Walk(plot.InteractionPoint);
            yield return Wait(() => game.Player.Carry.Count("carrot") >= 48, 90,
                "Chuẩn bị chồng hàng đầy để ghi chuyển động");
            yield return Walk(plot.InteractionPoint + Vector3.back * 2);
            var follow = Camera.main.GetComponent<FollowCamera>();
            float before = follow.Distance;
            follow.Distance = FollowCamera.MinimumDistance;
            follow.Snap();
            string directory = Path.Combine(game.QaDirectory, "carry-motion-frames");
            Directory.CreateDirectory(directory);
            int frame = 0;
            float next = Time.realtimeSinceStartup;
            var route = Walk(plot.InteractionPoint + Vector3.back * 12);
            while (route.MoveNext())
            {
                if (Time.realtimeSinceStartup >= next)
                {
                    yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, $"frame-{frame++:D4}.png"));
                    next = Time.realtimeSinceStartup + .1f;
                }
                yield return route.Current;
            }
            yield return Capture("carry-closeup");
            File.WriteAllText(Path.Combine(directory, "capture.txt"),
                "Live 48-item carry walk, native 1080p, nominal 10 fps, outside FPS measurement. Frames=" + frame);
            follow.Distance = before;
            follow.Snap();
        }
    }
}
