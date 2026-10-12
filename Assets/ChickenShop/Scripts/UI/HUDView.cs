using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ChickenShop
{
    /// <summary>
    /// 게임 화면 HUD: 날짜/시각/영업 상태, 보유 자금, 평판, 주문 티켓(상태 + 남은 시간), 손에 든 것,
    /// 상호작용 안내, 다음 할 일 힌트, 재고 부족 경고. 값이 바뀔 때만 문자열을 다시 만든다.
    /// </summary>
    public class HUDView : MonoBehaviour
    {
        private class Ticket
        {
            public RectTransform rect;
            public Text text;
            public RectTransform fill;
            public Image fillImg;
        }

        private UIManager ui;
        private Text infoText;
        private Text moneyText;
        private Text moneyDelta;
        private float moneyDeltaTimer;
        private Text repText;
        private RectTransform dayFill;
        private Text promptText;
        private Image crosshair;
        private Text heldText;
        private Text hintText;
        private Text stockWarnText;
        private RectTransform ticketContainer;
        private readonly List<Ticket> tickets = new List<Ticket>();
        private float refreshTimer;
        private FryerStation[] fryers;
        private readonly StringBuilder sb = new StringBuilder(256);
        private string lastInfo, lastMoney, lastRep, lastHeld, lastHint, lastStock;

        public void Build(UIManager manager)
        {
            ui = manager;
            var root = (RectTransform)transform;

            // ---- 좌상단 정보
            var info = UIFactory.Panel(root, "InfoBox", new Color(0.1f, 0.07f, 0.05f, 0.78f));
            UIFactory.Anchor(info, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(430f, 168f));
            infoText = UIFactory.Label(info, "", 28, TextAnchor.UpperLeft, UIFactory.TextMain, true);
            UIFactory.Anchor((RectTransform)infoText.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(18f, -12f), new Vector2(-36f, 40f));
            var barBg = UIFactory.Panel(info, "DayBar", new Color(0f, 0f, 0f, 0.5f));
            UIFactory.Anchor(barBg, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(18f, -56f), new Vector2(-36f, 8f));
            dayFill = UIFactory.Panel(barBg, "Fill", UIFactory.Accent);
            dayFill.anchorMin = Vector2.zero; dayFill.anchorMax = new Vector2(0f, 1f); dayFill.offsetMin = dayFill.offsetMax = Vector2.zero;
            moneyText = UIFactory.Label(info, "", 30, TextAnchor.UpperLeft, new Color(1f, 0.88f, 0.45f), true);
            UIFactory.Anchor((RectTransform)moneyText.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(18f, -72f), new Vector2(-36f, 40f));
            moneyDelta = UIFactory.Label(info, "", 26, TextAnchor.UpperRight, UIFactory.Good, true);
            UIFactory.Anchor((RectTransform)moneyDelta.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(18f, -74f), new Vector2(-36f, 40f));
            repText = UIFactory.Label(info, "", 26, TextAnchor.UpperLeft, UIFactory.TextMain, false);
            UIFactory.Anchor((RectTransform)repText.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(18f, -116f), new Vector2(-36f, 40f));

            // ---- 재고 부족 경고 (좌측)
            stockWarnText = UIFactory.Label(root, "", 24, TextAnchor.UpperLeft, UIFactory.Warn, true);
            UIFactory.Anchor((RectTransform)stockWarnText.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -200f), new Vector2(420f, 200f));

            // ---- 우상단 주문 티켓
            ticketContainer = UIFactory.Rect(root, "Tickets");
            UIFactory.Anchor(ticketContainer, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(360f, 700f));
            var v = UIFactory.Vertical(ticketContainer.gameObject, 8f, 0, TextAnchor.UpperRight);
            v.childForceExpandWidth = true;
            var title = UIFactory.Label(ticketContainer, "주문 목록  [Tab] 상세", 22, TextAnchor.MiddleRight, UIFactory.TextDim, true);
            UIFactory.Layout(title, prefH: 28);

            // ---- 상단 중앙 할 일 힌트
            var hintBg = UIFactory.Panel(root, "Hint", new Color(0.1f, 0.07f, 0.05f, 0.6f));
            UIFactory.Anchor(hintBg, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(900f, 52f));
            hintText = UIFactory.Label(hintBg, "", 25, TextAnchor.MiddleCenter, new Color(1f, 0.93f, 0.7f), true);
            UIFactory.Stretch((RectTransform)hintText.transform, 6f);

            // ---- 조준점
            var ch = UIFactory.Panel(root, "Crosshair", Color.white);
            UIFactory.Anchor(ch, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f));
            crosshair = ch.GetComponent<Image>();
            crosshair.raycastTarget = false;

            // ---- 상호작용 안내 (화면 중앙 아래)
            var pBg = UIFactory.Rect(root, "Prompt");
            UIFactory.Anchor(pBg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1200f, 60f));
            promptText = UIFactory.Label(pBg, "", 30, TextAnchor.UpperCenter, Color.white, true);
            UIFactory.Stretch((RectTransform)promptText.transform);
            var shadow = promptText.gameObject.AddComponent<Outline>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2f, -2f);

            // ---- 손에 든 것 (좌하단)
            var held = UIFactory.Panel(root, "Held", new Color(0.1f, 0.07f, 0.05f, 0.7f));
            UIFactory.Anchor(held, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(560f, 54f));
            heldText = UIFactory.Label(held, "", 25, TextAnchor.MiddleLeft, UIFactory.TextMain, true);
            UIFactory.Stretch((RectTransform)heldText.transform, 14f);

            // ---- 조작 요약 (우하단)
            var keys = UIFactory.Label(root, "WASD 이동  Shift 달리기  Space 점프  E 상호작용  Tab 주문/재고  Alt 커서  ESC 메뉴  F1 도움말", 20, TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.6f));
            UIFactory.Anchor((RectTransform)keys.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 16f), new Vector2(1100f, 30f));
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Economy != null) gm.Economy.MoneyChanged += OnMoneyChanged;
            if (ui != null && ui.Interactor != null) ui.Interactor.PromptChanged += OnPromptChanged;
            fryers = FindObjectsOfType<FryerStation>();
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Economy != null) gm.Economy.MoneyChanged -= OnMoneyChanged;
            if (ui != null && ui.Interactor != null) ui.Interactor.PromptChanged -= OnPromptChanged;
        }

        private void OnMoneyChanged(long money, long delta)
        {
            if (delta == 0) return;
            moneyDelta.text = MoneyFormat.SignedWon(delta);
            moneyDelta.color = delta > 0 ? UIFactory.Good : UIFactory.Bad;
            moneyDeltaTimer = 2f;
            refreshTimer = 0f;
        }

        private void OnPromptChanged(Interactable target, string prompt)
        {
            promptText.text = prompt;
            crosshair.color = target != null ? UIFactory.Accent : new Color(1f, 1f, 1f, 0.8f);
            crosshair.rectTransform.sizeDelta = target != null ? new Vector2(12f, 12f) : new Vector2(8f, 8f);
        }

        private void Update()
        {
            if (moneyDeltaTimer > 0f)
            {
                moneyDeltaTimer -= Time.unscaledDeltaTime;
                var c = moneyDelta.color; c.a = Mathf.Clamp01(moneyDeltaTimer); moneyDelta.color = c;
            }
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.2f;
            Refresh();
        }

        private void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Day == null || gm.Economy == null) return;
            bool hide = ui.AnyPanelOpen;
            promptText.enabled = !hide;
            crosshair.enabled = !hide;

            SetIfChanged(infoText, ref lastInfo, gm.Day.Day + "일차  " + gm.Day.ClockText + "  " + PhaseColored(gm.Day));
            dayFill.anchorMax = new Vector2(gm.Day.Phase == ShopPhase.PreOpen ? 0f : Mathf.Clamp01(gm.Day.BusinessProgress), 1f);
            SetIfChanged(moneyText, ref lastMoney, "자금 " + MoneyFormat.Won(gm.Economy.Money));
            if (gm.Reputation != null)
                SetIfChanged(repText, ref lastRep, "평판 " + ReputationManager.StarText(gm.Reputation.Stars) + "  " + gm.Reputation.Value.ToString("0"));

            var carry = gm.Carry;
            SetIfChanged(heldText, ref lastHeld, "손: " + (carry == null || carry.IsEmpty ? "(비어 있음)" : carry.Held.Describe()));
            SetIfChanged(hintText, ref lastHint, ComputeHint(gm));
            SetIfChanged(stockWarnText, ref lastStock, StockWarnings(gm));
            RefreshTickets(gm);
        }

        private static void SetIfChanged(Text t, ref string last, string value)
        {
            if (value == last) return;
            last = value;
            t.text = value;
        }

        private static string PhaseColored(DayCycleManager day)
        {
            switch (day.Phase)
            {
                case ShopPhase.Open: return UIFactory.Colorize(day.PhaseText, UIFactory.Good);
                case ShopPhase.Closing: return UIFactory.Colorize(day.PhaseText, UIFactory.Warn);
                default: return UIFactory.Colorize(day.PhaseText, UIFactory.TextDim);
            }
        }

        private string StockWarnings(GameManager gm)
        {
            if (gm.Inventory == null) return string.Empty;
            sb.Length = 0;
            foreach (var d in gm.Inventory.Definitions)
            {
                if (d.type == IngredientType.GanjangSauce && gm.Progression != null && !gm.Progression.IsMenuUnlocked(MenuType.Ganjang)) continue;
                int n = gm.Inventory.Get(d.type);
                if (n <= d.lowStockWarning)
                {
                    sb.Append(n == 0 ? "! " : "- ").Append(d.displayName).Append(n == 0 ? " 없음!" : " 부족 (" + n + ")").Append('\n');
                }
            }
            if (sb.Length > 0) sb.Insert(0, "재고 경고 - 발주 단말기에서 구매\n");
            return sb.ToString();
        }

        /// <summary>현재 상황에서 다음에 할 일을 알려준다 (튜토리얼 역할).</summary>
        private string ComputeHint(GameManager gm)
        {
            var day = gm.Day;
            if (day.Phase == ShopPhase.Settlement) return "오늘 영업 정산을 확인하세요";
            var carry = gm.Carry;
            if (carry != null && !carry.IsEmpty)
            {
                var item = carry.Held;
                if (item.IsPacked) return "포장 완료! 픽업대(카운터 오른쪽) 또는 주문한 손님에게 [E]로 전달하세요";
                switch (item.Stage)
                {
                    case ChickenStage.Raw: return "생닭 → 반죽 작업대에서 튀김옷을 입히세요";
                    case ChickenStage.Battered: return "반죽 완료 → 튀김기에 넣으세요";
                    case ChickenStage.Burnt: return "탄 치킨은 쓰레기통에 버리세요";
                    case ChickenStage.Fried:
                        var order = gm.Orders != null ? gm.Orders.GetActive(item.OrderId) : null;
                        if (item.Sauce == SauceType.None && order != null && OrderData.RequiredSauce(order.menu) != SauceType.None)
                            return "주문 #" + order.id + "은 " + OrderData.MenuName(order.menu) + " → " + (order.menu == MenuType.Ganjang ? "간장" : "양념") + " 소스통으로";
                        return "완성! → 포장 작업대에서 포장하세요";
                }
            }
            if (fryers != null)
            {
                for (int i = 0; i < fryers.Length; i++)
                    if (fryers[i] != null && fryers[i].IsDone) return "튀김 완성! 타기 전에 " + fryers[i].DisplayName + "에서 꺼내세요";
            }
            if (day.Phase == ShopPhase.PreOpen) return "준비 중(시간 정지): 재고 확인 후 영업 팻말에서 [E]로 영업 시작";
            if (gm.Customers != null && gm.Customers.CustomerAtCounter != null) return "카운터 앞 손님에게 [E]로 주문을 받으세요";
            if (gm.Orders != null)
            {
                int waiting = 0;
                var list = gm.Orders.Orders;
                for (int i = 0; i < list.Count; i++) if (list[i].IsActive && list[i].assignedItemId <= 0) waiting++;
                if (waiting > 0) return "조리 대기 주문 " + waiting + "건 → 냉장고에서 생닭을 꺼내세요";
            }
            if (day.Phase == ShopPhase.Closing) return "마감 중: 남은 손님을 응대하면 정산이 시작됩니다";
            return "손님을 기다리는 중... 재고를 확인하거나 미리 준비하세요";
        }

        private void RefreshTickets(GameManager gm)
        {
            if (gm.Orders == null) return;
            var orders = gm.Orders.Orders;
            int shown = 0;
            for (int i = 0; i < orders.Count && shown < 7; i++)
            {
                var o = orders[i];
                if (!o.IsActive) continue;
                var t = GetTicket(shown++);
                sb.Length = 0;
                sb.Append("<b>#").Append(o.id).Append(' ').Append(OrderData.MenuName(o.menu)).Append("</b>");
                if (o.withDrink) sb.Append(" +콜라");
                sb.Append("\n").Append(UIFactory.Colorize(OrderData.StatusName(o.status), StatusColor(o.status)))
                  .Append("   ").Append(Mathf.CeilToInt(o.remaining)).Append("초");
                t.text.text = sb.ToString();
                float r = o.RemainingRatio;
                t.fill.anchorMax = new Vector2(r, 1f);
                t.fillImg.color = r > 0.55f ? UIFactory.Good : r > 0.25f ? UIFactory.Warn : UIFactory.Bad;
                t.rect.gameObject.SetActive(true);
            }
            for (int i = shown; i < tickets.Count; i++) tickets[i].rect.gameObject.SetActive(false);
        }

        private static Color StatusColor(OrderStatus s)
        {
            switch (s)
            {
                case OrderStatus.Packed: return UIFactory.Good;
                case OrderStatus.Cooking: return UIFactory.Warn;
                case OrderStatus.Cooked:
                case OrderStatus.Sauced: return new Color(0.6f, 0.85f, 1f);
                default: return UIFactory.TextDim;
            }
        }

        private Ticket GetTicket(int index)
        {
            while (tickets.Count <= index)
            {
                var rt = UIFactory.Panel(ticketContainer, "Ticket", new Color(0.98f, 0.95f, 0.88f, 0.92f));
                UIFactory.Layout(rt, prefH: 74, minH: 74);
                var text = UIFactory.Label(rt, "", 22, TextAnchor.UpperLeft, new Color(0.15f, 0.1f, 0.08f));
                UIFactory.Anchor((RectTransform)text.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                ((RectTransform)text.transform).offsetMin = new Vector2(12f, 10f);
                ((RectTransform)text.transform).offsetMax = new Vector2(-10f, -6f);
                var barBg = UIFactory.Panel(rt, "Bar", new Color(0f, 0f, 0f, 0.25f));
                UIFactory.Anchor(barBg, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 7f));
                var fill = UIFactory.Panel(barBg, "Fill", UIFactory.Good);
                fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.offsetMin = fill.offsetMax = Vector2.zero;
                tickets.Add(new Ticket { rect = rt, text = text, fill = fill, fillImg = fill.GetComponent<Image>() });
            }
            return tickets[index];
        }
    }
}
