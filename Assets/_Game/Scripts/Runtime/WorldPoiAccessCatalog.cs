using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Tycoon
{
    public readonly struct WorldPoiAccessDefinition
    {
        public readonly string StableId;
        public readonly string Label;
        public readonly Vector3 Center;
        public readonly Vector3 Footprint;
        public readonly Vector3 ApproachAnchor;
        public readonly Vector3 ExitAnchor;
        public readonly WorldInteractionAction Action;
        public readonly float InteractionRadius;
        public readonly string RouteId;

        public WorldPoiAccessDefinition(
            string stableId,
            string label,
            Vector3 center,
            Vector3 footprint,
            Vector3 approachAnchor,
            Vector3 exitAnchor,
            WorldInteractionAction action,
            float interactionRadius,
            string routeId)
        {
            StableId = stableId;
            Label = label;
            Center = center;
            Footprint = footprint;
            ApproachAnchor = approachAnchor;
            ExitAnchor = exitAnchor;
            Action = action;
            InteractionRadius = interactionRadius;
            RouteId = routeId;
        }
    }

    public static class WorldPoiAccessCatalog
    {
        static readonly IReadOnlyList<WorldPoiAccessDefinition> Definitions =
            new ReadOnlyCollection<WorldPoiAccessDefinition>(new[]
            {
                new WorldPoiAccessDefinition("farm", "Nông trại", CityDistricts.Center("farm"),
                    CityDistricts.Footprint("farm").size, CityDistricts.PlayerSpawn,
                    new Vector3(-40, .05f, -5), WorldInteractionAction.Visit, 3.2f,
                    "farm-dirt-path"),
                new WorldPoiAccessDefinition("farm_shop", "Cửa hàng nông sản",
                    CityDistricts.Center("farm_shop"), CityDistricts.Footprint("farm_shop").size,
                    new Vector3(7, .05f, 0), new Vector3(3, .05f, -7),
                    WorldInteractionAction.Visit, 3.2f, "farm-sidewalk-connector"),
                new WorldPoiAccessDefinition("park", "Khu sinh hoạt cộng đồng",
                    CityDistricts.ParkCenter, new Vector3(40, 3, 32),
                    new Vector3(-19, .05f, 24), new Vector3(-19, .05f, 36),
                    WorldInteractionAction.Visit, 3.2f, "sidewalk-crossings"),
                new WorldPoiAccessDefinition("community_hall", "Nhà sinh hoạt cộng đồng",
                    new Vector3(-72, 0, 181), new Vector3(40, 12, 32),
                    new Vector3(-72, .05f, 164), new Vector3(-72, .05f, 168),
                    WorldInteractionAction.Visit, 3.2f, "sidewalk-crossings"),
                new WorldPoiAccessDefinition("residential_housing", "Khu nhà ở dân cư",
                    new Vector3(128, 0, 38), new Vector3(28, 8, 22),
                    new Vector3(128, .05f, 50), new Vector3(128, .05f, 45),
                    WorldInteractionAction.Visit, 3.2f, "highway-ring"),
                new WorldPoiAccessDefinition("bank", "Ngân hàng", new Vector3(-226, 0, 120),
                    new Vector3(38, 12, 38), new Vector3(-226, .05f, 101),
                    new Vector3(-226, .05f, 104), WorldInteractionAction.Banking, 3.2f,
                    "sidewalk-crossings"),
                new WorldPoiAccessDefinition("restaurant", "Nhà hàng",
                    CityDistricts.Center("restaurant"), CityDistricts.Footprint("restaurant").size,
                    new Vector3(-52, .05f, 52), new Vector3(-52, .05f, 55),
                    WorldInteractionAction.Dine, 3.2f, "sidewalk-crossings"),
                new WorldPoiAccessDefinition("commercial_market", "Khu thương mại",
                    CityDistricts.Center("market"), CityDistricts.Footprint("market").size,
                    new Vector3(70, .05f, 39), new Vector3(70, .05f, 43),
                    WorldInteractionAction.Visit, 3.2f, "highway-ring"),
                new WorldPoiAccessDefinition("station", "Nhà ga trung tâm",
                    CityRailNetwork.StationBounds.center, CityRailNetwork.StationBounds.size,
                    new Vector3(CityRailNetwork.StationX, .05f, -51.5f),
                    new Vector3(CityRailNetwork.StationX, .05f, -53.2f),
                    WorldInteractionAction.Visit, 3.2f, "rail-mainline"),
                new WorldPoiAccessDefinition("rail_platform_1", "Sân ga 1",
                    CityRailNetwork.PlatformPoint(0), new Vector3(66, 3, 3),
                    new Vector3(0, .05f, -54.4f), new Vector3(10, .05f, -53.2f),
                    WorldInteractionAction.BoardTrain, 2.8f, "rail-mainline"),
                new WorldPoiAccessDefinition("industrial_yard", "Bãi công nghiệp",
                    new Vector3(410, 0, 300), new Vector3(180, 4, 140),
                    new Vector3(465, .05f, 330), new Vector3(450, .05f, 320),
                    WorldInteractionAction.Visit, 3.2f, "expansion-industrial-approach"),
                new WorldPoiAccessDefinition("harbor", "Bến cảng", new Vector3(231, 0, -123),
                    new Vector3(62, 12, 66), new Vector3(206, .05f, -123),
                    new Vector3(210, .05f, -123), WorldInteractionAction.BoardVessel, 3.2f,
                    "dock-harbor-approach"),
                new WorldPoiAccessDefinition("marina", "Bến du thuyền", new Vector3(231, 0, -40),
                    new Vector3(62, 12, 44), new Vector3(206, .05f, -40),
                    new Vector3(210, .05f, -40), WorldInteractionAction.BoardVessel, 3.2f,
                    "dock-harbor-approach"),
                new WorldPoiAccessDefinition("forest_reserve", "Khu bảo tồn rừng",
                    new Vector3(-420, 0, 250), new Vector3(180, 4, 150),
                    new Vector3(-370, .05f, 275), new Vector3(-350, .05f, 267),
                    WorldInteractionAction.Explore, 3.2f, "expansion-forest-access"),
                new WorldPoiAccessDefinition("beach_swim", "Bãi biển và vùng bơi",
                    new Vector3(233, 0, 77), new Vector3(68, 12, 136),
                    new Vector3(206, .05f, 77), new Vector3(210, .05f, 77),
                    WorldInteractionAction.Swim, 3.2f, "sidewalk-crossings"),
                new WorldPoiAccessDefinition("north_village", "Khu dân cư phía Bắc",
                    new Vector3(0, 0, 330), new Vector3(160, 4, 90),
                    new Vector3(-55, .05f, 320), new Vector3(55, .05f, 345),
                    WorldInteractionAction.Visit, 3.2f, "expansion-village-road"),
                new WorldPoiAccessDefinition("highway_service_area", "Khu dừng xa lộ",
                    new Vector3(0, 0, -300), new Vector3(450, 4, 50),
                    new Vector3(-190, .05f, -300), new Vector3(190, .05f, -300),
                    WorldInteractionAction.Visit, 3.2f, "expansion-south-highway"),
                new WorldPoiAccessDefinition("highway_service_stop", "Điểm dừng phía Đông",
                    new Vector3(190, 0, -300), new Vector3(40, 4, 24),
                    new Vector3(190, .05f, -300), new Vector3(190, .05f, -294),
                    WorldInteractionAction.Visit, 2.8f, "expansion-south-highway")
            });

        public static IReadOnlyList<WorldPoiAccessDefinition> BuildDefault()
        {
            return Definitions;
        }

        public static bool Validate(out string reason)
        {
            var ids = new HashSet<string>();
            foreach (WorldPoiAccessDefinition definition in Definitions)
            {
                if (string.IsNullOrWhiteSpace(definition.StableId)
                    || string.IsNullOrWhiteSpace(definition.Label)
                    || !ids.Add(definition.StableId))
                {
                    reason = "POI access ID bị trống hoặc trùng.";
                    return false;
                }
                if (!Finite(definition.Center) || !Finite(definition.Footprint)
                    || !Finite(definition.ApproachAnchor) || !Finite(definition.ExitAnchor)
                    || definition.Footprint.x <= 0f || definition.Footprint.y <= 0f
                    || definition.Footprint.z <= 0f || definition.InteractionRadius <= 0f)
                {
                    reason = "POI access " + definition.StableId + " có dữ liệu không hợp lệ.";
                    return false;
                }
                Bounds expanded = new Bounds(definition.Center, definition.Footprint);
                expanded.Expand(Vector3.one * definition.InteractionRadius * 2f);
                if (!expanded.Contains(definition.ApproachAnchor)
                    || !expanded.Contains(definition.ExitAnchor))
                {
                    reason = "POI access " + definition.StableId + " có anchor ngoài footprint.";
                    return false;
                }
                if (!WorldRouteCatalog.TryGet(definition.RouteId, out _))
                {
                    reason = "POI access " + definition.StableId + " tham chiếu route thiếu.";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        static bool Finite(Vector3 value)
        {
            return Finite(value.x) && Finite(value.y) && Finite(value.z);
        }

        static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
