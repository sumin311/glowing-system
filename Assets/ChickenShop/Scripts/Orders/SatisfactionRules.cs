using System;

namespace ChickenShop
{
    /// <summary>
    /// 만족도/평판/팁 계산 규칙 (순수 C#). 수치는 OrderManager 의 Inspector 값으로 주입된다.
    /// </summary>
    [Serializable]
    public class SatisfactionRules
    {
        public float greatThreshold = 0.55f;   // 남은 시간 비율이 이 이상이면 ★★★
        public float goodThreshold = 0.25f;    // 이 이상이면 ★★, 미만이면 ★
        public float greatReputation = 3f;
        public float goodReputation = 1.5f;
        public float okReputation = 0.3f;
        public float orderTimeoutReputation = -6f;   // 음식 대기 시간 초과
        public float walkoutReputation = -3f;        // 주문도 못 하고 떠남
        public float greatTipRate = 0.1f;            // ★★★ 팁 비율

        public int Stars(float remainingRatio)
        {
            if (remainingRatio >= greatThreshold) return 3;
            if (remainingRatio >= goodThreshold) return 2;
            return 1;
        }

        public float ReputationFor(int stars)
        {
            return stars >= 3 ? greatReputation : stars == 2 ? goodReputation : okReputation;
        }

        public long TipFor(int stars, int price)
        {
            if (stars < 3) return 0;
            // 100원 단위 반올림
            long tip = (long)Math.Round(price * greatTipRate / 100.0) * 100;
            return Math.Max(0, tip);
        }
    }
}
