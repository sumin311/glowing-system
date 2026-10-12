using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChickenShop
{
    /// <summary>메뉴 정의 (가격, 조리 조건).</summary>
    [Serializable]
    public class MenuDefinition
    {
        public MenuType type;
        public string displayName;
        [Tooltip("판매 가격(원)")] public int price;
        [Tooltip("기본 튀김 시간에 곱해지는 배율 (메뉴별 조리 조건)")] public float cookTimeMultiplier = 1f;
        [Tooltip("주문 확률 가중치")] public float orderWeight = 1f;
        [Tooltip("처음부터 판매 가능한가 (false 면 ShopProgression 에서 해금)")] public bool unlockedByDefault = true;
        [Tooltip("필요한 소스")] public SauceType sauce;
        public Color uiColor = Color.white;
    }

    /// <summary>판매 메뉴 목록과 음료 설정.</summary>
    [DefaultExecutionOrder(-90)]
    public class MenuCatalog : MonoBehaviour
    {
        [SerializeField] private List<MenuDefinition> menus = DefaultMenus();
        [Header("음료")]
        [SerializeField] private int drinkPrice = 2000;
        [SerializeField, Range(0f, 1f)] private float drinkChance = 0.35f;

        private readonly Dictionary<MenuType, MenuDefinition> lookup = new Dictionary<MenuType, MenuDefinition>();

        public IReadOnlyList<MenuDefinition> Menus { get { return menus; } }
        public int DrinkPrice { get { return drinkPrice; } }
        public float DrinkChance { get { return drinkChance; } }

        private void Reset()
        {
            menus = DefaultMenus();
        }

        private void Awake()
        {
            if (menus == null || menus.Count == 0) menus = DefaultMenus();
            lookup.Clear();
            foreach (var m in menus)
            {
                m.sauce = OrderData.RequiredSauce(m.type); // 소스는 메뉴 종류로 고정
                lookup[m.type] = m;
            }
        }

        public static List<MenuDefinition> DefaultMenus()
        {
            return new List<MenuDefinition>
            {
                new MenuDefinition { type = MenuType.Fried, displayName = "후라이드치킨", price = 18000, cookTimeMultiplier = 1f, orderWeight = 1.2f, unlockedByDefault = true, sauce = SauceType.None, uiColor = new Color(0.95f, 0.7f, 0.25f) },
                new MenuDefinition { type = MenuType.Yangnyeom, displayName = "양념치킨", price = 19500, cookTimeMultiplier = 1f, orderWeight = 1f, unlockedByDefault = true, sauce = SauceType.Yangnyeom, uiColor = new Color(0.9f, 0.25f, 0.15f) },
                new MenuDefinition { type = MenuType.Ganjang, displayName = "간장치킨", price = 21000, cookTimeMultiplier = 1.15f, orderWeight = 1f, unlockedByDefault = false, sauce = SauceType.Ganjang, uiColor = new Color(0.55f, 0.3f, 0.12f) },
            };
        }

        public MenuDefinition Get(MenuType type)
        {
            MenuDefinition m;
            if (lookup.TryGetValue(type, out m)) return m;
            GameLog.Warn("Menu", "메뉴 정의 없음: " + type, this);
            return null;
        }

        public int Price(MenuType type)
        {
            var m = Get(type);
            return m != null ? m.price : 0;
        }

        public string Name(MenuType type)
        {
            var m = Get(type);
            return m != null && !string.IsNullOrEmpty(m.displayName) ? m.displayName : OrderData.MenuName(type);
        }
    }
}
