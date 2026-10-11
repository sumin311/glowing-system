using System.Collections.Generic;
using UnityEngine;

namespace ChickenShop
{
    /// <summary>손님 상태.</summary>
    public enum CustomerState
    {
        Entering,        // 입구 -> 줄
        Queueing,        // 줄 서서 이동/대기
        Ordering,        // 카운터 앞에서 주문 대기 (플레이어가 주문을 받아야 함)
        MovingToWait,    // 주문 후 대기 자리로 이동
        WaitingForFood,  // 음식 대기 (인내심 감소)
        Pickup,          // 음식 받으러 픽업대로
        Leaving,         // 퇴장
    }

    /// <summary>
    /// 손님 AI. NavMesh 대신 웨이포인트 직선 이동을 사용한다 (매장 구조가 고정되어 있어 안정적이고 별도 베이크가 필요 없음).
    /// 입구 -> 줄 서기 -> 주문 -> 대기 자리 -> 음식 수령 -> 퇴장 흐름을 상태 머신으로 처리한다.
    /// 손님을 바라보고 E: 주문 받기 / 포장된 음식 전달.
    /// </summary>
    public class CustomerAgent : Interactable
    {
        [Header("이동")]
        [SerializeField] private float walkSpeed = 1.35f;
        [SerializeField] private float turnSpeed = 10f;
        [SerializeField] private float arriveDistance = 0.06f;

        [Header("표시")]
        [SerializeField] private CustomerAnimator animator3D;
        [SerializeField] private CustomerAppearance appearance;
        [SerializeField] private WorldLabel bubble;
        [SerializeField] private WorldProgressBar patienceBar;
        [SerializeField] private Transform handAnchor;

        public int CustomerId { get; private set; }
        public CustomerState State { get; private set; }
        public OrderData Order { get; private set; }

        private CustomerSpawner spawner;
        private readonly List<Vector3> path = new List<Vector3>();
        private int pathIndex;
        private Vector3? faceDirOnArrive;
        private System.Action onArrive;
        private int queueSlot = -1;
        private int waitSpot = -1;
        private float orderPatience;
        private float orderTimer;
        private float bubbleTimer;
        private float leaveMoodTimer;
        private string leaveMessage;
        private Color leaveColor;
        private bool removed;

        public bool CanReceive
        {
            get { return Order != null && Order.IsActive && (State == CustomerState.WaitingForFood || State == CustomerState.MovingToWait); }
        }

        public bool IsMoving { get { return pathIndex < path.Count; } }

        public void Init(CustomerSpawner owner, int id, string customerName, float orderTakePatience, int appearanceSeed)
        {
            spawner = owner;
            CustomerId = id;
            displayName = customerName + " 손님";
            name = "Customer_" + id;
            orderPatience = orderTakePatience;
            if (animator3D == null) animator3D = GetComponentInChildren<CustomerAnimator>();
            if (appearance == null) appearance = GetComponentInChildren<CustomerAppearance>();
            if (appearance != null) appearance.Randomize(appearanceSeed);
            if (bubble == null) bubble = WorldLabel.Create(transform, new Vector3(0f, 2.05f, 0f), 44, 0.0035f);
            if (patienceBar == null)
            {
                var go = new GameObject("PatienceBar");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 1.9f, 0f);
                patienceBar = go.AddComponent<WorldProgressBar>();
            }
            if (handAnchor == null) handAnchor = CustomerAppearance.FindDeep(transform, "Arm_R");
            State = CustomerState.Entering;
        }

        // ------------------------------------------------------------------ 이동
        private void SetPath(List<Vector3> points, Vector3? faceDir, System.Action arrive)
        {
            path.Clear();
            path.AddRange(points);
            pathIndex = 0;
            faceDirOnArrive = faceDir;
            onArrive = arrive;
        }

