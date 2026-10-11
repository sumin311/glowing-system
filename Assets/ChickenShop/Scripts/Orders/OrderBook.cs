using System;
using System.Collections.Generic;

namespace ChickenShop
{
    /// <summary>
    /// 주문 장부 (순수 C#). 주문 생성/배정/상태 진행/결제/시간 초과를 관리한다.
    /// 중복 판매·중복 결제 방지 규칙:
    ///   1) 결제(TryComplete)는 Packed 상태 + 미결제 + 배정된 아이템 id 일치일 때만 성공
    ///   2) 성공 즉시 isPaid=true, status=Completed 로 바뀌므로 두 번째 호출은 항상 실패
    ///   3) 한 아이템은 동시에 한 주문에만 배정된다 (AssignItem 이 기존 배정을 해제)
    /// </summary>
    public class OrderBook
    {
        private readonly List<OrderData> orders = new List<OrderData>();
        private readonly HashSet<int> paidOrderIds = new HashSet<int>();
        private readonly List<OrderData> timeoutBuffer = new List<OrderData>();
        private int nextId = 1;

        public event Action<OrderData> OrderAdded;
        public event Action<OrderData> OrderChanged;

        /// <summary>진행 중 + 오늘 끝난 주문 전체 (UI 표시용)</summary>
        public IReadOnlyList<OrderData> All
        {
            get { return orders; }
        }

        public int NextId
        {
            get { return nextId; }
        }

        public OrderData Create(MenuType menu, bool withDrink, int menuPrice, int drinkPrice, float patience, int customerId)
        {
            var o = new OrderData
            {
                id = nextId++,
                menu = menu,
                withDrink = withDrink,
                menuPrice = menuPrice,
                drinkPrice = drinkPrice,
                status = OrderStatus.Waiting,
                customerId = customerId,
                assignedItemId = -1,
                patience = patience,
                remaining = patience,
            };
            orders.Add(o);
            OrderAdded?.Invoke(o);
            return o;
        }

        public OrderData Get(int id)
        {
            if (id <= 0) return null;
            for (int i = 0; i < orders.Count; i++)
            {
                if (orders[i].id == id) return orders[i];
            }
            return null;
        }

        public OrderData GetActive(int id)
        {
            var o = Get(id);
            return o != null && o.IsActive ? o : null;
        }

        public int CountActive()
        {
            int n = 0;
            for (int i = 0; i < orders.Count; i++) if (orders[i].IsActive) n++;
            return n;
        }

        /// <summary>아이템을 주문에 배정. 이 아이템이 다른 주문에 배정돼 있었다면 그 주문은 해제된다.</summary>
        public bool AssignItem(int orderId, int itemId)
        {
            var o = GetActive(orderId);
            if (o == null || itemId <= 0) return false;
            for (int i = 0; i < orders.Count; i++)
            {
                if (orders[i].assignedItemId == itemId && orders[i].id != orderId) ReleaseInternal(orders[i]);
            }
            o.assignedItemId = itemId;
            OrderChanged?.Invoke(o);
            return true;
        }

        /// <summary>주문의 아이템 배정 해제 (아이템 폐기/탐/다른 주문으로 이동). 진행 상태는 접수로 되돌린다.</summary>
        public void Release(int orderId)
        {
            var o = GetActive(orderId);
            if (o == null) return;
            ReleaseInternal(o);
        }

        private void ReleaseInternal(OrderData o)
        {
            o.assignedItemId = -1;
            if (o.IsActive && o.status != OrderStatus.Waiting) o.status = OrderStatus.Waiting;
            OrderChanged?.Invoke(o);
        }

        /// <summary>상태를 앞으로만 진행시킨다 (Completed/Failed 는 전용 메서드로만).</summary>
        public bool Advance(int orderId, OrderStatus newStatus)
        {
            var o = GetActive(orderId);
            if (o == null) return false;
            if (newStatus == OrderStatus.Completed || newStatus == OrderStatus.Failed) return false;
            if (newStatus <= o.status) return false;
            o.status = newStatus;
            OrderChanged?.Invoke(o);
            return true;
        }

        /// <summary>배정 아이템이 없는 가장 오래된 진행 중 주문 (조건 함수 선택).</summary>
        public OrderData FindOldestUnassigned(Func<OrderData, bool> filter)
        {
            for (int i = 0; i < orders.Count; i++)
            {
                var o = orders[i];
                if (!o.IsActive || o.assignedItemId > 0) continue;
                if (filter != null && !filter(o)) continue;
                return o;
            }
            return null;
        }

        /// <summary>
        /// 결제 확정. 성공하면 true 이고 이후 같은 주문/아이템으로는 다시 성공하지 않는다.
        /// </summary>
        public bool TryComplete(int orderId, int itemId, int stars, out string reason)
        {
            var o = Get(orderId);
            if (o == null) { reason = "존재하지 않는 주문입니다."; return false; }
            if (o.isPaid || paidOrderIds.Contains(orderId)) { reason = "이미 결제된 주문입니다."; return false; }
            if (o.status == OrderStatus.Failed) { reason = "취소된 주문입니다."; return false; }
            if (o.status != OrderStatus.Packed) { reason = "포장이 끝나지 않은 주문입니다."; return false; }
            if (o.assignedItemId != itemId) { reason = "이 주문의 포장이 아닙니다."; return false; }
            o.isPaid = true;
            o.status = OrderStatus.Completed;
            o.satisfactionStars = stars;
            paidOrderIds.Add(orderId);
            reason = null;
            OrderChanged?.Invoke(o);
            return true;
        }

        public bool Fail(int orderId)
        {
            var o = GetActive(orderId);
            if (o == null) return false;
            o.status = OrderStatus.Failed;
            o.assignedItemId = -1;
            OrderChanged?.Invoke(o);
            return true;
        }

        /// <summary>대기 시간 감소. 시간 초과된 주문 목록을 돌려준다 (상태 변경은 호출자가 Fail 로).</summary>
        public List<OrderData> Tick(float dt)
        {
            timeoutBuffer.Clear();
            for (int i = 0; i < orders.Count; i++)
            {
                var o = orders[i];
                if (!o.IsActive) continue;
                // 포장이 끝난 주문은 손님이 받으러 오는 중이므로 대기 시간이 천천히 줄어든다
                float rate = o.status == OrderStatus.Packed ? 0.25f : 1f;
                o.remaining -= dt * rate;
                if (o.remaining <= 0f)
                {
                    o.remaining = 0f;
                    timeoutBuffer.Add(o);
                }
            }
            return timeoutBuffer;
        }

        /// <summary>새 영업일: 끝난 주문 기록 정리 (id 는 계속 증가 - 이전 날 포장과 혼동 방지).</summary>
        public void ClearFinished()
        {
            orders.RemoveAll(o => !o.IsActive);
        }

        public void ClearAll()
        {
            orders.Clear();
        }
    }
}
