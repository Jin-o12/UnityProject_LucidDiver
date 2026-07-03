using UnityEngine;

/// <summary>
/// 박스 스폰 존의 규칙을 담는 컴포넌트입니다.
/// 최소 보장 수량, 최대 수량, 추가 랜덤 분배 가중치를 인스펙터에서 조절합니다.
/// </summary>
[DisallowMultipleComponent]
public class BoxSpawnZone : MonoBehaviour
{
    [Header("Guaranteed Spawn")]
    [SerializeField, Min(0)] private int guaranteedSpawnCount = 0;  // 이 존에 무조건 먼저 생성할 최소 박스 수
    [SerializeField, Min(0)] private int maxSpawnCount = 0;         // 이 존에 허용할 최대 박스 수

    [Header("Random Distribution")]
    [SerializeField] private bool allowRandomSpawn = true;          // 보장 수량 이후 추가 랜덤 배치를 허용할지 여부
    [SerializeField, Min(0f)] private float randomWeight = 1f;      // 추가 배치 시 존 선택 기본 가중치
    [SerializeField, Min(0f)] private float densityMultiplier = 1f; // 존별 밀도 보정값

    [Header("Point Scan")]
    [SerializeField] private bool includeInactivePoints = false;    // 비활성화된 포인트까지 스캔할지 여부

    public int GuaranteedSpawnCount => guaranteedSpawnCount;
    public bool AllowRandomSpawn => allowRandomSpawn;
    public float RandomWeight => randomWeight;
    public float DensityMultiplier => densityMultiplier;
    public bool IncludeInactivePoints => includeInactivePoints;

    /// <summary>
    /// 존 안에 실제로 존재하는 포인트 수를 기준으로 최대 스폰 가능 개수를 보정합니다.
    /// maxSpawnCount가 0 이하라면 포인트 수 전체를 최대치로 사용합니다.
    /// </summary>
    public int ResolveMaxSpawnCount(int pointCount)
    {
        if (maxSpawnCount <= 0)
        {
            return Mathf.Max(0, pointCount);
        }

        return Mathf.Clamp(maxSpawnCount, 0, Mathf.Max(0, pointCount));
    }
}
