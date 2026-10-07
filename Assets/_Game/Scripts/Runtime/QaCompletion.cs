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
        readonly Dictionary<string, string> completionRequirements = new();
        IEnumerator CompletionAudit()
        {
            yield return CompletionWait(() => game.NavigationReady && !game.IsRestoring, 30,
                "World restores before completion scenarios");
            string group = GameSession.Argument("--qa-completion-case", "All");
            if (group is "All" or "Progression") yield return CompletionProgression();
            if (group is "All" or "Crews") yield return CompletionCrews();
            if (group is "All" or "Roles") yield return CompletionRoleFlows();
            if (group is "All" or "Cargo") yield return CompletionCargo();
            if (group is "All" or "Events") yield return CompletionEvents();
            if (group is "All" or "Save") yield return CompletionSave();
            Check(game.RuntimeErrors.Count == 0, "Completion scenarios have no runtime errors");
            game.SaveGame();
            yield return Capture("completion.png");
        }

        IEnumerator CompletionReset(string[] crews, params string[] machines)
        {
            Time.timeScale = 0;
            game.Player.StopInteraction();
            foreach (var station in game.Stations)
            {
                if (!completionRequirements.ContainsKey(station.Id))
                    completionRequirements.Add(station.Id, station.Requirement);
                station.Requirement = completionRequirements[station.Id];
            }
            var fixture = JsonUtility.FromJson<TownFixture>(File.ReadAllText(
                GameSession.Argument("--qa-fixture", "mod/test/completion-regression.json")));
            SeedTown(fixture);
            var data = game.CaptureSaveData();
            var state = game.Transactions.Snapshot();
            state.crews.Clear();
            foreach (string id in crews)
            {
                var definition = Definitions.Upgrade(id);
                Check(definition != null && definition.kind == "worker", "Fixture crew exists: " + id);
                if (!state.unlocked.Contains(id)) state.unlocked.Add(id);
                state.crews.Add(GameSession.CrewFor(definition));
            }
            foreach (var station in state.stations.Where(s => s.kind == "machine"))
            {
                station.definitionId = station.id.Substring("machine_".Length);
                station.batch = null;
                station.manualRecipe = false;
                station.lastInputActor = null;
                if (!machines.Contains(station.id)) station.requirement = "qa:inactive";
                game.Machines.First(m => m.Id == station.id).Requirement = station.requirement;
            }
            data.workers.Clear();
            data.events = new EventState();
            data.playerX = -15.5f;
            data.playerZ = -3.5f;
            CompletionWriteFixture(state, data);
            // Director chỉ khôi phục actor; Milestone 0 chặn sinh khách ngoài ca kiểm thử.
            game.Milestone = 0;
            game.Commerce.enabled = true;
            game.Restaurant.enabled = true;
            foreach (var producer in game.Producers) producer.enabled = false;
            foreach (var machine in game.Machines) machine.enabled = false;
            yield return RecoveryReady();
            game.Commerce.enabled = false;
            game.Restaurant.enabled = false;
            game.Player.CanControl = false;
            foreach (var machine in game.Machines) machine.enabled = machines.Contains(machine.Id);
            Time.timeScale = fixture.timeScale;
        }

        void CompletionWriteFixture(TransactionState state, SaveData data)
        {
            string prefix = Path.GetFullPath(game.QaDirectory) + Path.DirectorySeparatorChar;
            if (!game.IsQa || !Path.GetFullPath(game.SavePath).StartsWith(prefix,
                StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Completion fixture phải nằm trong save QA riêng.");
            TransactionCore.NormalizeState(state);
            TransactionCore.Validate(state);
            RuntimeTransactions.Project(state, data);
            game.Transactions.Detach();
            File.Delete(game.SavePath + ".journal");
            SaveStore.Write(game.SavePath, data);
            game.LoadGame();
            Check(!game.SaveBlocked, "Completion fixture restores without blocking");
        }

        IEnumerator CompletionWait(Func<bool> predicate, float seconds, string label)
        {
            double limit = Time.realtimeSinceStartupAsDouble + Math.Max(20, seconds);
            float end = Time.time + seconds;
            while (!predicate())
            {
                if (game.SaveBlocked || Time.time > end || Time.realtimeSinceStartupAsDouble > limit)
                    throw new Exception(label + " | " + string.Join(" | ", game.Workers.Select(w => w.Diagnostic())));
                yield return null;
            }
            Check(true, label);
        }

        IEnumerator CompletionEvents()
        {
            yield return CompletionReset(new[] { "repair_processing" }, "machine_mill");
            var machine = game.Machines.First(m => m.Id == "machine_mill");
            var warehouse = game.StorageFor("processing");
            int amount = game.Transactions.Transfer(warehouse.Inventory, machine.Input, "wheat", 4);
            Check(amount == 4, "Breakdown test loads a real batch once");
            yield return CompletionWait(() => machine.Running, 5, "Machine starts before breakdown");
            machine.BreakDown(40);
            Check(machine.Broken, "One machine breaks while running");
            float remaining = machine.Remaining;
            int wallet = game.Economy.Money;
            yield return CompletionWait(() => machine.RepairPaid, 35, "Specialist reaches machine and pays repair once");
            Check(Math.Abs(machine.Remaining - remaining) < .01f, "Broken batch preserves production progress");
            float began = Time.time;
            yield return CompletionWait(() => !machine.Broken, 35, "Repairer finishes the 30-second repair");
            Check(Time.time - began > 27, "Base Repairer does not finish at player speed");
            Check(game.Economy.Money == wallet - 40, "Repair incident charges exactly one fee");
            yield return CompletionWait(() => machine.Batches == 1, 12, "Production resumes after specialist repair");
            Check(machine.Inventory.Count("flour") == machine.Recipe.yield,
                "Repaired batch produces one real output batch");
            machine.enabled = false;
            game.Events.untilRush = .2f;
            game.Commerce.enabled = true;
            game.Milestone = 1;
            yield return CompletionWait(() => game.Events.warning > 0, 2, "Rush enters warning after crew unlock");
            float warningStart = Time.time;
            yield return CompletionWait(() => game.RushActive, 17, "Rush warning reaches active phase");
            Check(Time.time - warningStart >= 14.5f, "Rush warning lasts 15 simulation seconds");
            float rushStart = Time.time;
            int peak = 0;
            while (game.RushActive)
            {
                peak = Math.Max(peak, game.Commerce.ActiveCount);
                Check(game.Commerce.ActiveCount <= 30, "Rush shared active customer cap");
                yield return new WaitForSeconds(1);
            }
            Check(Time.time - rushStart >= 89, "Rush lasts 90 simulation seconds");
            Check(peak >= 24 && peak <= 30, "Rush arrival rate fills the shared customer cap");
            Check(game.Events.warning == 0 && game.Events.untilRush > 470,
                "Rush returns to normal cooldown without immediate repeat");
            game.Milestone = 0;
            game.Commerce.enabled = false;
        }
    }
}
