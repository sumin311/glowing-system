using UnityEngine;
using UnityEngine.UI;

namespace ChickenShop
{
    /// <summary>ESC 일시정지 메뉴: 계속하기, 저장, 불러오기, 조작법, 메인 메뉴, 종료. 열려 있는 동안 게임 시간이 멈춘다.</summary>
    public class PausePanel : UIPanel
    {
        private Text statusText;
        private Slider sensitivity;

        public override bool PausesGame { get { return true; } }

        protected override void Build()
        {
            var body = CreateWindow("일시정지", new Vector2(560f, 760f));
            var v = UIFactory.Vertical(body.gameObject, 12f, 6, TextAnchor.UpperCenter);
            v.childForceExpandHeight = false;
            UIFactory.Button(body, "계속하기", () => ui.ClosePause(), UIFactory.Accent, 28);
            UIFactory.Button(body, "저장하기", OnSave, UIFactory.PanelLight, 28);
            UIFactory.Button(body, "불러오기", OnLoad, UIFactory.PanelLight, 28);
            UIFactory.Button(body, "조작법 / 도움말", () => ui.OpenHelp(), UIFactory.PanelLight, 28);
            UIFactory.Button(body, "메인 메뉴", () => { var gm = GameManager.Instance; if (gm != null) gm.ReturnToMainMenu(); }, UIFactory.PanelLight, 28);
            UIFactory.Button(body, "게임 종료", () => { var gm = GameManager.Instance; if (gm != null) gm.QuitGame(); }, UIFactory.AccentDark, 28);

            var sl = UIFactory.Label(body, "마우스 감도", 22, TextAnchor.MiddleLeft, UIFactory.TextDim);
            UIFactory.Layout(sl, prefH: 30);
            sensitivity = CreateSlider(body);
            statusText = UIFactory.Label(body, "", 22, TextAnchor.MiddleCenter, UIFactory.Warn, true);
            UIFactory.Layout(statusText, prefH: 70);
        }

        private Slider CreateSlider(Transform parent)
        {
            var rt = UIFactory.Rect(parent, "Slider");
            UIFactory.Layout(rt, prefH: 30);
            var bg = UIFactory.Panel(rt, "Background", new Color(0f, 0f, 0f, 0.4f));
            UIFactory.Stretch(bg);
            var fillArea = UIFactory.Rect(rt, "FillArea");
            UIFactory.Stretch(fillArea, 4f);
            var fill = UIFactory.Panel(fillArea, "Fill", UIFactory.Accent);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0f, 1f); fill.offsetMin = fill.offsetMax = Vector2.zero;
            var handleArea = UIFactory.Rect(rt, "HandleArea");
            UIFactory.Stretch(handleArea, 4f);
            var handle = UIFactory.Panel(handleArea, "Handle", Color.white);
            handle.sizeDelta = new Vector2(20f, 0f);
            var s = rt.gameObject.AddComponent<Slider>();
            s.fillRect = fill;
            s.handleRect = handle;
            s.targetGraphic = handle.GetComponent<Image>();
            s.minValue = 0.3f;
            s.maxValue = 6f;
            s.onValueChanged.AddListener(val =>
            {
                var gm = GameManager.Instance;
                if (gm != null && gm.Player != null) gm.Player.MouseSensitivity = val;
                PlayerPrefs.SetFloat("ChickenShop.MouseSensitivity", val);
            });
            return s;
        }

        public override void OnOpened()
        {
            statusText.text = string.Empty;
            var gm = GameManager.Instance;
            if (gm != null && gm.Player != null) sensitivity.SetValueWithoutNotify(gm.Player.MouseSensitivity);
        }

        private void OnSave()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Save == null) return;
            string err;
            if (gm.Save.TrySave(out err))
            {
                statusText.color = UIFactory.Good;
                statusText.text = "저장했습니다.\n" + SaveManager.SavePath;
            }
            else
            {
                statusText.color = UIFactory.Bad;
                statusText.text = err;
            }
        }

        private void OnLoad()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Save == null) return;
            if (!gm.Save.HasSave)
            {
                statusText.color = UIFactory.Bad;
                statusText.text = "저장 파일이 없습니다.";
                return;
            }
            string err;
            ui.ClosePause();
            if (gm.Save.TryLoad(out err)) GameManager.Toast("불러오기 완료: " + gm.Day.Day + "일차 영업 준비", ToastKind.Success);
            else GameManager.Toast("불러오기 실패: " + err, ToastKind.Error);
        }
    }
}
