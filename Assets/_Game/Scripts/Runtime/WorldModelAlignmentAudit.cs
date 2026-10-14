using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public enum WorldAlignmentRecordKind
    {
        Model,
        Label
    }

    public class Record
    {
        public string Name;
        public string Id;
        public WorldAlignmentRecordKind Kind;
        public bool Passed;
        public string Error;

        public bool Success => Passed;
        public bool IsValid => Passed;
        public string Message => Error;

        public Record()
        {
            Name = string.Empty;
            Id = string.Empty;
            Error = string.Empty;
        }

        public Record(string name, WorldAlignmentRecordKind kind)
        {
            Name = name ?? string.Empty;
            Id = Name;
            Kind = kind;
            Error = string.Empty;
        }
    }

    public sealed class ModelAlignmentRecord : Record
    {
        public bool HasRenderer;
        public bool TransformFinite;
        public bool ScaleFinite;
        public bool YawFinite;
        public bool Grounded;
        public bool PivotInsideFootprint;
        public Bounds WorldBounds;
        public Vector3 Pivot;
        public Vector3 Scale;
        public float Yaw;
        public float GroundOffset;

        public ModelAlignmentRecord(string name)
            : base(name, WorldAlignmentRecordKind.Model)
        {
        }
    }

    public sealed class LabelAlignmentRecord : Record
    {
        public bool IsFinite;
        public bool InsideFootprint;
        public bool WithinDistance;
        public bool BillboardConfigured;
        public bool FacingFinite;
        public Bounds Footprint;
        public Vector3 Position;
        public float FootprintDistance;

        public LabelAlignmentRecord(string name)
            : base(name, WorldAlignmentRecordKind.Label)
        {
        }
    }

    public static class WorldModelAlignmentAudit
    {
        const float Epsilon = .0001f;
        const float ScaleEpsilon = .000001f;

        public static ModelAlignmentRecord CheckModel(GameObject model, float groundTolerance)
        {
            string name = model ? model.name : "<null-model>";
            var record = new ModelAlignmentRecord(name);
            var errors = new List<string>();
            bool toleranceValid = IsFinite(groundTolerance) && groundTolerance >= 0;
            if (!toleranceValid) errors.Add("ground tolerance must be finite and non-negative");
            if (!model)
            {
                errors.Add("model is null");
                record.Error = string.Join("; ", errors);
                record.Passed = false;
                return record;
            }

            Transform root = model.transform;
            record.Pivot = root.position;
            record.Scale = root.lossyScale;
            record.Yaw = root.localEulerAngles.y;
            record.TransformFinite = IsFinite(root.position) && IsFinite(root.rotation);
            record.ScaleFinite = IsFinite(root.localScale) && IsFinite(root.lossyScale) &&
                HasUsableScale(root.lossyScale);
            record.YawFinite = IsFinite(record.Yaw) && IsFinite(root.localRotation);
            if (!record.TransformFinite) errors.Add("transform contains a non-finite position or rotation");
            if (!record.ScaleFinite) errors.Add("scale is non-finite or zero");
            if (!record.YawFinite) errors.Add("yaw is non-finite");

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Vector3 minimum = Vector3.zero;
            Vector3 maximum = Vector3.zero;
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer candidate = renderers[index];
                if (!candidate) continue;
                Bounds candidateBounds = candidate.bounds;
                if (!IsFinite(candidateBounds)) continue;
                if (!hasBounds)
                {
                    minimum = candidateBounds.min;
                    maximum = candidateBounds.max;
                    hasBounds = true;
                }
                else
                {
                    minimum = Vector3.Min(minimum, candidateBounds.min);
                    maximum = Vector3.Max(maximum, candidateBounds.max);
                }
            }

            record.HasRenderer = hasBounds;
            if (!hasBounds)
            {
                errors.Add("no finite renderer bounds found");
            }
            else
            {
                record.WorldBounds = new Bounds((minimum + maximum) * .5f, maximum - minimum);
                record.GroundOffset = record.WorldBounds.min.y - record.Pivot.y;
                record.Grounded = toleranceValid && Mathf.Abs(record.GroundOffset) <= groundTolerance;
                record.PivotInsideFootprint = IsInsideHorizontal(record.Pivot, record.WorldBounds,
                    toleranceValid ? groundTolerance : 0);
                if (!record.Grounded)
                {
                    errors.Add("renderer bounds do not contact the root pivot at ground level");
                }
                if (!record.PivotInsideFootprint)
                {
                    errors.Add("root pivot is outside the renderer footprint");
                }
            }

            record.Error = string.Join("; ", errors);
            record.Passed = errors.Count == 0;
            return record;
        }

        public static LabelAlignmentRecord CheckLabel(Transform label, Bounds footprint, float maxDistance)
        {
            string name = label ? label.name : "<null-label>";
            var record = new LabelAlignmentRecord(name);
            var errors = new List<string>();
            record.Footprint = footprint;
            bool distanceValid = IsFinite(maxDistance) && maxDistance >= 0;
            if (!distanceValid) errors.Add("maximum label distance must be finite and non-negative");
            if (!label)
            {
                errors.Add("label transform is null");
                record.Error = string.Join("; ", errors);
                record.Passed = false;
                return record;
            }

            record.Position = label.position;
            record.IsFinite = IsFinite(label.position) && IsFinite(label.rotation) &&
                IsFinite(label.lossyScale) && IsFinite(label.forward) && IsFinite(footprint);
            record.FacingFinite = record.IsFinite && label.forward.sqrMagnitude > ScaleEpsilon;
            record.FootprintDistance = HorizontalDistanceToBounds(label.position, footprint);
            record.InsideFootprint = record.IsFinite && record.FootprintDistance <= Epsilon;
            record.WithinDistance = distanceValid && record.IsFinite &&
                record.FootprintDistance <= maxDistance + Epsilon;
            record.BillboardConfigured = HasFacingOrBillboardFlag(label);
            if (!record.IsFinite) errors.Add("label transform or footprint contains non-finite values");
            if (!record.InsideFootprint) errors.Add("label anchor is outside the model footprint");
            if (!record.WithinDistance) errors.Add("label anchor exceeds the allowed footprint distance");
            if (!record.FacingFinite) errors.Add("label forward direction is invalid");
            if (!record.BillboardConfigured) errors.Add("label has no facing or billboard flag");

            record.Error = string.Join("; ", errors);
            record.Passed = errors.Count == 0;
            return record;
        }

        public static bool Validate(IEnumerable<Record> records, out string summary)
        {
            if (records == null)
            {
                summary = "World model alignment failed: records are null.";
                return false;
            }

            int total = 0;
            int failed = 0;
            var failures = new List<string>();
            foreach (Record record in records)
            {
                total++;
                if (record == null)
                {
                    failed++;
                    failures.Add("<null-record>");
                    continue;
                }
                if (record.Passed) continue;
                failed++;
                string label = string.IsNullOrEmpty(record.Name) ? "<unnamed>" : record.Name;
                string reason = string.IsNullOrEmpty(record.Error) ? "unspecified failure" : record.Error;
                failures.Add(label + ": " + reason);
            }

            if (total == 0)
            {
                summary = "World model alignment failed: no records were supplied.";
                return false;
            }
            if (failed == 0)
            {
                summary = "World model alignment passed (" + total + " records).";
                return true;
            }

            summary = "World model alignment failed (" + failed + "/" + total + "): " +
                string.Join(" | ", failures);
            return false;
        }

        static bool HasFacingOrBillboardFlag(Transform label)
        {
            WorldLabelAnchor anchor = label.GetComponent<WorldLabelAnchor>();
            if (anchor)
            {
                if (!anchor.Billboard) return true;
                return anchor.HasBillboardComponent;
            }
            return label.GetComponent<BillboardLabel>() != null;
        }

        static bool IsInsideHorizontal(Vector3 point, Bounds bounds, float tolerance)
        {
            return point.x >= bounds.min.x - tolerance && point.x <= bounds.max.x + tolerance &&
                point.z >= bounds.min.z - tolerance && point.z <= bounds.max.z + tolerance;
        }

        static float HorizontalDistanceToBounds(Vector3 point, Bounds bounds)
        {
            float distanceX = Mathf.Max(bounds.min.x - point.x, 0, point.x - bounds.max.x);
            float distanceZ = Mathf.Max(bounds.min.z - point.z, 0, point.z - bounds.max.z);
            return Mathf.Sqrt(distanceX * distanceX + distanceZ * distanceZ);
        }

        static bool HasUsableScale(Vector3 scale)
        {
            return Mathf.Abs(scale.x) > ScaleEpsilon && Mathf.Abs(scale.y) > ScaleEpsilon &&
                Mathf.Abs(scale.z) > ScaleEpsilon;
        }

        static bool IsFinite(Bounds bounds)
        {
            return IsFinite(bounds.center) && IsFinite(bounds.extents);
        }

        static bool IsFinite(Quaternion rotation)
        {
            return IsFinite(rotation.x) && IsFinite(rotation.y) && IsFinite(rotation.z) && IsFinite(rotation.w);
        }

        static bool IsFinite(Vector3 vector)
        {
            return IsFinite(vector.x) && IsFinite(vector.y) && IsFinite(vector.z);
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
