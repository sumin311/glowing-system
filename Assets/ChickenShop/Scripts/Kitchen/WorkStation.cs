using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 시간이 걸리는 작업대의 공통 동작 (반죽, 튀김, 소스, 포장).
    ///   1) 알맞은 아이템을 들고 E -> 조건/재고 확인 후 재료 차감, 아이템을 작업대에 올리고 작업 시작
    ///   2) 진행 막대가 차는 동안 플레이어는 다른 일을 할 수 있다
    ///   3) 완료 후 빈손으로 E -> 결과물을 손에 든다
    /// 재고 차감은 아이템이 실제로 올라가는 순간 한 번만 일어난다.
    /// </summary>
    public abstract class WorkStation : Interactable
    {
        [Header("작업대")]
        [SerializeField] protected Transform itemAnchor;
        [Tooltip("기본 작업 시간(초). 업그레이드 배율이 곱해진다.")]
        [SerializeField, Min(0.1f)] protected float workSeconds = 3f;
        [SerializeField] protected WorldProgressBar progressBar;
        [SerializeField] protected Color workingColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] protected Color doneColor = new Color(0.3f, 0.9f, 0.3f);

        protected ChickenItem slot;
        protected float timer;
        protected float duration;
        protected bool working;

        public ChickenItem SlotItem { get { return slot; } }
        public bool IsWorking { get { return working; } }
        public float Progress { get { return duration <= 0f ? 0f : Mathf.Clamp01(timer / duration); } }

        protected GameManager GM { get { return GameManager.Instance; } }

        protected virtual void Awake()
        {
            if (itemAnchor == null)
            {
                var a = new GameObject("ItemAnchor").transform;
                a.SetParent(transform, false);
                a.localPosition = new Vector3(0f, 0.92f, -0.1f);
                itemAnchor = a;
                GameLog.Warn("Station", name + ": ItemAnchor 가 없어 기본 위치에 생성했습니다.", this);
            }
        }

        // ------------------------------------------------------------------ 하위 클래스가 정의
        /// <summary>올려놓을 수 있는 아이템인가 (재고 제외 조건).</summary>
        protected abstract bool CanAccept(ChickenItem item, out string reason);
        /// <summary>작업에 필요한 재료를 차감 (원자적). 실패 시 아무것도 차감하지 않는다.</summary>
        protected abstract bool TryConsumeIngredients(ChickenItem item, out string reason);
        /// <summary>작업 완료 시 아이템 상태 변경.</summary>
        protected abstract void OnWorkCompleted(ChickenItem item);
        protected abstract string AcceptVerb(ChickenItem item);
        protected virtual string IdleHint { get { return displayName; } }
        protected virtual string TakeVerb(ChickenItem item) { return item.Describe() + " 가져가기"; }
        protected virtual void OnWorkStarted(ChickenItem item) { }
        protected virtual void OnItemTaken(ChickenItem item) { }
        protected virtual float BaseDuration(ChickenItem item) { return workSeconds; }

        /// <summary>잠김 여부 (업그레이드/메뉴 해금 필요 등).</summary>
        public virtual bool IsLocked(out string reason) { reason = null; return false; }

        protected float WorkMultiplier
        {
            get { return GM != null && GM.Progression != null ? GM.Progression.WorkTimeMultiplier : 1f; }
        }

        protected virtual float ComputeDuration(ChickenItem item)
        {
            return Mathf.Max(0.2f, BaseDuration(item) * WorkMultiplier);
        }

        // ------------------------------------------------------------------ 상호작용
        public override string GetPrompt(PlayerInteractor player)
        {
            string reason;
            if (IsLocked(out reason)) return displayName + " - " + reason;
            var carry = player.Carry;
            if (slot == null)
            {
                if (carry == null || carry.IsEmpty) return IdleHint;
                return CanAccept(carry.Held, out reason) ? Action(AcceptVerb(carry.Held)) : displayName + " - " + reason;
            }
            if (working) return displayName + " - 작업 중 " + Mathf.RoundToInt(Progress * 100f) + "%";
            if (carry != null && carry.IsEmpty) return Action(TakeVerb(slot));
            return displayName + " - 손을 비워야 결과물을 가져갈 수 있습니다";
        }

        public override void Interact(PlayerInteractor player)
        {
            string reason;
            if (IsLocked(out reason)) { GameManager.Toast(displayName + ": " + reason, ToastKind.Warning); return; }
            var carry = player.Carry;
            if (carry == null) return;
            if (slot == null)
            {
                if (carry.IsEmpty) { GameManager.Toast(IdleHint, ToastKind.Info); return; }
                TryPlace(carry);
                return;
            }
            if (working)
            {
                GameManager.Toast(displayName + " 작업 중입니다 (" + Mathf.RoundToInt(Progress * 100f) + "%)", ToastKind.Info);
                return;
            }
            if (!carry.IsEmpty)
            {
                GameManager.Toast("손에 든 것을 먼저 내려놓으세요.", ToastKind.Warning);
                return;
            }
            TakeToPlayer(carry);
        }

        protected bool TryPlace(PlayerCarry carry)
        {
            var item = carry.Held;
            string reason;
            if (!CanAccept(item, out reason)) { GameManager.Toast(reason, ToastKind.Warning); return false; }
            if (!TryConsumeIngredients(item, out reason)) { GameManager.Toast(reason, ToastKind.Warning); return false; }
            carry.Release();
            slot = item;
            item.AttachTo(itemAnchor, 1f, Quaternion.identity);
            timer = 0f;
            duration = ComputeDuration(item);
            working = true;
            OnWorkStarted(item);
            if (progressBar != null) progressBar.SetVisible(true);
            GameLog.Info("Station", displayName + " 작업 시작: " + item.Describe() + " (" + duration.ToString("0.0") + "초)");
            return true;
        }

        protected void TakeToPlayer(PlayerCarry carry)
        {
            var item = slot;
            slot = null;
            working = false;
            if (progressBar != null) progressBar.SetVisible(false);
            OnItemTaken(item);
            if (!carry.TryHold(item))
            {
                // 이론상 도달하지 않음 (빈손 확인 후 호출) - 아이템을 잃지 않도록 되돌린다
                slot = item;
                item.AttachTo(itemAnchor, 1f, Quaternion.identity);
            }
        }

        protected virtual void Update()
        {
            if (slot == null) return;
            if (working)
            {
                timer += Time.deltaTime;
                if (progressBar != null) progressBar.SetProgress(Progress, workingColor);
                if (timer >= duration)
                {
                    working = false;
                    OnWorkCompleted(slot);
                    if (progressBar != null) progressBar.SetProgress(1f, doneColor);
                    GameLog.Info("Station", displayName + " 작업 완료: " + slot.Describe());
                }
            }
        }

        /// <summary>불러오기/새 날 시작 시 작업대 비우기.</summary>
        public virtual void ClearStation()
        {
            if (slot != null) slot.Consume();
            slot = null;
            working = false;
            timer = 0f;
            if (progressBar != null) progressBar.SetVisible(false);
        }

        /// <summary>테스트/자동화용: 작업을 즉시 끝낸다.</summary>
        public void DebugFinishWork()
        {
            if (working) timer = duration;
        }
    }
}
