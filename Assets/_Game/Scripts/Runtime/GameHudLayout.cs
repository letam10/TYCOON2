using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace Tycoon
{
    public sealed partial class GameHud : MonoBehaviour
    {
        void LayoutHud()
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            lastSafeArea = Screen.safeArea;
            // Mọi mép HUD nằm trong vùng an toàn, kể cả khi đổi tỷ lệ cửa sổ.
            safeArea.anchorMin = new(lastSafeArea.xMin / Mathf.Max(1, screenWidth), lastSafeArea.yMin / Mathf.Max(1,
                screenHeight));
            safeArea.anchorMax = new(lastSafeArea.xMax / Mathf.Max(1, screenWidth), lastSafeArea.yMax / Mathf.Max(1,
                screenHeight));
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            float scale = Mathf.Max(.01f, canvas.scaleFactor);
            lastCanvasScale = scale;
            float width = lastSafeArea.width / scale, height = lastSafeArea.height / scale;
            bool compact = width < 1460;
            TopLeft(cashRect, 16, 16, Mathf.Min(340, width - 264), 66);
            BottomLeft(carryRect, 16, compact ? 132 : 44, 284, 74);
            BottomCenter(actionRect, 44, Mathf.Min(560, width - 32), 84);
            Rect(help.rectTransform, Vector2.zero, new(1, 0), new(16, 6), new(-16, 30));
            // Bảng lớn chỉ nằm trong modal; HUD chơi thường giữ diện tích che rất nhỏ.
            int[] buttonWidths =
            {
                76, 96, 104, 76, 98
            };
            bool wrapButtons = width < 850;
            float dockWidth = wrapButtons ? 224 : quickActions.Select((button, i) => buttonWidths[i]).Sum()
                + (quickActions.Count - 1) * 8;
            float dockHeight = wrapButtons ? Mathf.Ceil(quickActions.Count / 2f) * 44 - 8 : 36;
            TopRight(dockRect, 16, 16, dockWidth, dockHeight);
            float x = 0;
            for (int i = 0; i < quickActions.Count; i++)
            {
                float left = wrapButtons ? i % 2 * 116 : x;
                float top = wrapButtons ? i / 2 * 44 : 0;
                float buttonWidth = wrapButtons ? 108 : buttonWidths[i];
                Rect((RectTransform)quickActions[i].transform, new(0, 1), new(0, 1),
                new(left, -top - 36), new(left + buttonWidth, -top));
                x += buttonWidth + 8;
            }
            BottomCenter((RectTransform)toastRoot.transform, compact ? 266 : 184, Mathf.Min(500, width - 32), 58);
            BottomCenter((RectTransform)recipeButton.transform, compact ? 222 : 140, 140, 36);
            var recipeLabel = recipeButton.GetComponentInChildren<Text>();
            recipeLabel.fontSize = 19;
            Inset(recipeLabel.rectTransform, 3);
            if (compact)
            {
                Rect(titleRect, new(.04f, .73f), new(.96f, .84f), Vector2.zero, Vector2.zero);
                Rect(objectiveRect, new(.04f, .43f), new(.96f, .71f), Vector2.zero, Vector2.zero);
                Rect(financeRect, new(.04f, .19f), new(.47f, .41f), Vector2.zero, Vector2.zero);
                Rect(stockRect, new(.53f, .19f), new(.96f, .41f), Vector2.zero, Vector2.zero);
            }
            else
            {
                Rect(titleRect, new(.04f, .68f), new(.47f, .84f), Vector2.zero, Vector2.zero);
                Rect(objectiveRect, new(.04f, .19f), new(.47f, .66f), Vector2.zero, Vector2.zero);
                Rect(financeRect, new(.53f, .62f), new(.96f, .84f), Vector2.zero, Vector2.zero);
                Rect(stockRect, new(.53f, .19f), new(.96f, .60f), Vector2.zero, Vector2.zero);
            }
            LayoutModMenu();
            var pauseCard = menu.transform.Find("PauseCard") as RectTransform;
            pauseCard.localScale = Vector3.one * Mathf.Min(1, (height - 48) / 948, (width - 48) / 640);
        }
    }
}
