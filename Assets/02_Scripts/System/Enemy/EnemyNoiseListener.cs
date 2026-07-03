using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 소음 자극을 조사 상태로 변환하는 모듈입니다.
/// 마지막으로 들은 위치와 조사 유지 시간을 기억하고, 필요하면 추적을 끊고 조사로 전환합니다.
/// </summary>
[Serializable]
public class EnemyNoiseListener
{
    [SerializeField] private float investigateStopDistance = 1.0f;           // 조사 지점에 도착했다고 보는 거리
    [SerializeField] private float investigateDuration = 2.0f;               // 도착 후 주변을 살피는 시간
    [SerializeField] private float investigateNavMeshSampleDistance = 2.5f;  // 유효한 NavMesh 위치 보정 반경

    [NonSerialized] private float investigateStopDistanceSqr;                // 도착 판정용 제곱 거리
    [NonSerialized] private bool hasInvestigateTarget;                       // 현재 조사 대상이 있는지 여부
    [NonSerialized] private bool hasReachedInvestigatePoint;                 // 조사 위치까지 도착했는지 여부
    [NonSerialized] private Vector3 investigateTargetPosition;               // 조사할 월드 좌표
    [NonSerialized] private Transform investigateTargetAnchor;               // 움직이는 소음 기준점이 있으면 그 Transform
    [NonSerialized] private float investigateSearchEndTime;                  // 조사 종료 시각
    [NonSerialized] private float forcedInvestigationUntilTime;              // 강제 조사 유지 시각
    [NonSerialized] private int currentInvestigatePriority = -1;             // 현재 조사 대상 우선순위

    public void OnValidate()
    {
        investigateStopDistance = Mathf.Max(0.1f, investigateStopDistance);
        investigateDuration = Mathf.Max(0.1f, investigateDuration);
        investigateNavMeshSampleDistance = Mathf.Max(0.5f, investigateNavMeshSampleDistance);
        investigateStopDistanceSqr = investigateStopDistance * investigateStopDistance;
    }

    /// <summary>
    /// 강한 소음에 반응한 직후에는 즉시 다시 시야 타겟을 잡지 않도록 잠시 막아 둡니다.
    /// </summary>
    public bool ShouldBlockSightReacquire()
    {
        return hasInvestigateTarget && Time.time < forcedInvestigationUntilTime;
    }

    /// <summary>
    /// 새 소음 자극을 등록하고, 필요하면 현재 추적을 끊고 조사 상태로 전환합니다.
    /// </summary>
    public bool TryRegisterNoise(
        EnemyPerception perception,
        Vector3 selfPosition,
        NoiseStimulus stimulus,
        bool hasChaseTarget,
        EnemyStatus status,
        out bool interruptedChase)
    {
        interruptedChase = false;

        if (!perception.CanHear(selfPosition, stimulus))
        {
            return false;
        }

        if (hasChaseTarget && !stimulus.CanInterruptChase)
        {
            return false;
        }

        if (hasInvestigateTarget && !stimulus.CanInterruptChase && stimulus.Priority < currentInvestigatePriority)
        {
            return false;
        }

        if (stimulus.CanInterruptChase)
        {
            interruptedChase = true;
            status.SetIsAttacking(false);
            forcedInvestigationUntilTime = Time.time + Mathf.Max(investigateDuration, stimulus.Duration);
        }

        investigateTargetPosition = stimulus.Position;
        investigateTargetAnchor = stimulus.AnchorTransform;
        hasInvestigateTarget = true;
        hasReachedInvestigatePoint = false;
        investigateSearchEndTime = 0.0f;
        currentInvestigatePriority = stimulus.Priority;
        return true;
    }

    /// <summary>
    /// 등록된 소음 위치로 이동하고, 도착 후에는 짧게 조사 상태를 유지합니다.
    /// </summary>
    public bool Investigate(
        Transform self,
        NavMeshAgent agent,
        EnemyLocomotion locomotion,
        EnemyStatus status,
        Action<bool> onWalkEvent,
        Action<int, int> onLookDirEvent)
    {
        if (!hasInvestigateTarget)
        {
            return false;
        }

        if (!hasReachedInvestigatePoint)
        {
            Vector3 destination = GetInvestigateDestination();
            float sqrDistance = EnemyMathUtility.GetPlanarSqrDistance(self.position, destination);
            if (sqrDistance <= investigateStopDistanceSqr)
            {
                hasReachedInvestigatePoint = true;
                investigateSearchEndTime = Time.time + investigateDuration;
                locomotion.Stop(agent, onWalkEvent);
                status.SetNowState(EnemyStatus.EnemyState.Investigate);
                return true;
            }

            locomotion.MoveTo(
                self,
                agent,
                destination,
                status,
                EnemyStatus.EnemyState.Investigate,
                onWalkEvent,
                onLookDirEvent);
            return true;
        }

        if (Time.time >= investigateSearchEndTime)
        {
            Clear();
            return false;
        }

        locomotion.Stop(agent, onWalkEvent);
        status.SetNowState(EnemyStatus.EnemyState.Investigate);
        return true;
    }

    /// <summary>
    /// 현재 조사 상태를 모두 초기화합니다.
    /// </summary>
    public void Clear()
    {
        hasInvestigateTarget = false;
        hasReachedInvestigatePoint = false;
        investigateTargetPosition = Vector3.zero;
        investigateTargetAnchor = null;
        investigateSearchEndTime = 0.0f;
        forcedInvestigationUntilTime = 0.0f;
        currentInvestigatePriority = -1;
    }

    /// <summary>
    /// 움직이는 기준점이 있으면 최신 위치를 따라가고, NavMesh 위의 유효한 좌표로 보정합니다.
    /// </summary>
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
}
