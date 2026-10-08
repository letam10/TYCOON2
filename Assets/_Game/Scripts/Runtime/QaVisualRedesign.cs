using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        [Serializable]
        sealed class VisualRedesignReport
        {
            public string gpu;
            public int detailedSurfaces;
            public int architectureParts;
            public int treeMeshes;
            public int illustratedPads;
            public int iconResolution;
            public List<string> invalidMaterials = new();
            public List<string> roadOverlaps = new();
        }

        void CheckVisualRedesign()
        {
            var report = new VisualRedesignReport();
            report.gpu = SystemInfo.graphicsDeviceName;
            report.iconResolution = DrawnPurchaseIcons.Size;
            var world = game.transform.Find("World");
            foreach (var renderer in world.GetComponentsInChildren<MeshRenderer>(true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (!material || !material.shader || !material.shader.isSupported
                        || material.shader.name.Contains("Error")) report.invalidMaterials.Add(renderer.name);
                    if (material && material.shader.name == "TYCOON/Town Surface") report.detailedSurfaces++;
                }
                if (renderer.name == "TreeCrown") report.treeMeshes++;
                if (renderer.name is "WindowGlass" or "RearRoof" or "FacadeColumn") report.architectureParts++;
            }
            foreach (var pad in game.Stations.OfType<PurchasePad>())
            {
                report.illustratedPads++;
                var icon = pad.GetComponentInChildren<SpriteRenderer>(true);
                Check(icon && icon.sprite.texture.width == 256, "256px illustrated object icon " + pad.Id);
                var bounds = new Bounds(pad.transform.position, new(2.18f, 2, 2.18f));
                if (TownLayout.RoadBounds().Any(b => b.Intersects(bounds))) report.roadOverlaps.Add(pad.Id);
            }
            File.WriteAllText(Path.Combine(game.QaDirectory, "visual-redesign.json"),
                JsonUtility.ToJson(report, true));
            Check(report.invalidMaterials.Count == 0, "all world shaders supported on actual GPU");
            Check(report.detailedSurfaces > 200 && report.architectureParts >= 30,
                "detailed physical surfaces and cutaway architecture instantiated");
            Check(report.treeMeshes > 40, "branched landscape trees instantiated");
            Check(report.roadOverlaps.Count == 0, "upgrade banks keep truck roads clear");
            Check(world.GetComponentsInChildren<MeshFilter>(true)
                .Any(m => m.sharedMesh && m.sharedMesh.name == "RollingCountryside"),
                "sculpted countryside replaces sphere hills");
        }

        IEnumerator VisualRedesignScreens()
        {
            var camera = game.CameraRig;
            var pads = game.Stations.OfType<PurchasePad>().ToArray();
            foreach (string id in new[] { "milk_line", "egg_line", "truck_bundle", "cook", "waiter", "conveyor_processing" })
            {
                var pad = pads.First(p => p.Upgrade.id == id);
                var icon = pad.GetComponentInChildren<SpriteRenderer>(true);
                bool enabled = icon.enabled;
                icon.enabled = true;
                camera.FocusOffset = pad.transform.position - game.Player.transform.position;
                camera.Distance = 5;
                camera.Snap();
                yield return Capture("illustration-" + id + ".png");
                icon.enabled = enabled;
            }
            camera.FocusOffset = new Vector3(4, 0, 66) - game.Player.transform.position;
            camera.Distance = 22;
            camera.Snap();
            yield return Capture("river-landscape.png");
            float previousSpeed = Time.timeScale;
            Time.timeScale = 1;
            yield return new WaitForSeconds(.75f);
            yield return Capture("river-motion.png");
            Time.timeScale = previousSpeed;
        }
    }
}
