using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChickenShop.EditorTools
{
    /// <summary>에디터 자동화 공용 도우미: 폴더 생성, 직렬화 필드 연결, Collider 계산 등.</summary>
    public static class ChickenShopEditorUtil
    {
        public const string Root = "Assets/ChickenShop";
        public const string ModelsDir = Root + "/Models";
        public const string PrefabsDir = Root + "/Prefabs";
        public const string MaterialsDir = Root + "/Materials";
        public const string ScenesDir = Root + "/Scenes";
        public const string TexturesDir = Root + "/Textures";

        public static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ------------------------------------------------------------------ 직렬화 필드 설정 (private [SerializeField] 연결)
        public static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError("[ChickenShop/Builder] 필드 없음: " + target.GetType().Name + "." + field, target); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError("[ChickenShop/Builder] 필드 없음: " + target.GetType().Name + "." + field, target); return; }
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError("[ChickenShop/Builder] 필드 없음: " + target.GetType().Name + "." + field, target); return; }
            if (p.propertyType == SerializedPropertyType.Enum) p.enumValueIndex = value;
            else p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError("[ChickenShop/Builder] 필드 없음: " + target.GetType().Name + "." + field, target); return; }
            p.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError("[ChickenShop/Builder] 필드 없음: " + target.GetType().Name + "." + field, target); return; }
            p.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetColor(Object target, string field, Color value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError("[ChickenShop/Builder] 필드 없음: " + target.GetType().Name + "." + field, target); return; }
            p.colorValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetVector3(Object target, string field, Vector3 value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError("[ChickenShop/Builder] 필드 없음: " + target.GetType().Name + "." + field, target); return; }
            p.vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetRefArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null || !p.isArray) { Debug.LogError("[ChickenShop/Builder] 배열 필드 없음: " + target.GetType().Name + "." + field, target); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ 계층/Collider
        public static GameObject Child(GameObject parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            return go;
        }

        public static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindDeep(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        /// <summary>root 아래 모든 Renderer 의 경계를 root 로컬 공간 기준으로 계산.</summary>
        public static Bounds LocalBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var inv = root.transform.worldToLocalMatrix;
            bool has = false;
            var b = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;
                var wb = r.bounds;
                var c = wb.center;
                var e = wb.extents;
                for (int i = 0; i < 8; i++)
                {
                    var corner = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                    var lp = inv.MultiplyPoint3x4(corner);
                    if (!has) { b = new Bounds(lp, Vector3.zero); has = true; }
                    else b.Encapsulate(lp);
                }
            }
            if (!has) b = new Bounds(new Vector3(0f, 0.5f, 0f), Vector3.one);
            return b;
        }

        /// <summary>모델 경계에 맞는 BoxCollider 를 추가 (상호작용/충돌용 단순 충돌체).</summary>
        public static BoxCollider AddBoundsCollider(GameObject root, Vector3 padding, bool isTrigger = false)
        {
            var b = LocalBounds(root);
            var col = root.AddComponent<BoxCollider>();
            col.center = b.center;
            col.size = b.size + padding;
            col.isTrigger = isTrigger;
            return col;
        }

        public static void SetStatic(GameObject go)
        {
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
            foreach (Transform c in go.transform) SetStatic(c.gameObject);
        }
    }
}
