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

    [NonSerialized] private WaitForSeconds checkingDelay;     // 판단 루프에서 재사용할 대기 객체
    [NonSerialized] private Transform currentTarget;          // 현재 추적 중인 플레이어
    [NonSerialized] private Transform aggroTarget;            // 강제 우선 추적 대상 (어그로)
    [NonSerialized] private float aggroEndTime;               // 강제 추적 종료 시간

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
        memory.UpdateTargetTracking(currentTarget);

        if (status.isAttacking)
        {
            memory.ClearChasePlan();
            return;
        }

        float sqrDistToTarget = EnemyMathUtility.GetPlanarSqrDistance(self.position, currentTarget.position);
        if (combat.CanStartAttack(sqrDistToTarget))
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

        if (perception.CanKeepAwareness(self.position, currentTarget))
        {
            Vector3 chaseDestination = currentTarget.position;
            EnemyMemory.ChaseMoveMode chaseMoveMode = EnemyMemory.ChaseMoveMode.Direct;

            if (interceptPlanner != null &&
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

        currentTarget = null;
        memory.ClearTargetTracking();
        memory.ClearChasePlan();
        memory.MarkNeedsReturnToPatrol();
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

        // 1. 어그로 타겟이 유효한 경우 최우선 타겟으로 덮어씌움
        if (aggroTarget != null)
        {
            if (Time.time < aggroEndTime)
            {
                currentTarget = aggroTarget;
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

        if (currentTarget != null && perception.CanKeepAwareness(self.position, currentTarget))
        {
            return;
        }

        currentTarget = perception.FindVisibleTarget(self, players);
        if (currentTarget != null)
        {
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

        if (locomotion.HasReachedDestination(self.position, patrolPoint.position, memory.GetPointReachDistance()))
        {
            locomotion.Stop(agent, onWalkEvent);
            status.SetNowState(EnemyStatus.EnemyState.Patrol);

            if (!memory.IsWaitingAtPatrolPoint)
            {
                memory.BeginPatrolWait();

                if (memory.HasCompletedPatrolWait())
                {
                    memory.AdvancePatrolIndex();
                }

                return true;
            }

            if (memory.HasCompletedPatrolWait())
            {
                memory.ClearPatrolWait();
                memory.AdvancePatrolIndex();
            }

            return true;
        }

        memory.ClearPatrolWait();
        locomotion.MoveTo(
            self,
            agent,
            patrolPoint.position,
            status,
            EnemyStatus.EnemyState.Patrol,
            onWalkEvent,
            onLookDirEvent);
        return true;
    }
}
