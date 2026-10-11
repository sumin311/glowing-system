using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ChickenShop.EditorTools
{
    /// <summary>
    /// Assets/ChickenShop/Models 아래 FBX 의 가져오기 설정을 자동으로 맞춘다.
    ///  - Blender FBX(Apply Scalings: FBX All, -Z Forward, Y Up) 기준 1 unit = 1 m
    ///  - Blender 내보내기가 루트에 남기는 회전(90,0,0)과 정면 방향(-Z)을 메시/자식 위치에 굽는다
    ///    -> 모든 노드 회전 (0,0,0), 위 = +Y, 정면 = +Z (OnPostprocessModel)
    ///  - 노멀은 파일 값(베벨/웨이티드 노멀) 사용, 카메라/라이트/애니메이션은 가져오지 않음
    ///  - 머티리얼은 MaterialDescription 으로 가져온 뒤 ChickenShopMaterials 가 공용 .mat 으로 추출·리맵
    /// </summary>
    public class ChickenShopModelPostprocessor : AssetPostprocessor
    {
        public const string ModelsRoot = "Assets/ChickenShop/Models/";

        // 버전을 올리면 Unity 가 모델을 다시 가져온다.
        public override uint GetVersion() { return 2; }

        private bool InModels { get { return assetPath.Replace('\\', '/').StartsWith(ModelsRoot); } }

        private void OnPreprocessModel()
        {
            if (!InModels) return;
            var mi = assetImporter as ModelImporter;
            if (mi == null) return;
            mi.globalScale = 1f;
            mi.useFileScale = true;
            mi.bakeAxisConversion = true;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importVisibility = false;
            mi.importBlendShapes = false;
            mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.importNormals = ModelImporterNormals.Import;
            mi.importTangents = ModelImporterTangents.CalculateMikk;
            mi.isReadable = false;
            mi.addCollider = false; // Collider 는 프리팹 단계에서 단순 BoxCollider 로 추가
            mi.meshCompression = ModelImporterMeshCompression.Off;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }

        private void OnPostprocessModel(GameObject root)
        {
            if (!InModels) return;
            // 가져온 그대로면 Blender 정면(-Y)이 Unity -Z 를 향하므로 Y 180도를 더해 +Z 정면으로 맞춘다.
            Quaternion fix = Quaternion.Euler(0f, 180f, 0f) * root.transform.localRotation;
            if (Quaternion.Angle(fix, Quaternion.identity) < 0.01f) return;
            Bake(root.transform, fix, new HashSet<Mesh>());
            root.transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// t 의 로컬 좌표계 -> 새 루트 좌표계 회전(toRoot)을 메시에 적용하고,
        /// 자식들의 위치를 새 좌표계로 옮긴 뒤 회전을 0 으로 만든다. (균일 스케일 가정)
        /// </summary>
        private static void Bake(Transform t, Quaternion toRoot, HashSet<Mesh> done)
        {
            var mf = t.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null && done.Add(mf.sharedMesh)) RotateMesh(mf.sharedMesh, toRoot);
            foreach (Transform c in t)
            {
                Quaternion childToRoot = toRoot * c.localRotation;
                c.localPosition = toRoot * c.localPosition;
                c.localRotation = Quaternion.identity;
                Bake(c, childToRoot, done);
            }
        }

        private static void RotateMesh(Mesh mesh, Quaternion q)
        {
            var v = mesh.vertices;
            for (int i = 0; i < v.Length; i++) v[i] = q * v[i];
            mesh.vertices = v;
            var n = mesh.normals;
            if (n != null && n.Length == v.Length)
            {
                for (int i = 0; i < n.Length; i++) n[i] = q * n[i];
                mesh.normals = n;
            }
            var tg = mesh.tangents;
            if (tg != null && tg.Length == v.Length)
            {
                for (int i = 0; i < tg.Length; i++)
                {
                    Vector3 d = q * new Vector3(tg[i].x, tg[i].y, tg[i].z);
                    tg[i] = new Vector4(d.x, d.y, d.z, tg[i].w);
                }
                mesh.tangents = tg;
            }
            mesh.RecalculateBounds();
        }
    }
}
