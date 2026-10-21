using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed partial class GameHud
    {
        GameObject mapPanel;
        RectTransform minimapRect;
        CityMapGraphic minimap, worldMap;
        MapPlayerIndicator mapPlayer;
        InputAction mapKey, menuKey;
        ScreenMode mapReturnMode;
        int inputRevision = -1;
        Text mapMessage, minimapHelp;
        Vector2 mapPointerPosition;
        bool mapPointerActive;
        public bool MapOpen => mode == ScreenMode.Map;

        void BuildMap()
        {
            minimapRect = (RectTransform)Panel("Minimap", hud.transform, Surface).transform;
            // Mask phải nằm ở cha của terrain; nhãn và nút giữ nguyên trên khung ngoài.
            var viewport = NewRect("MinimapViewport", minimapRect);
            Rect(viewport, Vector2.zero, Vector2.one, new(8, 30), new(-8, -8));
            viewport.gameObject.AddComponent<RectMask2D>();
            var terrain = NewRect("MinimapTerrain", viewport);
            terrain.gameObject.AddComponent<CanvasRenderer>();
            minimap = terrain.gameObject.AddComponent<CityMapGraphic>();
            Rect(minimap.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            minimap.Initialize(game, true);
            minimapHelp = Text("MinimapHelp", minimapRect, "", 15, TextAnchor.MiddleCenter, Ink);
            Rect(minimapHelp.rectTransform, Vector2.zero, new(1, 0), new(4, 3), new(-4, 26));
            var north = Text("North", minimapRect, "N", 15, TextAnchor.UpperRight, Ink);
            Rect(north.rectTransform, new(1, 1), Vector2.one, new(-30, -30), new(-9, -9));
            var click = minimapRect.gameObject.AddComponent<Button>();
            click.targetGraphic = minimapRect.GetComponent<Image>();
            click.targetGraphic.raycastTarget = true;
            click.onClick.AddListener(OpenMap);
            mapPanel = Modal("WorldMapPanel");
            var board = ManagementBoard(mapPanel.transform, "BẢN ĐỒ THÀNH PHỐ",
                "Chọn điểm vàng để dịch chuyển • mũi tên trắng là bạn");
            var area = NewRect("MapArea", board.transform);
            Rect(area, new(.035f, .19f), new(.73f, .79f), Vector2.zero, Vector2.zero);
            worldMap = NewRect("WorldMapTerrain", area).gameObject.AddComponent<CityMapGraphic>();
            Rect(worldMap.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var aspect = worldMap.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = CityDistricts.GroundWidth / CityDistricts.GroundDepth;
            worldMap.Initialize(game, false);
            for (int i = 0; i < CityMapLocations.Points.Length; i++)
            {
                int index = i;
                var point = CityMapLocations.Points[i];
                var button = Button(board.transform, point.label, () => MapTravel(index));
                button.name = "MapTravel_" + point.id;
                var buttonLabel = button.GetComponentInChildren<Text>();
                if (buttonLabel)
                {
                    // Hàng map thấp; 18 px giữ đủ chiều cao sau Inset(10) ở 720p.
                    buttonLabel.fontSize = 18;
                    buttonLabel.verticalOverflow = VerticalWrapMode.Overflow;
                }
                float rowHeight = Mathf.Min(.06f,
                    .88f / Mathf.Max(1, CityMapLocations.Points.Length));
                float top = .96f - i * rowHeight;
                Rect((RectTransform)button.transform, new(.755f, top - rowHeight),
                    new(.97f, top), Vector2.zero, Vector2.zero);
                var pin = Button(worldMap.transform, CityMapGraphic.PoiGlyph(point.id),
                    () => MapTravel(index), true);
                pin.name = "MapPin_" + point.id;
                if (pin.targetGraphic is Image pinImage)
                    pinImage.color = Color.clear;
                var pinLabel = pin.GetComponentInChildren<Text>();
                if (pinLabel)
                {
                    pinLabel.color = CityMapGraphic.PoiColor(point.id);
                    pinLabel.fontSize = 17;
                }
                Vector2 position = worldMap.Position(point.position);
                Rect((RectTransform)pin.transform, position, position, new(-12, -12), new(12, 12));
                var caption = Text("MapLabel_" + point.id, worldMap.transform, point.label,
                    11, TextAnchor.MiddleLeft, Ink);
                caption.raycastTarget = false;
                Vector2 captionOffset = CityMapLocations.CaptionOffset(point.id);
                float captionWidth = CityMapLocations.CaptionWidth(point.id);
                Rect(caption.rectTransform, position, position,
                    captionOffset + new Vector2(0, -9),
                    captionOffset + new Vector2(captionWidth, 9));
            }
            var zoomOut = Button(board.transform, "−", () => ZoomMap(-.2f));
            zoomOut.name = "MapZoomOut";
            Rect((RectTransform)zoomOut.transform, new(.04f, .80f), new(.11f, .87f), Vector2.zero, Vector2.zero);
            var zoomIn = Button(board.transform, "+", () => ZoomMap(.2f));
            zoomIn.name = "MapZoomIn";
            Rect((RectTransform)zoomIn.transform, new(.115f, .80f), new(.185f, .87f), Vector2.zero, Vector2.zero);
            var northButton = Button(board.transform, "Bắc", ResetMapNorth);
            northButton.name = "MapResetNorth";
            Rect((RectTransform)northButton.transform, new(.19f, .80f), new(.31f, .87f), Vector2.zero, Vector2.zero);
            var focusButton = Button(board.transform, "Về bạn", FocusMapPlayer);
            focusButton.name = "MapFocusPlayer";
            Rect((RectTransform)focusButton.transform, new(.315f, .80f), new(.46f, .87f), Vector2.zero, Vector2.zero);
            var legend = Text("MapLegend", board.transform,
                "Kéo để pan • chuột giữa để xoay • cuộn để zoom", 14, TextAnchor.MiddleLeft, Muted);
            Rect(legend.rectTransform, new(.47f, .80f), new(.73f, .87f), Vector2.zero, Vector2.zero);
            mapPlayer = NewRect("MapPlayerIndicator", worldMap.transform).gameObject.AddComponent<MapPlayerIndicator>();
            Rect(mapPlayer.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            mapPlayer.Initialize(worldMap, game.Player);
            mapMessage = Text("MapMessage", board.transform, "", 18, TextAnchor.MiddleCenter, Muted);
            Rect(mapMessage.rectTransform, new(.04f, .12f), new(.96f, .17f), Vector2.zero, Vector2.zero);
            FooterButton(board.transform, "Đóng", CloseMap).name = "CloseMap";
            mapPanel.SetActive(false);
            mapKey = new InputAction("Map", InputActionType.Button, "<Keyboard>/m");
            menuKey = new InputAction("Menu", InputActionType.Button, "<Keyboard>/escape");
            mapKey.Enable(); menuKey.Enable();
        }

        public void OpenMap()
        {
            if (game.SaveBlocked || game.IsRestoring || MapOpen) return;
            mapReturnMode = mode;
            ShowScreen(ScreenMode.Map);
            worldMap.SetTooltip(string.Empty);
            worldMap.SetVerticesDirty();
            mapPlayer.SetVerticesDirty();
            SetText(mapMessage, "Dịch chuyển miễn phí, giữ nguyên hàng và tiền.");
            Select(mapPanel.transform.Find("ManagementBoard/CloseMap").GetComponent<Button>());
        }

        public void CloseMap()
        {
            if (MapOpen) ShowScreen(mapReturnMode);
        }

        void MapTravel(int index)
        {
            if (index >= 0 && index < CityMapLocations.Points.Length)
            {
                string id = CityMapLocations.Points[index].id;
                worldMap.SelectPoi(id);
                worldMap.SetRouteHighlight(id);
            }
            if (!CityMapLocations.Teleport(game, index, out string message))
            { SetText(mapMessage, message); return; }
            ShowScreen(ScreenMode.Play);
            game.Say(message);
            minimap.SetVerticesDirty();
        }

        public void ZoomMap(float amount)
        {
            worldMap?.ZoomBy(amount);
            RefreshMapOverlayPositions();
        }

        public void PanMap(Vector2 normalizedDelta)
        {
            worldMap?.Pan(normalizedDelta);
            RefreshMapOverlayPositions();
        }

        public void RotateMap(float degrees)
        {
            worldMap?.RotateBy(degrees);
            RefreshMapOverlayPositions();
        }

        public void ResetMapNorth()
        {
            worldMap?.ResetNorth();
            RefreshMapOverlayPositions();
        }

        public void FocusMapPlayer()
        {
            worldMap?.FocusPlayer();
            RefreshMapOverlayPositions();
        }

        public void ToggleMapKind(string kind)
        {
            worldMap?.ToggleKind(kind);
            RefreshMapOverlayPositions();
        }

        public void SelectMapPoi(string id)
        {
            worldMap?.SelectPoi(id);
            worldMap?.SetRouteHighlight(id);
            SetMapTooltip(id);
        }

        public void SetMapTooltip(string id)
        {
            if (!worldMap || !mapMessage)
                return;
            worldMap.SetTooltip(id);
            string tooltip = worldMap.GetPoiTooltip(id);
            SetText(mapMessage, string.IsNullOrEmpty(tooltip)
                ? "Dịch chuyển miễn phí, giữ nguyên hàng và tiền."
                : tooltip + " • điểm vàng để dịch chuyển");
        }

        void RefreshMapOverlayPositions()
        {
            if (!worldMap)
                return;
            foreach (var point in CityMapLocations.Points)
            {
                Vector2 position = worldMap.Position(point.position);
                Transform pin = worldMap.transform.Find("MapPin_" + point.id);
                if (pin)
                {
                    pin.gameObject.SetActive(worldMap.IsPoiVisible(point.id));
                    Rect((RectTransform)pin, position, position, new(-12, -12), new(12, 12));
                }
                Transform caption = worldMap.transform.Find("MapLabel_" + point.id);
                if (caption)
                {
                    caption.gameObject.SetActive(worldMap.IsPoiVisible(point.id));
                    Vector2 offset = CityMapLocations.CaptionOffset(point.id);
                    float width = CityMapLocations.CaptionWidth(point.id);
                    Rect((RectTransform)caption, position, position,
                        offset + new Vector2(0, -9), offset + new Vector2(width, 9));
                }
            }
            worldMap.SetVerticesDirty();
            if (mapPlayer)
                mapPlayer.SetVerticesDirty();
        }

        void UpdateMapInput()
        {
            if (!MapOpen)
                return;
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > .01f)
                    ZoomMap(scroll * .002f);
                Vector2 pointer = mouse.position.ReadValue();
                bool inMap = worldMap && RectTransformUtility.RectangleContainsScreenPoint(
                    worldMap.rectTransform, pointer);
                bool rotate = mouse.middleButton.isPressed || keyboard?.leftAltKey.isPressed == true;
                bool drag = mouse.leftButton.isPressed || rotate;
                if (drag && inMap)
                {
                    if (!mapPointerActive)
                        mapPointerPosition = pointer;
                    Vector2 delta = pointer - mapPointerPosition;
                    if (rotate)
                        RotateMap(delta.x * .35f);
                    else
                        PanMap(new Vector2(-delta.x / Mathf.Max(1, worldMap.rectTransform.rect.width),
                            -delta.y / Mathf.Max(1, worldMap.rectTransform.rect.height)));
                    mapPointerPosition = pointer;
                    mapPointerActive = true;
                }
                else
                    mapPointerActive = false;
            }
            if (keyboard != null)
            {
                if (keyboard.nKey.wasPressedThisFrame)
                    ResetMapNorth();
                if (keyboard.fKey.wasPressedThisFrame)
                    FocusMapPlayer();
            }
            if (gamepad != null)
            {
                Vector2 pan = gamepad.leftStick.ReadValue();
                if (pan.sqrMagnitude > .01f)
                    PanMap(-pan * Time.unscaledDeltaTime * .45f);
                Vector2 rotate = gamepad.rightStick.ReadValue();
                if (Mathf.Abs(rotate.x) > .01f)
                    RotateMap(rotate.x * Time.unscaledDeltaTime * 90);
                if (gamepad.leftShoulder.wasPressedThisFrame)
                    ZoomMap(-.2f);
                if (gamepad.rightShoulder.wasPressedThisFrame)
                    ZoomMap(.2f);
            }
        }

        bool UpdateNavigationInput()
        {
            if (UpdateBindingCapture()) return true;
            if (inputRevision != GameSettingsStore.Revision)
            {
                inputRevision = GameSettingsStore.Revision;
                GameKeyBindings.Apply(mapKey, 0, "map"); GameKeyBindings.Apply(menuKey, 0, "menu");
                minimapHelp.text = GameKeyBindings.Label("map") + " • " + GameLocalization.Translate("Bản đồ");
            }
            if (mapKey.WasPressedThisFrame())
            {
                if (MapOpen) CloseMap();
                else if (mode is ScreenMode.Play or ScreenMode.Pause or ScreenMode.Settings) OpenMap();
            }
            if (MapOpen)
            {
                UpdateMapInput();
                RefreshMapOverlayPositions();
            }
            if (menuKey.WasPressedThisFrame() || Keyboard.current?.escapeKey.wasPressedThisFrame == true
                || Gamepad.current?.selectButton.wasPressedThisFrame == true) TogglePause();
            return false;
        }
    }
}
