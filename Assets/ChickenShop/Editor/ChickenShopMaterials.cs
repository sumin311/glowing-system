using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChickenShop.EditorTools
{
    /// <summary>
    /// 머티리얼 관리.
    ///  1) 환경(바닥/벽/천장) 머티리얼: Textures/*.png 로 Materials/Environment/*.mat 생성
    ///  2) 모델 머티리얼: FBX 에 포함된 M_* 머티리얼을 Materials/Models/M_*.mat 으로 추출하고,
    ///     모든 FBX 가 같은 이름의 .mat 하나를 공유하도록 리맵한다 (예: M_Stainless 는 프로젝트에 1개).
    ///  3) 재질감 보정: 금속/광택/유리(반투명)/발광 설정
    /// </summary>
    public static class ChickenShopMaterials
    {
        public const string EnvDir = ChickenShopEditorUtil.MaterialsDir + "/Environment";
        public const string ModelMatDir = ChickenShopEditorUtil.MaterialsDir + "/Models";
        public const string FxDir = ChickenShopEditorUtil.MaterialsDir + "/FX";

        private static Shader Standard { get { return Shader.Find("Standard"); } }

        // ------------------------------------------------------------------ 환경 머티리얼
        public static Material Env(string name, string texture, Color tint, float smoothness, float metallic = 0f)
        {
            ChickenShopEditorUtil.EnsureFolder(EnvDir);
            string path = EnvDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Standard);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = Standard;
            mat.color = tint;
            mat.SetFloat("_Glossiness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            if (!string.IsNullOrEmpty(texture))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ChickenShopEditorUtil.TexturesDir + "/" + texture + ".png");
                if (tex == null) Debug.LogWarning("[ChickenShop/Materials] 텍스처 없음: " + texture + " (Blender/scripts/make_textures.py 실행)");
                mat.mainTexture = tex;
            }
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material Emissive(string name, Color color, float intensity)
        {
            var mat = Env(name, null, color, 0.2f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material Particle(string name, Color color)
        {
            ChickenShopEditorUtil.EnsureFolder(FxDir);
            string path = FxDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.color = color;
            if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 2f); // Fade
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ 모델 머티리얼 추출/공유
        /// <summary>
        /// 리맵을 모두 해제하고 추출된 모델 머티리얼을 지운 뒤 FBX 를 다시 가져온다.
        /// 색 공간(Gamma -> Linear) 이 바뀐 경우 FBX 색상 변환을 다시 적용하기 위해 사용한다.
        /// </summary>
        public static void ResetModelMaterials()
        {
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { ChickenShopEditorUtil.ModelsDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;
                foreach (var kv in importer.GetExternalObjectMap())
                {
                    if (kv.Key.type == typeof(Material)) importer.RemoveRemap(kv.Key);
                }
                importer.SaveAndReimport();
            }
            foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { ModelMatDir }))
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(g));
            AssetDatabase.SaveAssets();
            Debug.Log("[ChickenShop/Materials] 모델 머티리얼 초기화 (재추출 예정)");
        }

        /// <summary>모든 FBX 의 내장 머티리얼을 공용 .mat 으로 추출하고 리맵한다. 처리한 FBX 수 반환.</summary>
        public static int ExtractAndShareModelMaterials()
        {
            ChickenShopEditorUtil.EnsureFolder(ModelMatDir);
            var guids = AssetDatabase.FindAssets("t:Model", new[] { ChickenShopEditorUtil.ModelsDir });
            int count = 0;
            // 경로 -> 공용 머티리얼 (이번 실행에서 만든 것 포함, LoadAssetAtPath 재조회에 의존하지 않음)
            var shared = new Dictionary<string, Material>();
            var toReimport = new List<string>();
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;
                bool changed = false;
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    var mat = obj as Material;
                    if (mat == null) continue; // 이미 리맵된 머티리얼은 서브에셋으로 나오지 않는다
                    string target = ModelMatDir + "/" + mat.name + ".mat";
                    Material external;
                    if (!shared.TryGetValue(target, out external))
                    {
                        external = AssetDatabase.LoadAssetAtPath<Material>(target);
                        if (external == null)
                        {
                            external = new Material(mat) { name = mat.name };
                            AssetDatabase.CreateAsset(external, target);
                        }
                        shared[target] = external;
                    }
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), mat.name), external);
                    changed = true;
                }
                if (changed)
                {
                    toReimport.Add(path);
                    count++;
                }
            }
            AssetDatabase.SaveAssets();
            // 리맵 적용 (재가져오기)
            foreach (var p in toReimport)
            {
                var importer = AssetImporter.GetAtPath(p) as ModelImporter;
                if (importer != null) importer.SaveAndReimport();
            }
            TuneModelMaterials();
            return count;
        }

        /// <summary>이름 규칙에 따라 재질감(금속/광택/유리/발광) 보정.</summary>
        public static void TuneModelMaterials()
        {
            var guids = AssetDatabase.FindAssets("t:Material", new[] { ModelMatDir });
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;
                if (mat.shader == null || mat.shader.name != "Standard") mat.shader = Standard;
                string n = mat.name;
                float metallic = 0f, smooth = 0.35f;
                if (n.StartsWith("M_Stainless")) { metallic = 0.85f; smooth = n.Contains("Dark") ? 0.55f : 0.7f; }
                else if (n == "M_Oil" || n == "M_Cola" || n.StartsWith("M_Sauce") || n == "M_ChickenYangnyeom" || n == "M_ChickenGanjang") smooth = 0.82f;
                else if (n == "M_Screen" || n == "M_CounterTop" || n == "M_Tile") smooth = 0.75f;
                else if (n.Contains("Plastic") || n == "M_YellowPaint" || n == "M_RedPlastic" || n.StartsWith("M_Crate") || n == "M_SignBoard") smooth = 0.5f;
                else if (n.StartsWith("M_Wood") || n == "M_ChickenFried" || n == "M_ChickenRaw") smooth = 0.3f;
                else if (n == "M_Fabric" || n == "M_Kraft" || n == "M_Sack" || n == "M_Flour" || n == "M_ChickenBatter" || n == "M_ChickenBurnt") smooth = 0.08f;
                mat.SetFloat("_Metallic", metallic);
                mat.SetFloat("_Glossiness", smooth);
                if (n == "M_Glass") MakeTransparent(mat, 0.28f);
                if (n == "M_LightBulb" || n == "M_Screen")
                {
                    mat.EnableKeyword("_EMISSION");
                    var c = n == "M_Screen" ? new Color(0.15f, 0.3f, 0.5f) : new Color(1f, 0.85f, 0.6f) * 1.6f;
                    mat.SetColor("_EmissionColor", c);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();
        }

        public static void MakeTransparent(Material mat, float alpha)
        {
            mat.SetFloat("_Mode", 3f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)BlendMode.One);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)RenderQueue.Transparent;
            var c = mat.color;
            c.a = alpha;
            mat.color = c;
            mat.SetFloat("_Glossiness", 0.95f);
        }

        /// <summary>대체 도형용 단색 머티리얼 (FBX 가 없을 때).</summary>
        public static Material Solid(string name, Color color, float smooth = 0.4f, float metallic = 0f)
        {
            return Env("Fallback_" + name, null, color, smooth, metallic);
        }
    }
}
