using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChickenShop
{
    /// <summary>재고 품목 정의 (Inspector 에서 가격/시작 수량 조절).</summary>
    [Serializable]
    public class IngredientDefinition
    {
        public IngredientType type;
        public string displayName;
        [Tooltip("1개 구매 가격(원)")] public int unitPrice;
        [Tooltip("새 게임 시작 시 보유 수량")] public int startAmount;
        [Tooltip("이 수량 이하이면 HUD 에 부족 경고")] public int lowStockWarning = 2;
        [Tooltip("구매 버튼 묶음 단위")] public int purchaseBundle = 5;
    }

    /// <summary>
    /// 재고 관리. 작업대는 TryConsume/TryConsumeAll 로만 재고를 사용하고, 구매는 Purchase 로만 한다.
    /// 구매는 "자금 차감 성공 -> 재고 증가" 순서로 처리되어 돈과 재고가 항상 함께 움직인다.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class InventoryManager : MonoBehaviour
    {
        [SerializeField] private List<IngredientDefinition> definitions = DefaultDefinitions();

        private readonly InventoryStock stock = new InventoryStock();
        private readonly Dictionary<IngredientType, IngredientDefinition> lookup = new Dictionary<IngredientType, IngredientDefinition>();
        private readonly List<KeyValuePair<IngredientType, int>> scratch = new List<KeyValuePair<IngredientType, int>>();

        /// <summary>(품목, 변경 후 수량)</summary>
        public event Action<IngredientType, int> Changed;

        public IReadOnlyList<IngredientDefinition> Definitions { get { return definitions; } }

        private void Reset()
        {
            definitions = DefaultDefinitions();
        }

        private void Awake()
        {
            if (definitions == null || definitions.Count == 0) definitions = DefaultDefinitions();
            lookup.Clear();
            foreach (var d in definitions) lookup[d.type] = d;
            // 정의가 빠진 품목도 동작하도록 기본값 보충
            foreach (var d in DefaultDefinitions())
            {
                if (!lookup.ContainsKey(d.type))
                {
                    definitions.Add(d);
                    lookup[d.type] = d;
                    GameLog.Warn("Inventory", d.type + " 정의가 없어 기본값을 사용합니다.", this);
                }
            }
            stock.Changed += OnStockChanged;
        }

        private void OnDestroy()
        {
            stock.Changed -= OnStockChanged;
        }

        private void OnStockChanged(IngredientType type, int amount)
        {
            Changed?.Invoke(type, amount);
        }

        public static List<IngredientDefinition> DefaultDefinitions()
        {
            return new List<IngredientDefinition>
            {
                new IngredientDefinition { type = IngredientType.RawChicken, displayName = "생닭", unitPrice = 6000, startAmount = 8, lowStockWarning = 2, purchaseBundle = 5 },
                new IngredientDefinition { type = IngredientType.Batter, displayName = "튀김가루", unitPrice = 400, startAmount = 12, lowStockWarning = 2, purchaseBundle = 10 },
                new IngredientDefinition { type = IngredientType.FryingOil, displayName = "식용유(1회분)", unitPrice = 600, startAmount = 12, lowStockWarning = 2, purchaseBundle = 10 },
                new IngredientDefinition { type = IngredientType.YangnyeomSauce, displayName = "양념 소스", unitPrice = 700, startAmount = 8, lowStockWarning = 2, purchaseBundle = 5 },
                new IngredientDefinition { type = IngredientType.GanjangSauce, displayName = "간장 소스", unitPrice = 800, startAmount = 0, lowStockWarning = 1, purchaseBundle = 5 },
                new IngredientDefinition { type = IngredientType.ChickenBox, displayName = "치킨 상자", unitPrice = 500, startAmount = 10, lowStockWarning = 2, purchaseBundle = 10 },
                new IngredientDefinition { type = IngredientType.PaperBag, displayName = "종이봉투", unitPrice = 200, startAmount = 10, lowStockWarning = 2, purchaseBundle = 10 },
                new IngredientDefinition { type = IngredientType.Cola, displayName = "콜라", unitPrice = 1000, startAmount = 6, lowStockWarning = 1, purchaseBundle = 6 },
            };
        }

        public IngredientDefinition Definition(IngredientType type)
        {
            IngredientDefinition d;
            return lookup.TryGetValue(type, out d) ? d : null;
        }

        public string DisplayName(IngredientType type)
        {
            var d = Definition(type);
            return d != null && !string.IsNullOrEmpty(d.displayName) ? d.displayName : type.ToString();
        }

        public int Get(IngredientType type) { return stock.Get(type); }

        public bool Has(IngredientType type, int amount) { return stock.Has(type, amount); }

        public bool IsLow(IngredientType type)
        {
            var d = Definition(type);
            return d != null && stock.Get(type) <= d.lowStockWarning;
        }

        public void ResetForNewGame()
        {
            foreach (var d in definitions) stock.Set(d.type, d.startAmount);
        }

        public int[] ToArray() { return stock.ToArray(); }

        public void LoadState(int[] amounts) { stock.FromArray(amounts); }

        /// <summary>단일 품목 수량 직접 설정 (실패한 작업의 재고 되돌리기 등 예외 상황 전용).</summary>
        public void LoadStateSingle(IngredientType type, int amount) { stock.Set(type, amount); }

        /// <summary>단일 재료 사용.</summary>
        public bool TryConsume(IngredientType type, int amount, out string reason)
        {
            if (stock.TryConsume(type, amount))
            {
                reason = null;
                return true;
            }
            reason = DisplayName(type) + " 재고가 부족합니다. 발주 단말기에서 구매하세요.";
            GameLog.Info("Inventory", "재고 부족: " + type);
            return false;
        }

        /// <summary>여러 재료를 한 번에 사용 (전부 있을 때만 차감).</summary>
        public bool TryConsumeAll(out string reason, params KeyValuePair<IngredientType, int>[] items)
        {
            scratch.Clear();
            scratch.AddRange(items);
            IngredientType missing;
            if (stock.TryConsumeAll(scratch, out missing))
            {
                reason = null;
                return true;
            }
            reason = DisplayName(missing) + " 재고가 부족합니다. 발주 단말기에서 구매하세요.";
            return false;
        }

        public long PriceOf(IngredientType type, int quantity)
        {
            var d = Definition(type);
            return d == null ? 0 : (long)d.unitPrice * Math.Max(0, quantity);
        }

        /// <summary>재료 구매: 자금이 충분할 때만 재고가 늘어난다.</summary>
        public bool Purchase(IngredientType type, int quantity, out string message)
        {
            if (quantity <= 0) { message = "구매 수량이 올바르지 않습니다."; return false; }
            var economy = GameManager.Instance != null ? GameManager.Instance.Economy : null;
            if (economy == null) { message = "경제 시스템이 없습니다."; GameLog.Error("Inventory", message, this); return false; }
            long cost = PriceOf(type, quantity);
            string reason;
            if (!economy.TrySpend(cost, ExpenseCategory.Ingredients, out reason))
            {
                message = reason;
                return false;
            }
            stock.Add(type, quantity);
            message = DisplayName(type) + " " + quantity + "개 구매 (" + MoneyFormat.Won(cost) + ")";
            GameLog.Info("Inventory", message + " -> 보유 " + stock.Get(type));
            return true;
        }
    }
}
