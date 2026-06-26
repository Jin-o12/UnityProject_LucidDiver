using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

// 게임 내 등장하는 적에 대한 데이터
[CreateAssetMenu(fileName = "New Enemy", menuName = "GameData/Enemy")]
public class EnemyData : ScriptableObject
{
    // 기본 스텟
    public int TID;                             // 고유 ID
    public float hpMax;                         // 최대 체력
    public float moveSpeed;                     // 이동 속도

    // 추적
    public float sightLength;                   // 시야 거리
    public float sightAngle;                    // 시야 각도

    //공격
    public float atkValue;                      // 공격력
    public float attackRate;                    // 공격 루틴 간격
    public float attackRange;                   // 공격 사거리
    public int baseAtkID;                       // 기본 공격 타입 ID (SkillData)

    // 전리품 
    public ItemRootData rootGroup;             // 전리품 그룹 ID

    // 리소스
    public AssetReferenceGameObject enemyModel; // 적 모델링 (프리팹) 경로
}
