/// <summary>
/// 플레이어의 상태를 관리하는 스크립트
/// [26.06.16_강다영] 플레이어의 기본적인 스텟의 변화가 서로 다른 씬에서 일어날 상황에 대비해 기본값 초기화를 Awake에서 수행함. 추후 변동 가능
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatus : MonoBehaviour, IEffectReceiver
{
    // 플레이어 상태
    public enum livingState { idle, escape, gameover }      // 플레이어가 가질 수 있는 상태의 종류
    public livingState nowState { get; private set; }       // 현재 플레이어
    public void SetPlayerState(livingState _state) => nowState = _state;
    public bool isReloading { get; private set; }           // 재장전 실행 중 여부
    public bool canSprint { get; private set; }             // 달리기 실행 가능 여부
    public bool cannotSprint;                               // 마나 소진 시 달리기 실행 불가 상태
    public float sprintRecoverTime;                         // 달리기 실행 불가 회복 시간
    public float evadeCooltime;                             // 구르기 쿨타임
    public float lastEvadeTime;                             // 최근 구르기 실행 시점

    int playerID;                                           // 플레이어 고유 번호

    // 플레이어 체력
    public float hpMax;                                     // 전체 체력
    public float hpCurrent;                                 // 현재 체력

    // 플레이어 마나
    public float mpMax;                                     // 마나 최대값
    public float mpCurrent { get; private set; }            // 현재 마나
    public float manaRegen;                                 // 초당 마나 회복량
    public float sprintMP;                                  // 달리기 중 초당 마나 소비량
    public float evadeMP;                                   // 구르기 시 마나 소비량

    private LocalInputReader _input;                        // 플레이어 인게임 조작 입력
    private PlayerMovement _movement;                       // 플레이어 이동 조작

    void Awake()
    {
        // EntityIdentity에서 플레이어 고유 번호를 가져옴
        playerID = GetComponent<EntityIdentity>().entityID;
        // 플레이어의 조작 관련 컴포넌트를 가져옴
        _input = GetComponent<LocalInputReader>();
        _movement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        /// 이벤트 구독 ///
        GlobalEventBus.OnGainManaRequested += GainMana;
        GlobalEventBus.OnHealRequested += HealingHealth;
        GlobalEventBus.OnSprintInput += CanSprint;
        GlobalEventBus.OnSprintManaConsume += UseSprintMana;
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        GlobalEventBus.OnGainManaRequested -= GainMana;
        GlobalEventBus.OnHealRequested -= HealingHealth;
        GlobalEventBus.OnSprintInput -= CanSprint;
        GlobalEventBus.OnSprintManaConsume -= UseSprintMana;
    }

    void Start()
    {
        ResultServiceLocator.Instance.Register(playerID, this);

        // UI 초기 업데이트
        UpdateHp();
        UpdateMp();

        // 초당 마나 소모 코루틴 시작
        StartCoroutine(HealingManaPerSeconds());
    }

    void FixedUpdate()  //현재 상태를 확인하여 입력 동작 여부를 체크
    {
        // 플레이어 상태가 idle인 경우에 입력 동작을 처리함
        bool canInput = (nowState == livingState.idle);
        if (_input != null) _input.enabled = canInput;
        if (_movement != null) _movement.enabled = canInput;
    }

    #region Status Management
    /* 플레이어 상태 및 스텟 초기화 */
    public void initialize(float _hp, float _mp, float _regen, float _sMP, float _sTime, float _eMana, float _eCooltime)
    {
        nowState = livingState.idle;

        hpMax = _hp;
        hpCurrent = hpMax;
        mpMax = _mp;
        mpCurrent = mpMax;
        manaRegen = _regen;
        sprintMP = _sMP;
        cannotSprint = false;
        sprintRecoverTime = _sTime;
        evadeMP = _eMana;
        evadeCooltime = _eCooltime;
        lastEvadeTime = Time.time;
    }

    /* 피격 시 자신의 타입을 반환 */
    public Faction EntityFaction => Faction.player;

    /* 체력 UI 업데이트 */
    private void UpdateHp()
    {
        GlobalEventBus.OnPlayerHealthChanged?.Invoke(hpCurrent, hpMax);
    }

    /* 마나 UI 업데이트 */
    private void UpdateMp()
    {
        GlobalEventBus.OnPlayerManaChanged?.Invoke(mpCurrent, mpMax);
    }

#region 플레이어 상태 변화 및 수치 조절에 사용되는 메소드
    /* 체력 변화 */
    private void GetHp(float _val)
    {
        hpCurrent = Mathf.Clamp(hpCurrent + _val, 0, hpMax);
        UpdateHp();
        // 플레이어 체력이 0이 되었을 때 사망 처리 이벤트 및 게임오버 메소드를 발동
        if (hpCurrent <= 0)
        {
            GlobalEventBus.onPlayerDead?.Invoke(playerID);
            GameOver(playerID);
        }
    }

    /* 마나 변화 */
    private void GetMp(float _val)
    {
        mpCurrent = Mathf.Clamp(mpCurrent + _val, 0, mpMax);
        UpdateMp();
    }
#endregion

    /* 게임 오버 처리 */
    public void GameOver(int _playerID)
    {
        // 플레이어 상태가 idle이 아니면 탈출 판정을 시작하지 않음
        if (!IsPlayerIdle(_playerID)) return;
        // 플레이어 상태를 gameover로 변경
        ResultServiceLocator.Instance.HandleEscapeGameover(_playerID);
        // 코루틴 정지
        StopAllCoroutines();
        // 탈출 실패 판정 이벤트를 전송
        GlobalEventBus.OnEscapeRequest?.Invoke(false);
        // 스크립트 비활성화
        enabled = false;
    }

    // 플레이어가 idle 상태인지 확인
    public static bool IsPlayerIdle(int playerID)
    {
        var svc = ResultServiceLocator.Instance;
        // ResltServiceLocator가 제대로 로드되었는지 체크
        if (svc == null)
        {
            Debug.LogWarning($"IsPlayerIdle: ResultServiceLocator.Instance {svc} is null");
            return false;
        }
        else
        {
            Debug.Log($"IsPlayerIdle: ResultServiceLocator.Instance {svc} is loaded");
        }

        var comp = svc.GetPlayerComponent(playerID) as PlayerStatus;
        if (comp == null)
        {
            Debug.LogWarning($"PlayerStatus를 찾을 수 없습니다. playerID: {playerID}");
            return false;
        }
        return comp.nowState == livingState.idle;
    }

    /* 현재 달리기 가능한 상태인지 체크 */
    public void CanSprint(bool _isSprint)
    {
        // 현재 보유 마나가 1초당 달리기 시 소비 마나 이상인지 체크
        bool canSprint_Mana = mpCurrent >= sprintMP;
        // 달리기 동작 여부 최종 계산 후 달리기 이벤트 전송
        // (isSprint 입력 && 보유 마나가 1초당 소비 마나 이상 && 달리기 불가 상태 아님)
        canSprint = _isSprint && canSprint_Mana && !cannotSprint;
        GlobalEventBus.SendCanSprint?.Invoke(canSprint);
    }
    #endregion

    /* 피해 입을 시 체력 감소 처리 */
    public void TakeDamage(float dmg)
    {
        // 구르기 도중에는 플레이어가 피해를 받아 HP가 감소하지 않음
        if (_movement.isEvading) return;

        GetHp(-dmg);

        // 피해 입을 시 애니메이션 재생 이벤트
        GlobalEventBus.OnHitAnimate?.Invoke();

        // 피해 입을 시 탈출 실패 처리
        GlobalEventBus.OnEscapeFailure?.Invoke(playerID);
    }

    /* 공격 시 마나 사용 */
    public void UseAttackMana(float _useMana)
    {
        GetMp(-_useMana);
    }

    /* 달리기 중 시간당 마나 사용 */
    public void UseSprintMana(float _useMana)
    {
        GetMp(-_useMana * Time.fixedDeltaTime);

        // 마나 소비 후 보유 마나가 1초당 달리기 시 소비 마나 미만이 되면 달리기 불가 상태 전송 후 달리기 중단
        if (mpCurrent < sprintMP)
        {
            cannotSprint = true;
            GlobalEventBus.SendCannotSprint?.Invoke(cannotSprint);
            StartCoroutine(SprintRecover());
            return;
        }
    }

    /* 달리기 불가 상태 회복 코루틴*/
    private IEnumerator SprintRecover()
    {
        // 달리기 회복 시간 동안 대기 후 달리기 불가 상태를 해제
        yield return new WaitForSeconds(sprintRecoverTime);
        cannotSprint = false;
        GlobalEventBus.SendCannotSprint?.Invoke(cannotSprint);
    }

    /* 마나 사용 후 구르기 실행 */
    public void UseEvadeMana(float _useMana)
    {
        GetMp(-_useMana);
        _movement.PlayerEvade();
        lastEvadeTime = Time.time;
    }

    /* 초당 마나 회복 코루틴 */
    private IEnumerator HealingManaPerSeconds()
    {
        while(nowState != livingState.gameover)
        {
            GetMp(manaRegen);
            yield return new WaitForSeconds(1.0f);
        }
    }

    private void HealingHealth(GameObject _target, float _effectValue)
    {
        // 대상이 내가 아니라면 리턴
        if(_target!=this.gameObject) return;
        GetHp(_effectValue);
    }

    private void GainMana(GameObject _target, float _effectValue)
    {
        // 대상이 내가 아니라면 리턴
        if(_target!=this.gameObject) return;
        GetMp(_effectValue);
    }

    /* 초상화 UI 업데이트 */
    private void UpdateFaceImage()
    {
        /// 플레이어 데이터 구조가 구축되면 연동하여 업데이트 할 것 ///
    }

    /* 아이템을 사용해 체력 회복 */
    public void HealthRecoverInst(float _amount)
    {
        GetHp(_amount);
    }

    /* 아이템을 사용해 마나 회복 */
    public void ManaRecoverInst(float _amount)
    {
        GetMp(_amount);
    }
}
