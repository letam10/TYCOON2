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
            ("harbor", "Bến cảng", new(197, .05f, -123))
        };

        public static bool Teleport(GameSession game, int index, out string reason)
        {
            if (index < 0 || index >= Points.Length || game.SaveBlocked || game.IsRestoring
                || !game.NavigationReady)
            { reason = "Bản đồ chưa sẵn sàng."; return false; }
            var player = game.Player;
            var body = player.Controller;
            // Chỉ đặt người lên nền đi được, đủ chỗ cho capsule; giữ nguyên hàng và tiền đang cầm.
            if (!NavMesh.SamplePosition(Points[index].position, out var hit, 3, NavMesh.AllAreas))
            { reason = "Điểm dịch chuyển đang bị chặn."; return false; }
            Vector3 target = hit.position + Vector3.up * .05f;
            Vector3 lower = target + Vector3.up * (body.radius + body.skinWidth);
            Vector3 upper = target + Vector3.up * (body.height - body.radius);
            foreach (var collider in Physics.OverlapCapsule(lower, upper, body.radius * .9f,
                Physics.AllLayers, QueryTriggerInteraction.Ignore))
                if (!collider.transform.IsChildOf(player.transform))
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
