using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator AssetLibraryVisual()
        {
            yield return new WaitForSeconds(1);
            string fixturePath = GameSession.Argument("--qa-preview-fixture", "");
            if (!string.IsNullOrEmpty(fixturePath))
            {
                Check(game.IsQa && Path.GetFullPath(game.SavePath).StartsWith(Path.GetFullPath(game.QaDirectory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "asset fixture writes only its isolated QA save");
                var fixture = SaveStore.Read(fixturePath);
                Check(fixture != null, "asset fixture exists and passes Save v2 verification");
                game.Transactions.Detach(); SaveStore.Write(game.SavePath, fixture); game.LoadGame();
                Check(!game.SaveBlocked && game.Economy.Has("restaurant"), "old Save v2 restores all business areas with new visuals");
                File.WriteAllText(Path.Combine(game.QaDirectory, "fixture-note.txt"), "Chỉ dùng preset QA trước đó để xem toàn bộ asset/khu; không chứng minh progression hoặc balance. Nguồn: " + fixturePath);
                yield return null;
            }
            else
            {
                string preset = GameSession.Argument("--qa-fixture", Path.Combine(Directory.GetParent(Application.dataPath).FullName, "mod/test/asset-preview.json"));
                SeedMilestone(JsonUtility.FromJson<MilestoneCase>(File.ReadAllText(preset)));
                File.WriteAllText(Path.Combine(game.QaDirectory,"fixture-note.txt"),"Preset mod/test chỉ phục vụ xem toàn bộ asset/khu; không chứng minh progression/balance.");
                yield return null;
            }
            Check(game.Catalog.libraryKeys.Length == 48, "48 selected local models registered");
            foreach (string key in game.Catalog.libraryKeys)
            {
                var prefab = game.Catalog.Model(key);
                Check(prefab && prefab.GetComponentsInChildren<Renderer>(true).Length > 0, "asset renderer " + key);
                Check(prefab.GetComponentsInChildren<Collider>(true).Length == 0, "asset cannot change navigation footprint " + key);
                foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                    foreach (var material in renderer.sharedMaterials)
                    {
                        Check(material && material.shader && material.shader.name == "Universal Render Pipeline/Lit", "URP material " + key);
                        report.materialSlotsChecked++;
                    }
            }
            var camera = Camera.main;
            var canvases = game.Hud.GetComponentsInChildren<Canvas>().ToArray();
            game.Player.StopInteraction(); game.Player.CanControl = false;
            Time.timeScale = 0; game.CameraRig.enabled = false;
            int wallet = game.Economy.Money, items = game.Transactions.View.stacks.Sum(s => s.quantity);
            foreach (var canvas in canvases) canvas.gameObject.SetActive(false);
            string[] food = game.Catalog.libraryKeys.Where(k => Definitions.Item(k) != null).ToArray();
            string[] landscape = game.Catalog.libraryKeys.Where(k => k.StartsWith("tree_") || k.StartsWith("garden_") || k.StartsWith("crop_") || k is "wheat_crop" or "farm_fence" or "farm_shelter").ToArray();
            string[] furniture = game.Catalog.libraryKeys.Except(food).Except(landscape).ToArray();
            yield return AssetLineup("01-food-lineup.png", food);
            yield return AssetLineup("02-furniture-lineup.png", furniture);
            yield return AssetLineup("03-farm-landscape-lineup.png", landscape);
            camera.orthographic = false;
            yield return Area("04-farm.png", new Vector3(-13, 0, 8), 26);
            yield return Area("05-livestock.png", new Vector3(-32, 0, 22), 29);
            yield return Area("06-farm-shop.png", new Vector3(7, 0, 7), 28);
            yield return Area("07-processing.png", new Vector3(28, 0, 9), 26);
            yield return Area("08-supermarket.png", new Vector3(28, 0, 33), 29);
            yield return Area("09-bakery.png", new Vector3(4, 0, 38), 27);
            yield return Area("10-restaurant.png", new Vector3(-20, 0, 39), 28);
            foreach (var canvas in canvases) canvas.gameObject.SetActive(true);
            game.CameraRig.enabled = true; game.CameraRig.Overview = true; game.CameraRig.Snap();
            yield return Capture("11-world-overview.png");
            game.CameraRig.Overview = false; game.CameraRig.Snap();
            Check(game.Economy.Money == wallet && game.Transactions.View.stacks.Sum(s => s.quantity) == items, "visual tour does not create or consume money/items");
            Time.timeScale = 1;

            IEnumerator Area(string name, Vector3 focus, float distance)
            {
                var rotation = Quaternion.Euler(55, 40, 0);
                camera.transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
                yield return null; yield return Capture(name);
            }
        }

        IEnumerator AssetLineup(string name, string[] keys)
        {
            var root = new GameObject("QA_AssetLineup").transform;
            root.position = new Vector3(140, 0, 0);
            int columns = 5, rows = (keys.Length + columns - 1) / columns;
            Art.Box("QA_LineupFloor", new Vector3(12, -.06f, (rows - 1) * 3), new Vector3(34, .1f, rows * 6 + 8), "#E8E5DB", root);
            for (int i = 0; i < keys.Length; i++)
            {
                var size = Art.ModelSize(keys[i]);
                float scale = 2.8f / Mathf.Max(size.x, size.y, size.z);
                Art.Model(keys[i], new Vector3(i % columns * 6, 0, i / columns * 6), root, scale);
                Art.Label(keys[i], new Vector3(i % columns * 6, .07f, i / columns * 6 - 2), root, .21f);
            }
            var camera = Camera.main;
            camera.orthographic = true; camera.orthographicSize = Mathf.Max(10, rows * 2.5f + 2);
            var rotation = Quaternion.Euler(50, 0, 0);
            var center = root.position + new Vector3(12, 0, (rows - 1) * 3);
            camera.transform.SetPositionAndRotation(center - rotation * Vector3.forward * 45, rotation);
            yield return null; yield return Capture(name);
            Destroy(root.gameObject); yield return null;
        }
    }
}
