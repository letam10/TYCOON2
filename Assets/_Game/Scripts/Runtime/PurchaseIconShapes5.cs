using UnityEngine;
namespace Tycoon
{
    public static partial class DrawnPurchaseIcons
    {
        static void Worker(PurchaseIconCanvas p,string role)
        {
            if(role=="driver")
            {
                Truck(p.At(0,-7,.9f));
                p.Circle(20,0,5.5f,Skin);
                p.Rect(14,4,12,3,Blue);
                return;
            }
            bool chef=role is "cook" or "baker";
            p.Rect(-28,-29,37,32,Ink);
            p.Rect(-24,-25,29,25,chef?White:Blue);
            p.Line(-20,-18,-20,-2,5,Green);
            p.Line(1,-18,1,-2,5,Green);
            p.Ellipse(-10,19,16,19,Ink);
            p.Ellipse(-10,20,13,16,Skin);
            p.Circle(-15,21,2,Ink);
            p.Circle(-4,21,2,Ink);
            p.Line(-13,11,-6,11,2,Brown);
            if(chef)
            {
                p.Rect(-25,33,30,9,White);
                p.Circle(-23,44,9,White);
                p.Circle(-10,48,12,White);
                p.Circle(3,44,9,White);
            }
            else if(role=="farmer")
            {
                p.Rect(-34,32,48,6,Brown);
                p.Rect(-24,36,29,11,Gold);
            }
            else
            {
                p.Rect(-27,34,34,6,role is "repairer" or "loader"?Gold:Green);
                p.Rect(-23,38,26,8,role is "repairer" or "loader"?Gold:Green);
            }
            var job=p.At(26,-3,.48f);
            switch(role)
            {
                case "farmer":Carrot(job);
                break;
                case "animal_worker":Cow(job);
                break;
                case "cashier":Counter(job);
                break;
                case "processor":Gear(job);
                break;
                case "baker":Bread(job);
                break;
                case "cook":Pot(job);
                break;
                case "waiter":Meal(job);
                break;
                case "transporter":Cart(job);
                break;
                case "loader":Crate(job);
                break;
                case "restocker":job.Rect(-31,-24,62,7,Ink);
                job.Rect(-31,0,62,5,Ink);
                Crate(job.At(-16,16,.43f));
                Crate(job.At(16,16,.43f));
                break;
                case "repairer":job.Line(-23,-24,18,17,12,Ink);
                job.Line(-23,-24,18,17,7,Blue);
                job.Line(15,16,11,30,7,Ink);
                job.Line(15,16,30,18,7,Ink);
                break;
            }
        }
        static void Arrow(PurchaseIconCanvas p,Color32 color)=>p.Poly(color,new(-20,-2),new(-7,-2),new(-7,-23),new(7,-23),new(7,-2),new(20,-2),new(0,23));
        static void Badge(PurchaseIconCanvas p,UpgradeAxis axis)
        {
            p.Circle(0,0,22,Ink);
            p.Circle(0,1,18,axis==UpgradeAxis.QualityValue?Gold:axis==UpgradeAxis.Speed?Blue:Green);
            switch(axis)
            {
                case UpgradeAxis.Speed:p.Circle(0,0,12,White);
                p.Circle(0,0,9,Blue);
                p.Rect(-4,12,8,4,White);
                p.Line(0,0,0,7,3,White);
                p.Line(0,0,6,-3,3,White);
                break;
                case UpgradeAxis.QualityValue:
                var star=new Vector2[10];
                for(int i=0;i<10;i++)
                {
                    float a=(90+i*36)*Mathf.Deg2Rad;
                    float r=i%2==0?14:6;
                    star[i]=new(Mathf.Cos(a)*r,Mathf.Sin(a)*r);
                }
                p.Poly(White,star);
                break;
                case UpgradeAxis.Capacity:p.Rect(-12,-8,24,16,White);
                p.Rect(-9,-5,18,10,Green);
                p.Line(0,-5,0,7,3,White);
                p.Line(-9,0,9,0,3,White);
                break;
                case UpgradeAxis.StationLevel:Arrow(p.At(0,0,.55f),White);
                break;
                default:p.Line(-10,0,10,0,4,White);
                p.Line(0,-10,0,10,4,White);
                break;
            }
        }
    }
}
