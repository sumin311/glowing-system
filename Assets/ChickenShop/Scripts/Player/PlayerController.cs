using UnityEngine;

namespace ChickenShop
{
    /// <summary>
    /// 1인칭 이동/시점 (CharacterController 기반 - 벽·가구와 충돌).
    /// WASD 이동, Shift 달리기, Space 점프, 마우스 시점. UI 모드에서는 시점/이동이 멈춘다.
    /// 입력: Unity 기본 Input Manager (Mouse X / Mouse Y 축, KeyCode).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private Transform cameraPivot;

        [Header("이동")]
        [SerializeField] private float walkSpeed = 3.2f;
        [SerializeField] private float runSpeed = 5.6f;
        [SerializeField] private float acceleration = 14f;
        [SerializeField] private float jumpHeight = 0.9f;
        [SerializeField] private float gravity = -19.6f;

        [Header("시점")]
        [SerializeField] private float mouseSensitivity = 2.2f;
        [SerializeField, Range(30f, 89f)] private float pitchLimit = 85f;
        [SerializeField] private bool invertY;

        private CharacterController controller;
        private Vector3 planarVelocity;
        private float verticalVelocity;
        private float pitch;
        private InputModeManager inputMode;

        public Transform CameraPivot { get { return cameraPivot; } }
        public float MouseSensitivity { get { return mouseSensitivity; } set { mouseSensitivity = Mathf.Clamp(value, 0.1f, 10f); } }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (cameraPivot == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraPivot = cam.transform;
                else GameLog.Error("Player", "카메라 피벗이 없습니다.", this);
            }
        }

        private void Start()
        {
            inputMode = GameManager.Instance != null ? GameManager.Instance.InputMode : null;
            mouseSensitivity = PlayerPrefs.GetFloat("ChickenShop.MouseSensitivity", mouseSensitivity);
            if (cameraPivot != null) pitch = NormalizeAngle(cameraPivot.localEulerAngles.x);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            bool canLook = inputMode == null || inputMode.LookEnabled;
            bool canMove = inputMode == null || inputMode.MoveEnabled;
            if (canLook) Look();
            Move(canMove);
        }

        private void Look()
        {
            float mx = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float my = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * (invertY ? 1f : -1f);
            transform.Rotate(0f, mx, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch + my, -pitchLimit, pitchLimit);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void Move(bool canMove)
        {
            Vector3 input = Vector3.zero;
            bool run = false;
            if (canMove)
            {
                if (Input.GetKey(KeyCode.W)) input.z += 1f;
                if (Input.GetKey(KeyCode.S)) input.z -= 1f;
                if (Input.GetKey(KeyCode.D)) input.x += 1f;
                if (Input.GetKey(KeyCode.A)) input.x -= 1f;
                run = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            }
            if (input.sqrMagnitude > 1f) input.Normalize();
            Vector3 target = transform.TransformDirection(input) * (run ? runSpeed : walkSpeed);
            planarVelocity = Vector3.MoveTowards(planarVelocity, target, acceleration * Time.deltaTime);

            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f; // 바닥에 붙어 있도록
                if (canMove && Input.GetKeyDown(KeyCode.Space))
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }
            verticalVelocity += gravity * Time.deltaTime;
            Vector3 motion = planarVelocity + Vector3.up * verticalVelocity;
            controller.Move(motion * Time.deltaTime);
        }

        /// <summary>순간이동 (불러오기, 테스트용). CharacterController 를 잠시 끄고 위치를 옮긴다.</summary>
        public void Teleport(Vector3 position, float yaw)
        {
            controller.enabled = false;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            controller.enabled = true;
            planarVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        private static float NormalizeAngle(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            return a;
        }
    }
}
