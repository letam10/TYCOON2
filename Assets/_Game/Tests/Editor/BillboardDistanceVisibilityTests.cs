using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class BillboardDistanceVisibilityTests
    {
        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        [TestCase(2560, 1440)]
        [TestCase(3840, 2160)]
        public void DistantWorldLabelsHideAndReappearOnTheFirstCloseFrame(int width, int height)
        {
            var root = new GameObject("DistantLabelTest");
            var lens = new GameObject("Camera", typeof(Camera));
            lens.transform.SetParent(root.transform);
            var camera = lens.GetComponent<Camera>();
            var previous = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Where(x => x.CompareTag("MainCamera")).ToArray();
            foreach (var existing in previous) existing.tag = "Untagged";
            lens.tag = "MainCamera";
            var texture = new RenderTexture(width, height, 16);
            camera.targetTexture = texture;
            var textObject = new GameObject("Quantity", typeof(TextMesh));
            textObject.transform.SetParent(root.transform);
            textObject.transform.position = Vector3.forward * 200;
            var label = textObject.GetComponent<TextMesh>();
            var font = Font.CreateDynamicFontFromOSFont("Arial", 128);
            label.font = font;
            label.fontSize = 128;
            label.text = "48/48";
            var billboard = textObject.AddComponent<BillboardLabel>();
            typeof(BillboardLabel).GetMethod("Awake",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(billboard, null);
            var renderer = textObject.GetComponent<MeshRenderer>();
            try
            {
                billboard.RefreshNow();
                Assert.That(renderer.enabled, Is.False, "Far overview must not inflate detail labels.");
                camera.transform.position = Vector3.forward * 190;
                billboard.RefreshNow();
                Assert.That(renderer.enabled, Is.True, "No delayed reappearance after camera movement.");
                Assert.That(label.text, Is.EqualTo("48/48"));
                float worldPerPixel = 20 * Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad) / height;
                float textHeight = label.fontSize * label.characterSize * .1f;
                Assert.That(textHeight / worldPerPixel, Is.LessThanOrEqualTo(56.01f));
                camera.transform.position = Vector3.zero;
                billboard.RefreshNow();
                Assert.That(renderer.enabled, Is.False);
            }
            finally
            {
                foreach (var existing in previous) existing.tag = "MainCamera";
                camera.targetTexture = null;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(font);
            }
        }
    }
}
