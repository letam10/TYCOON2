using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tycoon
{
    public sealed class PurchaseBurst : MonoBehaviour
    {
        readonly List<Transform> sparks = new();
        readonly Vector3[] origins = new Vector3[12];
        readonly Vector3[] velocities = new Vector3[12];
        float elapsed;
        int count;

        public void Begin(Vector3 point, int amount, float size, string first, string second)
        {
            elapsed = 0;
            count = Mathf.Clamp(amount, 1, 12);
            transform.position = point;
            transform.localScale = Vector3.one;
            while (sparks.Count < count)
            {
                var spark = Art.Box("Spark", Vector3.zero, Vector3.one, first, transform);
                var renderer = spark.GetComponent<Renderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                sparks.Add(spark.transform);
            }
            for (int i = 0; i < sparks.Count; i++)
            {
                sparks[i].gameObject.SetActive(i < count);
                if (i >= count) continue;
                float angle = i * Mathf.PI * 2 / count;
                origins[i] = new Vector3(Mathf.Cos(angle), .15f, Mathf.Sin(angle)) * .2f;
                Vector3 outward = origins[i];
                outward.y = 0;
                velocities[i] = outward.normalized * (1 + i % 3 * .25f) +
                    Vector3.up * (1.6f + i % 2 * .4f);
                sparks[i].localPosition = origins[i];
                sparks[i].localRotation = Quaternion.identity;
                sparks[i].localScale = Vector3.one * size;
                sparks[i].GetComponent<Renderer>().sharedMaterial = Art.Material(i % 2 == 0 ? first : second);
            }
            gameObject.SetActive(true);
        }

        void Update()
        {
            if (GameSession.Instance && !GameSession.Instance.CanSimulate) return;
            Advance(Time.deltaTime);
        }

        public void Advance(float delta)
        {
            if (!gameObject.activeSelf || delta <= 0 || !float.IsFinite(delta)) return;
            elapsed += delta;
            for (int i = 0; i < count; i++)
            {
                // Quỹ đạo theo thời gian tuyệt đối: không lệch tốc độ khi FPS thay đổi.
                sparks[i].localPosition = origins[i] + velocities[i] * elapsed +
                    Vector3.down * (2 * elapsed * elapsed);
                sparks[i].localRotation = Quaternion.Euler(new Vector3(120, 80, 150) * elapsed);
            }
            transform.localScale = Vector3.one * Mathf.Clamp01((.5f - elapsed) / .2f);
            if (elapsed >= .5f) gameObject.SetActive(false);
        }
    }
}
