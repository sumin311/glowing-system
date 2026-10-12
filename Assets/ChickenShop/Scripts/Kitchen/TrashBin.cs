using UnityEngine;

namespace ChickenShop
{
    /// <summary>쓰레기통: 손에 든 음식을 버린다 (탄 치킨, 잘못 만든 치킨 처리). 연결된 주문은 배정 해제된다.</summary>
    public class TrashBin : Interactable
    {
        public override string GetPrompt(PlayerInteractor player)
        {
            var carry = player.Carry;
            if (carry == null || carry.IsEmpty) return displayName;
            return Action(carry.Held.Describe() + " 버리기");
        }

        public override void Interact(PlayerInteractor player)
        {
            var gm = GameManager.Instance;
            var carry = player.Carry;
            if (gm == null || carry == null || carry.IsEmpty) return;
            var item = carry.Held;
            if (item.IsPacked && item.OrderId > 0 && gm.Orders.GetActive(item.OrderId) != null)
            {
                GameManager.Toast("손님이 기다리는 포장 주문입니다. 버리지 말고 전달하세요.", ToastKind.Warning);
                return;
            }
            string what = item.Describe();
            if (gm.Orders != null) gm.Orders.ReleaseItem(item);
            if (gm.Economy != null) gm.Economy.Today.chickensWasted++;
            carry.ConsumeHeld();
            GameManager.Toast(what + "을(를) 버렸습니다.", ToastKind.Info);
            GameLog.Info("Trash", "폐기: " + what);
        }
    }
}
