using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 존 단위 적 스폰 설정을 담당하는 컴포넌트입니다.
/// 자식 스폰 포인트 중 몇 곳을 사용할지와 기본 패트롤 루트를 함께 관리합니다.
/// </summary>
[DisallowMultipleComponent]
public class EnemySpawnZone : MonoBehaviour
{
    [Header("Zone Spawn")]
    [SerializeField, Min(0)] private int spawnCount = 1;              // 이 존에서 실제로 생성할 적 수
    [SerializeField] private EnemyPatrolRoute defaultPatrolRoute;     // 포인트에 개별 루트가 없을 때 사용할 기본 패트롤 루트
    [SerializeField] private bool includeInactiveSpawnPoints = false; // 비활성 스폰 포인트까지 후보에 포함할지 여부

    public EnemyPatrolRoute DefaultPatrolRoute => defaultPatrolRoute;

    /// <summary>
    /// 이 존에서 사용할 수 있는 스폰 포인트를 results에 모아 넣습니다.
    /// EnemySpawnPoint가 붙어 있으면 그 설정을 우선 사용하고,
    /// 컴포넌트가 없어도 이름이 Spawn_으로 시작하면 임시 호환 포인트로 인정합니다.
    /// </summary>
    public void CollectSpawnPoints(List<Transform> results)
    {
        if (results == null)
        {
            return;
        }

        foreach (Transform child in transform)
        {
            if (child == null)
            {
                continue;
            }

            if (!includeInactiveSpawnPoints && !child.gameObject.activeInHierarchy)
            {
                continue;
            }

            EnemySpawnPoint spawnPoint = child.GetComponent<EnemySpawnPoint>();
            if (spawnPoint != null)
            {
                if (!spawnPoint.EnabledForSpawn)
                {
                    continue;
                }

                results.Add(child);
                continue;
            }

            if (child.name.StartsWith("Spawn_", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(child);
            }
        }
    }

    /// <summary>
    /// 실제로 사용할 수 있는 포인트 수를 기준으로 이 존의 스폰 수를 보정합니다.
    /// 예를 들어 spawnCount가 3이어도 포인트가 2개면 2마리만 생성되도록 막습니다.
    /// </summary>
    public int ResolveSpawnCount(int availablePointCount)
    {
        return Mathf.Clamp(spawnCount, 0, Mathf.Max(0, availablePointCount));
    }
}
