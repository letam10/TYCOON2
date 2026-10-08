using UnityEngine;
namespace Tycoon
{
    public static partial class DrawnPurchaseIcons
    {
        static void Conveyor(PurchaseIconCanvas p)
        {
            p.Line(-29,-17,29,-17,20,Ink);
            p.Line(-29,-17,29,-17,12,Blue);
            for(int i=0;i<5;i++)p.Line(-25+i*12,-22,-25+i*12,-12,3,Cream);
            Crate(p.At(-16,9,.38f));
            Crate(p.At(18,9,.38f));
            p.Line(-20,-26,-20,-32,5,Ink);
            p.Line(20,-26,20,-32,5,Ink);
        }
        static void Bread(PurchaseIconCanvas p)
        {
            p.Ellipse(0,0,34,23,Ink);
            p.Ellipse(0,2,30,20,Brown);
            p.Ellipse(0,7,28,13,Gold);
            for(int i=0;i<3;i++)p.Line(-19+i*16,10,-12+i*16,-3,4,Cream);
        }
        static void Meal(PurchaseIconCanvas p)
        {
            p.Ellipse(0,-3,34,25,Ink);
            p.Ellipse(0,-1,30,21,White);
            p.Ellipse(0,-1,22,15,Cream);
            p.Ellipse(-6,1,14,9,Orange);
            p.Circle(15,3,6,Green);
            p.Circle(12,-7,6,Green);
            p.Line(-43,-27,-43,24,4,Ink);
            p.Line(43,-27,43,24,4,Ink);
            for(int i=0;i<3;i++)p.Line(-49+i*6,16,-49+i*6,29,3,Ink);
        }
        static void Truck(PurchaseIconCanvas p)
        {
            p.Rect(-36,-18,42,38,Ink);
            p.Rect(-32,-14,34,30,Gold);
            p.Poly(Ink,new(4,-18),new(38,-18),new(38,2),new(25,18),new(4,18));
            p.Poly(Blue,new(9,-13),new(33,-13),new(33,1),new(23,13),new(9,13));
            p.Rect(12,2,12,9,White);
            p.Circle(-22,-21,10,Ink);
            p.Circle(25,-21,10,Ink);
            p.Circle(-22,-21,5,Cream);
            p.Circle(25,-21,5,Cream);
        }
    }
}
