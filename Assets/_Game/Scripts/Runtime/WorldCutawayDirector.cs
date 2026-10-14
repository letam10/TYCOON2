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
                foreach (var renderer in landmark.GetComponentsInChildren<Renderer>(true))
                {
                    if (!renderer || !IsUpperShell(renderer)) continue;
                    volume.RegisterRenderer(renderer);
                }
                entries.Add(new Entry
                {
                    Root = landmark.transform,
                    Footprint = landmark.Footprint,
                    Volume = volume
                });
            }
        }

        void Update()
        {
            if (!player || entries.Count == 0) return;
            Vector3 playerPosition = player.transform.position;
            foreach (var entry in entries)
            {
                Vector3 local = entry.Root.InverseTransformPoint(playerPosition);
                bool inside = Mathf.Abs(local.x) <= entry.Footprint.x * .5f
                    && Mathf.Abs(local.z) <= entry.Footprint.z * .5f
                    && local.y >= -.5f && local.y <= 8f;
                entry.Volume.SetPlayerInside(inside);
            }
        }

        static bool IsUpperShell(Renderer renderer)
        {
            string name = renderer.gameObject.name.ToLowerInvariant();
            string material = renderer.sharedMaterial ? renderer.sharedMaterial.name.ToLowerInvariant() : "";
            return name.Contains("roof") || name.Contains("upper") || material.Contains("roof")
                || renderer.bounds.center.y > 3.1f;
        }
    }
}
