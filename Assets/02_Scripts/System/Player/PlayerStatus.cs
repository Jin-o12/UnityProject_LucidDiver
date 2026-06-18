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
    public enum livingState { idle, escape, gameover }      // 플레이어가 가질 수 있는 상태의 종류
    public livingState nowState;                            // 현재 플레이어
    public bool isReloading { get; private set; }           // 재장전 실행 중 여부
    
    // 플레이어 체력
    public float hpMax;                                     // 전체 체력
    public float hpCurrent;                                 // 현재 체력

    // 플레이어 마나
    public float mpMax;                                     // 마나 최대값
    public float mpCurrent { get; private set; }            // 현재 마나
    public float manaRegen;                                 // 초탕 마나 화복량

    void Awake()
    {
        // 플레이어 상태 및 스텟 초기화
        nowState = livingState.idle;

        hpMax = 100;
        hpCurrent = hpMax;
        mpMax = 100;
        mpCurrent = mpMax;
        manaRegen = 5.0f;
    }

    private void OnEnable()
    {
        // 플레이어 첫 생성시 전체 플레이어 명단에 본인 등록
        if (!GlobalRuntimeData.ActivePlayers.Contains(this.transform))
        {
            GlobalRuntimeData.ActivePlayers.Add(this.transform);
        }

        /// 이벤트 구독 ///
        //GlobalEventBus.OnAttackInput += UseAttackMana;
    }

    private void OnDisable()
    {
        // 플레이어 디스폰시 현재 플레이어 목록에서 본인 제거
        if (GlobalRuntimeData.ActivePlayers.Contains(this.transform))
        {
            GlobalRuntimeData.ActivePlayers.Remove(this.transform);
        }

        /// 이벤트 구독 해제 ///
        //GlobalEventBus.OnAttackInput -= UseAttackMana;
    }

    void Start()
    {
        // UI 초기 업데이트
        UpdateHp();
        UpdateMp();

        // 코루틴 시작
        StartCoroutine(HealingManaPerSeconds());
    }

    /* 피격 시 자신의 타입을 반환 */
    public Faction EntityFaction => Faction.player;

    /* 피해 입을 시 체력 감소 처리 */
    public void TakeDamage(float dmg)
    {
        hpCurrent = Mathf.Clamp(hpCurrent-dmg, 0, hpMax);
        UpdateHp();
    }

    public void UseAttackMana(float _useMana)
    {
        mpCurrent = Mathf.Clamp(mpCurrent-_useMana, 0, mpMax);
        UpdateMp();
    }

    /* 초상화 UI 업데이트 */
    private void UpdateFaceImage()
    {
        /// 플레이어 데이터 구조가 구축되면 연동하여 업데이트 할 것 ///
    }

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

    /* 초당 마나 회복 코루틴 */
    private IEnumerator HealingManaPerSeconds()
    {
        while(nowState != livingState.gameover)
        {
            mpCurrent = Mathf.Clamp(mpCurrent += manaRegen, 0, mpMax);
            UpdateMp();
            yield return new WaitForSeconds(1.0f);
        }
    }
}
