using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 레벨 시작 시 박스 스폰 전체를 관리하는 전용 스포너입니다.
/// 존 규칙을 읽어서 보장 수량을 먼저 배치하고, 남은 수량은 가중치 기반으로 랜덤 분배합니다.
/// </summary>
[DisallowMultipleComponent]
public class LevelBoxSpawner : MonoBehaviour
{
    /// <summary>
    /// 추가 스폰 수량을 남은 수용량 비율로 계산할지,
    /// 인스펙터에서 직접 지정한 개수를 사용할지 결정합니다.
    /// </summary>
    private enum AdditionalSpawnMode
    {
        Density = 0,
        FixedCount = 1,
        TotalCountRange = 2,
    }

    /// <summary>
    /// 실제 포인트 Transform과 포인트 가중치를 함께 다루기 위한 런타임 구조체입니다.
    /// </summary>
    private readonly struct PointRuntime
    {
        public PointRuntime(Transform pointTransform, float weight)
        {
            PointTransform = pointTransform;
            Weight = weight;
        }

        public Transform PointTransform { get; }
        public float Weight { get; }
    }

    /// <summary>
    /// BoxSpawnZone 컴포넌트를 런타임 계산용으로 캐시한 데이터입니다.
    /// 스폰이 진행되면서 남은 포인트와 생성 개수를 함께 관리합니다.
    /// </summary>
    private sealed class ZoneRuntime
    {
        public string ZoneName;
        public int GuaranteedSpawnCount;
        public int MaxSpawnCount;
        public bool AllowRandomSpawn;
        public float RandomWeight;
        public readonly List<PointRuntime> AvailablePoints = new();
        public int SpawnedCount;

        public int RemainingCapacity => Mathf.Max(0, MaxSpawnCount - SpawnedCount);
    }

    [Header("Spawn References")]
    [SerializeField] private GameObject boxPrefab;         // 실제로 생성할 ItemBox 프리팹
    [SerializeField] private Transform boxSpawnPointPool;  // 존과 포인트를 담고 있는 루트 오브젝트
    [SerializeField] private Transform spawnedBoxRoot;     // 생성된 박스를 정리해 둘 부모 오브젝트

    [Header("Additional Spawn")]
    [SerializeField] private AdditionalSpawnMode additionalSpawnMode = AdditionalSpawnMode.Density; // 추가 수량 계산 방식
    [SerializeField, Range(0f, 1f)] private float additionalSpawnDensity = 0.4f;                    // 남은 수용량 기준 추가 스폰 비율
    [SerializeField, Min(0)] private int additionalSpawnCount = 0;                                  // 고정 개수 모드에서 사용할 추가 스폰 수
    [SerializeField, Min(0)] private int totalSpawnMin = 0;                                         // 전체 박스 수를 범위로 제어할 때 최소 목표값
    [SerializeField, Min(0)] private int totalSpawnMax = 0;                                         // 전체 박스 수를 범위로 제어할 때 최대 목표값
    [SerializeField] private bool clearPreviouslySpawnedBoxes = true;                               // 재호출 시 이전에 생성한 박스를 먼저 정리할지 여부

    [Header("Random Seed")]
    [SerializeField] private bool useDeterministicSeed = false;  // 고정 시드로 같은 결과를 재현할지 여부
    [SerializeField] private int randomSeed = 0;                 // 고정 시드 값

    private readonly List<GameObject> spawnedBoxes = new();

