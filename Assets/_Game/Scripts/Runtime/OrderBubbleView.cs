using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed class OrderBubbleView
    {
        public RectTransform Rect { get; }
        public Transform Target { get; private set; }
        public float PatienceFraction => patienceFraction;
        public int LineCount { get; private set; }
        public OrderBubbleTerminalOutcome TerminalOutcome { get; private set; }
        public bool HasContent => LineCount > 0 || TerminalOutcome != OrderBubbleTerminalOutcome.None;
        readonly Image ring;
        readonly Image terminalIcon;
        readonly RectTransform[] rows = new RectTransform[2];
        readonly Image[] icons = new Image[2];
        readonly Text[] quantities = new Text[2];
        readonly string[] itemIds = new string[2];
        readonly int[] shownDelivered = { -1, -1 };
        readonly int[] shownRequested = { -1, -1 };
        int layoutCount = -1;
        float patienceFraction = 1;

        public OrderBubbleView(Transform parent)
        {
            Rect = NewRect("OrderBubble", parent, Vector2.zero, new(100, 100));
            Rect.gameObject.SetActive(false);
            var background = NewImage("Paper", Rect, Vector2.zero, new(92, 92));
            background.sprite = OrderBubbleSprites.Disc;
            background.color = new(.98f, .98f, .92f, .97f);
            var track = NewImage("PatienceTrack", Rect, Vector2.zero, new(100, 100));
            track.sprite = OrderBubbleSprites.Ring;
            track.color = new(.12f, .2f, .17f, .5f);
            ring = NewImage("Patience", Rect, Vector2.zero, new(100, 100));
            ring.sprite = OrderBubbleSprites.Ring;
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = false;
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = NewRect("OrderLine" + i, Rect, Vector2.zero, new(82, 34));
                icons[i] = NewImage("ItemIcon", rows[i], new(-19, 0), new(34, 34));
                icons[i].preserveAspect = true;
                var textRect = NewRect("DeliveredRequested", rows[i], new(21, 0), new(44, 30));
                var label = textRect.gameObject.AddComponent<Text>();
                label.font = Art.Catalog && Art.Catalog.font
                    ? Art.Catalog.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 19;
                label.resizeTextForBestFit = false;
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.supportRichText = false;
                label.raycastTarget = false;
                label.color = new(.12f, .24f, .19f);
                quantities[i] = label;
            }
            terminalIcon = NewImage("TerminalOutcome", Rect, Vector2.zero, new(72, 72));
            terminalIcon.preserveAspect = true;
            terminalIcon.gameObject.SetActive(false);
        }

        public void Bind(Transform target)
        {
            Reset();
            Target = target;
        }

        public void SetLines(IReadOnlyList<OrderLine> lines, float patienceFraction)
        {
            int count = Mathf.Min(2, lines?.Count ?? 0);
            LayoutRows(count);
            for (int i = 0; i < count; i++)
                SetRow(i, lines[i].id, lines[i].delivered, lines[i].requested);
            SetPatience(patienceFraction);
        }

        public void SetSingle(string id, int delivered, int requested, float patienceFraction)
        {
            LayoutRows(1);
            SetRow(0, id, delivered, requested);
            SetPatience(patienceFraction);
        }

        void LayoutRows(int count)
        {
            LineCount = count;
            if (layoutCount == count) return;
            layoutCount = count;
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].gameObject.SetActive(i < count);
                rows[i].anchoredPosition = new(0, count == 1 ? 0 : i == 0 ? 17 : -17);
            }
        }

        void SetRow(int row, string item, int delivered, int requested)
        {
            if (itemIds[row] != item)
            {
                itemIds[row] = item;
                icons[row].sprite = ItemIconAtlas.Get(item);
            }
            if (shownDelivered[row] == delivered && shownRequested[row] == requested) return;
            shownDelivered[row] = delivered;
            shownRequested[row] = requested;
            quantities[row].text = delivered + "/" + requested;
        }

        void SetPatience(float fraction)
        {
            patienceFraction = Mathf.Clamp01(fraction);
            // Mỗi bước dưới một pixel ở cỡ UI chuẩn; tránh dựng lại mesh viền ở từng frame.
            ring.fillAmount = Mathf.Round(patienceFraction * 512) / 512;
            ring.color = PatienceColor(patienceFraction);
        }

        public static Color PatienceColor(float fraction) => fraction > .5f
            ? new(.23f, .7f, .34f) : fraction > .25f ? new(.95f, .7f, .15f) : new(.88f, .24f, .22f);

        public void ShowTerminal(bool completed)
        {
            LayoutRows(0);
            TerminalOutcome = completed
                ? OrderBubbleTerminalOutcome.Completed : OrderBubbleTerminalOutcome.Disappointed;
            terminalIcon.sprite = OrderBubbleTerminalSprites.Get(completed);
            terminalIcon.gameObject.SetActive(true);
            SetPatience(completed ? 1 : 0);
            ring.fillAmount = 1;
        }

        public void Reset()
        {
            layoutCount = -1;
            Target = null;
            LineCount = 0;
            TerminalOutcome = OrderBubbleTerminalOutcome.None;
            terminalIcon.sprite = null;
            terminalIcon.gameObject.SetActive(false);
            Rect.gameObject.SetActive(false);
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].gameObject.SetActive(false);
                icons[i].sprite = null;
                quantities[i].text = "";
                itemIds[i] = null;
                shownDelivered[i] = -1;
                shownRequested[i] = -1;
            }
            SetPatience(1);
        }

        static RectTransform NewRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static Image NewImage(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var image = NewRect(name, parent, position, size).gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }
    }
}
