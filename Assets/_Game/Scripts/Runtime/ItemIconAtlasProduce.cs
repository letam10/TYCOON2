using UnityEngine;

namespace Tycoon
{
    public static partial class ItemIconAtlas
    {
        static void Carrot(PurchaseIconCanvas p)
        {
            p.Poly(Ink, new(23, 16), new(45, 91), new(90, 66));
            p.Poly(Orange, new(29, 22), new(48, 86), new(84, 66));
            p.Line(64, 80, 62, 113, 10, Green);
            p.Line(65, 83, 94, 106, 10, Green);
            p.Line(67, 81, 105, 85, 9, Green);
            p.Line(43, 61, 57, 54, 4, Brown);
            p.Line(37, 43, 48, 38, 3, Brown);
        }

        static void Tomato(PurchaseIconCanvas p)
        {
            p.Ellipse(64, 56, 44, 38, Ink);
            p.Ellipse(64, 57, 40, 35, Red);
            p.Ellipse(48, 69, 10, 16, new Color32(255, 150, 99, 255));
            p.Poly(Green, new(64, 88), new(43, 105), new(48, 87), new(26, 90),
                new(49, 76), new(70, 78), new(94, 91), new(76, 90), new(80, 105));
            p.Line(64, 86, 69, 112, 6, Green);
        }

        static void Wheat(PurchaseIconCanvas p)
        {
            p.Line(50, 17, 69, 107, 5, Brown);
            for (int i = 0; i < 5; i++)
            {
                float y = 42 + i * 13;
                p.Ellipse(48 + i * 3, y + 4, 12, 7, Gold);
                p.Ellipse(75 + i * 2, y + 8, 12, 7, Gold);
            }
            p.Line(50, 18, 28, 65, 5, Green);
        }

        static void Corn(PurchaseIconCanvas p)
        {
            p.Ellipse(64, 65, 24, 48, Ink);
            p.Ellipse(64, 67, 21, 45, Gold);
            for (int row = 0; row < 7; row++)
                for (int column = 0; column < 3; column++)
                    p.Ellipse(51 + column * 13, 33 + row * 11, 4, 4, Orange);
            p.Poly(Green, new(63, 15), new(22, 29), new(21, 86), new(47, 64));
            p.Poly(Green, new(64, 14), new(101, 40), new(103, 78), new(79, 58));
        }

        static void Soybean(PurchaseIconCanvas p)
        {
            p.Line(31, 34, 95, 91, 31, Ink);
            p.Line(31, 35, 95, 91, 26, Green);
            for (int i = 0; i < 3; i++)
            {
                p.Circle(35 + i * 28, 40 + i * 24, 13, Gold);
                p.Line(32 + i * 28, 35 + i * 24, 35 + i * 28, 44 + i * 24, 3, Brown);
            }
        }

        static void Egg(PurchaseIconCanvas p)
        {
            p.Ellipse(64, 57, 34, 42, Ink);
            p.Ellipse(64, 58, 30, 39, White);
            p.Ellipse(65, 82, 24, 28, White);
            p.Ellipse(52, 79, 7, 16, new Color32(255, 255, 248, 255));
        }

        static void Beef(PurchaseIconCanvas p)
        {
            p.Ellipse(64, 62, 47, 31, White);
            p.Ellipse(64, 62, 42, 27, Red);
            p.Line(37, 53, 76, 76, 5, Pink);
            p.Line(47, 75, 84, 46, 4, Pink);
            p.Circle(86, 65, 11, White);
            p.Circle(86, 65, 6, Brown);
        }
    }
}
