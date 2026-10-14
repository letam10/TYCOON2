using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public enum WorldInteractionAction
    {
        Visit,
        Swim,
        Explore,
        Dine,
        Banking,
        BoardVessel,
        BoardTrain,
        CallTaxi
    }

    [DisallowMultipleComponent]
    public sealed class WorldInteractionArea : MonoBehaviour, IPlayerInteractionArea
    {
        public string Id;
        public string Label;
        public string Message;
        public WorldInteractionAction Action;
        public float Radius = 2.4f;
        public float Cooldown = .8f;
        public Vector3 LocalCenter;
        public Component CarrierSource;
        public float RideSeconds = 5f;
        float nextUse;

        public InteractionKind Kind => InteractionKind.Operate;
        public Vector3 Center => transform.TransformPoint(LocalCenter);
        public bool Available => enabled && gameObject.activeInHierarchy;

        public static WorldInteractionArea Add(
            Transform parent,
            string id,
            string label,
            string message,
            WorldInteractionAction action,
            Vector3 localCenter,
            float radius = 2.4f)
        {
            var area = parent.GetComponent<WorldInteractionArea>();
            if (!area) area = parent.gameObject.AddComponent<WorldInteractionArea>();
            area.Id = id;
            area.Label = label;
            area.Message = message;
            area.Action = action;
            area.LocalCenter = localCenter;
            area.Radius = radius;
            return area;
        }

        public bool Contains(Vector3 point)
        {
            Vector3 offset = point - Center;
            offset.y = 0;
            return offset.sqrMagnitude <= Radius * Radius
                && Mathf.Abs(point.y - Center.y) <= 3.2f;
        }

        public InteractionResult Perform(InteractionContext context, float delta)
        {
            if (!context.Player || delta <= 0 || Time.time < nextUse)
                return InteractionResult.Waiting;
            nextUse = Time.time + Mathf.Max(.1f, Cooldown);
            if (Action is WorldInteractionAction.BoardTrain
                or WorldInteractionAction.BoardVessel
                or WorldInteractionAction.CallTaxi)
                return Board(context.Player);
            GameSession.Instance?.Say(Message);
            return InteractionResult.Success;
        }

        InteractionResult Board(PlayerController player)
        {
            var carrier = CarrierSource as ICityPassengerCarrier;
            if (carrier != null && !carrier.DoorsOpen)
                return InteractionResult.Reject("Hãy đợi cửa phương tiện mở.");
            string reason = Action switch
            {
                WorldInteractionAction.BoardTrain => "Đã lên tàu; phương tiện sẽ đưa bạn tới ga kế tiếp.",
                WorldInteractionAction.CallTaxi => "Đã lên xe; phương tiện sẽ đưa bạn tới điểm tiếp theo.",
                _ => "Đã lên thuyền; chuyến tham quan bến nước đang bắt đầu."
            };
            if (!WorldPlayerRide.TryBoard(player, carrier, transform, RideSeconds, reason, out string failure))
                return InteractionResult.Reject(failure);
            return InteractionResult.Success;
        }

        public void Exit(EntityId actor)
        {
        }
    }

    public sealed class WorldPlayerRide : MonoBehaviour
    {
        PlayerController player;
        ICityPassengerCarrier carrier;
        Transform fallbackHull;
        Transform originalParent;
        Vector3 exitPoint;
        float endsAt;
        bool activeRide;

        public static bool TryBoard(
            PlayerController target,
            ICityPassengerCarrier passengerCarrier,
            Transform fallback,
            float seconds,
            string message,
            out string failure)
        {
            failure = "";
            if (!target || !target.Controller)
            {
                failure = "Người chơi chưa sẵn sàng lên phương tiện.";
                return false;
            }
            if (passengerCarrier != null && !passengerCarrier.DoorsOpen)
            {
                failure = "Hãy đợi cửa phương tiện mở.";
                return false;
            }
            Transform hull = passengerCarrier != null ? passengerCarrier.Hull : fallback;
            if (!hull)
            {
                failure = "Phương tiện chưa sẵn sàng để lên.";
                return false;
            }
            var ride = target.GetComponent<WorldPlayerRide>();
            if (!ride) ride = target.gameObject.AddComponent<WorldPlayerRide>();
            if (ride.activeRide)
            {
                failure = "Bạn đang ở trên một phương tiện khác.";
                return false;
            }
            ride.Begin(target, passengerCarrier, fallback, seconds, message);
            return true;
        }

        void Begin(
            PlayerController target,
            ICityPassengerCarrier passengerCarrier,
            Transform fallback,
            float seconds,
            string message)
        {
            player = target;
            carrier = passengerCarrier;
            fallbackHull = fallback;
            originalParent = player.transform.parent;
            Transform hull = carrier != null ? carrier.Hull : fallbackHull;
            if (!hull) return;
            Vector3 inside = carrier != null ? carrier.DoorInsideLocal : Vector3.up * 1.1f;
            exitPoint = carrier != null
                ? carrier.DoorOutside
                : hull.TransformPoint(Vector3.forward * 3f);
            player.Controller.enabled = false;
            player.CanControl = false;
            if (carrier != null)
            {
                player.transform.SetParent(hull, true);
                player.transform.localPosition = inside;
                player.transform.localRotation = Quaternion.identity;
            }
            else
            {
                player.transform.position = hull.position + Vector3.up * 1.1f;
                player.transform.rotation = hull.rotation;
            }
            endsAt = Time.time + Mathf.Max(1.5f, seconds);
            activeRide = true;
            carrier?.RecordBoarding();
            GameSession.Instance?.Say(message);
        }

        void Update()
        {
            if (!activeRide || !player) return;
            if (Time.time < endsAt && (carrier == null || carrier.Hull))
                return;
            EndRide();
        }

        void EndRide()
        {
            ICityPassengerCarrier completedCarrier = carrier;
            Transform parent = originalParent;
            Vector3 target = completedCarrier != null && completedCarrier.Hull
                ? completedCarrier.DoorOutside
                : exitPoint;
            player.transform.SetParent(parent, true);
            if (NavMesh.SamplePosition(target, out var hit, 2.5f, NavMesh.AllAreas))
                target = hit.position + Vector3.up * .05f;
            player.transform.position = target;
            player.Controller.enabled = true;
            player.CanControl = true;
            player.StopInteraction();
            completedCarrier?.RecordAlighting();
            activeRide = false;
            carrier = null;
            fallbackHull = null;
            GameSession.Instance?.Say("Đã xuống phương tiện tại điểm an toàn.");
        }

        void OnDisable()
        {
            if (activeRide && player) EndRide();
        }
    }

    public sealed class WorldInteractionRegistry : MonoBehaviour
    {
        readonly List<WorldInteractionArea> dynamicAreas = new();
        Transform world;
        float nextRefresh;

        public static WorldInteractionRegistry Build(Transform root)
        {
            if (!root) return null;
            var existing = root.GetComponentInChildren<WorldInteractionRegistry>();
            if (existing) return existing;
            var holder = new GameObject("WorldInteractions").transform;
            holder.SetParent(root, false);
            var registry = holder.gameObject.AddComponent<WorldInteractionRegistry>();
            registry.world = root;
            registry.BuildLandmarkAreas();
            registry.RefreshDynamicCarriers();
            return registry;
        }

        void Update()
        {
            if (Time.time < nextRefresh) return;
            nextRefresh = Time.time + 1f;
            RefreshDynamicCarriers();
        }

        void BuildLandmarkAreas()
        {
            foreach (var landmark in world.GetComponentsInChildren<CityLandmark>(true))
            {
                if (!landmark) continue;
                WorldInteractionAction action = WorldInteractionAction.Visit;
                string label = landmark.Id;
                string message = "Đã tới " + landmark.Id + ".";
                if (landmark.Id == "Beach")
                {
                    action = WorldInteractionAction.Swim;
                    label = "Bãi biển";
                    message = "Bạn đang tắm biển; hãy đứng trong vùng nước nông.";
                }
                else if (landmark.Id == "Harbor" || landmark.Id == "Marina")
                {
                    action = WorldInteractionAction.BoardVessel;
                    label = landmark.Id == "Harbor" ? "Bến cảng" : "Bến du thuyền";
                    message = "Tương tác với thuyền và bến neo.";
                }
                else if (landmark.Id == "Bank")
                {
                    action = WorldInteractionAction.Banking;
                    label = "Ngân hàng";
                    message = "Mở quầy ngân hàng để gửi hoặc rút tiền.";
                }
                WorldInteractionArea.Add(
                    landmark.transform,
                    "landmark_" + landmark.Id,
                    label,
                    message,
                    action,
                    landmark.Approach == landmark.transform.position
                        ? Vector3.zero
                        : landmark.transform.InverseTransformPoint(landmark.Approach),
                    3.2f);
            }
            AddDistrict("restaurant", "Nhà hàng", "Bạn có thể vào nhà hàng để phục vụ và dùng bữa.",
                WorldInteractionAction.Dine, CityDistricts.Center("restaurant") + Vector3.back * 3);
            AddDistrict("forest", "Rừng nguyên sinh", "Đường rừng đã mở; hãy đi theo lối mòn.",
                WorldInteractionAction.Explore, new Vector3(30, 0, 133));
        }

        void AddDistrict(
            string id,
            string label,
            string message,
            WorldInteractionAction action,
            Vector3 position)
        {
            var root = new GameObject("WorldInteraction_" + id).transform;
            root.SetParent(world, false);
            root.localPosition = position;
            WorldInteractionArea.Add(root, id, label, message, action, Vector3.zero, 3.2f);
        }

        void RefreshDynamicCarriers()
        {
            foreach (var train in world.GetComponentsInChildren<CityRailTrain>(true))
                AddCarrierArea(train, WorldInteractionAction.BoardTrain, "Lên tàu hỏa",
                    "Đang chờ cửa tàu mở.", "train");
            foreach (var vehicle in world.GetComponentsInChildren<CityPublicTransportVehicle>(true))
                AddCarrierArea(vehicle, WorldInteractionAction.CallTaxi, "Đón xe",
                    "Đang chờ cửa xe mở.", "vehicle");
        }

        void AddCarrierArea(
            Component carrier,
            WorldInteractionAction action,
            string label,
            string message,
            string prefix)
        {
            if (!carrier) return;
            var carrierInterface = carrier as ICityPassengerCarrier;
            if (carrierInterface == null) return;
            if (carrier.GetComponent<WorldInteractionArea>()) return;
            var area = WorldInteractionArea.Add(
                carrier.transform,
                prefix + "_" + carrier.name,
                label,
                message,
                action,
                carrierInterface.DoorOutside == carrier.transform.position
                    ? Vector3.zero
                    : carrier.transform.InverseTransformPoint(carrierInterface.DoorOutside),
                2.8f);
            area.CarrierSource = carrier;
            area.RideSeconds = action == WorldInteractionAction.BoardTrain ? 7f : 4f;
            dynamicAreas.Add(area);
        }
    }
}
