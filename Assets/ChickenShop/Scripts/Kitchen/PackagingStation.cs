using System.Collections.Generic;
using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 포장 작업대: 완성된 치킨만 포장 가능. 메뉴가 맞는 주문을 찾아 연결하고
    /// 치킨 상자 + 종이봉투 (+ 음료 주문이면 콜라) 를 원자적으로 차감한다.
    /// </summary>
    public class PackagingStation : WorkStation
    {
        private OrderData pendingOrder;

        protected override string IdleHint
        {
            get
            {
                var inv = GM != null ? GM.Inventory : null;
                if (inv == null) return displayName;
                return displayName + " - 완성된 치킨을 들고 오세요 (상자 " + inv.Get(IngredientType.ChickenBox) + ", 봉투 " + inv.Get(IngredientType.PaperBag) + ", 콜라 " + inv.Get(IngredientType.Cola) + ")";
            }
        }

        protected override bool CanAccept(ChickenItem item, out string reason)
        {
            if (item.IsPacked) { reason = "이미 포장되었습니다 - 손님에게 전달하세요"; return false; }
            if (item.Stage == ChickenStage.Burnt) { reason = "탄 치킨은 포장할 수 없습니다 - 버리세요"; return false; }
            if (item.Stage != ChickenStage.Fried) { reason = "완성된 치킨만 포장할 수 있습니다"; return false; }
            if (GM.Orders == null) { reason = "주문 시스템 없음"; return false; }
            var order = GM.Orders.FindOrderForPacking(item, out reason);
            if (order == null) return false;
            reason = null;
            return true;
        }

        protected override bool TryConsumeIngredients(ChickenItem item, out string reason)
        {
            // CanAccept 와 같은 주문을 다시 찾는다 (그 사이 상태가 바뀌었을 수 있음)
            pendingOrder = GM.Orders.FindOrderForPacking(item, out reason);
            if (pendingOrder == null) return false;
            var list = new List<KeyValuePair<IngredientType, int>>
            {
                new KeyValuePair<IngredientType, int>(IngredientType.ChickenBox, 1),
                new KeyValuePair<IngredientType, int>(IngredientType.PaperBag, 1),
            };
            if (pendingOrder.withDrink) list.Add(new KeyValuePair<IngredientType, int>(IngredientType.Cola, 1));
            return GM.Inventory.TryConsumeAll(out reason, list.ToArray());
        }

        protected override string AcceptVerb(ChickenItem item)
        {
            string r;
            var order = GM.Orders != null ? GM.Orders.FindOrderForPacking(item, out r) : null;
            return order != null ? "포장하기 → 주문 " + order.ShortLabel() : "포장하기";
        }

        protected override void OnWorkStarted(ChickenItem item)
        {
            // 작업 시작 순간 주문을 이 아이템에 확정 배정 (다른 치킨이 가로채지 않도록)
            if (pendingOrder != null) GM.Orders.ReserveForPacking(pendingOrder, item);
        }

        protected override void OnWorkCompleted(ChickenItem item)
        {
            var order = pendingOrder != null ? GM.Orders.GetActive(pendingOrder.id) : null;
            if (order == null || order.assignedItemId != item.ItemId)
            {
                // 포장 중 주문이 시간 초과된 경우: 같은 메뉴의 다른 주문을 다시 찾는다
                string reason;
                order = GM.Orders.FindOrderForPacking(item, out reason);
                if (order != null) GM.Orders.ReserveForPacking(order, item);
            }
            bool drink = order != null ? order.withDrink : (pendingOrder != null && pendingOrder.withDrink);
            item.Pack(order != null ? order.id : -1, drink);
            if (order != null) GM.Orders.MarkPacked(order, item);
            else GameManager.Toast("포장 중 주문이 취소되었습니다. 같은 메뉴 주문이 들어오면 전달할 수 있습니다.", ToastKind.Warning);
            pendingOrder = null;
        }

        protected override string TakeVerb(ChickenItem item) { return "포장된 주문 가져가기 " + (item.OrderId > 0 ? "#" + item.OrderId : string.Empty); }
    }
}
