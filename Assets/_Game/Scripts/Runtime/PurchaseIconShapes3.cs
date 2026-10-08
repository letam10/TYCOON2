using UnityEngine;
namespace Tycoon
{
    public static partial class DrawnPurchaseIcons
    {
        static void Oven(PurchaseIconCanvas p)
        {
            p.Rect(-31,-29,62,59,Ink);
            p.Rect(-27,-25,54,51,Blue);
            p.Rect(-22,-18,44,29,Ink);
            p.Rect(-18,-14,36,21,Brown);
            Bread(p.At(0,-4,.43f));
            p.Circle(-17,21,3,Gold);
            p.Circle(-5,21,3,Gold);
            p.Line(8,20,21,20,3,White);
        }
        static void Pot(PurchaseIconCanvas p)
        {
            p.Line(-32,8,32,8,7,Ink);
            p.Ellipse(0,-7,26,21,Ink);
            p.Ellipse(0,-5,22,17,Blue);
            p.Rect(-26,3,52,7,Ink);
            p.Line(-17,17,-12,23,3,Green);
            p.Line(-12,23,-15,30,3,Green);
            p.Line(3,18,8,26,3,Green);
            p.Line(8,26,5,33,3,Green);
        }
        static void Basket(PurchaseIconCanvas p)
        {
            p.Ellipse(0,15,25,25,Ink);
            p.Ellipse(0,15,20,20,Cream);
            p.Poly(Ink,new(-34,8),new(34,8),new(24,-28),new(-24,-28));
            p.Poly(Gold,new(-28,4),new(28,4),new(20,-23),new(-20,-23));
            p.Line(-25,-5,25,-5,3,Brown);
            p.Line(-22,-15,22,-15,3,Brown);
            p.Line(-11,2,-8,-23,3,Brown);
            p.Line(11,2,8,-23,3,Brown);
        }
        static void Crate(PurchaseIconCanvas p)
        {
            p.Rect(-30,-27,60,54,Ink);
            p.Rect(-26,-23,52,46,Brown);
            p.Rect(-26,13,52,7,Gold);
            p.Rect(-26,-20,52,7,Gold);
            p.Line(-23,-16,23,16,6,Gold);
            p.Line(-23,16,23,-16,6,Gold);
        }
        static void Counter(PurchaseIconCanvas p)
        {
            p.Rect(-32,-28,64,25,Ink);
            p.Rect(-28,-24,56,17,Green);
            p.Rect(-19,-2,38,23,Ink);
            p.Rect(-15,2,30,15,Blue);
            p.Rect(-14,20,28,11,Ink);
            p.Rect(-10,23,20,5,Gold);
            p.Circle(22,10,10,Gold);
            p.Line(22,4,22,16,3,Ink);
        }
    }
}
