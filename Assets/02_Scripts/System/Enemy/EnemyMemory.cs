using System;
using UnityEngine;

/// <summary>
/// 적 AI가 순찰과 추적 중에 기억해야 하는 런타임 상태를 보관합니다.
/// 순찰 루트, 복귀 기준점, 플레이어 최근 이동 샘플, 현재 추적 방식까지 한곳에서 관리합니다.
/// </summary>
[Serializable]
public class EnemyMemory
{
    public enum ChaseMoveMode
    {
        Direct,
        Intercept
    }

    [SerializeField] private Vector3 homePosition;                    // 적이 처음 스폰된 기준 위치
    [SerializeField] private EnemyPatrolRoute patrolRoute;            // 현재 연결된 순찰 루트
    [SerializeField, Min(0)] private int patrolIndex;                 // 현재 향하고 있는 순찰 포인트 인덱스
    [SerializeField] private bool needsReturnToPatrol;                // 조사/추적 종료 후 순찰로 복귀해야 하는지 여부
    [SerializeField] private bool isWaitingAtPatrolPoint;             // 순찰 포인트에 도착해 대기 중인지 여부
    [SerializeField] private float patrolWaitUntilTime;               // 순찰 포인트 대기가 끝나는 시각
    [SerializeField] private bool hasReturnAnchor;                    // 순찰에서 이탈한 기준점을 이미 기록했는지 여부
    [SerializeField] private Vector3 returnAnchorPosition;            // 복귀해야 할 순찰 이탈 시작 위치
    [SerializeField, Min(0)] private int returnPatrolIndex;           // 복귀 후 다시 이어갈 순찰 포인트 인덱스

    [Header("Target Tracking")]
    [SerializeField] private Vector3 lastKnownTargetPosition;         // 최근에 관측한 플레이어 위치
    [SerializeField] private Vector3 estimatedTargetVelocity;         // 최근 샘플 차이로 추정한 플레이어 이동 속도
    [SerializeField] private float lastTargetSampleTime;              // 마지막 위치 샘플 시각
    [SerializeField] private bool hasTargetMotionSample;              // 두 번 이상 샘플링해 속도 추정이 가능한지 여부
    [SerializeField] private int trackedTargetInstanceId;             // 현재 추적 중인 타겟 인스턴스 ID
    [SerializeField] private ChaseMoveMode currentChaseMoveMode;      // 현재 추적이 직선 추적인지 차단 추적인지
    [SerializeField] private Vector3 currentChaseDestination;         // 현재 프레임에 목표로 삼은 추적 목적지

    public Vector3 HomePosition => homePosition;
    public int PatrolIndex => patrolIndex;
    public bool NeedsReturnToPatrol => needsReturnToPatrol;
    public bool IsWaitingAtPatrolPoint => isWaitingAtPatrolPoint;
    public bool HasPatrolRoute => patrolRoute != null && patrolRoute.GetPointCount() > 0;
    public bool HasReturnAnchor => hasReturnAnchor;
    public Vector3 ReturnAnchorPosition => hasReturnAnchor ? returnAnchorPosition : homePosition;
    public Vector3 LastKnownTargetPosition => lastKnownTargetPosition;
    public Vector3 EstimatedTargetVelocity => estimatedTargetVelocity;
    public bool HasTargetMotionSample => hasTargetMotionSample;
    public ChaseMoveMode CurrentChaseMoveMode => currentChaseMoveMode;
    public Vector3 CurrentChaseDestination => currentChaseDestination;

    /// <summary>
    /// 스폰 직후 순찰과 추적 관련 초기 상태를 설정합니다.
    /// 순찰 루트가 없더라도 홈 위치와 추적 메모리는 초기화해 이후 상태 전환이 꼬이지 않도록 맞춥니다.
    /// </summary>
    public void InitializePatrol(Vector3 spawnPosition, EnemyPatrolRoute route, int startPatrolIndex)
    {
        homePosition = spawnPosition;
        patrolRoute = route;
        patrolIndex = 0;
        needsReturnToPatrol = false;
        isWaitingAtPatrolPoint = false;
        patrolWaitUntilTime = 0.0f;
        hasReturnAnchor = false;
        returnAnchorPosition = spawnPosition;
        returnPatrolIndex = 0;

        ClearTargetTracking();
        ClearChasePlan();
        currentChaseDestination = spawnPosition;

        if (!HasPatrolRoute)
        {
            return;
        }

        patrolIndex = Mathf.Clamp(startPatrolIndex, 0, patrolRoute.GetPointCount() - 1);
        returnPatrolIndex = patrolIndex;
    }

