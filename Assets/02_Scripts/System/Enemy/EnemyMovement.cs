using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적의 시야 인식, 추적 유지, 공격, 그리고 소리 조사 상태를 함께 관리합니다.
/// 현재 프로젝트는 EnemyBrain/Perception으로 완전히 분리되어 있지 않기 때문에,
/// 이번 단계에서는 기존 EnemyMovement 안에 노이즈 조사 흐름을 안전하게 통합합니다.
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    [Header("Enemy Movement Control")]
    [SerializeField] private float moveSpeed = 3.0f;
    [SerializeField] private float sightLength = 15.0f;
    [SerializeField] private float awarenessRange = 20.0f;
    [SerializeField] private float hearingRange = 40.0f;
    [SerializeField] private float sightAngle = 120.0f;
    [SerializeField] private float eyeHeight = 1.4f;

    [Header("Enemy Attack")]
    [SerializeField] private float attackLength = 3.0f;
    [SerializeField] private float attackCooldown = 2.0f;

    [Header("Enemy Search")]
    [SerializeField] private float checkInterval = 0.2f;
    [SerializeField] private float investigateStopDistance = 1.0f;
    [SerializeField] private float investigateDuration = 2.0f;
    [SerializeField] private float investigateNavMeshSampleDistance = 2.5f;

    private Transform targetPlayer;          // 현재 추적 중인 플레이어
    private float sightLengthSqr;            // 시야 거리 제곱값 캐시
    private float awarenessRangeSqr;         // 어그로 유지 거리 제곱값 캐시
    private float hearingRangeSqr;           // 청각 감지 거리 제곱값 캐시
    private float halfSightAngle;            // 시야 절반 각도 캐시
    private float attackLengthSqr;           // 공격 거리 제곱값 캐시
    private float investigateStopDistanceSqr;// 조사 목적지 도착 판정 제곱값 캐시
    private WaitForSeconds checkingTime;     // 탐색 코루틴 간격 캐시

    private bool hasInvestigateTarget;       // 조사할 소리 위치가 있는지 여부
    private bool hasReachedInvestigatePoint; // 조사 목적지에 도착했는지 여부
    private Vector3 investigateTargetPosition;
    private Transform investigateTargetAnchor;
    private float investigateSearchEndTime;  // 도착 후 제자리 조사 종료 시간
    private float forcedInvestigationUntilTime; // 디코이성 소음 때문에 시야 재획득을 잠시 막는 시간
    private int currentInvestigatePriority;  // 현재 조사 중인 소리의 우선순위

    private Animator animator;
    private EnemyStatus myStatus;
    private NavMeshAgent navAgent;

    public float SightLength => sightLength;
    public float AwarenessRange => awarenessRange;
    public float HearingRange => hearingRange;
    public float SightAngle => sightAngle;
    public float EyeHeight => eyeHeight;
    public Transform CurrentTarget => targetPlayer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        myStatus = GetComponent<EnemyStatus>();

        // 이동/상태/애니메이션 중 하나라도 빠지면 이 스크립트는 정상 동작할 수 없습니다.
        if (animator == null || navAgent == null || myStatus == null)
        {
            enabled = false;
            Debug.LogError("EnemyMovement: required components are missing.");
            return;
        }

        targetPlayer = null;
        ClearInvestigateTarget();
        ApplyCachedValues();
        navAgent.speed = moveSpeed;
    }

    private void OnValidate()
    {
        ApplyCachedValues();
    }

    private void OnEnable()
    {
        if (myStatus == null)
        {
            return;
        }

        myStatus.OnLocalDeath += Die;
        GlobalEventBus.OnNoiseEmitted += HandleNoiseEmitted;
        StartCoroutine(CheckRoutine());
    }

    private void OnDisable()
    {
        if (myStatus == null)
        {
            return;
        }

        myStatus.OnLocalDeath -= Die;
        GlobalEventBus.OnNoiseEmitted -= HandleNoiseEmitted;
    }

    /// <summary>
    /// 거리/각도 계산에서 반복 사용하는 값을 미리 캐시해 둡니다.
    /// </summary>
    private void ApplyCachedValues()
    {
        sightLength = Mathf.Max(0.0f, sightLength);
        awarenessRange = Mathf.Max(sightLength, awarenessRange);
        hearingRange = Mathf.Max(awarenessRange, hearingRange);
        sightAngle = Mathf.Clamp(sightAngle, 0.0f, 360.0f);
        attackLength = Mathf.Max(0.0f, attackLength);
        checkInterval = Mathf.Max(0.05f, checkInterval);
        investigateStopDistance = Mathf.Max(0.1f, investigateStopDistance);
        investigateDuration = Mathf.Max(0.1f, investigateDuration);
        investigateNavMeshSampleDistance = Mathf.Max(0.5f, investigateNavMeshSampleDistance);

        sightLengthSqr = sightLength * sightLength;
        awarenessRangeSqr = awarenessRange * awarenessRange;
        hearingRangeSqr = hearingRange * hearingRange;
        halfSightAngle = sightAngle * 0.5f;
        attackLengthSqr = attackLength * attackLength;
        investigateStopDistanceSqr = investigateStopDistance * investigateStopDistance;
        checkingTime = new WaitForSeconds(checkInterval);
    }

    /// <summary>
    /// 일정 주기로 시야/소리 상태를 갱신하고 현재 행동을 전환합니다.
    /// </summary>
    private IEnumerator CheckRoutine()
    {
        while (myStatus.nowState != EnemyStatus.EnemyState.Dead)
        {
            UpdateTarget();
            UpdateMovementState();
            yield return checkingTime;
        }
    }

    /// <summary>
    /// 현재 타겟을 유지할지, 새로 시야 내 플레이어를 찾을지 결정합니다.
    /// 강제 조사 시간 중에는 일부러 시야 재획득을 막아 "어그로를 끄는 소리"가 체감되게 합니다.
    /// </summary>
    private void UpdateTarget()
    {
        if (Time.time < forcedInvestigationUntilTime && hasInvestigateTarget)
        {
            targetPlayer = null;
            return;
        }

        if (targetPlayer != null && IsTargetWithinAwareness(targetPlayer))
        {
            return;
        }

        targetPlayer = FindVisibleTarget();
        if (targetPlayer != null)
        {
            ClearInvestigateTarget();
        }
    }

    /// <summary>
    /// 시야 조건을 모두 통과한 가장 가까운 플레이어를 찾습니다.
    /// </summary>
    private Transform FindVisibleTarget()
    {
        float closestDistance = float.MaxValue;
        Transform bestTarget = null;

        foreach (GameObject player in GlobalRuntimeData.GetPlayerList().Values)
        {
            if (player == null)
            {
                continue;
            }

            float sqrDistance = GetPlanarSqrDistance(transform.position, player.transform.position);
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

        return bestTarget;
    }

    /// <summary>
    /// 한 번 어그로가 붙은 뒤에는 시야각 밖으로 나가도 awarenessRange 안에 있는 동안 추적을 유지합니다.
    /// </summary>
    private bool IsTargetWithinAwareness(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        return GetPlanarSqrDistance(transform.position, target.position) <= awarenessRangeSqr;
    }

    /// <summary>
    /// 대상이 전방 시야각 안에 있고, 벽에 가려지지 않았는지 확인합니다.
    /// </summary>
    private bool IsTargetInSight(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        Vector3 flatForward = transform.forward;
        flatForward.y = 0.0f;

        Vector3 flatDirectionToTarget = target.position - transform.position;
        flatDirectionToTarget.y = 0.0f;

        if (flatDirectionToTarget.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        float angleToTarget = Vector3.Angle(flatForward.normalized, flatDirectionToTarget.normalized);
        if (angleToTarget > halfSightAngle)
        {
            return false;
        }

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
            if (hit.transform == target || hit.transform.IsChildOf(target))
            {
                return true;
            }

            return false;
        }

        return false;
    }

    /// <summary>
    /// 노이즈 매니저가 확정한 소음을 듣고 조사 목표를 갱신합니다.
    /// 일반 소리는 추적 중인 적을 꺾지 못하고, 디코이성 소리만 어그로를 끊을 수 있게 분기합니다.
    /// </summary>
    private void HandleNoiseEmitted(NoiseStimulus stimulus)
    {
        if (!enabled || myStatus == null || myStatus.nowState == EnemyStatus.EnemyState.Dead)
        {
            return;
        }

        if (stimulus.Source == gameObject)
        {
            return;
        }

        float effectiveRadius = Mathf.Min(hearingRange, Mathf.Max(0.0f, stimulus.Radius));
        if (effectiveRadius <= 0.0f)
        {
            return;
        }

        float sqrDistanceToNoise = GetPlanarSqrDistance(transform.position, stimulus.Position);
        if (sqrDistanceToNoise > hearingRangeSqr || sqrDistanceToNoise > effectiveRadius * effectiveRadius)
        {
            return;
        }

        // 이미 플레이어를 쫓는 중이라면, 디코이로 지정된 소리만 추적을 꺾을 수 있습니다.
        if (targetPlayer != null && !stimulus.CanInterruptChase)
        {
            return;
        }

        // 더 낮은 우선순위의 소리로 현재 조사 목표를 덮어쓰지 않도록 막습니다.
        if (hasInvestigateTarget && !stimulus.CanInterruptChase && stimulus.Priority < currentInvestigatePriority)
        {
            return;
        }

        if (stimulus.CanInterruptChase)
        {
            targetPlayer = null;
            myStatus.SetIsAttacking(false);

            // 강제 조사 시간 동안은 플레이어가 시야에 들어와도 바로 재획득하지 않습니다.
            forcedInvestigationUntilTime = Time.time + Mathf.Max(investigateDuration, stimulus.Duration);
        }

        investigateTargetPosition = stimulus.Position;
        investigateTargetAnchor = stimulus.AnchorTransform;
        hasInvestigateTarget = true;
        hasReachedInvestigatePoint = false;
        investigateSearchEndTime = 0.0f;
        currentInvestigatePriority = stimulus.Priority;
    }

    /// <summary>
    /// 현재 상황에 따라 추적, 공격, 조사, 대기 행동을 전환합니다.
    /// </summary>
    private void UpdateMovementState()
    {
        if (targetPlayer == null)
        {
            if (InvestigateNoise())
            {
                return;
            }

            SetIdleState();
            return;
        }

        ClearInvestigateTarget();

        float sqrDistToTarget = GetPlanarSqrDistance(transform.position, targetPlayer.position);

        if (myStatus.isAttacking)
        {
            return;
        }

        if (attackLengthSqr >= sqrDistToTarget)
        {
            animator.SetBool("isWalk", false);
            StartCoroutine(Attack());
        }
        else if (sqrDistToTarget <= awarenessRangeSqr)
        {
            navAgent.isStopped = false;
            navAgent.SetDestination(targetPlayer.position);
            myStatus.SetNowState(EnemyStatus.EnemyState.Chase);
            animator.SetBool("isWalk", true);
        }
        else
        {
            targetPlayer = null;
            SetIdleState();
        }
    }

    /// <summary>
    /// 마지막으로 들은 소리 위치까지 이동하고, 도착 후 잠시 그 자리에서 조사합니다.
    /// </summary>
    private bool InvestigateNoise()
    {
        if (!hasInvestigateTarget)
        {
            return false;
        }

        if (!hasReachedInvestigatePoint)
        {
            Vector3 investigateDestination = GetInvestigateDestination();
            float sqrDistanceToTarget = GetPlanarSqrDistance(transform.position, investigateDestination);
            if (sqrDistanceToTarget <= investigateStopDistanceSqr)
            {
                hasReachedInvestigatePoint = true;
                investigateSearchEndTime = Time.time + investigateDuration;

                navAgent.isStopped = true;
                navAgent.ResetPath();
                myStatus.SetNowState(EnemyStatus.EnemyState.Investigate);
                animator.SetBool("isWalk", false);
                return true;
            }

            navAgent.isStopped = false;
            navAgent.SetDestination(investigateDestination);
            myStatus.SetNowState(EnemyStatus.EnemyState.Investigate);
            animator.SetBool("isWalk", true);
            return true;
        }

        if (Time.time >= investigateSearchEndTime)
        {
            ClearInvestigateTarget();
            return false;
        }

        navAgent.isStopped = true;
        navAgent.ResetPath();
        myStatus.SetNowState(EnemyStatus.EnemyState.Investigate);
        animator.SetBool("isWalk", false);
        return true;
    }

    /// <summary>
    /// 조사 관련 상태를 모두 초기화합니다.
    /// </summary>
    private void ClearInvestigateTarget()
    {
        hasInvestigateTarget = false;
        hasReachedInvestigatePoint = false;
        investigateTargetPosition = transform.position;
        investigateTargetAnchor = null;
        investigateSearchEndTime = 0.0f;
        forcedInvestigationUntilTime = 0.0f;
        currentInvestigatePriority = -1;
    }

    private Vector3 GetInvestigateDestination()
    {
        if (investigateTargetAnchor != null)
        {
            investigateTargetPosition = investigateTargetAnchor.position;
        }

        if (NavMesh.SamplePosition(
                investigateTargetPosition,
                out NavMeshHit hit,
                investigateNavMeshSampleDistance,
                NavMesh.AllAreas))
        {
            return hit.position;
        }

        return investigateTargetPosition;
    }

    private void SetIdleState()
    {
        myStatus.SetNowState(EnemyStatus.EnemyState.Idle);
        myStatus.SetIsAttacking(false);
        animator.SetBool("isWalk", false);

        navAgent.isStopped = false;
        navAgent.ResetPath();
    }

    /// <summary>
    /// 적이 죽으면 추적 코루틴을 멈추고 사망 애니메이션만 남깁니다.
    /// </summary>
    public void Die()
    {
        StopAllCoroutines();
        animator.SetTrigger("isDead");
        Destroy(gameObject, 3.0f);
    }

    /// <summary>
    /// 공격 중에는 이동을 멈추고 대상을 바라본 뒤 애니메이션을 재생합니다.
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

        float sqrDistToTarget = GetPlanarSqrDistance(transform.position, targetPlayer.position);
        if (attackLengthSqr >= sqrDistToTarget)
        {
            targetPlayer.GetComponentInParent<IDamageable>().TakeDamage(myStatus.atkValue);
        }
    }

    /// <summary>
    /// 높이 차이를 무시한 수평 거리 비교용 헬퍼입니다.
    /// </summary>
    private static float GetPlanarSqrDistance(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        delta.y = 0.0f;
        return delta.sqrMagnitude;
    }
}
