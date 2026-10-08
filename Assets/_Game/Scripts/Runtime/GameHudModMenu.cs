using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Tycoon
{
    public sealed partial class GameHud
    {
        GameObject modOverlay;
        RectTransform modCard;
        Button modCash;
        bool modOpen;
        public bool IsModMenuOpen => modOpen;
        void BuildModMenu()
        {
            modOverlay = Panel("ModOutsideClick", safeArea, "#071C19", .12f);
            Rect((RectTransform)modOverlay.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var outside = modOverlay.AddComponent<Button>();
            outside.targetGraphic = modOverlay.GetComponent<Image>();
            outside.targetGraphic.raycastTarget = true;
            outside.transition = Selectable.Transition.None;
            outside.navigation = new UnityEngine.UI.Navigation
            {
                mode = UnityEngine.UI.Navigation.Mode.None
            };
            outside.onClick.AddListener(CloseModMenu);
            modCard = (RectTransform)Panel("ModCommands", modOverlay.transform, Surface).transform;
            modCard.GetComponent<Image>().raycastTarget = true;
            modCash = Button(modCard, "Mod tiền", () => ApplyMod(false));
            var maximum = Button(modCard, "Mod xây dựng tối đa", () => ApplyMod(true));
            modCash.navigation = new UnityEngine.UI.Navigation
            {
                mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnUp = maximum,
                selectOnDown = maximum
            };
            maximum.navigation = new UnityEngine.UI.Navigation
            {
                mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnUp = modCash,
                selectOnDown = modCash
            };
            Rect((RectTransform)modCash.transform, new(0, .5f), Vector2.one, new(8, 4), new(-8, -8));
            Rect((RectTransform)maximum.transform, Vector2.zero, new(1, .5f), new(8, 8), new(-8, -4));
            foreach (var label in modCard.GetComponentsInChildren<Text>())
            {
                label.fontSize = 18;
                label.resizeTextForBestFit = false;
            }
            modOverlay.SetActive(false);
        }
        void LayoutModMenu()
        {
            if (!modCard) return;
            float top = dockRect.offsetMin.y - 8;
            Rect(modCard, Vector2.one, Vector2.one, new(-304, top - 112), new(-16, top));
        }
        void ToggleModMenu()
        {
            if (modOpen)
            {
                CloseModMenu();
                return;
            }
            if (!DevelopmentAssistance.Enabled || !game.CanSimulate || mode != ScreenMode.Play) return;
            modOpen = true;
            game.Player.StopInteraction();
            game.Player.CanControl = false;
            modOverlay.SetActive(true);
            modOverlay.transform.SetAsLastSibling();
            LayoutModMenu();
            Select(modCash);
        }
        void CloseModMenu()
        {
            modOpen = false;
            if (modOverlay) modOverlay.SetActive(false);
            game.Player.StopInteraction();
            game.Player.CanControl = AllowsPlayerControl && game.CanSimulate;
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }
        void ApplyMod(bool maximum)
        {
            // Đóng trước khi gửi lệnh: CanSimulate có thể kiểm tra trạng thái menu.
            CloseModMenu();
            if (!game.CanSimulate) return;
            bool changed = maximum
            ? DevelopmentAssistance.ApplyMaximum(game)
            : DevelopmentAssistance.ApplyCashOnly(game);
            if (!changed)
            {
                game.Say(game.Transactions?.LastReason ?? "Chưa áp dụng được mod.");
                return;
            }
            game.SaveGame();
            // Giữ nguyên thông báo lỗi nếu lưu thất bại.
            if (game.Toast == "Đã lưu trò chơi")
            game.Say(maximum ? "Đã nâng tối đa và lưu trò chơi." : "Đã cộng 999.999 xu vào két và lưu.");
            RefreshPlayingHud();
            RefreshStatus();
        }
    }
}
