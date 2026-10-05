using System.Collections.Generic;
using System.Linq;

namespace Tycoon
{
    public enum PurchaseState { Locked, Available, Contributing, Purchased }

    public sealed class PurchaseEvaluation
    {
        public PurchaseState State;
        public int Contributed, Cost;
        public string[] Requirements;
        public bool CanContribute => Requirements.Length == 0 && State is PurchaseState.Available or PurchaseState.Contributing;
        public string StatusText => State switch
        {
            PurchaseState.Locked => "CHƯA ĐỦ ĐIỀU KIỆN",
            PurchaseState.Available => "CÓ THỂ MUA",
            PurchaseState.Contributing => "ĐANG GÓP",
            _ => "ĐÃ MUA"
        };
    }

    // Chỉ đọc số liệu authoritative; writer của đơn, trạm và unlock vẫn là TransactionCore.
    public sealed class ProgressionTracker
    {
        public static string FamilyLabel(string family)=>family switch{"farm"=>"Cây trồng","animal"=>"Chăn nuôi","counter"=>"Quầy nông sản","mill"=>"Máy chế biến","market"=>"Siêu thị","oven"=>"Lò bánh","kitchen"=>"Bếp nhà hàng",_=>family};
        readonly GameSession game;
        readonly Dictionary<string,PurchaseEvaluation> evaluations=new();
        RuntimeTransactions cachedTransactions;
        long cachedRevision=-1;
        public ProgressionTracker(GameSession game) { this.game = game; }

        TransactionState State => game.Transactions?.View;
        bool Unlocked(string id) => State == null ? game.Economy.Has(id) : State.unlocked.Contains(id);

        public int SuccessfulOrders => State == null ? game.Economy.Transactions : State.legacyTransactions + State.payments.Count;

        public int StationLevel(string family)
        {
            if (State != null)
            {
                return StationLevel(State,family);
            }
            return Unlocked(family + "_level3") ? 3 : Unlocked(family + "_level2") ? 2 : 1;
        }

        public int SuccessfulJobs(string area)
        {
            if (State != null) return State.stations.Where(x => x.area == area).Sum(x => x.workCount);
            return game.Stations.Where(x => x.AreaId == area).Sum(x => x.WorkCount);
        }
        public int PlayerJobs(CrewState crew)=>State==null?PlayerJobs(game,crew):PlayerJobs(State,crew);

        public int SuccessfulOrdersAt(string area)
        {
            if (State != null)
                return State.payments.Count(p => State.stations.Any(s => s.id == p.counter && s.area == area));
            return game.Checkouts.Where(x => x.AreaId == area).Sum(x => x.Sales);
        }

        public int RecipeBatches(string recipeId)
        {
            if (State != null) return State.stations.Where(x => x.kind == "machine" && x.definitionId == recipeId).Sum(x => x.batches);
            return game.Machines.Where(x => x.Recipe != null && x.Recipe.id == recipeId).Sum(x => x.Batches);
        }

        public bool CanProduce(string item) => State!=null?CanProduce(State,item):CanProduce(game,item,new HashSet<string>());

        public static bool CanProduce(TransactionState state,string item) => state!=null&&!string.IsNullOrEmpty(item)&&CanProduce(state,item,new HashSet<string>());

        static bool CanProduce(TransactionState state,string item,HashSet<string> visiting)
        {
            if(!visiting.Add(item))return false;
            bool available=state.stations.Any(s=>s.kind=="producer"&&s.item==item&&(string.IsNullOrEmpty(s.requirement)||state.unlocked.Contains(s.requirement)));
            if(!available)
                foreach(var machine in state.stations.Where(s=>s.kind=="machine"&&(string.IsNullOrEmpty(s.requirement)||state.unlocked.Contains(s.requirement))))
                {
                    foreach(string id in machine.recipeOptions.Count>0?machine.recipeOptions:new List<string>{machine.definitionId})
                    {
                        var recipe=Definitions.Recipe(id);
                        if(recipe!=null&&recipe.output==item&&recipe.inputs.All(x=>CanProduce(state,x.id,new HashSet<string>(visiting)))){available=true;break;}
                    }
                    if(available)break;
                }
            if(!available)visiting.Remove(item);
            return available;
        }

