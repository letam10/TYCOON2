using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tycoon
{
    public sealed class GameHud : MonoBehaviour
    {
        const string Surface = "#173C35", Card = "#244D42", Accent = "#CBE78B", Ink = "#F6F6E9", Muted = "#B9CEC2";
        enum ScreenMode { Play, Pause, Crew, Stock, Cargo, Information }
        GameSession game;
        Canvas canvas;
        RectTransform safeArea;
        GameObject hud, menu, crewPanel, stockPanel, cargoPanel, informationPanel, toastRoot;
        RectTransform walletRect, titleRect, objectiveRect, financeRect, stockRect, carryRect, actionRect, dockRect;
        Text money, progress, objective, carry, prompt, toast, finance, stock, cargoText, crewSummary, help;
        Image carryFill;
        Button continueButton, crewBack, stockClose, cargoClose, informationClose, recipeButton;
        readonly List<Button> cargoActions = new();
        readonly List<Button> quickActions = new();
        readonly Dictionary<string, Text> crewStatuses = new();
        readonly List<(StorageStation storage, string item, Text label)> stockRows = new();
        readonly StringBuilder stockLines = new();
        Transform stockContent, crewContent;
        ScreenMode mode;
        public bool AllowsPlayerControl => mode == ScreenMode.Play;
        float nextRefresh, nextStatusRefresh, resumeTimeScale = 1, lastCanvasScale;
        int screenWidth, screenHeight;
        UnityEngine.Rect lastSafeArea;
        GameObject ownedEvents;
        Sprite rounded;
        Texture2D roundedTexture;

        public void Initialize()
        {
            game = GameSession.Instance;
            var root = new GameObject("HUD", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main; canvas.planeDistance = .5f; canvas.sortingOrder = 10;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            root.AddComponent<GraphicRaycaster>();
            if (!EventSystem.current)
            {
                ownedEvents = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                ownedEvents.transform.SetParent(transform, false);
            }
            safeArea = NewRect("SafeArea", root.transform);
            Rect(safeArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            hud = new GameObject("PlayingHud", typeof(RectTransform));
            hud.transform.SetParent(safeArea, false);
            Rect((RectTransform)hud.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            BuildPlayingHud(); BuildInformation(); BuildPause(); BuildCrew(); BuildStock(); BuildCargo();
            toastRoot.transform.SetParent(safeArea, false); toastRoot.transform.SetAsLastSibling();
            menu.SetActive(false); crewPanel.SetActive(false); stockPanel.SetActive(false); cargoPanel.SetActive(false); informationPanel.SetActive(false);
            Canvas.ForceUpdateCanvases(); LayoutHud(); RefreshPlayingHud(); RefreshStatus();
        }

        void BuildPlayingHud()
        {
            var root = hud.transform;
            walletRect = (RectTransform)Panel("Wallet", root, Surface).transform;
            var badge = Panel("CashIcon", walletRect, Accent);
            Rect((RectTransform)badge.transform, new(0, .5f), new(0, .5f), new(10, -18), new(46, 18));
            var badgeText = Text("Currency", badge.transform, "XU", 15, TextAnchor.MiddleCenter, Surface); Inset(badgeText.rectTransform, 2);
            var walletLabel = Text("WalletLabel", walletRect, "TYCOON2 • VÍ", 13, TextAnchor.MiddleLeft, Muted);
            Rect(walletLabel.rectTransform, new(0, 1), new(1, 1), new(56, -25), new(-10, -5));
            money = Text("Money", walletRect, "0", 28, TextAnchor.MiddleLeft, Ink);
            Rect(money.rectTransform, Vector2.zero, Vector2.one, new(56, 5), new(-10, -25));
            money.resizeTextForBestFit = true; money.resizeTextMinSize = 20; money.resizeTextMaxSize = 28;
            titleRect = (RectTransform)Panel("Title", root, Surface).transform;
            var title = Text("Brand", titleRect, "TYCOON2", 18, TextAnchor.MiddleLeft, Accent);
            Rect(title.rectTransform, new(0, 1), new(1, 1), new(18, -35), new(-16, -8));
            progress = Text("Progress", titleRect, "", 22, TextAnchor.MiddleLeft, Ink);
            Rect(progress.rectTransform, Vector2.zero, Vector2.one, new(18, 10), new(-16, -37));
            objectiveRect = (RectTransform)Panel("Objective", root, Surface, .93f).transform;
            var objectiveTitle = Text("ObjectiveTitle", objectiveRect, "BƯỚC TIẾP THEO", 16, TextAnchor.MiddleLeft, Accent);
            Rect(objectiveTitle.rectTransform, new(0, 1), new(1, 1), new(18, -34), new(-16, -8));
            objective = Text("ObjectiveText", objectiveRect, "", 22, TextAnchor.UpperLeft, Ink);
            Rect(objective.rectTransform, Vector2.zero, Vector2.one, new(18, 14), new(-16, -42));
            objective.resizeTextForBestFit = true; objective.resizeTextMinSize = 18; objective.resizeTextMaxSize = 22;
            financeRect = (RectTransform)Panel("Finance", root, Surface, .9f).transform;
            finance = Text("FinanceText", financeRect, "", 21, TextAnchor.MiddleLeft, Muted); Inset(finance.rectTransform, 18);
            stockRect = (RectTransform)Panel("AreaStock", root, Surface, .9f).transform;
            stock = Text("StockText", stockRect, "", 20, TextAnchor.UpperLeft, Ink); Inset(stock.rectTransform, 18);
            carryRect = (RectTransform)Panel("Carry", root, Surface).transform;
            carry = Text("CarryText", carryRect, "", 20, TextAnchor.MiddleLeft, Ink);
            Rect(carry.rectTransform, Vector2.zero, Vector2.one, new(12, 14), new(-12, -4));
            carry.resizeTextForBestFit = true; carry.resizeTextMinSize = 18; carry.resizeTextMaxSize = 20;
            var capacity = Panel("CarryCapacity", carryRect, "#102C27");
            Rect((RectTransform)capacity.transform, Vector2.zero, new(1, 0), new(12, 6), new(-12, 10));
            carryFill = Panel("CarryFill", capacity.transform, Accent).GetComponent<Image>();
            Rect(carryFill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            actionRect = (RectTransform)Panel("Action", root, Surface, .96f).transform;
            var actionTitle = Text("ActionTitle", actionRect, "ĐỨNG GẦN • TỰ ĐỘNG", 13, TextAnchor.MiddleCenter, Accent);
            Rect(actionTitle.rectTransform, new(0, 1), new(1, 1), new(12, -22), new(-12, -4));
            prompt = Text("ActionText", actionRect, "", 20, TextAnchor.MiddleCenter, Ink);
            Rect(prompt.rectTransform, Vector2.zero, Vector2.one, new(12, 6), new(-12, -24));
            prompt.resizeTextForBestFit = true; prompt.resizeTextMinSize = 18; prompt.resizeTextMaxSize = 20;
            recipeButton = Button(root, "Đổi món", CycleRecipe, true); recipeButton.gameObject.SetActive(false);
            dockRect = NewRect("QuickActions", root);
            AddQuickAction("Xem kho / chọn hàng", "Kho", OpenStock);
            AddQuickAction("Vận chuyển xe tải", "Xe tải", OpenCargo);
            AddQuickAction("Thông tin", "Thông tin", OpenInformation);
            AddQuickAction("Menu", "Menu", TogglePause);
            if (DevelopmentAssistance.Enabled)
                AddQuickAction("Hỗ trợ +999.999 / mở khu", "Hỗ trợ",
                    () => { if (game.CanSimulate && DevelopmentAssistance.Apply(game)) game.Say("Đã cộng 999.999 xu và mở tuyến cơ bản."); });
            help = Text("Help", root, "", 14, TextAnchor.MiddleCenter, Ink);
            toastRoot = Panel("Toast", root, "#3C5936");
            toast = Text("ToastText", toastRoot.transform, "", 22, TextAnchor.MiddleCenter, Ink); Inset(toast.rectTransform, 16);
            toastRoot.SetActive(false);
        }

        void AddQuickAction(string name, string label, UnityEngine.Events.UnityAction click)
        {
            var button = Button(dockRect, name, click);
            var text = button.GetComponentInChildren<Text>(); text.text = label; text.fontSize = 19;
            Inset(text.rectTransform, 3); quickActions.Add(button);
        }
        void BuildInformation()
        {
            informationPanel = Modal("BusinessInformation");
            var board = ManagementBoard(informationPanel.transform, "TỔNG QUAN KINH DOANH", "Mục tiêu, doanh thu và kho gần bạn");
            foreach (var panel in new[] { titleRect, objectiveRect, financeRect, stockRect }) panel.SetParent(board.transform, false);
            informationClose = FooterButton(board.transform, "Đóng", () => ShowScreen(ScreenMode.Play));
        }
        void OpenInformation()
        {
            if (!game.CanSimulate) return;
            ShowScreen(ScreenMode.Information); RefreshStatus(); Select(informationClose);
        }
        void BuildPause()
        {
            menu = Modal("Pause");
            var card = Panel("PauseCard", menu.transform, Surface);
            Rect((RectTransform)card.transform, new(.5f, .5f), new(.5f, .5f), new(-320, -474), new(320, 474));
            var heading = Text("PauseHeading", card.transform, "TYCOON2", 48, TextAnchor.MiddleCenter, Ink);
            Rect(heading.rectTransform, new(0, 1), new(1, 1), new(20, -108), new(-20, -35));
            var subtitle = Text("PauseSubtitle", card.transform, "Từ nông trại đến đế chế kinh doanh", 22, TextAnchor.MiddleCenter, Muted);
            Rect(subtitle.rectTransform, new(0, 1), new(1, 1), new(26, -160), new(-26, -108));
            continueButton = PauseButton(card.transform, "Tiếp tục", -210, TogglePause, true);
            PauseButton(card.transform, "Thông tin kinh doanh", -298, OpenInformation);
            PauseButton(card.transform, "Kho • chọn hàng", -386, OpenStock);
            PauseButton(card.transform, "Vận chuyển xe tải", -474, OpenCargo);
            PauseButton(card.transform, "Quản lý đội", -562, OpenCrewMenu);
            PauseButton(card.transform, "Lưu trò chơi", -650, () => game.SaveGame());
            PauseButton(card.transform, "Lưu & thoát", -738, () => { game.SaveGame(); Application.Quit(); });
            var note = Text("PauseNote", card.transform, "Trò chơi đang tạm dừng.\nEsc / nút Back để tiếp tục.", 20, TextAnchor.MiddleCenter, Muted);
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
            var board = ManagementBoard(stockPanel.transform, "KHO HÀNG", "Chọn mặt hàng muốn lấy • giữ chỗ và hàng đang đến được cập nhật riêng");
            stockContent = ScrollContent("Stock", board.transform);
            stockClose = FooterButton(board.transform, "Đóng", CloseStock);
        }
        void BuildCargo()
        {
            cargoPanel = Modal("CargoRoutes");
            var board = ManagementBoard(cargoPanel.transform, "VẬN CHUYỂN XE TẢI", "Chọn hai kho khác nhau để điều phối tuyến hàng");
            cargoText = Text("CargoSummary", board.transform, "", 24, TextAnchor.MiddleLeft, Ink);
            Rect(cargoText.rectTransform, new(.04f, .64f), new(.96f, .8f), Vector2.zero, Vector2.zero);
            var source = Button(board.transform, "Đổi kho nguồn", () => CycleCargo(true));
            Rect((RectTransform)source.transform, new(.05f, .51f), new(.48f, .61f), Vector2.zero, Vector2.zero);
            var destination = Button(board.transform, "Đổi kho đích", () => CycleCargo(false));
            Rect((RectTransform)destination.transform, new(.52f, .51f), new(.95f, .61f), Vector2.zero, Vector2.zero);
            var dispatch = Button(board.transform, "Gửi xe", () => { if (game.Logistics != null && !game.Logistics.Dispatch()) game.Say(game.Logistics.Reason); }, true);
            Rect((RectTransform)dispatch.transform, new(.2f, .36f), new(.8f, .46f), Vector2.zero, Vector2.zero);
            var repeat = Button(board.transform, "Bật / tắt tuyến tự động", () =>
            {
                var truck = game.Transactions?.View.truck;
                if (truck != null) game.Logistics.Select(truck.source, truck.destination, !truck.repeat);
            });
            Rect((RectTransform)repeat.transform, new(.2f, .21f), new(.8f, .31f), Vector2.zero, Vector2.zero);
            cargoActions.AddRange(new[] { source, destination, dispatch, repeat });
            cargoClose = FooterButton(board.transform, "Đóng", CloseCargo);
        }

        void LayoutHud()
        {
            screenWidth = Screen.width; screenHeight = Screen.height; lastSafeArea = Screen.safeArea;
            // Mọi mép HUD nằm trong vùng an toàn, kể cả khi đổi tỷ lệ cửa sổ.
            safeArea.anchorMin = new(lastSafeArea.xMin / Mathf.Max(1, screenWidth), lastSafeArea.yMin / Mathf.Max(1, screenHeight));
            safeArea.anchorMax = new(lastSafeArea.xMax / Mathf.Max(1, screenWidth), lastSafeArea.yMax / Mathf.Max(1, screenHeight));
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            float scale = Mathf.Max(.01f, canvas.scaleFactor); lastCanvasScale = scale;
            float width = lastSafeArea.width / scale, height = lastSafeArea.height / scale;
            bool compact = width < 1460;
            TopLeft(walletRect, 16, 16, 228, 66);
            BottomLeft(carryRect, 16, compact ? 132 : 44, 284, 74);
            BottomCenter(actionRect, 44, Mathf.Min(560, width - 32), 84);
            Rect(help.rectTransform, Vector2.zero, new(1, 0), new(16, 6), new(-16, 30));
            // Bảng lớn chỉ nằm trong modal; HUD chơi thường giữ diện tích che rất nhỏ.
            int[] buttonWidths = { 76, 96, 104, 76, 98 };
            bool wrapButtons = width < 850;
            float dockWidth = wrapButtons ? 224 : quickActions.Select((button, i) => buttonWidths[i]).Sum() + (quickActions.Count - 1) * 8;
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
            var recipeLabel = recipeButton.GetComponentInChildren<Text>(); recipeLabel.fontSize = 19; Inset(recipeLabel.rectTransform, 3);
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
            var pauseCard = menu.transform.Find("PauseCard") as RectTransform;
            pauseCard.localScale = Vector3.one * Mathf.Min(1, (height - 48) / 948, (width - 48) / 640);
        }
        void Update()
        {
            if (!game || !game.Player || !canvas) return;
            if (Screen.width != screenWidth || Screen.height != screenHeight || Screen.safeArea != lastSafeArea ||
                !Mathf.Approximately(canvas.scaleFactor, lastCanvasScale)) LayoutHud();
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true || Gamepad.current?.selectButton.wasPressedThisFrame == true) TogglePause();
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + .15f; RefreshPlayingHud();
                if (mode == ScreenMode.Cargo) RefreshCargo();
            }
            if (Time.unscaledTime >= nextStatusRefresh) { nextStatusRefresh = Time.unscaledTime + .4f; RefreshStatus(); }
        }
        void RefreshPlayingHud()
        {
            SetText(money, game.Economy.Money.ToString("N0"));
            string area = game.BusinessStage switch
            {
                5 => "Nhà hàng & tiệm bánh", 4 => "Tiệm bánh", 3 => "Siêu thị",
                2 => game.Economy.Has("mill") ? "Chế biến" : "Cửa hàng nông sản", _ => "Nông trại & cửa hàng"
            };
            SetText(progress, area + "\n" + game.Workers.Count + " nhân viên • " + game.Economy.Transactions + " đơn");
            var carried = game.Player.Carry.Snapshot();
            SetText(carry, (carried.Count == 0 ? "Giỏ trống" : Definitions.Item(carried[0].id).label) + "  " +
                game.Player.Carry.Total + "/" + game.Player.Carry.Capacity + "\nChọn: " + Definitions.Items[game.SelectedItem].label);
            float capacity = game.Player.Carry.Capacity > 0 ? (float)game.Player.Carry.Total / game.Player.Carry.Capacity : 0;
            carryFill.rectTransform.anchorMax = new(Mathf.Clamp01(capacity), 1); carryFill.enabled = capacity > 0;
            var active = game.Player.ActiveInteraction as ProximityTarget;
            bool canSelectRecipe = mode == ScreenMode.Play && game.CanSimulate && active?.Target is MachineStation machine && machine.Options.Length > 1;
            recipeButton.gameObject.SetActive(canSelectRecipe);
            var station = game.NearestStation(game.Player.transform.position);
            string reason = !game.CanSimulate ? "Gameplay đang tạm dừng • mở menu để kiểm tra lưu game." :
                !string.IsNullOrEmpty(game.Player.InteractionReason) ? game.Player.InteractionReason :
                active != null ? active.Cash ? "Thu tiền tại cọc tiền" : active.Target.Prompt :
                game.Player.ActiveInteraction is PurchasePad pad ? pad.Prompt :
                game.Player.ActiveInteraction is StationZone zone ? zone.Prompt :
                station ? station.Prompt : "Dừng 0,25 giây gần vật thể để bắt đầu";
            SetText(prompt, reason);
            prompt.color = Art.Hex(string.IsNullOrEmpty(game.Player.InteractionReason) && game.CanSimulate ? Ink : "#FFE5A4");
            SetText(help, Gamepad.current != null ?
                "Cần trái: di chuyển    RB: chọn hàng    Start: lưu    Back: menu" :
                "WASD: di chuyển    Dừng 0,25 giây: thao tác    Q: chọn hàng    F5: lưu    Esc: menu");
            bool showToast = Time.time < game.ToastUntil && !string.IsNullOrEmpty(game.Toast);
            toastRoot.SetActive(showToast); SetText(toast, game.Toast);
        }
        void RefreshStatus()
        {
            SetText(objective, game.ObjectiveText);
            SetText(finance, "Chưa thu  " + game.Economy.PendingCash.ToString("N0") + " xu\nDoanh thu  " +
                game.Economy.Revenue.ToString("N0") + " xu\nĐã thu  " + game.Economy.CashCollected.ToString("N0") +
                " xu\nThất thoát  " + game.Economy.LostItems + " món");
            var nearby = game.Stations.Where(x => x && x.IsUnlocked && x is not StationZone and not PurchasePad and not ConveyorStation)
                .OrderBy(x => (x.transform.position - game.Player.transform.position).sqrMagnitude).FirstOrDefault();
            string area = nearby?.AreaId ?? "farm"; var storage = game.StorageFor(area);
            if (!storage || !storage.IsUnlocked) { area = "farm"; storage = game.StorageFor(area); }
            stockLines.Clear(); stockLines.AppendLine("KHO • " + AreaLabel(area));
            if (storage)
            {
                int rows = 0;
                foreach (var item in Definitions.Items.OrderByDescending(x => x.id == Definitions.Items[game.SelectedItem].id))
                {
                    int total = storage.Inventory.Count(item.id), held = storage.Inventory.Reserved(item.id), incoming = storage.Inventory.ReservedSpace(item.id);
                    if (total + held + incoming == 0 && item.id != Definitions.Items[game.SelectedItem].id) continue;
                    if (++rows > 3) { stockLines.AppendLine("Mở Kho để xem toàn bộ hàng"); break; }
                    stockLines.Append(item.label).Append("  ").Append(total);
                    if (held + incoming > 0) stockLines.Append(" • giữ ").Append(held).Append(" / đến ").Append(incoming);
                    stockLines.AppendLine();
                }
                stockLines.Append("Sức chứa  ").Append(storage.Inventory.Total).Append("/").Append(storage.Inventory.Capacity);
            }
            SetText(stock, stockLines.ToString());
            if (mode == ScreenMode.Crew)
                foreach (var row in crewStatuses)
                    SetText(row.Value, string.Join(" • ", game.Workers.Where(x => x && x.UpgradeId == row.Key).Select(x => x.Reason).Distinct().Take(3)));
            if (mode == ScreenMode.Stock) RefreshStockCounts();
        }
        void CycleRecipe()
        {
            if (game.Player.ActiveInteraction is not ProximityTarget target || target.Target is not MachineStation machine || machine.Running) return;
            int i = System.Array.IndexOf(machine.Options, machine.Recipe.id);
            if (game.Transactions.SelectRecipe(machine, machine.Options[(i + 1) % machine.Options.Length])) game.Say("Chuẩn bị: " + machine.Recipe.label);
        }
        void OpenStock()
        {
            if (!game.CanSimulate) return;
            ShowScreen(ScreenMode.Stock);
            foreach (Transform child in stockContent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            stockRows.Clear(); Button first = null; int row = 0;
            foreach (var storage in game.Stations.OfType<StorageStation>().Where(x => x.IsUnlocked))
                foreach (var item in Definitions.Items.Where(x => game.Progression.CanProduce(x.id) || storage.Inventory.Count(x.id) > 0))
                {
                    var line = Panel("StockRow", stockContent, Card);
                    Rect((RectTransform)line.transform, new(0, 1), new(1, 1), new(0, -row * 96 - 88), new(0, -row * 96));
                    var select = line.AddComponent<Button>(); StyleButton(select);
                    string sku = item.id;
                    select.onClick.AddListener(() => { game.Player.StopInteraction(); game.SelectedItem = System.Array.FindIndex(Definitions.Items, x => x.id == sku); CloseStock(); });
                    var label = Text("StockRowText", line.transform, "", 22, TextAnchor.MiddleLeft, Ink); Inset(label.rectTransform, 18);
                    stockRows.Add((storage, sku, label)); first ??= select; row++;
                }
            if (row == 0)
            {
                var empty = Text("EmptyStock", stockContent, "Chưa có kho hoặc mặt hàng đã mở.", 24, TextAnchor.MiddleCenter, Muted);
                Rect(empty.rectTransform, new(0, 1), new(1, 1), new(0, -120), Vector2.zero);
            }
            ((RectTransform)stockContent).sizeDelta = new(0, Mathf.Max(120, row * 96));
            stockContent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 1;
            RefreshStockCounts(); Select(first ? first : stockClose);
        }
        void RefreshStockCounts()
        {
            foreach (var row in stockRows)
                SetText(row.label, AreaLabel(row.storage.AreaId) + " • " + Definitions.Item(row.item).label +
                    "  " + row.storage.Inventory.Count(row.item) + "\nGiữ chỗ: " + row.storage.Inventory.Reserved(row.item) +
                    " • Đang đến: " + row.storage.Inventory.ReservedSpace(row.item) + " • Sức chứa kho: " + row.storage.Inventory.Capacity);
        }
        void CloseStock() { ShowScreen(ScreenMode.Play); }
        void OpenCargo()
        {
            if (!game.CanSimulate) return;
            ShowScreen(ScreenMode.Cargo); RefreshCargo();
            Select(game.Transactions?.View.truck != null ? cargoActions[0] : cargoClose);
        }
        void CloseCargo() { ShowScreen(ScreenMode.Play); }
        void RefreshCargo()
        {
            var truck = game.Transactions?.View.truck;
            foreach (var button in cargoActions) button.interactable = truck != null && game.CanSimulate;
            SetText(cargoText, truck == null ?
                "Xe tải + tài xế: mở khu Chế biến, kho cấp 3 và tự chở hàng 30 chuyến.\nGóp tiền tại điểm mua xe bên kho Chế biến." :
                "Nguồn: " + CargoWarehouse(truck.source) + " → Đích: " + CargoWarehouse(truck.destination) + "\n" +
                (truck.phase switch { "Idle" => "Đỗ tại bến", "Loading" => "Đang chất hàng", "Travelling" => "Đang vận chuyển", _ => "Chờ dỡ hàng" }) +
                " • Tự động: " + (truck.repeat ? "BẬT" : "TẮT") + "\n" + game.Logistics.Reason + "\n" +
                string.Join(" • ", game.Transactions.View.crates.Where(x => x.holder == truck.id).Select(x =>
                    Definitions.Item(x.item).label + " ×" + game.Transactions.View.stacks.Where(s => s.owner == x.id).Sum(s => s.quantity))));
        }
        string CargoWarehouse(string id) => AreaLabel(game.Stations.Find(x => x.Id == id)?.AreaId ?? id);
        void CycleCargo(bool source)
        {
            var truck = game.Transactions?.View.truck; if (truck == null) return;
            var options = game.Stations.OfType<StorageStation>().Where(x => x.IsUnlocked).Select(x => x.Id).ToArray();
            if (options.Length < 2) return;
            int index = System.Array.IndexOf(options, source ? truck.source : truck.destination);
            for (int i = 1; i <= options.Length; i++)
            {
                string id = options[(index + i) % options.Length];
                if (id == (source ? truck.destination : truck.source)) continue;
                game.Logistics.Select(source ? id : truck.source, source ? truck.destination : id, truck.repeat); break;
            }
            RefreshCargo();
        }
        public void TogglePause()
        {
            if (mode is ScreenMode.Stock or ScreenMode.Cargo or ScreenMode.Information) { ShowScreen(ScreenMode.Play); return; }
            if (mode == ScreenMode.Crew) { BackToPauseMenu(); return; }
            ShowScreen(mode == ScreenMode.Pause ? ScreenMode.Play : ScreenMode.Pause);
            if (mode == ScreenMode.Pause) Select(continueButton);
        }
        void ShowScreen(ScreenMode next)
        {
            bool wasPaused = mode is ScreenMode.Pause or ScreenMode.Crew or ScreenMode.Information;
            bool isPaused = next is ScreenMode.Pause or ScreenMode.Crew or ScreenMode.Information;
            // Đóng bảng không được mở lại điều khiển khi save/load đang bị chặn.
            if (!wasPaused && isPaused) { resumeTimeScale = Time.timeScale; Time.timeScale = 0; }
            else if (wasPaused && !isPaused) Time.timeScale = resumeTimeScale;
            mode = next; game.Player.StopInteraction();
            game.Player.CanControl = mode == ScreenMode.Play && game.CanSimulate;
            menu.SetActive(mode == ScreenMode.Pause); crewPanel.SetActive(mode == ScreenMode.Crew);
            stockPanel.SetActive(mode == ScreenMode.Stock); cargoPanel.SetActive(mode == ScreenMode.Cargo);
            informationPanel.SetActive(mode == ScreenMode.Information);
            hud.SetActive(mode == ScreenMode.Play);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }
        void OpenCrewMenu()
        {
            ShowScreen(ScreenMode.Crew); RefreshCrewMenu();
            Select(crewContent.GetComponentsInChildren<Button>().FirstOrDefault(x => x.interactable) ?? crewBack);
        }
        void BackToPauseMenu() { ShowScreen(ScreenMode.Pause); Select(continueButton); }
        void RefreshCrewMenu()
        {
            foreach (Transform child in crewContent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            crewStatuses.Clear(); var crews = game.CrewStates;
            if (crews.Count == 0)
            {
                SetText(crewSummary, "Trạm cấp 3 và 30 công việc tự làm sẽ mở điểm thuê đội.");
                ((RectTransform)crewContent).sizeDelta = new(0, 140);
                var empty = Text("NoCrew", crewContent, "Bạn có thể tự sản xuất, vận chuyển và phục vụ.", 24, TextAnchor.MiddleCenter, Muted);
                Rect(empty.rectTransform, new(0, 1), new(1, 1), new(20, -130), new(-20, -20)); return;
            }
            SetText(crewSummary, "Nâng cấp áp dụng đúng nghề và khu • tiền được trừ ngay khi chọn.");
            int index = 0;
            foreach (var crew in crews)
            {
                var row = Panel("CrewRow_" + crew.id, crewContent, Card);
                Rect((RectTransform)row.transform, new(0, 1), new(1, 1), new(0, -index * 232 - 220), new(0, -index * 232));
                string title = RoleLabel(crew.role) + " • " + AreaLabel(crew.area) + " • " + crew.count + " người";
                var name = Text("CrewName", row.transform, title, 25, TextAnchor.MiddleLeft, Ink);
                Rect(name.rectTransform, new(0, 1), new(1, 1), new(18, -53), new(-18, -8));
                var status = Text("CrewStatus", row.transform, "", 20, TextAnchor.MiddleLeft, Muted);
                Rect(status.rectTransform, new(0, 1), new(1, 1), new(18, -110), new(-18, -57)); crewStatuses[crew.id] = status;
                CrewButton(row.transform, 0, UpgradeLabel(crew, "speed"), crew.speedLevel < 3 ? () => UpgradeCrew(crew.id, "speed") : null);
                bool hasCarry = crew.role is not "Cashier" and not "Driver" and not "Repairer";
                CrewButton(row.transform, 1, hasCarry ? UpgradeLabel(crew, "carry") : "Không dùng sức mang",
                    hasCarry && crew.carryLevel < 3 ? () => UpgradeCrew(crew.id, "carry") : null);
                CrewButton(row.transform, 2, crew.role == "Driver" ? "Một tài xế / xe" : UpgradeLabel(crew, "count"),
                    crew.role != "Driver" && crew.count < 3 ? () => UpgradeCrew(crew.id, "count") : null); index++;
            }
            ((RectTransform)crewContent).sizeDelta = new(0, index * 232);
            crewContent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 1; RefreshStatus();
        }
        string UpgradeLabel(CrewState crew, string type)
        {
            int level = type == "speed" ? crew.speedLevel : type == "carry" ? crew.carryLevel : crew.count;
            string label = type == "speed" ? "Tốc độ" : type == "carry" ? "Sức mang" : "Số người";
            if (level >= 3) return label + " • TỐI ĐA";
            return (type == "count" ? "Thêm người • " + (level + 1) : label + " cấp " + (level + 1)) +
                "\n" + game.CrewUpgradeCost(crew.id, type).ToString("N0") + " xu";
        }
        void CrewButton(Transform parent, int column, string label, UnityEngine.Events.UnityAction click)
        {
            var button = Button(parent, "CrewUpgrade", click);
            button.GetComponentInChildren<Text>().text = label; button.GetComponentInChildren<Text>().fontSize = 20;
            button.interactable = click != null;
            Rect((RectTransform)button.transform, new(column / 3f, 0), new((column + 1) / 3f, 0), new(12, 14), new(-12, 98));
        }
        void UpgradeCrew(string id, string type)
        {
            bool changed = game.UpgradeCrew(id, type);
            game.Say(changed ? "Đã nâng đội " + (Definitions.Upgrade(id)?.label ?? id) : "Không đủ xu hoặc nâng cấp đã tối đa.");
            RefreshCrewMenu(); Select(crewBack);
        }
        public static string AreaLabel(string area) => area switch
        {
            "farm" => "Nông trại", "farm_shop" => "Cửa hàng nông sản", "processing" => "Chế biến",
            "supermarket" or "market" => "Siêu thị", "bakery" => "Tiệm bánh", "restaurant" => "Nhà hàng", _ => area
        };
        static string RoleLabel(string role) => role switch
        {
            "Farmer" => "Nông dân", "AnimalWorker" => "Chăm vật nuôi", "Restocker" => "Xếp hàng", "Cashier" => "Bán hàng",
            "Processor" => "Chế biến", "Cook" => "Đầu bếp / thợ bánh", "Waiter" => "Phục vụ", "Transporter" => "Vận chuyển",
            "Repairer" => "Kỹ thuật viên sửa chữa", "Loader" => "Bốc hàng", "Driver" => "Tài xế", _ => role
        };
        GameObject Modal(string name)
        {
            var root = Panel(name, safeArea, "#071C19", .92f);
            Rect((RectTransform)root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.GetComponent<Image>().raycastTarget = true; return root;
        }
        GameObject ManagementBoard(Transform parent, string title, string subtitle)
        {
            var board = Panel("ManagementBoard", parent, Surface);
            Rect((RectTransform)board.transform, new(.06f, .08f), new(.94f, .92f), Vector2.zero, Vector2.zero);
            var heading = Text("Heading", board.transform, title, 34, TextAnchor.MiddleCenter, Ink);
            Rect(heading.rectTransform, new(0, 1), new(1, 1), new(26, -80), new(-26, -20));
            var subheading = Text("Subtitle", board.transform, subtitle, 21, TextAnchor.MiddleCenter, Muted);
            Rect(subheading.rectTransform, new(0, 1), new(1, 1), new(26, -112), new(-26, -76)); return board;
        }
        Transform ScrollContent(string name, Transform board)
        {
            var viewport = Panel(name + "Viewport", board, "#102F29");
            Rect((RectTransform)viewport.transform, Vector2.zero, Vector2.one, new(24, 114), new(-24, -172));
            viewport.AddComponent<RectMask2D>(); viewport.GetComponent<Image>().raycastTarget = true;
            var content = NewRect(name + "Rows", viewport.transform);
            content.anchorMin = new(0, 1); content.anchorMax = new(1, 1); content.pivot = new(.5f, 1);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var scroll = viewport.AddComponent<ScrollRect>(); scroll.viewport = (RectTransform)viewport.transform; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40; return content;
        }
        Button FooterButton(Transform board, string label, UnityEngine.Events.UnityAction click)
        {
            var button = Button(board, label, click);
            Rect((RectTransform)button.transform, new(.5f, 0), new(.5f, 0), new(-190, 24), new(190, 88)); return button;
        }
        Button PauseButton(Transform parent, string label, float y, UnityEngine.Events.UnityAction click, bool accent = false)
        {
            var button = Button(parent, label, click, accent);
            Rect((RectTransform)button.transform, new(0, 1), new(1, 1), new(40, y - 36), new(-40, y + 36)); return button;
        }
        GameObject Panel(string name, Transform parent, string color, float alpha = 1)
        {
            var rect = NewRect(name, parent); var image = rect.gameObject.AddComponent<Image>();
            var tint = Art.Hex(color); tint.a = alpha; image.color = tint; image.raycastTarget = false;
            if (!rounded)
            {
                roundedTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                roundedTexture.name = "HudRoundedCorners"; roundedTexture.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                {
                    float dx = Mathf.Max(11 - x, x - 20, 0), dy = Mathf.Max(11 - y, y - 20, 0);
                    roundedTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(11.5f - Mathf.Sqrt(dx * dx + dy * dy))));
                }
                roundedTexture.Apply(false, true);
                rounded = Sprite.Create(roundedTexture, new UnityEngine.Rect(0, 0, 32, 32), Vector2.one * .5f,
                    100, 0, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12)); rounded.name = "HudRoundedCorners";
            }
            image.sprite = rounded; image.type = Image.Type.Sliced; return rect.gameObject;
        }
        static Text Text(string name, Transform parent, string value, int size, TextAnchor alignment, string color)
        {
            var root = NewRect(name, parent); var text = root.gameObject.AddComponent<Text>();
            text.font = Art.Catalog.font; text.text = value; text.fontSize = size; text.alignment = alignment;
            text.color = Art.Hex(color); text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; return text;
        }
        Button Button(Transform parent, string label, UnityEngine.Events.UnityAction click, bool accent = false)
        {
            var root = Panel(label, parent, accent ? Accent : "#356754");
            var button = root.AddComponent<Button>(); StyleButton(button);
            if (click != null) button.onClick.AddListener(click);
            var text = Text("Label", root.transform, label, 24, TextAnchor.MiddleCenter, accent ? Surface : Ink);
            Inset(text.rectTransform, 10); return button;
        }
        static void StyleButton(Button button)
        {
            button.targetGraphic = button.GetComponent<Image>(); button.targetGraphic.raycastTarget = true;
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new(1.12f, 1.12f, 1.02f);
            colors.selectedColor = new(1.12f, 1.12f, 1.02f); colors.pressedColor = new(.78f, .9f, .76f);
            colors.disabledColor = new(.55f, .6f, .56f, .6f); colors.fadeDuration = .1f; button.colors = colors;
            button.gameObject.AddComponent<HudButtonFocus>().Target = button;
        }
        static void Select(Button button) { if (button && EventSystem.current) EventSystem.current.SetSelectedGameObject(button.gameObject); }
        static void SetText(Text label, string value) { if (label.text != value) label.text = value; }
        static RectTransform NewRect(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform)); root.transform.SetParent(parent, false); return (RectTransform)root.transform;
        }
        static void Inset(RectTransform rect, float amount) => Rect(rect, Vector2.zero, Vector2.one, new(amount, amount), new(-amount, -amount));
        static void TopLeft(RectTransform rect, float x, float y, float width, float height) => Rect(rect, new(0, 1), new(0, 1), new(x, -y - height), new(x + width, -y));
        static void TopRight(RectTransform rect, float x, float y, float width, float height) => Rect(rect, Vector2.one, Vector2.one, new(-x - width, -y - height), new(-x, -y));
        static void BottomLeft(RectTransform rect, float x, float y, float width, float height) => Rect(rect, Vector2.zero, Vector2.zero, new(x, y), new(x + width, y + height));
        static void BottomRight(RectTransform rect, float x, float y, float width, float height) => Rect(rect, new(1, 0), new(1, 0), new(-x - width, y), new(-x, y + height));
        static void BottomCenter(RectTransform rect, float y, float width, float height) => Rect(rect, new(.5f, 0), new(.5f, 0), new(-width * .5f, y), new(width * .5f, y + height));
        static void Rect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax; }
        void OnDestroy()
        {
            if (mode is ScreenMode.Pause or ScreenMode.Crew or ScreenMode.Information) Time.timeScale = resumeTimeScale;
            if (ownedEvents) Destroy(ownedEvents); if (rounded) Destroy(rounded); if (roundedTexture) Destroy(roundedTexture);
        }
    }

    // Chỉ nhận hover/chọn để thao tác kéo vẫn truyền tới danh sách cuộn.
    sealed class HudButtonFocus : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        public Button Target;
        public void OnPointerEnter(PointerEventData eventData)
        { if (Target && Target.interactable && EventSystem.current) EventSystem.current.SetSelectedGameObject(Target.gameObject); }
        public void OnSelect(BaseEventData eventData)
        {
            var scroll = GetComponentInParent<ScrollRect>();
            if (!scroll || !scroll.content || !scroll.viewport) return;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, transform);
            var view = scroll.viewport.rect;
            float move = bounds.max.y > view.yMax ? view.yMax - bounds.max.y :
                bounds.min.y < view.yMin ? view.yMin - bounds.min.y : 0;
            scroll.content.anchoredPosition += new Vector2(0, move);
        }
    }
}
