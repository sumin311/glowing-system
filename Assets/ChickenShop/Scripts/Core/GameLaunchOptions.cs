namespace ChickenShop
{
    /// <summary>메인 메뉴 -> 게임 씬으로 넘기는 시작 옵션 (씬 전환 간 유지되는 정적 값).</summary>
    public static class GameLaunchOptions
    {
        /// <summary>true 면 저장 파일을 불러와 시작, false 면 새 게임.</summary>
        public static bool ContinueGame;

        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "ChickenShop";
    }
}
