using System.Text;
using UnityEditor;
using UnityEngine;

namespace ChickenShop.EditorTools
{
    /// <summary>
    /// 개발용 진단 (batchmode -executeMethod 로 실행).
    ///  Run   : FBX 들의 가져온 크기/회전/자식 위치 -> diag.txt
    ///  Shots : 게임 씬을 플레이어 시점 등에서 찍어 Shots/*.png 로 저장
    /// </summary>
    public static class ChickenShopModelDiag
    {
        public static void Run()
        {
            var sb = new StringBuilder("[ChickenShop/Diag]\n");
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/ChickenShop/Models" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var inst = Object.Instantiate(go);
                var b = new Bounds(inst.transform.position, Vector3.zero);
                bool any = false;
                foreach (var r in inst.GetComponentsInChildren<Renderer>()) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                sb.AppendFormat("{0}  rootRot={1} size={2} center={3}\n", System.IO.Path.GetFileNameWithoutExtension(path), inst.transform.localEulerAngles, b.size.ToString("F2"), b.center.ToString("F2"));
                foreach (Transform c in inst.transform)
                    sb.AppendFormat("    child {0} rot={1} pos={2}\n", c.name, c.localEulerAngles, c.localPosition.ToString("F2"));
                Object.DestroyImmediate(inst);
            }
            System.IO.File.WriteAllText("diag.txt", sb.ToString(), Encoding.UTF8);
        }

        /// <summary>게임 씬을 열어 플레이어 카메라 + 몇 개 시점에서 PNG 로 찍는다.</summary>
        public static void Shots()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ChickenShopSceneBuilder.GameScenePath);
            var cam = Camera.main;
            System.IO.Directory.CreateDirectory("Shots");
            Shot(cam, "Shots/player.png");
            var tmp = new GameObject("ShotCam").AddComponent<Camera>();
            tmp.CopyFrom(cam);
            tmp.fieldOfView = 70f;
            Place(tmp, cam.transform.position, cam.transform.eulerAngles.y + 180f, "Shots/player_back.png");
            Place(tmp, cam.transform.position, cam.transform.eulerAngles.y + 90f, "Shots/player_right.png");
            Place(tmp, cam.transform.position, cam.transform.eulerAngles.y - 90f, "Shots/player_left.png");
            tmp.transform.position = new Vector3(0f, 14f, 0f);
            tmp.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Shot(tmp, "Shots/top.png");
            Object.DestroyImmediate(tmp.gameObject);
        }

        private static void Place(Camera c, Vector3 pos, float yaw, string file)
        {
            c.transform.position = pos;
            c.transform.rotation = Quaternion.Euler(10f, yaw, 0f);
            Shot(c, file);
        }

        private static void Shot(Camera c, string file)
        {
            var rt = new RenderTexture(1280, 720, 24);
            c.targetTexture = rt;
            c.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            System.IO.File.WriteAllBytes(file, tex.EncodeToPNG());
            c.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }
    }
}
