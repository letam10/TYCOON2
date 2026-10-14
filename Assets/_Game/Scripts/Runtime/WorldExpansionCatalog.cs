using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Tycoon
{
    public readonly struct WorldExpansionPoi
    {
        readonly ReadOnlyCollection<Vector3> routeAnchors;

        public readonly string Id;
        public readonly string Label;
        public readonly Vector3 Center;
        public readonly Vector3 Footprint;
        public IReadOnlyList<Vector3> RouteAnchors => routeAnchors;
        public Bounds FootprintBounds => new Bounds(Center, Footprint);

        public WorldExpansionPoi(
            string id,
            string label,
            Vector3 center,
            Vector3 footprint,
            params Vector3[] routeAnchors)
        {
            Id = id;
            Label = label;
            Center = center;
            Footprint = footprint;
            this.routeAnchors = Array.AsReadOnly(routeAnchors ?? Array.Empty<Vector3>());
        }
    }

    public static class WorldExpansionCatalog
    {
        // Các kích thước này là world hiện tại nhân đôi quanh tâm (8, 0, 22).
        const float BaseWorldWidth = 565.685424f;
        const float BaseWorldDepth = 384.666088f;
        const float RouteAnchorMargin = 12f;

        public static readonly Vector3 WorldCenter = new Vector3(8, 0, 22);
        public static Bounds ExpandedWorldBounds => new Bounds(
            WorldCenter,
            new Vector3(BaseWorldWidth * 2f, 4f, BaseWorldDepth * 2f));

        static readonly IReadOnlyList<WorldExpansionPoi> DefaultPois =
            new ReadOnlyCollection<WorldExpansionPoi>(new[]
            {
                new WorldExpansionPoi(
                    "ForestReserve",
                    "Khu bảo tồn rừng",
                    new Vector3(-420, 0, 250),
                    new Vector3(180, 4, 150),
                    new Vector3(-370, 0, 275),
                    new Vector3(-350, 0, 267)),
                new WorldExpansionPoi(
                    "IndustrialYard",
                    "Bãi công nghiệp",
                    new Vector3(410, 0, 300),
                    new Vector3(180, 4, 140),
                    new Vector3(330, 0, 250),
                    new Vector3(465, 0, 330)),
                new WorldExpansionPoi(
                    "NorthVillage",
                    "Làng phía Bắc",
                    new Vector3(0, 0, 330),
                    new Vector3(160, 4, 90),
                    new Vector3(-55, 0, 320),
                    new Vector3(55, 0, 345)),
                new WorldExpansionPoi(
                    "EastMarina",
                    "Bến du thuyền phía Đông",
                    new Vector3(420, 0, -120),
                    new Vector3(180, 4, 110),
                    new Vector3(370, 0, -135),
                    new Vector3(470, 0, -105)),
                new WorldExpansionPoi(
                    "SouthHighway",
                    "Xa lộ phía Nam",
                    new Vector3(0, 0, -300),
                    new Vector3(450, 4, 50),
                    new Vector3(-190, 0, -300),
                    new Vector3(190, 0, -300))
            });

        public static IReadOnlyList<WorldExpansionPoi> BuildDefault()
        {
            return DefaultPois;
        }

        public static bool Validate(out string reason)
        {
            IReadOnlyList<WorldExpansionPoi> pois = BuildDefault();
            for (int index = 0; index < pois.Count; index++)
            {
                WorldExpansionPoi poi = pois[index];
                if (string.IsNullOrWhiteSpace(poi.Id) || string.IsNullOrWhiteSpace(poi.Label))
                {
                    reason = "POI " + index + " thiếu id hoặc nhãn.";
                    return false;
                }
                if (!IsFinite(poi.Center) || !IsFinite(poi.Footprint))
                {
                    reason = "POI " + poi.Id + " có tọa độ không hữu hạn.";
                    return false;
                }
                if (poi.Footprint.x <= 0f || poi.Footprint.y <= 0f || poi.Footprint.z <= 0f)
                {
                    reason = "POI " + poi.Id + " có footprint không dương.";
                    return false;
                }

                Bounds bounds = poi.FootprintBounds;
                if (!ExpandedWorldBounds.Contains(bounds.min) || !ExpandedWorldBounds.Contains(bounds.max))
                {
                    reason = "POI " + poi.Id + " nằm ngoài bounds world mở rộng.";
                    return false;
                }
                if (poi.RouteAnchors == null || poi.RouteAnchors.Count == 0)
                {
                    reason = "POI " + poi.Id + " chưa có route anchor.";
                    return false;
                }
                for (int anchorIndex = 0; anchorIndex < poi.RouteAnchors.Count; anchorIndex++)
                {
                    Vector3 anchor = poi.RouteAnchors[anchorIndex];
                    if (!IsFinite(anchor))
                    {
                        reason = "Route anchor " + anchorIndex + " của " + poi.Id
                            + " có tọa độ không hữu hạn.";
                        return false;
                    }
                    Bounds routeBounds = bounds;
                    routeBounds.Expand(Vector3.one * (RouteAnchorMargin * 2f));
                    if (!routeBounds.Contains(anchor) || !ExpandedWorldBounds.Contains(anchor))
                    {
                        reason = "Route anchor " + anchorIndex + " của " + poi.Id
                            + " nằm ngoài footprint mở rộng.";
                        return false;
                    }
                }
                for (int previousIndex = 0; previousIndex < index; previousIndex++)
                {
                    if (bounds.Intersects(pois[previousIndex].FootprintBounds))
                    {
                        reason = "POI " + poi.Id + " giao nhau với " + pois[previousIndex].Id + ".";
                        return false;
                    }
                }
            }

            reason = string.Empty;
            return true;
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
