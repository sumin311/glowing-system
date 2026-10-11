using UnityEngine;

namespace ChickenShop.EditorTools
{
    /// <summary>
    /// FBX 모델이 없을 때 쓰는 대체 모델 (기본 도형 + 단색). 이름 있는 부품(Basket, Board 등)도 만들어
    /// 게임 로직이 그대로 동작하게 한다. Blender 스크립트로 FBX 를 생성하면 자동으로 실제 모델로 교체된다.
    /// </summary>
    public static class ChickenShopFallbackModels
    {
        public static GameObject Create(string modelRel)
        {
            Debug.LogWarning("[ChickenShop/Builder] 모델 없음 -> 대체 도형 사용: " + modelRel + ".fbx  (Blender/scripts/build_all.py 실행 후 다시 생성하세요)");
            string name = System.IO.Path.GetFileName(modelRel);
            var root = new GameObject(name);
            Vector3 size; Color color;
            Lookup(name, out size, out color);
            if (name == "PendantLamp")
            {
                Box(root, "Shade", new Vector3(0f, -0.85f, 0f), new Vector3(0.35f, 0.1f, 0.35f), color);
                var bulb = new GameObject("Bulb");
                bulb.transform.SetParent(root.transform, false);
                bulb.transform.localPosition = new Vector3(0f, -0.86f, 0f);
                return root;
            }
            if (name == "ExhaustHood")
            {
                Box(root, "Hood", new Vector3(0f, 0.25f, 0.45f), size, color);
                return root;
            }
            Box(root, "Body", new Vector3(0f, size.y * 0.5f, 0f), size, color);
            if (name == "Fryer")
            {
                var basket = Box(root, "Basket", new Vector3(0f, 0.96f, -0.03f), new Vector3(0.34f, 0.17f, 0.34f), new Color(0.35f, 0.35f, 0.38f));
                basket.transform.GetChild(0).localPosition = new Vector3(0f, 0.085f, 0f);
            }
            if (name == "OpenSign")
            {
                Box(root, "Board", new Vector3(0f, 1.15f, 0f), new Vector3(0.5f, 0.34f, 0.04f), new Color(0.2f, 0.55f, 0.3f));
            }
            return root;
        }

        private static GameObject Box(GameObject parent, string name, Vector3 pos, Vector3 size, Color color)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent.transform, false);
            holder.transform.localPosition = pos;
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.name = name + "_Mesh";
            cube.transform.SetParent(holder.transform, false);
            cube.transform.localScale = size;
            cube.GetComponent<Renderer>().sharedMaterial = ChickenShopMaterials.Solid(ColorUtility.ToHtmlStringRGB(color), color);
            return holder;
        }

        private static void Lookup(string name, out Vector3 size, out Color color)
        {
            Color steel = new Color(0.72f, 0.74f, 0.76f), wood = new Color(0.6f, 0.42f, 0.26f), white = new Color(0.93f, 0.92f, 0.9f);
            switch (name)
            {
                case "Fryer": size = new Vector3(0.6f, 0.92f, 0.75f); color = steel; break;
                case "Worktable": size = new Vector3(1.4f, 0.9f, 0.7f); color = steel; break;
                case "Fridge": size = new Vector3(1.2f, 2f, 0.75f); color = steel; break;
                case "Sink": size = new Vector3(1f, 0.9f, 0.7f); color = steel; break;
                case "SauceTub_Yangnyeom": size = new Vector3(0.26f, 0.43f, 0.26f); color = new Color(0.75f, 0.12f, 0.05f); break;
                case "SauceTub_Ganjang": size = new Vector3(0.26f, 0.43f, 0.26f); color = new Color(0.35f, 0.17f, 0.06f); break;
                case "StorageRack": size = new Vector3(1.2f, 1.7f, 0.5f); color = steel; break;
                case "ExhaustHood": size = new Vector3(2f, 0.5f, 0.9f); color = steel; break;
                case "OrderTerminal": size = new Vector3(0.5f, 1.35f, 0.36f); color = white; break;
                case "Counter": size = new Vector3(2f, 1.05f, 0.7f); color = wood; break;
                case "Table": size = new Vector3(0.8f, 0.75f, 0.8f); color = wood; break;
                case "Chair": size = new Vector3(0.42f, 0.9f, 0.42f); color = wood; break;
                case "ShopSign": size = new Vector3(3.2f, 0.9f, 0.16f); color = new Color(0.85f, 0.12f, 0.08f); break;
                case "OpenSign": size = new Vector3(0.3f, 0.98f, 0.3f); color = new Color(0.2f, 0.2f, 0.2f); break;
                case "WallShelf": size = new Vector3(1.2f, 0.45f, 0.25f); color = wood; break;
                case "MenuBoard": size = new Vector3(2.4f, 0.8f, 0.06f); color = new Color(0.2f, 0.22f, 0.3f); break;
                case "Window": size = new Vector3(1.8f, 1.4f, 0.1f); color = new Color(0.75f, 0.88f, 0.92f); break;
                case "DoorFrame": size = new Vector3(0.1f, 2.3f, 0.2f); color = wood; break;
                case "Plant": size = new Vector3(0.5f, 1.1f, 0.5f); color = new Color(0.2f, 0.5f, 0.2f); break;
                case "PosRegister": size = new Vector3(0.42f, 0.46f, 0.42f); color = new Color(0.1f, 0.1f, 0.1f); break;
                case "ChickenBox": size = new Vector3(0.3f, 0.11f, 0.3f); color = white; break;
                case "PaperBag": size = new Vector3(0.34f, 0.4f, 0.2f); color = new Color(0.7f, 0.52f, 0.33f); break;
                case "ColaBottle": size = new Vector3(0.09f, 0.31f, 0.09f); color = new Color(0.2f, 0.07f, 0.03f); break;
                case "OilCan": size = new Vector3(0.24f, 0.36f, 0.24f); color = new Color(0.95f, 0.75f, 0.2f); break;
                case "TrashBin": size = new Vector3(0.4f, 0.69f, 0.4f); color = new Color(0.4f, 0.4f, 0.42f); break;
                case "BatterBowl": size = new Vector3(0.4f, 0.12f, 0.4f); color = white; break;
                case "Tongs": size = new Vector3(0.08f, 0.02f, 0.3f); color = steel; break;
                case "Customer": size = new Vector3(0.45f, 1.72f, 0.3f); color = new Color(0.3f, 0.45f, 0.8f); break;
                default:
                    if (name.StartsWith("Chicken_")) { size = new Vector3(0.3f, 0.1f, 0.22f); color = new Color(0.8f, 0.5f, 0.15f); }
                    else { size = new Vector3(0.5f, 0.5f, 0.5f); color = Color.gray; }
                    break;
            }
        }
    }
}
