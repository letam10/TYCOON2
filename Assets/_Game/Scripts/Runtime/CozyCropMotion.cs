using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    // Một component cho cả ruộng; chỉ nghiêng cây, scale vẫn do ProductionStation quản lý.
    public sealed class CozyCropMotion : MonoBehaviour
    {
        readonly struct Crop
        {
            public readonly ProductionStation Station;
            public readonly Transform Plant;
            public readonly Quaternion Rotation;
            public readonly float Offset;
            public Crop(ProductionStation station, Transform plant, float offset)
            { Station = station; Plant = plant; Rotation = plant.localRotation; Offset = offset; }
        }
        readonly List<Crop> crops = new();
        void Start()
        {
            foreach (var producer in GameSession.Instance.Producers)
                if (!producer.Animal && producer.Plants != null)
                    for (int i = 0; i < producer.Plants.Length; i++)
                        if (producer.Plants[i]) crops.Add(new(producer, producer.Plants[i], i * 1.7f + producer.transform.position.x * .19f));
        }
        void LateUpdate()
        {
            var game = GameSession.Instance; if (!game || !game.CanSimulate) return;
            float blend = 1 - Mathf.Exp(-8 * Time.deltaTime);
            foreach (var crop in crops)
            {
                if (!crop.Plant || !crop.Station) continue;
                bool grown = crop.Station.IsUnlocked && crop.Station.Phase >= 2;
                float sway = grown ? Mathf.Sin(Time.time * 1.2f + crop.Offset) * 1.35f : 0;
                crop.Plant.localRotation = Quaternion.Slerp(crop.Plant.localRotation,
                    crop.Rotation * Quaternion.Euler(sway * .4f, 0, sway), blend);
            }
        }
        void OnDisable()
        { foreach (var crop in crops) if (crop.Plant) crop.Plant.localRotation = crop.Rotation; }
    }
}
