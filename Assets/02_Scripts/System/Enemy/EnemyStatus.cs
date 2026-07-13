using System;
using UnityEngine;

public class EnemyStatus : MonoBehaviour, IEffectReceiver
{
    // 이번 AI 고도화 작업 기준으로 순찰 상태와 복귀 상태를 추가합니다.
    public enum EnemyState
    {
        Idle,
        Patrol,
        Investigate,
        Return,
        Chase,
        Attack,
        Dead
    }

    public EnemyState nowState { get; private set; }    // 현재 적 상태
    public bool isAttacking { get; private set; }       // 공격 실행 중 여부

    public int objID = 0;                               // 각 적 개체를 구분하는 런타임 고유 번호
    public float hpMax;                                 // 최대 체력
    public float hpCurrent;                             // 현재 체력
    public float atkValue { get; private set; }         // 공격력
    public float defValue { get; private set; }         // 방어력

    public event Action OnLocalDeath;                   // 이 적 자신이 죽었을 때만 호출되는 로컬 사망 이벤트
    public event Action<float, float> OnLocalHealthChanged; // 이 적 자신에게 연결된 HP UI만 갱신하기 위한 로컬 체력 이벤트

    private EntityIdentity entityIdentity;              // 런타임 식별 번호를 함께 맞춰 줄 식별 컴포넌트

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
        entityIdentity = GetComponent<EntityIdentity>();
        SyncRuntimeIdentity();

        nowState = EnemyState.Idle;

        // 현재 프로젝트에서는 기본 스탯을 여기서 초기화합니다.
        hpMax = 100.0f;
        hpCurrent = hpMax;
        atkValue = 10.0f;
        defValue = 0.0f;
    }

    private void Start()
    {
        SyncRuntimeIdentity();
        BroadcastHealthChanged();
    }

    public Faction EntityFaction => Faction.enemy;

    public void TakeDamage(float damage)
    {
        SyncRuntimeIdentity();

        hpCurrent = Mathf.Clamp(hpCurrent - damage, 0.0f, hpMax);
        BroadcastHealthChanged();

        if (hpCurrent <= 0.0f)
        {
            nowState = EnemyState.Dead;
            OnLocalDeath?.Invoke();
            GlobalEventBus.OnEnemyDead?.Invoke(objID);
        }
    }

    public event Action<Transform, float> OnAggroApplied;       // 강제 추적 타겟 지정 이벤트

    public void ApplyAggro(Transform target, float duration)
    {
        OnAggroApplied?.Invoke(target, duration);
    }

    /// <summary>
    /// SpawnManager가 적을 생성한 직후 런타임 고유 번호를 넣어 줄 때 호출합니다.
    /// EnemyStatus와 EntityIdentity가 같은 번호를 바라보도록 한 곳에서 함께 맞춥니다.
    /// </summary>
    public void SetRuntimeObjectId(int runtimeId)
    {
        objID = runtimeId;

        if (entityIdentity != null)
        {
            entityIdentity.SetupIdentity(runtimeId, Faction.enemy);
        }
    }

    /// <summary>
    /// 자기 자신에게 연결된 UI와, 디버그/시스템용 전역 이벤트를 함께 갱신합니다.
    /// 수정 이유:
    /// 기존에는 모든 적 UI가 같은 전역 체력 이벤트를 같이 받아서 한 마리가 맞아도 전부 갱신됐습니다.
    /// 그래서 개별 UI용 로컬 이벤트를 따로 두고, 전역 이벤트는 식별자와 함께 유지하도록 나눴습니다.
    /// </summary>
    private void BroadcastHealthChanged()
    {
        OnLocalHealthChanged?.Invoke(hpCurrent, hpMax);
        GlobalEventBus.OnEnemyHealthChanged?.Invoke(objID, hpCurrent, hpMax);
    }

    /// <summary>
    /// 런타임 번호가 아직 정해지지 않았다면 임시 고유 번호를 만들고,
    /// EntityIdentity와 EnemyStatus의 번호가 어긋나면 다시 동기화합니다.
    /// SpawnManager가 따로 번호를 주면 그 값을 최종 번호로 덮어씁니다.
    /// </summary>
    private void SyncRuntimeIdentity()
    {
        if (objID <= 0)
        {
            objID = GetInstanceID();
        }

        if (entityIdentity != null &&
            (entityIdentity.entityID != objID || entityIdentity.entityFaction != Faction.enemy))
        {
            entityIdentity.SetupIdentity(objID, Faction.enemy);
        }
    }

    public void HealthRecoverInst(float amount)
    {
        hpCurrent = Mathf.Clamp(hpCurrent + amount, 0.0f, hpMax);
        BroadcastHealthChanged();
    }

    public void ManaRecoverInst(float amount)
    {
        // 적 객체는 마나 시스템을 사용하지 않으므로 구현하지 않습니다.
    }
}