        private void Update()
        {
            float speed = 0f;
            if (pathIndex < path.Count)
            {
                Vector3 target = path[pathIndex];
                target.y = transform.position.y;
                Vector3 to = target - transform.position;
                float dist = to.magnitude;
                float step = walkSpeed * Time.deltaTime;
                if (dist <= Mathf.Max(arriveDistance, step))
                {
                    transform.position = target;
                    pathIndex++;
                    if (pathIndex >= path.Count)
                    {
                        var cb = onArrive;
                        onArrive = null;
                        if (cb != null) cb();
                    }
                }
                else
                {
                    Vector3 dir = to / dist;
                    transform.position += dir * step;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir, Vector3.up), Time.deltaTime * turnSpeed);
                    speed = walkSpeed;
                }
            }
            else if (faceDirOnArrive.HasValue)
            {
                var want = Quaternion.LookRotation(faceDirOnArrive.Value, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, want, Time.deltaTime * turnSpeed * 0.6f);
            }
            if (animator3D != null) animator3D.Speed = speed;

            UpdateStateTimers();
            bubbleTimer -= Time.deltaTime;
            if (bubbleTimer <= 0f)
            {
                bubbleTimer = 0.2f;
                RefreshBubble();
            }
        }

        private void UpdateStateTimers()
        {
            var gm = GameManager.Instance;
            bool timeRuns = gm != null && gm.Day != null && gm.Day.IsBusinessTime;
            if (State == CustomerState.Ordering && timeRuns)
            {
                orderTimer += Time.deltaTime;
                if (orderTimer >= orderPatience)
                {
                    if (gm.Orders != null) gm.Orders.RegisterWalkout(this);
                    LeaveShop("주문을 안 받네요...", new Color(1f, 0.45f, 0.4f), true);
                }
            }
            if (leaveMoodTimer > 0f) leaveMoodTimer -= Time.deltaTime;
        }

        // ------------------------------------------------------------------ 흐름
        /// <summary>스폰 직후: 줄에 합류.</summary>
        public void BeginEnter()
        {
            if (!spawner.Queue.Join(this))
            {
                // 줄이 가득 찼으면 바로 돌아간다 (스포너가 미리 확인하므로 드묾)
                LeaveShop(string.Empty, Color.white, false);
            }
        }

        /// <summary>줄의 자리가 정해지거나 앞으로 당겨질 때 (CustomerQueue 가 호출).</summary>
        public void SetQueueSlot(int index)
        {
            if (State != CustomerState.Entering && State != CustomerState.Queueing && State != CustomerState.Ordering) return;
            bool first = queueSlot < 0;
            queueSlot = index;
            var slot = spawner.Queue.GetSlot(index);
            if (slot == null) return;
            if (State == CustomerState.Ordering) return; // 맨 앞 손님은 그대로
            State = CustomerState.Queueing;
            if (first)
            {
                SetPath(spawner.BuildEnterRoute(slot.position), slot.forward, OnQueueSlotReached);
            }
            else if (pathIndex < path.Count)
            {
                // 아직 이동 중(입구 경로 포함)이면 경유지는 유지하고 최종 목적지만 바꾼다 - 벽을 뚫고 가지 않도록
                path[path.Count - 1] = slot.position;
                faceDirOnArrive = slot.forward;
                onArrive = OnQueueSlotReached;
            }
            else
            {
                SetPath(new List<Vector3> { slot.position }, slot.forward, OnQueueSlotReached);
            }
        }

        private void OnQueueSlotReached()
        {
            if (queueSlot == 0 && State == CustomerState.Queueing)
            {
                State = CustomerState.Ordering;
                orderTimer = 0f;
                GameLog.Info("Customer", DisplayName + " 카운터 도착 - 주문 대기");
            }
        }

        /// <summary>플레이어가 주문을 받음 (손님 상호작용 또는 POS).</summary>
        public void TakeOrder()
        {
            var gm = GameManager.Instance;
            if (State != CustomerState.Ordering)
            {
                GameManager.Toast(DisplayName + "은(는) 아직 주문할 수 없습니다.", ToastKind.Info);
                return;
            }
            if (gm == null || gm.Orders == null) return;
            Order = gm.Orders.CreateOrderFor(this);
            if (Order == null) return;
            spawner.Queue.Leave(this);
            queueSlot = -1;
            waitSpot = spawner.Waiting.Reserve(this, transform.position);
            var spot = spawner.Waiting.GetSpot(waitSpot);
            State = CustomerState.MovingToWait;
            Vector3 dest = spot != null ? spot.position : spawner.FallbackWaitPosition;
            SetPath(new List<Vector3> { dest }, spot != null ? spot.forward : Vector3.forward, () =>
            {
                if (State == CustomerState.MovingToWait) State = CustomerState.WaitingForFood;
            });
            if (animator3D != null) animator3D.PlayWave();
            GameManager.Toast("주문 접수: " + Order.ShortLabel() + " (" + MoneyFormat.Won(Order.TotalPrice) + ")", ToastKind.Success);
        }

        /// <summary>결제 완료 후 음식 수령 (OrderManager 가 호출).</summary>
        public void ReceiveOrder(int stars)
        {
            ReleaseWaitSpot();
            State = CustomerState.Pickup;
            var gm = GameManager.Instance;
            if (gm != null && gm.Items != null)
            {
                Transform anchor = handAnchor != null ? handAnchor : transform;
                var bag = gm.Items.CreateBagVisual(anchor);
                if (bag != null)
                {
                    bag.transform.localPosition = handAnchor != null ? new Vector3(0f, -0.62f, 0f) : new Vector3(0.3f, 0.6f, 0f);
                    bag.transform.localScale = Vector3.one * 0.8f;
                }
            }
            if (animator3D != null) animator3D.PlayWave();
            string msg = stars >= 3 ? "최고예요! " : stars == 2 ? "잘 먹을게요! " : "늦었지만 고마워요. ";
            ShowMood(msg + new string('★', stars), new Color(0.5f, 1f, 0.5f));
            // 다른 대기 손님 사이를 가로지르지 않도록 카운터 앞 통로를 거쳐 픽업대로
            var pickup = spawner.PickupPosition;
            var p = transform.position;
            SetPath(new List<Vector3> { new Vector3(p.x, p.y, spawner.InnerLaneZ), pickup }, Vector3.forward, () => LeaveShop(null, Color.white, false));
        }

        /// <summary>음식 대기 시간 초과 (OrderManager 가 호출).</summary>
        public void OnOrderFailed()
        {
            if (State == CustomerState.Leaving) return;
            LeaveShop("너무 오래 걸려요!", new Color(1f, 0.4f, 0.35f), true);
        }

        /// <summary>마감 시 주문 전 손님 돌려보내기.</summary>
        public void LeaveBecauseClosing()
        {
            if (Order != null) return;
            LeaveShop("영업이 끝났네요...", new Color(0.85f, 0.85f, 0.85f), false);
        }

        private void LeaveShop(string message, Color color, bool angry)
        {
            if (State == CustomerState.Leaving) return;
            if (queueSlot >= 0 || State == CustomerState.Queueing || State == CustomerState.Ordering || State == CustomerState.Entering)
            {
                spawner.Queue.Leave(this);
                queueSlot = -1;
            }
            ReleaseWaitSpot();
            State = CustomerState.Leaving;
            if (!string.IsNullOrEmpty(message)) ShowMood(message, color);
            if (angry && animator3D != null) animator3D.PlayAngry();
            SetPath(spawner.BuildExitRoute(transform.position), null, () => spawner.OnCustomerGone(this));
        }

        private void ShowMood(string message, Color color)
        {
            leaveMessage = message;
            leaveColor = color;
            leaveMoodTimer = 3f;
            bubbleTimer = 0f;
        }

        private void ReleaseWaitSpot()
        {
            if (waitSpot >= 0) spawner.Waiting.Release(waitSpot, this);
            waitSpot = -1;
        }

        private void RefreshBubble()
        {
            if (bubble == null) return;
            if (leaveMoodTimer > 0f && !string.IsNullOrEmpty(leaveMessage))
            {
                bubble.SetText(leaveMessage, leaveColor);
                if (patienceBar != null) patienceBar.SetVisible(false);
                return;
            }
            switch (State)
            {
                case CustomerState.Ordering:
                    bubble.SetText("주문할게요! [E]", new Color(1f, 0.95f, 0.5f));
                    ShowPatience(1f - orderTimer / Mathf.Max(1f, orderPatience));
                    break;
                case CustomerState.MovingToWait:
                case CustomerState.WaitingForFood:
                    if (Order != null)
                    {
                        string status = Order.status == OrderStatus.Packed ? " (포장 완료!)" : string.Empty;
                        bubble.SetText(Order.ShortLabel() + status, Color.white);
                        ShowPatience(Order.RemainingRatio);
                    }
                    break;
                default:
                    bubble.SetText(string.Empty);
                    if (patienceBar != null) patienceBar.SetVisible(false);
                    break;
            }
        }

        private void ShowPatience(float ratio)
        {
            if (patienceBar == null) return;
            patienceBar.SetVisible(true);
            Color c = ratio > 0.55f ? new Color(0.35f, 0.85f, 0.35f) : ratio > 0.25f ? new Color(1f, 0.75f, 0.2f) : new Color(1f, 0.3f, 0.25f);
            patienceBar.SetProgress(ratio, c);
        }

        // ------------------------------------------------------------------ 상호작용
        public override string GetPrompt(PlayerInteractor player)
        {
            switch (State)
            {
                case CustomerState.Ordering:
                    return Action("주문 받기 (" + DisplayName + ")");
                case CustomerState.WaitingForFood:
                case CustomerState.MovingToWait:
                    var carry = player.Carry;
                    if (carry != null && !carry.IsEmpty && carry.Held.IsPacked) return Action("주문 전달 " + Order.ShortLabel());
                    return DisplayName + " - " + Order.ShortLabel() + " 대기 중 (" + OrderData.StatusName(Order.status) + ")";
                case CustomerState.Queueing:
                case CustomerState.Entering:
                    return DisplayName + " - 줄 서는 중";
                default:
                    return DisplayName;
            }
        }

        public override void Interact(PlayerInteractor player)
        {
            if (State == CustomerState.Ordering)
            {
                TakeOrder();
                return;
            }
            if (State == CustomerState.WaitingForFood || State == CustomerState.MovingToWait)
            {
                var carry = player.Carry;
                var gm = GameManager.Instance;
                if (carry == null || carry.IsEmpty)
                {
                    GameManager.Toast(DisplayName + ": \"" + Order.ShortLabel() + " 아직인가요?\"", ToastKind.Info);
                    return;
                }
                string message;
                if (gm.Orders.TryDeliver(carry.Held, this, out message))
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

        /// <summary>스포너가 제거할 때 (중복 제거 방지).</summary>
        public bool MarkRemoved()
        {
            if (removed) return false;
            removed = true;
            ReleaseWaitSpot();
            return true;
        }

        /// <summary>테스트용: 즉시 목적지로 이동.</summary>
        public void DebugSnapToDestination()
        {
            while (pathIndex < path.Count)
            {
                transform.position = path[pathIndex];
                pathIndex++;
            }
            var cb = onArrive;
            onArrive = null;
            if (cb != null) cb();
        }
    }
}
