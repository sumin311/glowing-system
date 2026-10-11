using System;

namespace ChickenShop
{
    /// <summary>하루 영업 결과 (정산 화면/저장용).</summary>
    [Serializable]
    public class DailyReport
    {
        public int day;
        public long startMoney;
        public long revenue;          // 메뉴 판매 매출
        public long tips;             // 팁
        public long ingredientCost;   // 재료 구매비
        public long upgradeCost;      // 업그레이드/해금 투자
        public long operatingCost;    // 운영비 (임대료/관리비/인건비)
        public int ordersCompleted;
        public int ordersFailed;      // 조리 대기 시간 초과
        public int customersLost;     // 주문도 못 하고 떠난 손님
        public int chickensWasted;    // 폐기한 치킨
        public float reputationStart;
        public float reputationEnd;

        /// <summary>순이익 = 매출 + 팁 - 재료비 - 운영비 (업그레이드는 투자로 별도 표기)</summary>
        public long NetProfit
        {
            get { return revenue + tips - ingredientCost - operatingCost; }
        }

        public void Reset(int newDay, long money, float reputation)
        {
            day = newDay;
            startMoney = money;
            revenue = tips = ingredientCost = upgradeCost = operatingCost = 0;
            ordersCompleted = ordersFailed = customersLost = chickensWasted = 0;
            reputationStart = reputationEnd = reputation;
        }

        public DailyReport Clone()
        {
            return (DailyReport)MemberwiseClone();
        }
    }
}
