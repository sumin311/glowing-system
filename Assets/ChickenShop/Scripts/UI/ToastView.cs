using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChickenShop
{
    /// <summary>화면 중앙 하단에 잠깐 표시되는 안내 메시지 (최대 4개, 페이드아웃).</summary>
    public class ToastView : MonoBehaviour
    {
        private class Entry
        {
            public RectTransform rect;
            public Text text;
            public Image bg;
            public float life;
            public float duration;
        }

        private const int MaxEntries = 4;
        private readonly List<Entry> entries = new List<Entry>();
        private readonly Queue<Entry> pool = new Queue<Entry>();
        private RectTransform container;
        private string lastMessage;
        private float lastTime;

        public void Build()
        {
            container = UIFactory.Rect(transform, "ToastContainer");
            UIFactory.Anchor(container, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(1100f, 260f));
            var v = UIFactory.Vertical(container.gameObject, 6f, 0, TextAnchor.LowerCenter);
            v.childForceExpandWidth = false;
            v.childControlWidth = true;
        }

        public void Show(string message, ToastKind kind, float duration)
        {
            if (string.IsNullOrEmpty(message) || container == null) return;
            // 같은 메시지 연타 방지
            if (message == lastMessage && Time.unscaledTime - lastTime < 0.4f) return;
            lastMessage = message;
            lastTime = Time.unscaledTime;
            if (entries.Count >= MaxEntries) Recycle(entries[0]);
            var e = pool.Count > 0 ? pool.Dequeue() : CreateEntry();
            e.rect.gameObject.SetActive(true);
            e.rect.SetAsLastSibling();
            e.text.text = message;
            Color c;
            switch (kind)
            {
                case ToastKind.Success: c = new Color(0.16f, 0.42f, 0.2f, 0.92f); break;
                case ToastKind.Warning: c = new Color(0.55f, 0.38f, 0.08f, 0.92f); break;
                case ToastKind.Error: c = new Color(0.6f, 0.14f, 0.1f, 0.92f); break;
                default: c = new Color(0.1f, 0.1f, 0.12f, 0.85f); break;
            }
            e.bg.color = c;
            e.text.color = Color.white;
            e.life = e.duration = Mathf.Max(1f, duration);
            entries.Add(e);
        }

        private Entry CreateEntry()
        {
            var rt = UIFactory.Panel(container, "Toast", Color.black);
            var h = UIFactory.Horizontal(rt.gameObject, 0f, 10, TextAnchor.MiddleCenter);
            h.padding = new RectOffset(22, 22, 8, 8);
            h.childForceExpandWidth = false;
            var fitter = rt.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var t = UIFactory.Label(rt, string.Empty, 26, TextAnchor.MiddleCenter, Color.white, true);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return new Entry { rect = rt, text = t, bg = rt.GetComponent<Image>() };
        }

        private void Recycle(Entry e)
        {
            entries.Remove(e);
            e.rect.gameObject.SetActive(false);
            pool.Enqueue(e);
        }

        private void Update()
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];
                e.life -= Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(e.life / 0.5f);
                var bc = e.bg.color; bc.a = Mathf.Min(0.92f, a * 0.92f); e.bg.color = bc;
                var tc = e.text.color; tc.a = a; e.text.color = tc;
                if (e.life <= 0f) Recycle(e);
            }
        }
    }
}
