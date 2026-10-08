using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed class CustomerAgent : MonoBehaviour
    {
        public enum State
        {
            Waiting, Shopping, Queue, Leaving, Approaching
        }
        public State Current;
        public string Shop;
        public NavMeshAgent Agent;
        public ActorView View;
        [System.NonSerialized] public Inventory Basket = new(3);
        public long Receipt;
        public CheckoutStation Lane;
        public OrderState Order
        {
            get;
            private set;
        }
        float decideAt;
        int approachStep;
        Vector3 exitPoint;
        OrderBubbleView orderBubble;
        OrderBubbleTerminalFeedback terminalFeedback;
        public string Diagnostic() => Shop + " state=" + Current + " basket=" + Basket.Total + " receipt=" + Receipt
            + " remaining=" + (Order?.RemainingPatience(Time.time) ?? 0) + " position=" + transform.position;
        public void Initialize()
        {
            Agent = Navigation.Agent(gameObject,true);
            View = GetComponentInChildren<ActorView>();
            View.Initialize();
            var stackRoot = new GameObject("CustomerBasket");
            stackRoot.transform.SetParent(transform, false);
            stackRoot.transform.localPosition = new Vector3(0, .7f, .35f);
            var stack = stackRoot.AddComponent<InventoryStack>();
            stack.Inventory = Basket;
            stack.Pool = GameSession.Instance.Pool;
            stack.Columns = 1;
            stack.Rows = 1;
            stack.Scale = .45f;
            stack.LayerHeight = .2f;
            stack.Maximum = 3;
        }
        public void Begin(string shop, int index,bool approach=false)
        {
            if (terminalFeedback) terminalFeedback.ResetFeedback();
            OrderBubbleLayer.Release(ref orderBubble);
            var game = GameSession.Instance;
            Lane?.Queue.Remove(this);
            Shop = shop;
            if(Basket.Authority!=null)Basket.Unbind();
            Basket.Restore(null);
            Receipt = game.NextReceipt++;
            Lane = CommerceDirector.FindLane(shop);
            exitPoint = transform.position;
            decideAt = 0;
            if(approach)
            {
                Current=State.Approaching;
                approachStep=0;
                Order=null;
                Go(ApproachPoint());
                return;
            }
            JoinQueue();
        }
        Vector3 FrontStreet=>Shop=="market"?new(60,0,19):Shop=="bakery"?new(21,0,21):new(Lane.transform.position.x,
            0,-12);
        Vector3 FrontEntry=>Shop=="market"?new(Lane.transform.position.x,0,19):Lane.transform.position+Vector3.back*6;
        Vector3 ApproachPoint()=>approachStep==0?TownLayout.Gate:approachStep==1?FrontStreet:FrontEntry;
        Vector3 ExitStep() => approachStep == 0 ? FrontEntry : approachStep == 1 ? FrontStreet :
            approachStep == 2 ? TownLayout.Gate : exitPoint;
        void JoinQueue()
        {
            var game=GameSession.Instance;
            string shop=Shop;
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
                var livestock=available.FindAll(Definitions.IsAnimal);
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
                    int chosen = Random.Range(0, available.Count);
                    string id = available[chosen];
                    available.RemoveAt(chosen);
                    lines.Add(new OrderLine(id, 1, game.ItemPrice(id)));
                }
            }
            Order = new OrderState(Receipt, lines, Time.time);
            if (!Lane || lines.Count == 0)
            {
                Leave();
                return;
            }
            Current = State.Queue;
            Lane.Queue.Add(this);
            Go(Lane.QueuePoint(this));
            RefreshBubble();
            game.Transactions?.BindCustomer(this,Snapshot(),true);
        }
        public static int MaximumOrderItemTypes(string shop)=>shop is "farm" or "farm_shop"?1:2;
        public void Restore(CustomerSave saved)
        {
            if (terminalFeedback) terminalFeedback.ResetFeedback();
            OrderBubbleLayer.Release(ref orderBubble);
            Lane?.Queue.Remove(this);
            Shop = saved.shop;
            Receipt = saved.receipt;
            Lane = GameSession.Instance.Checkouts.Find(x => x.Id == saved.lane);
            if(saved.phase==(int)State.Approaching)
            {
                if(Basket.Authority!=null)Basket.Unbind();
                Basket.Restore(saved.basket);
                Order=null;
                Current=State.Approaching;
                approachStep=saved.approachStep;
                exitPoint=TownLayout.Spawn((int)(Receipt%3));
                Go(ApproachPoint());
                RefreshBubble();
                return;
            }
            if(Basket.Authority!=null)Basket.Unbind();
            Basket.Restore(saved.basket);
            Order = OrderState.Restore(saved, Time.time);
            exitPoint = new Vector3(saved.exitX, saved.exitY, saved.exitZ);
            decideAt = 0;
            Current = saved.phase == (int)State.Leaving || Order.Finished ? State.Leaving : State.Queue;
            if (Current == State.Queue && Lane)
            {
                int index = Mathf.Clamp(saved.queueIndex, 0, Lane.Queue.Count);
                Lane.Queue.Insert(index, this);
                Go(Lane.QueuePoint(this));
            }
            else
            {
                approachStep=transform.position.x>58?3:saved.approachStep;
                Go(ExitStep());
            }
            RefreshBubble();
            if(GameSession.Instance.Transactions!=null)GameSession.Instance.Transactions.BindCustomer(this,saved,
                GameSession.Instance.Transactions.Order(saved.receipt)==null);
        }
        void Update()
        {
            if(!GameSession.Instance.CanSimulate || Time.deltaTime <= 0)return;
            if (terminalFeedback && terminalFeedback.IsPlaying) return;
            if (View && Agent) View.SetMotion(Navigation.ActualSpeed(Agent), Basket.Total > 0);
            if(Current==State.Approaching)
            {
                if(Agent&&Navigation.Arrived(Agent))
                {
                    if(approachStep<2)
                    {
                        approachStep++;
                        Go(ApproachPoint());
                    }
                    else JoinQueue();
                }
                return;
            }
            if (Current == State.Queue)
            {
                Order.Expire(Basket, GameSession.Instance.Economy, Time.time);
                if (Order.Finished)
                {
                    Leave();
                    return;
                }
                if (Time.time >= decideAt)
                {
                    decideAt = Time.time + .4f;
                    Go(Lane.QueuePoint(this));
                }
                RefreshBubble();
            }
            else if (Current == State.Leaving && Agent && Navigation.Arrived(Agent))
            {
                if(approachStep<3)
                {
                    approachStep++;
                    Go(ExitStep());
                }
                else GameSession.Instance.Commerce.Recycle(this);
            }
        }
        void Go(Vector3 point)
        {
            if (Agent) Navigation.Go(Agent, point);
        }
        void RefreshBubble()
        {
            bool visible = isActiveAndEnabled && Current == State.Queue && Order != null && Lane;
            if (visible)
            {
                var player = GameSession.Instance.Player;
                visible = Lane.Queue.IndexOf(this) == 0 || player &&
                    (transform.position - player.transform.position).sqrMagnitude < 3;
            }
            if (!visible)
            {
                OrderBubbleLayer.Release(ref orderBubble);
                return;
            }
            orderBubble ??= OrderBubbleLayer.Acquire(transform);
            orderBubble.SetLines(Order.Lines, Order.RemainingPatience(Time.time) / OrderState.Patience);
        }
        void OnDisable()
        {
            OrderBubbleLayer.Release(ref orderBubble);
            if (terminalFeedback) terminalFeedback.ResetFeedback();
        }
        public void Leave()
        {
            if (Current == State.Leaving) return;
            // Hàng đã giao thuộc khách, kể cả khi hết giờ; không chuyển lại vào kho.
            Lane?.Queue.Remove(this);
            Current = State.Leaving;
            approachStep=0;
            Go(ExitStep());
            // Chỉ giữ phần trình bày; hàng và thanh toán đã được chốt trước khi rời quầy.
            if (Order?.Finished == true)
            {
                if (!terminalFeedback) terminalFeedback = gameObject.AddComponent<OrderBubbleTerminalFeedback>();
                terminalFeedback.Show(ref orderBubble, Order.Paid);
            }
            else OrderBubbleLayer.Release(ref orderBubble);
        }
        public CustomerSave Snapshot() => new()
        {
            receipt = Receipt, shop = Shop, lane = Lane ? Lane.Id : "", phase = (int)Current,approachStep=approachStep,
            queueIndex = Lane ? Lane.Queue.IndexOf(this) : -1, remaining = Order?.RemainingPatience(Time.time) ?? 0,
            paid = Order?.Paid ?? false, timedOut = Order?.TimedOut ?? false,
                order = Order?.SnapshotLines() ?? new List<OrderLine>(), basket = Basket.Snapshot(),
            x = transform.position.x, y = transform.position.y, z = transform.position.z, exitX = exitPoint.x,
                exitY = exitPoint.y, exitZ = exitPoint.z
        };
    }
}
