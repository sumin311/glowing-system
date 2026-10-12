using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 한글 폰트 로더. Resources/Fonts 의 나눔스퀘어라운드(OFL)를 사용하고, 없으면 Unity 기본 폰트로 대체한다.
    /// </summary>
    public static class UIFonts
    {
        private static Font regular;
        private static Font bold;

        public static Font Regular
        {
            get
            {
                if (regular == null) regular = Load("Fonts/NanumSquareRoundR");
                return regular;
            }
        }

        public static Font Bold
        {
            get
            {
                if (bold == null) bold = Load("Fonts/NanumSquareRoundB");
                return bold;
            }
        }

        private static Font Load(string path)
        {
            var f = Resources.Load<Font>(path);
            if (f == null)
            {
                GameLog.Warn("UI", "폰트를 찾을 수 없습니다: Resources/" + path + " (기본 폰트 사용 - 한글이 깨질 수 있음)");
                f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return f;
        }
    }
}
