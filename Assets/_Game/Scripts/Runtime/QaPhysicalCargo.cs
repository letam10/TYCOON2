using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator PhysicalCargoAcceptance()
        {
            var baseline = game.Transactions.Snapshot();
            PhysicalCarryFixture(baseline, "carrot", 6);
            foreach (var worker in game.Workers) worker.enabled = false;
            game.Commerce.enabled = false;
            game.Restaurant.enabled = false;
            var dock = game.Stations.OfType<CargoDock>()
                .First(x => x.WarehouseId == game.Transactions.View.truck.current);
            yield return Travel(Near(dock));
            yield return TownWait(() => game.Player.Carry.Total == 0, 8, "pack six held items into crate");
            yield return Travel(game.Logistics.PlayerCargoPoint(true));
            yield return TownWait(() => game.Transactions.View.crates.Any(x => x.holder == "player"),
                8, "pick up physical crate");
            Check(game.Player.HasCarry, "physical: crate enables carry pose");
            game.Player.CanControl = false;
            yield return new WaitForSeconds(.6f);
            Check(game.Player.View.State == "Carry", "physical: crate reaches the supporting hand pose");
            yield return Capture("06-carry-crate.png");
            if (System.Array.Exists(System.Environment.GetCommandLineArgs(), x => x == "--qa-items-vehicles-v8"))
            {
                Time.timeScale = 0;
                yield return CargoHandAngles();
                Time.timeScale = 1;
            }
            game.Player.CanControl = true;
            yield return Travel(game.Logistics.PlayerCargoPoint(false));
            yield return TownWait(() => !game.Transactions.View.crates.Any(x => x.holder == "player"),
                8, "place physical crate onto truck");
            Check(game.Transactions.View.crates.Any(x => x.holder == game.Transactions.View.truck.id),
                "physical: crate is owned by truck after placement");
            if (System.Array.Exists(System.Environment.GetCommandLineArgs(), x => x == "--qa-items-vehicles-v8"))
            {
                Time.timeScale = 0;
                yield return StandaloneWorldAngles(game.Logistics.Vehicle, "loaded-delivery-truck", "");
                Time.timeScale = 1;
            }
            game.Commerce.enabled = true;
            game.Restaurant.enabled = true;
            game.LoadGame();
            yield return TownWait(() => game.CanSimulate, 8, "restore actors after crate fixture");
            yield return null;
            Check(!game.SaveBlocked, "physical: crate fixture restores checkpoint");
        }

        IEnumerator CargoHandAngles()
        {
            Vector3 position = game.Player.transform.position;
            Quaternion rotation = game.Player.transform.rotation;
            var board = new GameObject("HeldCargoInspection").transform;
            board.position = new Vector3(0, 0, 300);
            Art.Box("PreviewGround", Vector3.down * .09f, new(12, .18f, 12), "#9CA993", board);
            game.Player.transform.position = board.position;
            game.Player.transform.rotation = Quaternion.Euler(0, 200, 0);
            Physics.SyncTransforms();
            yield return null;
            yield return CaptureModelAngles("held-crate", ModelBounds(game.Player.transform).center, 6.5f);
            game.Player.transform.SetPositionAndRotation(position, rotation);
            Object.Destroy(board.gameObject);
            Physics.SyncTransforms();
            yield return null;
        }

        IEnumerator PhysicalUpgradeMotion()
        {
            string directory = Path.Combine(game.QaDirectory, "upgrade-motion");
            Directory.CreateDirectory(directory);
            for (int frame = 0; frame < 24; frame++)
            {
                yield return new WaitForEndOfFrame();
                CapturePhysicalFrame(Path.Combine(directory, frame.ToString("D3") + ".png"));
                yield return new WaitForSecondsRealtime(.1f);
            }
        }
    }
}
