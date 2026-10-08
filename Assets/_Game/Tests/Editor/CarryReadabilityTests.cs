using NUnit.Framework;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class CarryReadabilityTests
    {
        GameObject root;

        [SetUp]
        public void SetUp() => root = new GameObject("CarryReadability");

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [TestCase("48/48", 1)]
        [TestCase("5000000000 xu", 1)]
        [TestCase("Thiếu nguyên liệu\nRút tiền tại két", 2)]
        public void WorldTextIsBoundedImmediatelyAndAfterCameraMoves(string text, int lines)
        {
            var cameraObject = new GameObject("ReadabilityCamera");
            cameraObject.transform.SetParent(root.transform);
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.targetTexture = new RenderTexture(1920, 1080, 16);
            var previousCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Where(x => x != camera && x.CompareTag("MainCamera")).ToArray();
            foreach (var existing in previousCameras) existing.tag = "Untagged";
            var labelObject = new GameObject("Count");
            labelObject.transform.SetParent(root.transform);
            labelObject.transform.localPosition = new Vector3(0, 0, 5);
            labelObject.transform.localScale = Vector3.one * 20;
            var label = labelObject.AddComponent<TextMesh>();
            label.font = Font.CreateDynamicFontFromOSFont("Arial", 128);
            label.fontSize = 128;
            label.characterSize = .1f;
            label.text = text;
            var billboard = labelObject.AddComponent<BillboardLabel>();
            typeof(BillboardLabel).GetMethod("Awake",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(billboard, null);
            try
            {
                Assert.That(Camera.main, Is.SameAs(camera));
                CheckHeight(camera, label, lines);
                camera.transform.position = new Vector3(0, 0, 4.5f);
                billboard.RefreshNow();
                CheckHeight(camera, label, lines);
            }
            finally
            {
                foreach (var existing in previousCameras) existing.tag = "MainCamera";
                var texture = camera.targetTexture;
                camera.targetTexture = null;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(label.font);
            }
        }

        static void CheckHeight(Camera camera, TextMesh text, int lines)
        {
            float distance = Vector3.Dot(text.transform.position - camera.transform.position,
                camera.transform.forward);
            float worldPerPixel = 2 * distance * Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad)
                / camera.pixelHeight;
            float height = text.fontSize * text.characterSize * .1f * text.transform.lossyScale.y * lines;
            Assert.That(height / worldPerPixel, Is.LessThanOrEqualTo(28 * lines + .01f));
            Assert.That(text.GetComponent<MeshRenderer>().enabled, Is.True);
        }

        [TestCase("carrot")]
        [TestCase("wheat")]
        [TestCase("beef")]
        [TestCase("milk")]
        [TestCase("meal")]
        [TestCase("cloth")]
        public void FullHandStackStaysCompactWithAllModelsVisible(string id)
        {
            var inventory = new Inventory(48, true);
            inventory.TryAdd(id, 48);
            var stack = root.AddComponent<InventoryStack>();
            stack.Inventory = inventory;
            stack.Pool = new ItemPool(root.transform);
            stack.HeldLayout = true;
            stack.Columns = 4;
            stack.Rows = 3;
            stack.Scale = .85f;
            stack.LayerHeight = .04f;
            stack.Spacing = .14f;
            stack.Refresh();
            Assert.That(stack.VisibleCount, Is.EqualTo(48));
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            Assert.That(bounds.size.y, Is.LessThan(1.8f), id);
            Assert.That(bounds.size.x, Is.LessThan(1.9f), id);
            Assert.That(bounds.size.z, Is.LessThan(1.4f), id);
        }
    }
}
