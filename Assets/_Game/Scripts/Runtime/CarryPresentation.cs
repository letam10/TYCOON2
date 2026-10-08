using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public sealed class CarryPresentation : MonoBehaviour
    {
        PlayerController player;
        InventoryStack goods;
        Transform anchor;
        Transform cashRoot;
        TextMesh cashLabel;
        readonly List<GameObject> notes = new();
        long displayedCash = -1;
        int displayedGoods = -1;
        public string LastItemId { get; private set; } = "carrot";
        public Vector3 HandPoint => anchor ? anchor.position : transform.position + Vector3.up;

        public static void Attach(PlayerController player)
        {
            if (!player || player.GetComponent<CarryPresentation>()) return;
            var view = player.gameObject.AddComponent<CarryPresentation>();
            view.player = player;
            view.anchor = new GameObject("FrontHandCarry").transform;
            view.anchor.SetParent(player.transform, false);
            view.anchor.localPosition = new Vector3(0, .86f, .78f);
            view.anchor.gameObject.AddComponent<CarrySway>().Actor = player.transform;
            view.goods = view.anchor.gameObject.AddComponent<InventoryStack>();
            Configure(view.goods, player.Carry, GameSession.Instance.Pool);
            view.cashRoot = new GameObject("CashInHands").transform;
            view.cashRoot.SetParent(view.anchor, false);
            view.cashLabel = Art.Label("", new Vector3(0, .58f, 0), view.cashRoot, .12f, "#FFF3B5");
            view.RefreshCash();
        }

        public static InventoryStack AttachWorker(WorkerAgent worker)
        {
            var anchor = new GameObject("WorkerFrontHandCarry").transform;
            anchor.SetParent(worker.transform, false);
            anchor.localPosition = new Vector3(0, .86f, .78f);
            var stack = anchor.gameObject.AddComponent<InventoryStack>();
            Configure(stack, worker.Carry, GameSession.Instance.Pool);
            var sway = anchor.gameObject.AddComponent<CarrySway>();
            sway.Actor = worker.transform;
            anchor.gameObject.AddComponent<CarryWorkerTransfers>().Worker = worker;
            return stack;
        }

        static void Configure(InventoryStack stack, Inventory inventory, ItemPool pool)
        {
            stack.Inventory = inventory;
            stack.Pool = pool;
            stack.HeldLayout = true;
            stack.Columns = 3;
            stack.Rows = 2;
            stack.Maximum = 48;
            stack.Spacing = .14f;
            stack.Scale = .85f;
            stack.LayerHeight = .04f;
            stack.ShowCount = true;
            stack.MinimumCountHeight = 1.15f;
        }

        void LateUpdate()
        {
            using var frameProbe = QaFrameProbe.Measure("CarryPresentation.LateUpdate");
            if (!player || !anchor) return;
            goods.Inventory = player.Carry;
            if (displayedGoods != player.Carry.Revision)
            {
                displayedGoods = player.Carry.Revision;
                if (player.Carry.Total > 0) LastItemId = player.Carry.Snapshot()[0].id;
            }
            RefreshCash();
        }

        void RefreshCash()
        {
            var game = GameSession.Instance;
            if (!game || !cashRoot) return;
            long amount = game.Economy.CashInHand;
            if (amount == displayedCash) return;
            displayedCash = amount;
            cashRoot.gameObject.SetActive(amount > 0);
            float magnitude = amount <= 0 ? 0 : (float)System.Math.Log10((double)amount + 1);
            int count = amount <= 0 ? 0 : Mathf.Clamp(Mathf.CeilToInt(magnitude * 3), 1, 18);
            while (notes.Count < count) notes.Add(HandItemModels.Build("cash", cashRoot));
            Vector3 size = notes.Count == 0 ? Vector3.zero :
                notes[0].GetComponent<MeshFilter>().sharedMesh.bounds.size * .9f;
            int columns = Mathf.Min(3, count);
            int rows = Mathf.Min(2, Mathf.CeilToInt(count / 3f));
            for (int i = 0; i < notes.Count; i++)
            {
                notes[i].SetActive(i < count);
                notes[i].transform.localPosition = new Vector3((i % 3 - (columns - 1) * .5f) * (size.x + .015f),
                    i / 6 * (size.y + .012f), (i / 3 % 2 - (rows - 1) * .5f) * (size.z + .015f));
                notes[i].transform.localRotation = Quaternion.identity;
                notes[i].transform.localScale = Vector3.one * .9f;
            }
            cashLabel.text = amount.ToString("N0") + " xu";
            float top = Mathf.Max(0, Mathf.CeilToInt(count / 6f) - 1) * (size.y + .012f) + size.y;
            cashLabel.transform.localPosition = new Vector3(0, Mathf.Max(1.15f, top + .16f), 0);
            cashLabel.GetComponent<BillboardLabel>()?.RefreshNow();
        }
    }

    [DefaultExecutionOrder(20)]
    public sealed class CarrySway : MonoBehaviour
    {
        public Transform Actor;
        Vector3 previous;
        Vector3 origin;
        Vector3 filteredPosition;
        Quaternion filteredRotation;
        float movement;
        bool initialized;
        void LateUpdate()
        {
            if (!Actor) return;
            if (!initialized)
            {
                previous = Actor.position;
                origin = transform.localPosition;
                filteredPosition = transform.position;
                filteredRotation = transform.rotation;
                initialized = true;
            }
            float distance = Vector3.Distance(previous, Actor.position);
            float speed = distance / Mathf.Max(.001f, Time.deltaTime);
            previous = Actor.position;
            float blend = 1 - Mathf.Exp(-18 * Time.deltaTime);
            bool simulate = !GameSession.Instance || GameSession.Instance.CanSimulate;
            movement = Mathf.Lerp(movement, simulate ? Mathf.Clamp01(speed / 4) : 0, blend);
            Vector3 target = Actor.TransformPoint(origin + Vector3.up *
                (Mathf.Sin(Time.time * 12) * .012f * movement));
            Quaternion rotation = Actor.rotation * Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 6) * movement);
            if (distance > 2)
            {
                filteredPosition = target;
                filteredRotation = rotation;
            }
            // Lọc trong không gian thế giới rồi giới hạn trễ để hàng vẫn nằm sát tay khi quay nhanh.
            filteredPosition = Vector3.Lerp(filteredPosition, target, blend);
            filteredPosition = target + Vector3.ClampMagnitude(filteredPosition - target, .12f);
            filteredRotation = Quaternion.Slerp(filteredRotation, rotation, blend);
            filteredRotation = Quaternion.RotateTowards(rotation, filteredRotation, 8);
            transform.SetPositionAndRotation(filteredPosition, filteredRotation);
        }
    }
}
