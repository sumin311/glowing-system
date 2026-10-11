using UnityEditor;

namespace ChickenShop.EditorTools
{
    /// <summary>
    /// Assets/ChickenShop/Models 아래 FBX 의 가져오기 설정을 자동으로 맞춘다.
    ///  - Blender FBX(Apply Scalings: FBX All, -Z Forward, Y Up) 기준 1 unit = 1 m
    ///  - Bake Axis Conversion: 루트 회전 (-90,0,0) 없이 정면이 +Z 가 되도록 축 변환을 메시에 굽는다
    ///  - 노멀은 파일 값(베벨/웨이티드 노멀) 사용, 카메라/라이트/애니메이션은 가져오지 않음
    ///  - 머티리얼은 MaterialDescription 으로 가져온 뒤 ChickenShopMaterials 가 공용 .mat 으로 추출·리맵
    /// </summary>
    public class ChickenShopModelPostprocessor : AssetPostprocessor
    {
        public const string ModelsRoot = "Assets/ChickenShop/Models/";

        private void OnPreprocessModel()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(ModelsRoot)) return;
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
    }
}