    /// <summary>
    /// 현재 씬에 박스를 스폰합니다.
    /// 존 규칙을 수집한 뒤 보장 수량을 먼저 배치하고, 남은 수량을 랜덤 분배합니다.
    /// </summary>
    public int SpawnBoxes()
    {
        if (boxPrefab == null)
        {
            Debug.LogWarning("LevelBoxSpawner: box prefab is not assigned.");
            return 0;
        }

        if (boxSpawnPointPool == null)
        {
            GameObject foundPool = GameObject.Find("BoxSpawnPointPool");
            if (foundPool != null)
            {
                boxSpawnPointPool = foundPool.transform;
            }
            else
            {
                // LevelDesignTable에 추가한 풀을 태그로 찾아옴
                boxSpawnPointPool = GameObject.FindGameObjectWithTag("BoxSpawnPool").transform;
            }
        }

        if (boxSpawnPointPool == null)
        {
            Debug.LogWarning("LevelBoxSpawner: box spawn point pool was not found.");
            return 0;
        }

        ResolveSpawnedBoxRoot();

        if (clearPreviouslySpawnedBoxes)
        {
            ClearSpawnedBoxes();
        }

        Random.State previousState = Random.state;
        if (useDeterministicSeed)
        {
            Random.InitState(randomSeed);
        }

        try
        {
            List<ZoneRuntime> zones = BuildZones();
            if (zones.Count == 0)
            {
                Debug.LogWarning("LevelBoxSpawner: no valid box spawn zones were found.");
                return 0;
            }

            int guaranteedSpawned = SpawnGuaranteedBoxes(zones);
            int additionalSpawned = SpawnAdditionalBoxes(zones, guaranteedSpawned);

            Debug.Log($"LevelBoxSpawner: spawned {guaranteedSpawned + additionalSpawned} boxes.");
            return guaranteedSpawned + additionalSpawned;
        }
        finally
        {
            if (useDeterministicSeed)
            {
                Random.state = previousState;
            }
        }
    }

    /// <summary>
    /// BoxSpawnPointPool 아래 자식 존들을 읽어서 런타임 계산용 데이터로 변환합니다.
    /// </summary>
    private List<ZoneRuntime> BuildZones()
    {
        List<ZoneRuntime> zones = new();

        foreach (Transform zoneTransform in boxSpawnPointPool)
        {
            if (zoneTransform == null || !zoneTransform.gameObject.activeInHierarchy)
            {
                continue;
            }

            BoxSpawnZone zoneSettings = zoneTransform.GetComponent<BoxSpawnZone>();
            List<PointRuntime> points = CollectPoints(zoneTransform, zoneSettings);
            if (points.Count == 0)
            {
                Debug.LogWarning($"LevelBoxSpawner: zone '{zoneTransform.name}' has no usable spawn points.");
                continue;
            }

            int maxSpawnCount = zoneSettings != null ? zoneSettings.ResolveMaxSpawnCount(points.Count) : points.Count;
            int guaranteedSpawnCount = zoneSettings != null ? Mathf.Clamp(zoneSettings.GuaranteedSpawnCount, 0, maxSpawnCount) : 0;
            bool allowRandomSpawn = zoneSettings == null || zoneSettings.AllowRandomSpawn;
            float randomWeight = zoneSettings != null
                ? Mathf.Max(0f, zoneSettings.RandomWeight * zoneSettings.DensityMultiplier)
                : 1f;

            ZoneRuntime zone = new()
            {
                ZoneName = zoneTransform.name,
                GuaranteedSpawnCount = guaranteedSpawnCount,
                MaxSpawnCount = maxSpawnCount,
                AllowRandomSpawn = allowRandomSpawn,
                RandomWeight = randomWeight,
            };

            zone.AvailablePoints.AddRange(points);
            zones.Add(zone);
        }

        return zones;
    }

