using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 카운터 POS: 카운터 앞에 선 손님의 주문을 받는다 (손님을 직접 바라보고 E 를 눌러도 된다).
    /// </summary>
    public class OrderTakingPoint : Interactable
    {
        public override string GetPrompt(PlayerInteractor player)
        {
            var gm = GameManager.Instance;
            var c = gm != null && gm.Customers != null ? gm.Customers.CustomerAtCounter : null;
            if (c == null) return displayName + " - 주문할 손님이 없습니다";
            return Action("주문 받기 (" + c.DisplayName + ")");
        }

        public override void Interact(PlayerInteractor player)
        {
            var gm = GameManager.Instance;
            var c = gm != null && gm.Customers != null ? gm.Customers.CustomerAtCounter : null;
            if (c == null)
            {
                GameManager.Toast("카운터 앞에 주문할 손님이 없습니다.", ToastKind.Info);
                return;
            }
            c.TakeOrder();
        }
    }
}
