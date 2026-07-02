using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy", menuName = "GameData/Enemy")]
public class EnemyData : ScriptableObject
{
    public string enemyName;                            // 적 이름
    public int health;                                  // 최대 체력
    public int damage;                                  // 공격력
    public float defValue;                             // 방어력

    [Header("이동 관련 변수")]
    public float moveSpeed;                             // 이동 속도
    [SerializeField] private float sightLength;         // 적이 대상을 인식할 수 있는 최대 거리
    [SerializeField] private float sightAngle;          // 적이 대상을 인식할 수 있는 시야 각도
    [SerializeField] private float eyeHeight;           // 적의 눈높이

    [Header("Enemy Attack")]
    [SerializeField] private float attackLength;        // 공격 판정 거리
    [SerializeField] private float attackCooldown;      // 공격 후 다음 행동까지의 대기 시간

    [Header("Enemy Search")]
    [SerializeField] private float checkInterval;       // 타겟 탐색과 상태 갱신 주기


}

