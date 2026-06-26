/// <summary>
/// 플레이어의 상태를 관리하는 스크립트
/// [26.06.16_강다영] 플레이어의 기본적인 스텟의 변화가 서로 다른 씬에서 일어날 상황에 대비해 기본값 초기화를 Awake에서 수행함. 추후 변동 가능
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatus : MonoBehaviour, IDamageable
{
    // 플레이어 상태
    public enum livingState { idle, down, escape, gameover }    // 플레이어가 가질 수 있는 상태의 종류
    public livingState nowState { get; private set; }           // 현재 플레이어 상태
    public void SetPlayerState(livingState _state) => nowState = _state;
    public bool isReloading { get; private set; }               // 재장전 실행 중 여부

    int playerID;                                               // 플레이어 고유 번호
    
    // 플레이어 체력
    public float hpMax;                                         // 전체 체력
    public float hpCurrent;                                     // 현재 체력

    // 플레이어 마나
    public float mpMax;                                         // 마나 최대값
    public float mpCurrent { get; private set; }                // 현재 마나
    public float manaRegen;                                     // 초탕 마나 회복량

    private LocalInputReader _input;                            // 플레이어 인게임 조작 입력
    private PlayerMovement _movement;                           // 플레이어 이동 조작

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
        // 플레이어 첫 생성시 전체 플레이어 명단에 본인 등록
        int entityId = 0;
        if (!GlobalRuntimeData.GetActivePlayers().Contains(this.gameObject))
            entityId = GlobalRuntimeData.CountingPlayerData(this.gameObject);
        this.GetComponent<EntityIdentity>().SetupIdentity(entityId, Faction.player);

        /// 이벤트 구독 ///
        GlobalEventBus.OnGainManaRequested += GainMana;
        GlobalEventBus.OnHealRequested += HealingHealth;
    }

    private void OnDisable()
    {
        // 플레이어 디스폰시 현재 플레이어 목록에서 본인 제거
        GlobalRuntimeData.RemovePlayer(playerID);

        /// 이벤트 구독 해제 ///
        GlobalEventBus.OnGainManaRequested -= GainMana;
        GlobalEventBus.OnHealRequested -= HealingHealth;

        // PlayerStatus에서 자기 자신이 등록된 경우에만 제거 (다른 플레이어 인스턴스가 덮어쓴 경우 제거 안 함)
        try
        {
            ResultServiceLocator.Instance?.UnregisterIfOwner(playerID, this);
        }
        catch { }
    }

    void Start()
    {
        // PlayerStatus에서 Locator 등록
        try { ResultServiceLocator.Instance?.Register(playerID, this); }
        catch { Debug.LogWarning("PlayerStatus.Start: ResultServiceLocator.Instance.Register 호출 실패 (타이밍 문제)."); }

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
    public void initialize(float _hp, float _mp, float _regen)
    {
        nowState = livingState.idle;

        hpMax = _hp;
        hpCurrent = hpMax;
        mpMax = _mp;
        mpCurrent = mpMax;
        manaRegen = _regen;
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

    /* 체력 변화 */
    private void GetHp(float _val)
    {
        hpCurrent = Mathf.Clamp(hpCurrent+_val, 0, hpMax);
        UpdateHp();
        // 플레이어 체력이 0이 되었을 때 사망 처리 이벤트 및 게임오버 메소드를 발동
        if (hpCurrent <= 0)
        {
            GlobalEventBus.onPlayerDead?.Invoke(playerID);
            GameOver();
        }
    }

    /* 게임 오버 처리 */
    public void GameOver()  
    {
        // 플레이어 상태가 이미 사망했으면 사망 판정을 시작하지 않음
        if (nowState==livingState.gameover) return;

        // 플레이어 상태를 gameover로 변경
        nowState = livingState.gameover;

        // 탈출 실패 판정 이벤트를 전송
        GlobalEventBus.OnEscapeRequest?.Invoke(false);
    }

    /* 마나 변화 */
    private void GetMp(float _val)
    {
        mpCurrent = Mathf.Clamp(mpCurrent+_val, 0, mpMax);
        UpdateMp();
    }
#endregion

    /* 피해 입을 시 체력 감소 처리 */
    public void TakeDamage(float dmg)
    {
        GetHp(-dmg);
    }

    /* 공격 시 마나 사용 */
    public void UseAttackMana(float _useMana)
    {
        GetMp(-_useMana);
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
}
