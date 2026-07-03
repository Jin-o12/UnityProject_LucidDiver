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
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        GlobalEventBus.OnAttackInput -= TryAttack;
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
}
