using UnityEngine;
namespace Tycoon
{
    public static partial class DrawnPurchaseIcons
    {
        static void ObjectIcon(PurchaseIconCanvas p,string subject)
        {
            switch(subject)
            {
                case "carrot":Carrot(p);
                break;
                case "cow":Cow(p);
                break;
                case "milk":Milk(p);
                break;
                case "eggs":Eggs(p);
                break;
                case "sheep":Sheep(p);
                break;
                case "shop":Shop(p);
                break;
                case "mill":Mill(p);
                break;
                case "cheese":Cheese(p);
                break;
                case "market":Cart(p);
                break;
                case "oven":Oven(p);
                break;
                case "kitchen":Pot(p);
                break;
                case "basket":Basket(p);
                break;
                case "crate":Crate(p);
                break;
                case "counter":Counter(p);
                break;
                case "conveyor":Conveyor(p);
                break;
                case "bread":Bread(p);
                break;
                case "meal":Meal(p);
                break;
                default:Arrow(p,Green);
                break;
            }
        }
        static void Carrot(PurchaseIconCanvas p)
        {
            p.Poly(Ink,new(-19,13),new(20,9),new(-8,-31));
            p.Poly(Orange,new(-15,10),new(16,7),new(-7,-26));
            p.Line(-1,13,-13,32,7,Green);
            p.Line(2,14,5,36,7,Green);
            p.Line(5,14,19,29,7,Green);
            p.Line(-12,1,-1,-1,3,Brown);
            p.Line(-9,-9,0,-10,3,Brown);
        }
        static void Cow(PurchaseIconCanvas p)
        {
            p.Line(-19,24,-29,31,7,Gold);
            p.Line(19,24,29,31,7,Gold);
            p.Ellipse(-23,15,14,8,Ink);
            p.Ellipse(23,15,14,8,Ink);
            p.Ellipse(-23,16,11,5,White);
            p.Ellipse(23,16,11,5,White);
            p.Ellipse(0,3,25,29,Ink);
            p.Ellipse(0,5,21,25,White);
            p.Ellipse(-9,18,9,10,Brown);
            p.Ellipse(0,-14,20,12,Ink);
            p.Ellipse(0,-13,17,9,Skin);
            p.Circle(-10,6,2.7f,Ink);
            p.Circle(10,6,2.7f,Ink);
            p.Circle(-7,-13,2,Ink);
            p.Circle(7,-13,2,Ink);
        }
        static void Milk(PurchaseIconCanvas p)
        {
            p.Poly(Ink,new(-20,-29),new(20,-29),new(20,11),new(10,23),new(10,31),new(-10,31),new(-10,23),new(-20,11));
            p.Poly(White,new(-16,-25),new(16,-25),new(16,10),new(7,21),new(-7,21),new(-16,10));
            p.Rect(-11,27,22,7,Blue);
            p.Rect(-16,-9,32,19,Blue);
            p.Ellipse(0,1,6,8,White);
        }
        static void Eggs(PurchaseIconCanvas p)
        {
            p.Rect(-34,-25,68,12,Brown);
            foreach(float x in new[]
            {
                -20f,0,20
            }
            )
            {
                p.Ellipse(x,0,14,24,Ink);
                p.Ellipse(x,1,11,21,White);
                p.Ellipse(x-3,6,3,9,Cream);
            }
            p.Line(-30,-22,30,-22,3,Gold);
        }
        static void Sheep(PurchaseIconCanvas p)
        {
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;
                p.Circle(Mathf.Cos(a)*19,Mathf.Sin(a)*15+4,15,Ink);
            }
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;
                p.Circle(Mathf.Cos(a)*19,Mathf.Sin(a)*15+5,12,White);
            }
            p.Ellipse(0,0,18,25,Ink);
            p.Ellipse(0,1,14,22,Brown);
            p.Circle(-7,8,2.4f,White);
            p.Circle(7,8,2.4f,White);
            p.Circle(-8,27,9,White);
            p.Circle(7,27,10,White);
        }
    }
}
