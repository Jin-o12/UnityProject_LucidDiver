using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적 프리팹에 실제로 붙어 있는 호스트 스크립트입니다.
/// 기존 프리팹 연결은 유지하면서, 내부 판단/감지/이동/전투 책임을
/// 각 보조 모듈에 위임하는 진입점 역할을 맡습니다.
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    [Header("Enemy AI Modules")]
    [SerializeField] private EnemyBrain brain = new EnemyBrain();                   // 상위 의사결정 담당
    [SerializeField] private EnemyPerception perception = new EnemyPerception();    // 시야/청각 감지 담당
    [SerializeField] private EnemyNoiseListener noiseListener = new EnemyNoiseListener(); // 조사 상태와 소음 반응 담당
    [SerializeField] private EnemyLocomotion locomotion = new EnemyLocomotion();    // 이동, 방향 전환, 돌진 담당
    [SerializeField] private EnemyCombat combat = new EnemyCombat();                // 공격 판정과 2연격 담당

    [Header("Attack Anchor")]
    [SerializeField] private Transform attackOrigin;                                // 실제 근접 타격 거리 계산의 기준점

    [Header("Required Components")]
    private EnemyStatus myStatus;                                                   // 상태/스탯 보유 컴포넌트
    private NavMeshAgent navAgent;                                                  // 네비게이션 이동 컴포넌트

    public float SightLength => perception.SightRange;
    public float AwarenessRange => perception.AwarenessRange;
    public float HearingRange => perception.HearingRange;
    public float SightAngle => perception.SightAngle;
    public float EyeHeight => perception.EyeHeight;
    public Transform CurrentTarget => brain.CurrentTarget;

    public event Action<bool> OnWalkEvent;                                          // 걷기 애니메이션 갱신 이벤트
    public event Action OnAttackEvent;                                              // 공격 애니메이션 재생 이벤트
    public event Action OnDeathEvent;                                               // 사망 애니메이션 재생 이벤트
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

        // 공격 기준점은 프리팹에서 수동 지정하지 않아도 body/HitRange를 우선 탐색합니다.
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
        StartCoroutine(CheckRoutine());
    }

    private void OnDisable()
    {
        if (myStatus == null)
        {
            return;
        }

        myStatus.OnLocalDeath -= Die;
        GlobalEventBus.OnNoiseEmitted -= HandleNoiseEmitted;
    }

    /// <summary>
    /// 일정 주기마다 브레인에게 "지금 무엇을 해야 하는지" 판단을 맡깁니다.
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
                perception,
                noiseListener,
                locomotion,
                combat,
                RaiseWalkEvent,
                RaiseLookDirEvent,
                RaiseAttackEvent);

            yield return brain.GetCheckDelay();
        }
    }

    /// <summary>
    /// 외부에서 발생한 소음 자극을 받아 조사 상태로 전환할지 판단합니다.
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

        if (noiseListener.TryRegisterNoise(
                perception,
                transform.position,
                stimulus,
                brain.HasTarget,
                myStatus,
                out bool interruptedChase) &&
            interruptedChase)
        {
            // 추적이 끊기는 강한 소음이면 현재 타겟을 비우고 조사로 넘깁니다.
            brain.ClearTarget();
        }
    }

    /// <summary>
    /// 적 사망 시 AI 루프를 멈추고 프레젠테이션 이벤트를 전달합니다.
    /// </summary>
    public void Die()
    {
        StopAllCoroutines();
        combat.ClearRuntimeState();
        OnDeathEvent?.Invoke();
        Destroy(gameObject, 3.0f);
    }

    /// <summary>
    /// 공격 애니메이션의 각 스윙 이벤트를 받아 해당 단계의 돌진과 피해를 실행합니다.
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
    /// 인스펙터 직렬화가 비어 있을 때 기본 모듈 인스턴스를 다시 채웁니다.
    /// </summary>
    private void EnsureModules()
    {
        brain ??= new EnemyBrain();
        perception ??= new EnemyPerception();
        noiseListener ??= new EnemyNoiseListener();
        locomotion ??= new EnemyLocomotion();
        combat ??= new EnemyCombat();
    }

    /// <summary>
    /// 공격 판정 기준점이 비어 있으면 프리팹 내부의 HitRange를 자동 탐색합니다.
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
