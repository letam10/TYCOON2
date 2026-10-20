using System;
using System.Collections.Generic;
using System.Globalization;
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
    public sealed class WorldInteractionArea : MonoBehaviour, IPlayerInteractionArea, ILogicalDwellInteractionArea
    {
        public string Id;
        public string Label;
        public string Message;
        public WorldInteractionAction Action;
        public float Radius = 2.4f;
        public float Cooldown = .8f;
        public Vector3 LocalCenter;
        public Vector3 LocalApproachPoint;
        public bool UseLocalApproachPoint;
        public string StableGroup = "landmark";
        public Vector3 LocalExitPoint;
        public Component CarrierSource;
        public float RideSeconds = 5f;
        float nextUse;
        int useCount;
        bool inside;

        public InteractionKind Kind => InteractionKind.Operate;
        public Vector3 Center => transform.TransformPoint(LocalCenter);
        public Vector3 ApproachPoint => UseLocalApproachPoint
            ? transform.TransformPoint(LocalApproachPoint)
            : Center;
        public Vector3 ExitPoint => LocalExitPoint == Vector3.zero
            ? Center
            : transform.TransformPoint(LocalExitPoint);
        public string StableId => Id ?? string.Empty;
        public string Prompt => string.IsNullOrEmpty(Message) ? Label : Message;
        public string SaveKey => "world.interaction." + StableId;
        public int UseCount => useCount;
        public bool IsInside => inside;
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
            area.LocalApproachPoint = localCenter;
            area.UseLocalApproachPoint = true;
            area.Radius = radius;
            area.StableGroup = action switch
            {
                WorldInteractionAction.Banking => "bank",
                WorldInteractionAction.Dine => "restaurant",
                WorldInteractionAction.Explore => "forest",
                WorldInteractionAction.Swim => "beach",
                WorldInteractionAction.BoardVessel => "harbor",
                WorldInteractionAction.BoardTrain => "train",
                WorldInteractionAction.CallTaxi => "taxi",
                _ => area.StableGroup
            };
            return area;
        }

        public void SetApproachPoint(Vector3 worldPoint)
        {
            LocalApproachPoint = transform.InverseTransformPoint(worldPoint);
            UseLocalApproachPoint = true;
        }

        public static Vector3 ComputeCarrierApproach(
            ICityPassengerCarrier carrier,
            float radius)
        {
            if (carrier == null || !carrier.Hull)
                return Vector3.zero;
            Vector3 door = carrier.DoorOutside;
            Vector3 outward = door - carrier.Hull.position;
            outward.y = 0;
            if (outward.sqrMagnitude < .01f)
                outward = carrier.Hull.forward;
            if (outward.sqrMagnitude < .01f)
                outward = Vector3.forward;
            float clearance = Mathf.Min(.8f, Mathf.Max(.1f, radius * .45f));
            return door + outward.normalized * clearance;
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
            if (!Enter(context.Player, out string failure))
                return InteractionResult.Reject(failure);
            InteractionResult result;
            if (Action is WorldInteractionAction.BoardTrain
                or WorldInteractionAction.BoardVessel
                or WorldInteractionAction.CallTaxi)
                result = Board(context.Player);
            else
            {
                GameSession.Instance?.Say(Prompt);
                result = InteractionResult.Success;
            }
            if (!result.Worked)
            {
                inside = false;
                return result;
            }
            nextUse = Time.time + Mathf.Max(.1f, Cooldown);
            useCount++;
            return result;
        }

        public bool Enter(PlayerController player, out string failure)
        {
            failure = string.Empty;
            if (!Available)
            {
                failure = "Khu vực tương tác đang tạm đóng.";
                return false;
            }
            if (!player || !player.Controller)
            {
                failure = "Người chơi chưa sẵn sàng tương tác.";
                return false;
            }
            if (!Contains(player.transform.position))
            {
                failure = "Hãy đứng tại điểm tiếp cận của khu vực.";
                return false;
            }
            if (CarrierSource is ICityPassengerCarrier carrier && !carrier.DoorsOpen)
            {
                failure = "Hãy đợi cửa phương tiện mở.";
                return false;
            }
            inside = true;
            return true;
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
            if (!WorldPlayerRide.TryBoard(player, carrier, transform, RideSeconds, reason,
                StableId, out string failure))
                return InteractionResult.Reject(failure);
            return InteractionResult.Success;
        }

        public void Exit(EntityId actor)
        {
            inside = false;
        }

        public string CaptureState()
        {
            string active = enabled ? "1" : "0";
            return string.Join("|", StableId, active,
                useCount.ToString(CultureInfo.InvariantCulture));
        }

        public bool RestoreState(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            string[] fields = value.Split('|');
            if (fields.Length != 3 || !string.Equals(fields[0], StableId, StringComparison.Ordinal))
                return false;
            if (fields[1] != "0" && fields[1] != "1") return false;
            if (!int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture,
                out int restoredUses) || restoredUses < 0)
                return false;
            enabled = fields[1] == "1";
            useCount = restoredUses;
            nextUse = 0;
            inside = false;
            return true;
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
        string interactionId;

        public bool ActiveRide => activeRide;
        public string InteractionId => interactionId ?? string.Empty;
        public float RemainingSeconds => activeRide ? Mathf.Max(0, endsAt - Time.time) : 0;

        public static bool TryBoard(
            PlayerController target,
            ICityPassengerCarrier passengerCarrier,
            Transform fallback,
            float seconds,
            string message,
            out string failure)
        {
            return TryBoard(target, passengerCarrier, fallback, seconds, message, null, out failure);
        }

        public static bool TryBoard(
            PlayerController target,
            ICityPassengerCarrier passengerCarrier,
            Transform fallback,
            float seconds,
            string message,
            string stableInteractionId,
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
            ride.Begin(target, passengerCarrier, fallback, seconds, message, stableInteractionId);
            return true;
        }

        public static bool TryCapture(PlayerController target, out PlayerRideSave save)
        {
            save = null;
            WorldPlayerRide ride = target ? target.GetComponent<WorldPlayerRide>() : null;
            if (!ride || !ride.activeRide) return false;
            Vector3 safeExit = ride.exitPoint;
            if (!Finite(safeExit)) safeExit = ride.player ? ride.player.transform.position : Vector3.zero;
            Transform parent = ride.originalParent;
            WorldInteractionArea parentArea = parent
                ? parent.GetComponentInParent<WorldInteractionArea>()
                : null;
            save = new PlayerRideSave
            {
                version = 1,
                active = true,
                phase = 1,
                interactionId = ride.InteractionId,
                remainingSeconds = ride.RemainingSeconds,
                exitX = safeExit.x,
                exitY = safeExit.y,
                exitZ = safeExit.z,
                originalParentWasNull = parent == null,
                originalParentId = parentArea ? parentArea.StableId : string.Empty
            };
            return !string.IsNullOrEmpty(save.interactionId);
        }

        public static bool TryRestore(
            PlayerController target,
            PlayerRideSave save,
            Transform worldRoot,
            out string failure)
        {
            failure = string.Empty;
            if (save == null || !save.active) return false;
            if (!target || !target.Controller)
            {
                failure = "Người chơi chưa sẵn sàng khôi phục chuyến đi.";
                return false;
            }
            WorldInteractionArea area = worldRoot
                ? worldRoot.GetComponentInChildren<WorldInteractionRegistry>(true)?.Resolve(save.interactionId)
                : null;
            ICityPassengerCarrier passengerCarrier = area?.CarrierSource as ICityPassengerCarrier;
            Transform hull = passengerCarrier != null ? passengerCarrier.Hull : area?.transform;
            if (!hull)
            {
                RestoreAtSafeExit(target, save, worldRoot, area, out failure);
                return false;
            }
            WorldPlayerRide ride = target.GetComponent<WorldPlayerRide>();
            if (!ride) ride = target.gameObject.AddComponent<WorldPlayerRide>();
            if (ride.activeRide)
            {
                failure = "Bạn đang ở trên một phương tiện khác.";
                return false;
            }
            ride.Restore(target, passengerCarrier, area ? area.transform : hull,
                save, ResolveParent(worldRoot, save), hull);
            return true;
        }

        public static void CancelForLoad(PlayerController target)
        {
            WorldPlayerRide ride = target ? target.GetComponent<WorldPlayerRide>() : null;
            ride?.ClearForLoad();
        }

        void Begin(
            PlayerController target,
            ICityPassengerCarrier passengerCarrier,
            Transform fallback,
            float seconds,
            string message,
            string stableInteractionId)
        {
            player = target;
            carrier = passengerCarrier;
            fallbackHull = fallback;
            interactionId = string.IsNullOrEmpty(stableInteractionId)
                ? StableCarrierId(passengerCarrier, fallback)
                : stableInteractionId;
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

        void Restore(
            PlayerController target,
            ICityPassengerCarrier passengerCarrier,
            Transform fallback,
            PlayerRideSave save,
            Transform parent,
            Transform hull)
        {
            player = target;
            carrier = passengerCarrier;
            fallbackHull = fallback;
            interactionId = save.interactionId;
            originalParent = parent;
            exitPoint = new Vector3(save.exitX, save.exitY, save.exitZ);
            player.Controller.enabled = false;
            player.CanControl = false;
            if (carrier != null)
            {
                player.transform.SetParent(hull, true);
                player.transform.localPosition = carrier.DoorInsideLocal;
                player.transform.localRotation = Quaternion.identity;
            }
            else
            {
                player.transform.position = hull.position + Vector3.up * 1.1f;
                player.transform.rotation = hull.rotation;
            }
            endsAt = Time.time + Mathf.Max(0, save.remainingSeconds);
            activeRide = true;
            carrier?.RecordBoarding();
            GameSession.Instance?.Say("Đã khôi phục vị trí trên phương tiện.");
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
            interactionId = null;
            originalParent = null;
            player = null;
            GameSession.Instance?.Say("Đã xuống phương tiện tại điểm an toàn.");
        }

        void ClearForLoad()
        {
            if (!activeRide) return;
            carrier?.RecordAlighting();
            if (player)
            {
                player.transform.SetParent(originalParent, true);
                if (player.Controller) player.Controller.enabled = true;
            }
            activeRide = false;
            player = null;
            carrier = null;
            fallbackHull = null;
            interactionId = null;
            originalParent = null;
        }

        static Transform ResolveParent(Transform worldRoot, PlayerRideSave save)
        {
            if (save == null || save.originalParentWasNull || worldRoot == null
                || string.IsNullOrEmpty(save.originalParentId)) return null;
            return worldRoot.GetComponentInChildren<WorldInteractionRegistry>(true)
                ?.Resolve(save.originalParentId)?.transform;
        }

        static void RestoreAtSafeExit(
            PlayerController target,
            PlayerRideSave save,
            Transform worldRoot,
            WorldInteractionArea area,
            out string failure)
        {
            Vector3 safe = new Vector3(save.exitX, save.exitY, save.exitZ);
            if (!Finite(safe)) safe = area ? area.ExitPoint : target.transform.position;
            Transform parent = ResolveParent(worldRoot, save);
            target.transform.SetParent(parent, true);
            if (NavMesh.SamplePosition(safe, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                safe = hit.position + Vector3.up * .05f;
            target.transform.position = safe;
            target.Controller.enabled = true;
            target.CanControl = true;
            target.StopInteraction();
            failure = "Phương tiện đã biến mất; bạn được đưa tới điểm an toàn.";
        }

        static string StableCarrierId(ICityPassengerCarrier passengerCarrier, Transform fallback)
        {
            Component component = passengerCarrier as Component;
            WorldInteractionArea area = component
                ? component.GetComponent<WorldInteractionArea>()
                    ?? component.GetComponentInParent<WorldInteractionArea>()
                : null;
            if (area && !string.IsNullOrEmpty(area.StableId)) return area.StableId;
            string name = component ? component.name : fallback ? fallback.name : "ride";
            return "carrier_" + name.Replace("(Clone)", string.Empty).Trim();
        }

        static bool Finite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }

        void OnDisable()
        {
            if (!activeRide || !player) return;
            Transform hull = carrier?.Hull;
            if (!player.gameObject.activeInHierarchy || hull && !hull.gameObject.activeInHierarchy)
            {
                activeRide = false;
                player = null;
                carrier = null;
                fallbackHull = null;
                interactionId = null;
                originalParent = null;
                return;
            }
            EndRide();
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

        public IReadOnlyList<WorldInteractionArea> Areas
        {
            get
            {
                return world
                    ? world.GetComponentsInChildren<WorldInteractionArea>(true)
                    : Array.Empty<WorldInteractionArea>();
            }
        }

        public WorldInteractionArea Resolve(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (WorldInteractionArea area in Areas)
                if (area && string.Equals(area.StableId, id, StringComparison.Ordinal)) return area;
            return null;
        }

        public static bool TryResolve(Transform root, string id, out WorldInteractionArea area)
        {
            area = Build(root)?.Resolve(id);
            return area != null;
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
                WorldExpansionBoat boat = landmark.GetComponentInChildren<WorldExpansionBoat>(true);
                Vector3 localCenter = boat
                    ? landmark.transform.InverseTransformPoint(boat.DoorOutside)
                    : landmark.Approach == landmark.transform.position
                        ? Vector3.zero
                        : landmark.transform.InverseTransformPoint(landmark.Approach);
                WorldInteractionArea area = WorldInteractionArea.Add(
                    landmark.transform,
                    StableLandmarkId(landmark.Id),
                    label,
                    message,
                    action,
                    localCenter,
                    3.2f);
                area.StableGroup = StableLandmarkGroup(landmark.Id, action);
                if (boat && action == WorldInteractionAction.BoardVessel)
                {
                    area.CarrierSource = boat;
                    area.SetApproachPoint(
                        WorldInteractionArea.ComputeCarrierApproach(boat, area.Radius));
                }
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
            area.StableGroup = prefix;
            area.CarrierSource = carrier;
            area.RideSeconds = action == WorldInteractionAction.BoardTrain ? 7f : 4f;
            area.SetApproachPoint(
                WorldInteractionArea.ComputeCarrierApproach(carrierInterface, area.Radius));
            dynamicAreas.Add(area);
        }

        static string StableLandmarkId(string landmarkId)
        {
            string lower = (landmarkId ?? string.Empty).ToLowerInvariant();
            return lower switch
            {
                "bank" => "bank",
                "beach" => "beach_swim",
                "harbor" => "harbor_vessel",
                "marina" => "marina_vessel",
                _ => "house_" + lower
            };
        }

        static string StableLandmarkGroup(string landmarkId, WorldInteractionAction action)
        {
            if (action == WorldInteractionAction.Banking) return "bank";
            if (action == WorldInteractionAction.Swim) return "beach";
            if (action == WorldInteractionAction.BoardVessel) return "harbor";
            return string.Equals(landmarkId, "Restaurant", StringComparison.OrdinalIgnoreCase)
                ? "restaurant"
                : "house";
        }
    }
}
