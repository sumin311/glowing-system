using UnityEngine;
using UnityEngine.UI;

namespace ChickenShop
{
    /// <summary>
    /// 월드 공간에 떠 있는 글자 라벨 (손님 말풍선, 아이템 주문번호, 작업대 이름 등).
    /// World Space Canvas + Text 로 런타임에 스스로 구성되며 항상 카메라를 바라본다.
    /// </summary>
    public class WorldLabel : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.5f, 0f);
        [SerializeField] private int fontSize = 48;
        [SerializeField] private float worldScale = 0.004f;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private bool billboard = true;
        [SerializeField] private string initialText;

        private Canvas canvas;
        private Text text;
        private Image background;
        private RectTransform bgRect;
        private Transform cam;
        private string currentText;

        public static WorldLabel Create(Transform parent, Vector3 localOffset, int size = 48, float scale = 0.004f)
        {
            // 비활성 상태에서 값을 채운 뒤 활성화해야 Awake 에서 올바른 값으로 구성된다
            var go = new GameObject("WorldLabel");
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<WorldLabel>();
            label.offset = localOffset;
            label.fontSize = size;
            label.worldScale = scale;
            go.SetActive(true);
            return label;
        }

        private void Awake()
        {
            if (canvas == null) Build();
            if (!string.IsNullOrEmpty(initialText)) SetText(initialText);
        }

        private void Build()
        {
            if (canvas != null) return;
            canvas = gameObject.AddComponent<Canvas>(); // RectTransform 으로 교체됨
            transform.localPosition = offset;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(600f, 120f);
            rt.localScale = Vector3.one * worldScale;

            var bgGo = new GameObject("BG", typeof(RectTransform));
            bgGo.transform.SetParent(transform, false);
            background = bgGo.AddComponent<Image>();
            background.color = backgroundColor;
            background.raycastTarget = false;
            bgRect = (RectTransform)bgGo.transform;

            var tGo = new GameObject("Text", typeof(RectTransform));
            tGo.transform.SetParent(transform, false);
            text = tGo.AddComponent<Text>();
            text.font = UIFonts.Bold;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = textColor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var trt = (RectTransform)tGo.transform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(600f, 120f);
            SetText(string.Empty);
        }

        public void SetText(string value)
        {
            SetText(value, textColor);
        }

        public void SetText(string value, Color color)
        {
            if (text == null) Build();
            if (value == null) value = string.Empty;
            text.color = color;
            if (value == currentText) return;
            currentText = value;
            text.text = value;
            bool visible = value.Length > 0;
            text.enabled = visible;
            background.enabled = visible && backgroundColor.a > 0.01f;
            if (visible)
            {
                float w = Mathf.Max(80f, text.preferredWidth + 40f);
                float h = Mathf.Max(fontSize + 20f, text.preferredHeight + 16f);
                bgRect.sizeDelta = new Vector2(w, h);
            }
        }

        public void SetVisible(bool visible)
        {
            if (canvas != null) canvas.enabled = visible;
        }

        private void LateUpdate()
        {
            if (!billboard) return;
            if (cam == null)
            {
                var c = Camera.main;
                if (c == null) return;
                cam = c.transform;
            }
            transform.rotation = Quaternion.LookRotation(transform.position - cam.position, Vector3.up);
        }
    }
}
