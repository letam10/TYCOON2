using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed class CommerceDirector : MonoBehaviour
    {
        public readonly List<CustomerAgent> Customers = new();
        readonly Queue<CustomerAgent> pool = new();
        int sequence;
        bool restored;
        float nextSpawn;
        public int PeakCustomers;
        public int MaximumActive => 30;
        public int ActiveCount => Customers.Count + (GameSession.Instance.Restaurant ? GameSession.Instance.Restaurant.Diners.Count : 0);

        void Update()
        {
            var game = GameSession.Instance;
            if (!game.NavigationReady || !game.CanSimulate) return;
            if (!restored)
            {
                // Nạp theo vị trí đã lưu trước khi sinh khách mới để giữ nguyên FIFO.
                var pending = new List<CustomerSave>(game.PendingCustomers);
                pending.Sort((a, b) => { int lane = string.CompareOrdinal(a.lane, b.lane); return lane != 0 ? lane : a.queueIndex.CompareTo(b.queueIndex); });
                foreach (var saved in pending)
                {
                    var lane = game.Checkouts.Find(x => x.Id == saved.lane);
                    if (lane && Spawn(lane, saved)) game.PendingCustomers.Remove(saved);
                }
                restored = game.PendingCustomers.Count == 0;
                if (!restored) return;
            }
            if (game.Milestone == 0) return;
            if (Time.time < nextSpawn || ActiveCount >= MaximumActive) return;
            nextSpawn = Time.time + (game.RushActive ? .65f : game.BusinessStage==1 ? 10f : 2.5f);
            int limit = game.RushActive ? MaximumActive : game.Economy.Has("supermarket") ? 24 : 15;
            if (ActiveCount >= limit) return;
            var shops=new System.Collections.Generic.List<string>{"farm"};
            if(game.Economy.Has("farm_shop"))shops.Add("farm_shop");
            if(game.Economy.Has("supermarket"))shops.Add("market");
            if(game.Economy.Has("bakery"))shops.Add("bakery");
            string shop = shops[sequence++ % shops.Count];
            var checkout = FindLane(shop);
            if (checkout) Spawn(checkout, null);
        }

        public static CheckoutStation FindLane(string shop)
        {
            CheckoutStation result = null;
            foreach (var lane in GameSession.Instance.Checkouts)
                if (lane.ShopId == shop && lane.IsUnlocked && (!result || lane.Queue.Count < result.Queue.Count)) result = lane;
            return result;
        }

        bool Spawn(CheckoutStation lane, CustomerSave saved)
        {
            Vector3 point = saved == null ? lane.transform.position + new Vector3((sequence % 6 - 3) * .85f, 0, -9) : new Vector3(saved.x, saved.y, saved.z);
            if (!NavMesh.SamplePosition(point, out var hit, 3, NavMesh.AllAreas)) return false;
            CustomerAgent customer;
            if (pool.Count > 0)
            {
                customer = pool.Dequeue(); customer.transform.position = hit.position; customer.gameObject.SetActive(true); customer.Agent.Warp(hit.position);
            }
            else
            {
                var root = new GameObject("Customer"); root.transform.SetParent(transform); root.transform.position = hit.position;
                var model = Art.Model(sequence % 2 == 0 ? "customer" : "customer_beach", Vector3.zero, root.transform);
                model.AddComponent<ActorView>(); customer = root.AddComponent<CustomerAgent>(); customer.Initialize();
            }
            if (saved == null) customer.Begin(lane.ShopId, sequence); else customer.Restore(saved);
            Customers.Add(customer); PeakCustomers = Mathf.Max(PeakCustomers, ActiveCount);
            return true;
        }

        public void Recycle(CustomerAgent customer)
        {
            if (!Customers.Remove(customer)) return;
            customer.Lane?.Queue.Remove(customer); customer.gameObject.SetActive(false); pool.Enqueue(customer);
        }
        public void ResetForLoad()
        {
            foreach(var customer in Customers.ToArray())Recycle(customer);
            restored=false;nextSpawn=Time.time;
        }
    }

    public sealed class CustomerAgent : MonoBehaviour
    {
        public enum State { Waiting, Shopping, Queue, Leaving }
        public State Current;
        public string Shop;
        public NavMeshAgent Agent;
        public ActorView View;
        [System.NonSerialized] public Inventory Basket = new(3);
        public long Receipt;
        public CheckoutStation Lane;
        public OrderState Order { get; private set; }
        float decideAt;
        Vector3 exitPoint;
        TextMesh bubble;
        GameObject icon;
        string iconItem;
        Transform orderBubble;
        readonly StringBuilder orderText = new();

        public string Diagnostic() => Shop + " state=" + Current + " basket=" + Basket.Total + " receipt=" + Receipt + " remaining=" + (Order?.RemainingPatience(Time.time) ?? 0) + " position=" + transform.position;

        public void Initialize()
        {
            Agent = Navigation.Agent(gameObject,true); View = GetComponentInChildren<ActorView>(); View.Initialize();
            orderBubble = new GameObject("OrderBubble").transform; orderBubble.SetParent(transform, false); orderBubble.localPosition = new Vector3(0, 2.4f, 0);
            orderBubble.gameObject.AddComponent<BillboardLabel>();
            var disk = Art.Cylinder("BubbleBackground", new Vector3(0, 0, .03f), new Vector3(.95f, .04f, .95f), "#FFFFFF", orderBubble);
            disk.transform.localRotation = Quaternion.Euler(90, 0, 0);
            bubble = Art.Label("", new Vector3(0, -.15f, -.08f), orderBubble, .11f, "#34483D", false);
            orderBubble.gameObject.SetActive(false);
            var stackRoot = new GameObject("CustomerBasket"); stackRoot.transform.SetParent(transform, false); stackRoot.transform.localPosition = new Vector3(0, .7f, .35f);
            var stack = stackRoot.AddComponent<InventoryStack>(); stack.Inventory = Basket; stack.Pool = GameSession.Instance.Pool;
            stack.Columns = 1; stack.Rows = 1; stack.Scale = .45f; stack.LayerHeight = .2f; stack.Maximum = 3;
        }

        public void Begin(string shop, int index)
        {
            var game = GameSession.Instance;
            Lane?.Queue.Remove(this); Shop = shop;
            if(Basket.Authority!=null)Basket.Unbind();Basket.Restore(null); Receipt = game.NextReceipt++;
            Lane = CommerceDirector.FindLane(shop); exitPoint = transform.position; decideAt = 0;
            var available = new List<string>();
            foreach (var shelf in game.Shelves)
            {
                if (shelf.ShopId != shop || !shelf.IsUnlocked || shelf.AllowedItems == null) continue;
                foreach (string id in shelf.AllowedItems)
                {
                    if (Definitions.Item(id) == null || available.Contains(id)) continue;
                    if(game.Progression.CanProduce(id))available.Add(id);
                }
            }
            var lines = new List<OrderLine>();
            if (shop == "farm")
            {
                // Khi chưa mở vật nuôi, Farm tiếp tục chỉ bán cà rốt.
                var livestock=available.FindAll(id=>id is "milk" or "egg" or "beef");
                string item=livestock.Count==0||Random.value<.65f?"carrot":livestock[Random.Range(0,livestock.Count)];
                int quantity=item=="carrot"?Random.Range(1,4):1;
                lines.Add(new OrderLine(item, quantity, game.ItemPrice(item)));
            }
            else
            {
                int maximumTypes=MaximumOrderItemTypes(shop);
                int count = Mathf.Min(available.Count, Random.Range(1, maximumTypes+1));
                for (int i = 0; i < count; i++)
                {
                    int chosen = Random.Range(0, available.Count); string id = available[chosen]; available.RemoveAt(chosen);
                    lines.Add(new OrderLine(id, 1, game.ItemPrice(id)));
                }
            }
            Order = new OrderState(Receipt, lines, Time.time);
            if (!Lane || lines.Count == 0) { Leave(); return; }
            Current = State.Queue; Lane.Queue.Add(this); Go(Lane.QueuePoint(this)); RefreshBubble();
            game.Transactions?.BindCustomer(this,Snapshot(),true);
        }

        public static int MaximumOrderItemTypes(string shop)=>shop is "farm" or "farm_shop"?1:2;

        public void Restore(CustomerSave saved)
        {
            Lane?.Queue.Remove(this); Shop = saved.shop; Receipt = saved.receipt;
            Lane = GameSession.Instance.Checkouts.Find(x => x.Id == saved.lane);
            if(Basket.Authority!=null)Basket.Unbind();Basket.Restore(saved.basket); Order = OrderState.Restore(saved, Time.time);
            exitPoint = new Vector3(saved.exitX, saved.exitY, saved.exitZ); decideAt = 0;
            Current = saved.phase == (int)State.Leaving || Order.Finished ? State.Leaving : State.Queue;
            if (Current == State.Queue && Lane)
            {
                int index = Mathf.Clamp(saved.queueIndex, 0, Lane.Queue.Count); Lane.Queue.Insert(index, this); Go(Lane.QueuePoint(this));
            }
            else Go(exitPoint);
            RefreshBubble();
            if(GameSession.Instance.Transactions!=null)GameSession.Instance.Transactions.BindCustomer(this,saved,GameSession.Instance.Transactions.Order(saved.receipt)==null);
        }

        void Update()
        {
            if(!GameSession.Instance.CanSimulate)return;
            if (View && Agent) View.SetMotion(Agent.velocity.magnitude, Basket.Total > 0);
            if (Current == State.Queue)
            {
                Order.Expire(Basket, GameSession.Instance.Economy, Time.time);
                if (Order.Finished) { Leave(); return; }
                if (Time.time >= decideAt) { decideAt = Time.time + .4f; Go(Lane.QueuePoint(this)); }
                RefreshBubble();
            }
            else if (Current == State.Leaving && Agent && Navigation.Arrived(Agent)) GameSession.Instance.Commerce.Recycle(this);
        }

        void Go(Vector3 point) { if (Agent) Navigation.Go(Agent, point); }

        void RefreshBubble()
        {
            if (!orderBubble) return;
            bool visible = Current == State.Queue && Order != null;
            orderBubble.gameObject.SetActive(visible);
            if (!visible) return;
            orderText.Clear(); string first = null;
            foreach (var line in Order.Lines)
            {
                orderText.Append(Definitions.Item(line.id).label).Append(' ').Append(line.delivered).Append('/').Append(line.requested).Append('\n');
                if (first == null && line.Remaining > 0) first = line.id;
            }
            orderText.Append(Mathf.CeilToInt(Order.RemainingPatience(Time.time))).Append("s"); bubble.text = orderText.ToString();
            if (first == iconItem) return;
            if (icon) GameSession.Instance.Pool.Return(icon);
            iconItem = first; icon = null;
            if (first == null) return;
            icon = GameSession.Instance.Pool.Take(first, orderBubble); icon.transform.localPosition = new Vector3(0, .48f, -.08f); icon.transform.localScale = Vector3.one * .3f;
            icon.transform.localRotation = Quaternion.Euler(first is "beef" or "meal" ? 70 : 15, 0, 0);
        }

        public void Leave()
        {
            if (icon) { GameSession.Instance.Pool.Return(icon); icon = null; } iconItem = null;
            if (orderBubble) orderBubble.gameObject.SetActive(false);
            // Hàng đã giao thuộc khách, kể cả khi hết giờ; không chuyển lại vào kho.
            Lane?.Queue.Remove(this); Current = State.Leaving; Go(exitPoint);
        }

        public CustomerSave Snapshot() => new()
        {
            receipt = Receipt, shop = Shop, lane = Lane ? Lane.Id : "", phase = (int)Current,
            queueIndex = Lane ? Lane.Queue.IndexOf(this) : -1, remaining = Order?.RemainingPatience(Time.time) ?? 0,
            paid = Order?.Paid ?? false, timedOut = Order?.TimedOut ?? false, order = Order?.SnapshotLines() ?? new List<OrderLine>(), basket = Basket.Snapshot(),
            x = transform.position.x, y = transform.position.y, z = transform.position.z, exitX = exitPoint.x, exitY = exitPoint.y, exitZ = exitPoint.z
        };
    }

    public sealed class CheckoutStation : Station
    {
        public override Vector3 WorkPoint=>transform.position+Vector3.forward*1.65f;
        public override Vector3 WaitingPoint=>transform.position+Vector3.right*4.8f+Vector3.forward*1.9f;
        public string ShopId;
        public StationZone CashZone;
        public Vector3 CollectionPoint=>transform.position+new Vector3(2.7f,0,.6f);
        public string[] AcceptedItems => GameSession.Instance.Shelves.Where(x=>x.ShopId==ShopId&&x.AllowedItems!=null)
            .SelectMany(x=>x.AllowedItems).Distinct().ToArray();
        public bool AcceptsItem(string id)
        { foreach(var shelf in GameSession.Instance.Shelves)if(shelf.ShopId==ShopId&&System.Array.IndexOf(shelf.AllowedItems??System.Array.Empty<string>(),id)>=0)return true;return false; }
        public readonly List<CustomerAgent> Queue = new();
        int cash,sales;
        public int Cash {get {if(Authority==null)return cash;int total=Authority.View.legacyCash.Find(x=>x.id==Id)?.amount??0;foreach(var p in Authority.View.payments)if(p.counter==Id&&!p.collected)total+=p.amount;return total;} set{if(Authority!=null&&value!=Cash)throw new System.InvalidOperationException("Cash chỉ được cập nhật qua transaction.");cash=value;}}
        public int Sales {get {if(Authority==null)return sales;int total=0;foreach(var p in Authority.View.payments)if(p.counter==Id)total++;return total;} set{if(Authority!=null&&value!=Sales)throw new System.InvalidOperationException("Sales chỉ được cập nhật qua transaction.");sales=value;}}
        public OrderState FrontOrder => Queue.Count > 0 ? Queue[0].Order : null;
        readonly List<GameObject> bills = new();
        int shownCash = -1;
        public override string Prompt => "Khách " + Queue.Count + " • hàng quầy " + (Inventory?.Total??0) + " • chưa thu " + Cash + " xu";
        public Vector3 QueuePoint(CustomerAgent customer)
        {
            int index = Mathf.Max(0, Queue.IndexOf(customer));
            return QueuePoint(index);
        }
        public Vector3 QueuePoint(int index)
        {
            if(ShopId=="market")
            {
                float[] columns={-.45f,.7f,-1.6f,1.85f,-2.75f,3};
                return transform.position+new Vector3(columns[index%columns.Length],0,-2-(index/columns.Length)*1.05f);
            }
            return transform.position + new Vector3(index%2==0?-.45f:.7f, 0, -2f-(index/2)*1.2f);
        }

        void Update()
        {
            if (!IsUnlocked || shownCash == Cash) return;
            int count = Mathf.Min(20, (Cash + 19) / 20);
            while (bills.Count < count) bills.Add(Art.Box("CashBill", Vector3.zero, new Vector3(.55f, .075f, .28f), "#66E932", transform));
            for (int i = 0; i < bills.Count; i++)
            {
                bills[i].SetActive(i < count); bills[i].transform.localPosition = new Vector3(2.4f + (i % 2) * .6f, .8f + (i / 2) * .08f, .6f);
            }
            shownCash = Cash;
        }

        public bool Serve(Inventory source) => Serve(source, null);
        public bool Serve(Inventory source, EntityId actor) => Serve(source, (EntityId?)actor);
        bool Serve(Inventory source, EntityId? actingActor)
        {
            if (!IsUnlocked || Queue.Count == 0 || source==null) return false;
            var game = GameSession.Instance; var customer = Queue[0]; var order = customer.Order;
            if (order == null) return false;
            order.Expire(customer.Basket, game.Economy, Time.time);
            if (order.Finished) { customer.Leave(); return false; }
            if (Vector3.Distance(customer.transform.position, QueuePoint(customer)) > .9f) return false;
            bool previouslyPaid = game.Economy.IsPaid(customer.Receipt);
            int moved = order.Deliver(source, customer.Basket, game.Economy, Time.time, actingActor);
            if (order.Paid && !previouslyPaid)
            {
                if(Authority==null){Cash += order.TotalPrice; Sales++; WorkCount++;}
                if (ShopId == "bakery") game.BakerySales++;
                game.Feedback?.PlaySale();
            }
            if (order.Finished) customer.Leave();
            return moved > 0;
        }

        public override bool Work(Inventory carrier, float delta, EntityId actorId) => Serve(carrier, actorId);

        public int CollectCash(PlayerController player)
        {
            var game = GameSession.Instance;
            if (!IsUnlocked || !player || player != game.Player || (CashZone ? !CashZone.ContainsInteractionPoint(player.transform.position) : Vector3.Distance(player.transform.position,CollectionPoint)>1.2f)) return 0;
            int collected = Authority!=null?Authority.Collect(Id):game.Economy.CollectCash(Cash); if(Authority==null)Cash -= collected;
            if (collected > 0) { game.Say("+" + collected + " xu"); game.Feedback?.PlaySale(); }
            return collected;
        }

        public override bool Interact(PlayerController player, bool withdraw) =>
            !withdraw && IsUnlocked && player && ContainsInteractionPoint(player.transform.position) && Serve(player.Carry,player.GetEntityId());

        protected override void LateUpdate()
        {if(StatusLabel){StatusLabel.text="THU NGÂN\n"+Cash+" xu • "+Queue.Count+" chờ";StatusLabel.gameObject.SetActive(!PlayerUsesZones&&IsUnlocked&&GameSession.Instance.Player&&(GameSession.Instance.Player.transform.position-InteractionPoint).sqrMagnitude<36);}}
    }
}