        static bool CanProduce(GameSession game,string item,HashSet<string> visiting)
        {
            if(!visiting.Add(item))return false;
            bool available=game.Producers.Any(x=>x.IsUnlocked&&x.ItemId==item);
            if(!available)
                foreach(var machine in game.Machines.Where(x=>x.IsUnlocked&&x.Recipe!=null))
                {
                    var path=new HashSet<string>(visiting);
                    if(machine.Options.Any(id=>Definitions.Recipe(id).output==item&&Definitions.Recipe(id).inputs.All(x=>CanProduce(game,x.id,new HashSet<string>(path))))){available=true;break;}
                }
            if(!available)visiting.Remove(item);
            return available;
        }

        public int AxisLevel(string family, UpgradeAxis axis)
        {
            return Definitions.Upgrades.Where(x => x.family == family && x.axis == axis && Unlocked(x.id))
                .Select(x => x.axisLevel).DefaultIfEmpty(1).Max();
        }

        public int AxisCapacity(string family, int defaultValue)
        {
            return Definitions.Upgrades.Where(x => x.family == family && x.axis == UpgradeAxis.Capacity &&
                    x.capacityValue > 0 && Unlocked(x.id))
                .OrderBy(x => x.axisLevel).Select(x => x.capacityValue).DefaultIfEmpty(defaultValue).Last();
        }

        public float FarmGrowSeconds => 2f * (1f - .2f * (AxisLevel("farm", UpgradeAxis.Speed) - 1));

        public PurchaseEvaluation Evaluate(UpgradeDefinition upgrade)
        {
            if (upgrade == null) return new PurchaseEvaluation { State = PurchaseState.Locked, Requirements = new[] { "Không có purchase" } };
            if(State!=null)
            {
                if(!ReferenceEquals(cachedTransactions,game.Transactions)||cachedRevision!=game.Transactions.Revision)
                {evaluations.Clear();cachedTransactions=game.Transactions;cachedRevision=game.Transactions.Revision;}
                if(evaluations.TryGetValue(upgrade.id,out var cached))return cached;
            }
            int contributed = game.Contribution(upgrade.id);
            string[] reasons = State != null ? MissingRequirements(State, upgrade) : MissingRequirements(game, upgrade);

            bool purchased = Unlocked(upgrade.id);
            var result=new PurchaseEvaluation
            {
                State = purchased ? PurchaseState.Purchased : reasons.Length > 0 ?
                    contributed > 0 ? PurchaseState.Contributing : PurchaseState.Locked :
                    contributed > 0 ? PurchaseState.Contributing : PurchaseState.Available,
                Contributed = contributed, Cost = upgrade.cost, Requirements = reasons
            };
            if(State!=null)evaluations[upgrade.id]=result;
            return result;
        }

