using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 프리팹에 실제로 붙어 있는 메인 적 AI 스크립트입니다.
/// 판단, 감지, 이동, 전투 모듈을 묶고 스폰 시 받은 순찰 정보도 초기화합니다.
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    [Header("Enemy AI Modules")]
    [SerializeField] private EnemyBrain brain = new EnemyBrain();                   // 상위 의사결정 담당
    [SerializeField] private EnemyMemory memory = new EnemyMemory();                // 순찰/복귀 런타임 기억 담당
    [SerializeField] private EnemyPerception perception = new EnemyPerception();    // 시야/청각 감지 담당
    [SerializeField] private EnemyNoiseListener noiseListener = new EnemyNoiseListener(); // 조사 상태와 소음 반응 담당
    [SerializeField] private EnemyLocomotion locomotion = new EnemyLocomotion();    // 이동, 방향 전환, 돌진 담당
    [SerializeField] private EnemyCombat combat = new EnemyCombat();                // 공격 판정과 2연격 담당

    [Header("Attack Anchor")]
    [SerializeField] private Transform attackOrigin;                                // 실제 근접 타격 거리 계산 기준점

    [Header("Required Components")]
    private EnemyStatus myStatus;                                                   // 상태/스탯 보유 컴포넌트
    private NavMeshAgent navAgent;                                                  // 네비게이션 이동 컴포넌트
    private Coroutine aiRoutine;                                                    // AI 판단 루프 코루틴 핸들

    public float SightLength => perception.SightRange;
    public float AwarenessRange => perception.AwarenessRange;
    public float HearingRange => perception.HearingRange;
    public float SightAngle => perception.SightAngle;
    public float EyeHeight => perception.EyeHeight;
    public Transform CurrentTarget => brain.CurrentTarget;

    public event Action<bool> OnWalkEvent;                                          // 걷기 애니메이션 갱신 이벤트
    public event Action OnAttackEvent;                                              // 공격 애니메이션 시작 이벤트
    public event Action OnDeathEvent;                                               // 사망 애니메이션 시작 이벤트
    public event Action<int, int> OnLookDirEvent;                                   // 방향 애니메이션 갱신 이벤트

    private void Awake()
    {
        EnsureModules();

        navAgent = GetComponent<NavMeshAgent>();
        myStatus = GetComponent<EnemyStatus>();

        if (navAgent == null || myStatus == null)
        {
            enabled = false;
            Debug.LogError("EnemyMovement: required components are missing.");
            return;
        }

        // 공격 기준점이 프리팹에 수동 지정되지 않아도 body/HitRange를 우선 탐색합니다.
        ResolveAttackOrigin();

        brain.OnValidate();
        perception.OnValidate();
        noiseListener.OnValidate();
        locomotion.OnValidate();
        locomotion.Initialize(navAgent);
        combat.OnValidate();
    }

    private void OnValidate()
    {
        EnsureModules();

        brain.OnValidate();
        perception.OnValidate();
        noiseListener.OnValidate();
        locomotion.OnValidate();
        combat.OnValidate();

        if (attackOrigin == null)
        {
            ResolveAttackOrigin();
        }
    }

    private void OnEnable()
    {
        if (myStatus == null)
        {
            return;
        }

        myStatus.OnLocalDeath += Die;
        GlobalEventBus.OnNoiseEmitted += HandleNoiseEmitted;
    }

    private void Start()
    {
        if (myStatus == null)
        {
            return;
        }

        aiRoutine = StartCoroutine(CheckRoutine());
    }

    private void OnDisable()
    {
        if (myStatus == null)
        {
            return;
        }

        myStatus.OnLocalDeath -= Die;
        GlobalEventBus.OnNoiseEmitted -= HandleNoiseEmitted;

        if (aiRoutine != null)
        {
            StopCoroutine(aiRoutine);
            aiRoutine = null;
        }
    }

    /// <summary>
    /// 일정 주기마다 브레인에게 "지금 무엇을 해야 하는지" 판단을 맡깁니다.
    /// 실제 행동 전환 로직은 EnemyBrain에 두고, 이 스크립트는 루프 진입점 역할만 담당합니다.
    /// </summary>
    private IEnumerator CheckRoutine()
    {
        while (myStatus.nowState != EnemyStatus.EnemyState.Dead)
        {
            brain.Tick(
                this,
                transform,
                myStatus,
                navAgent,
                memory,
                perception,
                noiseListener,
                locomotion,
                combat,
                RaiseWalkEvent,
                RaiseLookDirEvent,
                RaiseAttackEvent);

            yield return brain.GetCheckDelay();
        }

        aiRoutine = null;
    }

    /// <summary>
    /// 외부에서 발생한 소음 이벤트를 받아 조사 상태로 전환할지 판단합니다.
    /// 조사에 실제로 반응한 경우에만 이후 순찰 복귀 플래그를 세워 둡니다.
    /// </summary>
    private void HandleNoiseEmitted(NoiseStimulus stimulus)
    {
        if (!enabled || myStatus == null || myStatus.nowState == EnemyStatus.EnemyState.Dead)
        {
            return;
        }

        if (stimulus.Source == gameObject)
        {
            return;
        }

        bool hasRegisteredNoise = noiseListener.TryRegisterNoise(
            perception,
            transform,
            stimulus,
            brain.HasTarget,
            myStatus,
            out bool interruptedChase);

        if (hasRegisteredNoise && interruptedChase)
        {
            // 추적 중이던 대상을 강한 소음으로 끊을 때는 현재 타겟을 비웁니다.
            brain.ClearTarget();
        }

        if (hasRegisteredNoise)
        {
            // 여러 번 소리를 들어도 첫 이탈 지점을 유지해, 복귀 시 같은 기준점으로 돌아가게 합니다.
            memory.CaptureReturnAnchor(transform.position);
            memory.MarkNeedsReturnToPatrol();
        }
    }

    /// <summary>
    /// 적이 죽으면 AI 루프를 멈추고 프레젠테이션 이벤트를 전달합니다.
    /// 현재 공격 중이던 런타임 상태도 함께 정리합니다.
    /// </summary>
    public void Die()
    {
        StopAllCoroutines();
        aiRoutine = null;
        combat.ClearRuntimeState();
        OnDeathEvent?.Invoke();
        Destroy(gameObject, 3.0f);
    }

    /// <summary>
    /// 스폰 직후 받은 패트롤 루트와 시작 인덱스를 순찰 메모리에 기록합니다.
    /// SpawnManager가 적을 생성한 직후 한 번 호출하는 초기화 진입점입니다.
    /// </summary>
    public void InitializeSpawnContext(Vector3 spawnPosition, EnemyPatrolRoute patrolRoute, int startPatrolIndex)
    {
        memory.InitializePatrol(spawnPosition, patrolRoute, startPatrolIndex);
    }

    /// <summary>
    /// 공격 애니메이션의 각 스윙 이벤트를 받아 해당 단계의 돌진과 타격을 실행합니다.
    /// 2연격 구조를 애니메이션 이벤트 기준으로 분리하기 위한 진입점입니다.
    /// </summary>
    public void HandleAttackSwing(int swingIndex)
    {
        if (!enabled || myStatus == null || myStatus.nowState == EnemyStatus.EnemyState.Dead)
        {
            return;
        }

        StartCoroutine(combat.ExecuteSwing(
            transform,
            brain.CurrentTarget,
            attackOrigin != null ? attackOrigin : transform,
            navAgent,
            locomotion,
            RaiseLookDirEvent,
            swingIndex));
    }

    private void RaiseWalkEvent(bool isWalking)
    {
        OnWalkEvent?.Invoke(isWalking);
    }

    private void RaiseAttackEvent()
    {
        OnAttackEvent?.Invoke();
    }

    private void RaiseLookDirEvent(int lookDir, int lookRight)
    {
        OnLookDirEvent?.Invoke(lookDir, lookRight);
    }

    /// <summary>
    /// 인스펙터 직렬화가 비어 있을 때 기본 모듈 인스턴스를 다시 채워 넣습니다.
    /// 프리팹 재연결이나 스크립트 리로드 시 참조가 비는 상황을 방어하기 위한 함수입니다.
    /// </summary>
    private void EnsureModules()
    {
        brain ??= new EnemyBrain();
        memory ??= new EnemyMemory();
        perception ??= new EnemyPerception();
        noiseListener ??= new EnemyNoiseListener();
        locomotion ??= new EnemyLocomotion();
        combat ??= new EnemyCombat();
    }

    /// <summary>
    /// 공격 판정 기준점이 비어 있으면 프리팹 안에서 body/HitRange를 자동 탐색합니다.
    /// 수동 지정이 없어도 기본 프리팹 구조만 맞으면 바로 동작하게 하기 위한 보조 함수입니다.
    /// </summary>
    private void ResolveAttackOrigin()
    {
        if (attackOrigin != null)
        {
            return;
        }

        Transform body = transform.Find("body");
        if (body != null)
        {
            Transform hitRange = body.Find("HitRange");
            if (hitRange != null)
            {
                attackOrigin = hitRange;
                return;
            }
        }

        Transform directHitRange = transform.Find("HitRange");
        attackOrigin = directHitRange != null ? directHitRange : transform;
    }
}
