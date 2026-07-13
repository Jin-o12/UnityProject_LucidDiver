using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적 AI의 상위 의사결정 계층입니다.
/// 감지 결과와 현재 상태를 바탕으로 조사, 복귀, 순찰, 추적, 공격 중 무엇을 할지 결정합니다.
/// </summary>
[Serializable]
public class EnemyBrain
{
    [SerializeField] private float checkInterval = 0.2f;     // AI 판단 주기

    [Header("Chase Aggro And Leash")]
    [SerializeField, Min(1f)] private float maxAggro = 100f;                  // 추적을 완전히 유지하는 최대 어그로
    [SerializeField, Min(0f)] private float sightLossGraceTime = 1f;         // 시야가 끊겨도 어그로 감소를 유예하는 시간
    [SerializeField, Min(0f)] private float aggroDecayPerSecond = 25f;       // 일반 추적 구간에서 초당 감소하는 어그로
    [SerializeField, Min(0f)] private float aggroRecoveryPerSecond = 50f;    // 추적 허용 구간에서 타겟을 볼 때 초당 회복하는 어그로
    [SerializeField, Min(0f)] private float chaseLeashDistance = 16f;        // 루트에서 이 거리까지 새 추적과 어그로 회복을 허용
    [SerializeField, Min(0f)] private float hardReturnDistance = 24f;        // 루트에서 이 거리를 넘으면 시야와 무관하게 즉시 복귀

    [NonSerialized] private WaitForSeconds checkingDelay;     // 판단 루프에서 재사용할 대기 객체
    [NonSerialized] private Transform currentTarget;          // 현재 추적 중인 플레이어
    [NonSerialized] private Transform aggroTarget;            // 강제 우선 추적 대상 (어그로)
    [NonSerialized] private float aggroEndTime;               // 강제 추적 종료 시간
    [NonSerialized] private float currentAggro;                    // 현재 추적 유지에 남아 있는 어그로
    [NonSerialized] private float sightLostStartTime = -1f;        // 시야가 최초로 끊긴 시각
    [NonSerialized] private float lastTickTime;                     // 프레임 변화와 무관한 어그로 계산용 이전 판단 시각

    /// <summary>
    /// 지정한 타겟을 일정 시간 동안 강제 어그로 대상으로 등록합니다.
    /// 실제 추적 갱신은 자기 순찰 루트의 추적 허용 구간 안에서만 이루어집니다.
    /// </summary>
    public void ApplyAggro(Transform target, float duration)
    {
        aggroTarget = target;
        aggroEndTime = Time.time + duration;
    }

    public Transform CurrentTarget => currentTarget;
    public bool HasTarget => currentTarget != null;

    public void OnValidate()
    {
        checkInterval = Mathf.Max(0.05f, checkInterval);
        maxAggro = Mathf.Max(1f, maxAggro);
        sightLossGraceTime = Mathf.Max(0f, sightLossGraceTime);
        aggroDecayPerSecond = Mathf.Max(0f, aggroDecayPerSecond);
        aggroRecoveryPerSecond = Mathf.Max(0f, aggroRecoveryPerSecond);
        chaseLeashDistance = Mathf.Max(0f, chaseLeashDistance);
        hardReturnDistance = Mathf.Max(chaseLeashDistance, hardReturnDistance);
        checkingDelay = new WaitForSeconds(checkInterval);
    }

    /// <summary>
    /// 판단 루프에서 재사용할 WaitForSeconds를 반환합니다.
    /// 코루틴 반복 시 객체를 계속 새로 만들지 않도록 캐시합니다.
    /// </summary>
    public WaitForSeconds GetCheckDelay()
    {
        if (checkingDelay == null)
        {
            checkingDelay = new WaitForSeconds(checkInterval);
        }

        return checkingDelay;
    }

    /// <summary>
    /// 현재 추적 타겟을 강제로 비웁니다.
    /// 큰 소리 조사 등으로 추적을 끊어야 할 때 호출합니다.
    /// </summary>
    public void ClearTarget()
    {
        currentTarget = null;
        currentAggro = 0f;
        sightLostStartTime = -1f;
    }

