using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class GameSession : MonoBehaviour
    {
        public static GameSession Instance;
        public GameCatalog Catalog;
        [NonSerialized] public Economy Economy = new();
        public PlayerController Player;
        [NonSerialized] public ItemPool Pool;
        public FollowCamera CameraRig;
        public GameHud Hud;
        public readonly List<Station> Stations = new();
        public readonly List<ProductionStation> Producers = new();
        public readonly List<ShelfStation> Shelves = new();
        public readonly List<MachineStation> Machines = new();
        public readonly List<CheckoutStation> Checkouts = new();
        public readonly List<WorkerAgent> Workers = new();
        public CommerceDirector Commerce;
        public GameFeedback Feedback;
        public RestaurantDirector Restaurant;
        public readonly List<TableStation> Tables = new();
        public List<DinerSave> PendingDiners = new();
        public bool NavigationReady;
        public int BakerySales, RestaurantMeals;
        public int BusinessStage => Economy.Has("restaurant") ? 5 : Economy.Has("bakery") ? 4 : Economy.Has("supermarket") ? 3 : Economy.Has("mill") ? 2 : 1;
        float nextAutosave;
        public StorageStation Storage;
        public int SelectedItem;
        public long NextReceipt = 1;
        public bool IsQa;
        public string QaDirectory;
        public string SavePath;
        public int Milestone;
        public string Toast = "";
        public float ToastUntil;
        public List<string> RuntimeErrors = new();
        public RuntimeTransactions Transactions { get; private set; }
        public bool CanSimulate => !SaveBlocked;

        void Awake()
        {
            Instance = this;
            Application.logMessageReceived += OnLog;
            string[] args = Environment.GetCommandLineArgs();
            IsQa = Array.Exists(args, x => x.StartsWith("--qa"));
            if (IsQa) UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior =
                UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            Milestone = Array.Exists(args, x => x is "--qa-m0" or "--qa-art") ? 0 : 1;
            QaDirectory = Argument("--qa-output", Path.Combine(Application.persistentDataPath, "QA"));
            SavePath = IsQa ? Path.Combine(QaDirectory, "qa-save-v2.json") : Path.Combine(Application.persistentDataPath, "save-v2.json");
            if (Array.Exists(args, x => x == "--qa-load")) SavePath = Argument("--qa-load-from", SavePath);
            if (Array.Exists(args, x => x == "--qa-resume")) SavePath = Argument("--qa-resume-from", SavePath);
            Directory.CreateDirectory(QaDirectory);
            var device = new GraphicsAudit {
                name = SystemInfo.graphicsDeviceName, vendor = SystemInfo.graphicsDeviceVendor,
                api = SystemInfo.graphicsDeviceType.ToString(), memoryMiB = SystemInfo.graphicsMemorySize,
                width = Screen.width, height = Screen.height, started = DateTime.UtcNow.ToString("o")
            };
            if (IsQa) File.WriteAllText(Path.Combine(QaDirectory, "graphics-device.json"), JsonUtility.ToJson(device, true));
            if (Array.Exists(args, x => x == "--require-4060") && !device.name.Contains("4060"))
            {
                Debug.LogError("RTX 4060 bắt buộc; backend thực tế: " + device.name);
                Application.Quit(3);
                enabled = false; return;
            }
            if (Array.Exists(args, x => x == "--qa-gpu-probe"))
            {
                Debug.Log("GPU_PROBE " + device.name);
                Application.Quit(device.name.Contains("4060") ? 0 : 3);
                enabled = false; return;
            }
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Array.Exists(args, x => x == "--qa-benchmark") ? -1 : 165;
        }
        void Start()
        {
            if (!enabled) return;
            Catalog = Resources.Load<GameCatalog>("GameCatalog");
            if (!Catalog) throw new InvalidOperationException("Thiếu GameCatalog.");
            Art.Catalog = Catalog;
            Pool = new ItemPool(transform);
            WorldFactory.Build(this);
            var playerRoot = new GameObject("Player");
            playerRoot.transform.position = new Vector3(-3, .05f, -4);
            var actor = Art.Model("player", Vector3.zero, playerRoot.transform);
            actor.AddComponent<ActorView>();
            Player = playerRoot.AddComponent<PlayerController>();
            Player.Initialize();
            // Không nhận input khi save đang được khôi phục và core chưa sẵn sàng.
            Player.CanControl = false;
            var basket = new GameObject("CarryStack");
            basket.transform.SetParent(playerRoot.transform, false);
            basket.transform.localPosition = new Vector3(0, .8f, -.5f);
            var stack = basket.AddComponent<InventoryStack>();
            stack.Inventory = Player.Carry; stack.Pool = Pool; stack.Maximum = 24;
            stack.Columns = 1; stack.Rows = 1; stack.Scale = .85f; stack.Spacing = .18f; stack.LayerHeight = .31f;
            var ring = playerRoot.AddComponent<LineRenderer>(); ring.sharedMaterial = Art.Material("#78FF2D"); ring.useWorldSpace = false; ring.loop = true; ring.widthMultiplier = .04f; ring.positionCount = 32;
            for (int i = 0; i < 32; i++) ring.SetPosition(i, new Vector3(Mathf.Cos(i * Mathf.PI / 16) * .65f, .045f, Mathf.Sin(i * Mathf.PI / 16) * .65f));
            var cameraObject = new GameObject("MainCamera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 45; camera.nearClipPlane = .15f; camera.farClipPlane = 180;
            camera.backgroundColor = Art.Hex("#80D94A"); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = true;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;
            CameraRig = cameraObject.AddComponent<FollowCamera>(); CameraRig.Target = playerRoot.transform; CameraRig.Snap();
            Hud = gameObject.AddComponent<GameHud>(); Hud.Initialize();
            bool baseline=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--qa-baseline");
            if (File.Exists(SavePath)&&!baseline) LoadGame();
            ApplyProgression();
            if (!baseline && Transactions == null && !SaveBlocked)
                try { InitializeTransactions(); Player.CanControl = !SaveBlocked; } catch(Exception error) { BlockRecovery(error); }
            Feedback = gameObject.AddComponent<GameFeedback>();
            Commerce = gameObject.AddComponent<CommerceDirector>();
            Restaurant = gameObject.AddComponent<RestaurantDirector>();
            if(IsQa&&Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--qa-stage23"))gameObject.AddComponent<QaStages>();
            else if(IsQa&&baseline)gameObject.AddComponent<QaBaseline>();
            else if (IsQa) gameObject.AddComponent<QaDriver>();
            Say("Đứng gần để thu / xếp hàng • đến quầy nhận tiền • WASD di chuyển");
        }
        void Update()
        {
            if (!Player || !NavigationReady || !CanSimulate) return;
            SyncWorkers();
            TickEvents();
            if (!IsQa && Time.time > nextAutosave) { nextAutosave = Time.time + 60; SaveGame(); }
        }
        public void SyncWorkers()
        {
            foreach (var crew in CrewStates)
            {
                for (int slot=0; slot<crew.count; slot++)
                {
                    string key=crew.id+":"+slot;
                    if (Workers.Exists(x=>x.WorkerId==key)) continue;
                    var saved=PendingWorkers.Find(x=>x.id==key);
                    var storage=StorageFor(crew.area);
                    if(!storage)continue;
                    var root=new GameObject("Worker_"+key); root.transform.SetParent(transform);
                    Vector3 position=saved!=null?new Vector3(saved.x,.05f,saved.z):storage.InteractionPoint+Vector3.left*(2+slot);
                    if (UnityEngine.AI.NavMesh.SamplePosition(position,out var hit,3,UnityEngine.AI.NavMesh.AllAreas)) position=hit.position;
                    root.transform.position=position;
                    var model=Art.Model("player",Vector3.zero,root.transform);model.AddComponent<ActorView>();
                    var worker=root.AddComponent<WorkerAgent>();worker.Initialize(Definitions.Upgrade(crew.id));worker.WorkerId=key;Workers.Add(worker);
                    Transactions?.BindWorker(worker,saved);
                    if(saved!=null){worker.Restore(saved);PendingWorkers.Remove(saved);}
                }
            }
        }
        public static string Argument(string key, string defaultValue)
        {
            var args = Environment.GetCommandLineArgs();
            for (int index = 0; index < args.Length - 1; index++) if (args[index] == key) return args[index + 1];
            return defaultValue;
        }
        public IEnumerable<IPlayerInteractionArea> PlayerInteractionAreas()
        {
            foreach (var station in Stations)
                if (station && station is IPlayerInteractionArea area) yield return area;
        }
        public Station NearestStation(Vector3 position)
        {
            Station nearest = null; float distance = float.PositiveInfinity;
            foreach (var station in Stations)
            {
                if (!station || station.PlayerUsesZones || !station.ContainsInteractionPoint(position) || (!station.IsUnlocked && !(station is PurchasePad))) continue;
                if (station is PurchasePad pad && !CanPurchase(pad.Upgrade, out _)) continue;
                float square = (station.InteractionPoint - position).sqrMagnitude;
                if (square < distance) { distance = square; nearest = station; }
            }
            return nearest;
        }
        public void Say(string message) { Toast = message; ToastUntil = Time.time + 4; }
        public void OnPurchased(UpgradeDefinition upgrade)
        {
            if (Transactions == null && upgrade.kind == "worker" && !CrewStates.Exists(x => x.id == upgrade.id)) CrewStates.Add(CrewFor(upgrade));
            ApplyProgression();
            Say(upgrade.label + " hoàn tất");
        }
        public void SaveGame()
        {
            if(SaveBlocked){Say("Save đang bị chặn vì file tải không hợp lệ");return;}
            try { if(Transactions!=null)Transactions.Checkpoint();else SaveStore.Write(SavePath,CaptureSaveData());Say("Đã lưu trò chơi"); }
            catch(Exception error){Say("Không lưu được: "+error.Message);Debug.LogException(error);}
        }
        public SaveData CaptureSaveData()
        {
            var data = new SaveData {
                money = Economy.Money, revenue = Economy.Revenue, transactions = Economy.Transactions,
                nextReceipt = NextReceipt, savedAt = DateTime.UtcNow.ToString("o"),
                pendingCash = Economy.PendingCash, businessStage = BusinessStage, bakerySales = BakerySales, restaurantMeals = RestaurantMeals,
                playerX = Player.transform.position.x, playerZ = Player.transform.position.z
            };
            data.unlocked.AddRange(Economy.Unlocked);
            data.purchases.AddRange(Purchases); data.crews.AddRange(CrewStates); data.events=Events;
            data.receipts.AddRange(Economy.ReceiptIds); data.losses.AddRange(Economy.Losses);
            foreach(var s in Stations) if(!(s is StationZone)) data.stationStates.Add(s.CaptureProgress());
            data.inventories.Add(new InventorySave("player", Player.Carry));
            foreach (var station in Stations)
            {
                if (station.Inventory != null) data.inventories.Add(new InventorySave(station.Id, station.Inventory));
                if (station is MachineStation machine) data.inventories.Add(new InventorySave(station.Id+"_input",machine.Input));
                if (station is CheckoutStation checkout) data.cash.Add(new CashSave { id = station.Id, amount = checkout.Cash });
            }
            foreach (var worker in Workers) data.workers.Add(worker.Snapshot());
            if (Commerce) foreach (var customer in Commerce.Customers) data.customers.Add(customer.Snapshot());
            if (Restaurant) foreach (var diner in Restaurant.Diners) data.diners.Add(diner.Snapshot());
            // Actor chưa spawn vẫn giữ hàng khi người chơi lưu lại ngay sau load.
            data.workers.AddRange(PendingWorkers);data.customers.AddRange(PendingCustomers);data.diners.AddRange(PendingDiners);
            return data;
        }
        public void InitializeTransactions(TransactionState state=null,bool persist=true)
        {
            foreach(var machine in Machines)machine.ConfigureInputLimits();
            foreach(var station in Stations)if(station is StorageStation storage&&storage.Inventory!=null)storage.ConfigureItemBins(storage.Inventory.Capacity);
            var initial=state??RuntimeTransactions.Migrate(this,CaptureSaveData());
            RuntimeTransactions.UpgradeCounterOwners(initial,this);
            Transactions = new RuntimeTransactions(this,initial,persist);
            Transactions.CompletePurchases();
            foreach(var order in Transactions.Snapshot().orders)if(long.TryParse(order.id.Substring(6),out long receipt))Transactions.Settle(receipt);
        }
        public void BlockRecovery(Exception error)
        { SaveBlocked=true;if(Player)Player.CanControl=false;Say("Gameplay dừng: "+error.Message);Debug.LogException(error); }
        public bool SaveBlocked { get; private set; }
        public void LoadGame()
        {
            try
            {
                var data = SaveStore.Read(SavePath);
                if (data == null) return;
                ValidateSaveOwners(data);
                Player.StopInteraction();
                Transactions?.Detach();Transactions=null;
                foreach(var worker in Workers){worker.gameObject.SetActive(false);Destroy(worker.gameObject);}
                Workers.Clear();Commerce?.ResetForLoad();Restaurant?.ResetForLoad();
                foreach(var counter in Checkouts){counter.Queue.Clear();counter.Cash=0;}
                foreach(var table in Tables)table.Occupant=null;
                Economy.Restore(data.money, data.revenue, data.transactions, data.unlocked, data.pendingCash, data.receipts, data.losses);
                Purchases=data.purchases; CrewStates=data.crews; Events=data.events ?? new EventState();
                PendingCustomers=data.customers; PendingWorkers=data.workers;
                BakerySales = data.bakerySales; RestaurantMeals = data.restaurantMeals;
                PendingDiners = data.diners;
                NextReceipt = Math.Max(1, data.nextReceipt);
                foreach(long receipt in data.receipts)NextReceipt=Math.Max(NextReceipt,receipt+1);
                foreach(var loss in data.losses)NextReceipt=Math.Max(NextReceipt,loss.receipt+1);
                foreach(var customer in data.customers)NextReceipt=Math.Max(NextReceipt,customer.receipt+1);
                foreach(var diner in data.diners)NextReceipt=Math.Max(NextReceipt,diner.receipt+1);
                foreach (var inventory in data.inventories)
                {
                    if (inventory.id == "player") { Player.Carry.Restore(inventory.items); continue; }
                    var station = Stations.Find(x => x.Id == inventory.id);
                    station?.Inventory?.Restore(inventory.items);
                    if (inventory.id.EndsWith("_input") && Stations.Find(x => x.Id + "_input" == inventory.id) is MachineStation machine) machine.Input.Restore(inventory.items);
                }
                // Save v2 giữ hàng trên đúng chủ sở hữu; không hoàn hàng khách vào kho.
                foreach(var progress in data.stationStates) Stations.Find(x=>x.Id==progress.id)?.RestoreProgress(progress);
                foreach (var cash in data.cash) if (Stations.Find(x => x.Id == cash.id) is CheckoutStation checkout) checkout.Cash = cash.amount;
                if(Player.Controller)Player.Controller.enabled = false;
                Player.transform.position = new Vector3(data.playerX, .05f, data.playerZ);
                if(Player.Controller)Player.Controller.enabled = true;
                ApplyProgression();
                SaveBlocked=false;Player.CanControl=false;
                if(data.transactionState!=null)InitializeTransactions(data.transactionState);
                CameraRig?.Snap();
                SaveBlocked=false;
                Player.CanControl=true;
                Say("Đã tải trò chơi");
            }
            catch (Exception error) { BlockRecovery(error); }
        }
        void ValidateSaveOwners(SaveData data)
        {
            if(data.transactionState!=null)
            {
                TransactionCore.Validate(data.transactionState);
                foreach(var state in data.transactionState.stations)
                    if(!Stations.Exists(x=>x is not StationZone&&x.Id==state.id))throw new InvalidDataException("Station reference không còn tồn tại: "+state.id);
                if(data.transactionState.money!=data.money)throw new InvalidDataException("Core money không khớp save projection.");
            }
            if(data.transit.Count>0)throw new InvalidDataException("Transit prototype không có owner; không tự trả hàng về kho.");
            var owners=new Dictionary<string,Inventory>{{"player",Player.Carry}};
            var stations=new Dictionary<string,Station>();
            foreach(var station in Stations)
            {
                if(station is StationZone)continue;
                if(string.IsNullOrEmpty(station.Id)||!stations.TryAdd(station.Id,station))throw new InvalidDataException("Stable ID trạm bị thiếu hoặc lặp.");
                if(station.Inventory!=null&&!owners.TryAdd(station.Id,station.Inventory))throw new InvalidDataException("Owner inventory bị lặp.");
                if(station is MachineStation machine&&!owners.TryAdd(station.Id+"_input",machine.Input))throw new InvalidDataException("Owner đầu vào máy bị lặp.");
            }
            if(data.inventories.Count!=owners.Count)throw new InvalidDataException("Save thiếu owner inventory.");
            foreach(var inventory in data.inventories)
            {
                if(!owners.TryGetValue(inventory.id,out var target))throw new InvalidDataException("Owner không còn tồn tại: "+inventory.id);
                new Inventory(int.MaxValue,target.SingleItem).Restore(inventory.items);
            }
            var ids=new HashSet<string>();
            if(data.stationStates.Count!=stations.Count)throw new InvalidDataException("Save thiếu trạng thái trạm.");
            foreach(var state in data.stationStates)
                if(state==null||!stations.ContainsKey(state.id)||!ids.Add(state.id)||float.IsNaN(state.remaining)||float.IsInfinity(state.remaining))
                    throw new InvalidDataException("Trạng thái trạm không hợp lệ.");
            foreach(var cash in data.cash)if(!Checkouts.Exists(c=>c.Id==cash.id))throw new InvalidDataException("Quầy tiền không còn tồn tại.");
            var receipts=new HashSet<long>();
            foreach(var customer in data.customers)
            {
                if(customer==null||customer.receipt<=0||customer.receipt==long.MaxValue||!receipts.Add(customer.receipt)||
                    !Checkouts.Exists(c=>c.Id==customer.lane&&c.ShopId==customer.shop))
                    throw new InvalidDataException("Owner khách không hợp lệ.");
                new Inventory(int.MaxValue).Restore(customer.basket);
            }
            foreach(var diner in data.diners)
            {
                if(diner==null||diner.receipt<=0||diner.receipt==long.MaxValue||!receipts.Add(diner.receipt)||!Tables.Exists(t=>t.Id==diner.table))
                    throw new InvalidDataException("Owner khách bàn không hợp lệ.");
                new Inventory(int.MaxValue).Restore(diner.basket);
            }
            ids.Clear();
            foreach(var worker in data.workers)
            {
                if(worker==null||string.IsNullOrEmpty(worker.id)||!ids.Add(worker.id)||!data.crews.Exists(c=>c.id==worker.upgrade)||
                    worker.phase<0||worker.phase>2||worker.count<0||
                    (worker.phase>0&&(!stations.ContainsKey(worker.destination?.Replace("_input","")??"")||
                    (worker.phase==1&&!stations.ContainsKey(worker.source??"")))))
                    throw new InvalidDataException("Owner nhân viên hoặc tuyến vận chuyển không hợp lệ.");
                new Inventory(int.MaxValue,true).Restore(worker.carry);
            }
            if(data.nextReceipt==long.MaxValue||float.IsNaN(data.playerX)||float.IsNaN(data.playerZ)||
                float.IsInfinity(data.playerX)||float.IsInfinity(data.playerZ))
                throw new InvalidDataException("Vị trí hoặc số biên nhận không hợp lệ.");
        }
        void OnApplicationQuit() { if (Player != null && !IsQa) SaveGame(); }
        void OnLog(string condition, string trace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) RuntimeErrors.Add(condition);
        }
        void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            if (Instance == this) Instance = null;
        }
    }
    [Serializable]
    public sealed class GraphicsAudit
    {
        public string name, vendor, api, started;
        public int memoryMiB, width, height;
    }
}
