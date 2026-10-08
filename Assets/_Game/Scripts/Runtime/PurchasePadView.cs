using UnityEngine;

namespace Tycoon
{
    public sealed partial class PurchasePad
    {
        PurchaseIconView icon;
        TextMesh priceLabel;
        Transform progressFill;
        string shownUpgrade;
        bool initialized;
        Renderer[] displayRenderers;
        Collider[] interactionColliders;
        bool visibilityInitialized;
        bool displayedVisible;
        bool displayedCollidable;
        int displayedRemaining = -1;
        bool displayedCompleting;

        public void RestoreAfterLoad()
        {
            StopContributing();
            transitionUntil = 0;
            gameObject.SetActive(true);
            if (initialized) InitializeIcon();
        }

        public void InitializeIcon()
        {
            if (Upgrade == null) return;
            if (!icon)
            {
                var root = new GameObject("PurchaseIcon2D");
                root.transform.SetParent(transform, false);
                root.transform.localPosition = new Vector3(0, .72f, .1f);
                root.transform.localScale = Vector3.one * .95f;
                icon = root.AddComponent<PurchaseIconView>();
            }
            icon.SetUpgrade(Upgrade);
            if (!priceLabel)
                foreach (var label in GetComponentsInChildren<TextMesh>(true))
                    if (label != StatusLabel)
                    {
                        priceLabel = label;
                        break;
                    }
            if (!priceLabel) priceLabel = Art.Label("", Vector3.zero, transform, .22f, "#FFFFFF", false);
            priceLabel.transform.localPosition = new Vector3(0, .32f, -.05f);
            priceLabel.characterSize = .032f;
            if (!progressFill)
            {
                Art.Box("ContributionTrack", new Vector3(0, .1f, -.58f),
                    new Vector3(1.15f, .035f, .12f), "#283E35", transform);
                progressFill = Art.Box("ContributionProgress", new Vector3(-.575f, .125f, -.58f),
                    new Vector3(.01f, .028f, .08f), "#8CC799", transform).transform;
            }
            shownUpgrade = Upgrade.id;
            initialized = true;
            displayRenderers = GetComponentsInChildren<Renderer>(true);
            interactionColliders = GetComponentsInChildren<Collider>(true);
            visibilityInitialized = false;
            displayedRemaining = -1;
            RefreshDisplay();
        }

        protected override void LateUpdate()
        {
            var game = GameSession.Instance;
            if (!game || Upgrade == null) return;
            if (!initialized || shownUpgrade != Upgrade.id)
            {
                StopContributing();
                InitializeIcon();
            }
            RefreshDisplay();
            if (game.Player && (game.Player.transform.position - InteractionPoint).sqrMagnitude > 3)
                StopContributing();
        }

        void RefreshDisplay()
        {
            var evaluation = Evaluation;
            bool completing = Time.time < transitionUntil;
            bool visible = !IsDuplicateTier && evaluation.State != PurchaseState.Locked &&
                (evaluation.State != PurchaseState.Purchased || completing);
            bool collidable = visible && !completing;
            // Collider trang trí bị Destroy cuối frame; cache có thể còn tham chiếu đã chết.
            if (!visibilityInitialized || displayedVisible != visible)
                foreach (var renderer in displayRenderers)
                    if (renderer) renderer.enabled = visible;
            if (!visibilityInitialized || displayedCollidable != collidable)
                foreach (var collider in interactionColliders)
                    if (collider) collider.enabled = collidable;
            displayedVisible = visible;
            displayedCollidable = collidable;
            visibilityInitialized = true;
            if (StatusLabel && StatusLabel.gameObject.activeSelf) StatusLabel.gameObject.SetActive(false);
            if (!visible && (IsDuplicateTier || evaluation.State == PurchaseState.Purchased))
            {
                // Ô cuối biến mất cả component và vùng tương tác; load dựng lại theo save.
                StopContributing();
                gameObject.SetActive(false);
                return;
            }
            if (!priceLabel || !progressFill) return;
            int remaining = evaluation.Cost - evaluation.Contributed;
            if (displayedRemaining != remaining || displayedCompleting != completing)
            {
                priceLabel.text = completing ? "HOÀN TẤT" : remaining.ToString("N0") + " xu";
                displayedRemaining = remaining;
                displayedCompleting = completing;
            }
            if (Camera.main) priceLabel.transform.rotation = Camera.main.transform.rotation;
            // Cỡ chữ theo số ký tự có ngay frame đầu; không lấy bounds còn giữ chuỗi cũ.
            priceLabel.transform.localScale = Vector3.one * Mathf.Min(.9f, 4.8f / Mathf.Max(1, priceLabel.text.Length));
            float progress = completing ? 1 : evaluation.Cost > 0 ? (float)evaluation.Contributed / evaluation.Cost : 0;
            progressFill.localScale = new Vector3(Mathf.Max(.001f, progress * 1.15f), .028f, .08f);
            progressFill.localPosition = new Vector3(-.575f + progress * .575f, .125f, -.58f);
            if (icon)
            {
                float pulse = completing ? 1 + Mathf.Sin((.6f - transitionUntil + Time.time) / .6f * Mathf.PI) * .12f : 1;
                icon.transform.localScale = Vector3.one * (.95f * pulse);
            }
        }
    }
}