        static string[] MissingRequirements(GameSession game, UpgradeDefinition upgrade)
        {
            var reasons = new List<string>();
            if (Definitions.Upgrade(upgrade.id) != upgrade || upgrade.kind == "legacy") reasons.Add("Purchase prototype đã ngừng sử dụng.");
            if (!string.IsNullOrEmpty(upgrade.requirement) && !game.Economy.Has(upgrade.requirement))
                reasons.Add("Cần mở " + (Definitions.Upgrade(upgrade.requirement)?.label ?? upgrade.requirement) + ".");
            var requirement = upgrade.progression;
            if (requirement != null)
            {
                foreach (string id in requirement.allUnlocks ?? System.Array.Empty<string>())
                    if (!game.Economy.Has(id)) reasons.Add("Cần mở " + (Definitions.Upgrade(id)?.label ?? id) + ".");
                if (requirement.anyUnlocks != null && requirement.anyUnlocks.Length > 0 && !requirement.anyUnlocks.Any(game.Economy.Has))
                    reasons.Add("Cần mở một tuyến chăn nuôi (chuồng, sữa hoặc trứng).");
                if (!string.IsNullOrEmpty(requirement.stationFamily) && game.Progression.StationLevel(requirement.stationFamily) < requirement.stationLevel)
                    reasons.Add("Trạm " + FamilyLabel(requirement.stationFamily) + " cấp " + requirement.stationLevel + " • " +
                        game.Progression.StationLevel(requirement.stationFamily) + "/" + requirement.stationLevel + ".");
                var alternatives=requirement.anyStationFamilies??System.Array.Empty<string>();
                if(alternatives.Length>0&&!alternatives.Any(f=>game.Progression.StationLevel(f)>=requirement.anyStationLevel))
                    reasons.Add("Cần một tuyến cấp "+requirement.anyStationLevel+" • "+string.Join(" / ",alternatives.Select(f=>FamilyLabel(f)+" "+game.Progression.StationLevel(f)+"/"+requirement.anyStationLevel))+".");
                if (requirement.successfulOrders > game.Economy.Transactions)
                    reasons.Add("Đơn thành công • " + game.Economy.Transactions + "/" + requirement.successfulOrders + ".");
                if (requirement.successfulOrdersAtArea > game.Progression.SuccessfulOrdersAt(requirement.successfulOrderArea))
                    reasons.Add("Đơn " + requirement.successfulOrderArea + " thành công • " +
                        game.Progression.SuccessfulOrdersAt(requirement.successfulOrderArea) + "/" + requirement.successfulOrdersAtArea + ".");
                if (requirement.successfulJobs > game.Progression.SuccessfulJobs(requirement.successfulJobArea))
                    reasons.Add("Lượt việc " + requirement.successfulJobArea + " • " +
                        game.Progression.SuccessfulJobs(requirement.successfulJobArea) + "/" + requirement.successfulJobs + ".");
                foreach (var batch in requirement.recipeBatches ?? System.Array.Empty<RecipeBatchRequirement>())
                    if (game.Progression.RecipeBatches(batch.recipeId) < batch.batches)
                        reasons.Add("Mẻ " + (Definitions.Recipe(batch.recipeId)?.label ?? batch.recipeId) + " • " +
                            game.Progression.RecipeBatches(batch.recipeId) + "/" + batch.batches + ".");
                AddPlayerTraining(reasons,requirement,
                    game.Machines.Where(x=>x.AreaId=="restaurant").Sum(x=>x.PlayerBatches),
                    game.Tables.Sum(x=>x.PlayerServeCount),game.Tables.Sum(x=>x.PlayerCleanCount));
            }
            if (upgrade.kind == "worker")
            {
                var crew = GameSession.CrewFor(upgrade); string family = FamilyFor(crew);
                int level = game.Progression.StationLevel(family), work = PlayerJobs(game,crew);
                if (level < 3) reasons.Add("Trạm cấp 3 • hiện cấp " + level + ".");
                if (work < 30) reasons.Add("30 lượt việc bạn tự làm • " + work + "/30.");
                if (game.StorageFor(crew.area) == null) reasons.Add("Khu " + GameHud.AreaLabel(crew.area) + " chưa có kho riêng.");
            }
            return reasons.ToArray();
        }

