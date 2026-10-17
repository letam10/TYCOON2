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
        readonly List<Lamp> pedestrianStops = new();
        readonly List<Lamp> pedestrianWalks = new();
        readonly List<GameObject> poles = new();
        readonly List<GameObject> arms = new();
        readonly List<Material> materials = new();
        CityTrafficSignalSystem signals;

        public int LampCount => groups.Count * 3;
        public int PoleCount => poles.Count;
        public int ArmCount => arms.Count;

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
                AddSupport(index, definition, staticSignalOffset);
                Vector3 head = definition.Position + new Vector3(staticSignalOffset, 4.05f, -.42f);
                AddHeadHousing(index, head);
                groups.Add(new SignalGroup
                {
                    Red = AddLamp(index, "Red", head + Vector3.up * .0f, new Color(1f, .12f, .08f)),
                    Amber = AddLamp(index, "Amber", head + Vector3.down * .44f,
                        new Color(1f, .62f, .08f)),
                    Green = AddLamp(index, "Green", head + Vector3.down * .88f,
                        new Color(.18f, 1f, .35f))
                });
                AddPedestrianSignal(index, definition, staticSignalOffset);
            }
            RefreshVisuals();
        }

        void AddSupport(int signalIndex, CityTrafficSignalDefinition definition, float offset)
        {
            Vector3 polePosition = definition.Position + new Vector3(offset, 2.2f, 0);
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "SignalPole_" + signalIndex;
            pole.transform.SetParent(transform, false);
            pole.transform.localPosition = polePosition;
            pole.transform.localScale = new Vector3(.18f, 2.2f, .18f);
            ConfigureStructure(pole, new Color(.12f, .14f, .14f));
            poles.Add(pole);

            GameObject poleBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            poleBase.name = "SignalPoleBase_" + signalIndex;
            poleBase.transform.SetParent(transform, false);
            poleBase.transform.localPosition = definition.Position + new Vector3(offset, .08f, 0);
            poleBase.transform.localScale = new Vector3(.38f, .08f, .38f);
            ConfigureStructure(poleBase, new Color(.08f, .1f, .1f));

            GameObject reflectiveBand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            reflectiveBand.name = "SignalPoleReflectiveBand_" + signalIndex;
            reflectiveBand.transform.SetParent(transform, false);
            reflectiveBand.transform.localPosition = definition.Position + new Vector3(offset, 1.55f, 0);
            reflectiveBand.transform.localScale = new Vector3(.205f, .11f, .205f);
            ConfigureStructure(reflectiveBand, new Color(.78f, .63f, .23f));

            Vector3 armPosition = definition.Position + new Vector3(offset * .5f, 4f, 0);
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "SignalArm_" + signalIndex;
            arm.transform.SetParent(transform, false);
            arm.transform.localPosition = armPosition;
            arm.transform.localScale = new Vector3(Mathf.Abs(offset), .12f, .12f);
            ConfigureStructure(arm, new Color(.12f, .14f, .14f));
            arms.Add(arm);

            GameObject armCap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            armCap.name = "SignalArmCap_" + signalIndex;
            armCap.transform.SetParent(transform, false);
            armCap.transform.localPosition = definition.Position + new Vector3(offset, 4f, 0);
            armCap.transform.localScale = new Vector3(.19f, .08f, .19f);
            ConfigureStructure(armCap, new Color(.08f, .1f, .1f));
        }

        void ConfigureStructure(GameObject root, Color color)
        {
            Collider collider = root.GetComponent<Collider>();
            if (collider)
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
            Renderer renderer = root.GetComponent<Renderer>();
            Material material = CreateMaterial(color, false);
            if (renderer && material)
                renderer.sharedMaterial = material;
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
            Material material = CreateMaterial(color, true);
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

        void AddPedestrianSignal(int signalIndex, CityTrafficSignalDefinition definition, float offset)
        {
            Vector3 head = definition.Position + new Vector3(offset, 1.72f, -.62f);
            GameObject housing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            housing.name = "PedestrianSignalHousing_" + signalIndex;
            housing.transform.SetParent(transform, false);
            housing.transform.localPosition = head;
            housing.transform.localScale = new Vector3(.3f, .62f, .16f);
            ConfigureStructure(housing, new Color(.045f, .055f, .06f));
            pedestrianStops.Add(AddPedestrianLamp(signalIndex, "Stop", head + Vector3.up * .13f,
                new Color(1f, .12f, .08f)));
            pedestrianWalks.Add(AddPedestrianLamp(signalIndex, "Walk", head + Vector3.down * .13f,
                new Color(.18f, 1f, .35f)));
        }

        Lamp AddPedestrianLamp(int signalIndex, string state, Vector3 position, Color color)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "PedestrianSignalLamp_" + signalIndex + "_" + state;
            root.transform.SetParent(transform, false);
            root.transform.localPosition = position + Vector3.forward * -.095f;
            root.transform.localScale = new Vector3(.16f, .12f, .025f);
            Collider collider = root.GetComponent<Collider>();
            if (collider)
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
            Renderer renderer = root.GetComponent<Renderer>();
            Material material = CreateMaterial(color, true);
            if (renderer && material)
                renderer.sharedMaterial = material;
            return new Lamp { Root = root, Renderer = renderer };
        }

        Material CreateMaterial(Color color, bool emissive)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader || shader.name.Contains("Error")) shader = Shader.Find("Standard");
            if (!shader || shader.name.Contains("Error"))
                return null;
            Material material = new Material(shader);
            material.name = emissive ? "TrafficLensMaterial" : "TrafficSignalMetalMaterial";
            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", emissive ? .18f : .72f);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", emissive ? .88f : .62f);
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", TownSurfaceTexture.Get(emissive ? "glass" : "metal"));
            if (emissive && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.5f);
            }
            materials.Add(material);
            return material;
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            RefreshVisuals();
        }

        void RefreshVisuals()
        {
            if (!signals) return;
            for (int index = 0; index < groups.Count; index++)
            {
                CitySignalPhase phase = signals.PhaseAt(
                    CityTrafficSignalSystem.Definitions[index].Position, false);
                SetActive(groups[index].Red, phase is CitySignalPhase.Red or CitySignalPhase.Walk);
                SetActive(groups[index].Amber, phase == CitySignalPhase.Amber);
                SetActive(groups[index].Green, phase == CitySignalPhase.Green);
                CitySignalPhase pedestrian = signals.PhaseAt(
                    CityTrafficSignalSystem.Definitions[index].Position, true);
                SetActive(pedestrianStops[index], pedestrian != CitySignalPhase.Walk);
                SetActive(pedestrianWalks[index], pedestrian == CitySignalPhase.Walk);
            }
        }

        void AddHeadHousing(int signalIndex, Vector3 head)
        {
            GameObject housing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            housing.name = "SignalHeadHousing_" + signalIndex;
            housing.transform.SetParent(transform, false);
            housing.transform.localPosition = head + Vector3.forward * .06f;
            housing.transform.localScale = new Vector3(.36f, 1.45f, .22f);
            ConfigureStructure(housing, new Color(.045f, .055f, .06f));
            GameObject backplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backplate.name = "SignalHeadBackplate_" + signalIndex;
            backplate.transform.SetParent(transform, false);
            backplate.transform.localPosition = head + Vector3.forward * .16f;
            backplate.transform.localScale = new Vector3(.5f, 1.62f, .07f);
            ConfigureStructure(backplate, new Color(.08f, .1f, .1f));
            GameObject upperReflector = GameObject.CreatePrimitive(PrimitiveType.Cube);
            upperReflector.name = "SignalHeadReflector_" + signalIndex + "_Upper";
            upperReflector.transform.SetParent(transform, false);
            upperReflector.transform.localPosition = head + Vector3.up * .84f + Vector3.forward * .105f;
            upperReflector.transform.localScale = new Vector3(.42f, .06f, .08f);
            ConfigureStructure(upperReflector, new Color(.82f, .67f, .25f));
            GameObject lowerReflector = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lowerReflector.name = "SignalHeadReflector_" + signalIndex + "_Lower";
            lowerReflector.transform.SetParent(transform, false);
            lowerReflector.transform.localPosition = head + Vector3.down * .84f + Vector3.forward * .105f;
            lowerReflector.transform.localScale = new Vector3(.42f, .06f, .08f);
            ConfigureStructure(lowerReflector, new Color(.82f, .67f, .25f));
            for (int lamp = 0; lamp < 3; lamp++)
            {
                GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visor.name = "SignalVisor_" + signalIndex + "_" + lamp;
                visor.transform.SetParent(transform, false);
                visor.transform.localPosition = head + Vector3.down * (.1f + lamp * .44f) + Vector3.forward * -.09f;
                visor.transform.localScale = new Vector3(.46f, .12f, .2f);
                ConfigureStructure(visor, new Color(.025f, .03f, .035f));
            }
        }

        static void SetActive(Lamp lamp, bool active)
        {
            if (lamp.Renderer)
                lamp.Renderer.enabled = active;
            if (lamp.Light && Application.isPlaying)
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
