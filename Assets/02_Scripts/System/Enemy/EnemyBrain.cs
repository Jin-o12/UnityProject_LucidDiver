using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적 AI의 상위 의사결정 계층입니다.
/// 감지 결과와 현재 상태를 바탕으로 대기, 조사, 추적, 공격 중 무엇을 할지 결정합니다.
/// </summary>
[Serializable]
public class EnemyBrain
{
    [SerializeField] private float checkInterval = 0.2f;     // AI 판단 주기

    [NonSerialized] private WaitForSeconds checkingDelay;     // 판단 루프 재사용용 대기 객체
    [NonSerialized] private Transform currentTarget;          // 현재 추적 중인 플레이어

    public Transform CurrentTarget => currentTarget;
    public bool HasTarget => currentTarget != null;

    public void OnValidate()
    {
        checkInterval = Mathf.Max(0.05f, checkInterval);
        checkingDelay = new WaitForSeconds(checkInterval);
    }

    /// <summary>
    /// 판단 루프에서 재사용할 WaitForSeconds를 반환합니다.
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
    /// </summary>
    public void ClearTarget()
    {
        currentTarget = null;
    }

    /// <summary>
    /// 현재 감지 결과를 바탕으로 적의 다음 행동을 결정합니다.
    /// </summary>
    public void Tick(
        MonoBehaviour host, // 코루틴을 실행할 MonoBehaviour
        Transform self, // 적 자신의 Transform
        EnemyStatus status, // 적의 상태 정보
        NavMeshAgent agent, // 적의 NavMeshAgent
        EnemyPerception perception, // 적의 시야 감지
        EnemyNoiseListener noiseListener, // 적의 소리 감지
        EnemyLocomotion locomotion, // 적의 이동 처리
        EnemyCombat combat, // 적의 전투 처리
        Action<bool> onWalkEvent, // 이동 시작/종료 이벤트
        Action<int, int> onLookDirEvent, // 시선 방향 변경 이벤트
        Action onAttackEvent) // 공격 시작 이벤트
    {
        RefreshTarget(self, perception, noiseListener, GlobalRuntimeData.GetPlayerList().Values);

        if (currentTarget == null)
        {
            if (noiseListener.Investigate(self, agent, locomotion, status, onWalkEvent, onLookDirEvent))
            {
                return;
            }

            locomotion.SetIdle(agent, status, onWalkEvent);
            return;
        }

        // 플레이어를 다시 확보했으면 조사 상태는 종료합니다.
        noiseListener.Clear();

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
        locomotion.SetIdle(agent, status, onWalkEvent);
    }

    /// <summary>
    /// 기존 타겟을 유지할 수 있는지 확인하고, 아니면 새로 시야 내 대상을 찾습니다.
    /// </summary>
    private void RefreshTarget(
        Transform self,
        EnemyPerception perception,
        EnemyNoiseListener noiseListener,
        ICollection<GameObject> players)
    {
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
            noiseListener.Clear();
        }
    }
}
