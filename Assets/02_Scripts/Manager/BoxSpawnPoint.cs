using UnityEngine;

/// <summary>
/// 박스가 실제로 생성될 수 있는 개별 스폰 위치입니다.
/// 존 내부에서 이 포인트가 선택될 확률 가중치와 사용 여부를 설정합니다.
/// </summary>
[DisallowMultipleComponent]
public class BoxSpawnPoint : MonoBehaviour
{
    [SerializeField] private bool enabledForSpawn = true;      // 이 포인트를 랜덤 스폰 후보에 포함할지 여부
    [SerializeField, Min(0f)] private float pointWeight = 1f;  // 같은 존 안에서 선택될 확률 가중치

    public bool EnabledForSpawn => enabledForSpawn;
    public float PointWeight => pointWeight;
}
