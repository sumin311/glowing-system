using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 주문 시스템. 순수 로직(OrderBook)을 감싸 손님·아이템·결제·평판과 연결한다.
    ///  - 주문 생성: 손님이 카운터에서 주문할 때
    ///  - 배정: 튀김기에 넣는 순간 가장 오래 기다린 주문에 자동 배정, 소스/포장 단계에서 메뉴가 다르면 재배정
    ///  - 결제: 포장된 아이템을 해당 손님에게 전달할 때 단 한 번 (OrderBook.TryComplete 가 중복 결제 차단)
    ///  - 시간 초과: 대기 시간이 0 이 되면 실패 처리, 손님 불만족 퇴장, 평판 하락
    /// </summary>
    [DefaultExecutionOrder(-85)]
    public class OrderManager : MonoBehaviour
    {
        [Header("주문")]
        [Tooltip("주문 후 음식이 나올 때까지 손님이 기다리는 기본 시간(초). 인테리어 업그레이드 배율이 곱해진다.")]
        [SerializeField, Min(10f)] private float baseFoodPatience = 150f;
        [SerializeField] private SatisfactionRules rules = new SatisfactionRules();

        private readonly OrderBook book = new OrderBook();
        private readonly Dictionary<int, CustomerAgent> customerByOrder = new Dictionary<int, CustomerAgent>();
        private readonly List<OrderData> scratch = new List<OrderData>();

        public event Action<OrderData> OrderCreated;
        public event Action<OrderData> OrderUpdated;
        /// <summary>(주문, 팁)</summary>
        public event Action<OrderData, long> OrderCompleted;
        public event Action<OrderData> OrderFailed;

        public IReadOnlyList<OrderData> Orders { get { return book.All; } }
        public SatisfactionRules Rules { get { return rules; } }
        public float BaseFoodPatience { get { return baseFoodPatience; } set { baseFoodPatience = Mathf.Max(10f, value); } }

        private GameManager GM { get { return GameManager.Instance; } }

        private void OnEnable()
        {
            book.OrderChanged += HandleBookChanged;
        }

        private void OnDisable()
        {
            book.OrderChanged -= HandleBookChanged;
        }

        private void HandleBookChanged(OrderData o)
        {
            OrderUpdated?.Invoke(o);
        }

        public OrderData Get(int id) { return book.Get(id); }
        public OrderData GetActive(int id) { return book.GetActive(id); }
        public int ActiveCount { get { return book.CountActive(); } }

        public CustomerAgent GetCustomer(int orderId)
        {
            CustomerAgent c;
            return customerByOrder.TryGetValue(orderId, out c) && c != null ? c : null;
        }

        // ------------------------------------------------------------------ 생성
        /// <summary>손님의 주문 생성 (해금된 메뉴 중 가중치 무작위 + 음료 확률).</summary>
        public OrderData CreateOrderFor(CustomerAgent customer)
        {
            if (customer == null) return null;
            if (customer.Order != null && customer.Order.IsActive)
            {
                GameLog.Warn("Orders", "이미 주문한 손님입니다: " + customer.name, customer);
                return customer.Order;
            }
            var gm = GM;
            MenuType menu = PickMenu();
            bool drink = UnityEngine.Random.value < (gm.Menu != null ? gm.Menu.DrinkChance : 0.3f);
            int price = gm.Menu != null ? gm.Menu.Price(menu) : 18000;
            int drinkPrice = gm.Menu != null ? gm.Menu.DrinkPrice : 2000;
            float patience = baseFoodPatience * (gm.Progression != null ? gm.Progression.PatienceMultiplier : 1f);
            var order = book.Create(menu, drink, price, drinkPrice, patience, customer.CustomerId);
            customerByOrder[order.id] = customer;
            GameLog.Info("Orders", "주문 생성 " + order.ShortLabel() + " " + MoneyFormat.Won(order.TotalPrice) + " (대기 " + patience.ToString("0") + "초)");
            OrderCreated?.Invoke(order);
            return order;
        }

        private MenuType PickMenu()
        {
            var gm = GM;
            if (gm.Menu == null) return MenuType.Fried;
            float total = 0f;
            var menus = gm.Menu.Menus;
            for (int i = 0; i < menus.Count; i++)
                if (gm.Progression == null || gm.Progression.IsMenuUnlocked(menus[i].type)) total += Mathf.Max(0f, menus[i].orderWeight);
            float r = UnityEngine.Random.value * total;
            for (int i = 0; i < menus.Count; i++)
            {
                if (gm.Progression != null && !gm.Progression.IsMenuUnlocked(menus[i].type)) continue;
                r -= Mathf.Max(0f, menus[i].orderWeight);
                if (r <= 0f) return menus[i].type;
            }
            return MenuType.Fried;
        }

        // ------------------------------------------------------------------ 시간
        private void Update()
        {
            var gm = GM;
            if (gm == null || gm.Day == null || !gm.Day.IsBusinessTime) return;
            var timeouts = book.Tick(Time.deltaTime);
            if (timeouts.Count == 0) return;
            scratch.Clear();
            scratch.AddRange(timeouts); // Fail 이 리스트를 바꾸지 않도록 복사
            for (int i = 0; i < scratch.Count; i++) FailOrder(scratch[i], "대기 시간 초과", true);
        }

        private void FailOrder(OrderData order, string reason, bool penalize)
        {
            int itemId = order.assignedItemId;
            if (!book.Fail(order.id)) return;
            var gm = GM;
            if (itemId > 0 && gm.Items != null)
            {
                var item = gm.Items.Find(itemId);
                if (item != null) item.SetOrder(-1);
            }
            if (gm.Economy != null) gm.Economy.Today.ordersFailed++;
            if (penalize && gm.Reputation != null) gm.Reputation.Change(rules.orderTimeoutReputation, "주문 #" + order.id + " " + reason);
            var customer = GetCustomer(order.id);
            if (customer != null) customer.OnOrderFailed();
            GameLog.Info("Orders", "주문 실패 " + order.ShortLabel() + " - " + reason);
            if (penalize) GameManager.Toast("주문 #" + order.id + " 손님이 기다리다 떠났습니다! (평판 하락)", ToastKind.Error);
            OrderFailed?.Invoke(order);
        }

        /// <summary>영업 종료 시 남은 주문 정리 (평판 페널티 없음 - 마감 대기 시간 내 처리 못한 주문 기록만).</summary>
        public void FailAllActive(string reason)
        {
            scratch.Clear();
            for (int i = 0; i < book.All.Count; i++) if (book.All[i].IsActive) scratch.Add(book.All[i]);
            for (int i = 0; i < scratch.Count; i++) FailOrder(scratch[i], reason, false);
        }

        public void ClearFinished()
        {
            book.ClearFinished();
            var dead = new List<int>();
            foreach (var kv in customerByOrder) if (book.Get(kv.Key) == null || kv.Value == null) dead.Add(kv.Key);
            for (int i = 0; i < dead.Count; i++) customerByOrder.Remove(dead[i]);
        }

        public void ClearAll()
        {
            book.ClearAll();
            customerByOrder.Clear();
        }

        /// <summary>손님이 주문도 못 하고 떠남 (카운터 대기 시간 초과).</summary>
        public void RegisterWalkout(CustomerAgent customer)
        {
            var gm = GM;
            if (gm.Economy != null) gm.Economy.Today.customersLost++;
            if (gm.Reputation != null) gm.Reputation.Change(rules.walkoutReputation, "주문을 받지 않아 손님이 떠남");
            GameManager.Toast("주문을 받지 못해 손님이 떠났습니다. (평판 하락)", ToastKind.Error);
        }

        // ------------------------------------------------------------------ 조리 단계 연결
        /// <summary>튀김기에 넣을 때: 가장 오래 기다린 미배정 주문에 배정하고 '조리 중' 으로.</summary>
        public OrderData AssignForFrying(ChickenItem item)
        {
            var existing = book.GetActive(item.OrderId);
            if (existing != null && existing.assignedItemId == item.ItemId)
            {
                book.Advance(existing.id, OrderStatus.Cooking);
                return existing;
            }
            var order = book.FindOldestUnassigned(o => o.status == OrderStatus.Waiting);
            if (order == null) return null;
            book.AssignItem(order.id, item.ItemId);
            item.SetOrder(order.id);
            book.Advance(order.id, OrderStatus.Cooking);
            return order;
        }

        public void NotifyFried(ChickenItem item)
        {
            var o = book.GetActive(item.OrderId);
            if (o != null && o.assignedItemId == item.ItemId) book.Advance(o.id, OrderStatus.Cooked);
            else if (item.OrderId <= 0)
            {
                // 튀기는 동안 들어온 주문에 늦게라도 배정
                var order = book.FindOldestUnassigned(x => x.status == OrderStatus.Waiting);
                if (order != null)
                {
                    book.AssignItem(order.id, item.ItemId);
                    item.SetOrder(order.id);
                    book.Advance(order.id, OrderStatus.Cooked);
                }
            }
        }

        /// <summary>소스 작업 후: 주문 메뉴와 맞으면 '소스 완료', 다르면 같은 메뉴의 다른 주문으로 재배정.</summary>
        public void NotifySauced(ChickenItem item)
        {
            var menu = item.ResultMenu;
            if (!menu.HasValue) return;
            var o = book.GetActive(item.OrderId);
            if (o != null && o.assignedItemId == item.ItemId && o.menu == menu.Value)
            {
                book.Advance(o.id, OrderStatus.Sauced);
                return;
            }
            if (o != null && o.assignedItemId == item.ItemId) book.Release(o.id);
            item.SetOrder(-1);
            var other = book.FindOldestUnassigned(x => x.menu == menu.Value && x.status < OrderStatus.Packed);
            if (other != null)
            {
                book.AssignItem(other.id, item.ItemId);
                item.SetOrder(other.id);
                book.Advance(other.id, OrderStatus.Sauced);
                GameManager.Toast(OrderData.MenuName(menu.Value) + " → 주문 #" + other.id + "에 연결했습니다.", ToastKind.Info);
            }
            else if (o != null)
            {
                GameManager.Toast("주문 #" + o.id + "은 " + OrderData.MenuName(o.menu) + "입니다. 이 치킨과 맞는 주문이 아직 없습니다.", ToastKind.Warning);
            }
        }

        /// <summary>포장할 주문 찾기: 연결된 주문 -> 같은 메뉴의 미배정 주문 -> 같은 메뉴의 미포장 주문 순.</summary>
        public OrderData FindOrderForPacking(ChickenItem item, out string reason)
        {
            reason = null;
            var menu = item.ResultMenu;
            if (!menu.HasValue) { reason = "완성된 치킨만 포장할 수 있습니다"; return null; }
            var own = book.GetActive(item.OrderId);
            if (own != null && own.assignedItemId == item.ItemId && own.status < OrderStatus.Packed)
            {
                if (own.menu == menu.Value) return own;
            }
            var free = book.FindOldestUnassigned(x => x.menu == menu.Value && x.status < OrderStatus.Packed);
            if (free != null) return free;
            var all = book.All;
            for (int i = 0; i < all.Count; i++)
            {
                var x = all[i];
                if (x.IsActive && x.menu == menu.Value && x.status < OrderStatus.Packed && x.assignedItemId != item.ItemId) return x;
            }
            if (own != null && own.menu != menu.Value)
            {
                reason = "주문 #" + own.id + "은 " + OrderData.MenuName(own.menu) + "입니다" +
                         (OrderData.RequiredSauce(own.menu) != SauceType.None && item.Sauce == SauceType.None ? " - 소스를 먼저 바르세요" : string.Empty);
            }
            else
            {
                reason = OrderData.MenuName(menu.Value) + " 주문이 없습니다";
            }
            return null;
        }

        /// <summary>포장 시작 시 주문-아이템 연결 확정 (다른 아이템이 연결돼 있었다면 그 아이템은 미배정이 된다).</summary>
        public void ReserveForPacking(OrderData order, ChickenItem item)
        {
            if (order == null || item == null || !order.IsActive) return;
            if (order.assignedItemId > 0 && order.assignedItemId != item.ItemId && GM.Items != null)
            {
                var other = GM.Items.Find(order.assignedItemId);
                if (other != null) other.SetOrder(-1);
            }
            if (item.OrderId > 0 && item.OrderId != order.id)
            {
                var prev = book.GetActive(item.OrderId);
                if (prev != null && prev.assignedItemId == item.ItemId) book.Release(prev.id);
            }
            book.AssignItem(order.id, item.ItemId);
            item.SetOrder(order.id);
        }

        public void MarkPacked(OrderData order, ChickenItem item)
        {
            if (order == null || item == null) return;
            if (order.assignedItemId != item.ItemId) ReserveForPacking(order, item);
            book.Advance(order.id, OrderStatus.Packed);
            var c = GetCustomer(order.id);
            GameLog.Info("Orders", "포장 완료 " + order.ShortLabel() + (c != null ? " -> 손님 " + c.DisplayName : string.Empty));
        }

        /// <summary>아이템 폐기/과조리 시 주문 배정 해제.</summary>
        public void ReleaseItem(ChickenItem item)
        {
            if (item == null) return;
            var o = book.GetActive(item.OrderId);
            if (o != null && o.assignedItemId == item.ItemId) book.Release(o.id);
            item.SetOrder(-1);
        }

        // ------------------------------------------------------------------ 전달 + 결제
        /// <summary>
        /// 포장된 아이템을 손님에게 전달하고 결제한다. target 이 null 이면 주문한 손님에게 자동 전달(픽업대).
        /// 성공 시 true - 호출자는 아이템을 소멸시켜야 한다.
        /// </summary>
        public bool TryDeliver(ChickenItem item, CustomerAgent target, out string message)
        {
            if (item == null || item.IsConsumed) { message = "전달할 음식이 없습니다."; return false; }
            if (!item.IsPacked) { message = "포장하지 않은 음식은 전달할 수 없습니다. 포장 작업대에서 먼저 포장하세요."; return false; }
            var menu = item.ResultMenu;
            var order = book.GetActive(item.OrderId);
            if (order == null)
            {
                // 원래 주문이 취소된 포장: 같은 메뉴/음료 구성의 다른 주문으로 다시 연결
                order = FindRebindTarget(item, target);
                if (order == null) { message = "이 포장과 맞는 주문이 없습니다."; return false; }
                ReserveForPacking(order, item);
                book.Advance(order.id, OrderStatus.Packed);
                GameLog.Info("Orders", "취소된 포장을 주문 #" + order.id + " 으로 재연결");
            }
            if (!menu.HasValue || order.menu != menu.Value) { message = "포장된 메뉴가 주문과 다릅니다."; return false; }
            if (order.withDrink && !item.HasDrink) { message = "음료(콜라)가 빠졌습니다."; return false; }
            var customer = GetCustomer(order.id);
            if (customer == null) { message = "주문 #" + order.id + " 손님을 찾을 수 없습니다."; GameLog.Warn("Orders", message); return false; }
            if (target != null && target != customer) { message = "이 포장은 주문 #" + order.id + " (" + customer.DisplayName + ") 것입니다."; return false; }
            if (!customer.CanReceive) { message = "손님이 아직 음식을 받을 수 없습니다."; return false; }

            int stars = rules.Stars(order.RemainingRatio);
            long tip = rules.TipFor(stars, order.TotalPrice);
            string reason;
            if (!book.TryComplete(order.id, item.ItemId, stars, out reason))
            {
                message = reason;
                GameLog.Warn("Orders", "결제 거부 #" + order.id + ": " + reason);
                return false;
            }
            var gm = GM;
            gm.Economy.ReceivePayment(order.TotalPrice, tip);
            gm.Economy.Today.ordersCompleted++;
            float mul = gm.Progression != null ? gm.Progression.ReputationGainMultiplier : 1f;
            gm.Reputation.Change(rules.ReputationFor(stars) * mul, "주문 #" + order.id + " 만족도 " + stars);
            customer.ReceiveOrder(stars);
            message = "주문 #" + order.id + " 전달 완료! " + MoneyFormat.SignedWon(order.TotalPrice) + (tip > 0 ? " (팁 " + MoneyFormat.Won(tip) + ")" : string.Empty) + "  " + new string('★', stars);
            GameLog.Info("Orders", "결제 완료 " + order.ShortLabel() + " " + MoneyFormat.Won(order.TotalPrice) + " 팁 " + tip + " 잔액 " + MoneyFormat.Won(gm.Economy.Money));
            OrderCompleted?.Invoke(order, tip);
            return true;
        }

        private OrderData FindRebindTarget(ChickenItem item, CustomerAgent target)
        {
            var menu = item.ResultMenu;
            if (!menu.HasValue) return null;
            if (target != null)
            {
                var o = target.Order;
                if (o != null && o.IsActive && o.menu == menu.Value && o.withDrink == item.HasDrink && o.status < OrderStatus.Packed) return o;
                return null;
            }
            var all = book.All;
            for (int i = 0; i < all.Count; i++)
            {
                var o = all[i];
                if (o.IsActive && o.menu == menu.Value && o.withDrink == item.HasDrink && o.status < OrderStatus.Packed) return o;
            }
            return null;
        }
    }
}
