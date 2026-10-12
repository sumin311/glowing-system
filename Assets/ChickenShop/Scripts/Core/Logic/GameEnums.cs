// 게임 전역에서 쓰는 열거형 모음 (UnityEngine 비의존 - 단위 테스트 가능)
namespace ChickenShop
{
    /// <summary>판매 메뉴 종류.</summary>
    public enum MenuType
    {
        Fried = 0,      // 후라이드치킨
        Yangnyeom = 1,  // 양념치킨
        Ganjang = 2,    // 간장치킨 (해금 메뉴)
    }

    /// <summary>치킨에 바르는 소스.</summary>
    public enum SauceType
    {
        None = 0,
        Yangnyeom = 1,
        Ganjang = 2,
    }

    /// <summary>재고 품목. 저장 파일과 호환되도록 값은 바꾸지 않는다(추가만).</summary>
    public enum IngredientType
    {
        RawChicken = 0,     // 생닭
        Batter = 1,         // 튀김가루
        FryingOil = 2,      // 식용유(1회 튀김분)
        YangnyeomSauce = 3, // 양념 소스
        GanjangSauce = 4,   // 간장 소스
        ChickenBox = 5,     // 치킨 상자
        PaperBag = 6,       // 종이봉투
        Cola = 7,           // 콜라
    }

    /// <summary>주문 진행 상태. 값이 클수록 더 진행된 상태(Failed 제외).</summary>
    public enum OrderStatus
    {
        Waiting = 0,    // 접수됨 (조리 전)
        Cooking = 1,    // 튀기는 중
        Cooked = 2,     // 튀김 완료
        Sauced = 3,     // 소스 작업 완료 (후라이드는 생략)
        Packed = 4,     // 포장 완료
        Completed = 5,  // 전달 + 결제 완료
        Failed = 6,     // 시간 초과 / 취소
    }

    /// <summary>하루 영업 단계.</summary>
    public enum ShopPhase
    {
        PreOpen = 0,    // 영업 준비 (시간 정지, 재료 준비/구매 가능)
        Open = 1,       // 영업 중 (손님 입장)
        Closing = 2,    // 마감 중 (신규 입장 중지, 남은 주문 처리)
        Settlement = 3, // 일일 정산
    }

    /// <summary>지출 분류 (일일 정산용).</summary>
    public enum ExpenseCategory
    {
        Ingredients = 0,
        Upgrade = 1,
        Operating = 2,
    }
}
