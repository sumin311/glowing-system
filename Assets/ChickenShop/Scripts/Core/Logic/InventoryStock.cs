using System;
using System.Collections.Generic;

namespace ChickenShop
{
    /// <summary>
    /// 재고 수량 저장소 (순수 C#). 모든 차감은 TryConsume 을 통해서만 일어나며,
    /// 여러 품목을 동시에 쓰는 작업은 "전부 확인 후 전부 차감" 으로 원자적으로 처리한다.
    /// </summary>
    public class InventoryStock
    {
        private readonly Dictionary<IngredientType, int> counts = new Dictionary<IngredientType, int>();

        /// <summary>(품목, 변경 후 수량)</summary>
        public event Action<IngredientType, int> Changed;

        public static readonly IngredientType[] AllTypes = (IngredientType[])Enum.GetValues(typeof(IngredientType));

        public int Get(IngredientType type)
        {
            int v;
            return counts.TryGetValue(type, out v) ? v : 0;
        }

        public bool Has(IngredientType type, int amount)
        {
            return amount <= 0 || Get(type) >= amount;
        }

        public void Set(IngredientType type, int amount)
        {
            if (amount < 0) amount = 0;
            counts[type] = amount;
            Changed?.Invoke(type, amount);
        }

        public void Add(IngredientType type, int amount)
        {
            if (amount <= 0) return;
            Set(type, Get(type) + amount);
        }

        /// <summary>단일 품목 차감. 부족하면 아무것도 바꾸지 않고 false.</summary>
        public bool TryConsume(IngredientType type, int amount)
        {
            if (amount <= 0) return true;
            if (!Has(type, amount)) return false;
            Set(type, Get(type) - amount);
            return true;
        }

        /// <summary>
        /// 여러 품목 동시 차감 (원자적). 하나라도 부족하면 아무것도 차감하지 않고 missing 에 첫 부족 품목을 돌려준다.
        /// </summary>
        public bool TryConsumeAll(IList<KeyValuePair<IngredientType, int>> items, out IngredientType missing)
        {
            missing = default(IngredientType);
            // 같은 품목이 여러 번 나와도 합산해서 확인
            var need = new Dictionary<IngredientType, int>();
            for (int i = 0; i < items.Count; i++)
            {
                int cur;
                need.TryGetValue(items[i].Key, out cur);
                need[items[i].Key] = cur + items[i].Value;
            }
            foreach (var kv in need)
            {
                if (!Has(kv.Key, kv.Value))
                {
                    missing = kv.Key;
                    return false;
                }
            }
            foreach (var kv in need)
            {
                if (kv.Value > 0) Set(kv.Key, Get(kv.Key) - kv.Value);
            }
            return true;
        }

        /// <summary>저장용: 열거형 순서대로 수량 배열.</summary>
        public int[] ToArray()
        {
            var arr = new int[AllTypes.Length];
            for (int i = 0; i < AllTypes.Length; i++) arr[i] = Get(AllTypes[i]);
            return arr;
        }

        public void FromArray(int[] arr)
        {
            for (int i = 0; i < AllTypes.Length; i++)
            {
                Set(AllTypes[i], arr != null && i < arr.Length ? arr[i] : 0);
            }
        }
    }
}