    /// <summary>
    /// 존 자식 오브젝트들 중 실제 스폰 후보 포인트만 수집합니다.
    /// BoxSpawnPoint가 없으면 기본 가중치 1의 일반 포인트로 취급합니다.
    /// </summary>
    private List<PointRuntime> CollectPoints(Transform zoneTransform, BoxSpawnZone zoneSettings)
    {
        List<PointRuntime> points = new();
        bool includeInactivePoints = zoneSettings != null && zoneSettings.IncludeInactivePoints;

        foreach (Transform pointTransform in zoneTransform)
        {
            if (pointTransform == null)
            {
                continue;
            }

            if (!includeInactivePoints && !pointTransform.gameObject.activeInHierarchy)
            {
                continue;
            }

            BoxSpawnPoint pointSettings = pointTransform.GetComponent<BoxSpawnPoint>();
            if (pointSettings != null && !pointSettings.EnabledForSpawn)
            {
                continue;
            }

            float pointWeight = pointSettings != null ? Mathf.Max(0f, pointSettings.PointWeight) : 1f;
            points.Add(new PointRuntime(pointTransform, pointWeight));
        }

        return points;
    }

    /// <summary>
    /// 각 존의 guaranteedSpawnCount만큼 먼저 박스를 배치합니다.
    /// 이 단계가 끝나면 필수 구역은 최소 수량이 보장됩니다.
    /// </summary>
    private int SpawnGuaranteedBoxes(List<ZoneRuntime> zones)
    {
        int spawnedCount = 0;

        foreach (ZoneRuntime zone in zones)
        {
            int targetCount = Mathf.Min(zone.GuaranteedSpawnCount, zone.AvailablePoints.Count);

            for (int i = 0; i < targetCount; i++)
            {
                if (!TrySpawnInZone(zone))
                {
                    Debug.LogWarning($"LevelBoxSpawner: failed to satisfy guaranteed spawns in zone '{zone.ZoneName}'.");
                    break;
                }

                spawnedCount++;
            }
        }

        return spawnedCount;
    }

    /// <summary>
    /// 보장 수량 배치 이후 남은 수용량을 기준으로 추가 박스를 랜덤 분배합니다.
    /// </summary>
    private int SpawnAdditionalBoxes(List<ZoneRuntime> zones, int guaranteedSpawned)
    {
        int totalRemainingCapacity = 0;
        foreach (ZoneRuntime zone in zones)
        {
            if (!zone.AllowRandomSpawn)
            {
                continue;
            }

            totalRemainingCapacity += Mathf.Min(zone.RemainingCapacity, zone.AvailablePoints.Count);
        }

        int additionalTarget = ResolveAdditionalSpawnTarget(totalRemainingCapacity, guaranteedSpawned);

        int spawnedCount = 0;
        for (int i = 0; i < additionalTarget; i++)
        {
            ZoneRuntime selectedZone = PickZone(zones);
            if (selectedZone == null)
            {
                break;
            }

            if (!TrySpawnInZone(selectedZone))
            {
                break;
            }

            spawnedCount++;
        }

        return spawnedCount;
    }

    /// <summary>
    /// 인스펙터 설정에 따라 이번 레벨에서 추가로 생성할 박스 개수를 계산합니다.
    /// </summary>
    private int ResolveAdditionalSpawnTarget(int totalRemainingCapacity, int guaranteedSpawned)
    {
        int rawTarget = 0;

        switch (additionalSpawnMode)
        {
            case AdditionalSpawnMode.FixedCount:
                rawTarget = additionalSpawnCount;
                break;

            case AdditionalSpawnMode.TotalCountRange:
                int minTargetTotal = Mathf.Max(guaranteedSpawned, totalSpawnMin);
                int maxTargetTotal = Mathf.Max(minTargetTotal, totalSpawnMax);
                int randomTargetTotal = Random.Range(minTargetTotal, maxTargetTotal + 1);
                rawTarget = randomTargetTotal - guaranteedSpawned;
                break;

            case AdditionalSpawnMode.Density:
            default:
                rawTarget = Mathf.RoundToInt(totalRemainingCapacity * additionalSpawnDensity);
                break;
        }

        return Mathf.Clamp(rawTarget, 0, totalRemainingCapacity);
    }

