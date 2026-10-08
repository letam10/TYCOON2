using UnityEngine;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed partial class GameHud : MonoBehaviour
    {
        void BuildPlayingHud()
        {
            var root = hud.transform;
            cashRect = (RectTransform)Panel("Cash", root, Surface).transform;
            var handLabel = Text("HandLabel", cashRect, "TIỀN TRÊN TAY", 13, TextAnchor.MiddleLeft, Muted);
            Rect(handLabel.rectTransform, new(0, 1), new(.5f, 1), new(12, -26), new(-8, -5));
            var safeLabel = Text("SafeLabel", cashRect, "TIỀN TRONG KÉT", 13, TextAnchor.MiddleLeft, Muted);
            Rect(safeLabel.rectTransform, new(.5f, 1), Vector2.one, new(12, -26), new(-8, -5));
            cashInHand = Text("CashInHand", cashRect, "0", 24, TextAnchor.MiddleLeft, Ink);
            Rect(cashInHand.rectTransform, Vector2.zero, new(.5f, 1), new(12, 7), new(-8, -25));
            cashInSafe = Text("CashInSafe", cashRect, "0", 24, TextAnchor.MiddleLeft, Accent);
            Rect(cashInSafe.rectTransform, new(.5f, 0), Vector2.one, new(12, 7), new(-8, -25));
            foreach (var cash in new[]
            {
                cashInHand, cashInSafe
            }
            )
            {
                cash.resizeTextForBestFit = true;
                cash.resizeTextMinSize = 14;
                cash.resizeTextMaxSize = 24;
            }
            titleRect = (RectTransform)Panel("Title", root, Surface).transform;
            var title = Text("Brand", titleRect, "TYCOON2", 18, TextAnchor.MiddleLeft, Accent);
            Rect(title.rectTransform, new(0, 1), new(1, 1), new(18, -35), new(-16, -8));
            progress = Text("Progress", titleRect, "", 22, TextAnchor.MiddleLeft, Ink);
            Rect(progress.rectTransform, Vector2.zero, Vector2.one, new(18, 10), new(-16, -37));
            objectiveRect = (RectTransform)Panel("Objective", root, Surface, .93f).transform;
            var objectiveTitle = Text("ObjectiveTitle", objectiveRect, "BƯỚC TIẾP THEO", 16, TextAnchor.MiddleLeft,
                Accent);
            Rect(objectiveTitle.rectTransform, new(0, 1), new(1, 1), new(18, -34), new(-16, -8));
            objective = Text("ObjectiveText", objectiveRect, "", 22, TextAnchor.UpperLeft, Ink);
            Rect(objective.rectTransform, Vector2.zero, Vector2.one, new(18, 14), new(-16, -42));
            objective.resizeTextForBestFit = true;
            objective.resizeTextMinSize = 18;
            objective.resizeTextMaxSize = 22;
            financeRect = (RectTransform)Panel("Finance", root, Surface, .9f).transform;
            finance = Text("FinanceText", financeRect, "", 21, TextAnchor.MiddleLeft, Muted);
            Inset(finance.rectTransform, 18);
            stockRect = (RectTransform)Panel("AreaStock", root, Surface, .9f).transform;
            stock = Text("StockText", stockRect, "", 20, TextAnchor.UpperLeft, Ink);
            Inset(stock.rectTransform, 18);
            carryRect = (RectTransform)Panel("Carry", root, Surface).transform;
            carry = Text("CarryText", carryRect, "", 20, TextAnchor.MiddleLeft, Ink);
            Rect(carry.rectTransform, Vector2.zero, Vector2.one, new(12, 14), new(-12, -4));
            carry.resizeTextForBestFit = true;
            carry.resizeTextMinSize = 18;
            carry.resizeTextMaxSize = 20;
            var capacity = Panel("CarryCapacity", carryRect, "#102C27");
            Rect((RectTransform)capacity.transform, Vector2.zero, new(1, 0), new(12, 6), new(-12, 10));
            carryFill = Panel("CarryFill", capacity.transform, Accent).GetComponent<Image>();
            Rect(carryFill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            actionRect = (RectTransform)Panel("Action", root, Surface, .96f).transform;
            var actionTitle = Text("ActionTitle", actionRect, "ĐỨNG GẦN • TỰ ĐỘNG", 13, TextAnchor.MiddleCenter,
                Accent);
            Rect(actionTitle.rectTransform, new(0, 1), new(1, 1), new(12, -22), new(-12, -4));
            prompt = Text("ActionText", actionRect, "", 20, TextAnchor.MiddleCenter, Ink);
            Rect(prompt.rectTransform, Vector2.zero, Vector2.one, new(12, 6), new(-12, -24));
            prompt.resizeTextForBestFit = true;
            prompt.resizeTextMinSize = 18;
            prompt.resizeTextMaxSize = 20;
            recipeButton = Button(root, "Đổi món", CycleRecipe, true);
            recipeButton.gameObject.SetActive(false);
            dockRect = NewRect("QuickActions", root);
            AddQuickAction("Xem kho / chọn hàng", "Kho", OpenStock);
            AddQuickAction("Vận chuyển xe tải", "Xe tải", OpenCargo);
            AddQuickAction("Thông tin", "Thông tin", OpenInformation);
            AddQuickAction("Menu", "Menu", TogglePause);
            if (DevelopmentAssistance.Enabled)
            AddQuickAction("Mod game", "Mod game", ToggleModMenu);
            help = Text("Help", root, "", 14, TextAnchor.MiddleCenter, Ink);
            toastRoot = Panel("Toast", root, "#3C5936");
            toast = Text("ToastText", toastRoot.transform, "", 18, TextAnchor.MiddleCenter, Ink);
            Inset(toast.rectTransform, 8);
            toast.supportRichText = false;
            toast.resizeTextForBestFit = false;
            toastRoot.SetActive(false);
        }
        void AddQuickAction(string name, string label, UnityEngine.Events.UnityAction click)
        {
            var button = Button(dockRect, name, click);
            var text = button.GetComponentInChildren<Text>();
            text.text = label;
            text.fontSize = 19;
            if(name=="Mod game")
            {
                text.resizeTextForBestFit=true;
                text.resizeTextMinSize=14;
                text.resizeTextMaxSize=19;
                text.horizontalOverflow=HorizontalWrapMode.Overflow;
            }
            Inset(text.rectTransform, 3);
            quickActions.Add(button);
        }
        void BuildInformation()
        {
            informationPanel = Modal("BusinessInformation");
            var board = ManagementBoard(informationPanel.transform, "TỔNG QUAN KINH DOANH",
                "Mục tiêu, doanh thu và kho gần bạn");
            foreach (var panel in new[]
            {
                titleRect, objectiveRect, financeRect, stockRect
            }
            ) panel.SetParent(board.transform, false);
            informationClose = FooterButton(board.transform, "Đóng", () => ShowScreen(ScreenMode.Play));
        }
        void OpenInformation()
        {
            if (!game.CanSimulate) return;
            ShowScreen(ScreenMode.Information);
            RefreshStatus();
            Select(informationClose);
        }
        void BuildPause()
        {
            menu = Modal("Pause");
            var card = Panel("PauseCard", menu.transform, Surface);
            Rect((RectTransform)card.transform, new(.5f, .5f), new(.5f, .5f), new(-320, -474), new(320, 474));
            var heading = Text("PauseHeading", card.transform, "TYCOON2", 48, TextAnchor.MiddleCenter, Ink);
            Rect(heading.rectTransform, new(0, 1), new(1, 1), new(20, -108), new(-20, -35));
            var subtitle = Text("PauseSubtitle", card.transform, "Từ nông trại đến đế chế kinh doanh", 22,
                TextAnchor.MiddleCenter, Muted);
            Rect(subtitle.rectTransform, new(0, 1), new(1, 1), new(26, -160), new(-26, -108));
            continueButton = PauseButton(card.transform, "Tiếp tục", -210, TogglePause, true);
            PauseButton(card.transform, "Thông tin kinh doanh", -298, OpenInformation);
            PauseButton(card.transform, "Kho • chọn hàng", -386, OpenStock);
            PauseButton(card.transform, "Vận chuyển xe tải", -474, OpenCargo);
            PauseButton(card.transform, "Quản lý đội", -562, OpenCrewMenu);
            PauseButton(card.transform, "Lưu trò chơi", -650, () => game.SaveGame());
            PauseButton(card.transform, "Lưu & thoát", -738, () =>
            {
                game.SaveGame();
                Application.Quit();
            }
            );
            var note = Text("PauseNote", card.transform, "Trò chơi đang tạm dừng.\nEsc / nút Back để tiếp tục.", 20,
                TextAnchor.MiddleCenter, Muted);
            Rect(note.rectTransform, Vector2.zero, new(1, 0), new(24, 28), new(-24, 112));
        }
        void BuildCrew()
        {
            crewPanel = Modal("CrewManagement");
            var board = ManagementBoard(crewPanel.transform, "ĐỘI NGŨ", "Vai trò, khu vực và nâng cấp cho từng đội");
            crewSummary = Text("CrewSummary", board.transform, "", 21, TextAnchor.MiddleCenter, Muted);
            Rect(crewSummary.rectTransform, new(0, 1), new(1, 1), new(30, -155), new(-30, -115));
            crewContent = ScrollContent("Crew", board.transform);
            crewBack = FooterButton(board.transform, "Quay lại", BackToPauseMenu);
        }
        void BuildStock()
        {
            stockPanel = Modal("StockDetails");
            var board = ManagementBoard(stockPanel.transform, "KHO HÀNG",
                "Chọn mặt hàng muốn lấy • giữ chỗ và hàng đang đến được cập nhật riêng");
            stockContent = ScrollContent("Stock", board.transform);
            stockClose = FooterButton(board.transform, "Đóng", CloseStock);
        }
        void BuildCargo()
        {
            cargoPanel = Modal("CargoRoutes");
            var board = ManagementBoard(cargoPanel.transform, "VẬN CHUYỂN XE TẢI",
                "Chọn hai kho khác nhau để điều phối tuyến hàng");
            cargoText = Text("CargoSummary", board.transform, "", 24, TextAnchor.MiddleLeft, Ink);
            Rect(cargoText.rectTransform, new(.04f, .64f), new(.96f, .8f), Vector2.zero, Vector2.zero);
            var source = Button(board.transform, "Đổi kho nguồn", () => CycleCargo(true));
            Rect((RectTransform)source.transform, new(.05f, .51f), new(.48f, .61f), Vector2.zero, Vector2.zero);
            var destination = Button(board.transform, "Đổi kho đích", () => CycleCargo(false));
            Rect((RectTransform)destination.transform, new(.52f, .51f), new(.95f, .61f), Vector2.zero, Vector2.zero);
            var dispatch = Button(board.transform, "Gửi xe", () =>
            {
                if (game.Logistics != null && !game.Logistics.Dispatch()) game.Say(game.Logistics.Reason);
            },
            true);
            Rect((RectTransform)dispatch.transform, new(.2f, .36f), new(.8f, .46f), Vector2.zero, Vector2.zero);
            var repeat = Button(board.transform, "Bật / tắt tuyến tự động", () =>
            {
                var truck = game.Transactions?.View.truck;
                if (truck != null) game.Logistics.Select(truck.source, truck.destination, !truck.repeat);
            }
            );
            Rect((RectTransform)repeat.transform, new(.2f, .21f), new(.8f, .31f), Vector2.zero, Vector2.zero);
            cargoActions.AddRange(new[]
            {
                source, destination, dispatch, repeat
            }
            );
            cargoClose = FooterButton(board.transform, "Đóng", CloseCargo);
        }
    }
}
