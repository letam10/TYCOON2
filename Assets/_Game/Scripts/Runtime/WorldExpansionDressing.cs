using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public static class WorldExpansionDressing
    {
        public static void BuildNavigation(Transform world)
        {
            if (!world) return;
            Transform root = world.Find("WorldExpansionDressing");
            if (!root) return;
            var surface = root.GetComponent<NavMeshSurface>();
            if (!surface) surface = root.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true;
            surface.voxelSize = .16f;
            surface.BuildNavMesh();
            Transform links = world.Find("ExpansionNavLinks");
            if (!links)
            {
                links = new GameObject("ExpansionNavLinks").transform;
                links.SetParent(world, false);
                AddLink(links, "Forest", new Vector3(30, 0, 133), new Vector3(-370, 0, 275), 6f);
                AddLink(links, "Industrial", new Vector3(190, 0, 155), new Vector3(330, 0, 250), 7f);
                AddLink(links, "Village", new Vector3(0, 0, 155), new Vector3(0, 0, 330), 6f);
                AddLink(links, "Marina", new Vector3(199, 0, -40), new Vector3(370, 0, -135), 6f);
                AddLink(links, "SouthHighway", new Vector3(190, 0, -112), new Vector3(0, 0, -300), 8f);
            }
        }

        static void AddLink(Transform parent, string id, Vector3 start, Vector3 end, float width)
        {
            var link = new GameObject("ExpansionNavLink_" + id).AddComponent<NavMeshLink>();
            link.transform.SetParent(parent, false);
            link.startPoint = parent.InverseTransformPoint(start);
            link.endPoint = parent.InverseTransformPoint(end);
            link.width = width;
            link.bidirectional = true;
            link.autoUpdate = true;
            link.agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID;
            link.UpdateLink();
        }

        public static void Build(Transform world)
        {
            if (!world)
            {
                Debug.LogError("World expansion validation failed: world is null");
                return;
            }
            if (!WorldExpansionCatalog.Validate(out string reason))
            {
                Debug.LogError("World expansion validation failed: " + reason);
                return;
            }

            var root = new GameObject("WorldExpansionDressing").transform;
            root.SetParent(world, false);
            foreach (WorldExpansionPoi poi in WorldExpansionCatalog.BuildDefault())
                BuildPoi(root, poi);
        }

        static void BuildPoi(Transform parent, WorldExpansionPoi poi)
        {
            var root = new GameObject("Expansion_" + poi.Id).transform;
            root.SetParent(parent, false);
            root.localPosition = poi.Center;
            Art.Box("ExpansionGround", Vector3.zero,
                new Vector3(poi.Footprint.x, .08f, poi.Footprint.z), GroundColor(poi.Id), root, true);
            Art.Label(poi.Label, Vector3.up * .35f, root, .28f, "#F4E9C9", true);
            for (int index = 1; index < poi.RouteAnchors.Count; index++)
                Segment(root, poi.RouteAnchors[index - 1] - poi.Center,
                    poi.RouteAnchors[index] - poi.Center, RouteColor(poi.Id));

            WorldExpansionModels.Build(root, poi);
            WorldExpansionWayfinding.Build(root, poi);
            if (poi.Id is "NorthVillage" or "IndustrialYard" or "EastMarina")
                AddExpansionCutaway(root, poi.Footprint);

            WorldInteractionAction action = poi.Id == "EastMarina"
                ? WorldInteractionAction.BoardVessel
                : WorldInteractionAction.Visit;
            WorldExpansionBoat boat = root.GetComponentInChildren<WorldExpansionBoat>(true);
            Vector3 approach = boat ? root.InverseTransformPoint(boat.DoorOutside) : Vector3.zero;
            WorldInteractionArea area = WorldInteractionArea.Add(root, "expansion_" + poi.Id, poi.Label,
                "Đã tới " + poi.Label + ". Hãy đi theo tuyến được đánh dấu.", action,
                approach, 6f);
            if (boat)
                area.CarrierSource = boat;
        }

        static void AddExpansionCutaway(Transform root, Vector3 footprint)
        {
            string cutawayName = root.name == "Expansion_NorthVillage"
                ? "VillageCutaway"
                : "ExpansionCutaway";
            Transform cutawayRoot = new GameObject(cutawayName).transform;
            cutawayRoot.SetParent(root, false);
            WorldCutawayVolume volume = cutawayRoot.gameObject.AddComponent<WorldCutawayVolume>();
            volume.localFootprint = footprint;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer || renderer.transform.IsChildOf(cutawayRoot))
                    continue;
                if (IsExpansionShell(renderer))
                    volume.RegisterRenderer(renderer);
            }
        }

        static bool IsExpansionShell(Renderer renderer)
        {
            string name = renderer.gameObject.name.ToLowerInvariant();
            string material = renderer.sharedMaterial ? renderer.sharedMaterial.name.ToLowerInvariant() : "";
            return name.Contains("roof") || name.Contains("upper") || material.Contains("roof")
                || material.Contains("tile") || renderer.bounds.center.y > 4.5f;
        }

        static void Segment(Transform parent, Vector3 start, Vector3 end, string color)
        {
            Vector3 delta = end - start;
            delta.y = 0;
            if (delta.sqrMagnitude < .01f) return;
            var road = Art.Box("ExpansionRoute", (start + end) * .5f,
                new Vector3(4f, .06f, delta.magnitude), color, parent);
            road.transform.localRotation = Quaternion.LookRotation(delta);
        }

        static void Forest(Transform parent, Vector3 footprint)
        {
            for (int index = 0; index < 12; index++)
            {
                float x = Mathf.Lerp(-footprint.x * .42f, footprint.x * .42f, (index % 4) / 3f);
                float z = Mathf.Lerp(-footprint.z * .38f, footprint.z * .38f, (index / 4) / 2f);
                Art.Cylinder("ForestTrunk", new Vector3(x, 1.8f, z),
                    new Vector3(.28f, 1.8f, .28f), "#604633", parent);
                Art.Cylinder("ForestCrown", new Vector3(x, 4.2f, z),
                    new Vector3(2.2f, 2.1f, 2.2f), index % 2 == 0 ? "#2F6B4B" : "#3E8052", parent);
            }
        }

        static void Industrial(Transform parent, Vector3 footprint)
        {
            for (int index = 0; index < 6; index++)
            {
                float x = -footprint.x * .34f + index % 3 * 22f;
                float z = -footprint.z * .2f + index / 3 * 28f;
                Art.Box("IndustrialContainer", new Vector3(x, 2f, z),
                    new Vector3(18f, 4f, 10f), index % 2 == 0 ? "#60757A" : "#A56B4D", parent);
                Art.Box("IndustrialRoof", new Vector3(x, 4.2f, z),
                    new Vector3(18.5f, .25f, 10.5f), "#37474A", parent);
            }
            Art.Cylinder("IndustrialSilo", new Vector3(footprint.x * .28f, 5f, 0),
                new Vector3(4f, 5f, 4f), "#93A39D", parent);
        }

        static void Village(Transform parent, Vector3 footprint)
        {
            for (int index = 0; index < 4; index++)
            {
                float x = -footprint.x * .3f + index % 2 * 48f;
                float z = -footprint.z * .25f + index / 2 * 42f;
                Art.Box("VillageHouse", new Vector3(x, 2.2f, z),
                    new Vector3(28f, 4.4f, 22f), index % 2 == 0 ? "#D7B58A" : "#B7C59E", parent);
                Art.Box("VillageRoof", new Vector3(x, 4.65f, z),
                    new Vector3(31f, .6f, 25f), "#65766B", parent);
            }
        }

        static void Marina(Transform parent, Vector3 footprint)
        {
            Art.Box("MarinaWater", new Vector3(footprint.x * .22f, .02f, 0),
                new Vector3(footprint.x * .5f, .04f, footprint.z * .72f), "#3C8994", parent);
            for (int index = -1; index <= 1; index++)
            {
                float z = index * footprint.z * .25f;
                Art.Box("MarinaPier", new Vector3(footprint.x * .05f, .18f, z),
                    new Vector3(footprint.x * .7f, .25f, 4f), "#A18B68", parent);
                for (int post = -3; post <= 3; post++)
                    Art.Cylinder("MarinaPost", new Vector3(post * 20f, .7f, z),
                        new Vector3(.22f, .7f, .22f), "#5B5145", parent);
            }
        }

        static void Highway(Transform parent, Vector3 footprint)
        {
            Art.Box("SouthHighwayRoad", Vector3.zero,
                new Vector3(footprint.x * .92f, .08f, 13f), "#4C5558", parent);
            for (int index = -4; index <= 4; index++)
                Art.Box("HighwayLaneMark", new Vector3(index * 48f, .06f, 0),
                    new Vector3(24f, .02f, .18f), "#E8D9A7", parent);
            for (int index = -4; index <= 4; index++)
                Art.Cylinder("HighwayLamp", new Vector3(index * 48f, 3f, -10f),
                    new Vector3(.15f, 3f, .15f), "#526C6C", parent);
        }

        static string GroundColor(string id)
        {
            return id == "ForestReserve" ? "#6A915F" : id == "EastMarina" ? "#9EA9A2" : "#B8A88C";
        }

        static string RouteColor(string id)
        {
            return id == "SouthHighway" ? "#4C5558" : id == "EastMarina" ? "#A18B68" : "#80664B";
        }
    }
}
