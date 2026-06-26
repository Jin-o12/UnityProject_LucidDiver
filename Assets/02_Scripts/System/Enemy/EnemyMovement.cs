/// <summary>
/// 적의 이동과 상태에 따른 애니메이션을 관리하는 스크립트
/// 거리 기반 탐지에 더해, 적이 바라보는 방향 기준의 부채꼴 시야 판정을 사용합니다.
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [Header("Enemy Movement Controll")]
    [SerializeField] private float moveSpeed = 3.0f;              // 이동 속도
    [SerializeField] private float sightLength = 7.0f;            // 시야 거리
    [SerializeField] private float sightAngle = 120.0f;           // 시야 전체 각도
    [SerializeField] private float eyeHeight = 1.4f;            // 시야 판정 시작 높이

    // 플레이어 탐지 계산용 캐시값
    private Transform targetPlayer;                               // 현재 추적 중인 플레이어
    private float sightLengthSqr;                                 // 시야 거리의 제곱값
    private float halfSightAngle;                                 // 시야 반각
    private WaitForSeconds checkingTime = new WaitForSeconds(0.2f); // 상태 확인 주기

    [Header("Enemy Attack")]
    private float attackLength;                                   // 공격 거리
    private float attackLengthSqr;                                // 공격 거리의 제곱값
    private float attackCooldown;                                 // 공격 쿨타임

    [Header("Enemy Animation Controll")]
    private Animator animator;

    [Header("Componemt")]
    private EnemyStatus myStatus;
    private NavMeshAgent navAgent;

    // VisionGizmo가 읽어갈 시야 정보입니다.
    public float SightLength => sightLength;          // 시야 거리
    public float SightAngle => sightAngle;            // 시야 전체 각도
    public float EyeHeight => eyeHeight;              // 시야 판정 시작 높이
    public Transform CurrentTarget => targetPlayer;   // 현재 인식 중인 타겟

    void Awake()
    {
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        myStatus = GetComponent<EnemyStatus>();

        // 필수 컴포넌트가 하나라도 없으면 동작할 수 없다.
        if (animator == null || myStatus == null || navAgent == null)
        {
            enabled = false;
            Debug.LogError("EnemyMovement: 필요한 컴포넌트가 없습니다.");
            return;
        }

        targetPlayer = null;
        sightLengthSqr = sightLength * sightLength;
        halfSightAngle = sightAngle * 0.5f;

        attackLength = 3.0f;
        attackLengthSqr = attackLength * attackLength;
        attackCooldown = 2.0f;

        // NavMeshAgent가 인스펙터 속도값을 실제 이동에 사용하도록 맞춘다.
        navAgent.speed = moveSpeed;
    }

    private void OnEnable()
    {
        /// 이벤트 구독 ///
        myStatus.OnLocalDeath += Die;
        StartCoroutine(CheckRoutine());
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        myStatus.OnLocalDeath -= Die;
    }

    /* 현재 상태를 주기적으로 점검 */
    private IEnumerator CheckRoutine()
    {
        while (myStatus.nowState != EnemyStatus.EnemyState.Dead)
        {
            UpdateTarget();
            ChaseTarget();
            yield return checkingTime;
        }
    }

    /* 시야 거리와 시야각, 벽 가림 조건을 모두 통과한 가장 가까운 플레이어를 추적 대상으로 지정 */
    private void UpdateTarget()
    {
        float moreCloser = sightLengthSqr;
        Transform bestTarget = null;

        // 현재 존재하는 모든 플레이어들 중 적에게 가장 가까운 대상을 추적합니다
        foreach(GameObject player in GlobalRuntimeData.GetActivePlayers())
        {
            if (player == null)
                continue;

            // 1차로 거리 안에 들어왔는지 확인한다.
            float sqrDist = (transform.position - player.transform.position).sqrMagnitude;
            if (sqrDist > sightLengthSqr)
                continue;

            // 거리 안에 있어도 시야각 밖이거나 벽에 가려져 있으면 인식하지 않는다.
            if (!IsTargetInSight(player.transform))
                continue;

            if (sqrDist < moreCloser)
            {
                moreCloser = sqrDist;
                bestTarget = player.transform;
            }
        }

        targetPlayer = bestTarget;
    }

    /* 대상이 적의 전방 부채꼴 시야 안에 있고, 벽에 가려져 있지 않은지 확인 */
    private bool IsTargetInSight(Transform target)
    {
        if (target == null)
            return false;

        // 수평 기준 시야각 판정을 위해 y값을 제거한 방향을 만든다.
        Vector3 flatForward = transform.forward;
        flatForward.y = 0.0f;

        Vector3 flatDirectionToTarget = target.position - transform.position;
        flatDirectionToTarget.y = 0.0f;

        // 거의 같은 위치라면 각도 계산 없이 시야 안으로 본다.
        if (flatDirectionToTarget.sqrMagnitude <= 0.001f)
            return true;

        // 적의 정면 기준으로 시야 반각 안에 들어왔는지 확인한다.
        float angleToTarget = Vector3.Angle(flatForward.normalized, flatDirectionToTarget.normalized);
        if (angleToTarget > halfSightAngle)
            return false;

        // 이제 실제로 벽에 가려져 있는지 레이캐스트로 확인한다.
        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPosition = target.position + Vector3.up * eyeHeight;
        Vector3 directionToTarget = targetPosition - eyePosition;

        float targetDistance = directionToTarget.magnitude;
        if (targetDistance <= 0.001f)
            return true;

        // 플레이어보다 먼저 다른 콜라이더를 맞으면 시야가 막힌 것으로 본다.
        if (Physics.Raycast(eyePosition, directionToTarget.normalized, out RaycastHit hit, targetDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            // 첫 번째로 맞은 대상이 플레이어 본체 또는 플레이어 자식 오브젝트면 정상 인식이다.
            if (hit.transform == target || hit.transform.IsChildOf(target))
                return true;

            // 플레이어보다 먼저 벽이나 다른 오브젝트를 맞았으면 인식 실패다.
            return false;
        }

        return false;
    }

    /* 거리 계산을 통한 추적 및 공격 상태 지정 */
    private void ChaseTarget()
    {
        if (targetPlayer == null)
        {
            myStatus.SetNowState(EnemyStatus.EnemyState.Idle);
            myStatus.SetIsAttacking(false);
            animator.SetBool("isWalk", false);
            targetPlayer = null;
            navAgent.isStopped = false;
            navAgent.ResetPath();
            return;
        }

        float sqrDistToTarget = (transform.position - targetPlayer.position).sqrMagnitude;

        // 이미 공격 중이면 공격 코루틴이 끝날 때까지 중복 실행하지 않는다.
        if (myStatus.isAttacking)
            return;

        // 공격 거리 안에 있으면 공격 상태로 전환한다.
        if (attackLengthSqr >= sqrDistToTarget)
        {
            animator.SetBool("isWalk", false);
            StartCoroutine(Attack());
        }
        // 공격 거리는 아니지만 시야 거리 안이면 추적한다.
        else if (sqrDistToTarget <= sightLengthSqr)
        {
            navAgent.isStopped = false;
            navAgent.SetDestination(targetPlayer.position);
            myStatus.SetNowState(EnemyStatus.EnemyState.Chase);
            animator.SetBool("isWalk", true);
        }
    }

    public void Die()
    {
        StopAllCoroutines();
        animator.SetTrigger("isDead");
        Destroy(gameObject, 3.0f);
    }

    private IEnumerator Attack()
    {
        // 공격 시작 시 타겟이 사라졌으면 공격을 취소한다.
        if (targetPlayer == null)
            yield break;

        // 공격 상태 진입 및 이동 정지
        myStatus.SetNowState(EnemyStatus.EnemyState.Attack);
        myStatus.SetIsAttacking(true);

        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;
        navAgent.ResetPath();

        // 플레이어 방향으로 몸을 돌린다.
        transform.LookAt(new Vector3(targetPlayer.position.x, transform.position.y, targetPlayer.position.z));

        animator.SetTrigger("isAttack");

        // 공격 실행 후 쿨타임만큼 대기
        yield return new WaitForSeconds(attackCooldown);

        myStatus.SetIsAttacking(false);
        navAgent.isStopped = false;
    }

    public void isPlayerTakeDamage()
    {
        // 공격 중 플레이어가 범위를 벗어났으면 피해를 주지 않는다.
        if (targetPlayer == null)
            return;

        float sqrDistToTarget = (transform.position - targetPlayer.position).sqrMagnitude;

        if (attackLengthSqr >= sqrDistToTarget)
        {
            targetPlayer.GetComponentInParent<IDamageable>().TakeDamage(myStatus.atkValue);
        }
    }
}
