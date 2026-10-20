using System;
using UnityEngine;

namespace Tycoon
{
    [Serializable]
    public sealed class CityAssetProvenance
    {
        public string sourceUrl;
        public string sourceVersion;
        public string sourceCommit;
        public string license;
        public string localAssetPath;
        public string importProfile;
        public string importPolicy;
        public Vector3 scale = Vector3.one;
        public Vector3 pivot;
        public string colliderPolicy;
        public string lodPolicy;

        public string SourceUrl
        {
            get => sourceUrl;
            set => sourceUrl = value;
        }

        public string SourceVersion
        {
            get => sourceVersion;
            set => sourceVersion = value;
        }

        public string SourceCommit
        {
            get => sourceCommit;
            set => sourceCommit = value;
        }

        public string License
        {
            get => license;
            set => license = value;
        }

        public string LocalAssetPath
        {
            get => localAssetPath;
            set => localAssetPath = value;
        }

        public string ImportProfile
        {
            get => importProfile;
            set => importProfile = value;
        }

        public string ImportPolicy
        {
            get => importPolicy;
            set => importPolicy = value;
        }

        public Vector3 Scale
        {
            get => scale;
            set => scale = value;
        }

        public Vector3 Pivot
        {
            get => pivot;
            set => pivot = value;
        }

        public string ColliderPolicy
        {
            get => colliderPolicy;
            set => colliderPolicy = value;
        }

        public string LodPolicy
        {
            get => lodPolicy;
            set => lodPolicy = value;
        }

        public string LODPolicy
        {
            get => lodPolicy;
            set => lodPolicy = value;
        }

        public CityAssetProvenance()
        {
        }

        public CityAssetProvenance(
            string sourceUrl,
            string sourceVersion,
            string license,
            string localAssetPath,
            string importProfile,
            Vector3 scale,
            Vector3 pivot,
            string colliderPolicy,
            string lodPolicy)
        {
            this.sourceUrl = sourceUrl;
            this.sourceVersion = sourceVersion;
            this.license = license;
            this.localAssetPath = localAssetPath;
            this.importProfile = importProfile;
            this.scale = scale;
            this.pivot = pivot;
            this.colliderPolicy = colliderPolicy;
            this.lodPolicy = lodPolicy;
        }

        public bool IsComplete()
        {
            return HasText(sourceUrl)
                && (HasText(sourceVersion) || HasText(sourceCommit))
                && HasText(license)
                && HasText(localAssetPath)
                && (HasText(importProfile) || HasText(importPolicy))
                && HasText(colliderPolicy)
                && HasText(lodPolicy)
                && IsFinitePositive(scale)
                && IsFinite(pivot);
        }

        static bool HasText(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        static bool IsFinitePositive(Vector3 value)
        {
            return IsFinite(value)
                && value.x > 0f
                && value.y > 0f
                && value.z > 0f;
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
