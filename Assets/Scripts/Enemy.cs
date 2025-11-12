using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour
{
    [Header("Preset Fields")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject splashFx;
    [SerializeField] private NavMeshAgent agent; // NavMesh Agent 컴포넌트
    private Transform player;

    [Header("Settings")]
    [SerializeField] private float attackRange = 3f; // 공격 사거리
    [SerializeField] private float sightRange = 10f; // 인지 (최대)거리
    [SerializeField] private float fieldOfView = 120f; // (정면)시야각
    [SerializeField][Range(20f, 60f)] private float patrolRange = 40f;  // 순찰 범위
    [SerializeField] private float patrolWaitTime = 1.5f; // 순찰 지점 도착 후 대기

    [Header("Combat")]
    [SerializeField] private float maxHP = 100f; // 최대 체력
    [SerializeField][Range(0f, 1f)] private float kickProbability = 0.3f; // 발차기 확률 (0~1)
    [SerializeField] private float attackTimeout = 3f; // 공격 타임아웃 (애니메이션 이벤트 안전장치)
    [SerializeField] private float attackDamage = 15f; // 일반 공격 데미지
    [SerializeField] private float kickDamage = 25f; // 발차기 데미지
    private float currentHP; // 현재 체력

    public enum State
    {
        None,
        Idle,
        Patrol, // 순찰
        Chase, // 추적
        Attack,
        Martelo, // 발차기 공격
        Dying // HP 0
    }

    [Header("Debug")]
    public State state = State.None;
    public State nextState = State.None;

    private float stateTime;  // 상태 지속 시간 추적 -> 다음 상태 전환
    private bool attackDone; // 일반 공격 완료 플래그
    private bool marteloDone; // 발차기 완료 플래그

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindWithTag("Player")?.transform; // Null-Conditional Operator

        state = State.None;
        currentHP = maxHP; // 체력 초기화
        nextState = State.Patrol; // 시작 시 Patrol 상태로 전환
        stateTime = 0f; // 상태 지속 시간 초기화
    }

    private void Update()
    {
        // 죽음 상태에서는 더 이상 업데이트 X
        if (state == State.Dying) return;

        // 글로벌 체력 체크 (어떤 상태에서든 체력이 0이면 죽음)
        if (currentHP <= 0 && state != State.Dying)
        {
            nextState = State.Dying;
        }

        stateTime += Time.deltaTime; // 상태 지속 시간 업데이트
        bool playerInSight = CatchPlayer(); // 프레임마다 시야 검사

        //1. 스테이트 전환 상황 판단
        if (nextState == State.None)
        {
            switch (state)
            {
                case State.Idle:
                    if (playerInSight) // 플레이어를 발견 시 추적
                    {
                        nextState = State.Chase;
                    }
                    else if (stateTime >= patrolWaitTime) // 대기 시간 끝나면 순찰 상태로 전환
                    {
                        nextState = State.Patrol;
                    }
                    break;

                case State.Patrol:
                    if (playerInSight) // 플레이어를 발견 시 추적
                    {
                        nextState = State.Chase;
                    }
                    // NavMeshAgent의 남은 거리가 0.5f 이하이면 목적지에 도착한 것으로 판단하고 대기 상태(Idle)로 전환
                    else if (agent.remainingDistance <= 0.5f && !agent.pathPending)
                    {
                        nextState = State.Idle;
                    }
                    break;

                case State.Chase:
                    /* Physics.CheckSphere()\
                       특정 위치를 중심으로 구형 범위 안에 특정 레이어의 오브젝트가 있는지 검사
                       1 << 6: 비트 연산으로 6번 레이어(Player)만 검사 (Player의 Layer가 6) */

                    // 발차기 범위 체크 (일반 공격보다 조금 더 먼 거리)
                    if (Physics.CheckSphere(transform.position, attackRange, 1 << 6, QueryTriggerInteraction.Ignore))
                    {
                        // 확률에 따라 발차기 또는 일반 공격 선택
                        if (Random.value < kickProbability)
                        {
                            nextState = State.Martelo; // 발차기 공격
                            Debug.Log("발차기 공격 선택!");
                        }
                        else
                        {
                            nextState = State.Attack; // 일반 공격
                            Debug.Log("일반 공격 선택!");
                        }
                    }
                    else if (!playerInSight) // 플레이어를 놓치면 Idle 상태로 복귀
                    {
                        nextState = State.Idle;
                    }
                    break;

                case State.Attack:
                    // 애니메이션 완료 or 타임아웃
                    if (attackDone || stateTime >= attackTimeout)
                    {
                        if (stateTime >= attackTimeout)
                        {
                            Debug.LogWarning("일반 공격 타임아웃! 애니메이션 이벤트를 확인하세요.");
                        }

                        attackDone = false;

                        // 공격 완료 후 플레이어 위치 재확인
                        if (playerInSight && Physics.CheckSphere(transform.position, attackRange, 1 << 6, QueryTriggerInteraction.Ignore))
                        {
                            // 여전히 공격 범위 내 → 다시 Chase로 가서 공격 선택
                            nextState = State.Chase;
                        }
                        else if (playerInSight)
                        {
                            // 공격 범위 밖이지만 시야 내 → Chase
                            nextState = State.Chase;
                        }
                        else
                        {
                            // 시야 밖 → Idle
                            nextState = State.Idle;
                        }
                    }
                    break;

                case State.Martelo:
                    // 애니메이션 완료 or 타임아웃
                    if (marteloDone || stateTime >= attackTimeout)
                    {
                        if (stateTime >= attackTimeout)
                        {
                            Debug.LogWarning("발차기 타임아웃! 애니메이션 이벤트(WhenMarteloDone)를 확인하세요.");
                        }

                        marteloDone = false;

                        // 공격 완료 후 플레이어 위치 재확인
                        if (playerInSight && Physics.CheckSphere(transform.position, attackRange, 1 << 6, QueryTriggerInteraction.Ignore))
                        {
                            // 여전히 공격 범위 내 → 다시 Chase로 가서 공격 선택
                            nextState = State.Chase;
                        }
                        else if (playerInSight)
                        {
                            // 공격 범위 밖이지만 시야 내 → Chase
                            nextState = State.Chase;
                        }
                        else
                        {
                            // 시야 밖 → Idle
                            nextState = State.Idle;
                        }
                    }
                    break;

                case State.Dying: // 죽음 상태에서는 전환 X
                    // 죽음 애니메이션 재생 후 아무 행동도 하지 않음
                    break;
            }
        }

        //2. 스테이트 초기화 -> (새 상태 진입 시 단 한 번 실행)
        if (nextState != State.None)
        {
            state = nextState;
            nextState = State.None;
            stateTime = 0f; // 상태 지속 시간 초기화

            switch (state)
            {
                case State.Idle:
                    agent.isStopped = true; // 모든 이동 계산과 물리적 힘 적용 정지 -> Overshooting 등 방지
                    animator.SetBool("isMoving", false); // 애니메이션 상태 초기화
                    break;

                case State.Patrol:
                    agent.isStopped = false;
                    FindNewPatrolPoint(); // 새로운 순찰 지점 찾기
                    animator.SetBool("isMoving", true);
                    break;

                case State.Chase:
                    agent.isStopped = false;
                    animator.SetBool("isMoving", true);
                    break;

                case State.Attack:
                    agent.isStopped = true;
                    animator.SetBool("isMoving", false);
                    Attack(); // 일반 공격 트리거
                    break;

                case State.Martelo:
                    agent.isStopped = true;
                    animator.SetBool("isMoving", false);
                    DoMartelo(); // 발차기 애니메이션 트리거
                    break;

                case State.Dying:
                    agent.isStopped = true;
                    animator.SetBool("isMoving", false);
                    animator.SetTrigger("dying");

                    agent.enabled = false; // NavMeshAgent 비활성화 (더 이상 움직이지 않음)
                    GetComponent<Collider>().enabled = false; // Collider 비활성화 (죽은 적과 충돌 방지)

                    // GameManager에 Enemy 처치 알림
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.OnEnemyDeath();
                    }

                    // 3초 후 오브젝트 파괴
                    Destroy(gameObject, 3f);
                    break;


            }
        }

        //3. 글로벌 & 스테이트 업데이트
        if (state == State.Chase)
        {
            // 매 Update에서 목적지를 플레이어 위치로 업데이트하여 추적
            agent.SetDestination(player.position);

            // 플레이어를 바라보게 하여 시야각 검사
            LookAtTarget(player.position);
        }
    }

    private void Attack() //현재 공격은 애니메이션만 작동
    {
        animator.SetTrigger("attack");
    }

    private void DoMartelo() // 발차기 공격 실행
    {
        Debug.Log("DoMartelo() 호출됨 - animator.SetTrigger('martelo') 실행"); // 디버그용
        animator.SetTrigger("martelo");

        // Animator에 martelo 파라미터가 있는지 확인
        if (animator.parameterCount == 0)
        {
            Debug.LogError("Animator에 파라미터가 없습니다!");
        }
    }

    // 데미지를 받는 함수 (플레이어의 총알이나 공격에서 호출됩니다)
    public void TakeDamage(float damage)
    {
        // 이미 죽은 상태면 데미지 무시
        if (state == State.Dying) return;

        currentHP -= damage;
        Debug.Log($"[Enemy] {damage} 데미지 받음! 남은 체력: {currentHP}/{maxHP}");

        // 체력이 0 이하가 되면 Update에서 자동으로 Dying 상태로 전환됩니다
    }

    // Animation Event: 일반 공격 타이밍에 호출 (공격 애니메이션 중간 지점)
    public void DealAttackDamage()
    {
        Debug.Log("[Enemy] DealAttackDamage() 호출됨!");

        // 공격 범위 내에 플레이어가 있는지 확인
        if (Physics.CheckSphere(transform.position, attackRange, 1 << 6, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("[Enemy] 플레이어가 공격 범위 내에 있음!");

            // 플레이어에게 데미지
            if (player != null)
            {
                Player playerScript = player.GetComponent<Player>();
                if (playerScript != null)
                {
                    playerScript.TakeDamage(attackDamage);
                    Debug.Log($"[Enemy] 플레이어에게 일반 공격 {attackDamage} 데미지!");
                }
                else
                {
                    Debug.LogWarning("[Enemy] Player 스크립트를 찾을 수 없음!");
                }
            }
        }
        else
        {
            Debug.Log("[Enemy] 플레이어가 공격 범위 밖에 있음");
        }
    }

    // Animation Event: 발차기 공격 타이밍에 호출 (발차기 애니메이션 중간 지점)
    public void DealKickDamage()
    {
        Debug.Log("[Enemy] DealKickDamage() 호출됨!");

        // 공격 범위 내에 플레이어가 있는지 확인
        if (Physics.CheckSphere(transform.position, attackRange, 1 << 6, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("[Enemy] 플레이어가 공격 범위 내에 있음!");

            // 플레이어에게 데미지
            if (player != null)
            {
                Player playerScript = player.GetComponent<Player>();
                if (playerScript != null)
                {
                    playerScript.TakeDamage(kickDamage);
                    Debug.Log($"[Enemy] 플레이어에게 발차기 {kickDamage} 데미지!");
                }
                else
                {
                    Debug.LogWarning("[Enemy] Player 스크립트를 찾을 수 없음!");
                }
            }
        }
        else
        {
            Debug.Log("[Enemy] 플레이어가 공격 범위 밖에 있음");
        }
    }

    public void InstantiateFx() //Unity Animation Event 에서 실행됩니다.
    {
        Instantiate(splashFx, transform.position, Quaternion.identity);
    }

    public void WhenAnimationDone() //Unity Animation Event 에서 실행됩니다.
    {
        attackDone = true;
    }

    public void WhenMarteloDone() //Unity Animation Event 에서 실행됩니다. (발차기 애니메이션 끝날 때)
    {
        marteloDone = true;
    }

    private void OnDrawGizmosSelected()
    {
        //Gizmos를 사용하여 공격 범위를 Scene View에서 확인할 수 있게 합니다. (인게임에서는 볼 수 없습니다.)
        //해당 함수는 없어도 기능 상의 문제는 없지만, 기능 체크 및 디버깅을 용이하게 합니다.

        // 공격 범위 (빨간색)
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position, attackRange);

        // 시야 범위 (파란색, 투명)
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.1f);
        Gizmos.DrawSphere(transform.position, sightRange);
    }

    private void LookAtTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        // Y축 회전만 적용하여 캐릭터가 넘어지지 않도록 방지
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        // Quaternion.Slerp를 사용하여 부드럽게 회전
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    // 순찰 목표 지점을 NavMesh 위에서 찾아 설정
    private void FindNewPatrolPoint()
    {
        // 현재 위치(transform.position)를 중심으로 patrolRange 반경 내에서 랜덤 지점 탐색
        Vector3 randomDirection = Random.insideUnitSphere * patrolRange;
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRange, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private bool CatchPlayer()
    {
        if (player == null) return false;

        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        // 1. 거리 검사: sightRange 내에 있는지 확인
        if (distanceToPlayer > sightRange) return false;

        // 2. 시야각 검사: fieldOfView 내에 있는지 확인
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle > fieldOfView / 2f) return false;


        // 3. 장애물 검사 (Raycasting): 적의 눈높이에서 플레이어까지 레이를 발사
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        // ~ (1 << 3)은 Ground 레이어(3)를 제외한 모든 레이어와 충돌 검사
        if (Physics.Raycast(rayOrigin, directionToPlayer.normalized, out hit, distanceToPlayer, ~(1 << 3), QueryTriggerInteraction.Ignore))
        {
            // Raycast가 플레이어를 맞췄는지 확인 (플레이어 오브젝트에 Player 태그 필요)
            if (hit.collider.gameObject.CompareTag("Player"))
            {
                return true; // 플레이어 발견
            }
            return false; // 플레이어 외 다른 오브젝트(벽 등)에 가로막힘
        }

        // Raycast가 아무것도 맞추지 않았으면 (거리가 짧거나, 플레이어를 정확히 맞추지 못했을 경우)
        return true;
    }

    // UI용 체력 Getter 함수들
    public float GetCurrentHP()
    {
        return currentHP;
    }

    public float GetMaxHP()
    {
        return maxHP;
    }
}
