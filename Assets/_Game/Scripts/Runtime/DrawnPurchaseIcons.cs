using UnityEngine;
namespace Tycoon
{
    public static partial class DrawnPurchaseIcons
    {
        public const int Size = 256;
        static readonly Color32 Ink = new(44, 52, 50, 255);
        static readonly Color32 Cream = new(244, 238, 223, 255);
        static readonly Color32 Green = new(113, 145, 91, 255);
        static readonly Color32 Blue = new(109, 151, 162, 255);
        static readonly Color32 Gold = new(218, 179, 105, 255);
        static readonly Color32 Orange = new(208, 139, 75, 255);
        static readonly Color32 White = new(250, 249, 240, 255);
        static readonly Color32 Brown = new(151, 112, 77, 255);
        static readonly Color32 Skin = new(225, 180, 145, 255);
        public static string Subject(UpgradeDefinition u)
        {
            if(u.kind=="worker")return u.role switch
            {
                "Farmer"=>"farmer","Restocker" or "RestockerMarket"=>"restocker","Cashier" or "CashierMarket"=>"cashier",
                "Transporter"=>"transporter","Animal" or "AnimalWorker"=>"animal_worker","Processor"=>"processor",
                "Baker"=>"baker","Cook"=>"cook","Waiter"=>"waiter","Repairer"=>"repairer","Loader"=>"loader","Driver"=>"driver",_=>"upgrade"
            }
            ;
            if(u.family.StartsWith("storage_"))return "crate";
            if(u.family.Length>0)return u.family switch
            {
                "farm"=>"carrot","animal"=>"cow","counter"=>"counter","mill"=>"mill","market"=>"market","oven"=>"oven","kitchen"=>"kitchen","player"=>"basket",_=>"upgrade"
            }
            ;
            return u.id switch
            {
                "barn"=>"cow","milk_line"=>"milk","egg_line"=>"eggs","sheep_line"=>"sheep","farm_shop"=>"shop",
                "mill"=>"mill","dairy"=>"cheese","supermarket"=>"market","bakery"=>"bread","restaurant"=>"meal",
                "conveyor_processing" or "conveyor_bakery"=>"conveyor",_=>"upgrade"
            }
            ;
        }
        public static Color32[] Draw(UpgradeDefinition upgrade)
        {
            var pixels=new Color32[Size*Size];
            var canvas=new PurchaseIconCanvas(pixels,0,0,2);
            canvas.Circle(64,64,58,Ink);
            canvas.Circle(64,65,54,Cream);
            canvas.Ellipse(64,38,39,7,new Color32(80,72,55,35));
            var pen=canvas.At(62,68,1);
            string subject=Subject(upgrade);
            if(upgrade.kind=="worker")Worker(pen,subject);
            else ObjectIcon(pen,subject);
            if(upgrade.axisLevel>0)
            for(int i=0;i<Mathf.Min(4,upgrade.axisLevel);i++)canvas.Circle(44+i*10,17,3.2f,Green);
            Badge(canvas.At(102,25,1),upgrade.axis);
            return pixels;
        }
    }
}
