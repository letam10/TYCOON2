using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityFrameRecordingTests
    {
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [TestCase(160000, .005f)]
        [TestCase(250000, .004f)]
        public void ALongFastTourRetainsEveryFrameAndTheEntireDuration(int frames, float seconds)
        {
            var root = new GameObject("LongCityFrameRecording");
            root.SetActive(false);
            var session = root.AddComponent<CityPlaySession>();
            var type = typeof(CityPlaySession);
            var record = type.GetMethod("RecordFrameTime", PrivateInstance);
            var arguments = new object[] { seconds };
            try
            {
                for (int index = 0; index < frames; index++) record.Invoke(session, arguments);
                int captured = (int)type.GetField("frameCount", PrivateInstance).GetValue(session);
                double elapsed = (double)type.GetField("elapsed", PrivateInstance).GetValue(session);
                Assert.That(captured, Is.EqualTo(frames), "The final city districts must remain in the sample.");
                Assert.That(elapsed, Is.EqualTo(frames * (double)seconds).Within(.001),
                    "FPS must use the entire recording duration, even above150000frames.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
