using System.Collections.Generic;
using UnityEngine;

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
