using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    static class Stage11To13Fixtures
    {
        internal static T Read<T>(string file)=>JsonUtility.FromJson<T>(File.ReadAllText(Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,"mod","test",file)));

        internal static void Producer(TransactionState state,string item,string requirement)
        {
            state.stations.Add(new StationRuntimeState{id="producer_"+item,kind="producer",item=item,requirement=requirement});
        }

        internal static void Machine(TransactionState state,string recipe,string requirement,int batches=0,string area="processing",int playerJobs=0)
        {
            state.stations.Add(new StationRuntimeState{id="machine_"+recipe,kind="machine",definitionId=recipe,requirement=requirement,
                area=area,batches=System.Math.Max(batches,playerJobs),playerBatches=playerJobs,playerWorkCount=playerJobs});
        }
    }

}
