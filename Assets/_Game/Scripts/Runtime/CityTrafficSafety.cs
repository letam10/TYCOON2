using UnityEngine;

namespace Tycoon
{
    public static class CityTrafficSafety
    {
        // Bán kính cua 4 m cộng bề ngang làn và phần quét của hai thân xe dài 8.6 m.
        const float CornerClearance = 15;
        static readonly Vector3[] TransitRoute = CityRoadRoute.Round(CityPublicTransportVehicle.Lane);
        static readonly Vector3[] Corners =
        {
            new(-105, 0, -45), new(102, 0, -45), new(102, 0, 29), new(-105, 0, 29)
        };

        public static bool VehicleAhead(Vector3 position, Vector3 forward, float step,
            Vector3 otherPosition, Vector3 otherForward)
        {
            int lane = RoadLane(position, forward);
            int otherLane = RoadLane(otherPosition, otherForward);
            if (lane != otherLane)
                return CornerOccupied(position, step, otherPosition, lane);

            // Xe cùng làn dùng thứ tự trên vòng đường, không dùng hình nón cắt qua góc cua.
            float gap = RouteProgress(otherPosition) - RouteProgress(position);
            float length = 0;
            for (int i = 0; i < TransitRoute.Length; i++)
                length += GroundDistance(TransitRoute[i], TransitRoute[(i + 1) % TransitRoute.Length]);
            if (lane == 1) gap = -gap;
            gap = Mathf.Repeat(gap, length);
            // Khoảng dọc tuyến lớn hơn chiều dài thân vì hai xe còn xoay khác hướng ở góc.
            return gap > .001f && gap <= 12 + step;
        }

        static bool CornerOccupied(Vector3 position, float step, Vector3 otherPosition, int lane)
        {
            for (int i = 0; i < Corners.Length; i++)
            {
                Vector3 own = position - Corners[i];
                Vector3 other = otherPosition - Corners[i];
                float ownDistance = Mathf.Max(Mathf.Abs(own.x), Mathf.Abs(own.z));
                float otherDistance = Mathf.Max(Mathf.Abs(other.x), Mathf.Abs(other.z));
                if (ownDistance > CornerClearance + step + .1f || otherDistance > CornerClearance)
                    continue;
                if (ownDistance > CornerClearance) return true;

                // Xe đã qua sâu hơn được ra trước; cùng mức thì làn ngoài thắng, tránh chờ lẫn nhau.
                Vector3 incoming = (Corners[i] - Corners[(i + 3) % 4]).normalized;
                Vector3 outgoing = (Corners[(i + 1) % 4] - Corners[i]).normalized;
                Vector3 diagonal = incoming + outgoing;
                float ownProgress = Vector3.Dot(own, diagonal) * (lane == 0 ? 1 : -1);
                float otherProgress = Vector3.Dot(other, diagonal) * (lane == 0 ? -1 : 1);
                if (Mathf.Abs(ownProgress - otherProgress) <= .001f) return lane == 1;
                return ownProgress < otherProgress;
            }
            return false;
        }

        static int RoadLane(Vector3 position, Vector3 forward)
        {
            Vector3 fromCenter = position - new Vector3(-1.5f, 0, -8);
            return Vector3.Dot(Vector3.Cross(Vector3.up, forward), fromCenter) >= 0 ? 0 : 1;
        }

        static float RouteProgress(Vector3 position)
        {
            position.y = 0;
            float nearest = float.PositiveInfinity;
            float progress = 0;
            float distance = 0;
            for (int i = 0; i < TransitRoute.Length; i++)
            {
                Vector3 start = TransitRoute[i];
                Vector3 segment = TransitRoute[(i + 1) % TransitRoute.Length] - start;
                float length = segment.magnitude;
                float fraction = Mathf.Clamp01(Vector3.Dot(position - start, segment) / segment.sqrMagnitude);
                float squared = (position - start - segment * fraction).sqrMagnitude;
                if (squared < nearest)
                {
                    nearest = squared;
                    progress = distance + length * fraction;
                }
                distance += length;
            }
            return progress;
        }

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
