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

        static readonly CityTrafficSignalDefinition[] Defaults =
        {
            new("west_south", new(-188.8f, 0, -105.8f), 0),
            new("west_mid", new(-188.8f, 0, 35.2f), 4),
            new("west_north", new(-188.8f, 0, 161.2f), 8),
            new("east_south", new(196.2f, 0, -105.8f), 4),
            new("east_mid", new(196.2f, 0, 35.2f), 8),
            new("east_north", new(196.2f, 0, 161.2f), 0)
        };

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
