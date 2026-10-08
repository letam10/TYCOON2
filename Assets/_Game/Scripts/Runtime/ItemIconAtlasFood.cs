using UnityEngine;

namespace Tycoon
{
    public static partial class ItemIconAtlas
    {
        static void Plate(PurchaseIconCanvas p)
        {
            p.Ellipse(64, 55, 51, 35, Ink);
            p.Ellipse(64, 57, 48, 33, White);
            p.Ellipse(64, 57, 39, 25, new Color32(209, 221, 211, 255));
        }

        static void Bread(PurchaseIconCanvas p)
        {
            p.Line(35, 43, 91, 82, 45, Ink);
            p.Line(35, 44, 91, 82, 39, Brown);
            p.Line(37, 54, 81, 84, 25, Gold);
            for (int i = 0; i < 3; i++) p.Line(39 + i * 18, 49 + i * 12, 44 + i * 18, 66 + i * 12, 5, White);
        }

        static void Cake(PurchaseIconCanvas p)
        {
            p.Rect(22, 31, 84, 49, Ink);
            p.Rect(26, 35, 76, 42, Pink);
            p.Rect(26, 44, 76, 8, White);
            p.Ellipse(64, 79, 42, 16, White);
            for (int i = 0; i < 3; i++) p.Circle(41 + i * 23, 86, 8, Red);
            p.Line(65, 95, 65, 115, 5, Blue);
        }

        static void Dough(PurchaseIconCanvas p)
        {
            p.Ellipse(65, 39, 50, 17, White);
            p.Ellipse(64, 56, 40, 28, Gold);
            p.Ellipse(57, 65, 30, 25, White);
            p.Line(42, 72, 53, 66, 4, Gold);
            p.Line(64, 79, 74, 70, 4, Gold);
        }

        static void Batter(PurchaseIconCanvas p)
        {
            p.Poly(Blue, new(22, 71), new(106, 71), new(87, 26), new(42, 26));
            p.Ellipse(64, 72, 44, 17, Ink);
            p.Ellipse(64, 73, 39, 13, Gold);
            p.Line(74, 69, 101, 112, 7, Brown);
            p.Line(51, 76, 71, 75, 4, White);
        }

        static void Meal(PurchaseIconCanvas p)
        {
            Plate(p);
            p.Ellipse(45, 60, 17, 19, White);
            p.Ellipse(80, 60, 19, 12, Brown);
            p.Circle(70, 82, 10, Green);
            p.Circle(88, 81, 8, Green);
            p.Circle(78, 84, 5, Red);
        }

        static void BeefSoy(PurchaseIconCanvas p)
        {
            Plate(p);
            p.Ellipse(65, 59, 34, 21, Brown);
            for (int i = 0; i < 3; i++) p.Line(41 + i * 17, 48, 47 + i * 17, 73, 11, Red);
            p.Line(35, 81, 90, 76, 5, Green);
            p.Circle(43, 78, 4, Gold);
        }

        static void CornSoup(PurchaseIconCanvas p)
        {
            p.Poly(Blue, new(18, 71), new(110, 71), new(89, 25), new(40, 25));
            p.Ellipse(64, 73, 48, 23, White);
            p.Ellipse(64, 74, 41, 18, Gold);
            for (int i = 0; i < 4; i++) p.Ellipse(38 + i * 17, 70 + i % 2 * 10, 5, 4, Orange);
            p.Line(56, 76, 70, 82, 4, Green);
        }

        static void Pasta(PurchaseIconCanvas p)
        {
            Plate(p);
            for (int i = 0; i < 5; i++)
            {
                p.Line(36, 45 + i * 7, 91, 48 + i * 7, 5, Gold);
                p.Line(35 + i * 12, 44, 42 + i * 12, 80, 4, Orange);
            }
            p.Ellipse(65, 67, 21, 13, Red);
            p.Line(49, 78, 83, 61, 5, White);
            p.Ellipse(64, 86, 8, 4, Green);
        }

        static void Sandwich(PurchaseIconCanvas p)
        {
            p.Poly(Brown, new(20, 31), new(110, 31), new(78, 91));
            p.Poly(Green, new(21, 40), new(111, 40), new(77, 99));
            p.Poly(White, new(22, 48), new(112, 48), new(76, 106));
            p.Poly(Gold, new(24, 56), new(109, 56), new(76, 114));
            p.Ellipse(76, 49, 12, 7, White);
            p.Ellipse(76, 49, 6, 5, Orange);
            p.Line(34, 67, 86, 67, 3, Brown);
        }

        static void Vegetables(PurchaseIconCanvas p)
        {
            Plate(p);
            for (int i = 0; i < 4; i++)
            {
                p.Ellipse(36 + i * 18, 59 + i % 2 * 17, 12, 10, Green);
                p.Ellipse(36 + i * 18, 55 + i % 2 * 17, 6, 5, Gold);
            }
            p.Line(44, 43, 74, 75, 8, Orange);
            p.Line(60, 46, 88, 70, 7, Red);
        }
    }
}
