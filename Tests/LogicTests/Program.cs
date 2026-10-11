using System;
using System.Collections.Generic;
using ChickenShop;

/// <summary>순수 로직 단위 테스트 (외부 테스트 프레임워크 없이 동작).</summary>
internal static class Program
{
    private static int passed, failed;

    private static void Check(bool cond, string name)
    {
        if (cond) { passed++; Console.WriteLine("  PASS  " + name); }
        else { failed++; Console.WriteLine("  FAIL  " + name); }
    }

    private static KeyValuePair<IngredientType, int> P(IngredientType t, int n) { return new KeyValuePair<IngredientType, int>(t, n); }

    private static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("[InventoryStock]");
        var inv = new InventoryStock();
        inv.Set(IngredientType.RawChicken, 2);
        inv.Set(IngredientType.ChickenBox, 1);
        Check(inv.TryConsume(IngredientType.RawChicken, 1) && inv.Get(IngredientType.RawChicken) == 1, "단일 차감");
        Check(!inv.TryConsume(IngredientType.RawChicken, 2) && inv.Get(IngredientType.RawChicken) == 1, "부족하면 차감하지 않음");
        IngredientType missing;
        Check(!inv.TryConsumeAll(new[] { P(IngredientType.ChickenBox, 1), P(IngredientType.PaperBag, 1) }, out missing)
              && missing == IngredientType.PaperBag && inv.Get(IngredientType.ChickenBox) == 1, "복수 차감은 원자적 (하나라도 없으면 전부 유지)");
        inv.Set(IngredientType.PaperBag, 3);
        Check(inv.TryConsumeAll(new[] { P(IngredientType.ChickenBox, 1), P(IngredientType.PaperBag, 1) }, out missing)
              && inv.Get(IngredientType.ChickenBox) == 0 && inv.Get(IngredientType.PaperBag) == 2, "복수 차감 성공");
        Check(!inv.TryConsumeAll(new[] { P(IngredientType.PaperBag, 2), P(IngredientType.PaperBag, 1) }, out missing) && inv.Get(IngredientType.PaperBag) == 2, "같은 품목 중복 요청은 합산해서 확인");
        var arr = inv.ToArray();
        var inv2 = new InventoryStock();
        inv2.FromArray(arr);
        Check(inv2.Get(IngredientType.PaperBag) == 2 && inv2.Get(IngredientType.RawChicken) == 1, "저장 배열 왕복");
        inv2.FromArray(new int[] { 5 });
        Check(inv2.Get(IngredientType.RawChicken) == 5 && inv2.Get(IngredientType.Cola) == 0, "짧은 저장 배열(구버전) 호환");

        Console.WriteLine("[Wallet]");
        var w = new Wallet(10000);
        w.BeginDay(1, 50f);
        Check(!w.TrySpend(20000, ExpenseCategory.Ingredients) && w.Money == 10000, "잔액 부족 지출 거부");
        Check(w.TrySpend(4000, ExpenseCategory.Ingredients) && w.Money == 6000 && w.Today.ingredientCost == 4000, "재료비 지출 기록");
        w.Earn(18000, 1800);
        Check(w.Money == 25800 && w.Today.revenue == 18000 && w.Today.tips == 1800, "매출/팁 기록");
        w.ForceSpend(45000, ExpenseCategory.Operating);
        Check(w.Money == -19200 && w.Today.operatingCost == 45000, "운영비는 음수 잔액 허용");
        Check(w.Today.NetProfit == 18000 + 1800 - 4000 - 45000, "순이익 계산");
        Check(!w.TrySpend(-5, ExpenseCategory.Upgrade), "음수 지출 거부");

