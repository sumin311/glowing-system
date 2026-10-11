using UnityEngine;
using UnityEngine.UI;

namespace ChickenShop
{
    /// <summary>
    /// 발주 단말기 화면: 재료 구매 / 장비 업그레이드 / 메뉴 해금 탭.
    /// 모든 구매는 InventoryManager.Purchase / ShopProgression.TryPurchase 를 통해 자금과 함께 처리된다.
    /// </summary>
    public class ShopPanel : UIPanel
    {
        private ShopTab tab = ShopTab.Supplies;
        private RectTransform content;
        private Text moneyText;
        private Text statusText;
        private Button[] tabButtons;

        protected override void Build()
        {
            var body = CreateWindow("발주 단말기  (ESC 닫기)", new Vector2(1280f, 860f));
            var v = UIFactory.Vertical(body.gameObject, 12f, 0);
            v.childForceExpandHeight = false;

            var tabsRow = UIFactory.Rect(body, "Tabs");
            UIFactory.Horizontal(tabsRow.gameObject, 10f, 0).childForceExpandWidth = true;
            UIFactory.Layout(tabsRow, prefH: 56);
            tabButtons = new Button[3];
            tabButtons[0] = UIFactory.Button(tabsRow, "재료 구매", () => SetTab(ShopTab.Supplies));
            tabButtons[1] = UIFactory.Button(tabsRow, "장비 업그레이드", () => SetTab(ShopTab.Upgrades));
            tabButtons[2] = UIFactory.Button(tabsRow, "메뉴 해금", () => SetTab(ShopTab.Menus));
            for (int i = 0; i < 3; i++) UIFactory.Layout(tabButtons[i], flexW: 1, prefH: 52);

            moneyText = UIFactory.Label(body, "", 28, TextAnchor.MiddleLeft, new Color(1f, 0.88f, 0.45f), true);
            UIFactory.Layout(moneyText, prefH: 40);

            content = UIFactory.Rect(body, "Content");
            var cv = UIFactory.Vertical(content.gameObject, 8f, 0);
            cv.childForceExpandHeight = false;
            UIFactory.Layout(content, flexW: 1, prefH: 560);

            statusText = UIFactory.Label(body, "", 24, TextAnchor.MiddleLeft, UIFactory.Warn, true);
            UIFactory.Layout(statusText, prefH: 36);
        }

        public void SetTab(ShopTab newTab)
        {
            tab = newTab;
            if (IsOpen) Rebuild();
        }

        public override void OnOpened()
        {
            statusText.text = string.Empty;
            Rebuild();
        }

