using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator Capture(string name)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) yield break;
            yield return null;
            CapturePhysicalFrame(Path.Combine(game.QaDirectory, name));
            report.screenshots.Add(name);
        }

        void CapturePhysicalFrame(string path, int width = 0, int height = 0)
        {
            Action<Camera, Canvas[], int, int> inspect = physicalResolutionCapture == null
                ? null
                : new Action<Camera, Canvas[], int, int>(MeasurePhysicalResolution);
            QaRenderedFrame.Write(path, width, height, inspect);
        }
    }
}
