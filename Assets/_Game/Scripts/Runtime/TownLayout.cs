using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public static class TownLayout
    {
        public const int Revision=CityDistricts.Revision;
        public static Vector3 Spawn(int index)=>CityDistricts.CustomerSpawn(index);
        public static Vector3 Gate=>CityDistricts.Gate;
        public static Vector3 Offset(string area)=>area switch{"processing"=>new(8,0,0),"supermarket"=>new(12,0,0),"bakery"=>new(7,0,0),_=>Vector3.zero};
        public static bool Migrate(SaveData data)
        {
            if(data.transactionState?.layoutRevision>=Revision)return false;
            if (data.transactionState?.layoutRevision >= 1)
            {
                CitySaveMigration.Apply(data);
                return true;
            }
            foreach(var customer in data.customers)
            {
                Vector3 delta=Offset(customer.shop=="market"?"supermarket":customer.shop);customer.x+=delta.x;customer.z+=delta.z;
                var exit=Spawn((int)(customer.receipt%3));customer.exitX=exit.x;customer.exitY=exit.y;customer.exitZ=exit.z;
            }
            foreach(var worker in data.workers){var crew=data.crews.Find(x=>x.id==worker.upgrade);if(crew?.role is "Loader" or "Driver")continue;var delta=Offset(crew?.area);worker.x+=delta.x;worker.z+=delta.z;}
            if(data.transactionState!=null)
            {
                data.transactionState.layoutRevision=1;
                foreach(var owner in data.transactionState.owners)
                {
                    if(owner.customer?.receipt>0){var c=data.customers.Find(x=>x.receipt==owner.customer.receipt);if(c!=null)owner.customer=TransactionCore.Copy(c);}
                    if(!string.IsNullOrEmpty(owner.worker?.id)){var w=data.workers.Find(x=>x.id==owner.worker.id);if(w!=null)owner.worker=TransactionCore.Copy(w);}
                }
            }
            CitySaveMigration.Apply(data);
            return true;
        }
        public static void Apply(GameSession game,Transform world)
        {
            foreach(var station in game.Stations.Where(s=>s is not PurchasePad and not ConveyorStation))
            {Vector3 delta=Offset(station.AreaId);station.transform.position+=delta;station.InteractionPoint+=delta;if(station is TableStation table)table.Seat+=delta;}
            MoveStorage("storage_farm",new(-6,0,30));MoveStorage("storage_processing",new(42.8f,0,2));MoveStorage("storage_supermarket",new(31.5f,0,32));MoveStorage("storage_restaurant",new(-28.8f,0,45));
            var feed=game.Machines.Find(x=>x.Id=="machine_feedmill");Move(feed,new(-26,0,30));
            Move(game.Checkouts.Find(x=>x.ShopId=="restaurant"),new(-13.5f,0,29));
            foreach(Transform child in world)
            {
                if(child.name.Contains("CHẾ BIẾN"))child.position+=Offset("processing");
                if(child.name.Contains("SIÊU THỊ"))child.position+=Offset("supermarket");
                if(child.name.Contains("TIỆM BÁNH"))child.position+=Offset("bakery");
                if(child.name=="ProcessingDecor")child.position=new(42,0,14.6f);
                if(child.name=="BakeryDecor")child.position+=Offset("bakery");
                if(child.name=="Pen_user_sheep")child.position+=Vector3.left*5;
            }
            CityExpansion.Apply(game, world);
            WaitingPoints(game,world);
            TownUpgradeLayout.Apply(game);
            Roads(world);
            foreach(var belt in game.Stations.OfType<ConveyorStation>())ConveyorVisual(game,belt);
            void MoveStorage(string id,Vector3 point)=>Move(game.Stations.Find(x=>x.Id==id),point);
            void Move(Station station,Vector3 point){if(!station)return;var delta=point-station.transform.position;station.transform.position=point;station.InteractionPoint+=delta;}
        }
        static Bounds Box(Vector3 center,float width,float depth)=>new(center,new(width,2,depth));
        static void WaitingPoints(GameSession game,Transform world)
        {
            Physics.SyncTransforms();var solids=world.GetComponentsInChildren<Collider>(true).Where(x=>x.name.EndsWith("Collision")||x.name.Contains("Wall")).Select(x=>{var b=x.bounds;b.Expand(new Vector3(1.8f,0,1.8f));return b;}).ToArray();
            var used=new List<Vector3>();
            foreach(var station in game.Stations.Where(x=>x is not PurchasePad and not ConveyorStation))
            {
                var desired=station.WaitingPoint;
                for(float radius=0;radius<=6;radius+=1.5f)
                {
                    bool placed=false;
                    for(int i=0;i<16;i++)
                    {
                        var p=desired+new Vector3(Mathf.Sin(i*Mathf.PI/8),0,Mathf.Cos(i*Mathf.PI/8))*radius;var sample=p+Vector3.up*.55f;
                        if(solids.Any(b=>b.Contains(sample))||used.Any(u=>(u-p).sqrMagnitude<2.25f))continue;
                        station.TownWaitingPoint=p;station.HasTownWaitingPoint=true;used.Add(p);placed=true;break;
                    }
                    if(placed)break;
                }
            }
        }
        static void ConveyorVisual(GameSession game,ConveyorStation route)
        {
            var storage=game.StorageFor(route.AreaId);route.Sources=new[]{storage};
            foreach(Transform child in route.VisualRoot.transform)Object.Destroy(child.gameObject);
            route.transform.position=storage.transform.position;route.VisualRoot.transform.localPosition=Vector3.zero;route.VisualRoot.transform.localRotation=Quaternion.identity;
            Vector3 start=storage.transform.position+Vector3.forward*2,end=route.Target.transform.position+Vector3.left*2.1f;
            Vector3 offset = CityDistricts.Offset(route.AreaId);
            float lane=(route.ItemId=="wheat"?8.2f:route.ItemId=="milk"?8.8f:9.4f)+offset.z;
            float laneX = 46.5f + offset.x;
            Vector3[] points=route.AreaId=="bakery"?new[]{start,new Vector3(end.x,0,start.z),end}:new[]{start,new Vector3(laneX,0,start.z),new Vector3(laneX,0,lane),new Vector3(end.x,0,lane),end};
            for(int i=1;i<points.Length;i++)
            {
                var segment=new GameObject("FixedBelt").transform;segment.SetParent(route.VisualRoot.transform,false);segment.position=points[i-1];segment.rotation=Quaternion.LookRotation(points[i]-points[i-1]);float length=Vector3.Distance(points[i-1],points[i]);
                Art.Box("Belt",new(0,.23f,length*.5f),new(.45f,.16f,length),"#58685E",segment);
                Art.Box("RailLeft",new(-.26f,.33f,length*.5f),new(.05f,.12f,length),"#C9BA83",segment);Art.Box("RailRight",new(.26f,.33f,length*.5f),new(.05f,.12f,length),"#C9BA83",segment);
            }
            var stack=new GameObject("OwnedGoods").AddComponent<InventoryStack>();stack.transform.SetParent(route.VisualRoot.transform,false);stack.transform.position=points[points.Length-2]+Vector3.up*.35f;stack.Inventory=route.Inventory;stack.Pool=game.Pool;stack.Columns=1;stack.Rows=4;stack.Scale=.45f;
        }
        public static IEnumerable<Bounds> RoadBounds() => CityStreetNetwork.Bounds();
        static void Roads(Transform parent) => CityStreetNetwork.Build(parent);
    }
}