    /// <summary>
    /// 현재 감지 결과를 바탕으로 적의 다음 행동을 결정합니다.
    /// 우선순위는 공격/추적 -> 조사 -> 복귀 -> 순찰 -> 대기 순서입니다.
    /// </summary>
    public void Tick(
        MonoBehaviour host,
        Transform self,
        EnemyStatus status,
        NavMeshAgent agent,
        EnemyMemory memory,
        EnemyPerception perception,
        EnemyNoiseListener noiseListener,
        EnemyLocomotion locomotion,
        EnemyCombat combat,
        EnemyInterceptPlanner interceptPlanner,
        Action<bool> onWalkEvent,
        Action<int, int> onLookDirEvent,
        Action onAttackEvent)
    {
        float now = Time.time;
        float tickDelta = lastTickTime > 0f ? Mathf.Max(0f, now - lastTickTime) : checkInterval;
        lastTickTime = now;

        RefreshTarget(self, perception, noiseListener, memory, GlobalRuntimeData.GetPlayerList().Values);

        if (currentTarget == null)
        {
            memory.ClearTargetTracking();
            memory.ClearChasePlan();

            if (noiseListener.Investigate(self, agent, locomotion, status, onWalkEvent, onLookDirEvent))
            {
                return;
            }

            if (HandleReturnToPatrol(self, agent, status, memory, locomotion, onWalkEvent, onLookDirEvent))
            {
                return;
            }

            if (HandlePatrol(self, agent, status, memory, locomotion, onWalkEvent, onLookDirEvent))
            {
                return;
            }

            locomotion.SetIdle(agent, status, onWalkEvent);
            return;
        }

        // 플레이어를 다시 보면 조사/복귀 흐름은 즉시 끊고 추적 정보로 전환합니다.
        noiseListener.Clear();
        memory.ClearPatrolWait();

        float enemyRouteDistance = memory.GetClosestPatrolPointDistance(self.position);
        float targetRouteDistance = memory.GetClosestPatrolPointDistance(currentTarget.position);
        float farthestRouteDistance = Mathf.Max(enemyRouteDistance, targetRouteDistance);

        // 에너미 또는 타겟이 활동 한계를 넘으면 남은 어그로와 시야를 무시하고 즉시 복귀합니다.
        if (memory.HasPatrolRoute && farthestRouteDistance > hardReturnDistance)
        {
            StopChaseAndReturn(self.position, agent, status, memory, locomotion, onWalkEvent);
            return;
        }

        bool isInsideChaseArea = !memory.HasPatrolRoute || farthestRouteDistance <= chaseLeashDistance;
        bool canSeeTarget = perception.CanSeeTrackedTarget(self, currentTarget);
        if (canSeeTarget && isInsideChaseArea)
        {
            memory.UpdateTargetTracking(currentTarget);
            sightLostStartTime = -1f;
            currentAggro = Mathf.Min(maxAggro, currentAggro + aggroRecoveryPerSecond * tickDelta);
        }
        else if (!isInsideChaseArea)
        {
            // 완충 구간에서는 타겟이 보여도 어그로를 회복하지 않고 빠르게 소진합니다.
            // 이때 새 위치 샘플도 저장하지 않아 루트 바깥으로 추적 목적지가 계속 늘어나는 것을 막습니다.
            sightLostStartTime = sightLostStartTime < 0f ? now : sightLostStartTime;
            currentAggro = Mathf.Max(0f, currentAggro - aggroDecayPerSecond * 2f * tickDelta);
        }
        else
        {
            if (sightLostStartTime < 0f)
            {
                sightLostStartTime = now;
            }

            if (now - sightLostStartTime >= sightLossGraceTime)
            {
                currentAggro = Mathf.Max(0f, currentAggro - aggroDecayPerSecond * tickDelta);
            }
        }

        if (currentAggro <= 0f)
        {
            StopChaseAndReturn(self.position, agent, status, memory, locomotion, onWalkEvent);
            return;
        }

        if (status.isAttacking)
        {
            memory.ClearChasePlan();
            return;
        }

        float sqrDistToTarget = EnemyMathUtility.GetPlanarSqrDistance(self.position, currentTarget.position);
        if (canSeeTarget && isInsideChaseArea && combat.CanStartAttack(sqrDistToTarget))
        {
            memory.ClearChasePlan();
            locomotion.Stop(agent, onWalkEvent);
            host.StartCoroutine(combat.RunCombo(
                self,
                currentTarget,
                agent,
                status,
                locomotion,
                onWalkEvent,
                onLookDirEvent,
                onAttackEvent));
            return;
        }

        {
            Vector3 chaseDestination = canSeeTarget && isInsideChaseArea
                ? currentTarget.position
                : memory.LastKnownTargetPosition;
            EnemyMemory.ChaseMoveMode chaseMoveMode = EnemyMemory.ChaseMoveMode.Direct;

            if (canSeeTarget && isInsideChaseArea && interceptPlanner != null &&
                interceptPlanner.TryPlanIntercept(self, currentTarget, memory, out Vector3 interceptDestination))
            {
                chaseDestination = interceptDestination;
                chaseMoveMode = EnemyMemory.ChaseMoveMode.Intercept;
            }

            memory.SetChasePlan(chaseMoveMode, chaseDestination);
            locomotion.MoveTo(
                self,
                agent,
                chaseDestination,
                status,
                EnemyStatus.EnemyState.Chase,
                onWalkEvent,
                onLookDirEvent);
            return;
        }
    }

