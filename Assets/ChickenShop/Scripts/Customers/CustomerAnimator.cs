using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 절차적 캐릭터 애니메이션 (리깅 대안).
    /// Blender 에서 관절 피벗으로 분리해 둔 Arm_L/Arm_R/Leg_L/Leg_R/Head 부품을 회전시켜
    /// 걷기(팔다리 교차 스윙 + 몸 상하 흔들림), 대기(숨쉬기, 고개 돌리기), 손 흔들기, 화남(고개 젓기)을 표현한다.
    /// </summary>
    public class CustomerAnimator : MonoBehaviour
    {
        [SerializeField] private float strideFrequency = 5.5f;
        [SerializeField] private float legSwing = 28f;
        [SerializeField] private float armSwing = 22f;
        [SerializeField] private float bobHeight = 0.03f;

        private Transform armL, armR, legL, legR, head, body;
        private Quaternion armLRest, armRRest, legLRest, legRRest, headRest;
        private Vector3 bodyRest;
        private float phase;
        private float moveAmount;
        private float waveTimer;
        private float angryTimer;
        private float idleSeed;

        /// <summary>현재 이동 속도 (m/s) - CustomerAgent 가 매 프레임 넣어준다.</summary>
        public float Speed { get; set; }

        private void Awake()
        {
            armL = CustomerAppearance.FindDeep(transform, "Arm_L");
            armR = CustomerAppearance.FindDeep(transform, "Arm_R");
            legL = CustomerAppearance.FindDeep(transform, "Leg_L");
            legR = CustomerAppearance.FindDeep(transform, "Leg_R");
            head = CustomerAppearance.FindDeep(transform, "Head");
            body = CustomerAppearance.FindDeep(transform, "Model");
            if (body == null && transform.childCount > 0) body = transform.GetChild(0);
            if (armL != null) armLRest = armL.localRotation;
            if (armR != null) armRRest = armR.localRotation;
            if (legL != null) legLRest = legL.localRotation;
            if (legR != null) legRRest = legR.localRotation;
            if (head != null) headRest = head.localRotation;
            if (body != null) bodyRest = body.localPosition;
            idleSeed = Random.value * 10f;
        }

        public void PlayWave() { waveTimer = 1.6f; }
        public void PlayAngry() { angryTimer = 1.6f; }

        private void Update()
        {
            float dt = Time.deltaTime;
            moveAmount = Mathf.MoveTowards(moveAmount, Mathf.Clamp01(Speed / 1.3f), dt * 4f);
            phase += dt * strideFrequency * Mathf.Max(0.2f, moveAmount);
            float s = Mathf.Sin(phase);
            float leg = s * legSwing * moveAmount;
            float arm = s * armSwing * moveAmount;

            if (legL != null) legL.localRotation = legLRest * Quaternion.Euler(leg, 0f, 0f);
            if (legR != null) legR.localRotation = legRRest * Quaternion.Euler(-leg, 0f, 0f);

            float wave = 0f;
            if (waveTimer > 0f)
            {
                waveTimer -= dt;
                wave = Mathf.Sin(Mathf.Clamp01(waveTimer / 1.6f) * Mathf.PI);
            }
            if (armL != null) armL.localRotation = armLRest * Quaternion.Euler(-arm, 0f, 0f);
            if (armR != null)
            {
                // 오른팔: 걷기 스윙 + 손 흔들기 (옆으로 들어 올림)
                armR.localRotation = armRRest * Quaternion.Euler(arm, 0f, 0f) * Quaternion.Euler(0f, 0f, wave * (140f + Mathf.Sin(Time.time * 14f) * 15f));
            }

            if (head != null)
            {
                float look = Mathf.Sin(Time.time * 0.5f + idleSeed) * 12f * (1f - moveAmount);
                float shake = 0f;
                if (angryTimer > 0f)
                {
                    angryTimer -= dt;
                    shake = Mathf.Sin(Time.time * 18f) * 18f * Mathf.Clamp01(angryTimer);
                }
                head.localRotation = headRest * Quaternion.Euler(0f, look + shake, 0f);
            }
            if (body != null)
            {
                float breathe = Mathf.Sin(Time.time * 1.8f + idleSeed) * 0.004f * (1f - moveAmount);
                body.localPosition = bodyRest + Vector3.up * (Mathf.Abs(Mathf.Sin(phase)) * bobHeight * moveAmount + breathe);
            }
        }
    }
}
