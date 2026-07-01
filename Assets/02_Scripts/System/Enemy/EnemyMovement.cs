/// <summary>
/// 적의 이동, 시야 판정, 추적과 공격 상태를 관리하는 스크립트입니다.
/// 예전에 사용하던 부채꼴 시야각 판정과 벽 가림 판정을 현재 구조에 맞게 복원합니다.
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [Header("Enemy Movement Control")]
    [SerializeField] private float moveSpeed = 3.0f;       // NavMeshAgent에 반영할 이동 속도
    [SerializeField] private float sightLength = 10.0f;    // 적이 대상을 인식할 수 있는 최대 거리
    [SerializeField] private float sightAngle = 120.0f;    // 적의 전방 시야각
    [SerializeField] private float eyeHeight = 1.4f;       // 시야 레이캐스트를 시작할 높이

    [Header("Enemy Attack")]
    [SerializeField] private float attackLength = 3.0f;    // 공격 판정 거리
    [SerializeField] private float attackCooldown = 2.0f;  // 공격 후 다음 행동까지의 대기 시간

    [Header("Enemy Search")]
    [SerializeField] private float checkInterval = 0.2f;   // 타겟 탐색과 상태 갱신 주기

    private Transform targetPlayer;                        // 현재 추적 중인 플레이어
    private float sightLengthSqr;                          // 거리 비교용 제곱값 캐시
    private float halfSightAngle;                          // 시야 반각 캐시
    private float attackLengthSqr;                         // 공격 거리 제곱값 캐시
    private WaitForSeconds checkingTime;                   // 탐색 주기 캐시

    private Animator animator;
    private EnemyStatus myStatus;
    private NavMeshAgent navAgent;

    // VisionGizmo가 읽어갈 현재 시야 정보입니다.
    public float SightLength => sightLength;
    public float SightAngle => sightAngle;
    public float EyeHeight => eyeHeight;
    public Transform CurrentTarget => targetPlayer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        myStatus = GetComponent<EnemyStatus>();

        // 필수 컴포넌트가 없으면 잘못된 동작을 막기 위해 스크립트를 비활성화합니다.
        if (animator == null || navAgent == null || myStatus == null)
        {
            enabled = false;
            Debug.LogError("EnemyMovement: 필요한 컴포넌트가 없습니다.");
            return;
        }

        targetPlayer = null;
        ApplyCachedValues();

        // 인스펙터에서 조정한 이동 속도가 실제 NavMeshAgent에도 반영되도록 맞춰 둡니다.
        navAgent.speed = moveSpeed;
    }

    private void OnValidate()
    {
        // 인스펙터 값이 바뀌었을 때도 시야 계산용 캐시를 바로 갱신합니다.
        ApplyCachedValues();
    }

    private void OnEnable()
    {
        if (myStatus == null)
        {
            return;
        }

        myStatus.OnLocalDeath += Die;
        StartCoroutine(CheckRoutine());
    }

    private void OnDisable()
    {
        if (myStatus == null)
        {
            return;
        }

        myStatus.OnLocalDeath -= Die;
    }

    /// <summary>
    /// 거리와 각도 계산에 필요한 값을 미리 캐시해 둡니다.
    /// </summary>
    private void ApplyCachedValues()
    {
        sightLength = Mathf.Max(0.0f, sightLength);
        sightAngle = Mathf.Clamp(sightAngle, 0.0f, 360.0f);
        attackLength = Mathf.Max(0.0f, attackLength);
        checkInterval = Mathf.Max(0.05f, checkInterval);

        sightLengthSqr = sightLength * sightLength;
        halfSightAngle = sightAngle * 0.5f;
        attackLengthSqr = attackLength * attackLength;
        checkingTime = new WaitForSeconds(checkInterval);
    }

    /// <summary>
    /// 주기적으로 타겟을 갱신하고, 현재 상태에 따라 추적 또는 공격을 수행합니다.
    /// </summary>
    private IEnumerator CheckRoutine()
    {
        while (myStatus.nowState != EnemyStatus.EnemyState.Dead)
        {
            UpdateTarget();
            ChaseTarget();
            yield return checkingTime;
        }
    }

    /// <summary>
    /// 시야 거리, 시야각, 벽 가림 조건을 모두 통과한 가장 가까운 플레이어를 찾습니다.
    /// </summary>
    private void UpdateTarget()
    {
        float closestDistance = sightLengthSqr;
        Transform bestTarget = null;

        foreach (GameObject player in GlobalRuntimeData.GetPlayerList().Values)
        {
            if (player == null)
            {
                continue;
            }

            float sqrDistance = (transform.position - player.transform.position).sqrMagnitude;
            if (sqrDistance > sightLengthSqr)
            {
                continue;
            }

            if (!IsTargetInSight(player.transform))
            {
                continue;
            }

            if (sqrDistance < closestDistance)
            {
                closestDistance = sqrDistance;
                bestTarget = player.transform;
            }
        }

        targetPlayer = bestTarget;
    }

    /// <summary>
    /// 대상이 적의 전방 부채꼴 시야 안에 있고, 중간에 벽이 없는지 확인합니다.
    /// </summary>
    private bool IsTargetInSight(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        // 높이 차이보다 수평 방향을 우선해서 시야각 판정을 합니다.
        Vector3 flatForward = transform.forward;
        flatForward.y = 0.0f;

        Vector3 flatDirectionToTarget = target.position - transform.position;
        flatDirectionToTarget.y = 0.0f;

        // 거의 같은 위치라면 각도 판정 없이 바로 시야 안으로 취급합니다.
        if (flatDirectionToTarget.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        float angleToTarget = Vector3.Angle(flatForward.normalized, flatDirectionToTarget.normalized);
        if (angleToTarget > halfSightAngle)
        {
            return false;
        }

        // 시야각을 통과한 뒤에는 실제로 벽에 가려져 있는지 레이캐스트로 확인합니다.
        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPosition = target.position + Vector3.up * eyeHeight;
        Vector3 directionToTarget = targetPosition - eyePosition;
        float targetDistance = directionToTarget.magnitude;

        if (targetDistance <= 0.001f)
        {
            return true;
        }

        if (Physics.Raycast(
                eyePosition,
                directionToTarget.normalized,
                out RaycastHit hit,
                targetDistance,
                ~0,
                QueryTriggerInteraction.Ignore))
        {
            // 플레이어 본체나 자식 오브젝트를 먼저 맞으면 정상 인식으로 봅니다.
            if (hit.transform == target || hit.transform.IsChildOf(target))
            {
                return true;
            }

            // 그보다 먼저 다른 콜라이더를 맞았다면 벽이나 장애물에 가려진 상태입니다.
            return false;
        }

        return false;
    }

    /// <summary>
    /// 현재 타겟과의 거리 상태를 기준으로 대기, 추적, 공격 행동을 전환합니다.
    /// </summary>
    private void ChaseTarget()
    {
        if (targetPlayer == null)
        {
            myStatus.SetNowState(EnemyStatus.EnemyState.Idle);
            myStatus.SetIsAttacking(false);
            animator.SetBool("isWalk", false);

            navAgent.isStopped = false;
            navAgent.ResetPath();
            return;
        }

        float sqrDistToTarget = (transform.position - targetPlayer.position).sqrMagnitude;

        // 공격 중에는 추적과 공격 시작을 중복해서 실행하지 않습니다.
        if (myStatus.isAttacking)
        {
            return;
        }

        if (attackLengthSqr >= sqrDistToTarget)
        {
            animator.SetBool("isWalk", false);
            StartCoroutine(Attack());
        }
        else if (sqrDistToTarget <= sightLengthSqr)
        {
            navAgent.isStopped = false;
            navAgent.SetDestination(targetPlayer.position);
            myStatus.SetNowState(EnemyStatus.EnemyState.Chase);
            animator.SetBool("isWalk", true);
        }
    }

    /// <summary>
    /// 적이 죽었을 때 추적 코루틴을 멈추고 사망 애니메이션을 재생합니다.
    /// </summary>
    public void Die()
    {
        StopAllCoroutines();
        animator.SetTrigger("isDead");
        Destroy(gameObject, 3.0f);
    }

    /// <summary>
    /// 공격 중에는 이동을 멈추고, 대상을 바라본 뒤 공격 애니메이션을 재생합니다.
    /// </summary>
    private IEnumerator Attack()
    {
        if (targetPlayer == null)
        {
            yield break;
        }

        myStatus.SetNowState(EnemyStatus.EnemyState.Attack);
        myStatus.SetIsAttacking(true);

        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;
        navAgent.ResetPath();

        transform.LookAt(new Vector3(targetPlayer.position.x, transform.position.y, targetPlayer.position.z));
        animator.SetTrigger("isAttack");

        yield return new WaitForSeconds(attackCooldown);

        myStatus.SetIsAttacking(false);
        navAgent.isStopped = false;
    }

    /// <summary>
    /// 공격 타이밍에 플레이어가 여전히 사거리 안에 있을 때만 피해를 적용합니다.
    /// </summary>
    public void isPlayerTakeDamage()
    {
        if (targetPlayer == null)
        {
            return;
        }

        float sqrDistToTarget = (transform.position - targetPlayer.position).sqrMagnitude;
        if (attackLengthSqr >= sqrDistToTarget)
        {
            targetPlayer.GetComponentInParent<IDamageable>().TakeDamage(myStatus.atkValue);
        }
    }
}
