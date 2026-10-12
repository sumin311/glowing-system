using UnityEngine;

namespace ChickenShop
{
    /// <summary>반죽 작업대: 생닭에 튀김옷을 입힌다 (튀김가루 1 소모).</summary>
    public class PrepStation : WorkStation
    {
        protected override string IdleHint { get { return displayName + " - 생닭을 들고 오세요 (튀김가루 " + Stock() + ")"; } }

        private int Stock()
        {
            return GM != null && GM.Inventory != null ? GM.Inventory.Get(IngredientType.Batter) : 0;
        }

        protected override bool CanAccept(ChickenItem item, out string reason)
        {
            if (item.IsPacked || item.Stage != ChickenStage.Raw) { reason = "생닭만 반죽할 수 있습니다"; return false; }
            if (Stock() <= 0) { reason = "튀김가루 재고가 없습니다 (발주 단말기)"; return false; }
            reason = null;
            return true;
        }

        protected override bool TryConsumeIngredients(ChickenItem item, out string reason)
        {
            return GM.Inventory.TryConsume(IngredientType.Batter, 1, out reason);
        }

        protected override string AcceptVerb(ChickenItem item) { return "반죽 입히기 (튀김가루 1)"; }

        protected override void OnWorkCompleted(ChickenItem item)
        {
            item.SetStage(ChickenStage.Battered);
        }

        protected override string TakeVerb(ChickenItem item) { return "반죽 입힌 닭 가져가기"; }
    }
}
