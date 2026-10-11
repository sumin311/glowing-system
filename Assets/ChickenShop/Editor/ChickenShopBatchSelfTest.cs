using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChickenShop.EditorTools
{
    /// <summary>
    /// 명령줄(batchmode)에서 Play 모드 자동 검증을 실행한다.
    ///   Unity -batchmode -projectPath . -executeMethod ChickenShop.EditorTools.ChickenShopBatchSelfTest.Run -logFile selftest.log
    /// (-quit 는 넣지 않는다. 검증이 끝나면 스스로 종료한다.)
    /// 게임 씬을 열고 Play -> 새 게임 -> GameLoopSelfTest.Run() -> 결과를 SelfTestResult.txt 에 저장 -> 종료.
    /// 종료 코드: 0 = 전부 통과, 1 = 실패/중단/시간 초과, 2 = 시작 불가.
    /// Play 모드 중 발생한 예외/오류 로그(자체 검증 FAIL 제외)도 실패로 센다.
    /// </summary>
    public static class ChickenShopBatchSelfTest
    {
        private const string ActiveKey = "ChickenShop.BatchSelfTest.Active";
        private const string ResultFile = "SelfTestResult.txt";
        private const double TimeoutSeconds = 180;
        private const double SettleSeconds = 2;

        private static double playStart = -1;
        private static bool started;
        private static int runsBefore;
        private static readonly StringBuilder runtimeErrors = new StringBuilder();
        private static int runtimeErrorCount;

        public static void Run()
        {
            if (!File.Exists(ChickenShopSceneBuilder.GameScenePath))
            {
                Debug.LogError("[ChickenShop/BatchSelfTest] 게임 씬이 없습니다. 먼저 Chicken Shop > Create Game Scene 을 실행하세요.");
                EditorApplication.Exit(2);
                return;
            }
            GameLaunchOptions.ContinueGame = false;
            SessionState.SetBool(ActiveKey, true);
            Hook(); // 도메인 리로드가 꺼져 있어도 동작하도록 (리로드되면 Hook 이 다시 등록)
            EditorSceneManager.OpenScene(ChickenShopSceneBuilder.GameScenePath);
            Debug.Log("[ChickenShop/BatchSelfTest] Play 모드 진입");
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            EditorApplication.update += Tick;
            Application.logMessageReceived += OnLog;
        }

        private static void OnLog(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (message.StartsWith("[ChickenShop/SelfTest]")) return; // 자체 검증 결과는 따로 집계
            runtimeErrorCount++;
            runtimeErrors.AppendLine("  [" + type + "] " + message);
            if (type == LogType.Exception) runtimeErrors.AppendLine(stackTrace);
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            double now = EditorApplication.timeSinceStartup;
            if (playStart < 0) playStart = now;

            if (!started)
            {
                // 씬 초기화(Start -> 새 게임)가 끝날 때까지 잠시 대기
                if (now - playStart < SettleSeconds) return;
                var test = Object.FindObjectOfType<GameLoopSelfTest>();
                if (test == null) { Finish(false, "GameLoopSelfTest 컴포넌트가 씬에 없습니다."); return; }
                runsBefore = GameLoopSelfTest.FinishedRuns;
                started = true;
                Debug.Log("[ChickenShop/BatchSelfTest] 자체 검증 시작");
                test.Run();
                return;
            }

            if (GameLoopSelfTest.FinishedRuns > runsBefore)
            {
                Finish(GameLoopSelfTest.LastRunPassed && runtimeErrorCount == 0, GameLoopSelfTest.LastReport);
                return;
            }
            if (now - playStart > TimeoutSeconds)
                Finish(false, "시간 초과 (" + TimeoutSeconds + "초). 마지막 보고:\n" + GameLoopSelfTest.LastReport);
        }

        private static void Finish(bool passed, string report)
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            SessionState.EraseBool(ActiveKey);

            var sb = new StringBuilder();
            sb.AppendLine(passed ? "결과: 통과" : "결과: 실패");
            sb.AppendLine(report);
            sb.AppendLine("Play 모드 오류 로그: " + runtimeErrorCount + "개");
            if (runtimeErrorCount > 0) sb.Append(runtimeErrors);
            string text = sb.ToString();
            try { File.WriteAllText(ResultFile, text, Encoding.UTF8); }
            catch (System.Exception ex) { Debug.LogWarning("[ChickenShop/BatchSelfTest] 결과 파일 저장 실패: " + ex.Message); }

            Debug.Log("[ChickenShop/BatchSelfTest]\n" + text);
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(passed ? 0 : 1);
        }
    }
}
