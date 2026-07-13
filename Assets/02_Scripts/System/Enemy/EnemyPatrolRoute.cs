using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 하나의 적 순찰 경로를 담는 컴포넌트입니다.
/// 자식 오브젝트를 위에서 아래 순서대로 읽어 순찰 포인트로 사용합니다.
/// </summary>
[DisallowMultipleComponent]
public class EnemyPatrolRoute : MonoBehaviour
{
    [Header("Patrol Route")]
    [SerializeField] private bool loop = true;                    // 마지막 포인트 뒤에 다시 처음 포인트로 돌아갈지 여부
    [SerializeField, Min(0f)] private float waitTimeAtPoint = 1f; // 각 순찰 포인트 도착 후 머무를 시간
    [SerializeField, Min(0.1f)] private float pointReachDistance = 0.5f; // 포인트 도착 판정 거리
    [SerializeField] private bool includeInactivePoints = false;  // 비활성 포인트까지 경로에 포함할지 여부

    [Header("Random Wander")]
    [SerializeField, Min(0f)] private float defaultWanderRadius = 3f;       // 개별 설정이 없을 때 포인트 주변 배회 반경
    [SerializeField, Min(1)] private int defaultMinWanderCount = 1;        // 다음 포인트 전환 전 최소 배회 횟수
    [SerializeField, Min(1)] private int defaultMaxWanderCount = 3;        // 다음 포인트 전환 전 최대 배회 횟수
    [SerializeField, Min(0f)] private float defaultMinWaitTime = 0.5f;     // 배회 목적지 도착 후 최소 대기 시간
    [SerializeField, Min(0f)] private float defaultMaxWaitTime = 2.5f;     // 배회 목적지 도착 후 최대 대기 시간
    [SerializeField, Min(0.1f)] private float nearbyPointDistance = 12f;  // 무작위 다음 포인트 후보로 인정할 최대 거리
    [SerializeField, Min(1)] private int randomSampleAttempts = 10;       // NavMesh 배회 목적지 탐색 재시도 횟수
    [SerializeField, Min(0.1f)] private float navMeshSampleDistance = 2f; // 후보 위치를 NavMesh에 투영할 허용 거리

    private readonly List<Transform> cachedPoints = new();
    private bool cacheDirty = true;

    public bool Loop => loop;
    public float WaitTimeAtPoint => waitTimeAtPoint;
    public float PointReachDistance => pointReachDistance;

    private void Awake()
    {
        RebuildCache();
    }

    private void OnValidate()
    {
        waitTimeAtPoint = Mathf.Max(0f, waitTimeAtPoint);
        pointReachDistance = Mathf.Max(0.1f, pointReachDistance);
        defaultWanderRadius = Mathf.Max(0f, defaultWanderRadius);
        defaultMinWanderCount = Mathf.Max(1, defaultMinWanderCount);
        defaultMaxWanderCount = Mathf.Max(defaultMinWanderCount, defaultMaxWanderCount);
        defaultMinWaitTime = Mathf.Max(0f, defaultMinWaitTime);
        defaultMaxWaitTime = Mathf.Max(defaultMinWaitTime, defaultMaxWaitTime);
        nearbyPointDistance = Mathf.Max(0.1f, nearbyPointDistance);
        randomSampleAttempts = Mathf.Max(1, randomSampleAttempts);
        navMeshSampleDistance = Mathf.Max(0.1f, navMeshSampleDistance);
        cacheDirty = true;
        RebuildCache();
    }

    private void OnTransformChildrenChanged()
    {
        cacheDirty = true;
    }

    /// <summary>
    /// 현재 경로에 등록된 순찰 포인트 수를 반환합니다.
    /// </summary>
    public int GetPointCount()
    {
        EnsureCache();
        return cachedPoints.Count;
    }

    /// <summary>
    /// 지정한 인덱스의 순찰 포인트를 반환합니다.
    /// 인덱스가 범위를 벗어나면 유효 범위 안으로 보정합니다.
    /// </summary>
    public Transform GetPoint(int index)
    {
        EnsureCache();
        if (cachedPoints.Count == 0)
        {
            return null;
        }

        index = Mathf.Clamp(index, 0, cachedPoints.Count - 1);
        return cachedPoints[index];
    }