    /// <summary>
    /// 현재 추적 정보를 모두 정리하고 현 위치에서 가장 가까운 자기 순찰 포인트로 복귀를 예약합니다.
    /// </summary>
    private void StopChaseAndReturn(
        Vector3 currentPosition,
        NavMeshAgent agent,
        EnemyStatus status,
        EnemyMemory memory,
        EnemyLocomotion locomotion,
        Action<bool> onWalkEvent)
    {
        currentTarget = null;
        currentAggro = 0f;
        sightLostStartTime = -1f;
        memory.ClearTargetTracking();
        memory.ClearChasePlan();
        memory.MarkNeedsReturnToPatrol(currentPosition);
        locomotion.SetIdle(agent, status, onWalkEvent);
    }

    /// <summary>
    /// 기존 타겟을 계속 유지할지, 새로 시야에 들어온 플레이어를 찾을지 결정합니다.
    /// 강제 조사 중에는 즉시 시야 재획득하지 않도록 조사 모듈의 차단 규칙을 우선 적용합니다.
    /// </summary>
    private void RefreshTarget(
        Transform self,
        EnemyPerception perception,
        EnemyNoiseListener noiseListener,
        EnemyMemory memory,
        ICollection<GameObject> players)
    {
        bool hadTarget = currentTarget != null;

        // 강제 어그로도 추적 허용 구간을 벗어나면 갱신하지 않아 활동 반경 제한을 우회하지 못하게 합니다.
        if (aggroTarget != null)
        {
            if (Time.time < aggroEndTime && IsInsideChaseArea(memory, self.position, aggroTarget.position))
            {
                currentTarget = aggroTarget;
                currentAggro = maxAggro;
                if (!hadTarget)
                {
                    memory.CaptureReturnAnchor(self.position);
                }
                noiseListener.Clear();
                memory.ClearPatrolWait();
                return;
            }
            else
            {
                aggroTarget = null; // 지속 시간 종료 또는 타겟 파괴
            }
        }

        if (noiseListener.ShouldBlockSightReacquire())
        {
            currentTarget = null;
            return;
        }

        if (currentTarget != null)
        {
            return;
        }

        // 복귀 완충 구간에서는 새 타겟을 획득하지 않아 복귀와 재추적이 매 틱 반복되는 현상을 막습니다.
        if (!IsInsideChaseArea(memory, self.position))
        {
            return;
        }

        currentTarget = perception.FindVisibleTarget(self, players);
        if (currentTarget != null && !IsInsideChaseArea(memory, self.position, currentTarget.position))
        {
            currentTarget = null;
        }

        if (currentTarget != null)
        {
            currentAggro = maxAggro;
            sightLostStartTime = -1f;
            // 순찰이나 복귀 상태에서 처음 전투로 전환될 때만 복귀 기준점을 저장합니다.
            if (!hadTarget)
            {
                memory.CaptureReturnAnchor(self.position);
            }

            noiseListener.Clear();
            memory.ClearPatrolWait();
        }
    }

