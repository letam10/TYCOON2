using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator Stage10RideSave()
        {
            yield return CompletionWait(() => game.NavigationReady && !game.IsRestoring, 30,
                "Stage10 ride save waits for world restore");
            WorldExpansionBoat boat = game.GetComponentsInChildren<WorldExpansionBoat>(true)
                .FirstOrDefault(x => x);
            WorldInteractionArea area = FindCarrierArea(boat);
            Check(boat != null && area != null, "Stage10 save resolves a real boat interaction");
            Check(!string.IsNullOrEmpty(area.StableId), "Stage10 boat interaction has a stable ID");
            game.Player.Controller.enabled = false;
            game.Player.transform.position = boat.DoorOutside;
            game.Player.Controller.enabled = true;
            Check(WorldPlayerRide.TryBoard(game.Player, boat, area.transform, 20f,
                "QA boat boarding", area.StableId, out string failure),
                "Stage10 save boards the real boat: " + failure);
            Time.timeScale = 0;
            game.SaveGame();
            yield return WaitForCheckpoint();
            Check(!game.SaveBlocked && File.Exists(game.SavePath),
                "Stage10 save writes the QA slot");
            SaveData saved = SaveStore.Read(game.SavePath);
            Check(saved.playerRide != null && saved.playerRide.active,
                "Stage10 save contains an active PlayerRideSave");
            Check(saved.playerRide.interactionId == area.StableId,
                "Stage10 save stores the boat stable interaction ID");
            Check(saved.playerRide.remainingSeconds > 0,
                "Stage10 save stores remaining ride time");
            string snapshot = Path.Combine(game.QaDirectory, "stage10-ride-save.json");
            SaveStore.Write(snapshot, saved);
            File.WriteAllText(Path.Combine(game.QaDirectory, "stage10-ride-source.txt"), snapshot);
            File.WriteAllText(Path.Combine(game.QaDirectory, "stage10-ride-state.json"),
                JsonUtility.ToJson(saved.playerRide, true));
            yield return Capture("stage10-ride-save.png");
        }

        IEnumerator Stage10RideLoad()
        {
            yield return CompletionWait(() => game.NavigationReady && !game.IsRestoring, 30,
                "Stage10 ride load waits for world restore");
            string source = GameSession.Argument("--qa-stage10-ride-from", "");
            Check(File.Exists(source), "Stage10 load source exists");
            SaveData expected = SaveStore.Read(source);
            Check(expected.playerRide != null && expected.playerRide.active,
                "Stage10 load source contains an active PlayerRideSave");
            WorldExpansionBoat boat = game.GetComponentsInChildren<WorldExpansionBoat>(true)
                .FirstOrDefault(x => x);
            WorldInteractionArea area = FindCarrierArea(boat);
            WorldPlayerRide ride = game.Player.GetComponent<WorldPlayerRide>();
            Check(boat != null && area != null && ride != null,
                "Stage10 load resolves the saved boat and ride component");
            Check(ride.ActiveRide, "Stage10 load reattaches the player ride");
            Check(ride.InteractionId == expected.playerRide.interactionId &&
                ride.InteractionId == area.StableId,
                "Stage10 load restores the stable interaction ID");
            Check(game.Player.transform.parent == boat.transform,
                "Stage10 load parents the player to the boat hull");
            Check(!game.Player.Controller.enabled && boat.Boardings == 1 && boat.Alightings == 0,
                "Stage10 load imports exactly one boarding without alighting");
            Check(WorldPlayerRide.TryCapture(game.Player, out PlayerRideSave captured) &&
                captured.remainingSeconds > 0,
                "Stage10 load can capture the restored ride again");
            File.WriteAllText(Path.Combine(game.QaDirectory, "stage10-ride-state.json"),
                JsonUtility.ToJson(captured, true));
            yield return Capture("stage10-ride-load.png");
            Time.timeScale = 0;
        }

        WorldInteractionArea FindCarrierArea(WorldExpansionBoat boat)
        {
            if (!boat) return null;
            return game.GetComponentsInChildren<WorldInteractionArea>(true)
                .FirstOrDefault(area => area && area.CarrierSource is WorldExpansionBoat source &&
                    source == boat);
        }

        IEnumerator WaitForCheckpoint()
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 10;
            while (game.Transactions != null && game.Transactions.CheckpointPending)
            {
                if (Time.realtimeSinceStartupAsDouble > deadline)
                    throw new Exception("Stage10 save checkpoint timeout.");
                yield return null;
            }
        }
    }
}
