using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Tycoon
{
    public sealed partial class GameHud : MonoBehaviour
    {
        GameObject Modal(string name)
        {
            var root = Panel(name, safeArea, "#071C19", .92f);
            Rect((RectTransform)root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.GetComponent<Image>().raycastTarget = true;
            return root;
        }
        GameObject ManagementBoard(Transform parent, string title, string subtitle)
        {
            var board = Panel("ManagementBoard", parent, Surface);
            Rect((RectTransform)board.transform, new(.06f, .08f), new(.94f, .92f), Vector2.zero, Vector2.zero);
            var heading = Text("Heading", board.transform, title, 34, TextAnchor.MiddleCenter, Ink);
            Rect(heading.rectTransform, new(0, 1), new(1, 1), new(26, -80), new(-26, -20));
            var subheading = Text("Subtitle", board.transform, subtitle, 21, TextAnchor.MiddleCenter, Muted);
            Rect(subheading.rectTransform, new(0, 1), new(1, 1), new(26, -112), new(-26, -76));
            return board;
        }
        Transform ScrollContent(string name, Transform board)
        {
            var viewport = Panel(name + "Viewport", board, "#102F29");
            Rect((RectTransform)viewport.transform, Vector2.zero, Vector2.one, new(24, 114), new(-24, -172));
            viewport.AddComponent<RectMask2D>();
            viewport.GetComponent<Image>().raycastTarget = true;
            var content = NewRect(name + "Rows", viewport.transform);
            content.anchorMin = new(0, 1);
            content.anchorMax = new(1, 1);
            content.pivot = new(.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var scroll = viewport.AddComponent<ScrollRect>();
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;
            return content;
        }
        Button FooterButton(Transform board, string label, UnityEngine.Events.UnityAction click)
        {
            var button = Button(board, label, click);
            Rect((RectTransform)button.transform, new(.5f, 0), new(.5f, 0), new(-190, 24), new(190, 88));
            return button;
        }
        Button PauseButton(Transform parent, string label, float y, UnityEngine.Events.UnityAction click,
            bool accent = false)
        {
            var button = Button(parent, label, click, accent);
            Rect((RectTransform)button.transform, new(0, 1), new(1, 1), new(40, y - 36), new(-40, y + 36));
            return button;
        }
        GameObject Panel(string name, Transform parent, string color, float alpha = 1)
        {
            var rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            var tint = Art.Hex(color);
            tint.a = alpha;
            image.color = tint;
            image.raycastTarget = false;
            if (!rounded)
            {
                roundedTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                roundedTexture.name = "HudRoundedCorners";
                roundedTexture.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                {
                    float dx = Mathf.Max(11 - x, x - 20, 0), dy = Mathf.Max(11 - y, y - 20, 0);
                    roundedTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(11.5f - Mathf.Sqrt(dx * dx
                        + dy * dy))));
                }
                roundedTexture.Apply(false, true);
                rounded = Sprite.Create(roundedTexture, new UnityEngine.Rect(0, 0, 32, 32), Vector2.one * .5f,
                100, 0, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
                rounded.name = "HudRoundedCorners";
            }
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            return rect.gameObject;
        }
        static Text Text(string name, Transform parent, string value, int size, TextAnchor alignment, string color)
        {
            var root = NewRect(name, parent);
            var text = root.gameObject.AddComponent<Text>();
            text.font = Art.Catalog.font;
            text.fontSize = size;
            text.resizeTextForBestFit = false;
            text.supportRichText = false;
            text.text = value;
            text.alignment = alignment;
            text.color = Art.Hex(color);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        Button Button(Transform parent, string label, UnityEngine.Events.UnityAction click, bool accent = false)
        {
            var root = Panel(label, parent, accent ? Accent : "#356754");
            var button = root.AddComponent<Button>();
            StyleButton(button);
            if (click != null) button.onClick.AddListener(click);
            var text = Text("Label", root.transform, label, 24, TextAnchor.MiddleCenter, accent ? Surface : Ink);
            Inset(text.rectTransform, 10);
            return button;
        }
        static void StyleButton(Button button)
        {
            button.targetGraphic = button.GetComponent<Image>();
            button.targetGraphic.raycastTarget = true;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new(1.12f, 1.12f, 1.02f);
            colors.selectedColor = new(1.12f, 1.12f, 1.02f);
            colors.pressedColor = new(.78f, .9f, .76f);
            colors.disabledColor = new(.55f, .6f, .56f, .6f);
            colors.fadeDuration = .1f;
            button.colors = colors;
            button.gameObject.AddComponent<HudButtonFocus>().Target = button;
        }
        static void Select(Button button)
        {
            if (button && EventSystem.current) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
        static void SetText(Text label, string value)
        {
            if (label.text != value) label.text = value;
        }
        static RectTransform NewRect(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            return (RectTransform)root.transform;
        }
        static void Inset(RectTransform rect, float amount) => Rect(rect, Vector2.zero, Vector2.one, new(amount,
            amount), new(-amount, -amount));
        static void TopLeft(RectTransform rect, float x, float y, float width, float height) => Rect(rect, new(0,
            1), new(0, 1), new(x, -y - height), new(x + width, -y));
        static void TopRight(RectTransform rect, float x, float y, float width, float height) => Rect(rect,
            Vector2.one, Vector2.one, new(-x - width, -y - height), new(-x, -y));
        static void BottomLeft(RectTransform rect, float x, float y, float width, float height) => Rect(rect,
            Vector2.zero, Vector2.zero, new(x, y), new(x + width, y + height));
        static void BottomRight(RectTransform rect, float x, float y, float width, float height) => Rect(rect,
            new(1, 0), new(1, 0), new(-x - width, y), new(-x, y + height));
        static void BottomCenter(RectTransform rect, float y, float width, float height) => Rect(rect, new(.5f, 0),
            new(.5f, 0), new(-width * .5f, y), new(width * .5f, y + height));
        static void Rect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
