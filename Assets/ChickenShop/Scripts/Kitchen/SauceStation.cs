using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 소스 디스펜서 (양념 / 간장). 튀김이 끝난 치킨에 소스를 버무린다 (소스 1 소모).
    /// 후라이드 주문은 소스 작업 없이 바로 포장한다.
    /// 간장 소스통은 간장치킨 메뉴가 해금되어야 사용할 수 있다.
    /// </summary>
    public class SauceStation : WorkStation
    {
        [SerializeField] private SauceType sauceType = SauceType.Yangnyeom;

        public SauceType Sauce { get { return sauceType; } }

        private IngredientType Ingredient
        {
            get { return sauceType == SauceType.Ganjang ? IngredientType.GanjangSauce : IngredientType.YangnyeomSauce; }
        }

        private MenuType Menu
        {
            get { return sauceType == SauceType.Ganjang ? MenuType.Ganjang : MenuType.Yangnyeom; }
        }

        public override bool IsLocked(out string reason)
        {
            if (GM != null && GM.Progression != null && !GM.Progression.IsMenuUnlocked(Menu))
            {
                reason = "잠김 (" + OrderData.MenuName(Menu) + " 메뉴 해금 필요)";
                return true;
            }
            reason = null;
            return false;
        }

        protected override string IdleHint
        {
            get
            {
                int n = GM != null && GM.Inventory != null ? GM.Inventory.Get(Ingredient) : 0;
                return displayName + " - 튀긴 치킨을 들고 오세요 (소스 " + n + ")";
            }
        }

        protected override bool CanAccept(ChickenItem item, out string reason)
        {
            if (item.IsPacked) { reason = "이미 포장되었습니다"; return false; }
            if (item.Stage == ChickenStage.Burnt) { reason = "탄 치킨은 사용할 수 없습니다"; return false; }
            if (item.Stage != ChickenStage.Fried) { reason = "튀김이 끝난 치킨만 소스를 바를 수 있습니다"; return false; }
            if (item.Sauce != SauceType.None) { reason = "이미 소스가 발라져 있습니다"; return false; }
            if (GM.Inventory.Get(Ingredient) <= 0) { reason = GM.Inventory.DisplayName(Ingredient) + " 재고가 없습니다 (발주 단말기)"; return false; }
            reason = null;
            return true;
        }

        protected override bool TryConsumeIngredients(ChickenItem item, out string reason)
        {
            return GM.Inventory.TryConsume(Ingredient, 1, out reason);
        }

        protected override string AcceptVerb(ChickenItem item)
        {
            var order = GM.Orders != null ? GM.Orders.GetActive(item.OrderId) : null;
            string warn = order != null && order.menu != Menu ? " (주의: 주문 #" + order.id + "은 " + OrderData.MenuName(order.menu) + ")" : string.Empty;
            return OrderData.MenuName(Menu).Replace("치킨", string.Empty) + " 소스 바르기" + warn;
        }

        protected override void OnWorkCompleted(ChickenItem item)
        {
            item.ApplySauce(sauceType);
            if (GM.Orders != null) GM.Orders.NotifySauced(item);
        }

        protected override string TakeVerb(ChickenItem item) { return OrderData.MenuName(Menu) + " 가져가기"; }
    }
}