    /// <summary>
    /// 스폰 위치에서 가장 가까운 순찰 포인트 인덱스를 계산합니다.
    /// 적이 경로 중간에서 시작하더라도 자연스럽게 가장 가까운 지점부터 돌게 만들기 위한 함수입니다.
    /// </summary>
    public int GetClosestPointIndex(Vector3 position)
    {
        EnsureCache();
        if (cachedPoints.Count == 0)
        {
            return 0;
        }

        int bestIndex = 0;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < cachedPoints.Count; i++)
        {
            Transform point = cachedPoints[i];
            if (point == null)
            {
                continue;
            }

            float sqrDistance = EnemyMathUtility.GetPlanarSqrDistance(position, point.position);
            if (sqrDistance < bestDistance)
            {
                bestDistance = sqrDistance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    /// <summary>
    /// 현재 순찰 포인트 기준 다음 포인트 인덱스를 반환합니다.
    /// loop가 켜져 있으면 마지막 다음에 처음으로 돌아가고,
    /// 꺼져 있으면 마지막 포인트에 머물도록 처리합니다.
    /// </summary>
    public int GetNextIndex(int currentIndex)
    {
        EnsureCache();
        if (cachedPoints.Count == 0)
        {
            return -1;
        }

        if (cachedPoints.Count == 1)
        {
            return 0;
        }

        int nextIndex = currentIndex + 1;
        if (loop)
        {
            return nextIndex % cachedPoints.Count;
        }

        return Mathf.Clamp(nextIndex, 0, cachedPoints.Count - 1);
    }

    /// <summary>
    /// 전달된 위치에서 가장 가까운 활성 패트롤 포인트까지 평면 거리를 반환합니다.
    /// </summary>
    public float GetClosestPointDistance(Vector3 position)
    {
        Transform point = GetPoint(GetClosestPointIndex(position));
        return point == null
            ? float.MaxValue
            : Mathf.Sqrt(EnemyMathUtility.GetPlanarSqrDistance(position, point.position));
    }

    /// <summary>
    /// 지정한 포인트의 개별 설정 또는 루트 기본값으로 랜덤 배회 횟수를 생성합니다.
    /// </summary>
    public int GetRandomWanderCount(int pointIndex)
    {
        EnemyPatrolPoint settings = GetPointSettings(pointIndex);
        int min = settings != null ? settings.MinWanderCount : defaultMinWanderCount;
        int max = settings != null ? settings.MaxWanderCount : defaultMaxWanderCount;
        return Random.Range(min, max + 1);
    }

    /// <summary>
    /// 지정한 포인트의 개별 설정 또는 루트 기본값으로 랜덤 대기 시간을 생성합니다.
    /// </summary>
    public float GetRandomWaitTime(int pointIndex)
    {
        EnemyPatrolPoint settings = GetPointSettings(pointIndex);
        float min = settings != null ? settings.MinWaitTime : defaultMinWaitTime;
        float max = settings != null ? settings.MaxWaitTime : defaultMaxWaitTime;
        return Random.Range(min, max);
    }

    /// <summary>
    /// 지정한 포인트 주변에서 현재 에이전트가 완전한 경로로 도달할 수 있는 NavMesh 목적지를 찾습니다.
    /// </summary>
    public bool TryGetRandomWanderDestination(int pointIndex, NavMeshAgent agent, out Vector3 destination)
    {
        Transform point = GetPoint(pointIndex);
        destination = point != null ? point.position : transform.position;
        if (point == null || agent == null || !agent.isOnNavMesh)
        {
            return false;
        }

        EnemyPatrolPoint settings = point.GetComponent<EnemyPatrolPoint>();
        float radius = settings != null ? settings.WanderRadius : defaultWanderRadius;
        NavMeshPath path = new NavMeshPath();

        for (int i = 0; i < randomSampleAttempts; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 candidate = point.position + new Vector3(offset.x, 0f, offset.y);
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleDistance, agent.areaMask))
            {
                continue;
            }

            if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                destination = hit.position;
                return true;
            }
        }

        return NavMesh.SamplePosition(point.position, out NavMeshHit fallback, navMeshSampleDistance, agent.areaMask)
            && SetFallbackDestination(fallback.position, out destination);
    }

    /// <summary>
    /// 현재 포인트를 제외한 가까운 포인트 중 다음 목적지를 무작위로 선택합니다.
    /// 가까운 후보가 없으면 기존 순차 이동 규칙으로 대체합니다.
    /// </summary>
    public int GetRandomNearbyPointIndex(int currentIndex)
    {
        EnsureCache();
        if (cachedPoints.Count <= 1)
        {
            return Mathf.Clamp(currentIndex, 0, Mathf.Max(0, cachedPoints.Count - 1));
        }

        Transform current = GetPoint(currentIndex);
        List<int> candidates = new List<int>();
        float maxDistanceSqr = nearbyPointDistance * nearbyPointDistance;
        for (int i = 0; i < cachedPoints.Count; i++)
        {
            if (i == currentIndex || cachedPoints[i] == null)
            {
                continue;
            }

            if (current == null || EnemyMathUtility.GetPlanarSqrDistance(current.position, cachedPoints[i].position) <= maxDistanceSqr)
            {
                candidates.Add(i);
            }
        }

        return candidates.Count > 0
            ? candidates[Random.Range(0, candidates.Count)]
            : GetNextIndex(currentIndex);
    }

    /// <summary>
    /// 지정한 포인트에 연결된 개별 랜덤 배회 설정을 반환합니다.
    /// </summary>
    private EnemyPatrolPoint GetPointSettings(int pointIndex)
    {
        Transform point = GetPoint(pointIndex);
        return point != null ? point.GetComponent<EnemyPatrolPoint>() : null;
    }

    /// <summary>
    /// NavMesh에 투영된 대체 위치를 목적지로 반환합니다.
    /// </summary>
    private static bool SetFallbackDestination(Vector3 fallback, out Vector3 destination)
    {
        destination = fallback;
        return true;
    }

    /// <summary>
    /// 자식 포인트 캐시가 오래되었을 때만 다시 빌드합니다.
    /// 순찰 중 매 프레임 GetComponents 계열을 호출하지 않기 위한 캐시 처리입니다.
    /// </summary>
    private void EnsureCache()
    {
        if (!cacheDirty)
        {
            return;
        }

        RebuildCache();
    }

    /// <summary>
    /// 현재 자식 오브젝트 목록을 읽어 순찰 포인트 캐시를 다시 구성합니다.
    /// </summary>
    private void RebuildCache()
    {
        cacheDirty = false;
        cachedPoints.Clear();

        foreach (Transform child in transform)
        {
            if (child == null)
            {
                continue;
            }

            if (!includeInactivePoints && !child.gameObject.activeInHierarchy)
            {
                continue;
            }

            cachedPoints.Add(child);
        }
    }
}
