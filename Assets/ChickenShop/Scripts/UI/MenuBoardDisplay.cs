using System.Text;
using UnityEngine;

namespace ChickenShop
{
    /// <summary>카운터 위 메뉴판: 판매 중인 메뉴와 가격을 표시 (해금 시 자동 갱신).</summary>
    public class MenuBoardDisplay : MonoBehaviour
    {
        [SerializeField] private WorldLabel label;

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Progression != null) gm.Progression.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Progression != null) gm.Progression.Changed -= Refresh;
        }

        private void Refresh()
        {
            var gm = GameManager.Instance;
            if (label == null || gm == null || gm.Menu == null) return;
            var sb = new StringBuilder();
            foreach (var m in gm.Menu.Menus)
            {
                bool unlocked = gm.Progression == null || gm.Progression.IsMenuUnlocked(m.type);
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(m.displayName).Append("   ").Append(unlocked ? MoneyFormat.Won(m.price) : "(준비 중)");
            }
            sb.Append("\n콜라 추가   +").Append(MoneyFormat.Won(gm.Menu.DrinkPrice));
            label.SetText(sb.ToString(), new Color(1f, 0.97f, 0.88f));
        }
    }
}
