using System;
using UnityEngine;

/// <summary>
/// 개별 적 스폰 포인트 설정을 담당하는 컴포넌트입니다.
/// 포인트별 패트롤 루트 오버라이드와 시작 순찰 인덱스를 지정할 수 있습니다.
/// </summary>
[DisallowMultipleComponent]
public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Spawn Point")]
    [SerializeField] private bool enabledForSpawn = true;              // 이 포인트를 실제 스폰 후보에 포함할지 여부
    [SerializeField] private EnemyPatrolRoute patrolRoute;             // 이 포인트 전용 패트롤 루트
    [SerializeField] private bool useNearestPatrolPointAsStart = true; // 스폰 위치에서 가장 가까운 순찰 포인트부터 시작할지 여부
    [SerializeField, Min(0)] private int startPatrolIndex = 0;         // 고정 시작 인덱스를 사용할 때 적용할 값

    public bool EnabledForSpawn => enabledForSpawn;

    /// <summary>
    /// 이 포인트가 사용할 패트롤 루트를 결정합니다.
    /// 1. 포인트 전용 루트
    /// 2. 존 기본 루트
    /// 3. 이름 규칙으로 추론한 루트
    /// 순서로 우선순위를 둡니다.
    /// </summary>
    public EnemyPatrolRoute ResolvePatrolRoute(EnemySpawnZone zone)
    {
        if (patrolRoute != null)
        {
            return patrolRoute;
        }

        if (zone != null && zone.DefaultPatrolRoute != null)
        {
            return zone.DefaultPatrolRoute;
        }

        string routeName = InferRouteName();
        if (string.IsNullOrEmpty(routeName))
        {
            return null;
        }

        EnemyPatrolRoute[] routes = UnityEngine.Object.FindObjectsOfType<EnemyPatrolRoute>(true);
        for (int i = 0; i < routes.Length; i++)
        {
            if (routes[i] != null && string.Equals(routes[i].name, routeName, StringComparison.OrdinalIgnoreCase))
            {
                return routes[i];
            }
        }

        return null;
    }

    /// <summary>
    /// 적이 순찰을 시작할 첫 번째 포인트 인덱스를 계산합니다.
    /// 거리 기준으로 자동 계산할 수도 있고, 인스펙터에서 고정값을 직접 줄 수도 있습니다.
    /// </summary>
    public int ResolveStartPatrolIndex(EnemyPatrolRoute route, Vector3 spawnPosition)
    {
        if (route == null || route.GetPointCount() == 0)
        {
            return 0;
        }

        if (useNearestPatrolPointAsStart)
        {
            return route.GetClosestPointIndex(spawnPosition);
        }

        return Mathf.Clamp(startPatrolIndex, 0, route.GetPointCount() - 1);
    }

    /// <summary>
    /// Spawn_Lobby_A_01 같은 이름에서 Route_Lobby_A 형태의 루트 이름을 추론합니다.
    /// 포인트에 루트를 직접 연결하지 않았을 때 사용할 마지막 보조 규칙입니다.
    /// </summary>
    private string InferRouteName()
    {
        const string spawnPrefix = "Spawn_";
        const string routePrefix = "Route_";

        if (!name.StartsWith(spawnPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        int lastUnderscoreIndex = name.LastIndexOf('_');
        if (lastUnderscoreIndex <= spawnPrefix.Length)
        {
            return string.Empty;
        }

        return routePrefix + name.Substring(spawnPrefix.Length, lastUnderscoreIndex - spawnPrefix.Length);
    }
}