        Console.WriteLine("[OrderBook]");
        var book = new OrderBook();
        var o1 = book.Create(MenuType.Yangnyeom, true, 19500, 2000, 100f, 7);
        var o2 = book.Create(MenuType.Fried, false, 18000, 2000, 100f, 8);
        Check(o1.id == 1 && o2.id == 2 && o1.TotalPrice == 21500 && o2.TotalPrice == 18000, "주문 생성/가격");
        var first = book.FindOldestUnassigned(o => o.status == OrderStatus.Waiting);
        Check(first == o1, "가장 오래된 미배정 주문");
        Check(book.AssignItem(o1.id, 100) && o1.assignedItemId == 100, "아이템 배정");
        Check(book.AssignItem(o2.id, 100) && o1.assignedItemId == -1 && o2.assignedItemId == 100, "같은 아이템 재배정 시 이전 주문 해제");
        Check(book.Advance(o2.id, OrderStatus.Cooking) && !book.Advance(o2.id, OrderStatus.Waiting), "상태는 앞으로만 진행");
        string reason;
        Check(!book.TryComplete(o2.id, 100, 3, out reason) && !o2.isPaid, "포장 전 결제 거부");
        book.Advance(o2.id, OrderStatus.Packed);
        Check(!book.TryComplete(o2.id, 999, 3, out reason) && !o2.isPaid, "다른 아이템으로 결제 거부");
        Check(book.TryComplete(o2.id, 100, 3, out reason) && o2.isPaid && o2.status == OrderStatus.Completed, "포장 완료 + 아이템 일치 시 결제");
        Check(!book.TryComplete(o2.id, 100, 3, out reason), "중복 결제 거부");
        Check(!book.Advance(o2.id, OrderStatus.Packed) && o2.status == OrderStatus.Completed, "완료 주문 상태 변경 불가");
        book.AssignItem(o1.id, 200);
        book.Advance(o1.id, OrderStatus.Cooked);
        book.Release(o1.id);
        Check(o1.assignedItemId == -1 && o1.status == OrderStatus.Waiting, "배정 해제 시 접수 상태로");
        var timeouts = book.Tick(60f);
        Check(timeouts.Count == 0 && Math.Abs(o1.remaining - 40f) < 0.01f, "대기 시간 감소");
        timeouts = book.Tick(50f);
        Check(timeouts.Count == 1 && timeouts[0] == o1, "시간 초과 감지");
        Check(book.Fail(o1.id) && o1.status == OrderStatus.Failed && !book.Fail(o1.id), "실패 처리 (1회만)");
        Check(!book.TryComplete(o1.id, 200, 1, out reason), "실패한 주문 결제 거부");
        Check(book.CountActive() == 0, "진행 중 주문 0");
        book.ClearFinished();
        var o3 = book.Create(MenuType.Fried, false, 18000, 2000, 100f, 9);
        Check(book.All.Count == 1 && o3.id == 3, "새 날: 끝난 주문 정리, id 는 계속 증가 (이전 포장과 혼동 방지)");
        book.AssignItem(o3.id, 300);
        book.Advance(o3.id, OrderStatus.Packed);
        float before = o3.remaining;
        book.Tick(10f);
        Check(Math.Abs((before - o3.remaining) - 2.5f) < 0.01f, "포장 완료 주문은 대기 시간이 천천히 감소");

        Console.WriteLine("[SatisfactionRules / MoneyFormat]");
        var rules = new SatisfactionRules();
        Check(rules.Stars(0.9f) == 3 && rules.Stars(0.4f) == 2 && rules.Stars(0.1f) == 1, "별점 구간");
        Check(rules.TipFor(3, 21500) == 2200 && rules.TipFor(2, 21500) == 0, "팁 (★★★ 10%, 100원 단위)");
        Check(rules.ReputationFor(3) > rules.ReputationFor(2) && rules.ReputationFor(2) > rules.ReputationFor(1), "만족도별 평판 증가량");
        Check(MoneyFormat.Won(1234567) == "1,234,567원" && MoneyFormat.SignedWon(-3000) == "-3,000원" && MoneyFormat.SignedWon(500) == "+500원", "원화 표시");
        Check(OrderData.RequiredSauce(MenuType.Fried) == SauceType.None && OrderData.RequiredSauce(MenuType.Ganjang) == SauceType.Ganjang, "메뉴별 필요 소스");

        Console.WriteLine();
        Console.WriteLine("결과: " + passed + " 통과 / " + failed + " 실패");
        return failed == 0 ? 0 : 1;
    }
}
