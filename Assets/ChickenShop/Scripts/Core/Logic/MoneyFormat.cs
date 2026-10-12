using System.Globalization;

namespace ChickenShop
{
    /// <summary>원화 표시 도우미.</summary>
    public static class MoneyFormat
    {
        /// <summary>12000 -> "12,000원", -3000 -> "-3,000원"</summary>
        public static string Won(long amount)
        {
            // 시스템 언어와 무관하게 항상 1,234,567 형식 (독일어 등은 구분 기호가 '.' 이므로 고정)
            return amount.ToString("#,0", CultureInfo.InvariantCulture) + "원";
        }

        /// <summary>부호를 붙인 표시: +12,000원 / -3,000원</summary>
        public static string SignedWon(long amount)
        {
            return (amount > 0 ? "+" : string.Empty) + Won(amount);
        }
    }
}
