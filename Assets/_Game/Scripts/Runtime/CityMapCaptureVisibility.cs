using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    internal sealed class CityMapCaptureVisibility : IDisposable
    {
        readonly Dictionary<Renderer, bool> hidden = new();
        readonly Dictionary<Renderer, bool> shells = new();
        readonly Dictionary<Canvas, bool> canvases = new();
        readonly bool fog;

        internal CityMapCaptureVisibility()
        {
            fog = RenderSettings.fog;
            RenderSettings.fog = false;
            HideActors<CharacterController>();
            HideActors<NavMeshAgent>();
            HideActors<CityVehicleRig>();
            HideActors<CityRailTrain>();
            HideActors<WorldExpansionBoat>();
            HideActors<TextMesh>();
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>())
            {
                canvases[canvas] = canvas.enabled;
                canvas.enabled = false;
            }
            foreach (WorldCutawayVolume volume in UnityEngine.Object.FindObjectsByType<WorldCutawayVolume>())
            {
                if (!volume.IsRevealed)
                    continue;
                foreach (Renderer renderer in volume.upperShellRenderers)
                {
                    if (!renderer || shells.ContainsKey(renderer))
                        continue;
                    shells.Add(renderer, renderer.enabled);
                    renderer.enabled = true;
                }
            }
        }

        void HideActors<T>() where T : Component
        {
            foreach (T actor in UnityEngine.Object.FindObjectsByType<T>())
            {
                foreach (Renderer renderer in actor.GetComponentsInChildren<Renderer>(true))
                {
                    if (!renderer || hidden.ContainsKey(renderer))
                        continue;
                    hidden.Add(renderer, renderer.forceRenderingOff);
                    renderer.forceRenderingOff = true;
                }
            }
        }

        public void Dispose()
        {
            // Chỉ ẩn trong lệnh render đồng bộ; gameplay/collider không bị tắt giữa các frame.
            foreach (var entry in hidden)
                if (entry.Key)
                    entry.Key.forceRenderingOff = entry.Value;
            foreach (var entry in shells)
                if (entry.Key)
                    entry.Key.enabled = entry.Value;
            foreach (var entry in canvases)
                if (entry.Key)
                    entry.Key.enabled = entry.Value;
            RenderSettings.fog = fog;
        }
    }
}
