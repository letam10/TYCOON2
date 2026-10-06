using UnityEngine;
namespace Tycoon
{
    // Viền vật thể đang tương tác chỉ là view, không tạo ô thao tác hoặc sửa state.
    public sealed class InteractionFocusView:MonoBehaviour
    {
        LineRenderer line, progress;
        Material material;
        Color displayedColor;
        readonly Vector3[] outline = new Vector3[32];
        void Start()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            material = shader ? new Material(shader) : new Material(Art.Material("#FFD875"));
            material.name = "InteractionAccent";
            line = CreateLine(gameObject, 32, true, .055f);
            var arc = new GameObject("ActionProgress"); arc.transform.SetParent(transform, false);
            progress = CreateLine(arc, 33, false, .065f);
            displayedColor = Art.Hex("#FFD875");
        }
        LineRenderer CreateLine(GameObject owner, int count, bool loop, float width)
        {
            var renderer = owner.AddComponent<LineRenderer>();
            renderer.sharedMaterial = material; renderer.positionCount = count; renderer.loop = loop;
            renderer.useWorldSpace = true; renderer.widthMultiplier = width; renderer.numCapVertices = 3;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.enabled = false; return renderer;
        }
        void LateUpdate()
        {
            if (!line) return;
            var player = GameSession.Instance?.Player;
            var area = player?.ActiveInteraction;
            line.enabled = area != null && area.Available;
            progress.enabled = false;
            if (!line.enabled) return;
            bool blocked = !string.IsNullOrEmpty(player.InteractionReason);
            string hex = blocked ? "#F7AD63" : area.Kind switch
            { InteractionKind.Pickup => "#85E6B8", InteractionKind.Drop => "#86D8F1", InteractionKind.Collect or InteractionKind.Purchase => "#FFD875", InteractionKind.Repair => "#F5AD78", _ => "#D9E5A9" };
            displayedColor = Color.Lerp(displayedColor, Art.Hex(hex), 1 - Mathf.Exp(-12 * Time.deltaTime));
            material.SetColor("_BaseColor", displayedColor.linear);
            line.widthMultiplier = .05f + Mathf.Sin(Time.time * 4) * .005f;
            Vector3 center = area.Center + Vector3.up * .115f;
            Station station = area is ProximityTarget target ? target.Target : area is StationZone zone ? zone.Target : null;
            Vector2 half = area is ProximityTarget proximity && !proximity.Cash ? station switch
            {
                ProductionStation p when !p.Animal => new(2.15f, 2.15f),
                ShelfStation => new(.85f, 2.5f), StorageStation => new(1.8f, 1.4f),
                MachineStation => new(1.5f, 1.3f), CheckoutStation => new(1.45f, .75f),
                CargoDock => new(.7f, .7f), _ => new(1.2f, 1.2f)
            } : new(.65f, .65f);
            // Viền bo góc dùng buffer cố định; không sinh mảng hoặc vật thể mỗi khung hình.
            const float radius = .22f;
            for (int corner = 0; corner < 4; corner++)
            {
                float x = corner is 0 or 3 ? half.x - radius : -half.x + radius;
                float z = corner < 2 ? half.y - radius : -half.y + radius;
                for (int step = 0; step < 8; step++)
                {
                    float angle = (corner * 90 + step * 90f / 7) * Mathf.Deg2Rad;
                    outline[corner * 8 + step] = center + new Vector3(x + Mathf.Cos(angle) * radius, 0, z + Mathf.Sin(angle) * radius);
                }
            }
            line.SetPositions(outline);
            if (!blocked && station is ProductionStation plot && !plot.Animal && plot.Phase != 2 && plot.Action > 0)
            {
                progress.enabled = true;
                Vector3 origin = player.transform.position + Vector3.up * .13f;
                float fraction = Mathf.Clamp01(plot.Action);
                for (int i = 0; i <= 32; i++)
                {
                    float angle = (-90 + 360 * fraction * i / 32) * Mathf.Deg2Rad;
                    progress.SetPosition(i, origin + new Vector3(Mathf.Cos(angle) * .78f, 0, Mathf.Sin(angle) * .78f));
                }
            }
        }
        void OnDestroy() { if (material) Destroy(material); }
    }
}
