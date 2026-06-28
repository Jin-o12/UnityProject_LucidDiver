using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "GameData/Item/Consume")]
public class ConsumeItemData : ItemData
{
    [Header("소비 아이템 정보")]
    public AreaType useType;                 // 발동 타입
    public float useRange;                  // 사용 거리
    public float useDelay;                  // 발동 대기 시간

    [Header("소비 효과")]
    public List<ItemEffect> useEffect;      // 발동 효과
}
