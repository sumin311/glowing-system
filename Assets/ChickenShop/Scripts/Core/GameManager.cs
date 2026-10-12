using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChickenShop
{
    /// <summary>
    /// 게임 진입점. 각 시스템(매니저)에 대한 참조를 보관하고 새 게임/불러오기, 일시정지, 씬 전환만 담당한다.
    /// 실제 게임 규칙은 각 매니저에 분리되어 있다.
    /// 참조는 에디터 스크립트(Chicken Shop > Create Game Scene)가 연결하며, 비어 있으면 Awake 에서 한 번 자동 탐색한다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("시스템 참조")]
        [SerializeField] private DayCycleManager dayCycle;
        [SerializeField] private OrderManager orders;
        [SerializeField] private InventoryManager inventory;
        [SerializeField] private EconomyManager economy;
        [SerializeField] private ReputationManager reputation;
        [SerializeField] private ShopProgression progression;
        [SerializeField] private MenuCatalog menuCatalog;
        [SerializeField] private SaveManager saveManager;
        [SerializeField] private CustomerSpawner customers;
        [SerializeField] private InputModeManager inputMode;
        [SerializeField] private ItemFactory items;
        [SerializeField] private UIManager ui;

        [Header("플레이어")]
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerCarry playerCarry;

        public DayCycleManager Day { get { return dayCycle; } }
        public OrderManager Orders { get { return orders; } }
        public InventoryManager Inventory { get { return inventory; } }
        public EconomyManager Economy { get { return economy; } }
        public ReputationManager Reputation { get { return reputation; } }
        public ShopProgression Progression { get { return progression; } }
        public MenuCatalog Menu { get { return menuCatalog; } }
        public SaveManager Save { get { return saveManager; } }
        public CustomerSpawner Customers { get { return customers; } }
        public InputModeManager InputMode { get { return inputMode; } }
        public ItemFactory Items { get { return items; } }
        public UIManager UI { get { return ui; } }
        public PlayerController Player { get { return player; } }
        public PlayerCarry Carry { get { return playerCarry; } }

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                GameLog.Error("Game", "GameManager 가 두 개 있습니다. 중복을 제거합니다.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Time.timeScale = 1f;
            ResolveMissingReferences();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>에디터에서 연결이 빠진 경우를 대비한 1회성 탐색 (매 프레임 탐색 아님).</summary>
        private void ResolveMissingReferences()
        {
            if (dayCycle == null) dayCycle = FindObjectOfType<DayCycleManager>();
            if (orders == null) orders = FindObjectOfType<OrderManager>();
            if (inventory == null) inventory = FindObjectOfType<InventoryManager>();
            if (economy == null) economy = FindObjectOfType<EconomyManager>();
            if (reputation == null) reputation = FindObjectOfType<ReputationManager>();
            if (progression == null) progression = FindObjectOfType<ShopProgression>();
            if (menuCatalog == null) menuCatalog = FindObjectOfType<MenuCatalog>();
            if (saveManager == null) saveManager = FindObjectOfType<SaveManager>();
            if (customers == null) customers = FindObjectOfType<CustomerSpawner>();
            if (inputMode == null) inputMode = FindObjectOfType<InputModeManager>();
            if (items == null) items = FindObjectOfType<ItemFactory>();
            if (ui == null) ui = FindObjectOfType<UIManager>();
            if (player == null) player = FindObjectOfType<PlayerController>();
            if (playerCarry == null) playerCarry = FindObjectOfType<PlayerCarry>();

            string missing = string.Empty;
            if (dayCycle == null) missing += " DayCycleManager";
            if (orders == null) missing += " OrderManager";
            if (inventory == null) missing += " InventoryManager";
            if (economy == null) missing += " EconomyManager";
            if (reputation == null) missing += " ReputationManager";
            if (progression == null) missing += " ShopProgression";
            if (menuCatalog == null) missing += " MenuCatalog";
            if (saveManager == null) missing += " SaveManager";
            if (customers == null) missing += " CustomerSpawner";
            if (inputMode == null) missing += " InputModeManager";
            if (items == null) missing += " ItemFactory";
            if (ui == null) missing += " UIManager";
            if (playerCarry == null) missing += " PlayerCarry";
            if (missing.Length > 0)
            {
                GameLog.Error("Game", "필수 시스템이 씬에 없습니다:" + missing + "  -> 메뉴 Chicken Shop > Create Game Scene 으로 씬을 다시 생성하세요.", this);
            }
        }

        private void Start()
        {
            StartSession();
        }

        /// <summary>새 게임 또는 저장 불러오기로 세션을 시작한다.</summary>
        private void StartSession()
        {
            bool loaded = false;
            if (GameLaunchOptions.ContinueGame && saveManager != null && saveManager.HasSave)
            {
                string error;
                loaded = saveManager.TryLoad(out error);
                if (!loaded)
                {
                    GameLog.Error("Game", "저장 불러오기 실패, 새 게임으로 시작합니다: " + error, this);
                    if (ui != null) ui.ShowToast("저장 파일을 불러오지 못해 새 게임으로 시작합니다.", ToastKind.Warning);
                }
            }
            if (!loaded) NewGame();
            GameLaunchOptions.ContinueGame = false;
        }

        public void NewGame()
        {
            GameLog.Info("Game", "새 게임 시작");
            if (economy != null) economy.ResetForNewGame();
            if (inventory != null) inventory.ResetForNewGame();
            if (reputation != null) reputation.ResetForNewGame();
            if (progression != null) progression.ResetForNewGame();
            if (dayCycle != null) dayCycle.BeginDay(1);
            if (ui != null) ui.ShowToast("바삭치킨에 오신 것을 환영합니다! 영업 팻말에서 [E] 를 눌러 영업을 시작하세요.", ToastKind.Info, 6f);
        }

        /// <summary>현재 진행 중인 하루를 정리하고 다른 상태(불러오기)로 교체할 때 호출.</summary>
        public void ResetWorldForLoad()
        {
            if (customers != null) customers.DespawnAll();
            if (orders != null) orders.ClearAll();
            if (items != null) items.DestroyAllItems();
            if (playerCarry != null) playerCarry.Clear();
            var stations = FindObjectsOfType<WorkStation>();
            for (int i = 0; i < stations.Length; i++) stations[i].ClearStation();
        }

        /// <summary>새 영업일: 전날 남은 음식(손/작업대)은 폐기한다.</summary>
        public void ClearKitchenForNewDay()
        {
            int wasted = items != null ? items.LiveCount : 0;
            if (playerCarry != null) playerCarry.Clear();
            var stations = FindObjectsOfType<WorkStation>();
            for (int i = 0; i < stations.Length; i++) stations[i].ClearStation();
            if (items != null) items.DestroyAllItems();
            if (wasted > 0) GameLog.Info("Game", "전날 남은 음식 " + wasted + "개 폐기");
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        public void ReturnToMainMenu()
        {
            SetPaused(false);
            if (inputMode != null) inputMode.ClearAll();
            SceneManager.LoadScene(GameLaunchOptions.MainMenuScene);
        }

        public void QuitGame()
        {
            GameLog.Info("Game", "게임 종료");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>어디서든 쓰는 안내 메시지 단축 메서드.</summary>
        public static void Toast(string message, ToastKind kind = ToastKind.Info)
        {
            if (Instance != null && Instance.ui != null) Instance.ui.ShowToast(message, kind);
            else GameLog.Info("Toast", message);
        }
    }
}
