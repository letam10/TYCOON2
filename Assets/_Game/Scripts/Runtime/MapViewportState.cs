using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Tycoon
{
    /// <summary>
    /// Deterministic state for a map viewport.
    /// </summary>
    public sealed class MapViewportState
    {
        public const float MinZoom = 0.65f;
        public const float MaxZoom = 3.5f;

        readonly List<string> enabledKinds = new List<string>();
        readonly IReadOnlyCollection<string> readonlyEnabledKinds;
        Vector3 center;
        float zoom;
        float rotationDegrees;
        string selectedPoiId;

        public MapViewportState()
        {
            readonlyEnabledKinds = new ReadOnlyCollection<string>(enabledKinds);
            center = Vector3.zero;
            zoom = 1f;
            rotationDegrees = 0f;
            selectedPoiId = null;
        }

        public Vector3 Center => center;

        public float Zoom => zoom;

        public float RotationDegrees => rotationDegrees;

        public string SelectedPoiId => selectedPoiId;

        public IReadOnlyCollection<string> EnabledKinds => readonlyEnabledKinds;

        public void SetCenter(Vector3 worldPosition)
        {
            if (!IsFinite(worldPosition))
                return;

            center = worldPosition;
        }

        public void FocusPlayer(Vector3 playerPosition)
        {
            SetCenter(playerPosition);
        }

        public void ZoomBy(float amount)
        {
            if (!IsFinite(amount))
                return;

            zoom = Mathf.Clamp(zoom + amount, MinZoom, MaxZoom);
        }

        public void Pan(Vector2 normalizedDelta)
        {
            if (!IsFinite(normalizedDelta))
                return;

            Vector3 nextCenter = center + new Vector3(normalizedDelta.x, 0f, normalizedDelta.y);
            if (IsFinite(nextCenter))
                center = nextCenter;
        }

        public void RotateBy(float degrees)
        {
            if (!IsFinite(degrees))
                return;

            rotationDegrees = NormalizeDegrees((double)rotationDegrees + degrees);
        }

        public void ResetNorth()
        {
            rotationDegrees = 0f;
        }

        public void Select(string poiId)
        {
            selectedPoiId = string.IsNullOrEmpty(poiId) ? null : poiId;
        }

        public void ToggleKind(string kind)
        {
            if (string.IsNullOrEmpty(kind))
                return;

            int index = IndexOfKind(kind);
            if (index >= 0)
            {
                enabledKinds.RemoveAt(index);
                return;
            }

            enabledKinds.Add(kind);
        }

        public bool IsKindEnabled(string kind)
        {
            if (string.IsNullOrEmpty(kind))
                return false;

            return enabledKinds.Count == 0 || IndexOfKind(kind) >= 0;
        }

        public bool IsFiniteAndValid()
        {
            if (!IsFinite(center) || !IsFinite(zoom) || zoom < MinZoom || zoom > MaxZoom)
                return false;

            if (!IsFinite(rotationDegrees) || rotationDegrees < -180f || rotationDegrees >= 180f)
                return false;

            if (selectedPoiId != null && selectedPoiId.Length == 0)
                return false;

            for (int index = 0; index < enabledKinds.Count; index++)
                if (string.IsNullOrEmpty(enabledKinds[index]) || IndexOfKind(enabledKinds[index], index) >= 0)
                    return false;

            return true;
        }

        int IndexOfKind(string kind)
        {
            return IndexOfKind(kind, -1);
        }

        int IndexOfKind(string kind, int ignoredIndex)
        {
            for (int index = 0; index < enabledKinds.Count; index++)
            {
                if (index == ignoredIndex)
                    continue;

                if (string.Equals(enabledKinds[index], kind, StringComparison.Ordinal))
                    return index;
            }

            return -1;
        }

        static float NormalizeDegrees(double degrees)
        {
            double normalized = degrees % 360d;
            if (normalized >= 180d)
                normalized -= 360d;
            if (normalized < -180d)
                normalized += 360d;
            return (float)normalized;
        }

        static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
