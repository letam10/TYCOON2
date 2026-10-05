using System;
using System.Linq;

namespace Tycoon
{
    public sealed partial class TransactionCore
    {
        static void EnsureTruck(TransactionState s)
        {
            s.truck??=new();
            if(!s.owners.Any(x=>x.id==s.truck.id))s.owners.Add(new(){id=s.truck.id,actor="simulation",location=s.truck.id,kind=OwnerKind.Truck,capacity=0,writers=s.owners.Where(x=>x.kind is OwnerKind.Player or OwnerKind.Worker).Select(x=>x.actor).ToList()});
        }
        static OwnerState Warehouse(TransactionState s,string id)
        {var o=Owner(s,id);var station=s.stations.Find(x=>x.id==id);Require(o.kind==OwnerKind.Storage&&(station==null||string.IsNullOrEmpty(station.requirement)||s.unlocked.Contains(station.requirement)),"warehouse","Kho chưa mở.");return o;}
        static string WarehouseArea(TransactionState s,string id)=>s.stations.Find(x=>x.id==id)?.area??Owner(s,id).location;
        static CargoCrateState Crate(TransactionState s,string id)
        {var box=s.crates.Find(x=>x.id==id);Require(box!=null,"cargo","Thùng không còn tồn tại.");return box;}
        static int CrateCount(TransactionState s,CargoCrateState box)=>s.stacks.Where(x=>x.owner==box.id).Sum(x=>x.quantity);
        static void CargoActor(TransactionState s,string actor,string area)
        {
            if(actor=="player"){Player(s,actor);return;}
            var worker=s.owners.Find(x=>x.kind==OwnerKind.Worker&&x.actor==actor);
            var crew=s.crews.Find(x=>x.id==worker?.worker?.upgrade);
            Require(crew?.role=="Loader"&&crew.area==area,"authority","Cần nhân viên bốc hàng đúng khu.");
        }
        static void LocateCrate(TransactionState s,CargoCrateState box,string holder)
        {
            box.holder=holder;var owner=Owner(s,box.id);owner.location=holder+"/"+box.id;
            foreach(var stack in s.stacks.Where(x=>x.owner==box.id))stack.location=owner.location;
        }
        static int ProtectedStock(TransactionState s,string warehouse,string item)
        {
            string area=WarehouseArea(s,warehouse);int reserve=0;
            foreach(var station in s.stations.Where(x=>x.area==area&&(string.IsNullOrEmpty(x.requirement)||s.unlocked.Contains(x.requirement))))
            {
                if(station.kind=="machine")reserve+=Definitions.Recipe(station.definitionId)?.inputs.Where(x=>x.id==item).Sum(x=>x.count)??0;
                if(station.kind=="producer"&&Definitions.IsAnimal(station.item)&&item=="animal_feed")reserve+=Math.Max(0,3-station.progress.feed);
            }
            return reserve;
        }
        static int PackCrate(TransactionState s,TransactionCommand c)
        {
            Require(s.truck!=null&&s.truck.phase is not ("Travelling" or "WaitingUnload"),"truck","Xe chưa sẵn sàng chất hàng.");
            var truck=s.truck;CargoActor(s,c.actor,WarehouseArea(s,truck.current));
            Require(truck.current==truck.source&&c.quantity is >0 and <=6&&!s.crates.Any(x=>x.id==c.target),"cargo","Cần đóng thùng ở kho nguồn, tối đa 6 món.");
            var source=Owner(s,c.source);WriteAccess(source,c.actor);
            Require(source.kind==OwnerKind.Player&&source.actor==c.actor||source.kind==OwnerKind.Storage&&source.id==truck.source,"cargo","Nguồn đóng thùng không đúng.");
            if(source.kind==OwnerKind.Storage)
            {
                int available=s.stacks.Where(x=>x.owner==source.id&&x.item==c.item).Sum(x=>x.quantity-Reserved(s,x.id));
                Require(available-c.quantity>=ProtectedStock(s,source.id,c.item),"reserve","Giữ lại nguyên liệu cần cho sản xuất.");
            }
            var allocation=Allocate(s,source.id,c.item,c.quantity);
            var box=new CargoCrateState{id=c.target,item=c.item,source=truck.current,holder="dock:"+truck.current};
            s.owners.Add(new(){id=box.id,actor="simulation",location=box.holder+"/"+box.id,kind=OwnerKind.Crate,capacity=6,singleItem=true,accepts=new(){c.item},writers=s.owners.Where(x=>x.kind is OwnerKind.Player or OwnerKind.Worker).Select(x=>x.actor).ToList()});
            Consume(s,allocation);AddStack(s,"cargo:"+c.effectId,box.id,c.item,c.quantity);s.crates.Add(box);
            if(source.kind==OwnerKind.Player&&s.stacks.All(x=>x.owner!=source.id)){s.carryOrigin=null;s.carryBatch=null;}
            return c.quantity;
        }
        static int ClaimCrate(TransactionState s,TransactionCommand c)
        {
            var truck=s.truck;Require(truck!=null,"truck","Chưa mua xe.");CargoActor(s,c.actor,WarehouseArea(s,truck.current));
            var carrier=s.owners.Find(x=>x.kind==OwnerKind.Worker&&x.actor==c.actor);Require(carrier!=null,"authority","Claim dành cho nhân viên bốc hàng.");
            var box=Crate(s,c.target);bool loading=box.holder=="dock:"+truck.current&&truck.current==truck.source&&truck.phase!="Travelling";
            bool unloading=box.holder==truck.id&&truck.phase=="WaitingUnload";
            Require(loading||unloading,"cargo","Thùng đang có người giữ hoặc xe đang đi.");
            var held=s.crates.Where(x=>x.holder==c.actor).ToArray();
            Require(!s.stacks.Any(x=>x.owner==carrier.id)&&held.All(x=>x.item==box.item)&&held.Sum(x=>CrateCount(s,x))+CrateCount(s,box)<=carrier.capacity,"capacity","Nhân viên đầy hoặc đang mang loại khác.");
            if(loading)Require(s.crates.Count(x=>x.holder==truck.id||x.task=="load")<6,"capacity","Xe hết vị trí thùng.");
            box.task=loading?"load":"unload";LocateCrate(s,box,c.actor);return CrateCount(s,box);
        }
        static int LoadCrate(TransactionState s,TransactionCommand c)
        {
            var truck=s.truck;Require(truck!=null&&truck.phase is "Idle" or "Loading","truck","Xe chưa ở vị trí chất hàng.");
            CargoActor(s,c.actor,WarehouseArea(s,truck.current));var box=Crate(s,c.target);
            Require(truck.current==truck.source&&(box.holder=="dock:"+truck.current&&c.actor=="player"||box.holder==c.actor&&box.task=="load"),"cargo","Bạn không giữ thùng này.");
            Require(s.crates.Count(x=>x.holder==truck.id)<6,"capacity","Xe đã đủ 6 thùng.");
            box.task=null;LocateCrate(s,box,truck.id);truck.phase="Loading";
            if(c.actor=="player")RecordCargoJob(s,c,box,"load");return CrateCount(s,box);
        }
        static int SelectTruckRoute(TransactionState s,TransactionCommand c)
        {
            Player(s,c.actor);Require(s.truck!=null&&s.truck.phase!="Travelling"&&s.truck.phase!="WaitingUnload","truck","Hoàn thành chuyến hiện tại trước khi đổi tuyến.");
            Warehouse(s,c.source);Warehouse(s,c.destination);Require(c.source!=c.destination,"route","Chọn hai kho khác nhau.");
            Require(!s.crates.Any(x=>x.holder==s.truck.id)||c.source==s.truck.current,"route","Xe đang có hàng tại kho hiện tại.");
            s.truck.source=c.source;s.truck.destination=c.destination;s.truck.repeat=c.quantity==1;return 1;
        }
        int DispatchTruck(TransactionState s,TransactionCommand c)
        {
            Require(c.actor is "player" or "simulation","authority","Actor không được gửi xe.");var truck=s.truck;
            Require(truck!=null&&truck.phase is "Idle" or "Loading"&&!s.crates.Any(x=>x.task=="load"||x.task=="unload"),"truck","Chờ bốc/dỡ hàng hoàn tất.");
            Require(s.crews.Any(x=>x.id=="truck_bundle"&&x.role=="Driver"),"driver","Chưa có tài xế.");
            string target=truck.current!=truck.source?truck.source:truck.destination;Warehouse(s,target);
            var cargo=s.crates.Where(x=>x.holder==truck.id).ToArray();
            if(target==truck.destination)
            {
                Require(cargo.Length>0,"cargo","Chất ít nhất một thùng trước khi gửi xe.");
                foreach(var box in cargo)
                {
                    var reserve=new TransactionCommand{actor="simulation",target="cargo-reserve:"+c.effectId+":"+box.id,source=box.id,destination=target,item=box.item,quantity=CrateCount(s,box),expiresAt=double.MaxValue};
                    Reserve(s,reserve);box.reservation=reserve.target;
                }
            }
            else Require(cargo.Length==0,"cargo","Dỡ hết hàng trước khi đến kho nguồn khác.");
            Require(c.path!=null&&c.path.Count>=2,"route","Thiếu đường xe.");
            truck.distance=PathLength(c.path);Require(truck.distance>0&&double.IsFinite(truck.distance),"route","Đường xe không hợp lệ.");
            truck.path=c.path;truck.travelled=0;truck.tripTarget=target;truck.trip=c.effectId;truck.phase="Travelling";return cargo.Length;
        }
        static double PathLength(System.Collections.Generic.List<RoutePoint> points)
        {double length=0;for(int i=1;i<points.Count;i++){double x=points[i].x-points[i-1].x,z=points[i].z-points[i-1].z;length+=Math.Sqrt(x*x+z*z);}return length;}
        static int TickTruck(TransactionState s,TransactionCommand c)
        {
            var truck=s.truck;Require(c.actor=="simulation"&&truck!=null&&truck.phase=="Travelling"&&c.secondary==truck.trip&&double.IsFinite(c.duration)&&c.duration>0,"truck","Chuyến xe không hợp lệ.");
            int level=s.crews.Find(x=>x.id=="truck_bundle")?.speedLevel??1;
            truck.travelled=Math.Min(truck.distance,truck.travelled+c.duration*4*(1+.2*(level-1)));
            if(truck.travelled>=truck.distance){truck.current=truck.tripTarget;truck.phase=truck.current==truck.source?"Loading":"WaitingUnload";}
            return 1;
        }
        static int UnloadCrate(TransactionState s,TransactionCommand c)
        {
            var truck=s.truck;Require(truck!=null&&truck.phase=="WaitingUnload","truck","Xe chưa tới kho đích.");
            CargoActor(s,c.actor,WarehouseArea(s,truck.current));var box=Crate(s,c.target);
            Require(box.holder==truck.id&&c.actor=="player"||box.holder==c.actor&&box.task=="unload","cargo","Thùng đang do người khác dỡ.");
            var reservation=Reservation(s,box.reservation);Require(reservation.status==ReservationStatus.Active&&reservation.destination==truck.current,"reservation","Thiếu giữ chỗ kho đích.");
            reservation.holder=c.actor;var move=Copy(c);move.source=box.id;move.destination=truck.current;move.item=box.item;move.quantity=CrateCount(s,box);move.reservation=reservation.id;
            int n=Move(s,move,cargo:true);if(c.actor=="player")RecordCargoJob(s,c,box,"unload",n);
            s.owners.RemoveAll(x=>x.id==box.id);s.crates.Remove(box);
            if(!s.crates.Any(x=>x.holder==truck.id||x.task=="unload")){truck.phase="Idle";truck.completedTrips++;}return n;
        }
        static void RecordCargoJob(TransactionState s,TransactionCommand c,CargoCrateState box,string kind,int count=-1)
        {s.transportJobs.Add(new(){id="cargo-job:"+c.effectId,source=box.source,destination=s.truck.current,area=WarehouseArea(s,s.truck.current),item=box.item,quantity=count<0?CrateCount(s,box):count,kind=kind});}
        static void TrackHandDelivery(TransactionState s,TransactionCommand c)
        {
            if(c.destination=="player"&&s.stacks.Where(x=>x.owner=="player").Sum(x=>x.quantity)==c.quantity){s.carryOrigin=c.source;s.carryBatch=c.effectId;s.carryDestination=null;s.carryDelivered=0;}
            if(c.source!="player")return;
            if(Owner(s,c.destination).kind==OwnerKind.Storage){if(s.carryDestination!=c.destination){s.carryDestination=c.destination;s.carryDelivered=0;}s.carryDelivered+=c.quantity;}
            if(s.stacks.Any(x=>x.owner=="player"))return;
            if(s.carryOrigin!=null&&c.destination!=s.carryOrigin&&Owner(s,c.destination).kind==OwnerKind.Storage)
                s.transportJobs.Add(new(){id="hand-job:"+s.carryBatch,source=s.carryOrigin,destination=c.destination,area=WarehouseArea(s,c.destination),item=c.item,quantity=s.carryDelivered,kind="hand"});
            s.carryOrigin=null;s.carryBatch=null;s.carryDestination=null;s.carryDelivered=0;
        }
        static void ValidateCargo(TransactionState s)
        {
            Unique(s.crates.Select(x=>x.id));Unique(s.transportJobs.Select(x=>x.id));
            if(s.truck==null){Require(s.crates.Count==0,"cargo","Có thùng nhưng chưa có xe.");return;}
            Require(s.crates.Count(x=>x.holder==s.truck.id)<=6,"cargo","Xe vượt 6 thùng.");Owner(s,s.truck.id);Warehouse(s,s.truck.current);
            Require(s.truck.phase is "Idle" or "Loading" or "Travelling" or "WaitingUnload","truck","Sai phase xe.");
            Require(double.IsFinite(s.truck.distance)&&double.IsFinite(s.truck.travelled)&&s.truck.distance>=0&&s.truck.travelled>=0&&s.truck.travelled<=s.truck.distance,"truck","Progress xe không hợp lệ.");
            Require(s.truck.path!=null&&s.truck.path.All(p=>p!=null&&float.IsFinite(p.x)&&float.IsFinite(p.z)),"truck","Đường xe không hợp lệ.");
            foreach(var box in s.crates)
            {
                var owner=Owner(s,box.id);Require(owner.kind==OwnerKind.Crate&&owner.singleItem&&CrateCount(s,box) is >0 and <=6&&owner.location==box.holder+"/"+box.id,"cargo","Thùng/owner không hợp lệ.");
                Require(box.holder==s.truck.id||s.owners.Any(x=>x.kind==OwnerKind.Storage&&box.holder=="dock:"+x.id)||s.owners.Any(x=>x.kind==OwnerKind.Worker&&x.actor==box.holder),"cargo","Thùng không có vị trí hợp lệ.");
            }
        }
    }
}
