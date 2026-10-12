using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ChickenShop
{
    /// <summary>Tab 화면: 주문 목록(상태/남은 시간/가격), 재고 현황, 오늘 실적. 영업 시간은 계속 흐른다.</summary>
    public class OrdersInventoryPanel : UIPanel
    {
        private Text ordersText;
        private Text inventoryText;
        private Text statsText;
        private readonly StringBuilder sb = new StringBuilder(1024);

        protected override void Build()
        {
            var body = CreateWindow("주문 · 재고 관리  (Tab / ESC 닫기)", new Vector2(1500f, 820f));
            var cols = UIFactory.Rect(body, "Columns");
            UIFactory.Stretch(cols);
            var h = UIFactory.Horizontal(cols.gameObject, 20f, 0, TextAnchor.UpperLeft);
            h.childForceExpandHeight = true;
            h.childForceExpandWidth = true;

            ordersText = Column(cols, "주문 목록", 1.3f);
            inventoryText = Column(cols, "재고 현황", 1f);
            statsText = Column(cols, "오늘 실적", 0.9f);
        }

        private Text Column(Transform parent, string title, float flex)
        {
            var col = UIFactory.Panel(parent, title, UIFactory.PanelLight);
            UIFactory.Layout(col, flexW: flex);
            var v = UIFactory.Vertical(col.gameObject, 8f, 16);
            v.childForceExpandHeight = false;
            var t = UIFactory.Label(col, UIFactory.Bold(title), 28, TextAnchor.UpperLeft, UIFactory.Warn, true);
            UIFactory.Layout(t, prefH: 40);
            var body = UIFactory.Label(col, "", 23, TextAnchor.UpperLeft, UIFactory.TextMain);
            UIFactory.Layout(body, flexW: 1, prefH: 600);
            return body;
        }

        public override void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            // 주문
            sb.Length = 0;
            int count = 0;
            if (gm.Orders != null)
            {
                var list = gm.Orders.Orders;
                for (int i = 0; i < list.Count; i++)
                {
                    var o = list[i];
                    count++;
                    Color sc = o.status == OrderStatus.Completed ? UIFactory.Good : o.status == OrderStatus.Failed ? UIFactory.Bad : UIFactory.Warn;
                    var c = gm.Orders.GetCustomer(o.id);
                    sb.Append("<b>#").Append(o.id).Append(' ').Append(OrderData.MenuName(o.menu)).Append(o.withDrink ? " + 콜라" : string.Empty).Append("</b>  ")
                      .Append(MoneyFormat.Won(o.TotalPrice)).Append('\n')
                      .Append("   ").Append(UIFactory.Colorize(OrderData.StatusName(o.status), sc));
                    if (o.IsActive) sb.Append("   남은 시간 ").Append(Mathf.CeilToInt(o.remaining)).Append("초");
                    if (o.status == OrderStatus.Completed) sb.Append("   ").Append(new string('★', Mathf.Max(1, o.satisfactionStars)));
                    if (c != null && o.IsActive) sb.Append("   (").Append(c.DisplayName).Append(')');
                    sb.Append("\n\n");
                }
            }
            if (count == 0) sb.Append(UIFactory.Colorize("아직 주문이 없습니다.\n카운터 앞 손님에게 [E] 로 주문을 받으세요.", UIFactory.TextDim));
            sb.Append("\n").Append(UIFactory.Colorize("조리 순서: 냉장고(생닭) → 반죽 → 튀김기 → (양념/간장 소스) → 포장 → 픽업대/손님", UIFactory.TextDim));
            ordersText.text = sb.ToString();

            // 재고
            sb.Length = 0;
            if (gm.Inventory != null)
            {
                foreach (var d in gm.Inventory.Definitions)
                {
                    int n = gm.Inventory.Get(d.type);
                    Color c = n == 0 ? UIFactory.Bad : n <= d.lowStockWarning ? UIFactory.Warn : UIFactory.TextMain;
                    sb.Append(UIFactory.Colorize(d.displayName, c)).Append("  ").Append(UIFactory.Bold(n.ToString())).Append("개")
                      .Append(UIFactory.Colorize("   (" + MoneyFormat.Won(d.unitPrice) + "/개)", UIFactory.TextDim)).Append('\n');
                }
                sb.Append('\n').Append(UIFactory.Colorize("재료 구매: 주방 왼쪽 벽의 발주 단말기 [E]", UIFactory.TextDim));
            }
            inventoryText.text = sb.ToString();

            // 실적
            sb.Length = 0;
            if (gm.Economy != null && gm.Day != null)
            {
                var r = gm.Economy.Today;
                sb.Append(gm.Day.Day).Append("일차 ").Append(gm.Day.PhaseText).Append("  ").Append(gm.Day.ClockText).Append("\n\n")
                  .Append("보유 자금  ").Append(UIFactory.Bold(MoneyFormat.Won(gm.Economy.Money))).Append("\n")
                  .Append("매출  ").Append(MoneyFormat.Won(r.revenue)).Append("\n")
                  .Append("팁  ").Append(MoneyFormat.Won(r.tips)).Append("\n")
                  .Append("재료 구매비  ").Append(MoneyFormat.Won(r.ingredientCost)).Append("\n")
                  .Append("완료 주문  ").Append(r.ordersCompleted).Append("건\n")
                  .Append("실패 주문  ").Append(r.ordersFailed).Append("건\n")
                  .Append("놓친 손님  ").Append(r.customersLost).Append("명\n")
                  .Append("폐기  ").Append(r.chickensWasted).Append("개\n\n");
                if (gm.Reputation != null) sb.Append("평판  ").Append(gm.Reputation.Value.ToString("0.0")).Append(" / 100\n");
                if (gm.Economy != null && gm.Progression != null)
                    sb.Append(UIFactory.Colorize("마감 시 운영비 " + MoneyFormat.Won(gm.Economy.BaseOperatingCost + gm.Progression.DailyUpkeep) + " 청구", UIFactory.TextDim));
            }
            statsText.text = sb.ToString();
        }
    }
}
