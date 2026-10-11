using UnityEngine;
using UnityEngine.UI;

namespace ChickenShop
{
    /// <summary>
    /// 작업대 위에 뜨는 진행 막대 (조리 진행도 시각화). World Space Canvas 로 런타임 구성.
    /// </summary>
    public class WorldProgressBar : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(0.45f, 0.06f);
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);

        private RectTransform fill;
        private Image fillImage;
        private Canvas canvas;
        private Transform cam;
        private float lastProgress = -1f;

        private void Awake()
        {
            Build();
            SetVisible(false);
        }

        private void Build()
        {
            if (canvas != null) return;
            const float pxPerMeter = 400f;
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 5;
            var rt = (RectTransform)transform;
            rt.sizeDelta = size * pxPerMeter;
            rt.localScale = Vector3.one / pxPerMeter;

            var bg = new GameObject("BG", typeof(RectTransform)).GetComponent<RectTransform>();
            bg.SetParent(transform, false);
            bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one;
            bg.offsetMin = bg.offsetMax = Vector2.zero;
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = backgroundColor;
            bgImg.raycastTarget = false;

            fill = new GameObject("Fill", typeof(RectTransform)).GetComponent<RectTransform>();
            fill.SetParent(transform, false);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = new Vector2(3f, 3f); fill.offsetMax = new Vector2(-3f, -3f);
            fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = Color.green;
            fillImage.raycastTarget = false;
        }

        public void SetVisible(bool visible)
        {
            if (canvas == null) Build();
            if (canvas.enabled != visible) canvas.enabled = visible;
        }

        public void SetProgress(float t, Color color)
        {
            if (canvas == null) Build();
            t = Mathf.Clamp01(t);
            fillImage.color = color;
            if (Mathf.Abs(t - lastProgress) < 0.002f) return;
            lastProgress = t;
            fill.anchorMax = new Vector2(t, 1f);
        }

        private void LateUpdate()
        {
            if (canvas == null || !canvas.enabled) return;
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
