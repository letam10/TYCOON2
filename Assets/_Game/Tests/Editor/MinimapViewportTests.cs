using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tycoon.Tests
{
    public sealed class MinimapViewportTests
    {
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject sessionRoot, uiRoot;
        GameSession previousSession;
        GameCatalog previousCatalog, catalog;
        GameHud hud;
        RectTransform playingHud, minimapFrame;
        EventSystem previousEvents;
        GameObject previousSelection;
        float previousTimeScale;

        [SetUp]
        public void BuildOnlyTheRealMapUi()
        {
            previousSession = GameSession.Instance;
            previousCatalog = Art.Catalog;
            previousTimeScale = Time.timeScale;
            previousEvents = EventSystem.current;
            previousSelection = previousEvents ? previousEvents.currentSelectedGameObject : null;
            catalog = ScriptableObject.CreateInstance<GameCatalog>();
            catalog.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Art.Catalog = catalog;

            // Giữ session/HUD bất hoạt để Awake không dựng thế giới hoặc đọc save.
            sessionRoot = new GameObject("MinimapViewportTestSession");
            sessionRoot.SetActive(false);
            var game = sessionRoot.AddComponent<GameSession>();
            game.Player = sessionRoot.AddComponent<PlayerController>();
            GameSession.Instance = game;
            hud = sessionRoot.AddComponent<GameHud>();

            uiRoot = new GameObject("MinimapViewportTestCanvas", typeof(RectTransform));
            uiRoot.SetActive(false);
            uiRoot.AddComponent<Canvas>();
            var safeArea = AddRect("SafeArea", uiRoot.transform);
            playingHud = AddRect("PlayingHud", safeArea);
            SetField("game", game);
            SetField("safeArea", safeArea);
            SetField("hud", playingHud.gameObject);
            foreach (string name in new[] { "menu", "crewPanel", "stockPanel", "cargoPanel", "informationPanel" })
                SetField(name, AddRect(name, safeArea).gameObject);

            typeof(GameHud).GetMethod("BuildMap", PrivateInstance).Invoke(hud, null);
            minimapFrame = (RectTransform)playingHud.Find("Minimap");
            minimapFrame.sizeDelta = new Vector2(300, 220);
            // Chỉ bật cây UI: MaskUtilities bỏ qua tổ tiên RectMask2D bất hoạt.
            uiRoot.SetActive(true);
        }

        [TearDown]
        public void RestoreOwnedObjectsAndGlobalState()
        {
            if (hud)
            {
                foreach (string name in new[] { "mapKey", "menuKey" })
                {
                    var action = GetField<InputAction>(name);
                    action?.Dispose();
                    SetField(name, null);
                }
                var sprite = GetField<Sprite>("rounded");
                var texture = GetField<Texture2D>("roundedTexture");
                SetField("rounded", null);
                SetField("roundedTexture", null);
                if (sprite) Object.DestroyImmediate(sprite);
                if (texture) Object.DestroyImmediate(texture);
            }
            if (uiRoot) Object.DestroyImmediate(uiRoot);
            if (sessionRoot) Object.DestroyImmediate(sessionRoot);
            GameSession.Instance = previousSession;
            Art.Catalog = previousCatalog;
            Time.timeScale = previousTimeScale;
            if (previousEvents && EventSystem.current == previousEvents)
                previousEvents.SetSelectedGameObject(previousSelection);
            if (catalog) Object.DestroyImmediate(catalog);
        }

        [Test]
        public void TerrainUsesTheActiveAncestorViewportMask()
        {
            var terrain = minimapFrame.GetComponentInChildren<CityMapGraphic>(true);
            Assert.That(terrain, Is.Not.Null);
            var mask = MaskUtilities.GetRectMaskForClippable(terrain);
            Assert.That(mask, Is.Not.Null,
                "Route vertices outside local map bounds must be clipped by an active ancestor.");
            Assert.That(mask.transform.parent, Is.SameAs(minimapFrame));
            Assert.That(mask.name, Is.EqualTo("MinimapViewport"));
            Assert.That(terrain.transform.parent, Is.SameAs(mask.transform));
            Assert.That(terrain.maskable, Is.True);
            Assert.That(terrain.Local, Is.True);
            Assert.That(terrain.raycastTarget, Is.False);
            Assert.That(terrain.GetComponent<RectMask2D>(), Is.Null,
                "uGUI ignores a RectMask2D attached to the clippable graphic itself.");
        }

        [Test]
        public void ViewportKeepsInsetsWhileTerrainFillsItAndLabelsStayOutside()
        {
            var viewport = minimapFrame.Find("MinimapViewport") as RectTransform;
            Assert.That(viewport, Is.Not.Null);
            AssertVector(viewport.anchorMin, Vector2.zero);
            AssertVector(viewport.anchorMax, Vector2.one);
            AssertVector(viewport.offsetMin, new Vector2(8, 30));
            AssertVector(viewport.offsetMax, new Vector2(-8, -8));
            var terrain = viewport.Find("MinimapTerrain") as RectTransform;
            Assert.That(terrain, Is.Not.Null);
            AssertVector(terrain.anchorMin, Vector2.zero);
            AssertVector(terrain.anchorMax, Vector2.one);
            AssertVector(terrain.offsetMin, Vector2.zero);
            AssertVector(terrain.offsetMax, Vector2.zero);
            foreach (string name in new[] { "MinimapHelp", "North" })
            {
                var label = minimapFrame.Find(name)?.GetComponent<Text>();
                Assert.That(label, Is.Not.Null, name);
                Assert.That(label.transform.parent, Is.SameAs(minimapFrame), name);
                Assert.That(MaskUtilities.GetRectMaskForClippable(label), Is.Null, name);
            }
        }

        [Test]
        public void MinimapClickStillOpensMapAndCloseRestoresPlayingControlAndClock()
        {
            var button = minimapFrame.GetComponent<Button>();
            Assert.That(button, Is.Not.Null);
            Assert.That(button.targetGraphic, Is.SameAs(minimapFrame.GetComponent<Image>()));
            Assert.That(button.targetGraphic.raycastTarget, Is.True);
            Time.timeScale = .75f;
            Assert.That(hud.MapOpen, Is.False);
            button.onClick.Invoke();
            Assert.That(hud.MapOpen, Is.True);
            Assert.That(playingHud.gameObject.activeSelf, Is.False);
            Assert.That(GameSession.Instance.Player.CanControl, Is.False);
            Assert.That(Time.timeScale, Is.Zero);
            var panel = GetField<GameObject>("mapPanel");
            Assert.That(panel.activeSelf, Is.True);
            panel.transform.Find("ManagementBoard/CloseMap").GetComponent<Button>().onClick.Invoke();
            Assert.That(hud.MapOpen, Is.False);
            Assert.That(panel.activeSelf, Is.False);
            Assert.That(playingHud.gameObject.activeSelf, Is.True);
            Assert.That(GameSession.Instance.Player.CanControl, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(.75f));
        }

        static RectTransform AddRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        static void AssertVector(Vector2 actual, Vector2 expected) =>
            Assert.That(Vector2.Distance(actual, expected), Is.LessThan(.001f));

        void SetField(string name, object value) =>
            typeof(GameHud).GetField(name, PrivateInstance).SetValue(hud, value);

        T GetField<T>(string name) =>
            (T)typeof(GameHud).GetField(name, PrivateInstance).GetValue(hud);
    }
}
