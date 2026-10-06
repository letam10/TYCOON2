using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tycoon
{
    // Chỉ diễn hoạt model theo state hiện có; không gửi command hoặc sửa inventory.
    public sealed class CozyMachineMotion : MonoBehaviour
    {
        readonly struct Part
        {
            public readonly Transform Node;
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly int Kind;
            public Part(Transform node, int kind)
            { Node = node; Position = node.localPosition; Rotation = node.localRotation; Kind = kind; }
        }
        readonly struct Content
        {
            public readonly Transform Node;
            public readonly bool Ingredient;
            public Content(Transform node, bool ingredient) { Node = node; Ingredient = ingredient; }
        }
        readonly List<Part> parts = new();
        readonly List<Content> contents = new();
        MachineStation station;
        LineRenderer[] steam;
        Vector3 steamSource;
        float clock;

        public void Initialize(MachineStation owner, Transform model, bool hot, Vector3 source)
        {
            station = owner; steamSource = source;
            foreach (var node in model.GetComponentsInChildren<Transform>())
            {
                int kind = node.name switch { "Piston" => 1, "Nozzle" => 2, "Shuttle" => 3, "Pedal" => 4, "Mix" => 5, _ => 0 };
                if (kind != 0) parts.Add(new(node, kind));
                if (node.name is "Grain" or "Mix") contents.Add(new(node, true));
                else if (node.name == station.Recipe.output || node.name == "WovenCloth") contents.Add(new(node, false));
            }
            if (!hot) return;
            steam = new LineRenderer[2];
            for (int i = 0; i < steam.Length; i++)
            {
                var root = new GameObject("WorkSteam"); root.transform.SetParent(transform, false);
                var line = root.AddComponent<LineRenderer>();
                line.sharedMaterial = Art.Material("#DCE3CB"); line.useWorldSpace = false;
                line.positionCount = 6; line.widthMultiplier = .025f;
                line.widthCurve = new AnimationCurve(new Keyframe(0, .35f), new Keyframe(.35f, 1), new Keyframe(1, .04f));
                line.alignment = LineAlignment.View; line.generateLightingData = true;
                line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false; line.enabled = false;
                steam[i] = line;
            }
        }

        void LateUpdate()
        {
            if (!station || station.Recipe == null) return;
            bool visible = station.IsUnlocked;
            bool running = visible && GameSession.Instance.CanSimulate && !station.Broken && station.Phase == MachinePhase.Operating;
            clock = running ? clock + Time.deltaTime : 0;
            float blend = 1 - Mathf.Exp(-14 * Time.deltaTime);
            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i]; if (!part.Node) continue;
                float wave = running ? Mathf.Sin(clock * 6 + i * .9f) : 0;
                Vector3 offset = part.Kind == 1 ? Vector3.down * (running ? .025f * (1 + wave) : 0) :
                    part.Kind == 2 ? Vector3.down * (running ? .018f * (1 + wave) : 0) :
                    part.Kind == 3 ? Vector3.right * (.065f * wave) : part.Kind == 5 ? Vector3.up * (.006f * wave) : Vector3.zero;
                part.Node.localPosition = Vector3.Lerp(part.Node.localPosition, part.Position + offset, blend);
                part.Node.localRotation = Quaternion.Slerp(part.Node.localRotation,
                    part.Rotation * Quaternion.Euler(part.Kind == 4 ? 3 * wave : 0, 0, 0), blend);
            }
            bool ingredients = station.Input.Total > 0;
            bool output = station.Inventory != null && station.Inventory.Count(station.Recipe.output) > 0;
            foreach (var content in contents)
            {
                bool show = visible && (running || (content.Ingredient ? ingredients : output));
                if (content.Node && content.Node.gameObject.activeSelf != show) content.Node.gameObject.SetActive(show);
            }
            if (steam == null) return;
            for (int i = 0; i < steam.Length; i++)
            {
                steam[i].enabled = running;
                if (!running) continue;
                for (int point = 0; point < 6; point++)
                {
                    float t = point / 5f;
                    steam[i].SetPosition(point, steamSource + new Vector3((i - .5f) * .16f + Mathf.Sin(clock * 2 + t * 5 + i) * .04f * t,
                        t * .47f, Mathf.Cos(clock * 1.4f + t * 4 + i) * .035f * t));
                }
            }
        }

        void OnDisable()
        {
            foreach (var part in parts) if (part.Node) { part.Node.localPosition = part.Position; part.Node.localRotation = part.Rotation; }
            if (steam != null) foreach (var line in steam) if (line) line.enabled = false;
        }
    }
}
