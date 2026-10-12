using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChickenShop
{
    /// <summary>장비 업그레이드 정의.</summary>
    [Serializable]
    public class UpgradeDefinition
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public int maxLevel = 1;
        public int baseCost = 50000;
        [Tooltip("레벨마다 비용 배율")] public float costMultiplier = 1.5f;
        [Tooltip("해금 조건: 최소 영업일")] public int requiredDay = 1;
        [Tooltip("해금 조건: 최소 평판")] public float requiredReputation = 0f;
        [Tooltip("레벨당 하루 유지비(원) - 정산 시 운영비에 더해짐")] public int upkeepPerLevel = 0;
    }

    /// <summary>메뉴 해금 정의.</summary>
    [Serializable]
    public class MenuUnlockDefinition
    {
        public MenuType menu;
        public int cost = 70000;
        public int requiredDay = 2;
        public float requiredReputation = 55f;
    }

    /// <summary>
    /// 경영 성장: 업그레이드 레벨, 메뉴 해금, 그리고 그 효과(조리 속도, 손님 인내심, 손님 유입 등)를 계산한다.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class ShopProgression : MonoBehaviour
    {
        public const string FryerSpeed = "fryer_speed";
        public const string ExtraFryer = "fryer_extra";
        public const string Interior = "interior";
        public const string Marketing = "marketing";
        public const string Tools = "tools";

        [SerializeField] private List<UpgradeDefinition> upgrades = DefaultUpgrades();
        [SerializeField] private List<MenuUnlockDefinition> menuUnlocks = DefaultMenuUnlocks();

        [Header("효과 수치 (레벨당)")]
        [SerializeField, Range(0f, 0.3f)] private float fryerSpeedPerLevel = 0.15f;
        [SerializeField, Range(0f, 0.5f)] private float patiencePerLevel = 0.15f;
        [SerializeField, Range(0f, 0.5f)] private float reputationGainPerLevel = 0.1f;
        [SerializeField, Range(0f, 0.5f)] private float spawnRatePerLevel = 0.2f;
        [SerializeField, Range(0f, 0.3f)] private float toolSpeedPerLevel = 0.2f;

        private readonly Dictionary<string, int> levels = new Dictionary<string, int>();
        private readonly HashSet<MenuType> unlockedMenus = new HashSet<MenuType>();

        public event Action Changed;

        public IReadOnlyList<UpgradeDefinition> Upgrades { get { return upgrades; } }
        public IReadOnlyList<MenuUnlockDefinition> MenuUnlocks { get { return menuUnlocks; } }

        private void Reset()
        {
            upgrades = DefaultUpgrades();
            menuUnlocks = DefaultMenuUnlocks();
        }

        private void Awake()
        {
            if (upgrades == null || upgrades.Count == 0) upgrades = DefaultUpgrades();
            if (menuUnlocks == null || menuUnlocks.Count == 0) menuUnlocks = DefaultMenuUnlocks();
        }

        public static List<UpgradeDefinition> DefaultUpgrades()
        {
            return new List<UpgradeDefinition>
            {
                new UpgradeDefinition { id = FryerSpeed, displayName = "고화력 버너", description = "튀김 시간 15% 단축 (레벨당)", maxLevel = 3, baseCost = 60000, costMultiplier = 1.6f, requiredDay = 1, upkeepPerLevel = 2000 },
                new UpgradeDefinition { id = ExtraFryer, displayName = "튀김기 증설", description = "2번 튀김기 사용 가능 (동시에 2마리 조리)", maxLevel = 1, baseCost = 90000, requiredDay = 2, upkeepPerLevel = 5000 },
                new UpgradeDefinition { id = Tools, displayName = "전문 조리 도구", description = "반죽·소스·포장 작업 시간 20% 단축 (레벨당)", maxLevel = 2, baseCost = 35000, costMultiplier = 1.5f, requiredDay = 1 },
                new UpgradeDefinition { id = Interior, displayName = "인테리어 개선", description = "손님 인내심 +15%, 평판 상승 +10% (레벨당)", maxLevel = 3, baseCost = 50000, costMultiplier = 1.7f, requiredDay = 2, requiredReputation = 45f, upkeepPerLevel = 3000 },
                new UpgradeDefinition { id = Marketing, displayName = "전단지 홍보", description = "손님 방문 빈도 +20% (레벨당)", maxLevel = 3, baseCost = 40000, costMultiplier = 1.8f, requiredDay = 2, requiredReputation = 50f, upkeepPerLevel = 2000 },
            };
        }

        public static List<MenuUnlockDefinition> DefaultMenuUnlocks()
        {
            return new List<MenuUnlockDefinition>
            {
                new MenuUnlockDefinition { menu = MenuType.Ganjang, cost = 70000, requiredDay = 2, requiredReputation = 55f },
            };
        }

        // ------------------------------------------------------------------ 상태
        public void ResetForNewGame()
        {
            levels.Clear();
            unlockedMenus.Clear();
            Changed?.Invoke();
        }

        public int GetLevel(string id)
        {
            int lv;
            return id != null && levels.TryGetValue(id, out lv) ? lv : 0;
        }

        public UpgradeDefinition GetDefinition(string id)
        {
            for (int i = 0; i < upgrades.Count; i++) if (upgrades[i].id == id) return upgrades[i];
            return null;
        }

        public int GetNextCost(string id)
        {
            var def = GetDefinition(id);
            if (def == null) return 0;
            return Mathf.RoundToInt(def.baseCost * Mathf.Pow(def.costMultiplier, GetLevel(id)) / 1000f) * 1000;
        }

        public bool IsMenuUnlocked(MenuType menu)
        {
            var catalog = GameManager.Instance != null ? GameManager.Instance.Menu : null;
            var def = catalog != null ? catalog.Get(menu) : null;
            if (def != null && def.unlockedByDefault) return true;
            if (def == null && menu != MenuType.Ganjang) return true;
            return unlockedMenus.Contains(menu);
        }

        public MenuUnlockDefinition GetMenuUnlock(MenuType menu)
        {
            for (int i = 0; i < menuUnlocks.Count; i++) if (menuUnlocks[i].menu == menu) return menuUnlocks[i];
            return null;
        }

        // ------------------------------------------------------------------ 구매
        public bool CanPurchase(string id, out string reason)
        {
            var def = GetDefinition(id);
            if (def == null) { reason = "알 수 없는 업그레이드"; return false; }
            if (GetLevel(id) >= def.maxLevel) { reason = "최대 레벨"; return false; }
            var gm = GameManager.Instance;
            int day = gm != null && gm.Day != null ? gm.Day.Day : 1;
            float rep = gm != null && gm.Reputation != null ? gm.Reputation.Value : 0f;
            if (day < def.requiredDay) { reason = def.requiredDay + "일차부터 가능"; return false; }
            if (rep < def.requiredReputation) { reason = "평판 " + def.requiredReputation.ToString("0") + " 필요"; return false; }
            if (gm != null && gm.Economy != null && !gm.Economy.CanAfford(GetNextCost(id))) { reason = "자금 부족"; return false; }
            reason = null;
            return true;
        }

        public bool TryPurchase(string id, out string message)
        {
            string reason;
            if (!CanPurchase(id, out reason)) { message = reason; return false; }
            var def = GetDefinition(id);
            int cost = GetNextCost(id);
            if (!GameManager.Instance.Economy.TrySpend(cost, ExpenseCategory.Upgrade, out reason)) { message = reason; return false; }
            levels[id] = GetLevel(id) + 1;
            message = def.displayName + " Lv." + levels[id] + " 완료 (" + MoneyFormat.Won(cost) + ")";
            GameLog.Info("Progression", message);
            Changed?.Invoke();
            return true;
        }

        public bool CanUnlockMenu(MenuType menu, out string reason)
        {
            if (IsMenuUnlocked(menu)) { reason = "이미 판매 중"; return false; }
            var def = GetMenuUnlock(menu);
            if (def == null) { reason = "해금 정보 없음"; return false; }
            var gm = GameManager.Instance;
            int day = gm != null && gm.Day != null ? gm.Day.Day : 1;
            float rep = gm != null && gm.Reputation != null ? gm.Reputation.Value : 0f;
            if (day < def.requiredDay) { reason = def.requiredDay + "일차부터 가능"; return false; }
            if (rep < def.requiredReputation) { reason = "평판 " + def.requiredReputation.ToString("0") + " 필요"; return false; }
            if (gm != null && gm.Economy != null && !gm.Economy.CanAfford(def.cost)) { reason = "자금 부족"; return false; }
            reason = null;
            return true;
        }

        public bool TryUnlockMenu(MenuType menu, out string message)
        {
            string reason;
            if (!CanUnlockMenu(menu, out reason)) { message = reason; return false; }
            var def = GetMenuUnlock(menu);
            if (!GameManager.Instance.Economy.TrySpend(def.cost, ExpenseCategory.Upgrade, out reason)) { message = reason; return false; }
            unlockedMenus.Add(menu);
            message = OrderData.MenuName(menu) + " 메뉴 해금! 이제 손님이 주문합니다.";
            GameLog.Info("Progression", message);
            Changed?.Invoke();
            return true;
        }

        // ------------------------------------------------------------------ 효과
        /// <summary>튀김 시간 배율 (작을수록 빠름)</summary>
        public float CookTimeMultiplier { get { return Mathf.Max(0.3f, 1f - fryerSpeedPerLevel * GetLevel(FryerSpeed)); } }
        /// <summary>반죽/소스/포장 작업 시간 배율</summary>
        public float WorkTimeMultiplier { get { return Mathf.Max(0.3f, 1f - toolSpeedPerLevel * GetLevel(Tools)); } }
        public float PatienceMultiplier { get { return 1f + patiencePerLevel * GetLevel(Interior); } }
        public float ReputationGainMultiplier { get { return 1f + reputationGainPerLevel * GetLevel(Interior); } }
        public float SpawnRateMultiplier { get { return 1f + spawnRatePerLevel * GetLevel(Marketing); } }
        public bool SecondFryerUnlocked { get { return GetLevel(ExtraFryer) > 0; } }

        public long DailyUpkeep
        {
            get
            {
                long sum = 0;
                for (int i = 0; i < upgrades.Count; i++) sum += (long)upgrades[i].upkeepPerLevel * GetLevel(upgrades[i].id);
                return sum;
            }
        }

        // ------------------------------------------------------------------ 저장
        public List<UpgradeLevelEntry> GetLevelEntries()
        {
            var list = new List<UpgradeLevelEntry>();
            foreach (var kv in levels) list.Add(new UpgradeLevelEntry { id = kv.Key, level = kv.Value });
            return list;
        }

        public List<int> GetUnlockedMenus()
        {
            var list = new List<int>();
            foreach (var m in unlockedMenus) list.Add((int)m);
            return list;
        }

        public void LoadState(List<UpgradeLevelEntry> savedLevels, List<int> savedMenus)
        {
            levels.Clear();
            unlockedMenus.Clear();
            if (savedLevels != null)
            {
                foreach (var e in savedLevels)
                {
                    var def = GetDefinition(e.id);
                    if (def == null) { GameLog.Warn("Progression", "저장 파일의 알 수 없는 업그레이드: " + e.id); continue; }
                    levels[e.id] = Mathf.Clamp(e.level, 0, def.maxLevel);
                }
            }
            if (savedMenus != null)
            {
                foreach (var m in savedMenus)
                {
                    if (Enum.IsDefined(typeof(MenuType), m)) unlockedMenus.Add((MenuType)m);
                }
            }
            Changed?.Invoke();
        }
    }

    [Serializable]
    public class UpgradeLevelEntry
    {
        public string id;
        public int level;
    }
}
