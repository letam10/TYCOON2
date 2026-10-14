using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tycoon.Tests
{
    public sealed class ShadowSettingsTests
    {
        RenderPipelineAsset saved;
        UniversalRenderPipelineAsset source;

        [SetUp]
        public void SetUp()
        {
            GameSettingsGraphics.Release();
            saved = QualitySettings.renderPipeline;
            source = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            QualitySettings.renderPipeline = source;
        }

        [TearDown]
        public void TearDown()
        {
            GameSettingsGraphics.Release();
            QualitySettings.renderPipeline = saved;
        }

        [TestCase(GameGraphicsPreset.Low)]
        [TestCase(GameGraphicsPreset.Balanced)]
        [TestCase(GameGraphicsPreset.High)]
        public void NormalZoomAndSurroundingObjectsRemainBeforeShadowFade(GameGraphicsPreset quality)
        {
            GameSettingsGraphics.Apply(new GameSettingsData { quality = quality, renderScale = 1 });
            var runtime = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            Assert.That(runtime, Is.Not.SameAs(source));
            Assert.That(runtime.shadowCascadeCount, Is.EqualTo(1), "No internal cascade edge can cross the player.");
            float fadeStart = runtime.shadowDistance * (1 - runtime.cascadeBorder);
            Assert.That(fadeStart, Is.GreaterThan(FollowCamera.MaximumDistance + 5));
            Assert.That(runtime.mainLightShadowmapResolution, Is.GreaterThanOrEqualTo(1024));
        }

        [Test]
        public void ChangingGraphicsKeepsBorrowedPipelineAndRestoresItsIdentity()
        {
            float distance = source.shadowDistance;
            float border = source.cascadeBorder;
            int cascades = source.shadowCascadeCount;
            int resolution = source.mainLightShadowmapResolution;
            GameSettingsGraphics.Apply(new GameSettingsData { quality = GameGraphicsPreset.High, renderScale = 1 });
            GameSettingsGraphics.Apply(new GameSettingsData { quality = GameGraphicsPreset.Low, renderScale = 1 });
            Assert.That(source.shadowDistance, Is.EqualTo(distance));
            Assert.That(source.cascadeBorder, Is.EqualTo(border));
            Assert.That(source.shadowCascadeCount, Is.EqualTo(cascades));
            Assert.That(source.mainLightShadowmapResolution, Is.EqualTo(resolution));
            GameSettingsGraphics.Release();
            Assert.That(QualitySettings.renderPipeline, Is.SameAs(source));
        }
    }
}
