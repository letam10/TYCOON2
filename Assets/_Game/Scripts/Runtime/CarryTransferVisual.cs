using UnityEngine;

namespace Tycoon
{
    public sealed class CarryTransferVisual : MonoBehaviour
    {
        Vector3 start;
        Vector3 end;
        float elapsed;
        ItemPool pool;
        const float Duration = .5f;

        public static void Play(string itemId, Vector3 from, Vector3 to, Transform parent = null)
        {
            if (!HandItemModels.Supports(itemId)) return;
            var game = GameSession.Instance;
            var pool = game ? game.Pool : null;
            var root = pool != null ? pool.Take(itemId, parent) : HandItemModels.Build(itemId, parent);
            root.transform.position = from;
            root.transform.localScale = Vector3.one * .75f;
            var motion = root.GetComponent<CarryTransferVisual>();
            if (!motion) motion = root.AddComponent<CarryTransferVisual>();
            motion.pool = pool;
            motion.start = from;
            motion.end = to;
            motion.elapsed = 0;
            motion.enabled = true;
        }

        void Update()
        {
            if (GameSession.Instance && !GameSession.Instance.CanSimulate) return;
            Advance(Time.deltaTime);
        }

        public void Advance(float delta)
        {
            if (!enabled || delta <= 0 || !float.IsFinite(delta)) return;
            elapsed += delta;
            float progress = Mathf.Clamp01(elapsed / Duration);
            float eased = progress * progress * (3 - 2 * progress);
            transform.position = Vector3.Lerp(start, end, eased) +
                Vector3.up * (Mathf.Sin(progress * Mathf.PI) * .45f);
            transform.rotation = Quaternion.Euler(0, progress * 120, progress * 12);
            transform.localScale = Vector3.one * (.75f * Mathf.Lerp(1, .2f, Mathf.InverseLerp(.8f, 1, progress)));
            if (progress < 1) return;
            // Component nghỉ khi model được dùng lại cho chồng hàng; không tự bay khỏi tay.
            enabled = false;
            if (pool != null) pool.Return(gameObject);
            else Destroy(gameObject);
        }
    }
}
