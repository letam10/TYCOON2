using System;
namespace Tycoon
{
    [Serializable] public sealed class RecipeBatchSnapshot
    {
        public string recipe,output,initiator;
        public int version,yield;
        public float seconds;
        public ItemAmount[] inputs;
        public static RecipeBatchSnapshot From(RecipeDefinition d,string actor)=>new(){recipe=d.id,output=d.output,version=d.version,yield=d.yield,seconds=d.seconds,inputs=d.inputs,initiator=actor};
    }
}
