using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class CCinputManager : MonoBehaviour
{
    [Header("Refs")] // 참조 변수 저장
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cameraTransform; // 플레이어가 "앞으로" 이동할 때, 카메라 시야도 "앞"이어야 함

    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 5f; // 5m/s
    [SerializeField][Range(1.5f, 3f)] private float sprintMultiplier = 2f; // 달리기 속도 배율
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.2f;

    [Header("Fire Settings")]
    [SerializeField] private GameObject projectilePrefab; // 발사체 프리팹
    [SerializeField] private Transform firePoint; // 발사 위치 (없으면 카메라 위치)
    [SerializeField] private float projectileSpeed = 30f; // 발사체 속도
    [SerializeField] private float raycastDistance = 100f; // 100m 까지 확인 -> 조준점 찾기 (Raycast: 레이저를 쏴서 무엇에 맞는지 확인하는 기능)

    private PlayerActions input; // Unity Input System
    private Vector2 moveInput;
    private float yVelocity; // 수직(Y축) 속도 -> 점프하면 양수, 떨어지면 음수

    /* 게임 시작 시 초기화 작업 
       스크립트가 로드될 때 가장 먼저 실행 (Start 보다 먼저 실행) */
    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>(); // Inspector에서 할당하지 않았을 경우
        input = new PlayerActions(); // 입력 시스템 객체 생성
    }

    /* 오브젝트가 활성화될 때마다 실행 -> 입력 이벤트 구독 */
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

    /* 오브젝트가 비활성화될 때 실행 -> 입력 이벤트 구독 취소**
       메모리 누수 방지 */
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
        /* 매 프레임마다 플레이어의 입력을 받아서 캐릭터 이동시키기 */

        // 1. 카메라 방향 기준으로 이동 방향 계산
        Vector3 camForward = cameraTransform ? cameraTransform.forward : Vector3.forward;
        Vector3 camRight = cameraTransform ? cameraTransform.right : Vector3.right;

        camForward.y = 0f; camRight.y = 0f; // y축 성분 제거
        camForward.Normalize(); camRight.Normalize(); // 방향만 유지

        // 2. 입력에 따라 이동 벡터 합성
        Vector3 move = camForward * moveInput.y + camRight * moveInput.x; // y는 전후, x는 좌우
        if (move.sqrMagnitude > 1f) move.Normalize(); // 속도 동일

        // 3. 달리기 처리
        float currentSpeed = moveSpeed;
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && moveInput.y > 0)
        {
            currentSpeed = moveSpeed * sprintMultiplier;
        }

        // 4. 중력 처리 + 점프 착지 클램프
        if (controller.isGrounded && yVelocity < 0f)
            yVelocity = -2f; // 바닥에 붙여두기용 작은 음수

        yVelocity += gravity * Time.deltaTime;

        // 5. 최종 속도 계산 및 이동
        Vector3 velocity = move * currentSpeed + Vector3.up * yVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private void TryJump()
    {
        if (controller.isGrounded)
            yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity); // 점프 속도 계산
    }

    /// 마우스 클릭 시 발사체 발사 (Object Pooling 방식)
    private void Fire()
    {
        // 1. 카메라 확인 (->조준)
        if (cameraTransform == null)
        {
            Debug.LogWarning("카메라 Transform이 설정되지 않았습니다.");
            return;
        }

        // 2. Object Pool에서 발사체 가져오기
        GameObject projectile = ProjectilePool.Instance.GetProjectile();
        if (projectile == null)
        {
            Debug.LogWarning("사용 가능한 발사체가 없습니다.");
            return;
        }

        // 3. 발사 시작 위치 결정 (카메라 앞쪽 0.5m -> 카메라 위치에서 생성하면 플레이어와 충돌)
        Vector3 spawnPosition = firePoint != null ? firePoint.position : cameraTransform.position + cameraTransform.forward * 0.5f;

        /* Raycast: 목표 지점 찾기 
           Unity의 물리 엔진을 사용해 광선이 무엇에 부딪히는지 확인하는 기능 */

        Vector3 targetPoint; // 목표 지점 저장

        // 1. Ray(시작점과 방향을 가진 무한한 선) 생성
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward); // 카메라 위치에서, 카메라가 보는 방향

        // 2. Raycast 쏘기 (충돌 감지)
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
        {
            // true) Raycast가 무언가에 맞음
            targetPoint = hit.point; // 광선이 맞은 3D 좌표
            Debug.Log($"shooted {hit.collider.name} (distance: {hit.distance:F2}m)");
        }
        else
        {
            // false) Raycast가 아무것도 맞지 않음
            targetPoint = ray.origin + ray.direction * raycastDistance;
            Debug.Log("shooted");
        }

        /* 발사 실행 
           발사체를 정확한 위치에 배치하고, 목표를 향해 날려보냄 */

        // 1. 발사 방향 계산 (발사 위치에서 목표 지점으로)
        Vector3 fireDirection = (targetPoint - spawnPosition).normalized;

        // 2. 발사체 위치 및 회전 설정
        projectile.transform.position = spawnPosition;
        projectile.transform.rotation = Quaternion.LookRotation(fireDirection); // (발사체를) 발사 방향으로 회전 ex.bullet

        // 3. 속도 설정
        // 3-1. Projectile 스크립트가 있다면 사용
        // 3-2. Rigidbody로 직접 속도 설정 (백업용)
        Projectile projectileScript = projectile.GetComponent<Projectile>();

        if (projectileScript != null)
        {
            projectileScript.Launch(fireDirection, projectileSpeed);
        }
        else
        {
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = fireDirection * projectileSpeed; // 방향 * 속력 = 속도 벡터
            }
        }
    }
}
