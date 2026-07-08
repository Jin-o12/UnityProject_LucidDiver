using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombatPresenter : MonoBehaviour
{
    // 게임 데이터 스크립트 참조
    private PlayerWeapon playerWeapon;                  // 플레이어 무기
    private PlayerStatus playerStatus;                  // 플레이어 상태

    // JSON 데이터 저장소 접근용 리포지토리 인스턴스
    private ISkillRepository skillRepo;
    private ICharDataRepository charRepo;

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

        skillRepo = new LocalJsonSkillRepository();
        charRepo = new SOCharacterRepository();
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
        GlobalEventBus.OnEvadeRequested -= TryEvade;
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
        if (playerStatus.nowState != PlayerStatus.livingState.idle) return;

        // 해당 캐릭터의 고유 스킬을 사용하게 함
        CharacterData charData = charRepo.GetCharacterData(DataManager.Instance.playerData.SelectCharID);
        var skill = skillRepo.GetSkillData(charData.skillNum);

        // 스킬 사용 시 마나 소모 요청
        bool success = GlobalEventBus.OnRequestManaConsume(skill.mpCost);

        // 현재 마나가 스킬을 사용하기에 부족하다면 시전 취소
        if (!success) return;

        // 플레이어 스텟 정보를 넘겨줌
        CasterStatPayload payload = new CasterStatPayload();
        payload.attackPower = playerWeapon.nowAttackPower;

        if(TryGetMouseWorldPosition(out Vector3 mousePos))
        {
            SkillEffectProcessor.Instance.UseSkillEffect(skill, this.gameObject, payload, mousePos);
        }
    }

    /* 현재 마우스의 평면상의 위치*/
    private bool TryGetMouseWorldPosition(out Vector3 worldPosition)
    {
        // 가상의 평면 생성
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        // 카메라에서 마우스 커서 위치를 향해 쏘는 광선(Ray) 생성
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // 광선이 평면과 부딪혔는지 검사
        if (groundPlane.Raycast(ray, out float enter))
        {
            // 거리를 바탕으로 실제 충돌한 3D 좌표를 계산하여 반환
            worldPosition = ray.GetPoint(enter);
            return true;
        }
        // 평면과 만나지 않았을 경우
        worldPosition = Vector3.zero;
        return false;
    }
}
