using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator CapturePassengerMotion(string name, Vector3 center)
        {
            var camera = game.CameraRig;
            float savedPitch = camera.Pitch;
            float savedYaw = camera.Yaw;
            float savedDistance = camera.Distance;
            Vector3 savedFocus = camera.FocusOffset;
            camera.Pitch = 25;
            camera.Yaw = 215;
            camera.Distance = 8;
            camera.FocusOffset = center - camera.Target.position;
            camera.Snap();
            var folder = Path.Combine(game.QaDirectory, name);
            Directory.CreateDirectory(folder);
            double start = Time.realtimeSinceStartupAsDouble;
            var timestamps = new List<string> { "frame,elapsedSeconds" };
            // Ghi chuyển động simulation 1x; khoảng thời gian capture nằm ngoài lượt đo FPS.
            for (int frame = 0; frame < 36; frame++)
            {
                yield return new WaitForSecondsRealtime(.035f);
                string file = "frame-" + frame.ToString("D4") + ".png";
                timestamps.Add(file + "," + (Time.realtimeSinceStartupAsDouble - start)
                    .ToString("R", CultureInfo.InvariantCulture));
                CapturePhysicalFrame(Path.Combine(folder, file), 1280, 720);
            }
            File.WriteAllLines(Path.Combine(folder, "capture-timestamps.csv"), timestamps);
            camera.Pitch = savedPitch;
            camera.Yaw = savedYaw;
            camera.Distance = savedDistance;
            camera.FocusOffset = savedFocus;
            camera.Snap();
        }
    }
}
