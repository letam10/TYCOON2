using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    [DisallowMultipleComponent]
    public sealed class CityTrafficSignalVisual : MonoBehaviour
    {
        sealed class Lamp
        {
            public GameObject Root;
            public Renderer Renderer;
            public Light Light;
        }

        sealed class SignalGroup
        {
            public Lamp Red;
            public Lamp Amber;
            public Lamp Green;
        }

        readonly List<SignalGroup> groups = new();
        readonly List<Material> materials = new();
        CityTrafficSignalSystem signals;

        public int LampCount => groups.Count * 3;

        public static CityTrafficSignalVisual Build(
            Transform parent,
            CityTrafficSignalSystem signalSystem)
        {
            if (!parent || !signalSystem)
                return null;
            CityTrafficSignalVisual existing = parent.GetComponentInChildren<CityTrafficSignalVisual>(true);
            if (existing)
                return existing;

            Transform root = new GameObject("CityTrafficSignalVisuals").transform;
            root.SetParent(parent, false);
            CityTrafficSignalVisual visual = root.gameObject.AddComponent<CityTrafficSignalVisual>();
            visual.signals = signalSystem;
            visual.BuildLamps();
            return visual;
        }

        void BuildLamps()
        {
            for (int index = 0; index < CityTrafficSignalSystem.Definitions.Count; index++)
            {
                CityTrafficSignalDefinition definition = CityTrafficSignalSystem.Definitions[index];
                float staticSignalOffset = definition.Position.x < 0f ? 11f : -11f;
                Vector3 head = definition.Position + new Vector3(staticSignalOffset, 4.05f, -.42f);
                groups.Add(new SignalGroup
                {
                    Red = AddLamp(index, "Red", head + Vector3.up * .0f, new Color(1f, .12f, .08f)),
                    Amber = AddLamp(index, "Amber", head + Vector3.down * .44f,
                        new Color(1f, .62f, .08f)),
                    Green = AddLamp(index, "Green", head + Vector3.down * .88f,
                        new Color(.18f, 1f, .35f))
                });
            }
        }

        Lamp AddLamp(int signalIndex, string colorName, Vector3 position, Color color)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "SignalLamp_" + signalIndex + "_" + colorName;
            root.transform.SetParent(transform, false);
            root.transform.localPosition = position;
            root.transform.localScale = Vector3.one * .18f;
            Collider collider = root.GetComponent<Collider>();
            if (collider)
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }

            Renderer renderer = root.GetComponent<Renderer>();
            Material material = CreateMaterial(color);
            if (renderer && material)
                renderer.sharedMaterial = material;

            Light light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 3.5f;
            light.color = color;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            return new Lamp { Root = root, Renderer = renderer, Light = light };
        }

        Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader || shader.name.Contains("Error")) shader = Shader.Find("Standard");
            if (!shader || shader.name.Contains("Error"))
                return null;
            Material material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.5f);
            }
            materials.Add(material);
            return material;
        }

        void Update()
        {
            if (!signals)
                return;
            for (int index = 0; index < groups.Count; index++)
            {
                CitySignalPhase phase = signals.PhaseAt(
                    CityTrafficSignalSystem.Definitions[index].Position, false);
                SetActive(groups[index].Red, phase is CitySignalPhase.Red or CitySignalPhase.Walk);
                SetActive(groups[index].Amber, phase == CitySignalPhase.Amber);
                SetActive(groups[index].Green, phase == CitySignalPhase.Green);
            }
        }

        static void SetActive(Lamp lamp, bool active)
        {
            if (lamp.Renderer)
                lamp.Renderer.enabled = active;
            if (lamp.Light)
            {
                lamp.Light.enabled = active;
                lamp.Light.intensity = active ? 1.6f : 0f;
            }
        }

        void OnDestroy()
        {
            foreach (Material material in materials)
            {
                if (!material)
                    continue;
                if (Application.isPlaying)
                    Destroy(material);
                else
                    DestroyImmediate(material);
            }
            materials.Clear();
        }
    }
}
