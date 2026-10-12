using UnityEngine;

namespace ChickenShop
{
    /// <summary>F1 도움말: 조작법과 영업 흐름 안내.</summary>
    public class HelpPanel : UIPanel
    {
        public const string ControlsText =
            "<b>조작법</b>\n" +
            "WASD 이동   Shift 달리기   Space 점프   마우스 시점\n" +
            "E 상호작용 (바라보는 작업대/손님)\n" +
            "Tab 주문·재고 화면   ESC 일시정지/창 닫기   F1 도움말\n" +
            "Left Alt (누르는 동안) 커서 표시 - 영업 중 UI 클릭\n";

        public const string FlowText =
            "<b>영업 흐름</b>\n" +
            "1. 영업 팻말(카운터 왼쪽)에서 [E] → 영업 시작\n" +
            "2. 카운터 앞 손님에게 [E] → 주문 접수 (POS 에서도 가능)\n" +
            "3. 냉장고 [E] → 생닭 꺼내기 (생닭 재고 -1)\n" +
            "4. 반죽 작업대 [E] → 튀김옷 입히기 (튀김가루 -1)\n" +
            "5. 튀김기 [E] → 튀기기 (식용유 -1). 완성 후 방치하면 탑니다!\n" +
            "6. 양념/간장 주문이면 소스통 [E] (소스 -1). 후라이드는 생략\n" +
            "7. 포장 작업대 [E] (상자·봉투 -1, 음료 주문이면 콜라 -1)\n" +
            "8. 픽업대 또는 손님에게 [E] → 전달 + 결제\n" +
            "9. 마감 후 정산 → 발주 단말기에서 재료 구매 · 업그레이드 → 다음 날\n\n" +
            "<b>팁</b>: 영업 준비 중에는 시간이 멈춥니다. 튀김기가 돌아가는 동안 다음 닭을 준비하세요.\n" +
            "빨리 전달할수록 만족도(★)와 평판, 팁이 올라가고 손님이 더 자주 옵니다.";

        protected override void Build()
        {
            var body = CreateWindow("도움말  (F1 / ESC 닫기)", new Vector2(1150f, 820f));
            var t = UIFactory.Label(body, ControlsText + "\n" + FlowText, 26, TextAnchor.UpperLeft, UIFactory.TextMain);
            UIFactory.Stretch((RectTransform)t.transform, 10f);
        }
    }
}
