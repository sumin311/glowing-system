using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 손님 외형 무작위화: 하나의 베이스 모델(Customer.fbx)에서 머리 모양/모자/안경/가방을 켜고 끄며,
    /// MaterialPropertyBlock 으로 피부·옷·머리색을 바꾼다 (머티리얼 복제 없음 - 드로우콜/메모리 절약).
    /// </summary>
    public class CustomerAppearance : MonoBehaviour
    {
        [SerializeField] private Color[] skinTones =
        {
            new Color(0.98f, 0.82f, 0.7f), new Color(0.93f, 0.76f, 0.62f), new Color(0.85f, 0.65f, 0.5f), new Color(0.7f, 0.5f, 0.36f),
        };
        [SerializeField] private Color[] shirtColors =
        {
            new Color(0.25f, 0.45f, 0.8f), new Color(0.85f, 0.3f, 0.25f), new Color(0.3f, 0.65f, 0.4f), new Color(0.95f, 0.8f, 0.3f),
            new Color(0.6f, 0.4f, 0.75f), new Color(0.95f, 0.95f, 0.92f), new Color(0.2f, 0.2f, 0.22f), new Color(0.95f, 0.6f, 0.7f),
        };
        [SerializeField] private Color[] pantsColors =
        {
            new Color(0.2f, 0.22f, 0.3f), new Color(0.15f, 0.25f, 0.45f), new Color(0.45f, 0.38f, 0.3f), new Color(0.12f, 0.12f, 0.12f), new Color(0.6f, 0.58f, 0.52f),
        };
        [SerializeField] private Color[] hairColors =
        {
            new Color(0.08f, 0.06f, 0.05f), new Color(0.25f, 0.15f, 0.08f), new Color(0.45f, 0.3f, 0.15f), new Color(0.6f, 0.6f, 0.6f),
        };
        [SerializeField] private Color[] accessoryColors =
        {
            new Color(0.85f, 0.25f, 0.2f), new Color(0.2f, 0.4f, 0.85f), new Color(0.95f, 0.75f, 0.2f), new Color(0.2f, 0.6f, 0.35f),
        };

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly string[] HairVariants = { "Hair_Short", "Hair_Long", "Hair_Bun" };

        public void Randomize(int seed)
        {
            var rng = new System.Random(seed);
            // 머리 모양 하나만 켜기
            int hair = rng.Next(HairVariants.Length);
            for (int i = 0; i < HairVariants.Length; i++) SetActive(HairVariants[i], i == hair);
            bool cap = rng.NextDouble() < 0.3 && hair != 2;
            SetActive("Cap", cap);
            SetActive("Glasses", rng.NextDouble() < 0.3);
            SetActive("Backpack", rng.NextDouble() < 0.35);

            Color skin = Pick(skinTones, rng), shirt = Pick(shirtColors, rng), pants = Pick(pantsColors, rng);
            Color hairC = Pick(hairColors, rng), acc = Pick(accessoryColors, rng);
            var block = new MaterialPropertyBlock();
            var renderers = GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                var mats = renderers[r].sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] == null) continue;
                    string n = mats[m].name;
                    Color c;
                    if (n.StartsWith("M_Skin")) c = skin;
                    else if (n.StartsWith("M_Shirt")) c = shirt;
                    else if (n.StartsWith("M_Pants")) c = pants;
                    else if (n.StartsWith("M_Hair")) c = hairC;
                    else if (n.StartsWith("M_Accessory")) c = acc;
                    else continue;
                    block.Clear();
                    block.SetColor(ColorId, c);
                    renderers[r].SetPropertyBlock(block, m);
                }
            }
        }

        private static Color Pick(Color[] arr, System.Random rng)
        {
            return arr == null || arr.Length == 0 ? Color.white : arr[rng.Next(arr.Length)];
        }

        private void SetActive(string childName, bool active)
        {
            var t = FindDeep(transform, childName);
            if (t != null) t.gameObject.SetActive(active);
        }

        public static Transform FindDeep(Transform root, string childName)
        {
            if (root.name == childName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindDeep(root.GetChild(i), childName);
                if (r != null) return r;
            }
            return null;
        }
    }
}
