using System;
using System.Linq;

namespace Tycoon
{
    public sealed partial class TransactionCore
    {
        static RecipeDefinition MachineRecipe(TransactionState s,StationRuntimeState m)
        {
            var current=Definitions.Recipe(m.definitionId);
            if(m.legacyInput && m.definitionId is "oven" or "cakeoven")
            {
                var old=Definitions.LegacyRecipe(m.definitionId);
                if(old.inputs.All(i=>s.stacks.Where(x=>x.owner==m.input&&x.item==i.id).Sum(x=>x.quantity)>=i.count))return old;
            }
            return current;
        }
    }
    public sealed partial class RuntimeTransactions
    {
        public bool SelectRecipe(MachineStation machine,string recipe,string actor="player")
        {
            var command=Command(TransactionKind.SelectRecipe,actor,machine.Id);command.secondary=recipe;
            return TryExecute(command,out _);
        }
        public bool TickMachine(MachineStation machine,float delta)
        {
            var state=Station(machine.Id);if(!Ready||state==null||machine.Broken||!machine.IsUnlocked)return false;
            if(!state.running)
            {
                if(!state.manualRecipe && machine.Options.Length>1)
                {
                    string wanted=game.Tables.Where(t=>t.NeedsMeal).Select(t=>t.Occupant.WantedItem).FirstOrDefault();
                    var recipe=Definitions.Recipes.FirstOrDefault(r=>r.output==wanted&&machine.Options.Contains(r.id));
                    if(recipe!=null&&recipe.id!=state.definitionId){SelectRecipe(machine,recipe.id,"simulation");state=Station(machine.Id);}
                }
                bool input=machine.Recipe.CanMake(machine.Input)||state.legacyInput&&Definitions.LegacyRecipe(state.definitionId).CanMake(machine.Input);
                if(!input||machine.Inventory.FreeFor(machine.Recipe.output)<machine.Recipe.yield)return false;
                var start=Command(TransactionKind.StartMachine,"simulation",machine.Id);start.secondary="job:"+Guid.NewGuid().ToString("N");
                if(!TryExecute(start,out _))return false;state=Station(machine.Id);
            }
            string family=ProgressionTracker.Family(state);
            var advance=Command(TransactionKind.AdvanceMachine,"simulation",machine.Id);advance.secondary=state.jobId;
            advance.duration=delta*(1+.15f*(game.Progression.AxisLevel(family,UpgradeAxis.Speed)-1));
            if(!TryExecute(advance,out _))return false;state=Station(machine.Id);
            if(state.remaining==0)
            {
                var finish=Command(TransactionKind.CompleteMachine,"simulation",machine.Id,effect:"finish:"+machine.Id+":"+state.jobId);finish.secondary=state.jobId;
                return TryExecute(finish,out _);
            }
            return true;
        }
    }
}
