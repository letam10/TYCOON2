using System;
using System.Collections.Generic;

namespace Tycoon
{
    [Serializable] public sealed class CargoCrateState
    {
        internal CargoCrateState Copy()=>(CargoCrateState)MemberwiseClone();
        public string id,item,holder,source,reservation,task;
    }
    [Serializable] public sealed class RoutePoint { public float x,z; public RoutePoint(){} public RoutePoint(float x,float z){this.x=x;this.z=z;} }
    [Serializable] public sealed class TruckRuntimeState
    {
        internal TruckRuntimeState Copy()=>(TruckRuntimeState)MemberwiseClone();
        public string id="truck:starter",current="storage_processing",source="storage_processing",destination="storage_farm_shop",tripTarget,trip;
        public string phase="Idle";
        public bool repeat;
        public double travelled,distance;
        public int completedTrips;
        public List<RoutePoint> path=new();
    }
    [Serializable] public sealed class ManualTransportJob
    {public string id,source,destination,area,item,kind;public int quantity;}
}
