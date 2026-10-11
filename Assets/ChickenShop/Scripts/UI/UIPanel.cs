using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 모달 패널 기반 클래스. UIManager 가 열고 닫으며, 열려 있는 동안 커서가 풀리고 플레이어 조작이 멈춘다.
    /// </summary>
    public abstract class UIPanel : MonoBehaviour
    {
        protected UIManager ui;
        protected RectTransform root;

        /// <summary>ESC 로 닫을 수 있는가</summary>
        public virtual bool ClosableByEscape { get { return true; } }
        /// <summary>열려 있는 동안 게임 시간을 멈추는가</summary>
        public virtual bool PausesGame { get { return false; } }
        public bool IsOpen { get { return gameObject.activeSelf; } }

        public void Setup(UIManager manager)
        {
            ui = manager;
            root = (RectTransform)transform;
            Build();
            gameObject.SetActive(false);
        }

        protected abstract void Build();
        public virtual void OnOpened() { }
        public virtual void OnClosed() { }
        /// <summary>열려 있는 동안 주기적으로 호출 (UIManager, 0.25초 간격)</summary>
        public virtual void Refresh() { }

        /// <summary>화면 중앙 창(배경 반투명 + 본문 패널) 생성 도우미.</summary>
        protected RectTransform CreateWindow(string title, Vector2 size)
        {
            UIFactory.Stretch(root);
            var dim = root.gameObject.AddComponent<UnityEngine.UI.Image>();
            dim.color = new Color(0f, 0f, 0f, 0.45f);
            var win = UIFactory.Panel(root, "Window", UIFactory.PanelColor);
            UIFactory.Anchor(win, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var titleBar = UIFactory.Panel(win, "TitleBar", UIFactory.AccentDark);
            UIFactory.Anchor(titleBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 64f));
            var t = UIFactory.Label(titleBar, title, 32, TextAnchor.MiddleCenter, UIFactory.TextMain, true);
            UIFactory.Stretch((RectTransform)t.transform);
            var body = UIFactory.Rect(win, "Body");
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(24f, 20f);
            body.offsetMax = new Vector2(-24f, -80f);
            return body;
        }
    }
}
