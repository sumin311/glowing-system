using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ChickenShop
{
    /// <summary>일일 정산 화면: 매출/지출/순이익, 주문 결과, 평판 변화. 여기서 구매·업그레이드 후 다음 날로 넘어간다.</summary>
    public class SettlementPanel : UIPanel
    {
        private Text reportText;
        private Text sideText;
        private DailyReport report;

        public override bool ClosableByEscape { get { return false; } }

        protected override void Build()
        {
            var body = CreateWindow("영업 정산", new Vector2(1200f, 820f));
            var v = UIFactory.Vertical(body.gameObject, 14f, 0);
            v.childForceExpandHeight = false;

            var cols = UIFactory.Rect(body, "Cols");
            UIFactory.Horizontal(cols.gameObject, 20f, 0, TextAnchor.UpperLeft).childForceExpandHeight = true;
            UIFactory.Layout(cols, prefH: 560, flexW: 1);
            var left = UIFactory.Panel(cols, "Left", UIFactory.PanelLight);
            UIFactory.Layout(left, flexW: 1.2f);
            reportText = UIFactory.Label(left, "", 27, TextAnchor.UpperLeft, UIFactory.TextMain);
            UIFactory.Stretch((RectTransform)reportText.transform, 22f);
            var right = UIFactory.Panel(cols, "Right", UIFactory.PanelLight);
            UIFactory.Layout(right, flexW: 1f);
            sideText = UIFactory.Label(right, "", 24, TextAnchor.UpperLeft, UIFactory.TextMain);
            UIFactory.Stretch((RectTransform)sideText.transform, 22f);

            var buttons = UIFactory.Rect(body, "Buttons");
            UIFactory.Horizontal(buttons.gameObject, 16f, 0, TextAnchor.MiddleCenter).childForceExpandWidth = true;
            UIFactory.Layout(buttons, prefH: 70);
            var shop = UIFactory.Button(buttons, "재료 구매 · 업그레이드", () => ui.OpenShop(ShopTab.Supplies), UIFactory.PanelLight, 28);
            UIFactory.Layout(shop, flexW: 1, prefH: 64);
            var next = UIFactory.Button(buttons, "다음 날 시작 ▶", () => ui.CloseSettlementAndStartNextDay(), UIFactory.Accent, 28);
            UIFactory.Layout(next, flexW: 1, prefH: 64);
            var menu = UIFactory.Button(buttons, "저장하고 메인 메뉴로", () =>
            {
                var gm = GameManager.Instance;
                string err;
                if (gm != null && gm.Save != null && !gm.Save.TrySave(out err)) GameManager.Toast("저장 실패: " + err, ToastKind.Error);
                if (gm != null) gm.ReturnToMainMenu();
            }, UIFactory.PanelLight, 28);
            UIFactory.Layout(menu, flexW: 1, prefH: 64);
        }

        public void Show(DailyReport r)
        {
            report = r;
        }

        public override void Refresh()
        {
            var gm = GameManager.Instance;
            if (report == null || gm == null) return;
            var sb = new StringBuilder();
            sb.Append(UIFactory.Bold(report.day + "일차 영업 결과")).Append("\n\n");
            sb.Append("메뉴 매출            ").Append(UIFactory.Colorize(MoneyFormat.Won(report.revenue), UIFactory.Good)).Append('\n');
            sb.Append("팁                       ").Append(UIFactory.Colorize(MoneyFormat.Won(report.tips), UIFactory.Good)).Append('\n');
            sb.Append("재료 구매비          ").Append(UIFactory.Colorize("-" + MoneyFormat.Won(report.ingredientCost), UIFactory.Bad)).Append('\n');
            sb.Append("운영비(임대·관리·인건) ").Append(UIFactory.Colorize("-" + MoneyFormat.Won(report.operatingCost), UIFactory.Bad)).Append('\n');
            sb.Append("─────────────────\n");
            long net = report.NetProfit;
            sb.Append(UIFactory.Bold("순이익                 ")).Append(UIFactory.Bold(UIFactory.Colorize(MoneyFormat.SignedWon(net), net >= 0 ? UIFactory.Good : UIFactory.Bad))).Append("\n\n");
            if (report.upgradeCost > 0) sb.Append("설비 투자(업그레이드)  ").Append(UIFactory.Colorize("-" + MoneyFormat.Won(report.upgradeCost), UIFactory.Warn)).Append('\n');
            sb.Append("현재 보유 자금        ").Append(UIFactory.Bold(MoneyFormat.Won(gm.Economy.Money))).Append('\n');
            if (gm.Economy.Money < 0) sb.Append(UIFactory.Colorize("자금이 마이너스입니다! 재료를 아껴 쓰고 매출을 올리세요.", UIFactory.Bad)).Append('\n');
            reportText.text = sb.ToString();

            sb.Length = 0;
            sb.Append(UIFactory.Bold("주문")).Append('\n');
            sb.Append("완료 ").Append(report.ordersCompleted).Append("건   실패 ").Append(report.ordersFailed).Append("건\n");
            sb.Append("놓친 손님 ").Append(report.customersLost).Append("명   폐기 ").Append(report.chickensWasted).Append("개\n\n");
            float dRep = report.reputationEnd - report.reputationStart;
            sb.Append(UIFactory.Bold("평판")).Append('\n');
            sb.Append(report.reputationStart.ToString("0.0")).Append(" → ").Append(report.reputationEnd.ToString("0.0")).Append("  (")
              .Append(UIFactory.Colorize((dRep >= 0 ? "+" : string.Empty) + dRep.ToString("0.0"), dRep >= 0 ? UIFactory.Good : UIFactory.Bad)).Append(")\n\n");
            sb.Append(UIFactory.Bold("재고")).Append('\n');
            foreach (var d in gm.Inventory.Definitions)
            {
                int n = gm.Inventory.Get(d.type);
                sb.Append(n <= d.lowStockWarning ? UIFactory.Colorize(d.displayName + " " + n, UIFactory.Warn) : d.displayName + " " + n).Append("   ");
            }
            sb.Append("\n\n").Append(UIFactory.Colorize("자동 저장되었습니다.", UIFactory.TextDim));
            sideText.text = sb.ToString();
        }
    }
}
