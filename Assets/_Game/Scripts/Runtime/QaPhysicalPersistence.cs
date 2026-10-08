using System.Collections;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator PhysicalSavedAcceptance(bool visuals, bool performanceOnly = false)
        {
            yield return new WaitForSeconds(1);
            var source = SaveStore.Read(GameSession.Argument("--qa-physical-from", ""));
            Check(source?.transactionState != null, "physical relaunch: source checkpoint verified");
            Time.timeScale = 0;
            game.Player.CanControl = false;
            game.Transactions.Detach();
            SaveStore.Write(game.SavePath, source);
            game.LoadGame();
            float deadline = Time.realtimeSinceStartup + 8;
            while (!game.CanSimulate && Time.realtimeSinceStartup < deadline) yield return null;
            Check(game.CanSimulate, "physical relaunch restores all actors");
            yield return null;
            var state = game.Transactions.Snapshot();
            Check(!game.SaveBlocked, "physical relaunch: save is writable");
            Check(state.cashInHand == source.cashInHand && state.cashInSafe == source.cashInSafe,
                "physical relaunch: exact hand and safe balances");
            Check(state.stacks.Sum(x => x.quantity) == source.transactionState.stacks.Sum(x => x.quantity),
                "physical relaunch: exact inventory including workers and customers");
            Check(state.maximumBuilt && game.Player.Carry.Capacity == 48,
                "physical relaunch: maximum and derived capacity persist");
            Check(game.Workers.Count == state.crews.Sum(x => x.count),
                "physical relaunch: every maximized crew has all workers");
            Check(!game.Stations.OfType<PurchasePad>().Any(x => x.Available),
                "physical relaunch: final pads remain noninteractive");
            Check(!game.Stations.OfType<PurchasePad>().Any(x => x.gameObject.activeSelf),
                "physical relaunch: final pads disappear completely");
            yield return Capture("physical-relaunch.png");
            Time.timeScale = 1;
            game.Player.CanControl = true;
            if (visuals)
            {
                yield return PhysicalCargoAcceptance();
                yield return PhysicalDinerAcceptance();
                yield return PhysicalVisualAcceptance();
            }
            if (performanceOnly)
            {
                yield return PhysicalPerformanceAcceptance(state);
                game.LoadGame();
                deadline = Time.realtimeSinceStartup + 8;
                while (!game.CanSimulate && Time.realtimeSinceStartup < deadline) yield return null;
                Check(game.CanSimulate, "physical performance: checkpoint restored after measurement");
            }
            Check(game.RuntimeErrors.Count == 0, "physical relaunch: no runtime errors");
        }
    }
}
