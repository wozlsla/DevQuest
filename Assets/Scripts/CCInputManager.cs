using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class CCinputManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cameraTransform;

    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField][Range(1.5f, 3f)] private float sprintMultiplier = 2f; // 달리기 속도 배율
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.2f;

    [Header("Fire Settings")]
    [SerializeField] private GameObject projectilePrefab; // 발사체 프리팹
    [SerializeField] private Transform firePoint; // 발사 위치 (없으면 카메라 위치)
    [SerializeField] private float projectileSpeed = 30f; // 발사체 속도
    [SerializeField] private float raycastDistance = 100f; // 조준점 찾기용 Raycast 거리

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

        // Fire 액션 (마우스 클릭으로 발사)
        input.Players.Fire.performed += _ => Fire();
    }

    private void OnDisable()
    {
        input.Players.Move.performed -= ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Players.Move.canceled -= ctx => moveInput = Vector2.zero;
        input.Players.Jump.performed -= _ => TryJump();
        input.Players.Fire.performed -= _ => Fire();

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

        // 달리기: Shift & 앞으로 이동 (moveInput.y > 0)
        float currentSpeed = moveSpeed;
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && moveInput.y > 0)
        {
            currentSpeed = moveSpeed * sprintMultiplier;
        }

        // 중력 처리 + 점프 착지 클램프
        if (controller.isGrounded && yVelocity < 0f)
            yVelocity = -2f; // 바닥에 붙여두기용 작은 음수

        // (선택) 점프: Jump 액션 만들었을 때만 사용
        // void TryJump() { if (controller.isGrounded) yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity); }

        yVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * currentSpeed + Vector3.up * yVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private void TryJump()
    {
        if (controller.isGrounded)
            yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    /// 마우스 클릭 시 발사체 발사 (Object Pooling 방식)
    private void Fire()
    {
        if (cameraTransform == null)
        {
            Debug.LogWarning("카메라 Transform이 설정되지 않았습니다.");
            return;
        }

        // Object Pool에서 발사체 가져오기
        GameObject projectile = ProjectilePool.Instance.GetProjectile();
        if (projectile == null)
        {
            Debug.LogWarning("사용 가능한 발사체가 없습니다.");
            return;
        }

        // 발사 시작 위치 결정 (카메라 앞쪽 0.5m)
        Vector3 spawnPosition = firePoint != null ? firePoint.position : cameraTransform.position + cameraTransform.forward * 0.5f;

        // 화면 중앙(크로스헤어)에서 Raycast를 쏴서 목표 지점 찾기
        Vector3 targetPoint;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
        {
            // Raycast가 무언가에 맞음
            targetPoint = hit.point;
            Debug.Log($"shooted {hit.collider.name} (distance: {hit.distance:F2}m)");
        }
        else
        {
            // Raycast가 아무것도 맞지 않음
            targetPoint = ray.origin + ray.direction * raycastDistance;
            Debug.Log("shooted");
        }

        // 발사 방향 계산 (발사 위치에서 목표 지점으로)
        Vector3 fireDirection = (targetPoint - spawnPosition).normalized;

        // 발사체 위치 및 회전 설정
        projectile.transform.position = spawnPosition;
        projectile.transform.rotation = Quaternion.LookRotation(fireDirection);

        // Projectile 스크립트가 있다면 속도 설정
        Projectile projectileScript = projectile.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.Launch(fireDirection, projectileSpeed);
        }
        else
        {
            // Rigidbody로 직접 속도 설정
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = fireDirection * projectileSpeed;
            }
        }
    }
}
