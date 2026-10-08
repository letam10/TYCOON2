using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public TruckLogistics Logistics;
        public readonly List<TableStation> Tables = new();
        public List<DinerSave> PendingDiners = new();
        public bool NavigationReady;
        public int BakerySales, RestaurantMeals;
        public int BusinessStage => Economy.Has("restaurant") ? 5 : Economy.Has("bakery") ? 4 : Economy.Has("supermarket") ? 3 : Economy.Has("mill")||Economy.Has("farm_shop") ? 2 : 1;
        float nextAutosave;
        public StorageStation Storage;
        public int SelectedItem;
        public long NextReceipt = 1;
        public bool IsQa;
        public bool IsOrdinaryPlayCheck;
        public string QaDirectory;
        public string SavePath;
        public int Milestone;
        public string Toast = "";
        public float ToastUntil;
        public List<string> RuntimeErrors = new();
        public RuntimeTransactions Transactions { get; private set; }
        public bool IsRestoring {get;private set;}
        public bool CanRestore=>!SaveBlocked;
        public bool CanSimulate => !SaveBlocked&&!IsRestoring;

        void Awake()
        {
            Instance = this;
            Application.logMessageReceived += OnLog;
            string[] args = Environment.GetCommandLineArgs();
            IsQa = Array.Exists(args, x => x.StartsWith("--qa"));
            IsOrdinaryPlayCheck = Array.Exists(args, x => x == "--qa-city-play");
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
            IsRestoring=true;
            Catalog = Resources.Load<GameCatalog>("GameCatalog");
            if (!Catalog) throw new InvalidOperationException("Thiếu GameCatalog.");
            Art.Catalog = Catalog;
            Pool = new ItemPool(transform);
            WorldFactory.Build(this);
            var playerRoot = new GameObject("Player");
            playerRoot.transform.position = CityDistricts.PlayerSpawn;
            var actor = Art.Model("player", Vector3.zero, playerRoot.transform);
            actor.AddComponent<ActorView>();
            Player = playerRoot.AddComponent<PlayerController>();
            Player.Initialize();
            // Không nhận input khi save đang được khôi phục và core chưa sẵn sàng.
            Player.CanControl = false;
            CarryPresentation.Attach(Player);
            var ring = playerRoot.AddComponent<LineRenderer>(); ring.sharedMaterial = Art.Material("#78FF2D"); ring.useWorldSpace = false; ring.loop = true; ring.widthMultiplier = .04f; ring.positionCount = 32;
            for (int i = 0; i < 32; i++) ring.SetPosition(i, new Vector3(Mathf.Cos(i * Mathf.PI / 16) * .65f, .045f, Mathf.Sin(i * Mathf.PI / 16) * .65f));
            var cameraObject = new GameObject("MainCamera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 45; camera.nearClipPlane = .15f; camera.farClipPlane = 180;
            camera.backgroundColor = Art.Hex("#BEDADE"); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = true;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;
            CameraRig = cameraObject.AddComponent<FollowCamera>(); CameraRig.Target = playerRoot.transform; CameraRig.Snap();
            Hud = gameObject.AddComponent<GameHud>(); Hud.Initialize();
            bool baseline=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--qa-baseline");
            if (File.Exists(SavePath)&&!baseline) LoadGame();
            ApplyProgression();
            if (!baseline && Transactions == null && !SaveBlocked)
                try { InitializeTransactions(); Player.CanControl = false; } catch(Exception error) { BlockRecovery(error); }
            Feedback = gameObject.AddComponent<GameFeedback>();
            Commerce = gameObject.AddComponent<CommerceDirector>();
            Restaurant = gameObject.AddComponent<RestaurantDirector>();
            Logistics=gameObject.AddComponent<TruckLogistics>();
            UpgradeModelView.Initialize(this);
            new GameObject("InteractionFocus").AddComponent<InteractionFocusView>().transform.SetParent(transform);
            if (IsQa && !IsOrdinaryPlayCheck) gameObject.AddComponent<QaPresentationAudit>();
            if (IsOrdinaryPlayCheck) gameObject.AddComponent<CityPlaySession>();
            else if(IsQa&&Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--qa-stage23"))gameObject.AddComponent<QaStages>();
            else if(IsQa&&baseline)gameObject.AddComponent<QaBaseline>();
            else if (IsQa && !Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--qa-preview")) gameObject.AddComponent<QaDriver>();
            Say("Đứng gần để thu / xếp hàng • đến quầy nhận tiền • WASD di chuyển");
            nextAutosave = Time.time + 60;
        }
        void Update()
        {
            using var frameProbe = QaFrameProbe.Measure("GameSession.Update");
            if (!Player || !NavigationReady || SaveBlocked) return;
            SyncWorkers();
            if(IsRestoring)
            {
                if(PendingCustomers.Count+PendingDiners.Count+PendingWorkers.Count>0 || !WorkersReady)return;
                Transactions?.ResumeClock();IsRestoring=false;Player.CanControl=!Hud || Hud.AllowsPlayerControl;
                foreach(var agent in GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>())if(agent.enabled&&agent.isOnNavMesh)agent.isStopped=false;
            }
            TickEvents(Time.deltaTime);
            if ((!IsQa || IsOrdinaryPlayCheck) && Time.time > nextAutosave)
            { nextAutosave = Time.time + 60; SaveGame(); }
        }
        public static string Argument(string key, string defaultValue)
        {
            var args = Environment.GetCommandLineArgs();
            for (int index = 0; index < args.Length - 1; index++) if (args[index] == key) return args[index + 1];
            return defaultValue;
        }
        public IEnumerable<IPlayerInteractionArea> PlayerInteractionAreas()
        {
            if(Logistics)foreach(var cargoArea in Logistics.PlayerAreas())yield return cargoArea;
            foreach (var station in Stations)
            {
                if(!station || station is StationZone or ConveyorStation)continue;
                if(station is SafeStation safe){yield return safe.Deposit;yield return safe.Withdraw;continue;}
                if(station is IPlayerInteractionArea area){yield return area;continue;}
                if(!proximity.TryGetValue(station,out var target)){target=new ProximityTarget(station);proximity.Add(station,target);}
                yield return target;
                if(station is CheckoutStation)
                {
                    if(!cashProximity.TryGetValue(station,out var cash)){cash=new ProximityTarget(station,true);cashProximity.Add(station,cash);}
                    yield return cash;
                }
            }
        }
        readonly Dictionary<Station,ProximityTarget> proximity=new(),cashProximity=new();
        public Station NearestStation(Vector3 position)
        {
            Station nearest = null; float distance = float.PositiveInfinity;
            foreach (var station in Stations)
            {
                if (!station || station.PlayerUsesZones || station is StationZone zone&&!zone.Available || !station.ContainsInteractionPoint(position) || (!station.IsUnlocked && !(station is PurchasePad))) continue;
                if (station is PurchasePad pad && !pad.Available) continue;
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
            UpgradeModelView.RefreshAll(this,true);
            Say("Đã nâng cấp");
        }
        public void SaveGame()
        {
            using var operation = CityOperationTiming.Measure(this, "SaveGame.Checkpoint");
            if(SaveBlocked){Say("Save đang bị chặn vì file tải không hợp lệ");return;}
            try
            {
                if (Transactions != null) Transactions.Checkpoint();
                else SaveStore.Write(SavePath, CaptureSaveData());
                ShowCheckpointNotice();
            }
            catch(Exception error){Say("Không lưu được: "+error.Message);Debug.LogException(error);}
        }
        public SaveData CaptureSaveData()
        {
            var data = new SaveData {
                contentVersion=2, version=3, cashInHand=Economy.CashInHand, cashInSafe=Economy.CashInSafe,
                money = 0, revenue = Economy.Revenue, cashCollected=Economy.CashCollected,transactions = Economy.Transactions,
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
        public void InitializeTransactions(TransactionState state=null,bool persist=true,FileStream existingLease=null)
        {
            foreach(var machine in Machines)machine.ConfigureInputLimits();
            foreach(var station in Stations)if(station is StorageStation storage&&storage.Inventory!=null)storage.ConfigureItemBins(storage.Inventory.Capacity);
            var initial=state??RuntimeTransactions.Migrate(this,CaptureSaveData());PhysicalCashMigration.Upgrade(initial);initial.layoutRevision=TownLayout.Revision;
            RuntimeTransactions.UpgradeCounterOwners(initial,this);
            Transactions = new RuntimeTransactions(this,initial,persist,existingLease);
            Transactions.CompletePurchases();
            foreach(var order in Transactions.Snapshot().orders)if(long.TryParse(order.id.Substring(6),out long receipt))Transactions.Settle(receipt);
        }
        public void BlockRecovery(Exception error)
        { SaveBlocked=true;if(Player)Player.CanControl=false;Say("Gameplay dừng: "+error.Message);Debug.LogException(error); }
        public bool SaveBlocked { get; private set; }
        void OnApplicationQuit() { if (Player != null && !IsQa) SaveGame(); }
        void OnLog(string condition, string trace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {RuntimeErrors.Add(condition);if(IsQa&&Player==null)Application.Quit(2);}
        }
        void OnDestroy()
        {
            Transactions?.Detach();
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
