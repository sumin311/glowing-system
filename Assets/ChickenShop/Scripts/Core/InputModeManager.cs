using System.Collections.Generic;
using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 입력 모드 관리: 게임플레이(커서 잠금) / UI(커서 해제).
    /// UI 패널이 하나라도 열려 있으면 커서를 풀고 시점 회전·이동·상호작용을 막는다.
    /// Left Alt 를 누르고 있는 동안에도 커서를 잠시 풀어 HUD 를 클릭할 수 있다 (영업 중 UI 조작용).
    /// </summary>
    [DefaultExecutionOrder(-95)]
    public class InputModeManager : MonoBehaviour
    {
        [Tooltip("이 키를 누르는 동안 커서를 잠시 해제합니다 (시점 회전 정지).")]
        [SerializeField] private KeyCode freeCursorKey = KeyCode.LeftAlt;

        private readonly HashSet<object> uiRequesters = new HashSet<object>();
        private bool lastCursorFree = true;
        private bool initialized;

        /// <summary>UI 가 입력을 점유 중인가 (패널 열림 또는 Alt).</summary>
        public bool IsUIMode
        {
            get { return uiRequesters.Count > 0 || Input.GetKey(freeCursorKey); }
        }

        /// <summary>패널이 열려 있는가 (Alt 제외).</summary>
        public bool HasOpenPanel
        {
            get { return uiRequesters.Count > 0; }
        }

        public bool LookEnabled { get { return !IsUIMode; } }
        public bool MoveEnabled { get { return uiRequesters.Count == 0; } }
        public bool InteractEnabled { get { return uiRequesters.Count == 0; } }

        public void RequestUI(object owner)
        {
            if (owner != null) uiRequesters.Add(owner);
        }

        public void ReleaseUI(object owner)
        {
            if (owner != null) uiRequesters.Remove(owner);
        }

        public void ClearAll()
        {
            uiRequesters.Clear();
        }

        private void Update()
        {
            bool free = IsUIMode;
            if (!initialized || free != lastCursorFree)
            {
                ApplyCursor(free);
                lastCursorFree = free;
                initialized = true;
            }
            // 에디터에서 Game 뷰 클릭 시 잠금이 풀린 경우 복구
            if (!free && Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
            {
                ApplyCursor(false);
            }
        }

        private static void ApplyCursor(bool free)
        {
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }

        private void OnDisable()
        {
            ApplyCursor(true);
        }
    }
}
