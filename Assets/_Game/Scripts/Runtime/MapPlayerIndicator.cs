using UnityEngine;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed class MapPlayerIndicator : MaskableGraphic
    {
        CityMapGraphic map;
        PlayerController player;

        public void Initialize(CityMapGraphic source, PlayerController actor)
        { map = source; player = actor; raycastTarget = false; }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (!map || !player) return;
            var rect = rectTransform.rect;
            Vector2 center = rect.min + Vector2.Scale(map.Position(player.transform.position), rect.size);
            Vector3 forward = player.transform.forward;
            Vector2 direction = new Vector2(forward.x, forward.z).normalized;
            Vector2 side = new(-direction.y, direction.x);
            Triangle(mesh, center, direction, side, 1.35f, new Color32(18, 40, 34, 255));
            Triangle(mesh, center, direction, side, 1, Color.white);
        }

        static void Triangle(VertexHelper mesh, Vector2 center, Vector2 direction, Vector2 side, float scale, Color tint)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(center + direction * (14 * scale), tint, Vector2.zero);
            mesh.AddVert(center + (-direction * 7 + side * 8) * scale, tint, Vector2.zero);
            mesh.AddVert(center + (-direction * 7 - side * 8) * scale, tint, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
        }
    }
}
