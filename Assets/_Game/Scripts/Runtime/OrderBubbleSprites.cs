using UnityEngine;

namespace Tycoon
{
    static class OrderBubbleSprites
    {
        static Sprite disc;
        static Sprite ring;
        public static Sprite Disc => disc ? disc : disc = Create(false);
        public static Sprite Ring => ring ? ring : ring = Create(true);

        static Sprite Create(bool hollow)
        {
            const int size = 128;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float radius = Vector2.Distance(new(x + .5f, y + .5f), new(64, 64));
                    float alpha = Mathf.Clamp01(63 - radius);
                    if (hollow) alpha *= Mathf.Clamp01(radius - 56);
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = hollow ? "OrderPatienceRing" : "OrderBubbleDisc";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f);
        }
    }
}
