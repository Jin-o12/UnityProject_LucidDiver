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

    public Transform CurrentTarget => currentTarget;
    public bool HasTarget => currentTarget != null;

    public void OnValidate()
    {
        checkInterval = Mathf.Max(0.05f, checkInterval);
        checkingDelay = new WaitForSeconds(checkInterval);
    }

    /// <summary>
    /// 판단 루프에서 사용할 WaitForSeconds를 반환합니다.
    /// 코루틴에서 새 객체를 반복 생성하지 않기 위해 캐시된 값을 재사용합니다.
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
    /// 현재 추적 대상을 강제로 비웁니다.
    /// 강한 디코이 소리로 추적이 끊기는 상황에서 사용합니다.
    /// </summary>
    public void ClearTarget()
    {
        currentTarget = null;
    }

    /// <summary>
    /// 현재 감지 결과를 바탕으로 적의 다음 행동을 결정합니다.
    /// 우선순위는 타겟 추적/공격 -> 조사 -> 복귀 -> 순찰 -> 대기 순서입니다.
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
        Action<bool> onWalkEvent,
        Action<int, int> onLookDirEvent,
        Action onAttackEvent)
    {
        RefreshTarget(self, perception, noiseListener, memory, GlobalRuntimeData.GetPlayerList().Values);

        if (currentTarget == null)
        {
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

        // 플레이어를 다시 보게 되면 조사/복귀 흐름은 중단합니다.
        noiseListener.Clear();
        memory.ClearPatrolWait();

        if (status.isAttacking)
        {
            return;
        }

        float sqrDistToTarget = EnemyMathUtility.GetPlanarSqrDistance(self.position, currentTarget.position);
        if (combat.CanStartAttack(sqrDistToTarget))
        {
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
            locomotion.MoveTo(
                self,
                agent,
                currentTarget.position,
                status,
                EnemyStatus.EnemyState.Chase,
                onWalkEvent,
                onLookDirEvent);
            return;
        }

        currentTarget = null;
        memory.MarkNeedsReturnToPatrol();
        locomotion.SetIdle(agent, status, onWalkEvent);
    }

    /// <summary>
    /// 기존 대상을 유지할지, 새로 시야 내 플레이어를 찾을지 결정합니다.
    /// 강제 조사 시간 중에는 일부러 시야 재획득을 막아 소리에 반응하는 느낌을 살립니다.
    /// </summary>
    private void RefreshTarget(
        Transform self,
        EnemyPerception perception,
        EnemyNoiseListener noiseListener,
        EnemyMemory memory,
        ICollection<GameObject> players)
    {
        bool hadTarget = currentTarget != null;

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
            // 순찰이나 복귀 흐름에서 처음 벗어난 지점만 복귀 기준점으로 저장합니다.
            if (!hadTarget)
            {
                memory.CaptureReturnAnchor(self.position);
            }

            noiseListener.Clear();
            memory.ClearPatrolWait();
        }
    }

    /// <summary>
    /// 조사나 추적이 끝난 뒤 현재 순찰 포인트로 복귀합니다.
    /// 복귀가 끝나면 다시 Patrol 상태로 넘겨 이후 순찰을 이어서 계속하게 만듭니다.
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

        // 현재 순찰 포인트가 아니라, 순찰에서 이탈했던 시작 지점으로 복귀합니다.
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
    /// 포인트 도착 시 잠시 기다렸다가 다음 포인트로 넘어갑니다.
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
