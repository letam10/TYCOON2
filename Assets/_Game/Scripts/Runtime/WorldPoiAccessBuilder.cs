using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    [DisallowMultipleComponent]
    public sealed class WorldPoiAccessMarker : MonoBehaviour
    {
        public WorldPoiAccessDefinition Definition { get; private set; }
        public WorldInteractionArea Interaction { get; private set; }
        public Bounds CollisionBoundary { get; private set; }
        public string StableId => Definition.StableId;
        public Vector3 ApproachAnchor => Definition.ApproachAnchor;
        public Vector3 ExitAnchor => Definition.ExitAnchor;

        public void Initialize(
            WorldPoiAccessDefinition definition,
            WorldInteractionArea interaction,
            Bounds collisionBoundary)
        {
            Definition = definition;
            Interaction = interaction;
            CollisionBoundary = collisionBoundary;
        }

        public bool TryEnter(PlayerController player, out string failure)
        {
            if (!Interaction)
            {
                failure = "POI chưa có vùng tương tác.";
                return false;
            }
            return Interaction.Enter(player, out failure);
        }

        public void Exit(EntityId actor)
        {
            Interaction?.Exit(actor);
        }
    }

    [DisallowMultipleComponent]
    public sealed class WorldPoiAccessRuntime : MonoBehaviour
    {
        readonly List<WorldPoiAccessMarker> markers = new();
        Transform world;
        float nextBind;

        public void Initialize(Transform root, IReadOnlyList<WorldPoiAccessMarker> entries)
        {
            world = root;
            markers.Clear();
            if (entries != null)
                markers.AddRange(entries);
            BindCarriers();
        }

        void Update()
        {
            if (Time.time < nextBind) return;
            nextBind = Time.time + 1f;
            BindCarriers();
        }

        void BindCarriers()
        {
            if (!world) return;
            CityRailTrain train = world.GetComponentInChildren<CityRailTrain>(true);
            CityPublicTransportVehicle vehicle =
                world.GetComponentInChildren<CityPublicTransportVehicle>(true);
            foreach (WorldPoiAccessMarker marker in markers)
            {
                if (!marker || !marker.Interaction) continue;
                if (marker.Definition.Action == WorldInteractionAction.BoardVessel)
                {
                    WorldExpansionBoat boat = ResolveBoat(marker.Definition.StableId);
                    if (boat) Bind(marker, boat);
                }
                else if (marker.Definition.Action == WorldInteractionAction.BoardTrain && train)
                {
                    Bind(marker, train);
                }
                else if (marker.Definition.Action == WorldInteractionAction.CallTaxi && vehicle)
                {
                    Bind(marker, vehicle);
                }
            }
        }

        WorldExpansionBoat ResolveBoat(string stableId)
        {
            string name = stableId == "harbor" ? "Harbor" : "Marina";
            Transform host = world.Find(name);
            return host ? host.GetComponentInChildren<WorldExpansionBoat>(true) : null;
        }

        static void Bind(WorldPoiAccessMarker marker, ICityPassengerCarrier carrier)
        {
            marker.Interaction.CarrierSource = carrier as Component;
            marker.Interaction.SetApproachPoint(
                WorldInteractionArea.ComputeCarrierApproach(carrier, marker.Interaction.Radius));
        }
    }

    public static class WorldPoiAccessBuilder
    {
        public static Transform Build(Transform world)
        {
            if (!world) return null;
            if (!WorldPoiAccessCatalog.Validate(out string reason))
            {
                Debug.LogError("POI access validation failed: " + reason);
                return null;
            }
            Transform existing = world.Find("Stage7PoiAccess");
            if (existing) return existing;

            Transform root = new GameObject("Stage7PoiAccess").transform;
            root.SetParent(world, false);
            var runtime = root.gameObject.AddComponent<WorldPoiAccessRuntime>();
            var markers = new List<WorldPoiAccessMarker>();
            foreach (WorldPoiAccessDefinition definition in WorldPoiAccessCatalog.BuildDefault())
                markers.Add(BuildMarker(world, root, definition));
            runtime.Initialize(world, markers);
            return root;
        }

        static WorldPoiAccessMarker BuildMarker(
            Transform world,
            Transform root,
            WorldPoiAccessDefinition definition)
        {
            Transform node = new GameObject("POIAccess_" + definition.StableId).transform;
            node.SetParent(root, false);
            node.position = definition.Center;
            WorldInteractionArea interaction = FindArea(world, definition.StableId);
            if (!interaction)
            {
                interaction = WorldInteractionArea.Add(
                    node,
                    definition.StableId,
                    definition.Label,
                    Message(definition.Action, definition.Label),
                    definition.Action,
                    node.InverseTransformPoint(definition.ApproachAnchor),
                    definition.InteractionRadius);
            }
            interaction.SetApproachPoint(definition.ApproachAnchor);
            interaction.LocalExitPoint = node.InverseTransformPoint(definition.ExitAnchor);
            if (definition.Action == WorldInteractionAction.BoardVessel)
            {
                WorldExpansionBoat boat = ResolveBoat(world, definition.StableId);
                if (boat)
                {
                    interaction.CarrierSource = boat;
                    interaction.SetApproachPoint(
                        WorldInteractionArea.ComputeCarrierApproach(boat, interaction.Radius));
                }
            }

            Bounds collision = FindCollision(world, definition);
            WorldPoiAccessMarker marker = node.gameObject.AddComponent<WorldPoiAccessMarker>();
            marker.Initialize(definition, interaction, collision);
            return marker;
        }

        static WorldInteractionArea FindArea(Transform world, string stableId)
        {
            foreach (WorldInteractionArea area in world.GetComponentsInChildren<WorldInteractionArea>(true))
                if (area && string.Equals(area.StableId, stableId, StringComparison.Ordinal)) return area;
            return null;
        }

        static WorldExpansionBoat ResolveBoat(Transform world, string stableId)
        {
            string name = stableId == "harbor" ? "Harbor" : "Marina";
            Transform host = world.Find(name);
            return host ? host.GetComponentInChildren<WorldExpansionBoat>(true) : null;
        }

        static Bounds FindCollision(Transform world, WorldPoiAccessDefinition definition)
        {
            string hostName = definition.StableId switch
            {
                "bank" => "Bank",
                "community_hall" => "EntertainmentHall",
                "harbor" => "Harbor",
                "marina" => "Marina",
                "beach_swim" => "Beach",
                _ => string.Empty
            };
            Transform host = string.IsNullOrEmpty(hostName) ? null : world.Find(hostName);
            Bounds result = new Bounds(definition.Center, definition.Footprint);
            bool found = false;
            if (host)
            {
                foreach (Collider collider in host.GetComponentsInChildren<Collider>(true))
                {
                    if (!collider || collider.isTrigger) continue;
                    if (!found)
                    {
                        result = collider.bounds;
                        found = true;
                    }
                    else result.Encapsulate(collider.bounds);
                }
            }
            return result;
        }

        static string Message(WorldInteractionAction action, string label)
        {
            return action switch
            {
                WorldInteractionAction.Swim => "Đứng tại bờ nông để bơi.",
                WorldInteractionAction.Banking => "Đứng trong vùng ngân hàng để giao dịch.",
                WorldInteractionAction.Dine => "Vào nhà hàng để xếp hàng và dùng bữa.",
                WorldInteractionAction.BoardVessel => "Đứng tại cửa bến khi thuyền mở cửa.",
                WorldInteractionAction.BoardTrain => "Đứng tại sân ga khi tàu mở cửa.",
                WorldInteractionAction.CallTaxi => "Đứng tại điểm dừng để gọi xe.",
                _ => "Đã tới " + label + "."
            };
        }
    }
}
