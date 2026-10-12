using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ChickenShop
{
    /// <summary>
    /// uGUI 요소를 코드로 만드는 도우미. 모든 화면을 런타임에 구성하므로 씬에 직렬화된 UI 참조가 깨질 일이 없다.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Color PanelColor = new Color(0.12f, 0.08f, 0.06f, 0.92f);
        public static readonly Color PanelLight = new Color(0.22f, 0.15f, 0.11f, 0.95f);
        public static readonly Color Accent = new Color(0.93f, 0.42f, 0.16f);
        public static readonly Color AccentDark = new Color(0.62f, 0.22f, 0.1f);
        public static readonly Color TextMain = new Color(1f, 0.96f, 0.9f);
        public static readonly Color TextDim = new Color(0.8f, 0.74f, 0.66f);
        public static readonly Color Good = new Color(0.45f, 0.9f, 0.45f);
        public static readonly Color Bad = new Color(1f, 0.42f, 0.36f);
        public static readonly Color Warn = new Color(1f, 0.8f, 0.3f);

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        public static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        public static Text Label(Transform parent, string text, int size, TextAnchor align, Color color, bool bold = false)
        {
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = bold ? UIFonts.Bold : UIFonts.Regular;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.text = text;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button Button(Transform parent, string label, UnityAction onClick, Color? color = null, int fontSize = 26)
        {
            var rt = Rect(parent, "Button_" + label);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color ?? Accent;
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.1f, 1.05f);
            colors.pressedColor = new Color(0.75f, 0.7f, 0.65f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.8f);
            btn.colors = colors;
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);
            var t = Label(rt, label, fontSize, TextAnchor.MiddleCenter, Color.white, true);
            Stretch((RectTransform)t.transform, 4f);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = fontSize + 26;
            le.preferredHeight = fontSize + 26;
            return btn;
        }

        public static void SetButtonText(Button b, string text)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        public static VerticalLayoutGroup Vertical(GameObject go, float spacing, int padding, TextAnchor align = TextAnchor.UpperLeft)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.childAlignment = align;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup Horizontal(GameObject go, float spacing, int padding, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(padding, padding, padding, padding);
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            return h;
        }

        public static LayoutElement Layout(Component c, float minW = -1, float prefW = -1, float flexW = -1, float minH = -1, float prefH = -1)
        {
            // 주의: UnityEngine.Object 에는 ?? 를 쓰지 않는다 (에디터의 가짜 null 때문)
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.minWidth = minW;
            le.preferredWidth = prefW;
            le.flexibleWidth = flexW;
            le.minHeight = minH;
            le.preferredHeight = prefH;
            return le;
        }

        public static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                c.SetActive(false);
                UnityEngine.Object.Destroy(c);
            }
        }

        public static string Colorize(string s, Color c)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + s + "</color>";
        }

        public static string Bold(string s) { return "<b>" + s + "</b>"; }
    }
}
