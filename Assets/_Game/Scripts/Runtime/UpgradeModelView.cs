using UnityEngine;

namespace Tycoon
{
    public sealed class UpgradeModelView : MonoBehaviour
    {
        GameSession game;
        Station station;
        WorkerAgent worker;
        Transform parts;
        string signature;
        string renderedSignature;
        bool queued;
        bool pendingAnimation;
        int qualityLevel;
        float nextCheck;
        public int Tier { get; private set; }
        public int CapacityLevel { get; private set; }
        public int SpeedLevel { get; private set; }

        public static void Initialize(GameSession game) => RefreshAll(game, false);
        public static void RefreshAll(GameSession game, bool animate)
        {
            if (!game) return;
            foreach (var station in game.Stations)
            {
                if (!station || station is StationZone or PurchasePad or ConveyorStation) continue;
                if (station is not (ProductionStation or MachineStation or ShelfStation or
                    StorageStation or CheckoutStation))
                    continue;
                var view = station.GetComponent<UpgradeModelView>();
                if (!view) view = station.gameObject.AddComponent<UpgradeModelView>();
                view.game = game;
                view.station = station;
                view.Refresh(animate);
            }
            foreach (var worker in game.Workers)
            {
                AttachWorker(game, worker, animate);
            }
        }

        public static void AttachWorker(GameSession game, WorkerAgent worker, bool animate)
        {
            if (!game || !worker) return;
            var view = worker.GetComponent<UpgradeModelView>();
            if (!view) view = worker.gameObject.AddComponent<UpgradeModelView>();
            view.game = game;
            view.worker = worker;
            view.Refresh(animate);
        }

        void LateUpdate()
        {
            if (!game) return;
            if (Time.unscaledTime >= nextCheck)
            {
                nextCheck = Time.unscaledTime + .5f;
                Refresh(true);
            }
            if (!parts) return;
            if (station) parts.gameObject.SetActive(station.IsUnlocked);
        }

        void Refresh(bool animate)
        {
            int quality = 1;
            if (worker)
            {
                var crew = game.ReadCrew(worker.UpgradeId);
                if (crew == null) return;
                Tier = crew.count;
                CapacityLevel = crew.carryLevel;
                SpeedLevel = crew.speedLevel;
            }
            else if (station)
            {
                string family = Family(station);
                Tier = game.Progression.StationLevel(family);
                CapacityLevel = game.Progression.AxisLevel(family, UpgradeAxis.Capacity);
                SpeedLevel = game.Progression.AxisLevel(family, UpgradeAxis.Speed);
                quality = game.Progression.AxisLevel(family, UpgradeAxis.QualityValue);
            }
            else return;
            string current = Tier + ":" + CapacityLevel + ":" + SpeedLevel + ":" + quality;
            if (signature == current) return;
            signature = current;
            qualityLevel = quality;
            pendingAnimation |= animate;
            if (Application.isPlaying && animate)
            {
                if (queued) return;
                queued = true;
                UpgradeVisualQueue.Enqueue(game, this);
            }
            else BuildPendingVisual();
        }

        internal void BuildPendingVisual()
        {
            queued = false;
            if (!game || (!worker && !station)) return;
            if (renderedSignature == signature) return;
            renderedSignature = signature;
            if (parts)
            {
                parts.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(parts.gameObject);
                else DestroyImmediate(parts.gameObject);
            }
            parts = new GameObject("PersistentUpgradeDetails").transform;
            parts.SetParent(transform, false);
            if (worker) UpgradeModelParts.Crew(parts, Tier, CapacityLevel, SpeedLevel);
            else UpgradeModelParts.Build(parts, station, Tier, CapacityLevel, SpeedLevel, qualityLevel);
            if (station) parts.gameObject.SetActive(station.IsUnlocked);
            parts.gameObject.AddComponent<UpgradeVisualMotion>().Initialize(game, pendingAnimation, SpeedLevel);
            pendingAnimation = false;
        }

        public static string Family(Station station) => station switch
        {
            StorageStation => "storage_" + station.AreaId,
            ProductionStation producer => producer.Animal ? "animal" : "farm",
            MachineStation machine => machine.AreaId == "bakery" ? "oven" :
                machine.AreaId == "restaurant" ? "kitchen" : "mill",
            ShelfStation or CheckoutStation => station.AreaId == "supermarket" ? "market" : "counter",
            _ => ""
        };
    }
}
