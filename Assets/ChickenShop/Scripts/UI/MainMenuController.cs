using System;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChickenShop
{
    /// <summary>메인 메뉴: 새 게임 / 이어하기 / 조작법 / 종료.</summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Transform cameraOrbitTarget;
        [SerializeField] private Camera menuCamera;

        private RectTransform helpBox;
        private Text continueInfo;
        private Vector3 cameraOffset;

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (canvas == null)
            {
                var go = new GameObject("MenuCanvas", typeof(RectTransform));
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                go.AddComponent<GraphicRaycaster>();
            }
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            BuildUI();
            if (menuCamera != null && cameraOrbitTarget != null) cameraOffset = menuCamera.transform.position - cameraOrbitTarget.position;
        }

        private void BuildUI()
        {
            var root = (RectTransform)canvas.transform;
            var side = UIFactory.Panel(root, "Side", new Color(0.12f, 0.07f, 0.05f, 0.82f));
            UIFactory.Anchor(side, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(80f, 0f), new Vector2(620f, 0f));
            var v = UIFactory.Vertical(side.gameObject, 18f, 40, TextAnchor.MiddleCenter);
            v.childForceExpandHeight = false;

            var title = UIFactory.Label(side, "바삭치킨", 96, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.35f), true);
            UIFactory.Layout(title, prefH: 130);
            var sub = UIFactory.Label(side, "1인칭 치킨집 운영 시뮬레이터", 30, TextAnchor.MiddleCenter, UIFactory.TextMain);
            UIFactory.Layout(sub, prefH: 60);

            bool hasSave = SaveManager.SaveExists();
            var cont = UIFactory.Button(side, "이어하기", OnContinue, UIFactory.Accent, 34);
            cont.interactable = hasSave;
            continueInfo = UIFactory.Label(side, hasSave ? ReadSaveSummary() : "저장된 게임이 없습니다", 22, TextAnchor.MiddleCenter, UIFactory.TextDim);
            UIFactory.Layout(continueInfo, prefH: 34);
            UIFactory.Button(side, "새 게임", OnNewGame, hasSave ? UIFactory.PanelLight : UIFactory.Accent, 34);
            UIFactory.Button(side, "조작법", () => helpBox.gameObject.SetActive(!helpBox.gameObject.activeSelf), UIFactory.PanelLight, 30);
            UIFactory.Button(side, "게임 종료", OnQuit, UIFactory.AccentDark, 30);

            helpBox = UIFactory.Panel(root, "Help", UIFactory.PanelColor);
            UIFactory.Anchor(helpBox, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-80f, 0f), new Vector2(1000f, 760f));
            var ht = UIFactory.Label(helpBox, HelpPanel.ControlsText + "\n" + HelpPanel.FlowText, 24, TextAnchor.UpperLeft, UIFactory.TextMain);
            UIFactory.Stretch((RectTransform)ht.transform, 26f);
            helpBox.gameObject.SetActive(false);

            var credit = UIFactory.Label(root, "Unity 2022.3 · 3D 모델: Blender Python 스크립트로 제작 · 폰트: 나눔스퀘어라운드 (SIL OFL)", 18, TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.6f));
            UIFactory.Anchor((RectTransform)credit.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 14f), new Vector2(1200f, 30f));
        }

        private static string ReadSaveSummary()
        {
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SaveManager.SavePath));
                if (data == null) return "저장 파일 확인 불가";
                return data.day + "일차 · " + MoneyFormat.Won(data.money) + " · 평판 " + data.reputation.ToString("0") + "  (" + data.savedAt + ")";
            }
            catch (Exception ex)
            {
                GameLog.Warn("MainMenu", "저장 요약 읽기 실패: " + ex.Message);
                return "저장 파일 확인 불가";
            }
        }

        private void Update()
        {
            // 배경 카메라를 좌우로 천천히 흔든다 (±18도)
            if (menuCamera != null && cameraOrbitTarget != null)
            {
                float yaw = Mathf.Sin(Time.time * 0.15f) * 18f;
                menuCamera.transform.position = cameraOrbitTarget.position + Quaternion.Euler(0f, yaw, 0f) * cameraOffset;
                menuCamera.transform.LookAt(cameraOrbitTarget);
            }
        }

        private void OnNewGame()
        {
            GameLaunchOptions.ContinueGame = false;
            LoadGame();
        }

        private void OnContinue()
        {
            GameLaunchOptions.ContinueGame = true;
            LoadGame();
        }

        private static void LoadGame()
        {
            if (!Application.CanStreamedLevelBeLoaded(GameLaunchOptions.GameScene))
            {
                GameLog.Error("MainMenu", "게임 씬 '" + GameLaunchOptions.GameScene + "' 이 Build Settings 에 없습니다. Chicken Shop > Create Game Scene 을 실행하세요.");
                return;
            }
            SceneManager.LoadScene(GameLaunchOptions.GameScene);
        }

        private static void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
