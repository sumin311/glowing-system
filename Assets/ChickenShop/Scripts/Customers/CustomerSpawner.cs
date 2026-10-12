using System.Collections.Generic;
using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 손님 생성/경로/정리. 영업 중에만 손님이 들어오며, 평판과 홍보 업그레이드에 따라 방문 간격이 짧아진다.
    /// 경로는 고정 웨이포인트(입구, 통로)를 조합해 만든다 - NavMesh 베이크가 필요 없다.
    /// </summary>
    [DefaultExecutionOrder(-70)]
    public class CustomerSpawner : MonoBehaviour
    {
        [Header("프리팹 / 자리")]
        [SerializeField] private CustomerAgent customerPrefab;
        [SerializeField] private CustomerQueue queue;
        [SerializeField] private WaitingArea waitingArea;
        [SerializeField] private DeliveryCounter deliveryCounter;

        [Header("경로 웨이포인트")]
        [Tooltip("매장 밖 생성 위치")] [SerializeField] private Transform spawnPoint;
        [Tooltip("출입문 바깥")] [SerializeField] private Transform doorOutside;
        [Tooltip("출입문 안쪽")] [SerializeField] private Transform doorInside;
        [Tooltip("줄 맨 뒤로 들어가는 진입점")] [SerializeField] private Transform queueEntry;
        [Tooltip("퇴장 통로 X 좌표 (줄과 대기석 사이)")] [SerializeField] private float exitCorridorX = -0.9f;
        [Tooltip("홀 안쪽 이동 통로 Z (카운터 앞)")] [SerializeField] private float innerLaneZ = -1.75f;
        [Tooltip("출구로 가는 통로 Z (입구 쪽)")] [SerializeField] private float exitLaneZ = -5.5f;

        [Header("손님 유입")]
        [Tooltip("평판 50, 업그레이드 없음 기준 평균 방문 간격(초)")]
        [SerializeField, Min(2f)] private float baseSpawnInterval = 26f;
        [SerializeField, Min(1f)] private float minSpawnInterval = 8f;
        [SerializeField, Min(0f)] private float firstSpawnDelay = 3f;
        [SerializeField, Min(1)] private int maxActiveCustomers = 9;
        [Tooltip("카운터 앞에서 주문을 받아 주기까지 기다리는 시간(초)")]
        [SerializeField, Min(5f)] private float orderTakePatience = 45f;

        [SerializeField] private string[] customerNames =
        {
            "민준", "서연", "도윤", "하은", "지호", "수아", "예준", "지유", "시우", "하린",
            "주원", "윤서", "지훈", "채원", "건우", "지민", "현우", "다은", "우진", "소율",
        };

        private readonly List<CustomerAgent> active = new List<CustomerAgent>();
        private float spawnTimer;
        private int nextId = 1;

        public CustomerQueue Queue { get { return queue; } }
        public WaitingArea Waiting { get { return waitingArea; } }
        public int ActiveCount { get { return active.Count; } }
        public IReadOnlyList<CustomerAgent> Active { get { return active; } }

        public CustomerAgent CustomerAtCounter
        {
            get
            {
                var f = queue != null ? queue.Front : null;
                return f != null && f.State == CustomerState.Ordering ? f : null;
            }
        }

        public Vector3 PickupPosition
        {
            get { return deliveryCounter != null ? deliveryCounter.PickupSpot.position : transform.position; }
        }

        public float InnerLaneZ { get { return innerLaneZ; } }

        public Vector3 FallbackWaitPosition
        {
            get { return PickupPosition + new Vector3(0f, 0f, -1.2f); }
        }

        private void Awake()
        {
            if (queue == null) queue = GetComponentInChildren<CustomerQueue>();
            if (waitingArea == null) waitingArea = GetComponentInChildren<WaitingArea>();
            if (queue == null || waitingArea == null || spawnPoint == null || doorInside == null)
                GameLog.Error("Spawner", "손님 경로 설정(줄/대기석/입구)이 빠졌습니다. 씬을 다시 생성하세요.", this);
        }

        private void OnEnable()
        {
            spawnTimer = firstSpawnDelay;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Day == null || gm.Day.Phase != ShopPhase.Open) return;
            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f) return;
            if (TrySpawn()) spawnTimer = NextInterval();
            else spawnTimer = 2f; // 자리가 없으면 잠시 후 재시도
        }

        /// <summary>평판이 높을수록, 홍보 레벨이 높을수록 손님이 자주 온다.</summary>
        public float NextInterval()
        {
            var gm = GameManager.Instance;
            float rep = gm != null && gm.Reputation != null ? gm.Reputation.Value : 50f;
            float mul = gm != null && gm.Progression != null ? gm.Progression.SpawnRateMultiplier : 1f;
            float interval = baseSpawnInterval * Mathf.Lerp(1.5f, 0.6f, rep / 100f) / mul;
            interval *= Random.Range(0.75f, 1.25f);
            return Mathf.Max(minSpawnInterval, interval);
        }

        public bool TrySpawn()
        {
            if (active.Count >= maxActiveCustomers || queue == null || !queue.HasSpace) return false;
            if (spawnPoint == null) return false;
            CustomerAgent c;
            if (customerPrefab != null)
            {
                c = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
            }
            else
            {
                c = CreateFallbackCustomer(spawnPoint.position);
            }
            int id = nextId++;
            string n = customerNames != null && customerNames.Length > 0 ? customerNames[Random.Range(0, customerNames.Length)] : "손님";
            c.Init(this, id, n, orderTakePatience, Random.Range(0, int.MaxValue));
            active.Add(c);
            c.BeginEnter();
            GameLog.Info("Spawner", c.DisplayName + " 입장 (현재 " + active.Count + "명)");
            return true;
        }

        public void OnCustomerGone(CustomerAgent c)
        {
            if (c == null || !c.MarkRemoved()) return;
            active.Remove(c);
            if (queue != null) queue.Leave(c);
            Destroy(c.gameObject);
        }

        /// <summary>마감: 아직 주문하지 않은 손님은 돌아간다.</summary>
        public void OnClosing()
        {
            var copy = new List<CustomerAgent>(active);
            foreach (var c in copy)
            {
                if (c != null && c.Order == null) c.LeaveBecauseClosing();
            }
        }

        public void DespawnAll()
        {
            var copy = new List<CustomerAgent>(active);
            foreach (var c in copy)
            {
                if (c == null) continue;
                c.MarkRemoved();
                Destroy(c.gameObject);
            }
            active.Clear();
            if (queue != null) queue.Clear();
            if (waitingArea != null) waitingArea.Clear();
        }

        public CustomerAgent FindByCustomerId(int id)
        {
            for (int i = 0; i < active.Count; i++) if (active[i] != null && active[i].CustomerId == id) return active[i];
            return null;
        }

        // ------------------------------------------------------------------ 경로
        public List<Vector3> BuildEnterRoute(Vector3 slotPosition)
        {
            var r = new List<Vector3>(5);
            if (doorOutside != null) r.Add(doorOutside.position);
            if (doorInside != null) r.Add(doorInside.position);
            if (queueEntry != null) r.Add(queueEntry.position);
            r.Add(slotPosition);
            return r;
        }

        /// <summary>현재 위치에서 출구까지: 안쪽 통로 -> 퇴장 통로 -> 입구 -> 밖.</summary>
        public List<Vector3> BuildExitRoute(Vector3 from)
        {
            var r = new List<Vector3>(7);
            float y = from.y;
            bool inHall = doorInside == null || from.z > doorInside.position.z - 0.5f;
            if (inHall)
            {
                if (from.z > exitLaneZ + 0.8f)
                {
                    // 대기석/카운터 쪽: 안쪽 통로 -> 퇴장 통로(X) -> 출구 통로(Z)
                    if (from.x > exitCorridorX + 0.3f)
                    {
                        r.Add(new Vector3(from.x, y, innerLaneZ));
                        r.Add(new Vector3(exitCorridorX, y, innerLaneZ));
                    }
                    else
                    {
                        r.Add(new Vector3(exitCorridorX, y, from.z));
                    }
                    r.Add(new Vector3(exitCorridorX, y, exitLaneZ));
                }
                else
                {
                    // 이미 입구 쪽 통로 근처
                    r.Add(new Vector3(from.x, y, exitLaneZ));
                }
                if (doorInside != null) r.Add(new Vector3(doorInside.position.x, y, exitLaneZ));
            }
            if (doorOutside != null) r.Add(doorOutside.position);
            if (spawnPoint != null) r.Add(spawnPoint.position);
            return r;
        }

        // ------------------------------------------------------------------ 대체 손님
        private CustomerAgent CreateFallbackCustomer(Vector3 pos)
        {
            GameLog.Warn("Spawner", "손님 프리팹이 없어 기본 도형 손님을 생성합니다.", this);
            var root = new GameObject("Customer");
            root.transform.position = pos;
            var model = new GameObject("Model").transform;
            model.SetParent(root.transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(model, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.5f, 0.9f, 0.4f);
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(head.GetComponent<Collider>());
            head.name = "Head";
            head.transform.SetParent(model, false);
            head.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            head.transform.localScale = Vector3.one * 0.3f;
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.height = 1.8f;
            col.radius = 0.3f;
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            root.AddComponent<CustomerAnimator>();
            return root.AddComponent<CustomerAgent>();
        }
    }
}
