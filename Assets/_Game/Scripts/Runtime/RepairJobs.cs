using System;

namespace Tycoon
{
    public static class RepairTiming
    {
        public const float Player=5;
        public static float Employee(int speedLevel)=>Math.Max(10,30-10*(speedLevel-1));
    }
    public sealed partial class TransactionCore
    {
        int AdvanceRepair(TransactionState s,TransactionCommand c)
        {
            var m=Machine(s,c);var p=m.progress;bool player=c.actor=="player";float seconds=RepairTiming.Player;
            if(player)Player(s,c.actor);
            else
            {
                var owner=s.owners.Find(x=>x.kind==OwnerKind.Worker&&x.actor==c.actor);
                var crew=s.crews.Find(x=>x.id==owner?.worker?.upgrade);
                Require(crew!=null&&crew.role=="Repairer"&&crew.area==m.area,"authority","Chỉ kỹ thuật viên đúng khu được sửa máy.");
                seconds=RepairTiming.Employee(crew.speedLevel);
            }
            Require(p.broken&&double.IsFinite(c.duration)&&c.duration>0,"repair","Máy chưa cần sửa.");
            // Player tiếp quản work slot; tỷ lệ sửa và payment của sự cố được giữ nguyên.
            if(player&&m.operatorId!="player"){m.operatorId=null;m.operatorUntil=0;}
            Lease(m,c.actor,clock());
            if(!p.repairPaid){Require(s.money>=p.repairFee,"funds","Chờ đủ tiền trả phí sửa máy.");s.money-=p.repairFee;p.repairPaid=true;}
            if(!player)p.playerOnlyRepair=false;
            p.repairProgress=Math.Min(1,p.repairProgress+(float)c.duration/seconds);
            p.repairRemaining=(1-p.repairProgress)*RepairTiming.Player;
            if(p.repairProgress>=.999999f)
            {
                p.repairProgress=1;p.repairRemaining=0;p.broken=false;p.repairPaid=false;
                if(player&&p.playerOnlyRepair)p.playerRepairCount++;
                m.operatorId=null;m.operatorUntil=0;
            }
            return 1;
        }
    }
}
