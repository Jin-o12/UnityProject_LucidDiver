using UnityEngine;

/// <summary>
/// 개별 순찰 포인트 주변의 랜덤 배회 범위와 체류 방식을 설정합니다.
/// 컴포넌트가 없는 기존 포인트는 EnemyPatrolRoute의 기본값을 사용합니다.
/// </summary>
[DisallowMultipleComponent]
public class EnemyPatrolPoint : MonoBehaviour
{
    [SerializeField, Min(0.0f)] private float wanderRadius = 3.0f;   // 이 포인트를 중심으로 NavMesh 목적지를 뽑을 반경
    [SerializeField, Min(1)] private int minWanderCount = 1;        // 다음 포인트로 넘어가기 전 최소 배회 횟수
    [SerializeField, Min(1)] private int maxWanderCount = 3;        // 다음 포인트로 넘어가기 전 최대 배회 횟수
    [SerializeField, Min(0.0f)] private float minWaitTime = 0.5f;   // 배회 목적지 도착 후 최소 체류 시간
    [SerializeField, Min(0.0f)] private float maxWaitTime = 2.5f;   // 배회 목적지 도착 후 최대 체류 시간

    public float WanderRadius => wanderRadius;
    public int MinWanderCount => minWanderCount;
    public int MaxWanderCount => maxWanderCount;
    public float MinWaitTime => minWaitTime;
    public float MaxWaitTime => maxWaitTime;

    private void OnValidate()
    {
        wanderRadius = Mathf.Max(0.0f, wanderRadius);
        minWanderCount = Mathf.Max(1, minWanderCount);
        maxWanderCount = Mathf.Max(minWanderCount, maxWanderCount);
        minWaitTime = Mathf.Max(0.0f, minWaitTime);
        maxWaitTime = Mathf.Max(minWaitTime, maxWaitTime);
    }
}
