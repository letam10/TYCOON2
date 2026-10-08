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
            yield return Capture("06-carry-crate.png");
            yield return Travel(game.Logistics.PlayerCargoPoint(false));
            yield return TownWait(() => !game.Transactions.View.crates.Any(x => x.holder == "player"),
                8, "place physical crate onto truck");
            Check(game.Transactions.View.crates.Any(x => x.holder == game.Transactions.View.truck.id),
                "physical: crate is owned by truck after placement");
            game.Commerce.enabled = true;
            game.Restaurant.enabled = true;
            game.LoadGame();
            yield return TownWait(() => game.CanSimulate, 8, "restore actors after crate fixture");
            yield return null;
            Check(!game.SaveBlocked, "physical: crate fixture restores checkpoint");
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
