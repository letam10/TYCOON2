using UnityEngine;

namespace Tycoon
{
    public sealed partial class PurchasePad : Station, IPlayerInteractionArea
    {
        UpgradeDefinition upgrade;
        UpgradeDefinition completingUpgrade;
        float held;
        float settled;
        float activeSeconds;
        float transitionUntil;
        float nextCashArc;
        public UpgradeDefinition BaseUpgrade => upgrade;
        public UpgradeDefinition Upgrade
        {
            get
            {
                var game = GameSession.Instance;
                var current = upgrade;
                if (!game) return current;
                if (Time.time < transitionUntil) return completingUpgrade ?? current;
                while (current != null && game.Economy.Has(current.id))
                {
                    var next = NextUpgrade(current);
                    if (next == null) break;
                    current = next;
                }
                return current;
            }
            set
            {
                upgrade = value;
                transitionUntil = 0;
                completingUpgrade = null;
                StopContributing();
            }
        }
        public static UpgradeDefinition NextUpgrade(UpgradeDefinition current)
        {
            if (current == null || current.axis == UpgradeAxis.None) return null;
            foreach (var candidate in Definitions.Upgrades)
                if (candidate.kind != "legacy" && candidate.family == current.family &&
                    candidate.axis == current.axis && candidate.axisLevel == current.axisLevel + 1 &&
                    candidate.requirement == current.id) return candidate;
            return null;
        }
        bool IsDuplicateTier
        {
            get
            {
                var game = GameSession.Instance;
                if (!game || upgrade == null || upgrade.axisLevel <= 2) return false;
                foreach (var station in game.Stations)
                    if (station is PurchasePad pad && pad != this && pad.BaseUpgrade != null &&
                        pad.BaseUpgrade.family == upgrade.family && pad.BaseUpgrade.axis == upgrade.axis &&
                        pad.BaseUpgrade.axisLevel < upgrade.axisLevel) return true;
                return false;
            }
        }
        public InteractionKind Kind => InteractionKind.Purchase;
        public Vector3 Center => InteractionPoint;
        public PurchaseEvaluation Evaluation => GameSession.Instance.Progression.Evaluate(Upgrade);
        public bool Available => this && isActiveAndEnabled && GameSession.Instance &&
            !IsDuplicateTier && Time.time >= transitionUntil && Evaluation.CanContribute;
        public bool Contains(Vector3 point) => Available && ContainsInteractionPoint(point);
        public InteractionResult Perform(InteractionContext context, float delta) =>
            context.Player && !Contains(context.Player.transform.position)
                ? InteractionResult.Reject("Hãy đứng trong vùng mua.") : Perform(Kind, context, delta);
        public void Exit(EntityId actor) => EndInteraction(actor);

        public static float ContributionRate(int cost, float activeSeconds) =>
            Mathf.Max(35, cost / 12f) * Mathf.Lerp(1, 5, Mathf.Clamp01(activeSeconds / 2.5f));

        public bool HoldToBuy(float delta)
        {
            var game = GameSession.Instance;
            if (delta <= 0 || !float.IsFinite(delta) || !game || !game.CanSimulate ||
                Time.time < transitionUntil || IsDuplicateTier || !game.CanPurchase(Upgrade, out _) ||
                game.Economy.CashInHand <= 0)
            {
                StopContributing();
                return false;
            }
            // Pad được gọi trực tiếp: .25 giây là tổng chờ, không cộng thêm dwell của ProximityTarget.
            float delay = Mathf.Max(0, .25f - settled);
            settled += delta;
            delta = Mathf.Max(0, delta - delay);
            if (delta == 0) return false;
            var target = Upgrade;
            float before = activeSeconds;
            activeSeconds += delta;
            // Tích phân ramp để tốc độ góp không phụ thuộc tần số frame.
            held += IntegratedRate(target.cost, before, activeSeconds);
            int amount = Mathf.FloorToInt(Mathf.Min(held, target.cost - game.Contribution(target.id)));
            if (amount <= 0) return false;
            int moved = game.Contribute(target, amount);
            if (moved <= 0) return false;
            held -= moved;
            if (Time.time >= nextCashArc && game.Player)
            {
                nextCashArc = Time.time + .15f;
                var carry = game.Player.GetComponent<CarryPresentation>();
                CarryTransferVisual.Play("cash", carry ? carry.HandPoint : game.Player.transform.position + Vector3.up,
                    transform.position + Vector3.up * .3f, game.transform);
            }
            if (game.Economy.Has(target.id))
            {
                completingUpgrade = target;
                transitionUntil = Time.time + .6f;
                StopContributing();
                UpgradeModelView.RefreshAll(game, true);
            }
            return true;
        }
        static float IntegratedRate(int cost, float start, float end)
        {
            float Ramp(float value)
            {
                float first = Mathf.Min(value, 2.5f);
                return first + .8f * first * first + Mathf.Max(0, value - 2.5f) * 5;
            }
            return Mathf.Max(35, cost / 12f) * (Ramp(end) - Ramp(start));
        }
        public void StopContributing()
        {
            held = 0;
            settled = 0;
            activeSeconds = 0;
        }
        public override string Prompt => Upgrade == null ? "" : Upgrade.label + "\n" + StatusText(Evaluation);
        public override bool Interact(PlayerController player, bool withdraw) =>
            !withdraw && player && Contains(player.transform.position) && HoldToBuy(Time.deltaTime);
        static string StatusText(PurchaseEvaluation e) => e.State switch
        {
            PurchaseState.Locked => "CHƯA ĐỦ ĐIỀU KIỆN • " + string.Join(" ", e.Requirements),
            PurchaseState.Available => "CÒN " + (e.Cost - e.Contributed).ToString("N0") + " xu",
            PurchaseState.Contributing => "ĐÃ GÓP " + e.Contributed.ToString("N0") + "/" + e.Cost.ToString("N0") + " xu",
            _ => "HOÀN TẤT"
        };
    }
}