    /// <summary>
    /// 현재 순찰 인덱스의 포인트를 반환합니다.
    /// </summary>
    public Transform GetCurrentPatrolPoint()
    {
        if (!HasPatrolRoute)
        {
            return null;
        }

        return patrolRoute.GetPoint(patrolIndex);
    }

    /// <summary>
    /// 현재 루트가 사용하는 포인트 도착 판정 거리를 반환합니다.
    /// </summary>
    public float GetPointReachDistance()
    {
        return patrolRoute != null ? patrolRoute.PointReachDistance : 0.5f;
    }

    /// <summary>
    /// 현재 루트가 사용하는 포인트 대기 시간을 반환합니다.
    /// </summary>
    public float GetWaitTimeAtPoint()
    {
        return patrolRoute != null ? patrolRoute.WaitTimeAtPoint : 0.0f;
    }

    /// <summary>
    /// 적이 순찰 루트에서 처음 벗어나는 시점의 위치와 순찰 인덱스를 저장합니다.
    /// 이미 이탈 기준점을 저장한 상태라면 기존 값을 유지해 복귀 기준점이 흔들리지 않게 합니다.
    /// </summary>
    public void CaptureReturnAnchor(Vector3 currentPosition)
    {
        if (!HasPatrolRoute || hasReturnAnchor)
        {
            return;
        }

        hasReturnAnchor = true;
        returnAnchorPosition = currentPosition;
        returnPatrolIndex = patrolIndex;
    }

    /// <summary>
    /// 조사나 추적이 끝난 뒤 순찰 루트로 복귀해야 한다는 플래그를 기록합니다.
    /// 복귀 시작 시 순찰 포인트 대기는 다시 계산해야 하므로 대기 상태도 함께 초기화합니다.
    /// </summary>
    public void MarkNeedsReturnToPatrol()
    {
        if (!HasPatrolRoute)
        {
            return;
        }

        needsReturnToPatrol = true;
        ClearPatrolWait();
    }

    /// <summary>
    /// 복귀 관련 플래그와 기준점을 모두 초기화합니다.
    /// </summary>
    public void ClearReturnToPatrol()
    {
        needsReturnToPatrol = false;
        hasReturnAnchor = false;
        returnAnchorPosition = homePosition;
        returnPatrolIndex = patrolIndex;
    }

    /// <summary>
    /// 복귀 목적지까지 도달한 뒤 원래 순찰 흐름을 다시 이어갈 수 있도록 정리합니다.
    /// 이탈 직전에 바라보던 순찰 인덱스를 복원하고, 복귀 관련 플래그와 대기 상태를 함께 비웁니다.
    /// </summary>
    public void CompleteReturnToPatrol()
    {
        if (HasPatrolRoute)
        {
            patrolIndex = Mathf.Clamp(returnPatrolIndex, 0, patrolRoute.GetPointCount() - 1);
        }

        ClearPatrolWait();
        ClearReturnToPatrol();
    }

    /// <summary>
    /// 현재 순찰 포인트에서의 대기 타이머를 시작합니다.
    /// 루트 설정상 대기 시간이 0이면 즉시 다음 포인트로 넘어갈 수 있도록 대기 상태를 만들지 않습니다.
    /// </summary>
    public void BeginPatrolWait()
    {
        float waitDuration = GetWaitTimeAtPoint();
        if (waitDuration <= 0.0f)
        {
            isWaitingAtPatrolPoint = false;
            patrolWaitUntilTime = 0.0f;
            return;
        }

        isWaitingAtPatrolPoint = true;
        patrolWaitUntilTime = Time.time + waitDuration;
    }

