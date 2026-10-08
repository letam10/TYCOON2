using UnityEngine;

namespace Tycoon
{
    public static class CityTrafficSafety
    {
        public static bool ActorAhead(Vector3 position, Vector3 forward, float step, Vector3 actor)
        {
            forward.y = 0;
            if (forward.sqrMagnitude < .001f) return false;
            forward.Normalize();
            Vector3 offset = actor - position;
            offset.y = 0;
            float along = Vector3.Dot(offset, forward);
            float ahead = Mathf.Max(5, step + 2.7f);
            if (along < 0 || along > ahead) return false;
            // Hành lang liên tục phủ cả đầu xe; người đã đi ra sau không giữ xe đứng chờ.
            return (offset - forward * along).sqrMagnitude <= 1.7f * 1.7f;
        }

        public static bool CarAhead(Vector3 position, Vector3[] route, int next, int lane,
            Vector3 otherPosition, int otherNext, int otherLane, float step)
        {
            if (lane != otherLane) return false;
            // Khoảng cách dọc vòng đường giữ khoảng cách khi xe trước vừa rẽ qua góc.
            float gap = GroundDistance(position, route[next]) - GroundDistance(otherPosition, route[otherNext]);
            int current = next;
            while (current != otherNext)
            {
                int following = (current + 1) % route.Length;
                gap += GroundDistance(route[current], route[following]);
                current = following;
            }
            if (gap < 0)
                for (int i = 0; i < route.Length; i++)
                    gap += GroundDistance(route[i], route[(i + 1) % route.Length]);
            return gap > .001f && gap <= Mathf.Max(6, step + 4.6f);
        }

        static float GroundDistance(Vector3 a, Vector3 b)
        {
            a.y = 0;
            b.y = 0;
            return Vector3.Distance(a, b);
        }
    }
}
