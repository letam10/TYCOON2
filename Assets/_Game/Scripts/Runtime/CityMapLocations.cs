using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public static class CityMapLocations
    {
        public static readonly (string id, string label, Vector3 position)[] Points =
        {
            ("tycoon", "TYCOON • Nông trại", CityDistricts.PlayerSpawn),
            ("park", "Công viên", CityDistricts.ParkCenter + Vector3.back * 8),
            ("station", "Nhà ga", new(CityRailNetwork.StationX, .05f, CityRailNetwork.StationBounds.max.z - 3)),
            ("beach", "Bãi biển", new(202, .05f, 77)),
            ("harbor", "Bến cảng", new(197, .05f, -123)),
            ("marina", "Bến du thuyền", new(199, .05f, -40)),
            ("bank", "Ngân hàng", new(-226, .05f, 101)),
            ("restaurant", "Nhà hàng", CityDistricts.Center("restaurant") + Vector3.back * 8),
            ("forest", "Rừng nguyên sinh", new(30, .05f, 133)),
            ("forest_reserve", "Khu bảo tồn rừng", new(-420, .05f, 250)),
            ("industrial_yard", "Bãi công nghiệp", new(465, .05f, 330)),
            ("north_village", "Làng phía Bắc", new(0, .05f, 330)),
            ("east_marina", "Bến du thuyền phía Đông", new(370, .05f, -135)),
            ("south_highway", "Xa lộ phía Nam", new(0, .05f, -300))
        };

        public static bool Teleport(GameSession game, int index, out string reason)
        {
            if (index < 0 || index >= Points.Length || game.SaveBlocked || game.IsRestoring
                || !game.NavigationReady)
            { reason = "Bản đồ chưa sẵn sàng."; return false; }
            var player = game.Player;
            var body = player.Controller;
            // Chỉ đặt người lên nền đi được, đủ chỗ cho capsule; giữ nguyên hàng và tiền đang cầm.
            bool expansion = index >= 9;
            bool sampled = NavMesh.SamplePosition(Points[index].position, out var hit, 3, NavMesh.AllAreas);
            if (!sampled && !expansion)
            { reason = "Điểm dịch chuyển đang bị chặn."; return false; }
            Vector3 target = sampled ? hit.position + Vector3.up * .05f : Points[index].position;
            Vector3 lower = target + Vector3.up * (body.radius + body.skinWidth);
            Vector3 upper = target + Vector3.up * (body.height - body.radius);
            foreach (var collider in Physics.OverlapCapsule(lower, upper, body.radius * .9f,
                Physics.AllLayers, QueryTriggerInteraction.Ignore))
                if (!collider.transform.IsChildOf(player.transform) && !expansion)
                { reason = "Điểm dịch chuyển đang bị chặn."; return false; }
            player.CanControl = false;
            body.enabled = false;
            player.transform.position = target;
            body.enabled = true;
            game.CameraRig.Snap();
            reason = "Đã đến " + GameLocalization.Translate(Points[index].label);
            return true;
        }
    }
}
