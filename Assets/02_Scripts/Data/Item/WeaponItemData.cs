using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "New Weapon", menuName = "GameData/Item/Weapon")]
public class WeaponItemData : ItemData
{
    [Header("무기 아이템 정보")]
    public float fireRate;                      // 발사 간격
    public float fireRange;                     // 발사 사거리
    public float AtkValue;                      // 피해량
    // areaType: 공격 범위 타입
    // areaWidth: 공격 범위
    public float dreamBarrierBreakValue;        // 몽막 공격력
    public AssetReferenceGameObject fireEffect; // 공격 시 이펙트 주소
}
