using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 픽업대: 포장된 주문을 올려놓으면 해당 주문의 손님에게 전달되고 결제된다.
    /// (손님을 직접 바라보고 E 를 눌러 전달할 수도 있다)
    /// </summary>
    public class DeliveryCounter : Interactable
    {
        [Tooltip("손님이 음식을 받으러 오는 위치")]
        [SerializeField] private Transform pickupSpot;

        public Transform PickupSpot { get { return pickupSpot != null ? pickupSpot : transform; } }

        public override string GetPrompt(PlayerInteractor player)
        {
            var carry = player.Carry;
            if (carry == null || carry.IsEmpty) return displayName + " - 포장된 주문을 올려 손님에게 전달합니다";
            var item = carry.Held;
            if (!item.IsPacked) return displayName + " - 포장하지 않은 음식은 전달할 수 없습니다";
            return Action("픽업대에 전달 " + (item.OrderId > 0 ? "(주문 #" + item.OrderId + ")" : string.Empty));
        }

        public override void Interact(PlayerInteractor player)
        {
            var gm = GameManager.Instance;
            var carry = player.Carry;
            if (gm == null || gm.Orders == null || carry == null) return;
            if (carry.IsEmpty)
            {
                GameManager.Toast("포장된 주문을 들고 오세요.", ToastKind.Info);
                return;
            }
            string message;
            if (gm.Orders.TryDeliver(carry.Held, null, out message))
            {
                carry.ConsumeHeld();
                GameManager.Toast(message, ToastKind.Success);
            }
            else
            {
                GameManager.Toast(message, ToastKind.Warning);
            }
        }
    }
}
