using UnityEngine;

namespace ChickenShop
{
    /// <summary>재료 냉장고: 빈손으로 E -> 생닭 1마리를 꺼낸다 (생닭 재고 1 차감).</summary>
    public class IngredientFridge : Interactable
    {
        public override string GetPrompt(PlayerInteractor player)
        {
            var gm = GameManager.Instance;
            int n = gm != null && gm.Inventory != null ? gm.Inventory.Get(IngredientType.RawChicken) : 0;
            if (player.Carry == null || !player.Carry.IsEmpty) return displayName + " - 손을 비워야 꺼낼 수 있습니다";
            if (n <= 0) return displayName + " - 생닭 재고 없음 (발주 단말기에서 구매)";
            return Action("생닭 꺼내기 (재고 " + n + ")");
        }

        public override void Interact(PlayerInteractor player)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Inventory == null || gm.Items == null) return;
            var carry = player.Carry;
            if (carry == null || !carry.IsEmpty)
            {
                GameManager.Toast("손에 든 것을 먼저 처리하세요.", ToastKind.Warning);
                return;
            }
            string reason;
            if (!gm.Inventory.TryConsume(IngredientType.RawChicken, 1, out reason))
            {
                GameManager.Toast(reason, ToastKind.Warning);
                return;
            }
            var item = gm.Items.CreateChicken();
            if (!carry.TryHold(item))
            {
                // 들 수 없으면 재고를 되돌려 일관성 유지
                item.Consume();
                gm.Inventory.LoadStateSingle(IngredientType.RawChicken, gm.Inventory.Get(IngredientType.RawChicken) + 1);
                return;
            }
            GameLog.Info("Fridge", "생닭 꺼냄 -> 남은 재고 " + gm.Inventory.Get(IngredientType.RawChicken));
        }
    }
}
