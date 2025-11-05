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
    [SerializeField] private float attackRange;
    [SerializeField] private float sightRange = 10f; // 인지 (최대)거리
    [SerializeField] private float fieldOfView = 120f; // (정면)시야각
    [SerializeField][Range(20f, 60f)] private float patrolRange = 40f;  // 순찰 범위
    [SerializeField] private float patrolWaitTime = 1.5f; // 순찰 지점 도착 후 대기

    public enum State
    {
        None,
        Idle,
        Patrol, // 순찰
        Chase, // 추적
        Attack
    }

    [Header("Debug")]
    public State state = State.None;
    public State nextState = State.None;

    private float stateTime;  // 상태 지속 시간 추적 -> 다음 상태 전환
    private bool attackDone;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindWithTag("Player")?.transform; // Null-Conditional Operator

        state = State.None;
        nextState = State.Patrol; // 시작 시 Patrol 상태로 전환
        stateTime = 0f;
    }

    private void Update()
    {
        stateTime += Time.deltaTime;
        bool playerInSight = CatchPlayer(); // 프레임마다 시야 검사

        //1. 스테이트 전환 상황 판단
        if (nextState == State.None)
        {
            switch (state)
            {
                case State.Idle:
                    /* -> Chase 상태에서 처리
                    // 1 << 6인 이유는 Player의 Layer가 6이기 때문
                    if (Physics.CheckSphere(transform.position, attackRange, 1 << 6, QueryTriggerInteraction.Ignore))
                    {
                        nextState = State.Attack;
                    }
                    break; 
                    */
                    if (playerInSight) // Idle 중 플레이어를 발견하면 추적
                    {
                        nextState = State.Chase;
                    }
                    // 대기 시간 끝나면 다시 순찰 상태로 전환
                    else if (stateTime >= patrolWaitTime)
                    {
                        nextState = State.Patrol;
                    }
                    break;
                case State.Attack:
                    if (attackDone)
                    {
                        nextState = State.Idle;
                        attackDone = false;
                    }
                    break;
                //insert code here...
                case State.Patrol:
                    if (playerInSight) // 순찰 중 플레이어를 발견하면 추적
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
                    // 1 << 6인 이유는 Player의 Layer가 6이기 때문
                    if (Physics.CheckSphere(transform.position, attackRange, 1 << 6, QueryTriggerInteraction.Ignore))
                    {
                        nextState = State.Attack; // 공격 범위 안에 들어오면 공격
                    }
                    else if (!playerInSight) // 플레이어를 놓치면 Idle 상태로 복귀
                    {
                        nextState = State.Idle;
                    }
                    break;
            }
        }

        //2. 스테이트 초기화 -> (새 상태 진입 시 단 한 번 실행)
        if (nextState != State.None)
        {
            state = nextState;
            nextState = State.None;
            stateTime = 0f; // 상태 전환 시 시간 초기화

            switch (state)
            {
                case State.Idle:
                    agent.isStopped = true; // 모든 이동 계산과 물리적 힘 적용 정지 -> Overshooting 등 방지
                    // animator.SetBool("isMoving", false); // 애니메이션 상태 초기화 (선택..)
                    break;
                case State.Attack:
                    agent.isStopped = true;
                    // animator.SetBool("isMoving", false);
                    Attack();
                    break;
                //insert code here...
                case State.Patrol:
                    agent.isStopped = false;
                    FindNewPatrolPoint(); // 새로운 순찰 지점 찾기
                    // animator.SetBool("isMoving", true);
                    break;
                case State.Chase:
                    agent.isStopped = false;
                    // animator.SetBool("isMoving", true);
                    break;
            }
        }

        //3. 글로벌 & 스테이트 업데이트
        //insert code here...
        if (state == State.Chase)
        {
            // 매 Update에서 목적지를 플레이어 위치로 업데이트하여 추적
            agent.SetDestination(player.position);

            // 플레이어를 바라보게 하여 시야각 검사
            LookAtTarget(player.position);
        }
    }

    private void Attack() //현재 공격은 애니메이션만 작동합니다.
    {
        animator.SetTrigger("attack");
    }

    public void InstantiateFx() //Unity Animation Event 에서 실행됩니다.
    {
        Instantiate(splashFx, transform.position, Quaternion.identity);
    }

    public void WhenAnimationDone() //Unity Animation Event 에서 실행됩니다.
    {
        attackDone = true;
    }

    private void OnDrawGizmosSelected()
    {
        //Gizmos를 사용하여 공격 범위를 Scene View에서 확인할 수 있게 합니다. (인게임에서는 볼 수 없습니다.)
        //해당 함수는 없어도 기능 상의 문제는 없지만, 기능 체크 및 디버깅을 용이하게 합니다.
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position, attackRange);
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
}
