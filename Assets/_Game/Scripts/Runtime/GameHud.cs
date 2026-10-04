using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Tycoon
{
    public sealed class GameHud : MonoBehaviour
    {
        GameSession game;
        Canvas canvas;
        Text money, progress, carry, prompt, toast;
        GameObject menu;
        Button continueButton;
        bool paused;
        static Sprite rounded;
        public void Initialize()
        {
            game = GameSession.Instance;
            var root = new GameObject("HUD"); root.transform.SetParent(transform);
            canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = Camera.main; canvas.planeDistance = .5f;
            var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            root.AddComponent<GraphicRaycaster>();
            var events = new GameObject("EventSystem"); events.AddComponent<EventSystem>(); events.AddComponent<InputSystemUIInputModule>();
            var wallet = Panel("Wallet", root.transform, new Vector2(1,1), new Vector2(1,1), new Vector2(-288,-94), new Vector2(-28,-28), "#275D34", .92f);
            money = Text("Money", wallet.transform, "0", 36, TextAnchor.MiddleLeft, "#FFFFFF"); Rect(money.rectTransform, Vector2.zero, Vector2.one, new Vector2(74,0), new Vector2(-12,0));
            var cashIcon = Panel("CashIcon", wallet.transform, new Vector2(0,.5f), new Vector2(0,.5f), new Vector2(16,-18), new Vector2(57,18), "#A8EE25", 1);
            cashIcon.transform.localRotation = Quaternion.Euler(0,0,15);
            Panel("BillInner",cashIcon.transform,Vector2.zero,Vector2.one,new Vector2(5,5),new Vector2(-5,-5),"#45A722",1);
            var title = Panel("Title", root.transform, new Vector2(0,1), new Vector2(0,1), new Vector2(28,-110), new Vector2(465,-28), "#275D34", .8f);
            progress = Text("Progress", title.transform, "", 22, TextAnchor.MiddleLeft, "#FFFFFF"); Rect(progress.rectTransform, Vector2.zero, Vector2.one, new Vector2(16,8), new Vector2(-8,-8));
            var bag = Panel("Carry", root.transform, Vector2.zero, Vector2.zero, new Vector2(28,55), new Vector2(340,135), "#275D34", .8f);
            carry = Text("CarryText", bag.transform, "", 22, TextAnchor.MiddleLeft, "#FFFFFF"); Rect(carry.rectTransform, Vector2.zero, Vector2.one, new Vector2(16,6), new Vector2(-8,-6));
            var action = Panel("Action", root.transform, new Vector2(.5f,0), new Vector2(.5f,0), new Vector2(-430,55), new Vector2(430,115), "#275D34", .8f);
            prompt = Text("ActionText", action.transform, "", 22, TextAnchor.MiddleCenter, "#FFFFFF"); Rect(prompt.rectTransform, Vector2.zero, Vector2.one, new Vector2(10,0), new Vector2(-10,0));
            var toastRoot = Panel("Toast", root.transform, new Vector2(.5f,1), new Vector2(.5f,1), new Vector2(-500,-100), new Vector2(500,-36), "#275D34", .9f);
            toast = Text("ToastText", toastRoot.transform, "", 22, TextAnchor.MiddleCenter, "#FFFFFF"); Rect(toast.rectTransform, Vector2.zero, Vector2.one, new Vector2(12,0), new Vector2(-12,0));
            var help = Text("Help", root.transform, "WASD / tay cầm • tự thao tác khi đứng gần • E: thao tác • R: lấy từ kho • Q: chọn hàng • F5: lưu • Esc: menu", 18, TextAnchor.MiddleCenter, "#FFFFFF");
            Rect(help.rectTransform, Vector2.zero, new Vector2(1,0), new Vector2(10,10), new Vector2(-10,42));
            menu = Panel("Pause", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "#143627", .95f);
            var heading = Text("PauseHeading", menu.transform, "TYCOON2\nNông trại → Đế chế kinh doanh", 40, TextAnchor.MiddleCenter, "#FFFFFF");
            Rect(heading.rectTransform, new Vector2(.5f,.5f), new Vector2(.5f,.5f), new Vector2(-500,120), new Vector2(500,290));
            continueButton = Button(menu.transform,"Tiếp tục",40,TogglePause);
            Button(menu.transform,"Lưu trò chơi",-55,()=>game.SaveGame());
            Button(menu.transform,"Lưu & thoát",-150,()=> { game.SaveGame(); Application.Quit(); });
            menu.SetActive(false);
        }
        void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true || Gamepad.current?.selectButton.wasPressedThisFrame == true) TogglePause();
            money.text = game.Economy.Money.ToString("N0");
            string area = game.BusinessStage switch { 5 => "Nhà hàng & tiệm bánh", 4 => "Tiệm bánh", 3 => "Siêu thị", 2 => "Chế biến", _ => "Nông trại & cửa hàng" };
            progress.text = area + "\n" + game.Workers.Count + " nhân viên • " + game.Economy.Transactions + " lượt bán";
            carry.text = "Hàng: " + game.Player.Carry.Total + "/" + game.Player.Carry.Capacity + "\nKho: " + Definitions.Items[game.SelectedItem].label;
            var station = game.NearestStation(game.Player.transform.position);
            prompt.text = station ? station.Prompt : "Thu hoạch → xếp lên quầy → phục vụ khách → nhận tiền";
            toast.transform.parent.gameObject.SetActive(Time.time < game.ToastUntil); toast.text = game.Toast;
        }
        public void TogglePause()
        {
            paused = !paused; menu.SetActive(paused); Time.timeScale = paused ? 0 : 1; game.Player.CanControl = !paused;
            if (paused) EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }
        static GameObject Panel(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, string color, float alpha)
        {
            var root = new GameObject(name, typeof(RectTransform)); root.transform.SetParent(parent,false);
            var image = root.AddComponent<Image>(); var tint=Art.Hex(color); tint.a=alpha; image.color=tint;
            if(!rounded)
            {
                var texture=new Texture2D(32,32,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;
                for(int y=0;y<32;y++)for(int x=0;x<32;x++)
                {float dx=Mathf.Max(11-x,x-20,0),dy=Mathf.Max(11-y,y-20,0);texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(11.5f-Mathf.Sqrt(dx*dx+dy*dy))));}
                texture.Apply();rounded=Sprite.Create(texture,new UnityEngine.Rect(0,0,32,32),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(12,12,12,12));
            }
            image.sprite=rounded;image.type=Image.Type.Sliced;
            Rect((RectTransform)root.transform,min,max,offsetMin,offsetMax); return root;
        }
        static Text Text(string name, Transform parent, string value, int size, TextAnchor alignment, string color)
        {
            var root = new GameObject(name,typeof(RectTransform)); root.transform.SetParent(parent,false); var text=root.AddComponent<Text>();
            text.font=Art.Catalog.font; text.text=value; text.fontSize=size; text.alignment=alignment; text.color=Art.Hex(color); text.raycastTarget=false; return text;
        }
        static Button Button(Transform parent, string label, float y, UnityEngine.Events.UnityAction click)
        {
            var root=Panel(label,parent,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-210,y-30),new Vector2(210,y+40),"#59C840",1);
            var button=root.AddComponent<Button>(); button.onClick.AddListener(click);
            var text=Text("Label",root.transform,label,30,TextAnchor.MiddleCenter,"#FFFFFF"); Rect(text.rectTransform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero); return button;
        }
        static void Rect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        { rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=offsetMin;rect.offsetMax=offsetMax; }
    }
}
