using System.Collections.Generic;
using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 치킨 아이템 생성과 단계별 3D 모델(프리팹) 관리.
    /// 모델 프리팹은 에디터 스크립트가 Blender FBX 로 만든 Prefabs/Items/*.prefab 을 연결한다.
    /// 프리팹이 비어 있으면 기본 도형 조합으로 대체 모델을 만들어 게임이 멈추지 않도록 한다.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class ItemFactory : MonoBehaviour
    {
        [Header("치킨 단계별 모델 (쟁반 위 치킨)")]
        [SerializeField] private GameObject rawPrefab;
        [SerializeField] private GameObject batteredPrefab;
        [SerializeField] private GameObject friedPrefab;
        [SerializeField] private GameObject yangnyeomPrefab;
        [SerializeField] private GameObject ganjangPrefab;
        [SerializeField] private GameObject burntPrefab;

        [Header("포장")]
        [SerializeField] private GameObject boxPrefab;
        [SerializeField] private GameObject drinkPrefab;
        [SerializeField] private GameObject bagPrefab;

        private readonly Dictionary<int, ChickenItem> live = new Dictionary<int, ChickenItem>();
        private readonly Dictionary<string, Material> fallbackMats = new Dictionary<string, Material>();
        private int nextId = 1;

        public int LiveCount { get { return live.Count; } }

        public static string VisualKey(ChickenItem item)
        {
            if (item.IsPacked) return "packed";
            switch (item.Stage)
            {
                case ChickenStage.Raw: return "raw";
                case ChickenStage.Battered:
                case ChickenStage.Frying: return "battered";
                case ChickenStage.Burnt: return "burnt";
                default:
                    if (item.Sauce == SauceType.Yangnyeom) return "yangnyeom";
                    if (item.Sauce == SauceType.Ganjang) return "ganjang";
                    return "fried";
            }
        }

        /// <summary>새 생닭 아이템 생성 (냉장고에서 꺼낼 때).</summary>
        public ChickenItem CreateChicken()
        {
            var go = new GameObject("Chicken");
            var item = go.AddComponent<ChickenItem>();
            int id = nextId++;
            go.name = "Chicken#" + id;
            item.Init(id, this);
            live[id] = item;
            return item;
        }

        public ChickenItem Find(int itemId)
        {
            ChickenItem item;
            return live.TryGetValue(itemId, out item) && item != null ? item : null;
        }

        public void Unregister(ChickenItem item)
        {
            if (item != null) live.Remove(item.ItemId);
        }

        public void DestroyAllItems()
        {
            var list = new List<ChickenItem>(live.Values);
            foreach (var item in list) if (item != null) item.Consume();
            live.Clear();
        }

        /// <summary>단계에 맞는 모델을 parent 아래에 생성한다.</summary>
        public GameObject CreateVisual(string key, bool withDrink, Transform parent)
        {
            if (key == "packed")
            {
                var root = new GameObject("Packed").transform;
                root.SetParent(parent, false);
                Spawn(boxPrefab, "box", root, Vector3.zero);
                if (withDrink) Spawn(drinkPrefab, "drink", root, new Vector3(0.08f, 0.11f, 0.05f)).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                return root.gameObject;
            }
            GameObject prefab;
            switch (key)
            {
                case "raw": prefab = rawPrefab; break;
                case "battered": prefab = batteredPrefab; break;
                case "yangnyeom": prefab = yangnyeomPrefab; break;
                case "ganjang": prefab = ganjangPrefab; break;
                case "burnt": prefab = burntPrefab; break;
                default: prefab = friedPrefab; break;
            }
            return Spawn(prefab, key, parent, Vector3.zero);
        }

        /// <summary>손님이 들고 나가는 종이봉투 모델.</summary>
        public GameObject CreateBagVisual(Transform parent)
        {
            return Spawn(bagPrefab, "bag", parent, Vector3.zero);
        }

        private GameObject Spawn(GameObject prefab, string key, Transform parent, Vector3 localPos)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, parent, false);
            }
            else
            {
                go = CreateFallback(key);
                go.transform.SetParent(parent, false);
            }
            go.transform.localPosition = localPos;
            // 운반 아이템은 충돌체가 필요 없다 (레이캐스트를 가리지 않게)
            var cols = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++) Destroy(cols[i]);
            return go;
        }

        // ------------------------------------------------------------------ 대체 모델
        private GameObject CreateFallback(string key)
        {
            GameLog.Warn("Items", "'" + key + "' 모델 프리팹이 연결되지 않아 기본 도형을 사용합니다.", this);
            var root = new GameObject("Fallback_" + key);
            if (key == "box")
            {
                AddPrim(root, PrimitiveType.Cube, new Vector3(0, 0.05f, 0), new Vector3(0.3f, 0.1f, 0.3f), new Color(0.95f, 0.95f, 0.92f));
                AddPrim(root, PrimitiveType.Cube, new Vector3(0, 0.051f, 0), new Vector3(0.08f, 0.102f, 0.302f), new Color(0.8f, 0.12f, 0.08f));
                return root;
            }
            if (key == "drink")
            {
                AddPrim(root, PrimitiveType.Cylinder, new Vector3(0, 0.15f, 0), new Vector3(0.09f, 0.15f, 0.09f), new Color(0.2f, 0.07f, 0.03f));
                return root;
            }
            if (key == "bag")
            {
                AddPrim(root, PrimitiveType.Cube, new Vector3(0, 0.18f, 0), new Vector3(0.34f, 0.36f, 0.2f), new Color(0.7f, 0.52f, 0.33f));
                return root;
            }
            Color c;
            switch (key)
            {
                case "raw": c = new Color(0.95f, 0.7f, 0.62f); break;
                case "battered": c = new Color(0.96f, 0.9f, 0.76f); break;
                case "yangnyeom": c = new Color(0.66f, 0.08f, 0.03f); break;
                case "ganjang": c = new Color(0.36f, 0.17f, 0.05f); break;
                case "burnt": c = new Color(0.12f, 0.07f, 0.04f); break;
                default: c = new Color(0.8f, 0.47f, 0.13f); break;
            }
            AddPrim(root, PrimitiveType.Cube, new Vector3(0, 0.01f, 0), new Vector3(0.3f, 0.02f, 0.22f), new Color(0.75f, 0.76f, 0.78f));
            for (int i = 0; i < 6; i++)
            {
                float a = i * 1.05f;
                AddPrim(root, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 0.07f, 0.045f + (i % 2) * 0.02f, Mathf.Sin(a) * 0.05f), new Vector3(0.08f, 0.06f, 0.1f), c);
            }
            return root;
        }

        private void AddPrim(GameObject root, PrimitiveType type, Vector3 pos, Vector3 scale, Color color)
        {
            var p = GameObject.CreatePrimitive(type);
            Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(root.transform, false);
            p.transform.localPosition = pos;
            p.transform.localScale = scale;
            string k = ColorUtility.ToHtmlStringRGB(color);
            Material m;
            if (!fallbackMats.TryGetValue(k, out m))
            {
                var shader = Shader.Find("Standard");
                m = new Material(shader != null ? shader : Shader.Find("Unlit/Color")) { color = color };
                fallbackMats[k] = m;
            }
            p.GetComponent<Renderer>().sharedMaterial = m;
        }
    }
}
