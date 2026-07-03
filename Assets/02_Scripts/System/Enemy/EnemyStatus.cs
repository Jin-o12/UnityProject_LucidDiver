using System;
using UnityEngine;

public class EnemyStatus : MonoBehaviour, IDamageable
{
    // 이번 노이즈 시스템 작업으로 "소리를 듣고 조사하는 상태"를 추가합니다.
    public enum EnemyState
    {
        Idle,
        Investigate,
        Chase,
        Attack,
        Dead
    }

    public EnemyState nowState { get; private set; }    // 현재 적 상태
    public bool isAttacking { get; private set; }       // 공격 수행 여부

    public int objID = 0;                               // 적 개체 식별용 ID
    public float hpMax;                                 // 최대 체력
    public float hpCurrent;                             // 현재 체력
    public float atkValue  { get; private set; }        // 공격력
    public float defValue { get; private set; }         // 방어력

    public event Action OnLocalDeath;                   // 이 적이 죽었을 때만 알리는 로컬 이벤트

    public void SetIsAttacking(bool attacking)
    {
        isAttacking = attacking;
    }

    public void SetNowState(EnemyState state)
    {
        nowState = state;
    }

    private void Awake()
    {
        nowState = EnemyState.Idle;

        // 현재 프로젝트에서는 기본 스탯을 여기서 초기화합니다.
        hpMax = 100.0f;
        hpCurrent = hpMax;
        atkValue = 10.0f;
        defValue = 0.0f;
    }

    private void Start()
    {
        UpdateHp();
    }

    private void UpdateHp()
    {
        // UI와 디버그 로직이 현재 체력을 받을 수 있도록 이벤트를 보냅니다.
        GlobalEventBus.OnEnemyHealthChanged?.Invoke(0, hpCurrent, hpMax);
    }

    public Faction EntityFaction => Faction.enemy;

    public void TakeDamage(float damage)
    {
        hpCurrent = Mathf.Clamp(hpCurrent - damage, 0.0f, hpMax);
        GlobalEventBus.OnEnemyHealthChanged?.Invoke(objID, hpCurrent, hpMax);

        if (hpCurrent <= 0.0f)
        {
            nowState = EnemyState.Dead;
            OnLocalDeath?.Invoke();
            GlobalEventBus.OnEnemyDead?.Invoke(objID);
        }
    }
}
