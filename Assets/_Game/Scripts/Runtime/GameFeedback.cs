using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public sealed class GameFeedback : MonoBehaviour
    {
        AudioSource source;
        AudioClip pickup, drop, sale, work, blocked;
        float nextWork, nextBlocked, nextTransfer;
        readonly List<PurchaseBurst> bursts = new();
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
            var game = GameSession.Instance;
            var player = game ? game.Player : null;
            var view = player ? player.GetComponent<CarryPresentation>() : null;
            if (player && view)
            {
                string itemId = player.Carry.Total > 0 ? player.Carry.Snapshot()[0].id : view.LastItemId;
                Vector3 hand = view.HandPoint;
                Vector3 station = player.ActiveInteraction?.Center ?? point;
                station += Vector3.up * .65f;
                CarryTransferVisual.Play(itemId, taking ? station : hand, taking ? hand : station, transform);
            }
            SparkBurst(point, 5, .065f, taking ? "#9CECB8" : "#A5DEFA", "#FFEDBC");
        }
        public void Collect(Vector3 point)
        {
            PlaySale();
            var player = GameSession.Instance ? GameSession.Instance.Player : null;
            var carry = player ? player.GetComponent<CarryPresentation>() : null;
            if (carry)
                CarryTransferVisual.Play("cash", (player.ActiveInteraction?.Center ?? point) + Vector3.up * .65f,
                    carry.HandPoint, transform);
            SparkBurst(point, 8, .085f, "#FFD875", "#FFF4D6");
        }
        public void CashTransfer(Vector3 from, Vector3 to)
        {
            CarryTransferVisual.Play("cash", from, to, transform);
        }
        public void Burst(Vector3 point)
        {
            SparkBurst(point, 12, .11f, "#FFD875", "#A8E6AF"); PlaySale();
        }
        void SparkBurst(Vector3 point, int count, float size, string first, string second)
        {
            PurchaseBurst available = null;
            foreach (var burst in bursts)
            {
                if (burst.gameObject.activeSelf) continue;
                available = burst;
                break;
            }
            if (!available && bursts.Count < 12)
            {
                var root = new GameObject("FeedbackBurst");
                root.transform.SetParent(transform, false);
                available = root.AddComponent<PurchaseBurst>();
                bursts.Add(available);
            }
            if (!available) available = bursts[0];
            available.Begin(point, count, size, first, second);
        }
        void OnDestroy()
        {
            foreach (var clip in new[] { pickup, drop, sale, work, blocked })
            {
                if (!clip) continue;
                if (Application.isPlaying) Destroy(clip);
                else DestroyImmediate(clip);
            }
        }
    }
}
