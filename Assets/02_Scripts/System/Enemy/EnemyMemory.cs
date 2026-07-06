using System;
using UnityEngine;

/// <summary>
/// 적 AI가 런타임 동안 기억해야 하는 순찰 관련 데이터를 보관합니다.
/// 현재 순찰 루트, 순찰 인덱스, 복귀 필요 여부, 포인트 대기 시간을 한 곳에서 관리합니다.
/// </summary>
[Serializable]
public class EnemyMemory
{
    [SerializeField] private Vector3 homePosition;          // 적이 처음 스폰된 기준 위치
    [SerializeField] private EnemyPatrolRoute patrolRoute;  // 현재 연결된 순찰 루트
    [SerializeField, Min(0)] private int patrolIndex;       // 현재 향하고 있는 순찰 포인트 인덱스
    [SerializeField] private bool needsReturnToPatrol;      // 조사나 추적 후 루트로 복귀해야 하는지 여부
    [SerializeField] private bool isWaitingAtPatrolPoint;   // 순찰 포인트에서 대기 중인지 여부
    [SerializeField] private float patrolWaitUntilTime;     // 현재 대기 상태가 끝나는 시각

    public Vector3 HomePosition => homePosition;
    public int PatrolIndex => patrolIndex;
    public bool NeedsReturnToPatrol => needsReturnToPatrol;
    public bool IsWaitingAtPatrolPoint => isWaitingAtPatrolPoint;
    public bool HasPatrolRoute => patrolRoute != null && patrolRoute.GetPointCount() > 0;

    /// <summary>
    /// 스폰 직후 순찰 관련 초기 상태를 설정합니다.
    /// 스폰 위치를 홈 위치로 기록하고, 사용할 루트와 시작 포인트를 함께 저장합니다.
    /// </summary>
    public void InitializePatrol(Vector3 spawnPosition, EnemyPatrolRoute route, int startPatrolIndex)
    {
        homePosition = spawnPosition;
        patrolRoute = route;
        patrolIndex = 0;
        needsReturnToPatrol = false;
        ClearPatrolWait();

        if (!HasPatrolRoute)
        {
            return;
        }

        patrolIndex = Mathf.Clamp(startPatrolIndex, 0, patrolRoute.GetPointCount() - 1);
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
    /// 브레인에서 직접 루트를 뜯어보지 않도록 메모리에서 값을 대신 꺼내 줍니다.
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
    /// 조사나 추적이 끝난 뒤 순찰 루트로 복귀해야 함을 기록합니다.
    /// 복귀가 시작되면 기존 포인트 대기 상태는 끊고 다시 계산해야 하므로 대기 상태도 함께 초기화합니다.
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
    /// 순찰 루트 복귀가 끝났을 때 복귀 플래그를 해제합니다.
    /// </summary>
    public void ClearReturnToPatrol()
    {
        needsReturnToPatrol = false;
    }

    /// <summary>
    /// 현재 순찰 포인트에서 대기 타이머를 시작합니다.
    /// 대기 시간이 0이면 바로 다음 포인트로 넘어가도록 대기 상태를 만들지 않습니다.
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
    /// 조사 진입, 복귀 시작, 타겟 발견처럼 행동이 바뀌는 시점에 사용합니다.
    /// </summary>
    public void ClearPatrolWait()
    {
        isWaitingAtPatrolPoint = false;
        patrolWaitUntilTime = 0f;
    }

    /// <summary>
    /// 다음 순찰 포인트 인덱스로 이동합니다.
    /// 실제 다음 인덱스 계산 규칙은 루트 쪽에서 관리하고,
    /// 메모리는 그 결과만 저장합니다.
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
