using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Bullet", menuName = "GameData/Item/Bullet")]
public class BulletItemData : ItemData
{
    [Header("탄약 아이템 정보")]
    public float bulletDuration;    // 탄약 지속 시간
    public float bulletSpeed;       // 탄약 속도
    public int bulletAtk;           // 탄약 공격력
}
