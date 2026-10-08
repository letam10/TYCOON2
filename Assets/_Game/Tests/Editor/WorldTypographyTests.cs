using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class WorldTypographyTests
    {
        [Test]
        public void VietnameseNoticesAndCashDigitsHaveRealGlyphs()
        {
            var catalog = Resources.Load<GameCatalog>("GameCatalog");
            const string sample = "Cà rốt • Rút tiền tại két • Đã nâng cấp • Tay đầy • 48/48 • 5.000.000.000 xu";
            catalog.font.RequestCharactersInTexture(sample, 128);
            foreach (char letter in sample)
                if (!char.IsWhiteSpace(letter))
                    Assert.That(catalog.font.GetCharacterInfo(letter, out _, 128), Is.True, letter.ToString());
            Assert.That(WorldTypography.Material(catalog.font).shader.name, Is.EqualTo("Tycoon/WorldLabel"));
            Assert.That(ShaderUtil.ShaderHasError(WorldTypography.Material(catalog.font).shader), Is.False);
        }

        [Test]
        public void LabelsStayReadableAt720AndUseTheSameSemanticSize()
        {
            var cameraObject = new GameObject("TypographyCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            var target = new RenderTexture(1280, 720, 0);
            try
            {
                camera.targetTexture = target;
                Assert.That(WorldTypography.LinePixels(camera, true), Is.InRange(17, 20));
                Assert.That(WorldTypography.LinePixels(camera, false), Is.InRange(15, 18));
            }
            finally
            {
                camera.targetTexture = null;
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
