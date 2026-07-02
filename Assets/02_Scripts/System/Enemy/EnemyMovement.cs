using System.Collections;
using UnityEngine;
using UnityEngine.AI;

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

    private Transform targetPlayer;
    private float sightLengthSqr;
    private float awarenessRangeSqr;
    private float halfSightAngle;
    private float attackLengthSqr;
    private WaitForSeconds checkingTime;

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

        if (animator == null || navAgent == null || myStatus == null)
        {
            enabled = false;
            Debug.LogError("EnemyMovement: required components are missing.");
            return;
        }

        targetPlayer = null;
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

    private void ApplyCachedValues()
    {
        sightLength = Mathf.Max(0.0f, sightLength);
        awarenessRange = Mathf.Max(sightLength, awarenessRange);
        hearingRange = Mathf.Max(awarenessRange, hearingRange);
        sightAngle = Mathf.Clamp(sightAngle, 0.0f, 360.0f);
        attackLength = Mathf.Max(0.0f, attackLength);
        checkInterval = Mathf.Max(0.05f, checkInterval);

        sightLengthSqr = sightLength * sightLength;
        awarenessRangeSqr = awarenessRange * awarenessRange;
        halfSightAngle = sightAngle * 0.5f;
        attackLengthSqr = attackLength * attackLength;
        checkingTime = new WaitForSeconds(checkInterval);
    }

    private IEnumerator CheckRoutine()
    {
        while (myStatus.nowState != EnemyStatus.EnemyState.Dead)
        {
            UpdateTarget();
            ChaseTarget();
            yield return checkingTime;
        }
    }

    private void UpdateTarget()
    {
        if (targetPlayer != null && IsTargetWithinAwareness(targetPlayer))
        {
            return;
        }

        targetPlayer = FindVisibleTarget();
    }

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

    private bool IsTargetWithinAwareness(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        return GetPlanarSqrDistance(transform.position, target.position) <= awarenessRangeSqr;
    }

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
    }

    public void Die()
    {
        StopAllCoroutines();
        animator.SetTrigger("isDead");
        Destroy(gameObject, 3.0f);
    }

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

    private static float GetPlanarSqrDistance(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        delta.y = 0.0f;
        return delta.sqrMagnitude;
    }
}
