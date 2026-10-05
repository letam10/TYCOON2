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
        GameSession game;
        Canvas canvas;
        Text money, progress, objective, carry, prompt, toast,finance,stock;
        float nextStatusRefresh;
        readonly Dictionary<string,Text> crewStatuses=new();
        readonly StringBuilder stockLines=new();
        GameObject menu,crewPanel;
        Button continueButton;
        Transform crewContent;
        Text crewSummary;
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
            var financePanel=Panel("Finance",root.transform,Vector2.one,Vector2.one,new Vector2(-355,-245),new Vector2(-28,-106),"#275D34",.9f);
            finance=Text("FinanceText",financePanel.transform,"",20,TextAnchor.MiddleLeft,"#FFFFFF");Rect(finance.rectTransform,Vector2.zero,Vector2.one,new Vector2(14,5),new Vector2(-12,-5));
            var stockPanel=Panel("AreaStock",root.transform,new Vector2(1,0),new Vector2(1,0),new Vector2(-355,145),new Vector2(-28,440),"#275D34",.86f);
            stock=Text("StockText",stockPanel.transform,"",18,TextAnchor.UpperLeft,"#FFFFFF");Rect(stock.rectTransform,Vector2.zero,Vector2.one,new Vector2(14,10),new Vector2(-12,-10));
            var title = Panel("Title", root.transform, new Vector2(0,1), new Vector2(0,1), new Vector2(28,-110), new Vector2(465,-28), "#275D34", .8f);
            progress = Text("Progress", title.transform, "", 22, TextAnchor.MiddleLeft, "#FFFFFF"); Rect(progress.rectTransform, Vector2.zero, Vector2.one, new Vector2(16,8), new Vector2(-8,-8));
            var objectivePanel=Panel("Objective",root.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(28,-220),new Vector2(685,-112),"#275D34",.82f);
            objective=Text("ObjectiveText",objectivePanel.transform,"",17,TextAnchor.MiddleLeft,"#FFFFFF");Rect(objective.rectTransform,Vector2.zero,Vector2.one,new Vector2(13,5),new Vector2(-13,-5));
            var bag = Panel("Carry", root.transform, Vector2.zero, Vector2.zero, new Vector2(28,55), new Vector2(340,135), "#275D34", .8f);
            carry = Text("CarryText", bag.transform, "", 22, TextAnchor.MiddleLeft, "#FFFFFF"); Rect(carry.rectTransform, Vector2.zero, Vector2.one, new Vector2(16,6), new Vector2(-8,-6));
            var action = Panel("Action", root.transform, new Vector2(.5f,0), new Vector2(.5f,0), new Vector2(-430,55), new Vector2(430,115), "#275D34", .8f);
            prompt = Text("ActionText", action.transform, "", 22, TextAnchor.MiddleCenter, "#FFFFFF"); Rect(prompt.rectTransform, Vector2.zero, Vector2.one, new Vector2(10,0), new Vector2(-10,0));
            var toastRoot = Panel("Toast", root.transform, new Vector2(.5f,1), new Vector2(.5f,1), new Vector2(-500,-100), new Vector2(500,-36), "#275D34", .9f);
            toast = Text("ToastText", toastRoot.transform, "", 22, TextAnchor.MiddleCenter, "#FFFFFF"); Rect(toast.rectTransform, Vector2.zero, Vector2.one, new Vector2(12,0), new Vector2(-12,0));
            var help = Text("Help", root.transform, "WASD / cần trái: di chuyển • Đứng trong vùng để thao tác • Q / RB: chọn hàng • F5 / Start: lưu • Esc / Back: menu", 18, TextAnchor.MiddleCenter, "#FFFFFF");
            Rect(help.rectTransform, Vector2.zero, new Vector2(1,0), new Vector2(10,10), new Vector2(-10,42));
            menu = Panel("Pause", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "#143627", .95f);
            var heading = Text("PauseHeading", menu.transform, "TYCOON2\nNông trại → Đế chế kinh doanh", 40, TextAnchor.MiddleCenter, "#FFFFFF");
            Rect(heading.rectTransform, new Vector2(.5f,.5f), new Vector2(.5f,.5f), new Vector2(-500,120), new Vector2(500,290));
            continueButton = Button(menu.transform,"Tiếp tục",75,TogglePause);
            Button(menu.transform,"Quản lý đội",-5,OpenCrewMenu);
            Button(menu.transform,"Lưu trò chơi",-85,()=>game.SaveGame());
            Button(menu.transform,"Lưu & thoát",-165,()=> { game.SaveGame(); Application.Quit(); });
            menu.SetActive(false);
            if(DevelopmentAssistance.Enabled)
            {
                var support=Button(root.transform,"Hỗ trợ +999.999 / mở khu",0,()=>{if(DevelopmentAssistance.Apply(game))game.Say("Đã cộng 999.999 xu và mở tuyến cơ bản.");});
                Rect(support.GetComponent<RectTransform>(),new Vector2(1,1),new Vector2(1,1),new Vector2(-640,-125),new Vector2(-28,-28));
            }
            crewPanel=Panel("CrewManagement",root.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,"#143627",.97f);
            var crewTitle=Text("CrewTitle",crewPanel.transform,"ĐỘI NGŨ • VAI TRÒ VÀ KHU VỰC",34,TextAnchor.MiddleCenter,"#FFFFFF");
            Rect(crewTitle.rectTransform,new Vector2(.1f,.88f),new Vector2(.9f,.97f),Vector2.zero,Vector2.zero);
            crewSummary=Text("CrewSummary",crewPanel.transform,"",18,TextAnchor.MiddleCenter,"#FFFFFF");
            Rect(crewSummary.rectTransform,new Vector2(.1f,.81f),new Vector2(.9f,.88f),Vector2.zero,Vector2.zero);
            var viewport=new GameObject("CrewViewport",typeof(RectTransform),typeof(Image),typeof(Mask));viewport.transform.SetParent(crewPanel.transform,false);
            Rect(viewport.GetComponent<RectTransform>(),new Vector2(.08f,.17f),new Vector2(.92f,.8f),Vector2.zero,Vector2.zero);
            viewport.GetComponent<Image>().color=new Color(0,0,0,.12f);viewport.GetComponent<Mask>().showMaskGraphic=false;
            var content=new GameObject("CrewRows",typeof(RectTransform));content.transform.SetParent(viewport.transform,false);crewContent=content.transform;
            var contentRect=content.GetComponent<RectTransform>();contentRect.anchorMin=new Vector2(0,1);contentRect.anchorMax=new Vector2(1,1);contentRect.pivot=new Vector2(.5f,1);contentRect.anchoredPosition=Vector2.zero;contentRect.sizeDelta=Vector2.zero;
            var scroll=viewport.AddComponent<ScrollRect>();scroll.viewport=viewport.GetComponent<RectTransform>();scroll.content=contentRect;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            Button(crewPanel.transform,"Quay lại",-435,BackToPauseMenu);
            crewPanel.SetActive(false);
        }
        void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true || Gamepad.current?.selectButton.wasPressedThisFrame == true) TogglePause();
            money.text = game.Economy.Money.ToString("N0");
            string area = game.BusinessStage switch { 5 => "Nhà hàng & tiệm bánh", 4 => "Tiệm bánh", 3 => "Siêu thị", 2 => game.Economy.Has("mill")?"Chế biến":"Cửa hàng nông sản", _ => "Nông trại & cửa hàng" };
            progress.text = area + "\n" + game.Workers.Count + " nhân viên • " + game.Economy.Transactions + " lượt bán";
            objective.text=game.ObjectiveText;
            var carried = game.Player.Carry.Snapshot();
            carry.text = (carried.Count == 0 ? "Giỏ trống" : Definitions.Item(carried[0].id).label) + ": " + game.Player.Carry.Total + "/" + game.Player.Carry.Capacity + "\nChọn lấy: " + Definitions.Items[game.SelectedItem].label;
            var station = game.NearestStation(game.Player.transform.position);
            prompt.text = !string.IsNullOrEmpty(game.Player.InteractionReason) ? game.Player.InteractionReason : game.Player.ActiveInteraction is ProximityTarget target ? target.Cash?"Thu tiền tại cọc tiền":target.Target.Prompt : station ? station.Prompt : "Dừng gần vật thể để thao tác tự động";
            toast.transform.parent.gameObject.SetActive(Time.time < game.ToastUntil); toast.text = game.Toast;
            if(Time.unscaledTime>=nextStatusRefresh){nextStatusRefresh=Time.unscaledTime+.3f;RefreshStatus();}
        }
        void RefreshStatus()
        {
            finance.text="Chưa thu: "+game.Economy.PendingCash.ToString("N0")+" xu\nDoanh thu: "+game.Economy.Revenue.ToString("N0")+" xu\nThực thu: "+game.Economy.CashCollected.ToString("N0")+" xu\nThất thoát: "+game.Economy.LostItems+" món";
            var nearby=game.Stations.Where(x=>x&&x.IsUnlocked&&x is not StationZone and not PurchasePad and not ConveyorStation)
                .OrderBy(x=>(x.transform.position-game.Player.transform.position).sqrMagnitude).FirstOrDefault();
            string area=nearby?.AreaId??"farm";var storage=game.StorageFor(area);
            if(!storage||!storage.IsUnlocked){area="farm";storage=game.StorageFor(area);}stockLines.Clear();stockLines.AppendLine("KHO • "+AreaLabel(area));
            if(storage)
            {
                int rows=0;
                foreach(var item in Definitions.Items)
                {
                    int total=storage.Inventory.Count(item.id),held=storage.Inventory.Reserved(item.id),incoming=storage.Inventory.ReservedSpace(item.id);
                    if(total+held+incoming==0)continue;
                    if(++rows>7){stockLines.AppendLine("… các hàng khác trong khu");break;}
                    stockLines.Append(item.label).Append(": ").Append(total).Append(" / ").Append(storage.Inventory.Capacity);
                    if(held+incoming>0)stockLines.Append(" • giữ ").Append(held).Append(" / đến ").Append(incoming);
                    stockLines.AppendLine();
                }
                if(rows==0)stockLines.AppendLine("Kho trống");
            }
            foreach(var route in game.Stations.OfType<ConveyorStation>().Where(x=>x.AreaId==area&&x.IsUnlocked).Take(2))stockLines.AppendLine(route.Prompt);
            var waiting=game.Workers.FirstOrDefault(x=>x&&GameSession.CrewFor(Definitions.Upgrade(x.UpgradeId)).area==area&&x.Reason.StartsWith("Chờ"));
            if(waiting)stockLines.AppendLine("Đội: "+waiting.Reason);
            stock.text=stockLines.ToString();
            if(crewPanel.activeSelf)foreach(var row in crewStatuses)
                row.Value.text=string.Join(" • ",game.Workers.Where(x=>x&&x.UpgradeId==row.Key).Select(x=>x.Reason).Distinct().Take(3));
        }
        public void TogglePause()
        {
            crewPanel?.SetActive(false);paused = !paused; menu.SetActive(paused); Time.timeScale = paused ? 0 : 1; game.Player.CanControl = !paused;
            if (paused) EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }
        void OpenCrewMenu()
        {
            menu.SetActive(false);crewPanel.SetActive(true);RefreshCrewMenu();
        }
        void BackToPauseMenu()
        {
            crewPanel.SetActive(false);menu.SetActive(true);EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }
        void RefreshCrewMenu()
        {
            foreach(Transform child in crewContent)Destroy(child.gameObject);
            crewStatuses.Clear();
            var crews=game.CrewStates;
            if(crews.Count==0)
            {
                crewSummary.text="Chưa có đội. Trạm cấp 3 + 30 công việc bạn tự hoàn thành để mở ô thuê.";
                crewContent.GetComponent<RectTransform>().sizeDelta=new Vector2(0,crewContent.parent.GetComponent<RectTransform>().rect.height);
                var empty=Text("NoCrew",crewContent,"Bạn vẫn có thể tự sản xuất, vận chuyển và phục vụ.",20,TextAnchor.MiddleCenter,"#FFFFFF");
                empty.transform.SetParent(crewContent,false);Rect(empty.rectTransform,new Vector2(.05f,.35f),new Vector2(.95f,.65f),Vector2.zero,Vector2.zero);return;
            }
            crewSummary.text="Mỗi nâng cấp chỉ tác động đội cùng nghề và khu. Tiền được trừ ngay khi chọn.";
            int index=0;
            foreach(var crew in crews)
            {
                var row=new GameObject("CrewRow_"+crew.id,typeof(RectTransform),typeof(Image));row.transform.SetParent(crewContent,false);
                var rect=row.GetComponent<RectTransform>();rect.anchorMin=new Vector2(0,1);rect.anchorMax=new Vector2(1,1);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-index*152);rect.sizeDelta=new Vector2(0,144);
                var image=row.GetComponent<Image>();image.color=new Color(.15f,.36f,.2f,.9f);image.raycastTarget=false;
                string title=RoleLabel(crew.role)+" • "+AreaLabel(crew.area)+" • "+crew.count+" người";
                var name=Text("CrewName",row.transform,title,22,TextAnchor.MiddleLeft,"#FFFFFF");Rect(name.rectTransform,new Vector2(.02f,.7f),new Vector2(.98f,.98f),Vector2.zero,Vector2.zero);
                var status=Text("CrewStatus",row.transform,"",17,TextAnchor.MiddleLeft,"#CBE9BA");Rect(status.rectTransform,new Vector2(.02f,.43f),new Vector2(.98f,.72f),Vector2.zero,Vector2.zero);crewStatuses[crew.id]=status;
                CrewButton(row.transform,.18f,UpgradeLabel(crew,"speed"),()=>UpgradeCrew(crew.id,"speed"));
                CrewButton(row.transform,.50f,crew.role=="Cashier"?"Không cần sức mang":UpgradeLabel(crew,"carry"),crew.role=="Cashier"?null:()=>UpgradeCrew(crew.id,"carry"));
                CrewButton(row.transform,.82f,UpgradeLabel(crew,"count"),()=>UpgradeCrew(crew.id,"count"));
                index++;
            }
            var size=crewContent.GetComponent<RectTransform>().sizeDelta;crewContent.GetComponent<RectTransform>().sizeDelta=new Vector2(size.x,Mathf.Max(crewContent.parent.GetComponent<RectTransform>().rect.height,index*152));
        }
        string UpgradeLabel(CrewState crew,string type)
        {
            int level=type=="speed"?crew.speedLevel:type=="carry"?crew.carryLevel:crew.count;
            if(level>=3)return (type=="speed"?"Tốc độ":type=="carry"?"Sức mang":"Số người")+" • TỐI ĐA";
            string next=type=="speed"?"Tốc độ cấp "+(level+1):type=="carry"?"Sức mang cấp "+(level+1):"Thêm người • "+(crew.count+1);
            return next+" • "+game.CrewUpgradeCost(crew.id,type)+" xu";
        }
        void CrewButton(Transform parent,float center,string label,UnityEngine.Events.UnityAction click)
        {
            var buttonRoot=Panel("CrewUpgrade",parent,new Vector2(center-.15f,.05f),new Vector2(center+.15f,.39f),Vector2.zero,Vector2.zero,"#59C840",click==null?.35f:1);
            var button=buttonRoot.AddComponent<Button>();button.interactable=click!=null;if(click!=null)button.onClick.AddListener(click);
            var text=Text("Label",buttonRoot.transform,label,17,TextAnchor.MiddleCenter,"#FFFFFF");Rect(text.rectTransform,Vector2.zero,Vector2.one,new Vector2(3,0),new Vector2(-3,0));
        }
        void UpgradeCrew(string id,string type)
        {
            bool changed=game.UpgradeCrew(id,type);game.Say(changed?"Đã nâng đội "+(Definitions.Upgrade(id)?.label??id):"Không đủ xu hoặc nâng cấp đã tối đa.");RefreshCrewMenu();
        }
        public static string AreaLabel(string area)=>area switch{"farm"=>"Nông trại","farm_shop"=>"Cửa hàng nông sản","processing"=>"Chế biến","supermarket" or "market"=>"Siêu thị","bakery"=>"Tiệm bánh","restaurant"=>"Nhà hàng",_=>area};
        static string RoleLabel(string role)=>role switch{"Farmer"=>"Nông dân","AnimalWorker"=>"Chăm vật nuôi","Restocker"=>"Xếp hàng","Cashier"=>"Bán hàng","Processor"=>"Chế biến","Cook"=>"Đầu bếp / thợ bánh","Waiter"=>"Phục vụ","Transporter"=>"Vận chuyển",_=>role};
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
