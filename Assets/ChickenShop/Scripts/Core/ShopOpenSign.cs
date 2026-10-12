using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 영업 팻말: E 로 영업 시작 / 조기 마감. 영업 상태에 따라 팻말 판(Board)을 돌려 OPEN/CLOSED 면을 보여준다.
    /// </summary>
    public class ShopOpenSign : Interactable
    {
        [SerializeField] private Transform board;
        [SerializeField] private WorldLabel label;

        private float boardAngle;

        private void Awake()
        {
            if (board == null) board = CustomerAppearance.FindDeep(transform, "Board");
        }

        public override string GetPrompt(PlayerInteractor player)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Day == null) return displayName;
            switch (gm.Day.Phase)
            {
                case ShopPhase.PreOpen: return Action("영업 시작 (" + gm.Day.Day + "일차)");
                case ShopPhase.Open: return Action("조기 마감 (신규 손님 받지 않기)");
                case ShopPhase.Closing: return displayName + " - 마감 중: 남은 주문을 처리하세요";
                default: return displayName + " - 정산 중";
            }
        }

        public override void Interact(PlayerInteractor player)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Day == null) return;
            if (gm.Day.Phase == ShopPhase.PreOpen)
            {
                string reason;
                if (gm.Day.TryOpenShop(out reason)) GameManager.Toast("영업 시작! 손님이 곧 들어옵니다.", ToastKind.Success);
                else GameManager.Toast(reason, ToastKind.Warning);
            }
            else if (gm.Day.Phase == ShopPhase.Open)
            {
                gm.Day.CloseEarly();
            }
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            bool open = gm != null && gm.Day != null && gm.Day.Phase == ShopPhase.Open;
            float target = open ? 0f : 180f;
            boardAngle = Mathf.MoveTowards(boardAngle, target, Time.deltaTime * 360f);
            if (board != null) board.localRotation = Quaternion.Euler(0f, boardAngle, 0f);
            if (label != null && gm != null && gm.Day != null)
            {
                switch (gm.Day.Phase)
                {
                    case ShopPhase.Open: label.SetText("OPEN 영업 중", new Color(0.5f, 1f, 0.5f)); break;
                    case ShopPhase.Closing: label.SetText("마감 중", new Color(1f, 0.8f, 0.4f)); break;
                    case ShopPhase.PreOpen: label.SetText("CLOSED  [E] 영업 시작", new Color(1f, 0.6f, 0.5f)); break;
                    default: label.SetText("CLOSED", new Color(1f, 0.6f, 0.5f)); break;
                }
            }
        }
    }
}
