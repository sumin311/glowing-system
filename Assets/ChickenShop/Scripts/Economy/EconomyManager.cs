using System;
using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 자금/매출/지출 관리. 실제 돈의 이동은 내부 Wallet 이 담당하고, 이 컴포넌트는 Inspector 수치와 이벤트를 제공한다.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class EconomyManager : MonoBehaviour
    {
        [Header("시작 자금 / 운영비")]
        [SerializeField] private long startingMoney = 120000;
        [Tooltip("하루 영업 마감 시 자동으로 빠져나가는 기본 운영비 (임대료/관리비/인건비)")]
        [SerializeField] private long baseOperatingCost = 45000;

        private Wallet wallet;

        /// <summary>(잔액, 변동액)</summary>
        public event Action<long, long> MoneyChanged;

        public long Money { get { return wallet.Money; } }
        public long LifetimeRevenue { get { return wallet.LifetimeRevenue; } }
        public DailyReport Today { get { return wallet.Today; } }
        public long BaseOperatingCost { get { return baseOperatingCost; } }

        private void Awake()
        {
            wallet = new Wallet(startingMoney);
            wallet.MoneyChanged += OnWalletChanged;
        }

        private void OnDestroy()
        {
            if (wallet != null) wallet.MoneyChanged -= OnWalletChanged;
        }

        private void OnWalletChanged(long money, long delta)
        {
            MoneyChanged?.Invoke(money, delta);
        }

        public void ResetForNewGame()
        {
            wallet.SetMoney(startingMoney, 0);
        }

        public void LoadState(long money, long lifetimeRevenue)
        {
            wallet.SetMoney(money, lifetimeRevenue);
        }

        public void BeginDay(int day, float reputation)
        {
            wallet.BeginDay(day, reputation);
        }

        public bool CanAfford(long amount)
        {
            return wallet.CanAfford(amount);
        }

        /// <summary>지출 시도. 실패 시 reason 에 사용자 안내 문구.</summary>
        public bool TrySpend(long amount, ExpenseCategory category, out string reason)
        {
            if (wallet.TrySpend(amount, category))
            {
                reason = null;
                return true;
            }
            reason = "자금이 부족합니다 (필요 " + MoneyFormat.Won(amount) + ", 보유 " + MoneyFormat.Won(wallet.Money) + ")";
            return false;
        }

        /// <summary>주문 결제 수입.</summary>
        public void ReceivePayment(int price, long tip)
        {
            wallet.Earn(price, tip);
        }

        /// <summary>마감 시 운영비 청구 (업그레이드 유지비 포함). 잔액이 부족해도 빠져나간다.</summary>
        public long ChargeOperatingCost(long extraUpkeep)
        {
            long total = baseOperatingCost + Math.Max(0, extraUpkeep);
            wallet.ForceSpend(total, ExpenseCategory.Operating);
            GameLog.Info("Economy", "운영비 청구 " + MoneyFormat.Won(total) + " -> 잔액 " + MoneyFormat.Won(wallet.Money));
            return total;
        }
    }
}
