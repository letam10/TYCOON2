using UnityEngine;

namespace Tycoon
{
    public static partial class ItemIconAtlas
    {
        static void MilkCarton(PurchaseIconCanvas p)
        {
            p.Poly(Ink, new(33, 17), new(90, 17), new(90, 95), new(77, 110), new(45, 110), new(33, 95));
            p.Rect(37, 21, 49, 69, White);
            p.Poly(Blue, new(37, 91), new(47, 106), new(74, 106), new(86, 91));
            p.Rect(37, 28, 49, 17, Blue);
            p.Ellipse(62, 64, 13, 16, Blue);
            p.Poly(Blue, new(49, 66), new(62, 86), new(75, 66));
        }

        static void Bottle(PurchaseIconCanvas p, Color32 liquid, Color32 cap)
        {
            p.Rect(50, 95, 28, 16, Ink);
            p.Rect(52, 96, 24, 13, cap);
            p.Poly(Ink, new(31, 17), new(97, 17), new(97, 79), new(78, 94), new(50, 94), new(31, 79));
            p.Poly(liquid, new(35, 21), new(93, 21), new(93, 77), new(75, 90), new(53, 90), new(35, 77));
            p.Rect(35, 41, 58, 27, White);
            p.Circle(64, 54, 9, cap);
            p.Line(42, 72, 42, 31, 4, new Color32(255, 255, 245, 140));
        }

        static void Sack(PurchaseIconCanvas p, bool feed)
        {
            p.Poly(Ink, new(25, 19), new(101, 19), new(105, 50), new(81, 91), new(85, 111),
                new(44, 111), new(47, 91), new(22, 50));
            p.Poly(feed ? Brown : White, new(29, 23), new(97, 23), new(100, 48), new(76, 91),
                new(80, 106), new(49, 106), new(52, 91), new(27, 48));
            p.Line(47, 88, 82, 88, 6, Brown);
            p.Circle(64, 54, 19, feed ? Gold : Blue);
            if (feed)
            {
                p.Ellipse(56, 57, 5, 8, Green);
                p.Ellipse(71, 50, 5, 8, Orange);
            }
            else
            {
                p.Line(63, 41, 63, 68, 3, White);
                for (int i = 0; i < 3; i++) p.Line(55, 50 + i * 6, 72, 56 + i * 5, 3, White);
            }
        }

        static void Cheese(PurchaseIconCanvas p)
        {
            p.Poly(Ink, new(18, 30), new(106, 30), new(106, 76), new(65, 104), new(18, 73));
            p.Poly(Gold, new(22, 34), new(102, 34), new(102, 73), new(63, 98), new(22, 71));
            p.Line(22, 72, 103, 73, 4, Orange);
            p.Circle(43, 51, 9, Orange);
            p.Circle(82, 56, 7, Orange);
            p.Circle(65, 83, 6, Orange);
        }

        static void Wool(PurchaseIconCanvas p)
        {
            p.Ellipse(64, 58, 44, 30, Ink);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3;
                p.Circle(64 + Mathf.Cos(angle) * 25, 63 + Mathf.Sin(angle) * 23, 20, White);
            }
            p.Circle(64, 63, 25, White);
            p.Line(53, 55, 64, 48, 3, Gold);
        }

        static void Yarn(PurchaseIconCanvas p)
        {
            p.Circle(60, 65, 42, Ink);
            p.Circle(60, 65, 38, Gold);
            for (int i = 0; i < 5; i++) p.Line(30 + i * 11, 41, 43 + i * 10, 89, 3, Brown);
            p.Line(61, 27, 96, 22, 5, Gold);
            p.Line(96, 22, 109, 39, 5, Gold);
        }

        static void Cloth(PurchaseIconCanvas p)
        {
            p.Poly(Ink, new(20, 32), new(99, 23), new(110, 89), new(34, 105));
            p.Poly(Blue, new(24, 36), new(96, 28), new(105, 86), new(37, 100));
            p.Line(40, 38, 49, 94, 3, White);
            p.Line(76, 33, 85, 90, 3, White);
            p.Line(32, 52, 100, 43, 3, White);
            p.Line(35, 77, 102, 65, 3, White);
            p.Poly(White, new(84, 27), new(96, 28), new(94, 43));
        }
    }
}
