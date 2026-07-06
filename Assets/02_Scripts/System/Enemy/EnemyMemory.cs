using System;
using UnityEngine;

/// <summary>
/// 적 AI가 순찰 중 기억해야 하는 상태를 보관합니다.
/// 현재 순찰 루트, 목표 포인트 인덱스, 복귀 필요 여부,
/// 순찰 이탈 시작 지점(복귀 앵커), 대기 시간 등을 한 곳에서 관리합니다.
/// </summary>
[Serializable]
public class EnemyMemory
{
    [SerializeField] private Vector3 homePosition;          // 적이 처음 스폰된 기준 위치
    [SerializeField] private EnemyPatrolRoute patrolRoute;  // 현재 연결된 순찰 루트
    [SerializeField, Min(0)] private int patrolIndex;       // 현재 향하고 있는 순찰 포인트 인덱스
    [SerializeField] private bool needsReturnToPatrol;      // 조사나 추적 후 순찰로 복귀해야 하는지 여부
    [SerializeField] private bool isWaitingAtPatrolPoint;   // 순찰 포인트에서 대기 중인지 여부
    [SerializeField] private float patrolWaitUntilTime;     // 현재 대기 상태가 끝나는 시각
    [SerializeField] private bool hasReturnAnchor;          // 순찰에서 이탈한 지점을 이미 저장했는지 여부
    [SerializeField] private Vector3 returnAnchorPosition;  // 복귀해야 할 이탈 시작 지점
    [SerializeField, Min(0)] private int returnPatrolIndex; // 복귀 후 다시 이어갈 순찰 포인트 인덱스

    public Vector3 HomePosition => homePosition;
    public int PatrolIndex => patrolIndex;
    public bool NeedsReturnToPatrol => needsReturnToPatrol;
    public bool IsWaitingAtPatrolPoint => isWaitingAtPatrolPoint;
    public bool HasPatrolRoute => patrolRoute != null && patrolRoute.GetPointCount() > 0;
    public bool HasReturnAnchor => hasReturnAnchor;
    public Vector3 ReturnAnchorPosition => hasReturnAnchor ? returnAnchorPosition : homePosition;

    /// <summary>
    /// 스폰 직후 순찰 관련 초기 상태를 설정합니다.
    /// 스폰 위치를 기준 위치로 기록하고, 사용할 루트와 시작 인덱스를 기억합니다.
    /// </summary>
    public void InitializePatrol(Vector3 spawnPosition, EnemyPatrolRoute route, int startPatrolIndex)
    {
        homePosition = spawnPosition;
        patrolRoute = route;
        patrolIndex = 0;
        needsReturnToPatrol = false;
        isWaitingAtPatrolPoint = false;
        patrolWaitUntilTime = 0f;
        hasReturnAnchor = false;
        returnAnchorPosition = spawnPosition;
        returnPatrolIndex = 0;

        if (!HasPatrolRoute)
        {
            return;
        }

        patrolIndex = Mathf.Clamp(startPatrolIndex, 0, patrolRoute.GetPointCount() - 1);
        returnPatrolIndex = patrolIndex;
    }

    /// <summary>
    /// 현재 인덱스의 순찰 포인트를 반환합니다.
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
    /// 현재 루트가 사용하는 도착 판정 거리를 반환합니다.
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
        return patrolRoute != null ? patrolRoute.WaitTimeAtPoint : 0f;
    }

    /// <summary>
    /// 적이 순찰 루트에서 처음 벗어나는 시점의 위치와 순찰 인덱스를 저장합니다.
    /// 이미 이탈 상태라면 기존 기준점을 유지해 여러 번 덮어쓰지 않습니다.
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
    /// 조사나 추적이 끝난 뒤 순찰 흐름으로 돌아가야 함을 기록합니다.
    /// 복귀가 시작되면 기존 포인트 대기는 무효화하고 다시 계산합니다.
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
    /// 복귀 플래그와 복귀 기준점을 모두 초기화합니다.
    /// 복귀가 더 이상 필요 없거나, 기준점이 무효해졌을 때 사용합니다.
    /// </summary>
    public void ClearReturnToPatrol()
    {
        needsReturnToPatrol = false;
        hasReturnAnchor = false;
        returnAnchorPosition = homePosition;
        returnPatrolIndex = patrolIndex;
    }

    /// <summary>
    /// 이탈 시작 지점까지 복귀를 마쳤을 때 호출합니다.
    /// 이탈 직전에 바라보던 순찰 인덱스를 복원한 뒤, 다음 틱부터 기본 순찰 로직이 이어지게 정리합니다.
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
    /// 현재 순찰 포인트에서 대기 타이머를 시작합니다.
    /// 대기 시간이 0이면 즉시 다음 포인트로 넘어갈 수 있게 대기 상태를 만들지 않습니다.
    /// </summary>
    public void BeginPatrolWait()
    {
        float waitDuration = GetWaitTimeAtPoint();
        if (waitDuration <= 0f)
        {
            isWaitingAtPatrolPoint = false;
            patrolWaitUntilTime = 0f;
            return;
        }

        isWaitingAtPatrolPoint = true;
        patrolWaitUntilTime = Time.time + waitDuration;
    }

    /// <summary>
    /// 현재 순찰 포인트에서의 대기 시간이 끝났는지 확인합니다.
    /// </summary>
    public bool HasCompletedPatrolWait()
    {
        return !isWaitingAtPatrolPoint || Time.time >= patrolWaitUntilTime;
    }

    /// <summary>
    /// 순찰 포인트 대기 상태를 초기화합니다.
    /// 조사 진입, 복귀 시작, 타깃 재발견처럼 행동이 바뀌는 시점에서 사용합니다.
    /// </summary>
    public void ClearPatrolWait()
    {
        isWaitingAtPatrolPoint = false;
        patrolWaitUntilTime = 0f;
    }

    /// <summary>
    /// 다음 순찰 포인트 인덱스로 이동합니다.
    /// 실제 다음 인덱스 계산 규칙은 루트 쪽에서 관리하고, 메모리는 결과만 반영합니다.
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
}
