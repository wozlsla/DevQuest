using UnityEngine;

public class CCinputManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cameraTransform;

    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.2f;


    private PlayerActions input;
    private Vector2 moveInput;
    private float yVelocity;

    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        input = new PlayerActions();
    }

    private void OnEnable()
    {
        // 액션맵 활성화
        input.Players.Enable();

        // Move(Vector2) 입력 수신
        input.Players.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Players.Move.canceled += ctx => moveInput = Vector2.zero;

        // (선택) Jump 액션을 만들었다면 예시:
        input.Players.Jump.performed += _ => TryJump();
    }

    private void OnDisable()
    {
        input.Players.Move.performed -= ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Players.Move.canceled -= ctx => moveInput = Vector2.zero;
        input.Players.Jump.performed -= _ => TryJump();

        input.Players.Disable();
    }



    private void Update()
    {
        // 카메라 기준으로 평면 이동 벡터 만들기
        Vector3 camForward = cameraTransform ? cameraTransform.forward : Vector3.forward;
        Vector3 camRight = cameraTransform ? cameraTransform.right : Vector3.right;
        camForward.y = 0f; camRight.y = 0f;
        camForward.Normalize(); camRight.Normalize();

        Vector3 move = camForward * moveInput.y + camRight * moveInput.x; // y는 전후, x는 좌우
        if (move.sqrMagnitude > 1f) move.Normalize();

        // 중력 처리 + 점프 착지 클램프
        if (controller.isGrounded && yVelocity < 0f)
            yVelocity = -2f; // 바닥에 붙여두기용 작은 음수

        // (선택) 점프: Jump 액션 만들었을 때만 사용
        void TryJump() { if (controller.isGrounded) yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity); }

        yVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * moveSpeed + Vector3.up * yVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private void TryJump()
    {
        if (controller.isGrounded)
            yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }
}
