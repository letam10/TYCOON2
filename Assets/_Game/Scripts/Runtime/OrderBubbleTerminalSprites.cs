using UnityEngine;

namespace Tycoon
{
    public enum OrderBubbleTerminalOutcome { None, Completed, Disappointed }

    static class OrderBubbleTerminalSprites
    {
        static Sprite completed;
        static Sprite disappointed;

        public static Sprite Get(bool success)
        {
            if (success) return completed ? completed : completed = Draw(true);
            return disappointed ? disappointed : disappointed = Draw(false);
        }

        static Sprite Draw(bool success)
        {
            const int size = DrawnPurchaseIcons.Size;
            var pixels = new Color32[size * size];
            var pen = new PurchaseIconCanvas(pixels, 0, 0, 2);
            Color32 ink = new(52, 68, 48, 255);
            if (success)
            {
                Color32 green = new(58, 176, 86, 255);
                pen.Line(28, 64, 53, 39, 17, ink);
                pen.Line(53, 39, 102, 94, 17, ink);
                pen.Line(28, 64, 53, 39, 11, green);
                pen.Line(53, 39, 102, 94, 11, green);
            }
            else
            {
                pen.Circle(64, 64, 46, ink);
                pen.Circle(64, 64, 42, new Color32(243, 180, 67, 255));
                pen.Circle(48, 77, 5, ink);
                pen.Circle(80, 77, 5, ink);
                pen.Line(43, 43, 53, 51, 5, ink);
                pen.Line(53, 51, 64, 54, 5, ink);
                pen.Line(64, 54, 75, 51, 5, ink);
                pen.Line(75, 51, 85, 43, 5, ink);
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = success ? "OrderCompletedCheck" : "OrderDisappointedFace";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f);
        }
    }
}
