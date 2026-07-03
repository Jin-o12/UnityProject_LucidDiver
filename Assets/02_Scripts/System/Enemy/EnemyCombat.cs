using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적의 전투 흐름을 담당하는 모듈입니다.
/// 공격 시작 가능 여부, 2연격 타이밍, 타격 거리, 데미지 적용을 한 곳에서 관리합니다.
/// </summary>
[Serializable]
public class EnemyCombat
{
    [SerializeField] private float attackStartRange = 3.0f;            // 공격 코루틴을 시작할 수 있는 거리
    [SerializeField] private float attackHitRange = 1.2f;              // 실제 데미지가 들어가는 근접 판정 거리
    [SerializeField] private float attackCooldown = 2.0f;              // 한 번의 콤보 후 다시 공격 가능한 시간

    [SerializeField] private float firstSlashTelegraphTime = 0.35f;    // 공격 애니메이션을 시작하기 전 선딜 시간
    [SerializeField] private float attackAnimationDuration = 1.3333f;  // 공격 애니메이션 1회 전체 길이
    [SerializeField] private float attackRecoveryTime = 0.65f;         // 2타 종료 후 회복 시간

    [SerializeField] private float firstSlashLungeDistance = 1.0f;     // 1타 돌진 거리
    [SerializeField] private float secondSlashLungeDistance = 1.2f;    // 2타 돌진 거리
    [SerializeField] private float firstSlashLungeDuration = 0.15f;    // 1타 돌진 시간
    [SerializeField] private float secondSlashLungeDuration = 0.18f;   // 2타 돌진 시간

    [SerializeField] private float firstSlashDamage = 8.0f;            // 1타 피해량
    [SerializeField] private float secondSlashDamage = 12.0f;          // 2타 피해량
    [SerializeField] private float secondSlashTurnLimit = 25.0f;       // 2타 직전 보정 가능한 최대 회전 각도

    [NonSerialized] private float attackStartRangeSqr;                 // 공격 시작 거리 제곱값 캐시
    [NonSerialized] private float attackHitRangeSqr;                   // 실제 타격 거리 제곱값 캐시
    [NonSerialized] private float nextAttackAvailableTime;             // 다음 공격 가능 시각
    [NonSerialized] private float currentSlashDamage;                  // 현재 공격 단계에서 적용할 피해량
    [NonSerialized] private bool hasAppliedCurrentSlashDamage;         // 현재 타격 단계 피해 적용 여부

    public void OnValidate()
    {
        attackStartRange = Mathf.Max(0.1f, attackStartRange);
        attackHitRange = Mathf.Max(0.1f, attackHitRange);
        attackCooldown = Mathf.Max(0.0f, attackCooldown);
        firstSlashTelegraphTime = Mathf.Max(0.0f, firstSlashTelegraphTime);
        attackAnimationDuration = Mathf.Max(0.1f, attackAnimationDuration);
        attackRecoveryTime = Mathf.Max(0.0f, attackRecoveryTime);
        firstSlashLungeDistance = Mathf.Max(0.0f, firstSlashLungeDistance);
        secondSlashLungeDistance = Mathf.Max(0.0f, secondSlashLungeDistance);
        firstSlashLungeDuration = Mathf.Max(0.0f, firstSlashLungeDuration);
        secondSlashLungeDuration = Mathf.Max(0.0f, secondSlashLungeDuration);
        firstSlashDamage = Mathf.Max(0.0f, firstSlashDamage);
        secondSlashDamage = Mathf.Max(0.0f, secondSlashDamage);
        secondSlashTurnLimit = Mathf.Clamp(secondSlashTurnLimit, 0.0f, 180.0f);

        attackStartRangeSqr = attackStartRange * attackStartRange;
        attackHitRangeSqr = attackHitRange * attackHitRange;
    }

    /// <summary>
    /// 현재 대상과의 거리, 쿨다운 기준으로 공격 시작 가능 여부를 판정합니다.
    /// </summary>
    public bool CanStartAttack(float sqrDistToTarget)
    {
        return Time.time >= nextAttackAvailableTime && sqrDistToTarget <= attackStartRangeSqr;
    }

