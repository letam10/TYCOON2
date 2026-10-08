using UnityEngine;

namespace Tycoon
{
    public sealed class SafeStation : Station
    {
        public override bool Interact(PlayerController player, bool withdraw) => false;
        public SafeInteractionArea Deposit { get; private set; }
        public SafeInteractionArea Withdraw { get; private set; }
        public override string Prompt => "Két tiền • chọn Gửi hoặc Rút";

        public static void Build(GameSession game, Transform parent)
        {
            var root = new GameObject("StarterSafe");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(-20.5f, 0, -5.3f);
            var station = root.AddComponent<SafeStation>();
            station.Id = PhysicalCashRules.SafeId;
            station.Label = "Két tiền";
            station.AreaId = "farm";
            station.InteractionPoint = root.transform.position;
            station.Deposit = new SafeInteractionArea(station, true, new Vector3(-1.65f, 0, 0));
            station.Withdraw = new SafeInteractionArea(station, false, new Vector3(1.65f, 0, 0));
            Art.Box("SafeBase", new(0, .12f, 0), new(1.6f, .24f, 1.3f), "#777F7B", root.transform, true);
            Art.Box("SafeBody", new(0, .83f, 0), new(1.3f, 1.3f, 1.05f), "#426762", root.transform, true);
            Art.Box("SafeDoor", new(0, .84f, .55f), new(1.12f, 1.1f, .1f), "#92AAA2", root.transform);
            Art.Box("SafeInset", new(0, .84f, .615f), new(.85f, .83f, .03f), "#284D49", root.transform);
            var dial = Art.Cylinder("SafeDial", new(.18f, .91f, .67f), new(.26f, .035f, .26f), "#CFB77F", root.transform);
            dial.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Art.Box("SafeHandle", new(-.22f, .78f, .69f), new(.06f, .32f, .12f), "#D0D9D4", root.transform);
            BuildZone(root.transform, -1.65f, "GỬI", "#69AC8E");
            BuildZone(root.transform, 1.65f, "RÚT", "#D5B45F");
            Art.Label("KÉT TIỀN", new(0, 1.75f, 0), root.transform, .13f);
            game.Stations.Add(station);
        }

        static void BuildZone(Transform parent, float x, string label, string color)
        {
            Art.Cylinder(label, new(x, .025f, 0), new(1.28f, .015f, 1.28f), color, parent);
            Art.Label(label, new(x, .28f, .05f), parent, .13f);
        }
    }

    public sealed class SafeInteractionArea : IPlayerInteractionArea
    {
        readonly SafeStation station;
        readonly bool deposit;
        readonly Vector3 offset;
        bool completed;

        public SafeInteractionArea(SafeStation station, bool deposit, Vector3 offset)
        {
            this.station = station;
            this.deposit = deposit;
            this.offset = offset;
        }

        public InteractionKind Kind => deposit ? InteractionKind.DepositCash : InteractionKind.WithdrawCash;
        public Vector3 Center => station.transform.TransformPoint(offset);
        public bool Available => station && station.isActiveAndEnabled && GameSession.Instance.CanSimulate;
        public bool Contains(Vector3 point) => Vector3.ProjectOnPlane(point - Center, Vector3.up).sqrMagnitude < .49f;
        public string Hint => deposit ? "Gửi tiền vào két" : "Rút tiền lên tay";

        public InteractionResult Perform(InteractionContext context, float delta)
        {
            var game = GameSession.Instance;
            if (!context.Player || !Contains(context.Player.transform.position) || delta <= 0 || completed)
                return new InteractionResult(false);
            if (context.Player.Carry.Total > 0 || PhysicalCashRules.HasGoods(game.Transactions.View))
                return InteractionResult.Reject("Cất hàng để lấy tiền.");
            long amount = deposit ? game.Economy.CashInHand : game.Economy.CashInSafe;
            if (amount == 0) return InteractionResult.Reject(deposit ? "Tay không có tiền." : "Két đang trống.");
            var kind = deposit ? TransactionKind.DepositCash : TransactionKind.WithdrawCash;
            var command = game.Transactions.Command(kind, "player", station.Id);
            if (!game.Transactions.TryExecute(command, out _))
                return InteractionResult.Reject(game.Transactions.LastReason);
            completed = true;
            context.Player.View?.Interact(!deposit);
            game.Feedback?.Collect(Center + Vector3.up * .8f);
            var hand = context.Player.transform.position + Vector3.up;
            var safe = station.transform.position + Vector3.up;
            game.Feedback?.CashTransfer(deposit ? hand : safe, deposit ? safe : hand);
            game.Say(deposit ? "Đã gửi tiền vào két" : "Đã rút tiền lên tay");
            return new InteractionResult(true);
        }

        public void Exit(EntityId actor) => completed = false;
    }
}
