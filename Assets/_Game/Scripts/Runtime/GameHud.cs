using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using System.Collections.Generic;
using System.Text;

namespace Tycoon
{
    public sealed partial class GameHud : MonoBehaviour
    {
        const string Surface = "#173C35", Card = "#244D42", Accent = "#CBE78B", Ink = "#F6F6E9", Muted = "#B9CEC2";
        enum ScreenMode
        {
            Play, Pause, Crew, Stock, Cargo, Information
        }
        GameSession game;
        Canvas canvas;
        RectTransform safeArea;
        GameObject hud, menu, crewPanel, stockPanel, cargoPanel, informationPanel, toastRoot;
        RectTransform cashRect, titleRect, objectiveRect, financeRect, stockRect, carryRect, actionRect, dockRect;
        Text cashInHand, cashInSafe, progress, objective, carry, prompt, toast, finance, stock, cargoText,
            crewSummary, help;
        Image carryFill;
        Button continueButton, crewBack, stockClose, cargoClose, informationClose, recipeButton;
        readonly List<Button> cargoActions = new();
        readonly List<Button> quickActions = new();
        readonly Dictionary<string, Text> crewStatuses = new();
        readonly List<(StorageStation storage, string item, Text label)> stockRows = new();
        readonly StringBuilder stockLines = new();
        Transform stockContent, crewContent;
        ScreenMode mode;
        public bool AllowsPlayerControl => mode == ScreenMode.Play && !modOpen;
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
            canvas.enabled = false;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            canvas.scaleFactor = Mathf.Sqrt(Screen.width / 1920f * (Screen.height / 1080f));
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
            BuildPlayingHud();
            BuildInformation();
            BuildPause();
            BuildCrew();
            BuildStock();
            BuildCargo();
            BuildModMenu();
            toastRoot.transform.SetParent(safeArea, false);
            toastRoot.transform.SetAsLastSibling();
            menu.SetActive(false);
            crewPanel.SetActive(false);
            stockPanel.SetActive(false);
            cargoPanel.SetActive(false);
            informationPanel.SetActive(false);
            Canvas.ForceUpdateCanvases();
            LayoutHud();
            RefreshPlayingHud();
            RefreshStatus();
            canvas.enabled = true;
        }
        void Update()
        {
            if (!game || !game.Player || !canvas) return;
            if (Screen.width != screenWidth || Screen.height != screenHeight || Screen.safeArea != lastSafeArea ||
            !Mathf.Approximately(canvas.scaleFactor, lastCanvasScale)) LayoutHud();
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true
                || Gamepad.current?.selectButton.wasPressedThisFrame == true) TogglePause();
            RefreshNotices();
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + .15f;
                RefreshPlayingHud();
                if (mode == ScreenMode.Cargo) RefreshCargo();
            }
            if (Time.unscaledTime >= nextStatusRefresh)
            {
                nextStatusRefresh = Time.unscaledTime + .4f;
                RefreshStatus();
            }
        }
        public void TogglePause()
        {
            if (modOpen)
            {
                CloseModMenu();
                return;
            }
            if (mode is ScreenMode.Stock or ScreenMode.Cargo or ScreenMode.Information)
            {
                ShowScreen(ScreenMode.Play);
                return;
            }
            if (mode == ScreenMode.Crew)
            {
                BackToPauseMenu();
                return;
            }
            ShowScreen(mode == ScreenMode.Pause ? ScreenMode.Play : ScreenMode.Pause);
            if (mode == ScreenMode.Pause) Select(continueButton);
        }
        void ShowScreen(ScreenMode next)
        {
            if (modOpen) CloseModMenu();
            bool wasPaused = mode is ScreenMode.Pause or ScreenMode.Crew or ScreenMode.Information;
            bool isPaused = next is ScreenMode.Pause or ScreenMode.Crew or ScreenMode.Information;
            // Đóng bảng không được mở lại điều khiển khi save/load đang bị chặn.
            if (!wasPaused && isPaused)
            {
                resumeTimeScale = Time.timeScale;
                Time.timeScale = 0;
            }
            else if (wasPaused && !isPaused) Time.timeScale = resumeTimeScale;
            mode = next;
            game.Player.StopInteraction();
            game.Player.CanControl = mode == ScreenMode.Play && game.CanSimulate;
            menu.SetActive(mode == ScreenMode.Pause);
            crewPanel.SetActive(mode == ScreenMode.Crew);
            stockPanel.SetActive(mode == ScreenMode.Stock);
            cargoPanel.SetActive(mode == ScreenMode.Cargo);
            informationPanel.SetActive(mode == ScreenMode.Information);
            hud.SetActive(mode == ScreenMode.Play);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }
        void OnDestroy()
        {
            if (mode is ScreenMode.Pause or ScreenMode.Crew or ScreenMode.Information) Time.timeScale = resumeTimeScale;
            if (ownedEvents) Destroy(ownedEvents);
            if (rounded) Destroy(rounded);
            if (roundedTexture) Destroy(roundedTexture);
        }
    }
}
