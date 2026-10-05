using System.Collections.Generic;
using UnityEngine;
namespace Tycoon
{
    public static class TruckRoutes
    {
        public static Vector3 Dock(string id)=>id switch
        {"storage_farm"=>new(-2,0,30),"storage_farm_shop"=>new(7,0,13),"storage_processing"=>new(48,0,2),"storage_supermarket"=>new(27,0,32),"storage_bakery"=>new(3,0,52),"storage_restaurant"=>new(-34.7f,0,44),_=>Vector3.zero};
        static List<Vector3> Access(string id)=>id switch
        {
            "storage_farm"=>new(){Dock(id),new(-2,0,55)},
            "storage_farm_shop"=>new(){Dock(id),new(7,0,19),new(24,0,19),new(24,0,55)},
            "storage_processing"=>new(){Dock(id),new(54,0,2),new(54,0,55)},
            "storage_supermarket"=>new(){Dock(id),new(24,0,32),new(24,0,55)},
            "storage_bakery"=>new(){Dock(id),new(3,0,55)},
            _=>new(){Dock(id),new(-34.7f,0,55)}
        };
        public static List<RoutePoint> Path(string source,string destination)
        {
            var points=Access(source);var end=Access(destination);end.Reverse();points.AddRange(end);
            var result=new List<RoutePoint>();foreach(var p in points)result.Add(new(p.x,p.z));return result;
        }
        public static Vector3 Position(TruckRuntimeState state)
        {
            if(state.phase!="Travelling"||state.path.Count<2)return Dock(state.current);
            double left=state.travelled;
            for(int i=1;i<state.path.Count;i++)
            {
                var a=new Vector3(state.path[i-1].x,0,state.path[i-1].z);var b=new Vector3(state.path[i].x,0,state.path[i].z);float distance=Vector3.Distance(a,b);
                if(left<=distance)return Vector3.Lerp(a,b,distance>.001?Mathf.Clamp01((float)left/distance):1);left-=distance;
            }
            var last=state.path[^1];return new(last.x,0,last.z);
        }
        public static void BuildDocks(GameSession game,Transform world)
        {
            foreach(var storage in game.Stations.ToArray())if(storage is StorageStation)
            {
                var root=new GameObject("dock_"+storage.Id);root.transform.SetParent(world);root.transform.position=Dock(storage.Id);
                var dock=root.AddComponent<CargoDock>();dock.Id=root.name;dock.WarehouseId=storage.Id;dock.AreaId=storage.AreaId;dock.Requirement=storage.Requirement;dock.InteractionPoint=root.transform.position+Vector3.left*1.7f;dock.TownWaitingPoint=root.transform.position+Vector3.back*3.5f;dock.HasTownWaitingPoint=true;dock.Inventory=new Inventory(0);dock.Label="BẾN "+GameHud.AreaLabel(dock.AreaId).ToUpperInvariant();
                Art.Model("supply_crate",new(-1.7f,.05f,0),root.transform,.9f);dock.StatusLabel=Art.Label(dock.Label,new(-1.7f,1.5f,0),root.transform,.16f);
                root.AddComponent<UnlockVisual>().Requirement=dock.Requirement;game.Stations.Add(dock);
            }
        }
    }
}
