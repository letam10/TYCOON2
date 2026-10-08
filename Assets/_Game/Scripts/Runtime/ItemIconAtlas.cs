using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static partial class ItemIconAtlas
    {
        static readonly Dictionary<string, Sprite> cache = new();
        static readonly Color32 Ink = new(63, 64, 46, 255);
        static readonly Color32 White = new(251, 248, 229, 255);
        static readonly Color32 Gold = new(242, 197, 70, 255);
        static readonly Color32 Green = new(87, 144, 66, 255);
        static readonly Color32 Brown = new(147, 85, 43, 255);
        static readonly Color32 Red = new(226, 74, 49, 255);
        static readonly Color32 Orange = new(244, 133, 42, 255);
        static readonly Color32 Blue = new(84, 159, 185, 255);
        static readonly Color32 Pink = new(233, 151, 178, 255);

        public static Sprite Get(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || Definitions.Item(itemId) == null) return null;
            if (cache.TryGetValue(itemId, out var existing) && existing) return existing;
            const int size = DrawnPurchaseIcons.Size;
            var pixels = new Color32[size * size];
            var pen = new PurchaseIconCanvas(pixels, 0, 0, 2);
            Draw(pen, itemId);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "ItemIcon_" + itemId;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f);
            sprite.name = texture.name;
            cache[itemId] = sprite;
            return sprite;
        }

        static void Draw(PurchaseIconCanvas p, string id)
        {
            switch (id)
            {
                case "carrot":
                    Carrot(p);
                    break;
                case "tomato":
                    Tomato(p);
                    break;
                case "wheat":
                    Wheat(p);
                    break;
                case "corn":
                    Corn(p);
                    break;
                case "soybean":
                    Soybean(p);
                    break;
                case "milk":
                    MilkCarton(p);
                    break;
                case "egg":
                    Egg(p);
                    break;
                case "flour":
                    Sack(p, false);
                    break;
                case "cheese":
                    Cheese(p);
                    break;
                case "sauce":
                    Bottle(p, Red, Red);
                    break;
                case "bread":
                    Bread(p);
                    break;
                case "cake":
                    Cake(p);
                    break;
                case "meal":
                    Meal(p);
                    break;
                case "beef":
                    Beef(p);
                    break;
                case "animal_feed":
                    Sack(p, true);
                    break;
                case "soy_sauce":
                    Bottle(p, Brown, Gold);
                    break;
                case "bottled_milk":
                    Bottle(p, White, Blue);
                    break;
                case "wool":
                    Wool(p);
                    break;
                case "yarn":
                    Yarn(p);
                    break;
                case "cloth":
                    Cloth(p);
                    break;
                case "bread_dough":
                    Dough(p);
                    break;
                case "cake_batter":
                    Batter(p);
                    break;
                case "beef_soy":
                    BeefSoy(p);
                    break;
                case "corn_soup":
                    CornSoup(p);
                    break;
                case "pasta":
                    Pasta(p);
                    break;
                case "egg_sandwich":
                    Sandwich(p);
                    break;
                case "soy_vegetables":
                    Vegetables(p);
                    break;
            }
        }
    }
}
