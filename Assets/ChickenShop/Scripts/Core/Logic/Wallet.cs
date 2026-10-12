using System;

namespace ChickenShop
{
    /// <summary>
    /// 보유 자금과 당일 장부 (순수 C#). 모든 돈의 이동은 이 클래스를 거친다.
    /// 지출은 잔액 부족 시 거부되어 자금이 음수가 되지 않는다(운영비만 예외적으로 음수 허용).
    /// </summary>
    public class Wallet
    {
        public long Money { get; private set; }
        public DailyReport Today { get; private set; }
        public long LifetimeRevenue { get; private set; }

        /// <summary>(변경 후 잔액, 변동액)</summary>
        public event Action<long, long> MoneyChanged;

        public Wallet(long startMoney)
        {
            Money = startMoney;
            Today = new DailyReport();
            Today.Reset(1, startMoney, 50f);
        }

        public void SetMoney(long amount, long lifetimeRevenue)
        {
            long delta = amount - Money;
            Money = amount;
            LifetimeRevenue = lifetimeRevenue;
            MoneyChanged?.Invoke(Money, delta);
        }

        public void BeginDay(int day, float reputation)
        {
            Today = new DailyReport();
            Today.Reset(day, Money, reputation);
        }

        public bool CanAfford(long amount)
        {
            return amount <= Money;
        }

        /// <summary>지출. 잔액이 부족하면 false (아무것도 변하지 않음).</summary>
        public bool TrySpend(long amount, ExpenseCategory category)
        {
            if (amount < 0) return false;
            if (amount > Money) return false;
            Money -= amount;
            Record(category, amount);
            MoneyChanged?.Invoke(Money, -amount);
            return true;
        }

        /// <summary>운영비 등 반드시 빠져나가는 지출 (음수 잔액 허용).</summary>
        public void ForceSpend(long amount, ExpenseCategory category)
        {
            if (amount <= 0) return;
            Money -= amount;
            Record(category, amount);
            MoneyChanged?.Invoke(Money, -amount);
        }

        /// <summary>판매 수입.</summary>
        public void Earn(long price, long tip)
        {
            if (price < 0) price = 0;
            if (tip < 0) tip = 0;
            Money += price + tip;
            LifetimeRevenue += price + tip;
            Today.revenue += price;
            Today.tips += tip;
            MoneyChanged?.Invoke(Money, price + tip);
        }

        private void Record(ExpenseCategory category, long amount)
        {
            switch (category)
            {
                case ExpenseCategory.Ingredients: Today.ingredientCost += amount; break;
                case ExpenseCategory.Upgrade: Today.upgradeCost += amount; break;
                case ExpenseCategory.Operating: Today.operatingCost += amount; break;
            }
        }
    }
}
