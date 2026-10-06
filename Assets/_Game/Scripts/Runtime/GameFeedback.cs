using UnityEngine;

namespace Tycoon
{
    public sealed class GameFeedback : MonoBehaviour
    {
        AudioSource source;
        AudioClip pickup, drop, sale, work, blocked;
        float nextWork, nextBlocked, nextTransfer;
        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.volume = .17f; source.spatialBlend = 0;
            pickup = Tone("Pickup", 680, .09f); drop = Tone("Drop", 420, .07f);
            sale = Tone("Sale", 1050, .22f); work = Tone("Work", 250, .055f); blocked = Tone("Waiting", 180, .07f);
        }
        static AudioClip Tone(string name, float frequency, float seconds)
        {
            int count = (int)(44100 * seconds); var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float envelope = Mathf.Min(1, i / 160f) * Mathf.Pow(1 - t, 2);
                samples[i] = (Mathf.Sin(2 * Mathf.PI * frequency * i / 44100) + .2f * Mathf.Sin(2 * Mathf.PI * frequency * 2 * i / 44100)) * envelope * .4f;
            }
            var clip = AudioClip.Create(name, count, 1, 44100, false); clip.SetData(samples, 0); return clip;
        }
        public void PlayPickup() { if (source) source.PlayOneShot(pickup); }
        public void PlaySale() { if (source) source.PlayOneShot(sale); }
        public void PlayWork() { if (source && Time.unscaledTime >= nextWork) { nextWork = Time.unscaledTime + .38f; source.PlayOneShot(work, .35f); } }
        public void PlayBlocked() { if (source && Time.unscaledTime >= nextBlocked) { nextBlocked = Time.unscaledTime + 1.1f; source.PlayOneShot(blocked, .25f); } }
        public void ItemTransfer(Vector3 point, bool taking)
        {
            if (Time.unscaledTime < nextTransfer) return;
            nextTransfer = Time.unscaledTime + .12f;
            if (source) source.PlayOneShot(taking ? pickup : drop);
            SparkBurst(point, 5, .065f, taking ? "#9CECB8" : "#A5DEFA", "#FFEDBC");
        }
        public void Collect(Vector3 point) { PlaySale(); SparkBurst(point, 8, .085f, "#FFD875", "#FFF4D6"); }
        public void Burst(Vector3 point)
        {
            SparkBurst(point, 12, .11f, "#FFD875", "#A8E6AF"); PlaySale();
        }
        void SparkBurst(Vector3 point, int count, float size, string first, string second)
        {
            var root = new GameObject("FeedbackBurst"); root.transform.SetParent(transform, false); root.transform.position = point;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2 / count;
                var spark = Art.Box("Spark", new Vector3(Mathf.Cos(angle), .15f, Mathf.Sin(angle)) * .2f, Vector3.one * size, i % 2 == 0 ? first : second, root.transform);
                var renderer = spark.GetComponent<Renderer>(); renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            }
            root.AddComponent<PurchaseBurst>();
        }
        void OnDestroy() { Destroy(pickup); Destroy(drop); Destroy(sale); Destroy(work); Destroy(blocked); }
    }
    public sealed class PurchaseBurst : MonoBehaviour
    {
        float elapsed;
        Transform[] sparks;
        Vector3[] velocities;
        void Awake()
        {
            sparks = new Transform[transform.childCount]; velocities = new Vector3[sparks.Length];
            for (int i = 0; i < sparks.Length; i++)
            {
                sparks[i] = transform.GetChild(i);
                Vector3 outward = sparks[i].localPosition; outward.y = 0;
                velocities[i] = outward.normalized * (1 + i % 3 * .25f) + Vector3.up * (1.6f + i % 2 * .4f);
            }
        }
        void Update()
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < sparks.Length; i++)
            {
                velocities[i] += Vector3.down * (4 * Time.deltaTime);
                sparks[i].localPosition += velocities[i] * Time.deltaTime;
                sparks[i].Rotate(new Vector3(120, 80, 150) * Time.deltaTime);
            }
            transform.localScale = Vector3.one * Mathf.Clamp01((.75f - elapsed) / .3f);
            if (elapsed >= .75f) Destroy(gameObject);
        }
    }
}
