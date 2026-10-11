using System;
using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 화면 중앙 레이캐스트로 상호작용 대상을 찾고, 안내 문구를 제공하며, E 키로 상호작용을 실행한다.
    /// 같은 Collider 를 계속 보고 있으면 GetComponentInParent 를 다시 호출하지 않는다 (캐시).
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private PlayerCarry carry;
        [SerializeField] private float interactRange = 2.6f;
        [SerializeField] private LayerMask interactMask = ~(1 << 2); // Ignore Raycast 제외
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [Tooltip("안내 문구 갱신 주기(초). 매 프레임 문자열을 만들지 않도록 한다.")]
        [SerializeField] private float promptRefreshInterval = 0.1f;

        private Collider lastCollider;
        private Interactable current;
        private string currentPrompt = string.Empty;
        private float promptTimer;
        private InputModeManager inputMode;

        public Interactable Current { get { return current; } }
        public string CurrentPrompt { get { return currentPrompt; } }
        public PlayerCarry Carry { get { return carry; } }

        /// <summary>대상 또는 안내 문구가 바뀌었을 때 (UI 갱신용)</summary>
        public event Action<Interactable, string> PromptChanged;

        private void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            if (carry == null) carry = GetComponent<PlayerCarry>();
            if (viewCamera == null) GameLog.Error("Interact", "카메라가 연결되지 않았습니다.", this);
        }

        private void Start()
        {
            inputMode = GameManager.Instance != null ? GameManager.Instance.InputMode : null;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f || viewCamera == null) return;
            bool canInteract = inputMode == null || inputMode.InteractEnabled;
            UpdateTarget(canInteract);

            promptTimer -= Time.unscaledDeltaTime;
            if (promptTimer <= 0f)
            {
                promptTimer = promptRefreshInterval;
                RefreshPrompt();
            }

            if (canInteract && current != null && Input.GetKeyDown(interactKey))
            {
                try
                {
                    current.Interact(this);
                }
                catch (Exception ex)
                {
                    GameLog.Error("Interact", current.name + " 상호작용 중 오류: " + ex, current);
                }
                RefreshPrompt();
            }
        }

        private void UpdateTarget(bool canInteract)
        {
            Interactable found = null;
            if (canInteract)
            {
                Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, interactRange, interactMask, QueryTriggerInteraction.Collide))
                {
                    if (hit.collider == lastCollider)
                    {
                        found = current;
                    }
                    else
                    {
                        lastCollider = hit.collider;
                        found = hit.collider.GetComponentInParent<Interactable>();
                    }
                }
                else
                {
                    lastCollider = null;
                }
            }
            else
            {
                lastCollider = null;
            }
            if (found != current)
            {
                current = found;
                promptTimer = 0f; // 즉시 갱신
            }
        }

        private void RefreshPrompt()
        {
            string p = current != null && current.isActiveAndEnabled ? current.GetPrompt(this) : string.Empty;
            if (p == null) p = string.Empty;
            if (p != currentPrompt)
            {
                currentPrompt = p;
                PromptChanged?.Invoke(current, currentPrompt);
            }
        }
    }
}
