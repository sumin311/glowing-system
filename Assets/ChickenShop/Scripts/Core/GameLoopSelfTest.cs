using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 플레이 모드 자동 검증 (에디터/개발 빌드에서 F10, 또는 메뉴 Chicken Shop > Run Self-Test).
    /// 실제 씬의 작업대/손님/매니저를 그대로 사용해 한 번의 영업 루프를 자동 수행하며 다음을 확인한다:
    ///  재고 차감, 미완성 음식 포장/전달 차단, 조리 시간/상태 전환, 결제 1회, 중복 결제 차단,
    ///  주문 시간 초과 + 평판 하락, 마감 + 정산(운영비), 저장/불러오기 왕복.
    /// 결과는 Console 에 [ChickenShop/SelfTest] PASS/FAIL 로 출력된다.
    /// 주의: 진행 중인 게임 상태를 바꾸므로 새 게임에서 실행할 것. 기존 저장 파일은 백업 후 복원한다.
    /// </summary>
    public class GameLoopSelfTest : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
#pragma warning disable 0414 // 개발 빌드가 아닐 때는 사용되지 않음
        [SerializeField] private KeyCode runKey = KeyCode.F10;
#pragma warning restore 0414

        private readonly StringBuilder log = new StringBuilder();
        private int passed, failed;
        private bool running;

        public static bool LastRunPassed { get; private set; }
        /// <summary>끝난 실행 횟수 (중단 포함). batchmode 실행기가 완료를 기다릴 때 사용.</summary>
        public static int FinishedRuns { get; private set; }
        /// <summary>마지막 실행의 전체 로그.</summary>
        public static string LastReport { get; private set; } = "";

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!running && Input.GetKeyDown(runKey)) Run();
#endif
        }

        public void Run()
        {
            if (running) return;
            if (interactor == null) interactor = FindObjectOfType<PlayerInteractor>();
            StartCoroutine(RunRoutine());
        }

        private void Check(bool condition, string what)
        {
            if (condition) { passed++; log.AppendLine("  PASS  " + what); }
            else { failed++; log.AppendLine("  FAIL  " + what); Debug.LogError("[ChickenShop/SelfTest] FAIL: " + what); }
        }

        private static T Find<T>(System.Func<T, bool> pred = null) where T : Object
        {
            foreach (var o in FindObjectsOfType<T>()) if (pred == null || pred(o)) return o;
            return null;
        }

        private IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        private IEnumerator RunRoutine()
        {
            running = true;
            passed = failed = 0;
            log.Length = 0;
            var gm = GameManager.Instance;
            string backup = SaveManager.SavePath + ".selftest_backup";
            bool hadSave = File.Exists(SaveManager.SavePath);
            if (hadSave) File.Copy(SaveManager.SavePath, backup, true);
            log.AppendLine("[ChickenShop/SelfTest] 영업 루프 자동 검증 시작");

            if (gm == null || interactor == null || gm.Day == null)
            {
                Abort("GameManager/PlayerInteractor 가 없습니다.");
                yield break;
            }
            if (gm.Day.Phase != ShopPhase.PreOpen)
            {
                Abort("영업 준비 단계(새 게임 시작 직후)에서 실행하세요.");
                yield break;
            }
            var carry = interactor.Carry;
            carry.Clear();
            var inv = gm.Inventory;
            // 테스트에 충분한 재고 확보 (구매 경로 검증 포함)
            long m0 = gm.Economy.Money;
            string msg;
            bool bought = inv.Purchase(IngredientType.RawChicken, 2, out msg);
            Check(bought && gm.Economy.Money == m0 - inv.PriceOf(IngredientType.RawChicken, 2), "재료 구매: 자금 차감 = 단가 x 수량");
            gm.Economy.ReceivePayment(0, 50000); // 테스트용 자금 (팁 장부에 기록됨)

            var fridge = Find<IngredientFridge>();
            var prep = Find<PrepStation>();
            var fryer = Find<FryerStation>(f => { string r; return !f.IsLocked(out r); });
            var pack = Find<PackagingStation>();
            var delivery = Find<DeliveryCounter>();
            Check(fridge && prep && fryer && pack && delivery, "작업대 존재 (냉장고/반죽/튀김기/포장/픽업대)");
            if (!(fridge && prep && fryer && pack && delivery)) { Finish(gm, hadSave, backup); yield break; }

            // ---- 영업 시작 + 손님
            string reason;
            Check(gm.Day.TryOpenShop(out reason), "영업 시작");
            Check(gm.Customers.TrySpawn(), "손님 생성");
            var customer = gm.Customers.Active[gm.Customers.ActiveCount - 1];
            for (int i = 0; i < 10 && customer.State != CustomerState.Ordering; i++) { customer.DebugSnapToDestination(); yield return null; }
            Check(customer.State == CustomerState.Ordering, "손님이 카운터까지 이동해 주문 대기");
            customer.TakeOrder();
            var order = customer.Order;
            Check(order != null && order.status == OrderStatus.Waiting && !order.isPaid, "주문 생성 (접수 상태, 미결제)");
            if (order == null) { Finish(gm, hadSave, backup); yield break; }
            log.AppendLine("  주문: " + order.ShortLabel() + " " + MoneyFormat.Won(order.TotalPrice));

            // ---- 생닭
            int raw0 = inv.Get(IngredientType.RawChicken);
            fridge.Interact(interactor);
            Check(!carry.IsEmpty && carry.Held.Stage == ChickenStage.Raw, "냉장고: 생닭을 손에 듦");
            Check(inv.Get(IngredientType.RawChicken) == raw0 - 1, "생닭 재고 1 차감");
            var item = carry.Held;

            // ---- 미완성 음식 차단
            long mBefore = gm.Economy.Money;
            delivery.Interact(interactor);
            Check(carry.Held == item && gm.Economy.Money == mBefore && !order.isPaid, "포장 안 된 생닭 전달 차단 (결제 없음)");
            int box0 = inv.Get(IngredientType.ChickenBox);
            pack.Interact(interactor);
            Check(carry.Held == item && inv.Get(IngredientType.ChickenBox) == box0, "생닭 포장 차단 (상자 재고 유지)");
            fryer.Interact(interactor);
            Check(carry.Held == item, "반죽 전 생닭 튀김 차단");

            // ---- 반죽
            int batter0 = inv.Get(IngredientType.Batter);
            prep.Interact(interactor);
            Check(carry.IsEmpty && prep.SlotItem == item && prep.IsWorking, "반죽 작업 시작");
            Check(inv.Get(IngredientType.Batter) == batter0 - 1, "튀김가루 1 차감");
            prep.Interact(interactor);
            Check(carry.IsEmpty, "작업 중에는 꺼낼 수 없음");
            prep.DebugFinishWork();
            yield return Frames(2);
            prep.Interact(interactor);
            Check(carry.Held == item && item.Stage == ChickenStage.Battered, "반죽 완료 -> 손에 듦");

            // ---- 튀김
            int oil0 = inv.Get(IngredientType.FryingOil);
            fryer.Interact(interactor);
            Check(fryer.SlotItem == item && item.Stage == ChickenStage.Frying, "튀김기 투입 (조리 중)");
            Check(inv.Get(IngredientType.FryingOil) == oil0 - 1, "식용유 1 차감");
            Check(item.OrderId == order.id && order.status == OrderStatus.Cooking, "튀김 시작 시 주문 자동 배정 + 상태 '조리 중'");
            float t0 = fryer.Progress;
            yield return new WaitForSeconds(0.5f);
            Check(fryer.Progress > t0 && fryer.Progress < 1f, "조리 진행도가 시간에 따라 증가 (" + Mathf.RoundToInt(fryer.Progress * 100f) + "%)");
            fryer.Interact(interactor);
            Check(carry.IsEmpty && fryer.SlotItem == item, "조리 중에는 꺼낼 수 없음");
            fryer.DebugFinishWork();
            yield return Frames(2);
            Check(item.Stage == ChickenStage.Fried && order.status == OrderStatus.Cooked, "튀김 완성 + 주문 '튀김 완료'");
            fryer.Interact(interactor);
            Check(carry.Held == item, "완성 치킨 꺼내기");

            // ---- 소스 (필요한 메뉴만)
            var need = OrderData.RequiredSauce(order.menu);
            if (need != SauceType.None)
            {
                var sauce = Find<SauceStation>(s => s.Sauce == need);
                var ing = need == SauceType.Ganjang ? IngredientType.GanjangSauce : IngredientType.YangnyeomSauce;
                int s0 = inv.Get(ing);
                // 소스 전 포장 시도 -> 메뉴 불일치로 차단
                pack.Interact(interactor);
                Check(carry.Held == item, "소스 안 바른 치킨은 양념/간장 주문으로 포장되지 않음");
                sauce.Interact(interactor);
                Check(inv.Get(ing) == s0 - 1, "소스 1 차감");
                sauce.DebugFinishWork();
                yield return Frames(2);
                sauce.Interact(interactor);
                Check(carry.Held == item && item.Sauce == need && order.status == OrderStatus.Sauced, "소스 완료 + 주문 '소스 완료'");
            }

            // ---- 포장
            int bag0 = inv.Get(IngredientType.PaperBag), cola0 = inv.Get(IngredientType.Cola);
            box0 = inv.Get(IngredientType.ChickenBox);
            pack.Interact(interactor);
            Check(pack.SlotItem == item, "포장 작업 시작");
            Check(inv.Get(IngredientType.ChickenBox) == box0 - 1 && inv.Get(IngredientType.PaperBag) == bag0 - 1, "상자/봉투 1개씩 차감");
            Check(inv.Get(IngredientType.Cola) == cola0 - (order.withDrink ? 1 : 0), "음료 주문일 때만 콜라 차감");
            pack.DebugFinishWork();
            yield return Frames(2);
            pack.Interact(interactor);
            Check(carry.Held == item && item.IsPacked && order.status == OrderStatus.Packed, "포장 완료 + 주문 '포장 완료'");

            // ---- 전달 + 결제
            mBefore = gm.Economy.Money;
            long revenueBefore = gm.Economy.Today.revenue;
            float repBefore = gm.Reputation.Value;
            customer.Interact(interactor);
            Check(carry.IsEmpty && order.isPaid && order.status == OrderStatus.Completed, "손님에게 전달 -> 결제 완료");
            long gained = gm.Economy.Money - mBefore;
            Check(gained >= order.TotalPrice && gm.Economy.Today.revenue == revenueBefore + order.TotalPrice, "매출 = 주문 금액 (" + MoneyFormat.Won(gained) + " 입금)");
            Check(gm.Reputation.Value > repBefore, "만족한 손님 -> 평판 상승");
            // 중복 결제 시도
            long mAfter = gm.Economy.Money;
            string dupMsg;
            bool dup = gm.Orders.TryDeliver(item, customer, out dupMsg);
            Check(!dup && gm.Economy.Money == mAfter, "같은 주문 중복 결제 차단");

            // ---- 시간 초과
            Check(gm.Customers.TrySpawn(), "두 번째 손님 생성");
            var c2 = gm.Customers.Active[gm.Customers.ActiveCount - 1];
            for (int i = 0; i < 10 && c2.State != CustomerState.Ordering; i++) { c2.DebugSnapToDestination(); yield return null; }
            c2.TakeOrder();
            var o2 = c2.Order;
            float repB = gm.Reputation.Value;
            int failedBefore = gm.Economy.Today.ordersFailed;
            if (o2 != null) o2.remaining = 0.05f;
            yield return new WaitForSeconds(0.3f);
            Check(o2 != null && o2.status == OrderStatus.Failed, "대기 시간 초과 -> 주문 실패");
            Check(gm.Reputation.Value < repB && gm.Economy.Today.ordersFailed == failedBefore + 1, "시간 초과 -> 평판 하락 + 실패 집계");
            Check(c2.State == CustomerState.Leaving, "불만족 손님 퇴장");

            // ---- 마감 + 정산
            long beforeClose = gm.Economy.Money;
            gm.Day.CloseEarly();
            for (int i = 0; i < 300 && gm.Day.Phase != ShopPhase.Settlement; i++)
            {
                foreach (var c in new List<CustomerAgent>(gm.Customers.Active)) if (c != null) c.DebugSnapToDestination();
                yield return null;
            }
            Check(gm.Day.Phase == ShopPhase.Settlement, "마감 -> 손님 퇴장 후 정산 단계");
            var report = gm.Day.LastReport;
            long op = gm.Economy.BaseOperatingCost + gm.Progression.DailyUpkeep;
            Check(report != null && report.operatingCost == op && gm.Economy.Money == beforeClose - op, "정산 시 운영비 청구 (" + MoneyFormat.Won(op) + ")");
            Check(report != null && report.ordersCompleted == 1 && report.ordersFailed >= 1, "정산 보고서: 완료 1건, 실패 1건 이상");
            Check(report != null && report.NetProfit == report.revenue + report.tips - report.ingredientCost - report.operatingCost, "순이익 = 매출 + 팁 - 재료비 - 운영비");
            Check(gm.Save.HasSave, "정산 후 자동 저장 파일 생성");

            // ---- 저장/불러오기 왕복
            int dayNext = gm.Day.Day + 1;
            long savedMoney = gm.Economy.Money;
            int savedRaw = inv.Get(IngredientType.RawChicken);
            float savedRep = gm.Reputation.Value;
            string err;
            Check(gm.Save.TrySave(out err), "정산 화면에서 저장");
            inv.LoadStateSingle(IngredientType.RawChicken, 999);
            gm.Economy.ReceivePayment(0, 12345);
            Check(gm.Save.TryLoad(out err), "불러오기");
            yield return null;
            Check(gm.Day.Day == dayNext && gm.Day.Phase == ShopPhase.PreOpen, "불러오기 후 다음 날(" + dayNext + "일차) 준비 상태");
            Check(gm.Economy.Money == savedMoney && inv.Get(IngredientType.RawChicken) == savedRaw && Mathf.Abs(gm.Reputation.Value - savedRep) < 0.01f, "자금/재고/평판 복원");

            Finish(gm, hadSave, backup);
        }

        private void Finish(GameManager gm, bool hadSave, string backup)
        {
            // 원래 저장 파일 복원
            try
            {
                if (hadSave && File.Exists(backup)) { File.Copy(backup, SaveManager.SavePath, true); File.Delete(backup); }
                else if (!hadSave && File.Exists(SaveManager.SavePath)) File.Delete(SaveManager.SavePath);
            }
            catch (System.Exception ex) { Debug.LogWarning("[ChickenShop/SelfTest] 저장 파일 복원 실패: " + ex.Message); }
            LastRunPassed = failed == 0 && passed > 0;
            log.AppendLine(string.Format("[ChickenShop/SelfTest] 결과: {0} 통과 / {1} 실패", passed, failed));
            LastReport = log.ToString();
            if (failed == 0) Debug.Log(LastReport);
            else Debug.LogError(LastReport);
            GameManager.Toast("자동 검증: " + passed + " 통과 / " + failed + " 실패 (Console 확인)", failed == 0 ? ToastKind.Success : ToastKind.Error);
            running = false;
            FinishedRuns++;
        }

        private void Abort(string why)
        {
            Debug.LogError("[ChickenShop/SelfTest] " + why);
            LastRunPassed = false;
            LastReport = "[ChickenShop/SelfTest] 중단: " + why;
            running = false;
            FinishedRuns++;
        }
    }
}