        public override void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Economy != null) moneyText.text = "보유 자금  " + MoneyFormat.Won(gm.Economy.Money);
        }

        private void Rebuild()
        {
            if (content == null) return;
            UIFactory.ClearChildren(content);
            for (int i = 0; i < tabButtons.Length; i++)
                tabButtons[i].GetComponent<Image>().color = (int)tab == i ? UIFactory.Accent : UIFactory.PanelLight;
            switch (tab)
            {
                case ShopTab.Supplies: BuildSupplies(); break;
                case ShopTab.Upgrades: BuildUpgrades(); break;
                default: BuildMenus(); break;
            }
            Refresh();
        }

        private RectTransform Row(float height = 64f)
        {
            var row = UIFactory.Panel(content, "Row", UIFactory.PanelLight);
            UIFactory.Horizontal(row.gameObject, 12f, 8).childForceExpandHeight = true;
            UIFactory.Layout(row, prefH: height, minH: height);
            return row;
        }

        private void Feedback(bool ok, string message)
        {
            statusText.color = ok ? UIFactory.Good : UIFactory.Bad;
            statusText.text = message;
            if (ok) GameManager.Toast(message, ToastKind.Success);
            Rebuild();
        }

        // ------------------------------------------------------------------ 재료
        private void BuildSupplies()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Inventory == null) return;
            foreach (var def in gm.Inventory.Definitions)
            {
                var d = def;
                var row = Row();
                int n = gm.Inventory.Get(d.type);
                var name = UIFactory.Label(row, UIFactory.Bold(d.displayName) + "   보유 " + n + "개", 26, TextAnchor.MiddleLeft, n <= d.lowStockWarning ? UIFactory.Warn : UIFactory.TextMain);
                UIFactory.Layout(name, flexW: 1);
                var price = UIFactory.Label(row, MoneyFormat.Won(d.unitPrice) + "/개", 24, TextAnchor.MiddleRight, UIFactory.TextDim);
                UIFactory.Layout(price, prefW: 150);
                AddBuyButton(row, d.type, 1);
                AddBuyButton(row, d.type, Mathf.Max(2, d.purchaseBundle));
                AddBuyButton(row, d.type, Mathf.Max(3, d.purchaseBundle * 2));
            }
            var tip = UIFactory.Label(content, UIFactory.Colorize("후라이드 1마리 원가 = 생닭 + 튀김가루 + 식용유 + 상자 + 봉투. 양념/간장은 소스 1개 추가.", UIFactory.TextDim), 22, TextAnchor.MiddleLeft, UIFactory.TextDim);
            UIFactory.Layout(tip, prefH: 34);
        }

        private void AddBuyButton(Transform row, IngredientType type, int qty)
        {
            var gm = GameManager.Instance;
            long cost = gm.Inventory.PriceOf(type, qty);
            var b = UIFactory.Button(row, "+" + qty + "  (" + MoneyFormat.Won(cost) + ")", () =>
            {
                string msg;
                bool ok = GameManager.Instance.Inventory.Purchase(type, qty, out msg);
                Feedback(ok, msg);
            }, null, 22);
            UIFactory.Layout(b, prefW: 210);
            b.interactable = gm.Economy.CanAfford(cost);
        }

        // ------------------------------------------------------------------ 업그레이드
        private void BuildUpgrades()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Progression == null) return;
            foreach (var def in gm.Progression.Upgrades)
            {
                var d = def;
                var row = Row(84f);
                int lv = gm.Progression.GetLevel(d.id);
                bool max = lv >= d.maxLevel;
                string reason;
                bool can = gm.Progression.CanPurchase(d.id, out reason);
                var label = UIFactory.Label(row, UIFactory.Bold(d.displayName) + "  Lv." + lv + "/" + d.maxLevel + "\n" +
                    UIFactory.Colorize(d.description + (d.upkeepPerLevel > 0 ? "  (유지비 " + MoneyFormat.Won(d.upkeepPerLevel) + "/일)" : string.Empty), UIFactory.TextDim),
                    23, TextAnchor.MiddleLeft, UIFactory.TextMain);
                UIFactory.Layout(label, flexW: 1);
                var req = UIFactory.Label(row, max ? "최대 레벨" : (can ? string.Empty : reason), 22, TextAnchor.MiddleRight, max ? UIFactory.Good : UIFactory.Warn);
                UIFactory.Layout(req, prefW: 220);
                var b = UIFactory.Button(row, max ? "완료" : "구매 " + MoneyFormat.Won(gm.Progression.GetNextCost(d.id)), () =>
                {
                    string msg;
                    bool ok = GameManager.Instance.Progression.TryPurchase(d.id, out msg);
                    Feedback(ok, msg);
                }, null, 22);
                UIFactory.Layout(b, prefW: 240);
                b.interactable = can;
            }
        }

        // ------------------------------------------------------------------ 메뉴
        private void BuildMenus()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Menu == null || gm.Progression == null) return;
            foreach (var m in gm.Menu.Menus)
            {
                var menu = m;
                var row = Row(84f);
                bool unlocked = gm.Progression.IsMenuUnlocked(menu.type);
                var unlock = gm.Progression.GetMenuUnlock(menu.type);
                string desc = unlocked ? "판매 중" : unlock != null ? "조건: " + unlock.requiredDay + "일차 이상, 평판 " + unlock.requiredReputation.ToString("0") + " 이상" : "해금 정보 없음";
                var label = UIFactory.Label(row, UIFactory.Bold(menu.displayName) + "  " + MoneyFormat.Won(menu.price) + "\n" + UIFactory.Colorize(desc + "   조리 시간 x" + menu.cookTimeMultiplier.ToString("0.00"), UIFactory.TextDim), 23, TextAnchor.MiddleLeft, UIFactory.TextMain);
                UIFactory.Layout(label, flexW: 1);
                if (unlocked)
                {
                    var ok = UIFactory.Label(row, "판매 중", 24, TextAnchor.MiddleRight, UIFactory.Good, true);
                    UIFactory.Layout(ok, prefW: 240);
                    continue;
                }
                string reason;
                bool can = gm.Progression.CanUnlockMenu(menu.type, out reason);
                var r = UIFactory.Label(row, can ? string.Empty : reason, 22, TextAnchor.MiddleRight, UIFactory.Warn);
                UIFactory.Layout(r, prefW: 200);
                var b = UIFactory.Button(row, "해금 " + MoneyFormat.Won(unlock != null ? unlock.cost : 0), () =>
                {
                    string msg;
                    bool ok = GameManager.Instance.Progression.TryUnlockMenu(menu.type, out msg);
                    if (ok && menu.type == MenuType.Ganjang) msg += " (간장 소스를 구매하세요)";
                    Feedback(ok, msg);
                }, null, 22);
                UIFactory.Layout(b, prefW: 240);
                b.interactable = can;
            }
        }
    }
}