        public static string[] MissingRequirements(TransactionState state, UpgradeDefinition upgrade)
        {
            var reasons = new List<string>();
            if (Definitions.Upgrade(upgrade.id) != upgrade || upgrade.kind == "legacy") reasons.Add("Purchase prototype đã ngừng sử dụng.");
            bool unlocked(string id) => state.unlocked.Contains(id);
            if (!string.IsNullOrEmpty(upgrade.requirement) && !unlocked(upgrade.requirement))
                reasons.Add("Cần mở " + (Definitions.Upgrade(upgrade.requirement)?.label ?? upgrade.requirement) + ".");
            var requirement = upgrade.progression;
            if (requirement != null)
            {
                foreach (string id in requirement.allUnlocks ?? System.Array.Empty<string>())
                    if (!unlocked(id)) reasons.Add("Cần mở " + (Definitions.Upgrade(id)?.label ?? id) + ".");
                if (requirement.anyUnlocks != null && requirement.anyUnlocks.Length > 0 && !requirement.anyUnlocks.Any(unlocked))
                    reasons.Add("Cần mở một tuyến chăn nuôi (chuồng, sữa hoặc trứng).");
                if (!string.IsNullOrEmpty(requirement.stationFamily))
                {
                    int level = StationLevel(state, requirement.stationFamily);
                    if (level < requirement.stationLevel) reasons.Add("Trạm " + FamilyLabel(requirement.stationFamily) + " cấp " + requirement.stationLevel + " • " + level + "/" + requirement.stationLevel + ".");
                }
                var alternatives=requirement.anyStationFamilies??System.Array.Empty<string>();
                if(alternatives.Length>0&&!alternatives.Any(f=>StationLevel(state,f)>=requirement.anyStationLevel))
                    reasons.Add("Cần một tuyến cấp "+requirement.anyStationLevel+" • "+string.Join(" / ",alternatives.Select(f=>FamilyLabel(f)+" "+StationLevel(state,f)+"/"+requirement.anyStationLevel))+".");
                int sales = state.legacyTransactions + state.payments.Count;
                if (requirement.successfulOrders > sales) reasons.Add("Đơn thành công • " + sales + "/" + requirement.successfulOrders + ".");
                if (requirement.successfulOrdersAtArea > 0)
                {
                    int count = state.payments.Count(p => state.stations.Any(s => s.id == p.counter && s.area == requirement.successfulOrderArea));
                    if (count < requirement.successfulOrdersAtArea) reasons.Add("Đơn " + requirement.successfulOrderArea + " thành công • " + count + "/" + requirement.successfulOrdersAtArea + ".");
                }
                if (requirement.successfulJobs > 0)
                {
                    int count = state.stations.Where(s => s.area == requirement.successfulJobArea).Sum(s => s.workCount);
                    if (count < requirement.successfulJobs) reasons.Add("Lượt việc " + requirement.successfulJobArea + " • " + count + "/" + requirement.successfulJobs + ".");
                }
                foreach (var batch in requirement.recipeBatches ?? System.Array.Empty<RecipeBatchRequirement>())
                {
                    int count = state.stations.Where(s => s.kind == "machine" && s.definitionId == batch.recipeId).Sum(s => s.batches);
                    if (count < batch.batches) reasons.Add("Mẻ " + (Definitions.Recipe(batch.recipeId)?.label ?? batch.recipeId) + " • " + count + "/" + batch.batches + ".");
                }
                AddPlayerTraining(reasons,requirement,
                    state.stations.Where(s=>s.kind=="machine"&&s.area=="restaurant").Sum(s=>s.playerBatches),
                    state.stations.Where(s=>s.kind=="table"&&s.area=="restaurant").Sum(s=>s.progress.playerServeCount),
                    state.stations.Where(s=>s.kind=="table"&&s.area=="restaurant").Sum(s=>s.progress.playerCleanCount));
            }
            if (upgrade.kind == "worker")
            {
                var crew = GameSession.CrewFor(upgrade); string family = FamilyFor(crew);
                int level = StationLevel(state, family);
                int work = PlayerJobs(state,crew);
                if (level < 3) reasons.Add("Trạm cấp 3 • hiện cấp " + level + ".");
                if (work < 30) reasons.Add("30 lượt việc bạn tự làm • " + work + "/30.");
                if (!state.stations.Any(s => s.kind == "storage" && s.area == crew.area)) reasons.Add("Khu " + GameHud.AreaLabel(crew.area) + " chưa có kho riêng.");
            }
            return reasons.ToArray();
        }

        static void AddPlayerTraining(List<string> reasons,ProgressionRequirement requirement,int cooked,int served,int cleaned)
        {
            if(requirement.playerCookJobs>cooked)reasons.Add("Bạn phải tự nấu ít nhất "+requirement.playerCookJobs+" món • "+cooked+"/"+requirement.playerCookJobs+".");
            if(requirement.playerTableServes>served)reasons.Add("Bạn phải tự phục vụ ít nhất "+requirement.playerTableServes+" bàn • "+served+"/"+requirement.playerTableServes+".");
            if(requirement.playerTableCleans>cleaned)reasons.Add("Bạn phải tự dọn ít nhất "+requirement.playerTableCleans+" bàn • "+cleaned+"/"+requirement.playerTableCleans+".");
        }

        public string NextObjectiveText()
        {
            string id = !Unlocked("farm_shop") ? "farm_shop" : !Unlocked("mill") ? "mill" :
                !Unlocked("supermarket") ? "supermarket" : !Unlocked("bakery") ? "bakery" : !Unlocked("restaurant") ? "restaurant" : null;
            if (id == null) return "ĐÃ MỞ ĐỦ KHU • Nhà hàng đang hoạt động.";
            var definition = Definitions.Upgrade(id); var evaluation = Evaluate(definition);
            string title = evaluation.StatusText + " • " + definition.label + " • " + evaluation.Contributed + "/" + evaluation.Cost + " xu";
            if (evaluation.Requirements.Length == 0) return title + "\nĐứng trên ô mua để góp tiền.";
            return title + "\n" + string.Join("  •  ", evaluation.Requirements);
        }

