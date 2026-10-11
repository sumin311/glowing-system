using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// E 키로 상호작용할 수 있는 모든 오브젝트의 기반 클래스.
    /// 이 컴포넌트가 붙은 오브젝트(또는 그 자식)에 Collider 가 있어야 PlayerInteractor 의 레이캐스트에 걸린다.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField] protected string displayName = "작업대";

        public string DisplayName { get { return displayName; } }

        /// <summary>
        /// 바라볼 때 화면에 표시할 안내 문구. 예: "[E] 생닭 꺼내기 (재고 8)".
        /// 상호작용할 수 없는 상태면 이유를 그대로 보여준다 (E 를 눌러도 이유 안내만 나온다).
        /// </summary>
        public abstract string GetPrompt(PlayerInteractor player);

        /// <summary>E 키를 눌렀을 때.</summary>
        public abstract void Interact(PlayerInteractor player);

        /// <summary>화면 안내 문구용 헬퍼.</summary>
        protected static string Action(string verb)
        {
            return "[E] " + verb;
        }
    }
}
