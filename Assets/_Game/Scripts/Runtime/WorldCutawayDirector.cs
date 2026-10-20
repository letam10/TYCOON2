using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public sealed class WorldCutawayDirector : MonoBehaviour
    {
        sealed class Entry
        {
            public Transform Root;
            public Vector3 Footprint;
            public WorldCutawayVolume Volume;
        }

        readonly List<Entry> entries = new();
        PlayerController player;

        public int RegisteredVolumeCount => entries.Count;

        public static WorldCutawayDirector Build(GameSession game, Transform world)
        {
            if (!game || !world) return null;
            var existing = world.GetComponentInChildren<WorldCutawayDirector>();
            if (existing) return existing;
            var root = new GameObject("WorldCutawayDirector").transform;
            root.SetParent(world, false);
            var director = root.gameObject.AddComponent<WorldCutawayDirector>();
            director.player = game.Player;
            director.Configure(world);
            return director;
        }

        void Configure(Transform world)
        {
            entries.Clear();

            foreach (var landmark in world.GetComponentsInChildren<CityLandmark>(true))
            {
                if (!landmark || landmark.Footprint.x <= 0 || landmark.Footprint.z <= 0)
                    continue;
                var volume = landmark.GetComponent<WorldCutawayVolume>();
                if (!volume) volume = landmark.gameObject.AddComponent<WorldCutawayVolume>();
                volume.useTriggerCallbacks = false;
                volume.playerTag = string.Empty;
                var trigger = landmark.GetComponent<BoxCollider>();
                if (!trigger) trigger = landmark.gameObject.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.center = Vector3.up * 4f;
                trigger.size = new Vector3(landmark.Footprint.x, 8f, landmark.Footprint.z);
                volume.localFootprint = landmark.Footprint;
                volume.localMinHeight = -.5f;
                volume.localMaxHeight = 8f;
                foreach (var renderer in landmark.GetComponentsInChildren<Renderer>(true))
                {
                    if (!renderer)
                        continue;

                    if (IsUpperShell(renderer))
                        volume.RegisterRenderer(renderer);
                    else if (IsInteriorCandidate(renderer))
                        volume.RegisterInteriorRenderer(renderer);
                }
                AddEntry(landmark.transform, landmark.Footprint, volume);
            }

            foreach (var volume in world.GetComponentsInChildren<WorldCutawayVolume>(true))
            {
                if (!volume || volume.GetComponent<CityLandmark>() || volume.localFootprint.x <= 0
                    || volume.localFootprint.z <= 0)
                    continue;
                AddEntry(volume.transform, volume.localFootprint, volume);
            }
        }

        void AddEntry(Transform root, Vector3 footprint, WorldCutawayVolume volume)
        {
            if (!root || !volume || footprint.x <= 0f || footprint.z <= 0f)
                return;

            foreach (Entry entry in entries)
            {
                if (entry.Volume == volume)
                    return;
            }

            volume.localFootprint = footprint;
            entries.Add(new Entry
            {
                Root = root,
                Footprint = footprint,
                Volume = volume
            });
        }

        void Update()
        {
            if (!player || entries.Count == 0) return;
            Vector3 playerPosition = player.transform.position;
            foreach (var entry in entries)
            {
                if (entry.Volume != null)
                    entry.Volume.SetPlayerInside(entry.Volume.ContainsWorldPoint(playerPosition));
            }
        }

        public bool TryGetVolumeAt(Vector3 worldPosition, out WorldCutawayVolume volume)
        {
            foreach (Entry entry in entries)
            {
                if (entry.Volume != null && entry.Volume.ContainsWorldPoint(worldPosition))
                {
                    volume = entry.Volume;
                    return true;
                }
            }

            volume = null;
            return false;
        }

        static bool IsUpperShell(Renderer renderer)
        {
            string name = renderer.gameObject.name.ToLowerInvariant();
            string material = renderer.sharedMaterial ? renderer.sharedMaterial.name.ToLowerInvariant() : "";
            return name.Contains("roof") || name.Contains("upper") || material.Contains("roof")
                || renderer.bounds.center.y > 3.1f;
        }

        static bool IsInteriorCandidate(Renderer renderer)
        {
            return renderer.bounds.center.y <= 3.1f;
        }
    }
}
