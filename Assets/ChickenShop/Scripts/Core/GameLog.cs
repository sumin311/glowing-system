using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 로그 도우미. 모든 로그에 [ChickenShop/태그] 접두사를 붙여 Console 에서 원인 추적을 쉽게 한다.
    /// </summary>
    public static class GameLog
    {
        public static bool Verbose = true;

        public static void Info(string tag, string message)
        {
            if (Verbose) Debug.Log("[ChickenShop/" + tag + "] " + message);
        }

        public static void Warn(string tag, string message, Object context = null)
        {
            Debug.LogWarning("[ChickenShop/" + tag + "] " + message, context);
        }

        public static void Error(string tag, string message, Object context = null)
        {
            Debug.LogError("[ChickenShop/" + tag + "] " + message, context);
        }
    }
}
