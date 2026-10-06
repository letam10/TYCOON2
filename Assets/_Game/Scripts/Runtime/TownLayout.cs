using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public static class TownLayout
    {
        public const int Revision=1;
        public static Vector3 Spawn(int index)=>new(104,0,-12+(index%3-1)*.6f);
        public static Vector3 Gate=>new(58,0,-12);
        public static Vector3 Offset(string area)=>area switch{"processing"=>new(8,0,0),"supermarket"=>new(12,0,0),"bakery"=>new(7,0,0),_=>Vector3.zero};
        public static bool Migrate(SaveData data)
        {
            if(data.transactionState?.layoutRevision>=Revision)return false;
            foreach(var customer in data.customers)
            {
                Vector3 delta=Offset(customer.shop=="market"?"supermarket":customer.shop);customer.x+=delta.x;customer.z+=delta.z;
                var exit=Spawn((int)(customer.receipt%3));customer.exitX=exit.x;customer.exitY=exit.y;customer.exitZ=exit.z;
            }
            foreach(var worker in data.workers){var crew=data.crews.Find(x=>x.id==worker.upgrade);if(crew?.role is "Loader" or "Driver")continue;var delta=Offset(crew?.area);worker.x+=delta.x;worker.z+=delta.z;}
            if(data.transactionState!=null)
            {
                data.transactionState.layoutRevision=Revision;
                foreach(var owner in data.transactionState.owners)
                {
                    if(owner.customer?.receipt>0){var c=data.customers.Find(x=>x.receipt==owner.customer.receipt);if(c!=null)owner.customer=TransactionCore.Copy(c);}
                    if(!string.IsNullOrEmpty(owner.worker?.id)){var w=data.workers.Find(x=>x.id==owner.worker.id);if(w!=null)owner.worker=TransactionCore.Copy(w);}
                }
            }
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
            WaitingPoints(game,world);
            LayoutPads(game);
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
            float lane=route.ItemId=="wheat"?8.2f:route.ItemId=="milk"?8.8f:9.4f;
            Vector3[] points=route.AreaId=="bakery"?new[]{start,new Vector3(end.x,0,start.z),end}:new[]{start,new Vector3(46.5f,0,start.z),new Vector3(46.5f,0,lane),new Vector3(end.x,0,lane),end};
            for(int i=1;i<points.Length;i++)
            {
                var segment=new GameObject("FixedBelt").transform;segment.SetParent(route.VisualRoot.transform,false);segment.position=points[i-1];segment.rotation=Quaternion.LookRotation(points[i]-points[i-1]);float length=Vector3.Distance(points[i-1],points[i]);
                Art.Box("Belt",new(0,.23f,length*.5f),new(.45f,.16f,length),"#58685E",segment);
                Art.Box("RailLeft",new(-.26f,.33f,length*.5f),new(.05f,.12f,length),"#C9BA83",segment);Art.Box("RailRight",new(.26f,.33f,length*.5f),new(.05f,.12f,length),"#C9BA83",segment);
            }
            var stack=new GameObject("OwnedGoods").AddComponent<InventoryStack>();stack.transform.SetParent(route.VisualRoot.transform,false);stack.transform.position=points[points.Length-2]+Vector3.up*.35f;stack.Inventory=route.Inventory;stack.Pool=game.Pool;stack.Columns=1;stack.Rows=4;stack.Scale=.45f;
        }
        static List<Bounds> Footprints(GameSession game)
        {
            var list=new List<Bounds>{Box(new(7,0,6),22,20),Box(new(36,0,7),18,18),Box(new(40,0,33),22,22),Box(new(11,0,38),20,20),Box(new(-20,0,39),22,20),Box(new(-33,0,18),10,8),Box(new(-39,0,28),8,7),Box(new(-43,0,39),8,8)};
            foreach(var s in game.Stations.Where(x=>x is not PurchasePad and not ConveyorStation))
            {
                list.Add(s is ProductionStation p&&!p.Animal?Box(s.transform.position,6.6f,6.6f):Box(s.transform.position,s is StorageStation?5.7f:5.1f,s is ShelfStation?7.2f:4.9f));
                list.Add(Box(s.WorkPoint,1.5f,1.5f));list.Add(Box(s.WaitingPoint,1.5f,1.5f));
                if(s is CheckoutStation counter){list.Add(Box(counter.CollectionPoint,2.5f,2.5f));for(int i=0;i<30;i++)list.Add(Box(counter.QueuePoint(i),1.2f,1.2f));}
            }
            foreach(var s in game.Stations.OfType<StorageStation>())list.Add(Box(TruckRoutes.Dock(s.Id),4.4f,6));
            foreach(var road in RoadBounds())list.Add(road);return list;
        }
        static Station Related(GameSession game,UpgradeDefinition u)
        {
            if(u.kind=="worker")
            {
                var crew=GameSession.CrewFor(u);
                return crew.role=="Farmer"?game.Producers.First(x=>!x.Animal):crew.role=="AnimalWorker"?game.Producers.First(x=>x.Animal):
                    crew.role=="Repairer"||crew.role is "Processor" or "Cook"?game.Machines.FirstOrDefault(x=>x.AreaId==crew.area):game.StorageFor(crew.area);
            }
            if(u.family=="player")return game.Producers.First(x=>!x.Animal);
            if(u.family.StartsWith("storage_"))return game.Stations.Find(s=>s.Id==u.family);
            if(!string.IsNullOrEmpty(u.family))return game.Stations.FirstOrDefault(s=>s is not PurchasePad&&ProgressionTracker.Family(new(){kind=s is ProductionStation?"producer":s is StorageStation?"storage":"machine",item=(s as ProductionStation)?.ItemId,area=s.AreaId})==u.family);
            return game.Stations.FirstOrDefault(s=>s is not PurchasePad&&s.Requirement==u.id);
        }
        static void LayoutPads(GameSession game)
        {
            var occupied=Footprints(game);var placed=new List<Bounds>();
            foreach(var pad in game.Stations.OfType<PurchasePad>().OrderBy(p=>p.Upgrade.id))
            {
                var target=Related(game,pad.Upgrade)??game.Storage;Vector3 best=target.transform.position+Vector3.back*3.5f;bool found=false;
                for(float radius=3.3f;radius<=20&&!found;radius+=1.4f)
                    for(int i=0;i<24;i++)
                    {
                        Vector3 point=target.transform.position+new Vector3(Mathf.Sin(i*Mathf.PI/12),0,Mathf.Cos(i*Mathf.PI/12))*radius;
                        if(point.x < -47||point.x>58||point.z < -18||point.z>58)continue;
                        var box=Box(point,2.15f,2.15f);if(occupied.Any(b=>b.Intersects(box))||placed.Any(b=>b.Intersects(box)))continue;
                        best=point;found=true;placed.Add(box);break;
                    }
                pad.transform.position=best;pad.InteractionPoint=best;pad.InteractionRadius=1;
                foreach(var label in pad.GetComponentsInChildren<TextMesh>())if(label!=pad.StatusLabel){label.text=pad.Upgrade.cost.ToString("N0");label.transform.localPosition=new(0,.12f,-.56f);}
                pad.InitializeIcon();
            }
        }
        public static IEnumerable<Bounds> RoadBounds()
        {
            yield return Box(new(16,0,-12),180,6);yield return Box(new(6,0,55),106,5);
            yield return Box(new(54,0,28.5f),5,53);yield return Box(new(24,0,37),5,36);
            yield return Box(new(-2,0,42.5f),5,25);yield return Box(new(-34.7f,0,49.5f),5,11);
            yield return Box(new(15.5f,0,19),17,5);
        }
        static void Roads(Transform parent)
        {
            var root=new GameObject("TownRoads").transform;root.SetParent(parent,false);
            foreach(var b in RoadBounds())Art.Box("Road",new(b.center.x,.045f,b.center.z),new(b.size.x,.035f,b.size.z),"#B8B1A0",root);
            for(int i=0;i<18;i++)Art.Box("CenterMark",new(-46+i*6,.067f,-12),new(2,.01f,.1f),"#EFE8D3",root);
            Art.Box("CustomerStreetEast",new(60,.044f,4),new(2.8f,.03f,34),"#C1BDAA",root);
            Art.Box("CustomerStreetBakery",new(20.5f,.044f,4),new(2.8f,.03f,34),"#C1BDAA",root);
            Art.Box("River",new(4,-.015f,66),new(126,.03f,7),"#66AEB8",root);
            for(int i=0;i<8;i++)Art.Model("garden_rock",new(-46+i*15,0,71),root,4+i%3);
            for(int i=0;i<7;i++)
            {var mound=GameObject.CreatePrimitive(PrimitiveType.Sphere);mound.name="Hill";mound.transform.SetParent(root,false);mound.transform.localPosition=new(-43+i*18,-3,80);mound.transform.localScale=new(20,14+i%3*2,19);mound.GetComponent<Renderer>().sharedMaterial=Art.Material(i%2==0?"#789C75":"#819B83");Object.Destroy(mound.GetComponent<Collider>());}
        }
    }
}