    /// <summary>
    /// 에너미와 선택적 타겟이 모두 자기 순찰 루트의 추적 허용 반경 안에 있는지 확인합니다.
    /// 루트가 없는 기존 에너미는 이전 동작을 유지하도록 거리 제한을 적용하지 않습니다.
    /// </summary>
    private bool IsInsideChaseArea(EnemyMemory memory, Vector3 enemyPosition, Vector3? targetPosition = null)
    {
        if (memory == null || !memory.HasPatrolRoute)
        {
            return true;
        }

        if (memory.GetClosestPatrolPointDistance(enemyPosition) > chaseLeashDistance)
        {
            return false;
        }

        return !targetPosition.HasValue ||
               memory.GetClosestPatrolPointDistance(targetPosition.Value) <= chaseLeashDistance;
    }

    /// <summary>
    /// 조사나 추적이 끝난 뒤 현재 순찰 이탈 기준점으로 복귀합니다.
    /// 복귀가 끝나면 다시 Patrol 상태로 돌아가 직전 순찰 흐름을 이어갑니다.
    /// </summary>
    private bool HandleReturnToPatrol(
        Transform self,
        NavMeshAgent agent,
        EnemyStatus status,
        EnemyMemory memory,
        EnemyLocomotion locomotion,
        Action<bool> onWalkEvent,
        Action<int, int> onLookDirEvent)
    {
        if (memory == null || !memory.NeedsReturnToPatrol || !memory.HasPatrolRoute)
        {
            return false;
        }

        Vector3 returnDestination = memory.ReturnAnchorPosition;
        if (locomotion.HasReachedDestination(self.position, returnDestination, memory.GetPointReachDistance()))
        {
            locomotion.Stop(agent, onWalkEvent);
            status.SetNowState(EnemyStatus.EnemyState.Patrol);
            memory.CompleteReturnToPatrol();
            return true;
        }

        memory.ClearPatrolWait();
        locomotion.MoveTo(
            self,
            agent,
            returnDestination,
            status,
            EnemyStatus.EnemyState.Return,
            onWalkEvent,
            onLookDirEvent);
        return true;
    }

    /// <summary>
    /// 타겟도 조사 상태도 없을 때 기본 순찰 루트를 따라 이동합니다.
    /// 포인트에 도착하면 설정된 시간만큼 대기한 뒤 다음 포인트로 넘어갑니다.
    /// </summary>
    private bool HandlePatrol(
        Transform self,
        NavMeshAgent agent,
        EnemyStatus status,
        EnemyMemory memory,
        EnemyLocomotion locomotion,
        Action<bool> onWalkEvent,
        Action<int, int> onLookDirEvent)
    {
        if (memory == null || !memory.HasPatrolRoute)
        {
            return false;
        }

        Transform patrolPoint = memory.GetCurrentPatrolPoint();
        if (patrolPoint == null)
        {
            return false;
        }

        if (!memory.HasPatrolDestination && !memory.TryCreatePatrolDestination(agent))
        {
            locomotion.SetIdle(agent, status, onWalkEvent);
            return true;
        }

        Vector3 patrolDestination = memory.PatrolDestination;
        if (locomotion.HasReachedDestination(self.position, patrolDestination, memory.GetPointReachDistance()))
        {
            locomotion.Stop(agent, onWalkEvent);
            status.SetNowState(EnemyStatus.EnemyState.Patrol);

            if (!memory.IsWaitingAtPatrolPoint)
            {
                memory.BeginPatrolWait(memory.GetRandomPatrolWaitTime());
                return true;
            }

            if (memory.HasCompletedPatrolWait())
            {
                memory.ClearPatrolWait();
                memory.CompleteWanderDestination();
                if (memory.ShouldAdvancePatrolPoint)
                {
                    memory.AdvanceToRandomNearbyPoint();
                }
            }

            return true;
        }

        memory.ClearPatrolWait();
        locomotion.MoveTo(
            self,
            agent,
            patrolDestination,
            status,
            EnemyStatus.EnemyState.Patrol,
            onWalkEvent,
            onLookDirEvent);
        return true;
    }
}
