using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChickenShop
{
    public enum ToastKind { Info, Success, Warning, Error }

    public enum ShopTab { Supplies, Upgrades, Menus }

    /// <summary>
    /// UI 총괄: HUD, 안내 메시지, 모달 패널(주문/재고, 발주, 정산, 일시정지, 도움말)을 런타임에 구성하고 열고 닫는다.
    /// 키: Tab = 주문·재고 화면, ESC = 패널 닫기 / 일시정지, F1 = 조작법.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private KeyCode ordersKey = KeyCode.Tab;
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private KeyCode helpKey = KeyCode.F1;

        private HUDView hud;
        private ToastView toasts;
        private OrdersInventoryPanel ordersPanel;
        private ShopPanel shopPanel;
        private SettlementPanel settlementPanel;
        private PausePanel pausePanel;
        private HelpPanel helpPanel;
        private readonly List<UIPanel> openStack = new List<UIPanel>();
        private float refreshTimer;

        public Canvas Canvas { get { return canvas; } }
        public PlayerInteractor Interactor { get { return interactor; } }
        public bool AnyPanelOpen { get { return openStack.Count > 0; } }

        private void Awake()
        {
            EnsureCanvas();
            if (interactor == null) interactor = FindObjectOfType<PlayerInteractor>();
            var t = canvas.transform;
            hud = CreateChild<HUDView>(t, "HUD");
            hud.Build(this);
            toasts = CreateChild<ToastView>(t, "Toasts");
            toasts.Build();
            ordersPanel = CreatePanel<OrdersInventoryPanel>(t, "OrdersInventoryPanel");
            shopPanel = CreatePanel<ShopPanel>(t, "ShopPanel");
            settlementPanel = CreatePanel<SettlementPanel>(t, "SettlementPanel");
            helpPanel = CreatePanel<HelpPanel>(t, "HelpPanel");
            pausePanel = CreatePanel<PausePanel>(t, "PausePanel");
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Day != null) gm.Day.DayEnded += OnDayEnded;
            if (gm != null && gm.Save != null) gm.Save.Loaded += OnGameLoaded;
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Day != null) gm.Day.DayEnded -= OnDayEnded;
            if (gm != null && gm.Save != null) gm.Save.Loaded -= OnGameLoaded;
        }

        /// <summary>불러오기 후에는 정산/발주 등 이전 상태의 창을 닫는다.</summary>
        private void OnGameLoaded()
        {
            for (int i = openStack.Count - 1; i >= 0; i--)
            {
                if (openStack[i] != pausePanel) Close(openStack[i]);
            }
        }

        private void EnsureCanvas()
        {
            if (canvas == null)
            {
                var go = new GameObject("UICanvas", typeof(RectTransform));
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                go.AddComponent<GraphicRaycaster>();
                GameLog.Warn("UI", "Canvas 가 연결되지 않아 새로 만들었습니다.", this);
            }
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        private static T CreateChild<T>(Transform parent, string name) where T : Component
        {
            var rt = UIFactory.Rect(parent, name);
            UIFactory.Stretch(rt);
            return rt.gameObject.AddComponent<T>();
        }

        private T CreatePanel<T>(Transform parent, string name) where T : UIPanel
        {
            var p = CreateChild<T>(parent, name);
            p.Setup(this);
            return p;
        }

        // ------------------------------------------------------------------ 입력
        private void Update()
        {
            var gm = GameManager.Instance;
            if (Input.GetKeyDown(pauseKey))
            {
                var top = openStack.Count > 0 ? openStack[openStack.Count - 1] : null;
                if (top != null && top.ClosableByEscape) Close(top);
                else if (top == null || top == settlementPanel) Open(pausePanel);
            }
            else if (Input.GetKeyDown(ordersKey))
            {
                if (ordersPanel.IsOpen) Close(ordersPanel);
                else if (openStack.Count == 0) Open(ordersPanel);
            }
            else if (Input.GetKeyDown(helpKey))
            {
                if (helpPanel.IsOpen) Close(helpPanel);
                else if (!pausePanel.IsOpen) Open(helpPanel);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 개발용 치트: F9 = 자금 +100,000원
            if (Input.GetKeyDown(KeyCode.F9) && gm != null && gm.Economy != null)
            {
                gm.Economy.ReceivePayment(0, 100000);
                ShowToast("[개발용] 자금 +100,000원", ToastKind.Info);
            }
#endif
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = 0.25f;
                for (int i = 0; i < openStack.Count; i++) openStack[i].Refresh();
            }
        }

        // ------------------------------------------------------------------ 패널
        public void Open(UIPanel panel)
        {
            if (panel == null || panel.IsOpen) return;
            panel.gameObject.SetActive(true);
            panel.transform.SetAsLastSibling();
            if (toasts != null) toasts.transform.SetAsLastSibling(); // 안내 메시지는 항상 맨 위
            openStack.Add(panel);
            var gm = GameManager.Instance;
            if (gm != null && gm.InputMode != null) gm.InputMode.RequestUI(panel);
            if (panel.PausesGame && gm != null) gm.SetPaused(true);
            panel.OnOpened();
            panel.Refresh();
        }

        public void Close(UIPanel panel)
        {
            if (panel == null || !panel.IsOpen) return;
            panel.OnClosed();
            panel.gameObject.SetActive(false);
            openStack.Remove(panel);
            var gm = GameManager.Instance;
            if (gm != null && gm.InputMode != null) gm.InputMode.ReleaseUI(panel);
            if (panel.PausesGame && gm != null)
            {
                bool stillPaused = false;
                for (int i = 0; i < openStack.Count; i++) if (openStack[i].PausesGame) stillPaused = true;
                if (!stillPaused) gm.SetPaused(false);
            }
        }

        public void OpenShop(ShopTab tab)
        {
            shopPanel.SetTab(tab);
            Open(shopPanel);
        }

        public void OpenHelp() { Open(helpPanel); }
        public void ClosePause() { Close(pausePanel); }

        private void OnDayEnded(DailyReport report)
        {
            // 정산 화면은 다른 패널(주문/재고 등)을 닫고 연다
            for (int i = openStack.Count - 1; i >= 0; i--)
            {
                if (openStack[i] != pausePanel) Close(openStack[i]);
            }
            settlementPanel.Show(report);
            Open(settlementPanel);
        }

        public void CloseSettlementAndStartNextDay()
        {
            var gm = GameManager.Instance;
            Close(shopPanel);
            Close(settlementPanel);
            if (gm != null && gm.Day != null) gm.Day.StartNextDay();
            ShowToast((gm != null && gm.Day != null ? gm.Day.Day : 0) + "일차 준비! 재료를 확인하고 영업 팻말로 영업을 시작하세요.", ToastKind.Info, 5f);
        }

        // ------------------------------------------------------------------ 안내
        public void ShowToast(string message, ToastKind kind = ToastKind.Info, float duration = 3.2f)
        {
            if (toasts != null) toasts.Show(message, kind, duration);
            GameLog.Info("Toast", message);
        }
    }
}
