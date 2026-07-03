using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombatPresenter : MonoBehaviour
{
    // 게임 데이터 스크립트 참조
    private PlayerWeapon playerWeapon;                  // 플레이어 무기
    private PlayerStatus playerStatus;                  // 플레이어 상태

    private void Awake()
    {
        playerWeapon = GetComponent<PlayerWeapon>();
        playerStatus = GetComponent<PlayerStatus>();
        
        if(playerWeapon==null || playerStatus==null)
        {
            this.enabled = false;
            Debug.LogError("InventoryPresenter: 필요한 컴포넌트가 없습니다.");
            return;
        }
    }

    private void OnEnable()
    {
        /// 이벤트 구독 ///
        GlobalEventBus.OnAttackInput += TryAttack;
        GlobalEventBus.OnEvadeRequested += TryEvade;
        GlobalEventBus.OnMainActiveSkillRequested += TrySkill;
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        GlobalEventBus.OnAttackInput -= TryAttack;
        GlobalEventBus.OnEvadeRequested += TryEvade;
        GlobalEventBus.OnMainActiveSkillRequested -= TrySkill;
    }

    private void TryAttack()
    {
        // 플레이어 상태가 idle이 아니면 공격을 수행하지 않음
        if (playerStatus.nowState != PlayerStatus.livingState.idle)
            return;

        // 무기를 가지고 있지 않거나 현재 마나가 소비 할 마나보다 부족하다면 공격하지 않음
        if(!playerWeapon.isEquipped || playerStatus.mpCurrent<playerWeapon.nowUseMana)
            return;

        playerWeapon.PlayerAttack();
        playerStatus.UseAttackMana(playerWeapon.nowUseMana);
    }

    private void TryEvade()
    {
        // 플레이어 상태가 idle이 아니면 구르기를 수행하지 않음
        if (playerStatus.nowState != PlayerStatus.livingState.idle)
            return;

        // 현재 마나가 소비할 마나보다 부족하다면 구르기 불가
        if (playerStatus.mpCurrent < playerStatus.evadeMP)
            return;

        // 최근 구르기 사용 후 쿨타임이 경과하지 않았다면 구르기 불가
        if (Time.time < playerStatus.lastEvadeTime + playerStatus.evadeCooltime)
            return;

        playerStatus.UseEvadeMana(playerStatus.evadeMP);
    }

    private void TrySkill()
    {
        // 플레이어 상태가 idle이 아니면 스킬을 사용할 수 없음
        if (playerStatus.nowState != PlayerStatus.livingState.idle)
            return;

        Debug.Log("스킬 키 입력 실행됨");
    }
}
