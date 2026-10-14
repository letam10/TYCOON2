using UnityEngine;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed partial class GameHud
    {
        static void SettingsRowRect(RectTransform rect, int row, float left = .51f, float right = .96f)
        {
            float top = .80f - row * .082f;
            Rect(rect, new(left, top - .06f), new(right, top), Vector2.zero, Vector2.zero);
        }

        void SettingsRowLabel(Transform board, string label, int row)
        {
            var text = Text("SettingLabel", board, label, 23, TextAnchor.MiddleLeft, Ink);
            SettingsRowRect(text.rectTransform, row, .04f, .49f);
        }

        Dropdown SettingsDropdown(Transform board, string name, string label, int row, string[] options)
        {
            SettingsRowLabel(board, label, row);
            var root = Panel(name, board, Card);
            SettingsRowRect((RectTransform)root.transform, row);
            var dropdown = root.AddComponent<Dropdown>();
            dropdown.targetGraphic = root.GetComponent<Image>();
            dropdown.targetGraphic.raycastTarget = true;
            var caption = Text("Caption", root.transform, "", 22, TextAnchor.MiddleLeft, Ink);
            Rect(caption.rectTransform, Vector2.zero, Vector2.one, new(12, 0), new(-42, 0));
            var arrow = Text("Arrow", root.transform, "▼", 18, TextAnchor.MiddleCenter, Accent);
            Rect(arrow.rectTransform, new(1, 0), Vector2.one, new(-38, 0), Vector2.zero);
            var template = Panel("Template", root.transform, Surface);
            var templateRect = (RectTransform)template.transform;
            templateRect.pivot = new(.5f, 1);
            Rect(templateRect, Vector2.zero, new(1, 0), new(0, -240), Vector2.zero);
            var viewport = Panel("Viewport", template.transform, Card);
            var viewportRect = (RectTransform)viewport.transform;
            Inset(viewportRect, 4);
            viewport.AddComponent<RectMask2D>();
            var content = NewRect("Content", viewport.transform);
            content.pivot = new(.5f, 1);
            Rect(content, new(0, 1), Vector2.one, new(0, -44), Vector2.zero);
            var item = Panel("Item", content, Card);
            Rect((RectTransform)item.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var toggle = item.AddComponent<Toggle>();
            toggle.targetGraphic = item.GetComponent<Image>();
            toggle.targetGraphic.raycastTarget = true;
            var check = Text("Checkmark", item.transform, "✓", 20, TextAnchor.MiddleCenter, Accent);
            Rect(check.rectTransform, Vector2.zero, new(0, 1), Vector2.zero, new(30, 0));
            toggle.graphic = check;
            var itemText = Text("ItemLabel", item.transform, "", 22, TextAnchor.MiddleLeft, Ink);
            Rect(itemText.rectTransform, Vector2.zero, Vector2.one, new(34, 0), new(-8, 0));
            var scroll = template.AddComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35;
            dropdown.template = templateRect;
            dropdown.captionText = caption;
            dropdown.itemText = itemText;
            dropdown.AddOptions(new System.Collections.Generic.List<string>(options));
            template.SetActive(false);
            return dropdown;
        }

        Toggle SettingsToggle(Transform board, string name, string label, int row)
        {
            SettingsRowLabel(board, label, row);
            var root = Panel(name, board, Card);
            SettingsRowRect((RectTransform)root.transform, row);
            var toggle = root.AddComponent<Toggle>();
            toggle.targetGraphic = root.GetComponent<Image>();
            toggle.targetGraphic.raycastTarget = true;
            var box = Panel("ToggleBox", root.transform, "#102C27");
            Rect((RectTransform)box.transform, new(0, .5f), new(0, .5f), new(14, -17), new(48, 17));
            var check = Text("Checkmark", box.transform, "✓", 25, TextAnchor.MiddleCenter, Accent);
            Inset(check.rectTransform, 0);
            toggle.graphic = check;
            var text = Text("ToggleLabel", root.transform, "Bật / tắt", 22, TextAnchor.MiddleLeft, Ink);
            Rect(text.rectTransform, Vector2.zero, Vector2.one, new(62, 0), new(-10, 0));
            return toggle;
        }

        Slider SettingsSlider(Transform board, string name, int row)
        {
            var root = NewRect(name, board);
            SettingsRowRect(root, row, .51f, .86f);
            var track = Panel("Track", root, "#102C27");
            Rect((RectTransform)track.transform, new(0, .4f), new(1, .6f), Vector2.zero, Vector2.zero);
            var fill = Panel("Fill", track.transform, Accent);
            Rect((RectTransform)fill.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var handleArea = NewRect("HandleArea", root);
            Inset(handleArea, 12);
            var handle = Panel("Handle", handleArea, Ink);
            var handleRect = (RectTransform)handle.transform;
            handleRect.sizeDelta = new(22, 0);
            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = (RectTransform)fill.transform;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.targetGraphic.raycastTarget = true;
            slider.minValue = .5f;
            slider.maxValue = 1.5f;
            slider.value = 1;
            return slider;
        }
    }
}
