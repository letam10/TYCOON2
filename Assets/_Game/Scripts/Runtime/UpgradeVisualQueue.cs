using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public sealed class UpgradeVisualQueue : MonoBehaviour
    {
        readonly Queue<UpgradeModelView> pending = new();

        public static void Enqueue(GameSession game, UpgradeModelView view)
        {
            var queue = game.GetComponent<UpgradeVisualQueue>();
            if (!queue) queue = game.gameObject.AddComponent<UpgradeVisualQueue>();
            queue.pending.Enqueue(view);
        }

        void LateUpdate()
        {
            if (pending.Count == 0) return;
            using var operation = CityOperationTiming.Measure(GameSession.Instance, "UpgradeVisuals.BuildPending");
            double start = Time.realtimeSinceStartupAsDouble;
            int count = 0;
            // Dữ liệu cấp đã cập nhật; chỉ chia phần dựng hình nặng sang các frame kế tiếp.
            while (pending.Count > 0 && count < 2)
            {
                var view = pending.Dequeue();
                if (view) view.BuildPendingVisual();
                count++;
                if (Time.realtimeSinceStartupAsDouble - start > .002) break;
            }
        }
    }
}
