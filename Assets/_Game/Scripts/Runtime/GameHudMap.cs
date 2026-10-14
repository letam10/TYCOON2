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
        public bool MapOpen => mode == ScreenMode.Map;

        void BuildMap()
        {
            minimapRect = (RectTransform)Panel("Minimap", hud.transform, Surface).transform;
            minimap = NewRect("MinimapTerrain", minimapRect).gameObject.AddComponent<CityMapGraphic>();
            Rect(minimap.rectTransform, Vector2.zero, Vector2.one, new(8, 30), new(-8, -8));
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
                float top = .94f - i * .06f;
                Rect((RectTransform)button.transform, new(.755f, top - .06f), new(.97f, top), Vector2.zero, Vector2.zero);
                var pin = Button(worldMap.transform, "•", () => MapTravel(index), true);
                pin.name = "MapPin_" + point.id;
                Vector2 position = worldMap.Position(point.position);
                Rect((RectTransform)pin.transform, position, position, new(-12, -12), new(12, 12));
                var caption = Text("MapLabel_" + point.id, worldMap.transform, point.label,
                    11, TextAnchor.MiddleLeft, Ink);
                caption.raycastTarget = false;
                Rect(caption.rectTransform, position, position, new(10, -9), new(170, 9));
            }
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
            if (!CityMapLocations.Teleport(game, index, out string message))
            { SetText(mapMessage, message); return; }
            ShowScreen(ScreenMode.Play);
            game.Say(message);
            minimap.SetVerticesDirty();
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
            if (menuKey.WasPressedThisFrame() || Keyboard.current?.escapeKey.wasPressedThisFrame == true
                || Gamepad.current?.selectButton.wasPressedThisFrame == true) TogglePause();
            return false;
        }
    }
}
