using System;

namespace ChickenShop
{
    /// <summary>
    /// 주문 한 건의 데이터 (순수 C#). 손님 1명 = 주문 1건 = 치킨 1마리 (+ 선택 음료).
    /// 상태 변경은 OrderBook 을 통해서만 한다.
    /// </summary>
    [Serializable]
    public class OrderData
    {
        public int id;
        public MenuType menu;
        public bool withDrink;
        public int menuPrice;
        public int drinkPrice;
        public OrderStatus status;
        public int customerId;
        /// <summary>이 주문에 연결된 치킨 아이템 id (-1 = 미배정)</summary>
        public int assignedItemId = -1;
        public float patience;       // 최대 대기 시간(초)
        public float remaining;      // 남은 대기 시간(초)
        public bool isPaid;
        public int satisfactionStars; // 전달 시 만족도 1~3

        public int TotalPrice
        {
            get { return menuPrice + (withDrink ? drinkPrice : 0); }
        }

        /// <summary>남은 시간 비율 0~1</summary>
        public float RemainingRatio
        {
            get { return patience <= 0f ? 0f : Math.Max(0f, Math.Min(1f, remaining / patience)); }
        }

        public bool IsActive
        {
            get { return status != OrderStatus.Completed && status != OrderStatus.Failed; }
        }

        public static SauceType RequiredSauce(MenuType menu)
        {
            switch (menu)
            {
                case MenuType.Yangnyeom: return SauceType.Yangnyeom;
                case MenuType.Ganjang: return SauceType.Ganjang;
                default: return SauceType.None;
            }
        }

        public static string MenuName(MenuType menu)
        {
            switch (menu)
            {
                case MenuType.Fried: return "후라이드치킨";
                case MenuType.Yangnyeom: return "양념치킨";
                case MenuType.Ganjang: return "간장치킨";
                default: return menu.ToString();
            }
        }

        public static string StatusName(OrderStatus s)
        {
            switch (s)
            {
                case OrderStatus.Waiting: return "접수";
                case OrderStatus.Cooking: return "조리 중";
                case OrderStatus.Cooked: return "튀김 완료";
                case OrderStatus.Sauced: return "소스 완료";
                case OrderStatus.Packed: return "포장 완료";
                case OrderStatus.Completed: return "전달 완료";
                case OrderStatus.Failed: return "시간 초과";
                default: return s.ToString();
            }
        }

        public string ShortLabel()
        {
            return "#" + id + " " + MenuName(menu) + (withDrink ? "+콜라" : string.Empty);
        }
    }
}
