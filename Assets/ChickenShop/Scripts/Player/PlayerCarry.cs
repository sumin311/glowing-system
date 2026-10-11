using System;
using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 플레이어가 손에 든 아이템 (한 번에 1개). 아이템은 카메라 앞 HoldAnchor 에 붙어 화면 오른쪽 아래에 보인다.
    /// </summary>
    public class PlayerCarry : MonoBehaviour
    {
        [SerializeField] private Transform holdAnchor;
        [SerializeField] private float heldScale = 0.85f;

        public ChickenItem Held { get; private set; }
        public bool IsEmpty { get { return Held == null; } }

        public event Action<ChickenItem> HeldChanged;

        private void Awake()
        {
            if (holdAnchor == null)
            {
                var cam = GetComponentInChildren<Camera>();
                var go = new GameObject("HoldAnchor");
                go.transform.SetParent(cam != null ? cam.transform : transform, false);
                go.transform.localPosition = new Vector3(0.28f, -0.32f, 0.55f);
                holdAnchor = go.transform;
            }
        }

        /// <summary>아이템을 손에 든다. 이미 들고 있으면 실패.</summary>
        public bool TryHold(ChickenItem item)
        {
            if (item == null) return false;
            if (Held != null)
            {
                GameManager.Toast("손에 이미 " + Held.Describe() + "을(를) 들고 있습니다.", ToastKind.Warning);
                return false;
            }
            Held = item;
            item.AttachTo(holdAnchor, heldScale, Quaternion.Euler(0f, -15f, 0f));
            HeldChanged?.Invoke(Held);
            return true;
        }

        /// <summary>손에서 내려놓는다 (작업대가 가져감). 아이템 소유권은 호출자에게 넘어간다.</summary>
        public ChickenItem Release()
        {
            var item = Held;
            Held = null;
            if (item != null) HeldChanged?.Invoke(null);
            return item;
        }

        /// <summary>손에 든 아이템을 파괴 (전달/폐기 완료).</summary>
        public void ConsumeHeld()
        {
            var item = Release();
            if (item != null) item.Consume();
        }

        public void Clear()
        {
            ConsumeHeld();
        }

        /// <summary>아이템 상태가 바뀌었을 때 UI 갱신 알림.</summary>
        public void NotifyChanged()
        {
            HeldChanged?.Invoke(Held);
        }
    }
}
