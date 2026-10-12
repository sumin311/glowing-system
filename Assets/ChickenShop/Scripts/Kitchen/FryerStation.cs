using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 튀김기: 반죽한 닭을 넣으면 가장 오래 기다린 주문에 자동 배정되고, 메뉴별 조리 시간 동안 튀긴다.
    /// 완성 후 꺼내지 않고 overcookSeconds 가 지나면 타 버린다(과조리 - 판매 불가, 주문 배정 해제).
    /// 상태: 비어 있음(미조리) -> 조리 중 -> 완성 -> 과조리
    /// </summary>
    public class FryerStation : WorkStation
    {
        [Header("튀김")]
        [Tooltip("기본 튀김 시간(초). 메뉴 배율과 업그레이드 배율이 곱해진다.")]
        [SerializeField, Min(1f)] private float baseCookSeconds = 14f;
        [Tooltip("완성 후 이 시간이 지나면 탄다(초).")]
        [SerializeField, Min(1f)] private float overcookSeconds = 10f;
        [Tooltip("2번 튀김기: '튀김기 증설' 업그레이드가 있어야 사용 가능")]
        [SerializeField] private bool requiresExtraFryerUpgrade;
        [Header("연출")]
        [SerializeField] private Transform basket;
        [Tooltip("조리 중 바구니가 내려가는 깊이(m). 치킨이 기름 위로 보이도록 절반만 잠기게 한다.")]
        [SerializeField] private float basketDrop = 0.12f;
        [SerializeField] private ParticleSystem bubbles;
        [SerializeField] private WorldLabel statusLabel;

        private float overTimer;
        private Vector3 basketUp;
        private float basketT;

        public bool IsDone { get { return slot != null && !working && slot.Stage == ChickenStage.Fried; } }
        public bool IsBurnt { get { return slot != null && slot.Stage == ChickenStage.Burnt; } }
        public float BaseCookSeconds { get { return baseCookSeconds; } set { baseCookSeconds = Mathf.Max(1f, value); } }

        protected override void Awake()
        {
            base.Awake();
            if (basket == null)
            {
                var t = FindDeep(transform, "Basket");
                if (t != null) basket = t;
            }
            if (basket != null) basketUp = basket.localPosition;
            if (bubbles != null) bubbles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private static Transform FindDeep(Transform root, string childName)
        {
            if (root.name == childName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindDeep(root.GetChild(i), childName);
                if (r != null) return r;
            }
            return null;
        }

        public override bool IsLocked(out string reason)
        {
            if (requiresExtraFryerUpgrade && (GM == null || GM.Progression == null || !GM.Progression.SecondFryerUnlocked))
            {
                reason = "잠김 (업그레이드 '튀김기 증설' 필요)";
                return true;
            }
            reason = null;
            return false;
        }

        protected override string IdleHint
        {
            get
            {
                int oil = GM != null && GM.Inventory != null ? GM.Inventory.Get(IngredientType.FryingOil) : 0;
                return displayName + " - 반죽 입힌 닭을 넣으세요 (기름 " + oil + "회분)";
            }
        }

        protected override bool CanAccept(ChickenItem item, out string reason)
        {
            if (item.IsPacked) { reason = "포장된 음식은 넣을 수 없습니다"; return false; }
            if (item.Stage == ChickenStage.Raw) { reason = "먼저 반죽 작업대에서 튀김옷을 입히세요"; return false; }
            if (item.Stage != ChickenStage.Battered) { reason = "반죽 입힌 닭만 튀길 수 있습니다"; return false; }
            if (GM.Inventory.Get(IngredientType.FryingOil) <= 0) { reason = "식용유 재고가 없습니다 (발주 단말기)"; return false; }
            reason = null;
            return true;
        }

        protected override bool TryConsumeIngredients(ChickenItem item, out string reason)
        {
            return GM.Inventory.TryConsume(IngredientType.FryingOil, 1, out reason);
        }

        protected override string AcceptVerb(ChickenItem item) { return "튀김기에 넣기 (식용유 1)"; }

        protected override float BaseDuration(ChickenItem item)
        {
            // 메뉴별 조리 조건: 배정된 주문의 메뉴 배율
            float menuMul = 1f;
            var order = GM.Orders != null ? GM.Orders.GetActive(item.OrderId) : null;
            if (order != null && GM.Menu != null)
            {
                var def = GM.Menu.Get(order.menu);
                if (def != null) menuMul = def.cookTimeMultiplier;
            }
            return baseCookSeconds * menuMul;
        }

        protected override float ComputeDuration(ChickenItem item)
        {
            float mul = GM != null && GM.Progression != null ? GM.Progression.CookTimeMultiplier : 1f;
            return Mathf.Max(1f, BaseDuration(item) * mul);
        }

        protected override void OnWorkStarted(ChickenItem item)
        {
            item.SetStage(ChickenStage.Frying);
            // 주문 자동 배정 (가장 오래 기다린 미배정 주문)
            var order = GM.Orders != null ? GM.Orders.AssignForFrying(item) : null;
            // 배정 후 메뉴 배율이 반영되도록 시간 재계산
            duration = ComputeDuration(item);
            overTimer = 0f;
            if (bubbles != null) bubbles.Play();
            GameManager.Toast(order != null
                ? "튀김 시작 - 주문 #" + order.id + " " + OrderData.MenuName(order.menu) + " (" + duration.ToString("0") + "초)"
                : "튀김 시작 (아직 배정할 주문이 없습니다)", ToastKind.Info);
        }

        protected override void OnWorkCompleted(ChickenItem item)
        {
            item.SetStage(ChickenStage.Fried);
            item.CookProgress = 1f;
            overTimer = 0f;
            if (GM.Orders != null) GM.Orders.NotifyFried(item);
            GameManager.Toast(displayName + ": 치킨 완성! 타기 전에 꺼내세요.", ToastKind.Success);
        }

        protected override string TakeVerb(ChickenItem item)
        {
            return item.Stage == ChickenStage.Burnt ? "탄 치킨 꺼내기 (쓰레기통에 버리세요)" : "튀긴 치킨 꺼내기";
        }

        protected override void OnItemTaken(ChickenItem item)
        {
            if (bubbles != null) bubbles.Stop();
            overTimer = 0f;
        }

        protected override void Update()
        {
            base.Update();
            if (slot != null)
            {
                if (working) slot.CookProgress = Progress;
                // 완성 후 방치 -> 과조리
                if (!working && slot.Stage == ChickenStage.Fried)
                {
                    overTimer += Time.deltaTime;
                    float danger = Mathf.Clamp01(overTimer / overcookSeconds);
                    if (progressBar != null)
                    {
                        // 완성(초록) -> 위험(빨강) 깜빡임
                        bool blink = danger > 0.6f && Mathf.Repeat(Time.time * 4f, 1f) > 0.5f;
                        progressBar.SetProgress(1f - danger, blink ? Color.red : Color.Lerp(doneColor, new Color(1f, 0.3f, 0.1f), danger));
                    }
                    if (overTimer >= overcookSeconds)
                    {
                        slot.SetStage(ChickenStage.Burnt);
                        if (GM.Orders != null) GM.Orders.ReleaseItem(slot);
                        if (bubbles != null) bubbles.Stop();
                        if (progressBar != null) progressBar.SetProgress(1f, new Color(0.25f, 0.1f, 0.05f));
                        GameManager.Toast(displayName + ": 치킨이 타 버렸습니다! 꺼내서 버리세요.", ToastKind.Error);
                        GameLog.Info("Fryer", "과조리 발생");
                    }
                }
            }
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            bool down = slot != null && (working || slot.Stage == ChickenStage.Fried);
            basketT = Mathf.MoveTowards(basketT, down ? 1f : 0f, Time.deltaTime * 2.5f);
            if (basket != null) basket.localPosition = basketUp + Vector3.down * (basketDrop * basketT);
            if (itemAnchor != null && basket != null) itemAnchor.position = basket.position + Vector3.up * 0.01f;
            if (statusLabel != null)
            {
                string reason;
                if (IsLocked(out reason)) statusLabel.SetText("잠김", new Color(0.8f, 0.8f, 0.8f));
                else if (slot == null) statusLabel.SetText(string.Empty);
                else if (working) statusLabel.SetText("조리 중 " + Mathf.RoundToInt(Progress * 100f) + "%", new Color(1f, 0.85f, 0.3f));
                else if (slot.Stage == ChickenStage.Fried) statusLabel.SetText("완성!", new Color(0.4f, 1f, 0.4f));
                else if (slot.Stage == ChickenStage.Burnt) statusLabel.SetText("과조리", new Color(1f, 0.35f, 0.3f));
            }
        }

        public override void ClearStation()
        {
            base.ClearStation();
            if (bubbles != null) bubbles.Stop();
            overTimer = 0f;
        }
    }
}
