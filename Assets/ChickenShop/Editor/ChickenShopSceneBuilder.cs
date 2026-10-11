using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChickenShop.EditorTools
{
    /// <summary>
    /// 메뉴 "Chicken Shop > Create Game Scene".
    /// 프로젝트 설정 -> 모델 가져오기/머티리얼 공유 -> 프리팹 생성 -> 메인 메뉴 씬 + 매장 씬 생성 -> Build Settings 등록까지 자동으로 수행한다.
    ///
    /// 매장 좌표 (1 unit = 1 m, +Z = 매장 안쪽, -Z = 길거리)
    ///   실내 X -8~8, Z -6~6, 천장 3.2 m
    ///   홀(손님)  Z -6 ~ -0.4 : 입구(X -7~-5.6), 줄 서기(X -2.2), 대기석(Z -2.45), 테이블
    ///   카운터    Z -0.4      : X -5.2~2.8 (POS, 픽업대), 직원 통로 X 2.8~4.2
    ///   주방      Z -0.4 ~ 6  : 뒷벽 라인(냉장고, 선반, 반죽대, 튀김기 2, 싱크, 쓰레기통), 가운데 섬(소스, 포장), 왼쪽 벽 발주 단말기
    /// </summary>
    public static class ChickenShopSceneBuilder
    {
        public const string GameScenePath = ChickenShopEditorUtil.ScenesDir + "/ChickenShop.unity";
        public const string MenuScenePath = ChickenShopEditorUtil.ScenesDir + "/MainMenu.unity";

        // 환경 머티리얼과 UV 반복 크기(m)
        private static Material mFloorHall, mFloorKitchen, mPlaster, mTile, mWood, mBrick, mSidewalk, mAsphalt, mCeiling, mTrim, mLightPanel, mLane;
        private static readonly Dictionary<Material, float> uvMeters = new Dictionary<Material, float>();

        [MenuItem("Chicken Shop/Create Game Scene", false, 0)]
        public static void CreateGameSceneMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        public static void Build()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Chicken Shop", "프로젝트 설정", 0.05f);
                PrepareProject();
                EditorUtility.DisplayProgressBar("Chicken Shop", "모델 머티리얼 추출/공유", 0.2f);
                int fbx = ChickenShopMaterials.ExtractAndShareModelMaterials();
                CreateEnvironmentMaterials();
                EditorUtility.DisplayProgressBar("Chicken Shop", "프리팹 생성", 0.4f);
                var prefabs = ChickenShopPrefabs.BuildAll();
                EditorUtility.DisplayProgressBar("Chicken Shop", "메인 메뉴 씬", 0.55f);
                BuildMenuScene();
                EditorUtility.DisplayProgressBar("Chicken Shop", "매장 씬", 0.7f);
                BuildGameScene(prefabs);
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(MenuScenePath, true),
                    new EditorBuildSettingsScene(GameScenePath, true),
                };
                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
                Debug.Log("[ChickenShop/Builder] 완료! 모델 머티리얼 리맵 " + fbx + "개 FBX. 씬: " + GameScenePath + " / " + MenuScenePath +
                          "  -> Play 를 누르면 바로 플레이할 수 있습니다 (메인 메뉴부터 하려면 MainMenu 씬에서 Play).");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[ChickenShop/Builder] 씬 생성 실패: " + ex);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // ------------------------------------------------------------------ 프로젝트 준비
        private static void PrepareProject()
        {
            foreach (var d in new[] { ChickenShopEditorUtil.PrefabsDir, ChickenShopEditorUtil.MaterialsDir, ChickenShopEditorUtil.ScenesDir, ChickenShopEditorUtil.ModelsDir })
                ChickenShopEditorUtil.EnsureFolder(d);
            PlayerSettings.companyName = "ChickenShop";
            PlayerSettings.productName = "바삭치킨 시뮬레이터";
            if (PlayerSettings.colorSpace != ColorSpace.Linear) PlayerSettings.colorSpace = ColorSpace.Linear;
            QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, 8);
            QualitySettings.shadowDistance = 35f;

            // 후처리기(ChickenShopModelPostprocessor) 설정이 적용되지 않은 FBX 는 다시 가져온다
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { ChickenShopEditorUtil.ModelsDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi != null && (!mi.bakeAxisConversion || mi.importAnimation))
                {
                    mi.SaveAndReimport();
                }
            }
        }

        private static void CreateEnvironmentMaterials()
        {
            uvMeters.Clear();
            mFloorHall = Reg(ChickenShopMaterials.Env("M_Env_FloorHall", "Floor_HallTile", Color.white, 0.45f), 1.2f);
            mFloorKitchen = Reg(ChickenShopMaterials.Env("M_Env_FloorKitchen", "Floor_KitchenTile", Color.white, 0.35f), 1.2f);
            mPlaster = Reg(ChickenShopMaterials.Env("M_Env_WallPlaster", "Wall_Plaster", Color.white, 0.1f), 2f);
            mTile = Reg(ChickenShopMaterials.Env("M_Env_WallTile", "Wall_SubwayTile", Color.white, 0.75f), 1.2f);
            mWood = Reg(ChickenShopMaterials.Env("M_Env_WoodPanel", "Wood_Planks", Color.white, 0.35f), 1.2f);
            mBrick = Reg(ChickenShopMaterials.Env("M_Env_Brick", "Wall_Brick", Color.white, 0.12f), 1.6f);
            mSidewalk = Reg(ChickenShopMaterials.Env("M_Env_Sidewalk", "Ground_Sidewalk", Color.white, 0.1f), 2f);
            mAsphalt = Reg(ChickenShopMaterials.Env("M_Env_Asphalt", null, new Color(0.17f, 0.17f, 0.19f), 0.25f), 1f);
            mCeiling = Reg(ChickenShopMaterials.Env("M_Env_Ceiling", "Wall_Plaster", new Color(1f, 1f, 0.98f), 0.05f), 2f);
            mTrim = Reg(ChickenShopMaterials.Env("M_Env_Trim", null, new Color(0.3f, 0.19f, 0.11f), 0.45f), 1f);
            mLightPanel = Reg(ChickenShopMaterials.Emissive("M_Env_LightPanel", new Color(1f, 0.97f, 0.9f), 1.4f), 1f);
            mLane = Reg(ChickenShopMaterials.Env("M_Env_LaneMark", null, new Color(0.95f, 0.9f, 0.6f), 0.2f), 1f);
            AssetDatabase.SaveAssets();
        }

        private static Material Reg(Material m, float meters)
        {
            uvMeters[m] = meters;
            return m;
        }

        // ------------------------------------------------------------------ 블록/배치 도우미
        private static GameObject Block(string name, Transform parent, Vector3 center, Vector3 size, Material mat, bool collider = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            float uv;
            if (!uvMeters.TryGetValue(mat, out uv)) uv = 1f;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = ChickenShopMeshUtil.CreateBox(size, uv, name + "_Mesh");
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            if (collider) go.AddComponent<BoxCollider>();
            ChickenShopEditorUtil.SetStatic(go);
            return go;
        }

        private static GameObject Invisible(string name, Transform parent, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        /// <summary>X 방향 벽에 개구부(문/창)를 뚫어 여러 블록으로 만든다. openings: (xMin, xMax, yMin, yMax)</summary>
        private static void WallX(string name, Transform parent, float xMin, float xMax, float z, float thickness, float height, Material mat, List<Vector4> openings, float yBase = 0f)
        {
            openings.Sort((a, b) => a.x.CompareTo(b.x));
            float cursor = xMin;
            int i = 0;
            System.Action<float, float, float, float> piece = (x0, x1, y0, y1) =>
            {
                if (x1 - x0 < 0.01f || y1 - y0 < 0.01f) return;
                Block(name + "_" + (i++), parent, new Vector3((x0 + x1) * 0.5f, yBase + (y0 + y1) * 0.5f, z), new Vector3(x1 - x0, y1 - y0, thickness), mat);
            };
            foreach (var o in openings)
            {
                if (o.x > cursor) piece(cursor, o.x, 0f, height);
                if (o.z > 0f) piece(o.x, o.y, 0f, Mathf.Min(o.z, height));
                if (o.w < height) piece(o.x, o.y, o.w, height);
                cursor = Mathf.Max(cursor, o.y);
            }
            if (cursor < xMax) piece(cursor, xMax, 0f, height);
        }

        private static GameObject Place(GameObject prefab, Transform parent, Vector3 pos, float yaw, string name = null)
        {
            if (prefab == null) { Debug.LogError("[ChickenShop/Builder] 프리팹 없음: " + name); return new GameObject(name ?? "Missing"); }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            if (name != null) go.name = name;
            return go;
        }

        /// <summary>장식용 FBX 배치 + 경계 BoxCollider (가구 충돌).</summary>
        private static GameObject Decor(string modelRel, Transform parent, Vector3 pos, float yaw, bool collider = true, string name = null)
        {
            var holder = new GameObject(name ?? System.IO.Path.GetFileName(modelRel));
            holder.transform.SetParent(parent, false);
            ChickenShopPrefabs.InstantiateModel(modelRel, holder.transform);
            if (collider) ChickenShopEditorUtil.AddBoundsCollider(holder, Vector3.zero);
            holder.transform.localPosition = pos;
            holder.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            ChickenShopEditorUtil.SetStatic(holder);
            return holder;
        }

        private static Light PointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.ForcePixel;
            return l;
        }

        private static void SetupLightingSettings(string assetName)
        {
            // 라이트맵 굽기 없이 실시간 조명만 사용 (씬 생성 직후 바로 플레이 가능)
            var ls = new LightingSettings { name = assetName, bakedGI = false, realtimeGI = false };
            string path = ChickenShopEditorUtil.ScenesDir + "/" + assetName + ".lighting";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(ls, path);
            var so = new SerializedObject(ls);
            var wf = so.FindProperty("m_GIWorkflowMode"); // 1 = On Demand (자동 생성 끔)
            if (wf != null) { wf.intValue = 1; so.ApplyModifiedPropertiesWithoutUndo(); }
            Lightmapping.lightingSettings = ls;
        }

        private static void SetupRenderSettings(Light sun)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.76f, 0.72f);
            RenderSettings.ambientEquatorColor = new Color(0.58f, 0.53f, 0.47f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.31f, 0.27f);
            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            RenderSettings.sun = sun;
            RenderSettings.fog = false;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        }

        // ------------------------------------------------------------------ 매장 씬
        private static void BuildGameScene(ChickenShopPrefabs.Set p)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLightingSettings("ChickenShopLighting");

            var env = new GameObject("Environment").transform;
            var floors = new GameObject("Floors").transform; floors.SetParent(env);
            var walls = new GameObject("Walls").transform; walls.SetParent(env);
            var exterior = new GameObject("Exterior").transform; exterior.SetParent(env);
            var decor = new GameObject("Decor").transform; decor.SetParent(env);
            var lights = new GameObject("Lights").transform; lights.SetParent(env);
            var stations = new GameObject("Stations").transform;

            // ---- 바닥
            Block("Floor_Hall", floors, new Vector3(0f, -0.05f, -3.2f), new Vector3(16f, 0.1f, 5.6f), mFloorHall);
            Block("Floor_Kitchen", floors, new Vector3(0f, -0.05f, 2.8f), new Vector3(16f, 0.1f, 6.4f), mFloorKitchen);
            Block("Floor_Doorway", floors, new Vector3(-6.3f, -0.05f, -6.1f), new Vector3(1.4f, 0.1f, 0.2f), mFloorHall);

            // ---- 벽 (석고 코어 0.2 m)
            Block("Wall_Back", walls, new Vector3(0f, 1.6f, 6.1f), new Vector3(16.4f, 3.2f, 0.2f), mPlaster);
            Block("Wall_Left", walls, new Vector3(-8.1f, 1.6f, 0f), new Vector3(0.2f, 3.2f, 12.4f), mPlaster);
            Block("Wall_Right", walls, new Vector3(8.1f, 1.6f, 0f), new Vector3(0.2f, 3.2f, 12.4f), mPlaster);
            var doorOpening = new Vector4(-7.0f, -5.6f, 0f, 2.2f);
            var windows = new[] { new Vector4(-3.9f, -2.1f, 0.9f, 2.3f), new Vector4(-0.4f, 1.4f, 0.9f, 2.3f), new Vector4(3.6f, 5.4f, 0.9f, 2.3f) };
            var frontOpenings = new List<Vector4> { doorOpening };
            frontOpenings.AddRange(windows);
            WallX("Wall_Front", walls, -8.2f, 8.2f, -6.1f, 0.2f, 3.2f, mPlaster, new List<Vector4>(frontOpenings));
            WallX("Facade_Brick", walls, -8.26f, 8.26f, -6.23f, 0.06f, 3.7f, mBrick, new List<Vector4>(frontOpenings));
            // 주방 벽 타일 (높이 2.4 m)
            Block("Tile_Back", walls, new Vector3(0f, 1.2f, 5.99f), new Vector3(16f, 2.4f, 0.02f), mTile, false);
            Block("Tile_Left", walls, new Vector3(-7.99f, 1.2f, 2.8f), new Vector3(0.02f, 2.4f, 6.4f), mTile, false);
            Block("Tile_Right", walls, new Vector3(7.99f, 1.2f, 2.8f), new Vector3(0.02f, 2.4f, 6.4f), mTile, false);
            // 홀 벽 하단 원목 웨인스코팅 + 몰딩
            Block("Wainscot_Left", walls, new Vector3(-7.99f, 0.45f, -3.2f), new Vector3(0.02f, 0.9f, 5.6f), mWood, false);
            Block("Wainscot_Right", walls, new Vector3(7.99f, 0.45f, -3.2f), new Vector3(0.02f, 0.9f, 5.6f), mWood, false);
            WallX("Wainscot_Front", walls, -8f, 8f, -5.99f, 0.02f, 0.9f, mWood, new List<Vector4> { doorOpening });
            Block("Trim_Left", walls, new Vector3(-7.98f, 0.92f, -3.2f), new Vector3(0.04f, 0.05f, 5.6f), mTrim, false);
            Block("Trim_Right", walls, new Vector3(7.98f, 0.92f, -3.2f), new Vector3(0.04f, 0.05f, 5.6f), mTrim, false);
            // 홀/주방 경계: 왼쪽 벽, 카운터 위 소핏, 오른쪽 허리벽
            Block("Partition_Left", walls, new Vector3(-6.6f, 1.6f, -0.4f), new Vector3(2.8f, 3.2f, 0.2f), mPlaster);
            Block("Partition_Left_Tile", walls, new Vector3(-6.6f, 1.2f, -0.29f), new Vector3(2.8f, 2.4f, 0.02f), mTile, false);
            Block("Partition_Left_Wainscot", walls, new Vector3(-6.6f, 0.45f, -0.51f), new Vector3(2.8f, 0.9f, 0.02f), mWood, false);
            Block("Soffit", walls, new Vector3(1.4f, 2.95f, -0.4f), new Vector3(13.2f, 0.5f, 0.3f), mPlaster);
            Block("HalfWall", walls, new Vector3(6.1f, 0.55f, -0.4f), new Vector3(3.8f, 1.1f, 0.2f), mWood);
            Block("HalfWall_Cap", walls, new Vector3(6.1f, 1.125f, -0.4f), new Vector3(3.9f, 0.05f, 0.28f), mTrim);
            // 천장
            Block("Ceiling", env, new Vector3(0f, 3.25f, 0f), new Vector3(16.4f, 0.1f, 12.4f), mCeiling);

            // ---- 외부 (인도, 도로, 경계)
            Block("Sidewalk", exterior, new Vector3(0f, -0.07f, -9.2f), new Vector3(30f, 0.1f, 6f), mSidewalk);
            Block("Curb", exterior, new Vector3(0f, -0.06f, -12.35f), new Vector3(30f, 0.12f, 0.3f), mTrim);
            Block("Street", exterior, new Vector3(0f, -0.17f, -17f), new Vector3(40f, 0.1f, 9.4f), mAsphalt);
            for (int i = 0; i < 8; i++) Block("LaneMark_" + i, exterior, new Vector3(-17.5f + i * 5f, -0.115f, -17f), new Vector3(2.4f, 0.01f, 0.15f), mLane, false);
            Invisible("Bound_South", exterior, new Vector3(0f, 1.5f, -12.6f), new Vector3(30f, 3f, 0.2f));
            Invisible("Bound_West", exterior, new Vector3(-15f, 1.5f, -9.4f), new Vector3(0.2f, 3f, 6.4f));
            Invisible("Bound_East", exterior, new Vector3(15f, 1.5f, -9.4f), new Vector3(0.2f, 3f, 6.4f));
            Decor("Shop/ShopSign", exterior, new Vector3(-2.4f, 2.45f, -6.26f), 180f, false, "ShopSign");
            Decor("Shop/DoorFrame", walls, new Vector3(-6.3f, 0f, -6.1f), 180f, false, "DoorFrame");
            foreach (var w in windows) Decor("Shop/Window", walls, new Vector3((w.x + w.y) * 0.5f, 0.9f, -6.1f), 180f, false, "Window");
            Decor("Shop/Plant", exterior, new Vector3(-7.7f, 0f, -6.75f), 0f, true, "Plant_Outside1");
            Decor("Shop/Plant", exterior, new Vector3(-4.9f, 0f, -6.75f), 40f, true, "Plant_Outside2");

            // ---- 카운터 (2 m 모듈 4개)
            var counter = new GameObject("Counter").transform; counter.SetParent(env);
            for (int i = 0; i < 4; i++) Decor("Shop/Counter", counter, new Vector3(-4.2f + i * 2f, 0f, -0.4f), 180f, true, "Counter_" + i);
            var menuBoard = Decor("Shop/MenuBoard", decor, new Vector3(-1.2f, 1.95f, -0.56f), 180f, false, "MenuBoard");
            var mbLabelGo = new GameObject("MenuLabel", typeof(RectTransform));
            mbLabelGo.transform.SetParent(menuBoard.transform, false);
            mbLabelGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var mbLabel = mbLabelGo.AddComponent<WorldLabel>();
            ChickenShopEditorUtil.SetVector3(mbLabel, "offset", new Vector3(0f, 0.36f, 0.075f));
            ChickenShopEditorUtil.SetInt(mbLabel, "fontSize", 44);
            ChickenShopEditorUtil.SetFloat(mbLabel, "worldScale", 0.0026f);
            ChickenShopEditorUtil.SetBool(mbLabel, "billboard", false);
            ChickenShopEditorUtil.SetColor(mbLabel, "backgroundColor", new Color(0f, 0f, 0f, 0f));
            var mbd = menuBoard.AddComponent<MenuBoardDisplay>();
            ChickenShopEditorUtil.SetRef(mbd, "label", mbLabel);

            // ---- 주방 뒷벽 라인
            Place(p.fridge, stations, new Vector3(-6.9f, 0f, 5.55f), 180f, "IngredientFridge");
            Decor("Kitchen/StorageRack", decor, new Vector3(-5.3f, 0f, 5.72f), 180f, true, "StorageRack");
            Place(p.prep, stations, new Vector3(-3.7f, 0f, 5.62f), 180f, "PrepStation");
            var fryer1 = Place(p.fryer, stations, new Vector3(-1.95f, 0f, 5.62f), 180f, "FryerStation_1");
            ChickenShopEditorUtil.SetString(fryer1.GetComponent<FryerStation>(), "displayName", "1번 튀김기");
            var fryer2 = Place(p.fryer, stations, new Vector3(-1.25f, 0f, 5.62f), 180f, "FryerStation_2");
            var f2 = fryer2.GetComponent<FryerStation>();
            ChickenShopEditorUtil.SetString(f2, "displayName", "2번 튀김기");
            ChickenShopEditorUtil.SetBool(f2, "requiresExtraFryerUpgrade", true);
            Decor("Kitchen/ExhaustHood", decor, new Vector3(-1.6f, 1.95f, 6.0f), 180f, false, "ExhaustHood");
            Decor("Props/OilCan", decor, new Vector3(-0.7f, 0f, 5.75f), 170f, true, "OilCan");
            Decor("Kitchen/Sink", decor, new Vector3(0.45f, 0f, 5.62f), 180f, true, "Sink");
            Place(p.trash, stations, new Vector3(1.55f, 0f, 5.72f), 180f, "TrashBin");
            // 오른쪽 직원 공간
            Decor("Kitchen/StorageRack", decor, new Vector3(7.65f, 0f, 3.2f), -90f, true, "StorageRack_2");
            Decor("Props/OilCan", decor, new Vector3(7.6f, 0f, 1.7f), -80f, true, "OilCan_2");
            Decor("Props/OilCan", decor, new Vector3(7.6f, 0f, 1.35f), -95f, true, "OilCan_3");
            Decor("Shop/Table", decor, new Vector3(5.6f, 0f, 5.0f), 0f, true, "StaffTable");
            Decor("Shop/Chair", decor, new Vector3(4.95f, 0f, 5.0f), 90f, true, "StaffChair");
            // 왼쪽 벽: 발주 단말기 (재고 구매 지점)
            Place(p.terminal, stations, new Vector3(-7.55f, 0f, 2.0f), 90f, "SupplyTerminal");

            // ---- 주방 가운데 섬: 소스 작업대 + 포장 작업대
            Decor("Kitchen/Worktable", stations, new Vector3(-2.4f, 0f, 2.55f), 0f, true, "SauceTable");
            Place(p.sauceYangnyeom, stations, new Vector3(-2.75f, 0.9f, 2.43f), 0f, "SauceStation_Yangnyeom");
            Place(p.sauceGanjang, stations, new Vector3(-2.05f, 0.9f, 2.43f), 0f, "SauceStation_Ganjang");
            Place(p.packaging, stations, new Vector3(0.9f, 0f, 2.55f), 0f, "PackagingStation");

            // ---- 카운터 위: POS, 픽업대, 영업 팻말
            Place(p.pos, stations, new Vector3(-2.2f, 1.05f, -0.32f), 180f, "PosOrderPoint");
            var delivery = Place(p.delivery, stations, new Vector3(2.2f, 1.05f, -0.4f), 0f, "DeliveryCounter");
            Place(p.openSign, stations, new Vector3(-4.75f, 0f, 0.4f), 180f, "ShopOpenSign");

            // ---- 홀 장식
            var hall = new GameObject("Hall").transform; hall.SetParent(env);
            foreach (var tx in new[] { 2.4f, 5.6f })
            {
                Decor("Shop/Table", hall, new Vector3(tx, 0f, -4.4f), 0f, true, "Table");
                Decor("Shop/Chair", hall, new Vector3(tx - 0.62f, 0f, -4.4f), 90f, true, "Chair");
                Decor("Shop/Chair", hall, new Vector3(tx + 0.62f, 0f, -4.4f), -90f, true, "Chair");
            }
            Decor("Shop/Plant", hall, new Vector3(7.4f, 0f, -5.5f), 0f, true, "Plant_Hall1");
            Decor("Shop/Plant", hall, new Vector3(-7.4f, 0f, -1.2f), 120f, true, "Plant_Hall2");
            Decor("Shop/WallShelf", hall, new Vector3(7.98f, 1.3f, -3.0f), -90f, false, "WallShelf_Right");
            Decor("Shop/WallShelf", hall, new Vector3(-7.98f, 1.3f, -3.6f), 90f, false, "WallShelf_Left");

            // ---- 조명
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(lights, false);
            sunGo.transform.rotation = Quaternion.Euler(38f, 20f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.86f);
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            foreach (var lp in new[] { new Vector3(-5.5f, 3.18f, 3.2f), new Vector3(-1.8f, 3.18f, 4.1f), new Vector3(1.6f, 3.18f, 2.8f), new Vector3(5.2f, 3.18f, 3.2f) })
            {
                Block("LightPanel", lights, lp, new Vector3(1.2f, 0.04f, 0.6f), mLightPanel, false);
                PointLight(lights, "KitchenLight", lp + Vector3.down * 0.35f, new Color(1f, 0.97f, 0.92f), 1.15f, 7.5f);
            }
            foreach (var hp in new[] { new Vector3(-2.2f, 3.2f, -2.6f), new Vector3(1.0f, 3.2f, -2.6f), new Vector3(2.4f, 3.2f, -4.4f), new Vector3(5.6f, 3.2f, -4.4f) })
            {
                var lamp = Decor("Shop/PendantLamp", lights, hp, 0f, false, "PendantLamp");
                var bulb = ChickenShopEditorUtil.FindDeep(lamp.transform, "Bulb");
                Vector3 lightPos = bulb != null ? bulb.position - Vector3.up * 0.1f : hp + Vector3.down * 0.95f;
                PointLight(lights, "PendantLight", lightPos, new Color(1f, 0.86f, 0.62f), 0.95f, 5.5f);
            }
            var probeGo = new GameObject("ReflectionProbe");
            probeGo.transform.SetParent(lights, false);
            probeGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.size = new Vector3(16f, 3.3f, 12f);
            probe.boxProjection = true;
            probe.resolution = 128;
            SetupRenderSettings(sun);

            // ---- 손님 경로
            var custRoot = new GameObject("Customers");
            var spawner = custRoot.AddComponent<CustomerSpawner>();
            var queueGo = ChickenShopEditorUtil.Child(custRoot, "Queue", Vector3.zero);
            var queue = queueGo.AddComponent<CustomerQueue>();
            var slots = new Object[4];
            for (int i = 0; i < 4; i++) slots[i] = ChickenShopEditorUtil.Child(queueGo, "Slot" + i, new Vector3(-2.2f, 0f, -1.3f - i * 0.9f)).transform;
            ChickenShopEditorUtil.SetRefArray(queue, "slots", slots);
            var waitGo = ChickenShopEditorUtil.Child(custRoot, "WaitingArea", Vector3.zero);
            var waiting = waitGo.AddComponent<WaitingArea>();
            var spots = new Object[7];
            for (int i = 0; i < 7; i++) spots[i] = ChickenShopEditorUtil.Child(waitGo, "Spot" + i, new Vector3(0.3f + i * 1.05f, 0f, -2.45f)).transform;
            ChickenShopEditorUtil.SetRefArray(waiting, "spots", spots);
            var wp = ChickenShopEditorUtil.Child(custRoot, "Waypoints", Vector3.zero);
            var spawnPoint = ChickenShopEditorUtil.Child(wp, "SpawnPoint", new Vector3(-12f, 0f, -7.8f));
            spawnPoint.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var doorOutside = ChickenShopEditorUtil.Child(wp, "DoorOutside", new Vector3(-6.3f, 0f, -7.0f));
            var doorInside = ChickenShopEditorUtil.Child(wp, "DoorInside", new Vector3(-6.3f, 0f, -5.25f));
            var queueEntry = ChickenShopEditorUtil.Child(wp, "QueueEntry", new Vector3(-2.2f, 0f, -4.8f));
            var pickupSpot = ChickenShopEditorUtil.Child(wp, "PickupSpot", new Vector3(2.2f, 0f, -1.3f));
            ChickenShopEditorUtil.SetRef(delivery.GetComponent<DeliveryCounter>(), "pickupSpot", pickupSpot.transform);
            ChickenShopEditorUtil.SetRef(spawner, "customerPrefab", p.customer != null ? p.customer.GetComponent<CustomerAgent>() : null);
            ChickenShopEditorUtil.SetRef(spawner, "queue", queue);
            ChickenShopEditorUtil.SetRef(spawner, "waitingArea", waiting);
            ChickenShopEditorUtil.SetRef(spawner, "deliveryCounter", delivery.GetComponent<DeliveryCounter>());
            ChickenShopEditorUtil.SetRef(spawner, "spawnPoint", spawnPoint.transform);
            ChickenShopEditorUtil.SetRef(spawner, "doorOutside", doorOutside.transform);
            ChickenShopEditorUtil.SetRef(spawner, "doorInside", doorInside.transform);
            ChickenShopEditorUtil.SetRef(spawner, "queueEntry", queueEntry.transform);

            // ---- 플레이어
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(-3.5f, 0.02f, 1.5f);
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 45f;
            cc.skinWidth = 0.03f;
            var camGo = ChickenShopEditorUtil.Child(player, "CameraPivot", new Vector3(0f, 1.62f, 0f));
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            camGo.AddComponent<AudioListener>();
            var hold = ChickenShopEditorUtil.Child(camGo, "HoldAnchor", new Vector3(0.3f, -0.34f, 0.6f));
            var pc = player.AddComponent<PlayerController>();
            ChickenShopEditorUtil.SetRef(pc, "cameraPivot", camGo.transform);
            var carry = player.AddComponent<PlayerCarry>();
            ChickenShopEditorUtil.SetRef(carry, "holdAnchor", hold.transform);
            var interactor = player.AddComponent<PlayerInteractor>();
            ChickenShopEditorUtil.SetRef(interactor, "viewCamera", cam);
            ChickenShopEditorUtil.SetRef(interactor, "carry", carry);

            // ---- 게임 시스템
            var sys = new GameObject("GameSystems");
            var gm = sys.AddComponent<GameManager>();
            var day = sys.AddComponent<DayCycleManager>();
            var orders = sys.AddComponent<OrderManager>();
            var inv = sys.AddComponent<InventoryManager>();
            var eco = sys.AddComponent<EconomyManager>();
            var rep = sys.AddComponent<ReputationManager>();
            var prog = sys.AddComponent<ShopProgression>();
            var menu = sys.AddComponent<MenuCatalog>();
            var save = sys.AddComponent<SaveManager>();
            var input = sys.AddComponent<InputModeManager>();
            var items = sys.AddComponent<ItemFactory>();
            var testRunner = sys.AddComponent<GameLoopSelfTest>();
            ChickenShopEditorUtil.SetRef(items, "rawPrefab", Item(p, "Chicken_Raw"));
            ChickenShopEditorUtil.SetRef(items, "batteredPrefab", Item(p, "Chicken_Battered"));
            ChickenShopEditorUtil.SetRef(items, "friedPrefab", Item(p, "Chicken_Fried"));
            ChickenShopEditorUtil.SetRef(items, "yangnyeomPrefab", Item(p, "Chicken_Yangnyeom"));
            ChickenShopEditorUtil.SetRef(items, "ganjangPrefab", Item(p, "Chicken_Ganjang"));
            ChickenShopEditorUtil.SetRef(items, "burntPrefab", Item(p, "Chicken_Burnt"));
            ChickenShopEditorUtil.SetRef(items, "boxPrefab", Item(p, "ChickenBox"));
            ChickenShopEditorUtil.SetRef(items, "drinkPrefab", Item(p, "ColaBottle"));
            ChickenShopEditorUtil.SetRef(items, "bagPrefab", Item(p, "PaperBag"));

            // ---- UI
            var canvasGo = new GameObject("UICanvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var ui = canvasGo.AddComponent<UIManager>();
            ChickenShopEditorUtil.SetRef(ui, "canvas", canvas);
            ChickenShopEditorUtil.SetRef(ui, "interactor", interactor);
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();

            // ---- GameManager 참조 연결
            ChickenShopEditorUtil.SetRef(gm, "dayCycle", day);
            ChickenShopEditorUtil.SetRef(gm, "orders", orders);
            ChickenShopEditorUtil.SetRef(gm, "inventory", inv);
            ChickenShopEditorUtil.SetRef(gm, "economy", eco);
            ChickenShopEditorUtil.SetRef(gm, "reputation", rep);
            ChickenShopEditorUtil.SetRef(gm, "progression", prog);
            ChickenShopEditorUtil.SetRef(gm, "menuCatalog", menu);
            ChickenShopEditorUtil.SetRef(gm, "saveManager", save);
            ChickenShopEditorUtil.SetRef(gm, "customers", spawner);
            ChickenShopEditorUtil.SetRef(gm, "inputMode", input);
            ChickenShopEditorUtil.SetRef(gm, "items", items);
            ChickenShopEditorUtil.SetRef(gm, "ui", ui);
            ChickenShopEditorUtil.SetRef(gm, "player", pc);
            ChickenShopEditorUtil.SetRef(gm, "playerCarry", carry);
            ChickenShopEditorUtil.SetRef(testRunner, "interactor", interactor);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, GameScenePath)) Debug.LogError("[ChickenShop/Builder] 씬 저장 실패: " + GameScenePath);
        }

        private static GameObject Item(ChickenShopPrefabs.Set p, string key)
        {
            GameObject go;
            if (p.items.TryGetValue(key, out go)) return go;
            Debug.LogWarning("[ChickenShop/Builder] 아이템 프리팹 없음: " + key);
            return null;
        }

        // ------------------------------------------------------------------ 메인 메뉴 씬
        private static void BuildMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLightingSettings("MainMenuLighting");
            var set = new GameObject("Set").transform;
            Block("Floor", set, new Vector3(0f, -0.05f, 0f), new Vector3(12f, 0.1f, 12f), mFloorKitchen);
            Block("BackWall", set, new Vector3(0f, 1.6f, 2.2f), new Vector3(12f, 3.2f, 0.2f), mTile, false);
            Decor("Kitchen/Fryer", set, new Vector3(-0.9f, 0f, 1.5f), 180f, false);
            Decor("Kitchen/Fryer", set, new Vector3(-0.2f, 0f, 1.5f), 180f, false);
            Decor("Kitchen/ExhaustHood", set, new Vector3(-0.55f, 1.95f, 2.1f), 180f, false);
            Decor("Kitchen/Worktable", set, new Vector3(1.4f, 0f, 1.45f), 180f, false);
            Decor("Food/Chicken_Fried", set, new Vector3(1.05f, 0.9f, 1.4f), 160f, false);
            Decor("Food/Chicken_Yangnyeom", set, new Vector3(1.45f, 0.9f, 1.3f), 200f, false);
            Decor("Props/ChickenBox", set, new Vector3(1.85f, 0.9f, 1.5f), 190f, false);
            Decor("Props/ColaBottle", set, new Vector3(1.75f, 1.01f, 1.4f), 0f, false);
            Decor("Kitchen/SauceTub_Yangnyeom", set, new Vector3(2.05f, 0f, 1.0f), 200f, false);
            Decor("Shop/ShopSign", set, new Vector3(0.4f, 2.05f, 2.1f), 180f, false);
            Decor("Shop/OpenSign", set, new Vector3(-1.9f, 0f, 0.6f), 160f, false);
            Decor("Shop/PendantLamp", set, new Vector3(1.2f, 3.0f, 0.6f), 0f, false);

            var sunGo = new GameObject("Sun");
            sunGo.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.0f;
            sun.color = new Color(1f, 0.93f, 0.85f);
            sun.shadows = LightShadows.Soft;
            PointLight(set, "Warm", new Vector3(1.2f, 2.0f, 0.4f), new Color(1f, 0.82f, 0.55f), 1.4f, 6f);
            PointLight(set, "Fill", new Vector3(-1.5f, 2.2f, -1.0f), new Color(0.9f, 0.95f, 1f), 0.6f, 7f);
            SetupRenderSettings(sun);

            var target = new GameObject("OrbitTarget");
            target.transform.position = new Vector3(0.5f, 1.0f, 1.2f);
            var camGo = new GameObject("MenuCamera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(-0.6f, 1.75f, -2.6f);
            camGo.transform.LookAt(target.transform);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            camGo.AddComponent<AudioListener>();

            var canvasGo = new GameObject("MenuCanvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            var ctrl = new GameObject("MainMenu").AddComponent<MainMenuController>();
            ChickenShopEditorUtil.SetRef(ctrl, "canvas", canvas);
            ChickenShopEditorUtil.SetRef(ctrl, "menuCamera", cam);
            ChickenShopEditorUtil.SetRef(ctrl, "cameraOrbitTarget", target.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, MenuScenePath)) Debug.LogError("[ChickenShop/Builder] 씬 저장 실패: " + MenuScenePath);
        }
    }
}
