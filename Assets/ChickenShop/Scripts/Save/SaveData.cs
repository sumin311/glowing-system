using System;
using System.Collections.Generic;

namespace ChickenShop
{
    /// <summary>
    /// 저장 파일 구조 (JsonUtility 직렬화). 영업일 경계(준비/정산)에서만 저장하므로 손님/조리 중 상태는 포함하지 않는다.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string savedAt;
        /// <summary>다음에 시작할 영업일</summary>
        public int day = 1;
        public long money;
        public long lifetimeRevenue;
        public float reputation = 50f;
        /// <summary>IngredientType 순서대로 재고 수량</summary>
        public int[] inventory = new int[0];
        public List<UpgradeLevelEntry> upgrades = new List<UpgradeLevelEntry>();
        public List<int> unlockedMenus = new List<int>();
        /// <summary>최근 영업일 결과 (최대 14일)</summary>
        public List<DailyReport> history = new List<DailyReport>();
    }
}