    /// <summary>
    /// 공격 애니메이션은 한 번만 재생하고, 실제 1타/2타는 애니메이션 이벤트가 호출합니다.
    /// </summary>
    public IEnumerator RunCombo(
        Transform self,
        Transform target,
        NavMeshAgent agent,
        EnemyStatus status,
        EnemyLocomotion locomotion,
        Action<bool> onWalkEvent,
        Action<int, int> onLookDirEvent,
        Action onAttackEvent)
    {
        if (target == null)
        {
            yield break;
        }

        nextAttackAvailableTime = Time.time + attackCooldown;
        status.SetNowState(EnemyStatus.EnemyState.Attack);
        status.SetIsAttacking(true);

        locomotion.Stop(agent, onWalkEvent);
        locomotion.FacePosition(self, target.position, onLookDirEvent);
        ClearRuntimeState();

        if (firstSlashTelegraphTime > 0.0f)
        {
            yield return new WaitForSeconds(firstSlashTelegraphTime);
        }

        onAttackEvent?.Invoke();

        float comboRecovery = attackAnimationDuration + attackRecoveryTime;
        if (comboRecovery > 0.0f)
        {
            yield return new WaitForSeconds(comboRecovery);
        }

        ClearRuntimeState();
        status.SetIsAttacking(false);

        if (agent != null)
        {
            agent.isStopped = false;
        }
    }

    /// <summary>
    /// 애니메이션 이벤트로 들어온 스윙 번호에 따라 돌진 거리와 데미지를 분리 적용합니다.
    /// 이벤트 시점은 "팔이 나가기 직전"에 두는 편이 자연스럽습니다.
    /// </summary>
    public IEnumerator ExecuteSwing(
        Transform self,
        Transform target,
        Transform attackOrigin,
        NavMeshAgent agent,
        EnemyLocomotion locomotion,
        Action<int, int> onLookDirEvent,
        int swingIndex)
    {
        if (self == null || target == null)
        {
            yield break;
        }

        float swingDamage = swingIndex == 0 ? firstSlashDamage : secondSlashDamage;
        float lungeDistance = swingIndex == 0 ? firstSlashLungeDistance : secondSlashLungeDistance;
        float lungeDuration = swingIndex == 0 ? firstSlashLungeDuration : secondSlashLungeDuration;

        if (swingIndex > 0)
        {
            locomotion.FacePositionLimited(self, target.position, secondSlashTurnLimit, onLookDirEvent);
        }

        currentSlashDamage = swingDamage;
        hasAppliedCurrentSlashDamage = false;

        if (lungeDistance > 0.0f && lungeDuration > 0.0f)
        {
            yield return locomotion.PerformLunge(self, agent, lungeDistance, lungeDuration);
        }

        TryApplyCurrentSlashDamage(self, target, attackOrigin);
    }

    /// <summary>
    /// 현재 열린 타격 단계에 맞춰 데미지를 한 번만 적용합니다.
    /// </summary>
    public void TryApplyCurrentSlashDamage(Transform self, Transform target, Transform attackOrigin)
    {
        if (hasAppliedCurrentSlashDamage || currentSlashDamage <= 0.0f || target == null)
        {
            return;
        }

        Vector3 origin = attackOrigin != null ? attackOrigin.position : self.position;
        if (EnemyMathUtility.GetPlanarSqrDistance(origin, target.position) > attackHitRangeSqr)
        {
            return;
        }

        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            return;
        }

        damageable.TakeDamage(currentSlashDamage);
        hasAppliedCurrentSlashDamage = true;
    }

    /// <summary>
    /// 현재 공격 단계에서 남아 있던 런타임 상태를 정리합니다.
    /// </summary>
    public void ClearRuntimeState()
    {
        currentSlashDamage = 0.0f;
        hasAppliedCurrentSlashDamage = false;
    }
}
