using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "GameData/Item/Weapon")]
public class WeaponItemData : ItemData
{
    [Header("무기 아이템 정보")]
    public float fireRate;      // 발사 간격
    public float fireRange;     // 발사 사거리
    public int magazine;        // 탄창 용량
    public int totalBullet;     // 총 총알 수
    public float reloadTime;    // 재장전 시간
    public int bulletTID;       // 발사되는 총알의 아이템 TID
}
