using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ChickenShop.EditorTools
{
    /// <summary>
    /// Blender FBX 모델 + 게임 컴포넌트를 조합해 프리팹을 만든다.
    ///  Prefabs/Items     : 치킨 단계별 모델, 상자, 콜라, 봉투 (ItemFactory 가 사용)
    ///  Prefabs/Stations  : 튀김기, 반죽대, 소스통, 포장대, 냉장고, 발주 단말기, 쓰레기통, 픽업대, POS, 영업 팻말
    ///  Prefabs/Customer  : 손님 (모델 + AI + 외형 무작위 + 절차적 애니메이션)
    /// 각 프리팹은 루트에 컴포넌트/Collider, 자식 "Model" 에 FBX 인스턴스를 둔다 → 모델만 교체하기 쉽다.
    /// </summary>
    public static class ChickenShopPrefabs
    {
        public const string ItemsDir = ChickenShopEditorUtil.PrefabsDir + "/Items";
        public const string StationsDir = ChickenShopEditorUtil.PrefabsDir + "/Stations";

        public class Set
        {
            public readonly Dictionary<string, GameObject> items = new Dictionary<string, GameObject>();
            public GameObject customer;
            public GameObject fryer, prep, sauceYangnyeom, sauceGanjang, packaging, fridge, terminal, trash, delivery, pos, openSign;
        }

        // ------------------------------------------------------------------ 모델 로드
        public static GameObject LoadModel(string rel)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(ChickenShopEditorUtil.ModelsDir + "/" + rel + ".fbx");
        }

        /// <summary>FBX 를 프리팹 인스턴스로 배치 (없으면 대체 도형). 이름은 "Model".</summary>
        public static GameObject InstantiateModel(string rel, Transform parent, string instanceName = "Model")
        {
            var asset = LoadModel(rel);
            GameObject go;
            if (asset != null) go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            else go = ChickenShopFallbackModels.Create(rel);
            go.name = instanceName;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            // 모델에 딸려온 Collider 제거 (충돌체는 프리팹 루트에서 관리)
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            return go;
        }

        private static GameObject Save(GameObject temp, string dir, string name)
        {
            ChickenShopEditorUtil.EnsureFolder(dir);
            string path = dir + "/" + name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            if (prefab == null) Debug.LogError("[ChickenShop/Builder] 프리팹 저장 실패: " + path);
            return prefab;
        }

        public static Set BuildAll()
        {
            var set = new Set();
            BuildItems(set);
            set.customer = BuildCustomer();
            BuildStations(set);
            AssetDatabase.SaveAssets();
            return set;
        }

        // ------------------------------------------------------------------ 아이템
        private static void BuildItems(Set set)
        {
            string[] food = { "Raw", "Battered", "Fried", "Yangnyeom", "Ganjang", "Burnt" };
            foreach (var f in food)
            {
                var root = new GameObject("Chicken_" + f);
                InstantiateModel("Food/Chicken_" + f, root.transform);
                set.items["Chicken_" + f] = Save(root, ItemsDir, "Chicken_" + f);
            }
            foreach (var p in new[] { "ChickenBox", "ColaBottle", "PaperBag" })
            {
                var root = new GameObject(p);
                InstantiateModel("Props/" + p, root.transform);
                set.items[p] = Save(root, ItemsDir, p);
            }
        }

        // ------------------------------------------------------------------ 손님
        private static GameObject BuildCustomer()
        {
            var root = new GameObject("Customer");
            InstantiateModel("Characters/Customer", root.transform);
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.height = 1.8f;
            col.radius = 0.28f;
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var appearance = root.AddComponent<CustomerAppearance>();
            var anim = root.AddComponent<CustomerAnimator>();
            var agent = root.AddComponent<CustomerAgent>();
            ChickenShopEditorUtil.SetString(agent, "displayName", "손님");
            ChickenShopEditorUtil.SetRef(agent, "animator3D", anim);
            ChickenShopEditorUtil.SetRef(agent, "appearance", appearance);
            var hand = ChickenShopEditorUtil.FindDeep(root.transform, "Arm_R");
            if (hand != null)
            {
                var anchor = ChickenShopEditorUtil.Child(hand.gameObject, "HandAnchor", new Vector3(0f, -0.88f, 0.02f));
                ChickenShopEditorUtil.SetRef(agent, "handAnchor", anchor.transform);
            }
            return Save(root, ChickenShopEditorUtil.PrefabsDir, "Customer");
        }

        // ------------------------------------------------------------------ 작업대
        private static T Station<T>(out GameObject root, string name, string modelRel, string displayName) where T : Interactable
        {
            root = new GameObject(name);
            if (!string.IsNullOrEmpty(modelRel)) InstantiateModel(modelRel, root.transform);
            var comp = root.AddComponent<T>();
            ChickenShopEditorUtil.SetString(comp, "displayName", displayName);
            return comp;
        }

        private static WorldLabel Label(GameObject parent, string text, Vector3 localPos, int size = 40, float scale = 0.003f)
        {
            var go = new GameObject("NameLabel", typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            var l = go.AddComponent<WorldLabel>();
            ChickenShopEditorUtil.SetVector3(l, "offset", localPos);
            ChickenShopEditorUtil.SetInt(l, "fontSize", size);
            ChickenShopEditorUtil.SetFloat(l, "worldScale", scale);
            ChickenShopEditorUtil.SetString(l, "initialText", text);
            return l;
        }

        private static WorldProgressBar Bar(GameObject parent, Vector3 localPos)
        {
            var go = new GameObject("ProgressBar", typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            return go.AddComponent<WorldProgressBar>();
        }

        private static Transform Anchor(GameObject parent, Vector3 localPos)
        {
            return ChickenShopEditorUtil.Child(parent, "ItemAnchor", localPos).transform;
        }

        private static void Prop(GameObject parent, string modelRel, Vector3 pos, float yaw = 0f, float scale = 1f)
        {
            var p = InstantiateModel(modelRel, parent.transform, System.IO.Path.GetFileName(modelRel));
            p.transform.localPosition = pos;
            p.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            p.transform.localScale = Vector3.one * scale;
        }

        private static void BuildStations(Set set)
        {
            GameObject root;

            // 튀김기
            var fryer = Station<FryerStation>(out root, "FryerStation", "Kitchen/Fryer", "튀김기");
            ChickenShopEditorUtil.AddBoundsCollider(root, new Vector3(0.02f, 0.02f, 0.02f));
            var basket = ChickenShopEditorUtil.FindDeep(root.transform, "Basket");
            var fAnchor = Anchor(root, new Vector3(0f, 0.97f, -0.03f));
            ChickenShopEditorUtil.SetRef(fryer, "itemAnchor", fAnchor);
            ChickenShopEditorUtil.SetRef(fryer, "basket", basket);
            ChickenShopEditorUtil.SetRef(fryer, "progressBar", Bar(root, new Vector3(0f, 1.55f, 0.1f)));
            ChickenShopEditorUtil.SetRef(fryer, "bubbles", CreateBubbles(root, new Vector3(0f, 0.87f, -0.03f)));
            ChickenShopEditorUtil.SetRef(fryer, "statusLabel", Label(root, string.Empty, new Vector3(0f, 1.72f, 0.1f), 40, 0.0028f));
            set.fryer = Save(root, StationsDir, "FryerStation");

            // 반죽 작업대
            var prep = Station<PrepStation>(out root, "PrepStation", "Kitchen/Worktable", "반죽 작업대");
            Prop(root, "Props/BatterBowl", new Vector3(0.38f, 0.9f, -0.08f));
            Prop(root, "Props/Tongs", new Vector3(0.05f, 0.9f, -0.2f), 70f);
            ChickenShopEditorUtil.AddBoundsCollider(root, new Vector3(0f, 0.1f, 0f));
            ChickenShopEditorUtil.SetRef(prep, "itemAnchor", Anchor(root, new Vector3(-0.25f, 0.9f, 0.05f)));
            ChickenShopEditorUtil.SetRef(prep, "progressBar", Bar(root, new Vector3(-0.25f, 1.3f, 0.05f)));
            ChickenShopEditorUtil.SetFloat(prep, "workSeconds", 3f);
            Label(root, "반죽 작업대", new Vector3(0f, 1.55f, 0f));
            set.prep = Save(root, StationsDir, "PrepStation");

            // 소스통 2종 (조리대 위에 올려 사용)
            set.sauceYangnyeom = BuildSauce("SauceStation_Yangnyeom", "Kitchen/SauceTub_Yangnyeom", "양념 소스통", SauceType.Yangnyeom);
            set.sauceGanjang = BuildSauce("SauceStation_Ganjang", "Kitchen/SauceTub_Ganjang", "간장 소스통", SauceType.Ganjang);

            // 포장 작업대
            var pack = Station<PackagingStation>(out root, "PackagingStation", "Kitchen/Worktable", "포장 작업대");
            for (int i = 0; i < 4; i++) Prop(root, "Props/ChickenBox", new Vector3(0.45f, 0.9f + i * 0.105f, -0.12f), i * 3f);
            Prop(root, "Props/PaperBag", new Vector3(-0.5f, 0.9f, -0.15f), 10f);
            for (int i = 0; i < 3; i++) Prop(root, "Props/ColaBottle", new Vector3(0.08f + i * 0.1f, 0.9f, -0.25f));
            ChickenShopEditorUtil.AddBoundsCollider(root, new Vector3(0f, 0.05f, 0f));
            ChickenShopEditorUtil.SetRef(pack, "itemAnchor", Anchor(root, new Vector3(-0.08f, 0.9f, 0.1f)));
            ChickenShopEditorUtil.SetRef(pack, "progressBar", Bar(root, new Vector3(-0.08f, 1.35f, 0.1f)));
            ChickenShopEditorUtil.SetFloat(pack, "workSeconds", 3f);
            Label(root, "포장 작업대", new Vector3(0f, 1.6f, 0f));
            set.packaging = Save(root, StationsDir, "PackagingStation");

            // 냉장고
            Station<IngredientFridge>(out root, "IngredientFridge", "Kitchen/Fridge", "냉장고");
            ChickenShopEditorUtil.AddBoundsCollider(root, Vector3.zero);
            Label(root, "냉장고 (생닭)", new Vector3(0f, 2.2f, 0.2f));
            set.fridge = Save(root, StationsDir, "IngredientFridge");

            // 발주 단말기
            Station<SupplyTerminal>(out root, "SupplyTerminal", "Kitchen/OrderTerminal", "발주 단말기");
            ChickenShopEditorUtil.AddBoundsCollider(root, new Vector3(0.1f, 0f, 0.1f));
            Label(root, "발주 단말기", new Vector3(0f, 1.62f, 0f));
            set.terminal = Save(root, StationsDir, "SupplyTerminal");

            // 쓰레기통
            Station<TrashBin>(out root, "TrashBin", "Props/TrashBin", "쓰레기통");
            ChickenShopEditorUtil.AddBoundsCollider(root, new Vector3(0.05f, 0.1f, 0.05f));
            Label(root, "쓰레기통", new Vector3(0f, 0.95f, 0f), 36, 0.0025f);
            set.trash = Save(root, StationsDir, "TrashBin");

            // 픽업대 (카운터 위 쟁반 + 종)
            var delivery = Station<DeliveryCounter>(out root, "DeliveryCounter", null, "픽업대");
            var tray = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(tray.GetComponent<Collider>());
            tray.name = "Tray";
            tray.transform.SetParent(root.transform, false);
            tray.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            tray.transform.localScale = new Vector3(0.6f, 0.024f, 0.45f);
            var stainless = AssetDatabase.LoadAssetAtPath<Material>(ChickenShopMaterials.ModelMatDir + "/M_Stainless.mat");
            tray.GetComponent<Renderer>().sharedMaterial = stainless != null ? stainless : ChickenShopMaterials.Solid("Steel", new Color(0.72f, 0.74f, 0.76f), 0.7f, 0.85f);
            var bell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(bell.GetComponent<Collider>());
            bell.name = "Bell";
            bell.transform.SetParent(root.transform, false);
            bell.transform.localPosition = new Vector3(0.22f, 0.04f, 0.12f);
            bell.transform.localScale = new Vector3(0.08f, 0.05f, 0.08f);
            bell.GetComponent<Renderer>().sharedMaterial = ChickenShopMaterials.Solid("Gold", new Color(0.95f, 0.75f, 0.3f), 0.8f, 0.9f);
            var dc = root.AddComponent<BoxCollider>();
            dc.center = new Vector3(0f, 0.18f, 0f);
            dc.size = new Vector3(0.7f, 0.36f, 0.6f);
            Label(root, "픽업대", new Vector3(0f, 0.5f, 0f), 40, 0.003f);
            set.delivery = Save(root, StationsDir, "DeliveryCounter");

            // POS (주문 받기)
            Station<OrderTakingPoint>(out root, "PosOrderPoint", "Shop/PosRegister", "POS");
            ChickenShopEditorUtil.AddBoundsCollider(root, new Vector3(0.1f, 0.05f, 0.1f));
            Label(root, "주문 POS", new Vector3(0f, 0.65f, 0f), 34, 0.0025f);
            set.pos = Save(root, StationsDir, "PosOrderPoint");

            // 영업 팻말
            var sign = Station<ShopOpenSign>(out root, "ShopOpenSign", "Shop/OpenSign", "영업 팻말");
            ChickenShopEditorUtil.AddBoundsCollider(root, new Vector3(0.15f, 0.05f, 0.15f));
            ChickenShopEditorUtil.SetRef(sign, "board", ChickenShopEditorUtil.FindDeep(root.transform, "Board"));
            ChickenShopEditorUtil.SetRef(sign, "label", Label(root, string.Empty, new Vector3(0f, 1.55f, 0f), 40, 0.003f));
            set.openSign = Save(root, StationsDir, "ShopOpenSign");
        }

        private static GameObject BuildSauce(string name, string modelRel, string displayName, SauceType sauce)
        {
            GameObject root;
            var st = Station<SauceStation>(out root, name, modelRel, displayName);
            ChickenShopEditorUtil.SetInt(st, "sauceType", (int)sauce);
            ChickenShopEditorUtil.SetFloat(st, "workSeconds", 2.5f);
            // 소스통 + 앞쪽 치킨 놓는 자리까지 덮는 충돌체
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.25f, 0.14f);
            col.size = new Vector3(0.5f, 0.5f, 0.6f);
            ChickenShopEditorUtil.SetRef(st, "itemAnchor", Anchor(root, new Vector3(0f, 0f, 0.28f)));
            ChickenShopEditorUtil.SetRef(st, "progressBar", Bar(root, new Vector3(0f, 0.62f, 0.2f)));
            Label(root, displayName, new Vector3(0f, 0.8f, 0f), 36, 0.0025f);
            return Save(root, StationsDir, name);
        }

        private static ParticleSystem CreateBubbles(GameObject parent, Vector3 localPos)
        {
            var go = new GameObject("OilBubbles");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
            main.startColor = new Color(1f, 0.95f, 0.75f, 0.7f);
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.gravityModifier = -0.05f;
            var emission = ps.emission;
            emission.rateOverTime = 45f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.38f, 0.38f, 0.01f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = ChickenShopMaterials.Particle("M_FX_OilBubble", new Color(1f, 0.95f, 0.8f, 0.7f));
            return ps;
        }
    }
}
