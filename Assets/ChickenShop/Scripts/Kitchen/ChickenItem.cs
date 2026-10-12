using UnityEngine;

namespace ChickenShop
{
    /// <summary>치킨 조리 단계.</summary>
    public enum ChickenStage
    {
        Raw = 0,       // 생닭
        Battered = 1,  // 반죽 입힘
        Frying = 2,    // 튀김기 안에서 조리 중
        Fried = 3,     // 튀김 완료
        Burnt = 4,     // 과조리 (판매 불가)
    }

    /// <summary>
    /// 플레이어가 들고 다니는 치킨 (쟁반 위 치킨 -> 포장 상자). 게임에서 유일한 운반 아이템.
    /// 상태(단계, 소스, 포장, 연결된 주문)를 보관하고 상태에 맞는 3D 모델을 ItemFactory 에서 받아 표시한다.
    /// 아이템은 Collider 가 없어 플레이어의 상호작용 레이캐스트를 가리지 않는다.
    /// </summary>
    public class ChickenItem : MonoBehaviour
    {
        public int ItemId { get; private set; }
        public ChickenStage Stage { get; private set; }
        public SauceType Sauce { get; private set; }
        public bool IsPacked { get; private set; }
        public bool HasDrink { get; private set; }
        /// <summary>연결된 주문 id (-1 = 미배정)</summary>
        public int OrderId { get; private set; } = -1;
        /// <summary>튀김 진행도 0~1 (표시용)</summary>
        public float CookProgress { get; set; }
        public bool IsConsumed { get; private set; }

        private ItemFactory factory;
        private Transform visualRoot;
        private GameObject visual;
        private string visualKey;
        private WorldLabel label;

        public void Init(int id, ItemFactory owner)
        {
            ItemId = id;
            factory = owner;
            Stage = ChickenStage.Raw;
            Sauce = SauceType.None;
            visualRoot = new GameObject("Visual").transform;
            visualRoot.SetParent(transform, false);
            label = WorldLabel.Create(transform, new Vector3(0f, 0.28f, 0f), 40, 0.0025f);
            RefreshVisual();
        }

        // ------------------------------------------------------------------ 상태 변경
        public void SetStage(ChickenStage stage)
        {
            Stage = stage;
            RefreshVisual();
        }

        public void ApplySauce(SauceType sauce)
        {
            Sauce = sauce;
            RefreshVisual();
        }

        public void SetOrder(int orderId)
        {
            OrderId = orderId > 0 ? orderId : -1;
            RefreshLabel();
        }

        public void Pack(int orderId, bool withDrink)
        {
            IsPacked = true;
            HasDrink = withDrink;
            SetOrder(orderId);
            RefreshVisual();
        }

        /// <summary>튀김 완료 후 소스 상태에 따른 완성 메뉴. 완성 전/탄 경우 null.</summary>
        public MenuType? ResultMenu
        {
            get
            {
                if (Stage != ChickenStage.Fried) return null;
                switch (Sauce)
                {
                    case SauceType.Yangnyeom: return MenuType.Yangnyeom;
                    case SauceType.Ganjang: return MenuType.Ganjang;
                    default: return MenuType.Fried;
                }
            }
        }

        /// <summary>HUD/안내용 설명.</summary>
        public string Describe()
        {
            string s;
            if (IsPacked) s = "포장된 " + (ResultMenu.HasValue ? OrderData.MenuName(ResultMenu.Value) : "치킨") + (HasDrink ? "+콜라" : string.Empty);
            else
            {
                switch (Stage)
                {
                    case ChickenStage.Raw: s = "생닭"; break;
                    case ChickenStage.Battered: s = "반죽 입힌 닭"; break;
                    case ChickenStage.Frying: s = "튀기는 중인 닭"; break;
                    case ChickenStage.Burnt: s = "탄 치킨(판매 불가)"; break;
                    default:
                        s = Sauce == SauceType.None ? "후라이드 (소스 전)" : OrderData.MenuName(ResultMenu.Value);
                        break;
                }
            }
            if (OrderId > 0) s += " [주문 #" + OrderId + "]";
            return s;
        }

        // ------------------------------------------------------------------ 위치/표시
        public void AttachTo(Transform parent, float scale, Quaternion localRotation)
        {
            transform.SetParent(parent, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = localRotation;
            transform.localScale = Vector3.one * scale;
            // 손에 든 동안 라벨이 화면을 가리지 않도록 숨긴다
            if (label != null) label.SetVisible(parent == null || parent.GetComponentInParent<PlayerCarry>() == null);
        }

        public void RefreshVisual()
        {
            if (factory == null || visualRoot == null) return;
            string key = ItemFactory.VisualKey(this);
            if (key != visualKey)
            {
                if (visual != null) Destroy(visual);
                visual = factory.CreateVisual(key, HasDrink, visualRoot);
                visualKey = key;
            }
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (label == null) return;
            if (OrderId > 0)
            {
                var gm = GameManager.Instance;
                var order = gm != null && gm.Orders != null ? gm.Orders.GetActive(OrderId) : null;
                label.SetText(order != null ? order.ShortLabel() : string.Empty, new Color(1f, 0.95f, 0.6f));
            }
            else if (Stage == ChickenStage.Burnt)
            {
                label.SetText("탄 치킨 - 버리세요", new Color(1f, 0.5f, 0.4f));
            }
            else
            {
                label.SetText(string.Empty);
            }
        }

        /// <summary>전달/폐기로 아이템 소멸. 중복 처리 방지를 위해 플래그를 먼저 세운다.</summary>
        public void Consume()
        {
            if (IsConsumed) return;
            IsConsumed = true;
            if (factory != null) factory.Unregister(this);
            Destroy(gameObject);
        }
    }
}
