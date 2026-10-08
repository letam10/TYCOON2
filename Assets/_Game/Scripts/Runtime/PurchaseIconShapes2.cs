using UnityEngine;
namespace Tycoon
{
    public static partial class DrawnPurchaseIcons
    {
        static void Shop(PurchaseIconCanvas p)
        {
            p.Rect(-31,-28,62,47,Ink);
            p.Rect(-27,-24,54,39,Gold);
            p.Rect(-21,-22,19,25,Blue);
            p.Rect(8,-24,14,32,White);
            p.Poly(Ink,new(-37,14),new(37,14),new(28,33),new(-28,33));
            p.Poly(Green,new(-32,17),new(32,17),new(25,29),new(-25,29));
            for(int i=0;i<5;i++)p.Rect(-30+i*13,10,9,8,i%2==0?White:Green);
        }
        static void Gear(PurchaseIconCanvas p)
        {
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;
                p.Line(Mathf.Cos(a)*12,Mathf.Sin(a)*12,Mathf.Cos(a)*25,Mathf.Sin(a)*25,10,Ink);
            }
            p.Circle(0,0,22,Ink);
            p.Circle(0,0,17,Blue);
            p.Circle(0,0,8,Ink);
            p.Circle(0,0,4,Cream);
        }
        static void Mill(PurchaseIconCanvas p)
        {
            p.Rect(-29,-28,58,49,Ink);
            p.Rect(-25,-24,50,41,Blue);
            p.Poly(Gold,new(-22,23),new(22,23),new(9,11),new(-9,11));
            Gear(p.At(0,-1,.65f));
            p.Rect(-25,-32,9,5,Ink);
            p.Rect(16,-32,9,5,Ink);
        }
        static void Cheese(PurchaseIconCanvas p)
        {
            p.Poly(Ink,new(-31,-22),new(31,-22),new(31,19),new(-15,31),new(-31,12));
            p.Poly(Gold,new(-27,-18),new(27,-18),new(27,16),new(-14,27),new(-27,10));
            p.Line(-27,10,27,16,3,Orange);
            p.Circle(-13,-5,5,Orange);
            p.Circle(10,2,6,Orange);
            p.Circle(18,-12,3,Orange);
        }
        static void Cart(PurchaseIconCanvas p)
        {
            p.Line(-34,30,-24,30,5,Ink);
            p.Line(-24,30,-12,-23,5,Ink);
            p.Line(-12,-23,28,-23,5,Ink);
            p.Poly(Ink,new(-21,20),new(35,20),new(25,-10),new(-14,-10));
            p.Poly(Green,new(-16,16),new(29,16),new(22,-6),new(-10,-6));
            p.Line(0,15,3,-5,3,Cream);
            p.Line(16,15,15,-5,3,Cream);
            p.Line(-14,5,27,5,3,Cream);
            p.Circle(-8,-29,7,Ink);
            p.Circle(24,-29,7,Ink);
            p.Circle(-8,-29,3,Gold);
            p.Circle(24,-29,3,Gold);
        }
    }
}
