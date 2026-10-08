using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tycoon
{
    [DefaultExecutionOrder(150)]
    public sealed class OrderBubbleLayer : MonoBehaviour
    {
        static OrderBubbleLayer instance;
        readonly List<OrderBubbleView> active = new();
        readonly Stack<OrderBubbleView> pool = new();
        RectTransform canvasRect;
        Canvas canvas;
        Camera worldCamera;

        public static OrderBubbleView Acquire(Transform target)
        {
            if (!instance)
            {
                var root = new GameObject("SharedOrderBubbles", typeof(RectTransform));
                if (GameSession.Instance) root.transform.SetParent(GameSession.Instance.transform, false);
                instance = root.AddComponent<OrderBubbleLayer>();
                instance.Initialize();
            }
            var view = instance.pool.Count > 0 ? instance.pool.Pop() : new OrderBubbleView(instance.transform);
            view.Bind(target);
            instance.active.Add(view);
            return view;
        }

        public static void Release(ref OrderBubbleView view)
        {
            if (view == null) return;
            if (instance && instance.active.Remove(view))
            {
                view.Reset();
                instance.pool.Push(view);
            }
            view = null;
        }

        void Initialize()
        {
            canvasRect = (RectTransform)transform;
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            canvas.scaleFactor = Mathf.Sqrt(Screen.width / 1920f * (Screen.height / 1080f));
            worldCamera = Camera.main;
        }

        void LateUpdate()
        {
            using var timing = QaFrameProbe.Measure("OrderBubbleLayer.LateUpdate");
            if (!worldCamera) worldCamera = Camera.main;
            RefreshProjection(worldCamera, Screen.safeArea);
        }

        public void RefreshProjection(Camera camera, Rect visibleRect)
        {
            bool playing = !GameSession.Instance || !GameSession.Instance.Hud ||
                GameSession.Instance.Hud.AllowsPlayerControl;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var view = active[i];
                if (!view.Target || !view.Target.gameObject.activeInHierarchy)
                {
                    active.RemoveAt(i);
                    view.Reset();
                    pool.Push(view);
                    continue;
                }
                bool visible = playing && camera && view.HasContent;
                if (visible)
                {
                    Vector3 screen = camera.WorldToScreenPoint(view.Target.position + Vector3.up * 2.5f);
                    visible = screen.z > 0 && visibleRect.Contains(screen);
                    if (visible)
                    {
                        Vector2 half = view.Rect.rect.size * (canvas.scaleFactor * .5f);
                        screen.y += 48 * canvas.scaleFactor;
                        // Giữ trọn viền bong bóng trong màn hình khi khách đi sát mép.
                        screen.x = Mathf.Clamp(screen.x, visibleRect.xMin + half.x + 4,
                            visibleRect.xMax - half.x - 4);
                        screen.y = Mathf.Clamp(screen.y, visibleRect.yMin + half.y + 4,
                            visibleRect.yMax - half.y - 4);
                        var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                            ? null : canvas.worldCamera;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen,
                            uiCamera, out var point);
                        if ((view.Rect.anchoredPosition - point).sqrMagnitude > .01f)
                            view.Rect.anchoredPosition = point;
                    }
                }
                if (view.Rect.gameObject.activeSelf != visible) view.Rect.gameObject.SetActive(visible);
            }
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
