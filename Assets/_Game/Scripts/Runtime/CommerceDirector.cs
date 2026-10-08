using System.Collections.Generic;
using System.Linq;
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
            if (!game.NavigationReady || !game.CanRestore) return;
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
            if (!game.CanSimulate || game.Milestone == 0 || Time.deltaTime <= 0) return;
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
                if (lane.ShopId == shop && lane.IsUnlocked && (!result || Pressure(lane)<Pressure(result))) result = lane;
            return result;
        }
        static int Pressure(CheckoutStation lane)=>lane.Queue.Count+(GameSession.Instance.Commerce?.Customers.Count(c=>c.Current==CustomerAgent.State.Approaching&&c.Lane==lane)??0);

        bool Spawn(CheckoutStation lane, CustomerSave saved)
        {
            Vector3 point = saved == null ? TownLayout.Spawn(sequence) : new Vector3(saved.x, saved.y, saved.z);
            if (!NavMesh.SamplePosition(point, out var hit, 3, NavMesh.AllAreas)) return false;
            CustomerAgent customer;
            if (pool.Count > 0)
            {
                customer = pool.Dequeue(); customer.transform.position = hit.position; customer.gameObject.SetActive(true); Navigation.Warp(customer.Agent,hit.position);
            }
            else
            {
                var root = new GameObject("Customer"); root.transform.SetParent(transform); root.transform.position = hit.position;
                var model = Art.Model(sequence % 2 == 0 ? "customer" : "customer_beach", Vector3.zero, root.transform);
                model.AddComponent<ActorView>(); customer = root.AddComponent<CustomerAgent>(); customer.Initialize();
            }
            if (saved == null) customer.Begin(lane.ShopId, sequence,true); else customer.Restore(saved);
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

    public sealed class CheckoutStation : Station
    {
        public override Vector3 WorkPoint=>transform.position+Vector3.forward*1.65f;
        public override Vector3 WaitingPoint=>HasTownWaitingPoint?TownWaitingPoint:transform.position+Vector3.right*4.8f+Vector3.forward*1.9f;
        public string ShopId;
        public StationZone CashZone;
        public Vector3 CollectionPoint=>transform.position+new Vector3(2.7f,0,.6f);
        public string[] AcceptedItems => GameSession.Instance.Shelves.Where(x=>x.ShopId==ShopId&&x.AllowedItems!=null)
            .SelectMany(x=>x.AllowedItems).Distinct().ToArray();
        public bool AcceptsItem(string id)
        { foreach(var shelf in GameSession.Instance.Shelves)if(shelf.ShopId==ShopId&&System.Array.IndexOf(shelf.AllowedItems??System.Array.Empty<string>(),id)>=0)return true;return false; }
        public readonly List<CustomerAgent> Queue = new();
        long cash;
        int sales;
        public long Cash {get {if(Authority==null)return cash;long total=Authority.View.legacyCash.Find(x=>x.id==Id)?.amount??0;foreach(var p in Authority.View.payments)if(p.counter==Id&&!p.collected)total+=p.amount;return total;} set{if(Authority!=null&&value!=Cash)throw new System.InvalidOperationException("Cash chỉ được cập nhật qua transaction.");cash=value;}}
        public int Sales {get {if(Authority==null)return sales;int total=0;foreach(var p in Authority.View.payments)if(p.counter==Id)total++;return total;} set{if(Authority!=null&&value!=Sales)throw new System.InvalidOperationException("Sales chỉ được cập nhật qua transaction.");sales=value;}}
        public OrderState FrontOrder => Queue.Count > 0 ? Queue[0].Order : null;
        readonly List<GameObject> bills = new();
        long shownCash = -1;
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
            int count = (int)System.Math.Min(20, Cash / 20 + (Cash % 20 > 0 ? 1 : 0));
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

        public long CollectCash(PlayerController player)
        {
            var game = GameSession.Instance;
            if (!IsUnlocked || !player || player != game.Player || (CashZone ? !CashZone.ContainsInteractionPoint(player.transform.position) : Vector3.Distance(player.transform.position,CollectionPoint)>1.2f)) return 0;
            long collected = Authority!=null?Authority.Collect(Id):game.Economy.CollectCash(Cash); if(Authority==null)Cash -= collected;
            if (collected > 0) { game.Say("+" + collected + " xu"); game.Feedback?.PlaySale(); game.Feedback?.CashTransfer(CollectionPoint + Vector3.up, player.transform.position + Vector3.up); }
            return collected;
        }

        public override bool Interact(PlayerController player, bool withdraw) =>
            !withdraw && IsUnlocked && player && ContainsInteractionPoint(player.transform.position) && Serve(player.Carry,player.GetEntityId());

        protected override void LateUpdate()
        {if(StatusLabel){StatusLabel.text="THU NGÂN\n"+Cash+" xu • "+Queue.Count+" chờ";StatusLabel.gameObject.SetActive(!PlayerUsesZones&&IsUnlocked&&GameSession.Instance.Player&&(GameSession.Instance.Player.transform.position-InteractionPoint).sqrMagnitude<36);}}
    }
}


