using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tycoon
{
    public static class TownLighting
    {
        public static void Apply(Transform world)
        {
            var sun = new GameObject("AfternoonSun");
            sun.transform.SetParent(world, false);
            sun.transform.rotation = Quaternion.Euler(48, -32, 0);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1, .95f, .84f);
            light.intensity = 1.65f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = .82f;
            light.shadowBias = .025f;
            light.shadowNormalBias = .15f;
            RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.58f, .67f, .75f);
            RenderSettings.ambientEquatorColor = new Color(.39f, .42f, .38f);
            RenderSettings.ambientGroundColor = new Color(.21f, .24f, .19f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.67f, .74f, .75f);
            RenderSettings.fogStartDistance = 90;
            RenderSettings.fogEndDistance = 210;
            var root = new GameObject("TownColorGrade");
            root.transform.SetParent(world, false);
            var volume = root.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "RuntimeTownLighting";
            volume.sharedProfile = profile;
            var tone = profile.Add<Tonemapping>();
            tone.mode.Override(TonemappingMode.Neutral);
            var color = profile.Add<ColorAdjustments>();
            color.postExposure.Override(-.2f);
            color.contrast.Override(10);
            color.saturation.Override(-10);
            var bloom = profile.Add<Bloom>();
            bloom.threshold.Override(1.5f);
            bloom.intensity.Override(.12f);
            root.AddComponent<TownLightingLifetime>().Profile = profile;
        }
    }

    public sealed class TownLightingLifetime : MonoBehaviour
    {
        public VolumeProfile Profile;
        void OnDestroy()
        {
            if (Profile) Destroy(Profile);
        }
    }
}
