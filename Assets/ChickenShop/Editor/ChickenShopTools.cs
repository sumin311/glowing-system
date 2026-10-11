using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChickenShop.EditorTools
{
    /// <summary>Chicken Shop 메뉴의 보조 도구.</summary>
    public static class ChickenShopTools
    {
        [MenuItem("Chicken Shop/Open Main Menu Scene", false, 20)]
        private static void OpenMenuScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() && File.Exists(ChickenShopSceneBuilder.MenuScenePath))
                EditorSceneManager.OpenScene(ChickenShopSceneBuilder.MenuScenePath);
        }

        [MenuItem("Chicken Shop/Open Game Scene", false, 21)]
        private static void OpenGameScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() && File.Exists(ChickenShopSceneBuilder.GameScenePath))
                EditorSceneManager.OpenScene(ChickenShopSceneBuilder.GameScenePath);
        }

        [MenuItem("Chicken Shop/Reimport Models And Share Materials", false, 40)]
        private static void ReimportModels()
        {
            ChickenShopMaterials.ResetModelMaterials();
            int n = ChickenShopMaterials.ExtractAndShareModelMaterials();
            Debug.Log("[ChickenShop/Tools] 모델 재가져오기 + 머티리얼 공유 완료 (" + n + "개 FBX)");
        }

        [MenuItem("Chicken Shop/Run Self-Test (Play Mode)", false, 60)]
        private static void RunSelfTest()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Chicken Shop", "Play 모드에서 새 게임을 시작한 직후(영업 준비 단계)에 실행하세요.\n게임 중 F10 키로도 실행할 수 있습니다.", "확인");
                return;
            }
            var t = Object.FindObjectOfType<GameLoopSelfTest>();
            if (t == null) { Debug.LogError("[ChickenShop/Tools] GameLoopSelfTest 컴포넌트가 씬에 없습니다."); return; }
            t.Run();
        }

        [MenuItem("Chicken Shop/Open Save Folder", false, 80)]
        private static void OpenSaveFolder()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        [MenuItem("Chicken Shop/Delete Save Data", false, 81)]
        private static void DeleteSave()
        {
            string path = Path.Combine(Application.persistentDataPath, SaveManager.FileName);
            if (!File.Exists(path)) { Debug.Log("[ChickenShop/Tools] 저장 파일 없음: " + path); return; }
            if (EditorUtility.DisplayDialog("Chicken Shop", "저장 파일을 삭제할까요?\n" + path, "삭제", "취소"))
            {
                File.Delete(path);
                Debug.Log("[ChickenShop/Tools] 저장 파일 삭제: " + path);
            }
        }
    }
}