    /// <summary>
    /// 추가 랜덤 분배용 존을 가중치 기반으로 1개 선택합니다.
    /// 최대 수량을 채운 존이나 랜덤 비허용 존은 후보에서 제외합니다.
    /// </summary>
    private ZoneRuntime PickZone(List<ZoneRuntime> zones)
    {
        List<ZoneRuntime> candidates = new();
        float totalWeight = 0f;

        foreach (ZoneRuntime zone in zones)
        {
            if (!zone.AllowRandomSpawn || zone.RemainingCapacity <= 0 || zone.AvailablePoints.Count == 0)
            {
                continue;
            }

            if (zone.RandomWeight <= 0f)
            {
                continue;
            }

            candidates.Add(zone);
            totalWeight += zone.RandomWeight;
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        float randomValue = Random.value * totalWeight;
        float cumulativeWeight = 0f;

        for (int i = 0; i < candidates.Count; i++)
        {
            cumulativeWeight += candidates[i].RandomWeight;
            if (randomValue <= cumulativeWeight)
            {
                return candidates[i];
            }
        }

        return candidates[candidates.Count - 1];
    }

    /// <summary>
    /// 선택된 존 안에서 실제 포인트를 하나 뽑아 박스를 생성합니다.
    /// 한 번 사용한 포인트는 중복 생성되지 않도록 후보 목록에서 제거합니다.
    /// </summary>
    private bool TrySpawnInZone(ZoneRuntime zone)
    {
        if (zone == null || zone.RemainingCapacity <= 0 || zone.AvailablePoints.Count == 0)
        {
            return false;
        }

        int pointIndex = PickPointIndex(zone.AvailablePoints);
        if (pointIndex < 0 || pointIndex >= zone.AvailablePoints.Count)
        {
            return false;
        }

        PointRuntime point = zone.AvailablePoints[pointIndex];
        zone.AvailablePoints.RemoveAt(pointIndex);

        Transform parent = spawnedBoxRoot != null ? spawnedBoxRoot : null;
        GameObject spawnedBox = Instantiate(boxPrefab, point.PointTransform.position, point.PointTransform.rotation, parent);
        spawnedBoxes.Add(spawnedBox);
        zone.SpawnedCount++;
        return true;
    }

    /// <summary>
    /// 존 내부 포인트 목록에서 가중치 기반으로 1개 인덱스를 선택합니다.
    /// 모든 가중치가 0이면 균등 랜덤으로 처리합니다.
    /// </summary>
    private int PickPointIndex(List<PointRuntime> points)
    {
        if (points == null || points.Count == 0)
        {
            return -1;
        }

        float totalWeight = 0f;
        for (int i = 0; i < points.Count; i++)
        {
            totalWeight += points[i].Weight;
        }

        if (totalWeight <= 0f)
        {
            return Random.Range(0, points.Count);
        }

        float randomValue = Random.value * totalWeight;
        float cumulativeWeight = 0f;

        for (int i = 0; i < points.Count; i++)
        {
            cumulativeWeight += points[i].Weight;
            if (randomValue <= cumulativeWeight)
            {
                return i;
            }
        }

        return points.Count - 1;
    }

    /// <summary>
    /// 이 스포너가 이전 호출에서 생성했던 박스를 정리합니다.
    /// </summary>
    private void ClearSpawnedBoxes()
    {
        for (int i = spawnedBoxes.Count - 1; i >= 0; i--)
        {
            if (spawnedBoxes[i] == null)
            {
                spawnedBoxes.RemoveAt(i);
                continue;
            }

            Destroy(spawnedBoxes[i]);
            spawnedBoxes.RemoveAt(i);
        }
    }

    /// <summary>
    /// 생성된 박스를 정리할 부모가 비어 있으면 ==ObjectPool== 를 기본 루트로 사용합니다.
    /// </summary>
    private void ResolveSpawnedBoxRoot()
    {
        if (spawnedBoxRoot != null)
        {
            return;
        }

        GameObject objectPoolRoot = GameObject.Find("==ObjectPool==");
        if (objectPoolRoot != null)
        {
            spawnedBoxRoot = objectPoolRoot.transform;
        }
    }
}
