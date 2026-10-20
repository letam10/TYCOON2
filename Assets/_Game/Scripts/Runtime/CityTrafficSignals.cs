using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public enum CitySignalPhase
    {
        Green,
        Amber,
        Red,
        Walk
    }

    [Serializable]
    public struct CityTrafficSignalDefinition
    {
        public string Id;
        public Vector3 Position;
        public float CycleOffset;

        public CityTrafficSignalDefinition(string id, Vector3 position, float cycleOffset)
        {
            Id = id;
            Position = position;
            CycleOffset = cycleOffset;
        }
    }

    public sealed class CityTrafficSignalSystem : MonoBehaviour
    {
        public const float GreenSeconds = 8f;
        public const float AmberSeconds = 2f;
        public const float RedSeconds = 8f;
        public const float DetectionRadius = 13f;

        static readonly CityTrafficSignalDefinition[] Defaults = BuildDefinitions();

        static CityTrafficSignalDefinition[] BuildDefinitions()
        {
            var definitions = new CityTrafficSignalDefinition[]
            {
            new("west_south", new(-188.8f, 0, -105.8f), 0),
            new("west_mid", new(-188.8f, 0, 35.2f), 4),
            new("west_north", new(-188.8f, 0, 161.2f), 8),
            new("east_south", new(196.2f, 0, -105.8f), 4),
            new("east_mid", new(196.2f, 0, 35.2f), 8),
            new("east_north", new(196.2f, 0, 161.2f), 0)
            };
            for (int index = 0; index < definitions.Length; index++)
            {
                CityTrafficSignalDefinition definition = definitions[index];
                if (WorldRouteCatalog.TryGetNearestPoint("highway-ring", definition.Position,
                    -1, out Vector3 nearest, out _))
                {
                    // Giữ khoảng lùi 6,2 m từ tim đường: nửa đường 4 m + lề tín hiệu 2,2 m.
                    Vector3 outward = (definition.Position - nearest).normalized;
                    definition.Position = nearest + outward * 6.2f;
                    if (WorldRouteCatalog.TryGetNearestPoint("highway-ring", definition.Position,
                        -1, out Vector3 support, out float distance) && distance > 0f)
                    {
                        // Góc lõm có thể đổi cạnh gần nhất; tính bù đúng theo pháp tuyến cạnh đó.
                        float normalAmount = Vector3.Dot((definition.Position - support).normalized, outward);
                        if (normalAmount > 0f)
                            definition.Position += outward * ((6.2f - distance) / normalAmount);
                    }
                    definitions[index] = definition;
                }
            }
            return definitions;
        }

        public static CityTrafficSignalSystem Instance { get; private set; }
        public static IReadOnlyList<CityTrafficSignalDefinition> Definitions => Defaults;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static CityTrafficSignalSystem Build(Transform world)
        {
            if (world == null) return null;
            var existing = world.GetComponentInChildren<CityTrafficSignalSystem>();
            if (existing)
            {
                CityTrafficSignalVisual.Build(existing.transform.parent, existing);
                return existing;
            }
            var root = new GameObject("CityTrafficSignals").transform;
            root.SetParent(world, false);
            var system = root.gameObject.AddComponent<CityTrafficSignalSystem>();
            CityTrafficSignalVisual.Build(world, system);
            return system;
        }

        public CitySignalPhase PhaseAt(Vector3 point, bool pedestrian)
        {
            if (!TryNearest(point, out var signal, out _))
                return CitySignalPhase.Green;
            float cycle = GreenSeconds + AmberSeconds + RedSeconds;
            float elapsed = Mathf.Repeat(Time.time + signal.CycleOffset, cycle);
            if (elapsed < GreenSeconds)
                return pedestrian ? CitySignalPhase.Walk : CitySignalPhase.Green;
            if (elapsed < GreenSeconds + AmberSeconds)
                return pedestrian ? CitySignalPhase.Red : CitySignalPhase.Amber;
            return pedestrian ? CitySignalPhase.Walk : CitySignalPhase.Red;
        }

        public bool ShouldStopVehicle(Vector3 point)
        {
            return PhaseAt(point, false) != CitySignalPhase.Green;
        }

        public bool CanCross(Vector3 point)
        {
            return PhaseAt(point, true) == CitySignalPhase.Walk;
        }

        public bool ShouldWaitPedestrian(Vector3 point)
        {
            return TryNearest(point, out _, out float distance) &&
                distance <= DetectionRadius && !CanCross(point);
        }

        public string DescribePhases()
        {
            var lines = new List<string>(Defaults.Length);
            for (int index = 0; index < Defaults.Length; index++)
            {
                CityTrafficSignalDefinition definition = Defaults[index];
                CitySignalPhase vehicle = PhaseAt(definition.Position, false);
                CitySignalPhase pedestrian = PhaseAt(definition.Position, true);
                lines.Add(definition.Id + " vehicle=" + vehicle
                    + " pedestrian=" + pedestrian
                    + " stopVehicle=" + (vehicle != CitySignalPhase.Green)
                    + " canWalk=" + (pedestrian == CitySignalPhase.Walk));
            }
            return string.Join("\n", lines);
        }

        public bool TryNearest(Vector3 point, out CityTrafficSignalDefinition signal, out float distance)
        {
            signal = default;
            distance = float.PositiveInfinity;
            for (int index = 0; index < Defaults.Length; index++)
            {
                float candidate = Vector2.Distance(
                    new(point.x, point.z), new(Defaults[index].Position.x, Defaults[index].Position.z));
                if (candidate >= distance || candidate > DetectionRadius) continue;
                signal = Defaults[index];
                distance = candidate;
            }
            return distance < float.PositiveInfinity;
        }
    }
}
