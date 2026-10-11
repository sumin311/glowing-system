using System;
using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 날짜와 영업 시간, 영업 단계(준비 -> 영업 -> 마감 -> 정산)를 관리한다.
    /// 준비 단계에서는 시간이 흐르지 않으므로 재료 준비와 구매를 여유 있게 할 수 있다.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class DayCycleManager : MonoBehaviour
    {
        [Header("영업 시간 (게임 시각)")]
        [SerializeField, Range(0, 23)] private int openHour = 17;
        [SerializeField, Range(1, 24)] private int closeHour = 23;
        [Tooltip("게임 1분이 실제 몇 초인가. 기본 0.8초 -> 6시간 영업 = 약 4.8분")]
        [SerializeField, Min(0.05f)] private float realSecondsPerGameMinute = 0.8f;
        [Tooltip("마감 후 남은 손님을 최대 몇 초까지 기다릴지 (초과 시 남은 손님은 떠남)")]
        [SerializeField, Min(10f)] private float maxClosingSeconds = 150f;

        public int Day { get; private set; } = 1;
        public ShopPhase Phase { get; private set; } = ShopPhase.PreOpen;
        /// <summary>자정부터 흐른 게임 시간(분)</summary>
        public float ClockMinutes { get; private set; }
        public DailyReport LastReport { get; private set; }

        public event Action<ShopPhase> PhaseChanged;
        public event Action<int> DayBegan;
        public event Action<DailyReport> DayEnded;

        private float closingTimer;

        public int OpenHour { get { return openHour; } }
        public int CloseHour { get { return closeHour; } }
        public bool IsBusinessTime { get { return Phase == ShopPhase.Open || Phase == ShopPhase.Closing; } }

        public string ClockText
        {
            get
            {
                int total = Mathf.FloorToInt(ClockMinutes);
                return string.Format("{0:00}:{1:00}", (total / 60) % 24, total % 60);
            }
        }

        public string PhaseText
        {
            get
            {
                switch (Phase)
                {
                    case ShopPhase.PreOpen: return "영업 준비";
                    case ShopPhase.Open: return "영업 중";
                    case ShopPhase.Closing: return "마감 중";
                    case ShopPhase.Settlement: return "정산";
                    default: return Phase.ToString();
                }
            }
        }

        /// <summary>영업 진행률 0~1 (HUD 시계 바)</summary>
        public float BusinessProgress
        {
            get { return Mathf.InverseLerp(openHour * 60f, closeHour * 60f, ClockMinutes); }
        }

        /// <summary>새 영업일 시작 (준비 단계). 장부를 새로 연다.</summary>
        public void BeginDay(int day)
        {
            Day = Mathf.Max(1, day);
            ClockMinutes = openHour * 60f - 30f;
            closingTimer = 0f;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                if (gm.Economy != null) gm.Economy.BeginDay(Day, gm.Reputation != null ? gm.Reputation.Value : 50f);
                if (gm.Orders != null) gm.Orders.ClearFinished();
            }
            GameLog.Info("Day", Day + "일차 준비 시작");
            SetPhase(ShopPhase.PreOpen);
            DayBegan?.Invoke(Day);
        }

        public bool TryOpenShop(out string reason)
        {
            if (Phase != ShopPhase.PreOpen) { reason = "지금은 영업을 시작할 수 없습니다."; return false; }
            ClockMinutes = openHour * 60f;
            SetPhase(ShopPhase.Open);
            reason = null;
            GameLog.Info("Day", Day + "일차 영업 시작");
            return true;
        }

        /// <summary>조기 마감 (신규 손님 입장 중지).</summary>
        public void CloseEarly()
        {
            if (Phase == ShopPhase.Open) BeginClosing();
        }

        private void BeginClosing()
        {
            closingTimer = 0f;
            SetPhase(ShopPhase.Closing);
            GameLog.Info("Day", "마감 시작 - 남은 손님 응대");
            var gm = GameManager.Instance;
            if (gm != null && gm.Customers != null) gm.Customers.OnClosing();
            GameManager.Toast("마감 시간입니다. 남은 주문을 처리하세요.", ToastKind.Info);
        }

        private void Update()
        {
            if (Phase == ShopPhase.Open || Phase == ShopPhase.Closing)
            {
                ClockMinutes += Time.deltaTime / realSecondsPerGameMinute;
            }
            if (Phase == ShopPhase.Open && ClockMinutes >= closeHour * 60f)
            {
                BeginClosing();
            }
            else if (Phase == ShopPhase.Closing)
            {
                closingTimer += Time.deltaTime;
                var gm = GameManager.Instance;
                int remaining = gm != null && gm.Customers != null ? gm.Customers.ActiveCount : 0;
                if (remaining == 0 || closingTimer >= maxClosingSeconds)
                {
                    EndDay();
                }
            }
        }

        /// <summary>영업 종료 -> 운영비 청구, 정산 보고서 생성, 자동 저장.</summary>
        private void EndDay()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.Customers != null) gm.Customers.DespawnAll();
            if (gm.Orders != null) gm.Orders.FailAllActive("영업 종료");
            long upkeep = gm.Progression != null ? gm.Progression.DailyUpkeep : 0;
            gm.Economy.ChargeOperatingCost(upkeep);
            var report = gm.Economy.Today;
            report.reputationEnd = gm.Reputation.Value;
            LastReport = report.Clone();
            SetPhase(ShopPhase.Settlement);
            GameLog.Info("Day", string.Format("{0}일차 정산: 매출 {1}, 팁 {2}, 재료비 {3}, 운영비 {4}, 순이익 {5}",
                Day, report.revenue, report.tips, report.ingredientCost, report.operatingCost, report.NetProfit));
            DayEnded?.Invoke(LastReport);
            // 정산 직후 자동 저장 (다음 날 준비 상태로 저장됨)
            if (gm.Save != null)
            {
                string err;
                if (!gm.Save.TrySave(out err)) GameLog.Error("Day", "자동 저장 실패: " + err);
            }
        }

        /// <summary>정산 화면에서 "다음 날" 버튼.</summary>
        public void StartNextDay()
        {
            if (Phase != ShopPhase.Settlement) return;
            var g = GameManager.Instance;
            if (g != null) g.ClearKitchenForNewDay();
            BeginDay(Day + 1);
            var gm = GameManager.Instance;
            if (gm != null && gm.Save != null)
            {
                string err;
                if (!gm.Save.TrySave(out err)) GameLog.Error("Day", "자동 저장 실패: " + err);
            }
        }

        private void SetPhase(ShopPhase phase)
        {
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}