    /// <summary>
    /// 현재 순찰 포인트 대기 시간이 끝났는지 확인합니다.
    /// </summary>
    public bool HasCompletedPatrolWait()
    {
        return !isWaitingAtPatrolPoint || Time.time >= patrolWaitUntilTime;
    }

    /// <summary>
    /// 순찰 포인트 대기 상태를 초기화합니다.
    /// 조사 진입, 복귀 시작, 추적 전환 시 대기 타이머가 남아 있지 않도록 맞춰줍니다.
    /// </summary>
    public void ClearPatrolWait()
    {
        isWaitingAtPatrolPoint = false;
        patrolWaitUntilTime = 0.0f;
    }

    /// <summary>
    /// 다음 순찰 포인트 인덱스로 이동합니다.
    /// 실제 다음 인덱스 규칙은 루트가 관리하고, 메모리는 결과만 반영합니다.
    /// </summary>
    public void AdvancePatrolIndex()
    {
        if (!HasPatrolRoute)
        {
            patrolIndex = 0;
            return;
        }

        patrolIndex = patrolRoute.GetNextIndex(patrolIndex);
        if (patrolIndex < 0)
        {
            patrolIndex = 0;
        }
    }

    /// <summary>
    /// 현재 추적 중인 타겟의 위치 샘플을 갱신합니다.
    /// 같은 타겟을 두 번 이상 샘플링한 뒤부터는 최근 위치 차이로 이동 속도를 추정해 차단 이동 계산에 사용합니다.
    /// </summary>
    public void UpdateTargetTracking(Transform target)
    {
        if (target == null)
        {
            ClearTargetTracking();
            return;
        }

        int targetInstanceId = target.GetInstanceID();
        if (trackedTargetInstanceId != targetInstanceId)
        {
            BeginTargetTracking(target);
            return;
        }

        float now = Time.time;
        float deltaTime = now - lastTargetSampleTime;
        if (deltaTime <= 0.0001f)
        {
            return;
        }

        Vector3 currentPosition = target.position;
        estimatedTargetVelocity = EnemyMathUtility.GetFlatDirection(lastKnownTargetPosition, currentPosition) / deltaTime;
        lastKnownTargetPosition = currentPosition;
        lastTargetSampleTime = now;
        hasTargetMotionSample = true;
    }

    /// <summary>
    /// 새 타겟을 추적하기 시작할 때 첫 샘플을 기록합니다.
    /// 첫 샘플만으로는 속도를 알 수 없으므로 차단 이동은 다음 갱신부터 활성화됩니다.
    /// </summary>
    private void BeginTargetTracking(Transform target)
    {
        trackedTargetInstanceId = target.GetInstanceID();
        lastKnownTargetPosition = target.position;
        estimatedTargetVelocity = Vector3.zero;
        lastTargetSampleTime = Time.time;
        hasTargetMotionSample = false;
    }

    /// <summary>
    /// 플레이어 추적 샘플을 모두 초기화합니다.
    /// 타겟을 잃거나 조사 상태로 전환될 때 이전 샘플이 다음 추적에 섞이지 않도록 비웁니다.
    /// </summary>
    public void ClearTargetTracking()
    {
        trackedTargetInstanceId = 0;
        lastKnownTargetPosition = Vector3.zero;
        estimatedTargetVelocity = Vector3.zero;
        lastTargetSampleTime = 0.0f;
        hasTargetMotionSample = false;
    }

    /// <summary>
    /// 이번 프레임 추적이 직선 추적인지 차단 추적인지와 목적지를 기록합니다.
    /// 인스펙터에서 현재 추적 의도를 바로 확인할 수 있도록 디버그용으로도 사용합니다.
    /// </summary>
    public void SetChasePlan(ChaseMoveMode chaseMoveMode, Vector3 destination)
    {
        currentChaseMoveMode = chaseMoveMode;
        currentChaseDestination = destination;
    }

    /// <summary>
    /// 현재 추적 계획 표시를 초기화합니다.
    /// 추적이 끝났거나 공격 상태로 넘어가 더 이상 이동 목적지를 유지하지 않을 때 사용합니다.
    /// </summary>
    public void ClearChasePlan()
    {
        currentChaseMoveMode = ChaseMoveMode.Direct;
        currentChaseDestination = Vector3.zero;
    }
}
