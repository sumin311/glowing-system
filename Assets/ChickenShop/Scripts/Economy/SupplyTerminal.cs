namespace ChickenShop
{
    /// <summary>
    /// 발주 단말기 (재고 구매 지점): E 로 재료 구매 / 장비 업그레이드 / 메뉴 해금 화면을 연다.
    /// </summary>
    public class SupplyTerminal : Interactable
    {
        public override string GetPrompt(PlayerInteractor player)
        {
            return Action("발주 단말기 열기 (재료 구매 / 업그레이드)");
        }

        public override void Interact(PlayerInteractor player)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.UI == null) return;
            gm.UI.OpenShop(ShopTab.Supplies);
        }
    }
}