        public static int StationLevel(TransactionState state, string family)
        {
            int stored=state.stations.Where(x => Family(x) == family).Select(x => x.level).DefaultIfEmpty(1).Max();
            int purchased=Definitions.Upgrades.Where(x=>x.family==family&&x.axis==UpgradeAxis.StationLevel&&state.unlocked.Contains(x.id))
                .Select(x=>x.axisLevel).DefaultIfEmpty(1).Max();
            return System.Math.Max(stored,purchased);
        }

        public static int AxisLevel(TransactionState state, string family, UpgradeAxis axis)
        {
            return Definitions.Upgrades.Where(x => x.family == family && x.axis == axis && state.unlocked.Contains(x.id))
                .Select(x => x.axisLevel).DefaultIfEmpty(1).Max();
        }

        public static int AxisCapacity(TransactionState state, string family, int defaultValue)
        {
            return Definitions.Upgrades.Where(x => x.family == family && x.axis == UpgradeAxis.Capacity && x.capacityValue > 0 && state.unlocked.Contains(x.id))
                .OrderBy(x => x.axisLevel).Select(x => x.capacityValue).DefaultIfEmpty(defaultValue).Last();
        }

        public static float FarmGrowDuration(TransactionState state) => 2f * (1f - .2f * (AxisLevel(state, "farm", UpgradeAxis.Speed) - 1));

        internal static string Family(StationRuntimeState station)
        {
            if (station.kind == "producer") return Definitions.IsAnimal(station.item) ? "animal" : "farm";
            return station.area switch
            {
                "processing" => "mill", "supermarket" => "market", "bakery" => "oven", "restaurant" => "kitchen",
                "farm_shop" => "counter", _ => station.area
            };
        }

        internal static string FamilyFor(CrewState crew) => crew.role == "Farmer" || crew.role=="Repairer"&&crew.area=="farm" ? "farm" : crew.role == "AnimalWorker" ? "animal" :
            crew.area == "supermarket" ? "market" : crew.area == "processing" ? "mill" :
            crew.area == "bakery" ? "oven" : crew.area == "restaurant" ? "kitchen" : "counter";

        static int PlayerJobs(TransactionState state,CrewState crew)
        {
            if(crew.role=="Repairer")return state.stations.Where(s=>s.area==crew.area).Sum(s=>s.progress.playerRepairCount);
            return state.stations.Where(s=>s.area==crew.area&&PlayerJobStation(s,crew.role)).Sum(s=>crew.role is "Processor" or "Baker" or "Cook"?s.playerBatches:s.playerWorkCount);
        }
        static int PlayerJobs(GameSession game,CrewState crew)
        {
            if(crew.role=="Repairer")return game.Transactions?.Snapshot().stations.Where(s=>s.area==crew.area).Sum(s=>s.progress.playerRepairCount)??0;
            return game.Stations.Where(s=>s&&!(s is StationZone)&&s.AreaId==crew.area&&PlayerJobStation(s,crew.role)).Sum(s=>s is MachineStation machine&&(crew.role is "Processor" or "Baker" or "Cook")?machine.PlayerBatches:s.PlayerWorkCount);
        }
        static bool PlayerJobStation(StationRuntimeState station,string role)=>role switch
        {
            "Farmer"=>station.kind=="producer"&&Definitions.IsCrop(station.item),
            "AnimalWorker"=>station.kind=="producer"&&Definitions.IsAnimal(station.item),
            "Restocker"=>station.kind=="shelf",
            "Cashier"=>station.kind=="counter",
            "Processor" or "Baker" or "Cook"=>station.kind=="machine",
            "Waiter"=>station.kind=="table",
            "Transporter"=>station.kind is "storage" or "shelf" or "machine" or "producer" or "counter",
            _=>false
        };
        static bool PlayerJobStation(Station station,string role)=>station switch
        {
            ProductionStation producer when role=="Farmer"=>!producer.Animal,
            ProductionStation producer when role=="AnimalWorker"=>producer.Animal,
            ShelfStation when role=="Restocker"=>true,
            CheckoutStation when role=="Cashier"=>true,
            MachineStation when role is "Processor" or "Baker" or "Cook"=>true,
            TableStation when role=="Waiter"=>true,
            StorageStation or ShelfStation or MachineStation or ProductionStation or CheckoutStation when role=="Transporter"=>true,
            _=>false
        };
    }
}
