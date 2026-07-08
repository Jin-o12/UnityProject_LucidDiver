using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombatPresenter : MonoBehaviour
{
    // 게임 데이터 스크립트 참조
    private PlayerWeapon playerWeapon;                  // 플레이어 무기
    private PlayerStatus playerStatus;                  // 플레이어 상태

    private float skillTimer;                           // 스킬 사용 시 딜레이 타이머

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

        // 첫 실행 시에는 시간이 충분히 흘렀다고 판단하도록 음수 값 설정
        skillTimer = -9999f;

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
        /// 스킬 시전을 위한 조건들 확인 (플레이어 상태, 스킬 쿨타임, 마나량) ///

        // 플레이어 상태가 idle이 아니면 스킬을 사용할 수 없음
        if (playerStatus.nowState != PlayerStatus.livingState.idle) return;

        // 해당 캐릭터의 고유 스킬을 사용하게 함
        CharacterData charData = charRepo.GetCharacterData(DataManager.Instance.playerData.SelectCharID);
        var skill = skillRepo.GetSkillData(charData.skillNum);

        // 스킬 시전 딜레이 시간이 지나지 않았으면 스킬을 사용하지 않음
        if(Time.time-skillTimer < skill.skillCooltime) return;

        // 스킬 사용 시 마나 소모 요청
        bool success = GlobalEventBus.OnRequestManaConsume(skill.mpCost);

        // 현재 마나가 스킬을 사용하기에 부족하다면 시전 취소
        if (!success) return;

        /// 시전을 위한 조건을 모두 통과, 스킬 시전 ///
        
        // 스킬을 시전한 현재 시점의 시간 등록
        skillTimer = Time.time;

        // 플레이어 스텟 정보를 넘겨줌
        CasterStatPayload payload = new CasterStatPayload();
        payload.attackPower = playerWeapon.nowAttackPower;

        // 마우스 위치에 따라 사거리 내 스킬 발사 지점을 지정함
        if(TryGetMouseWorldPosition(skill.fireRange, out Vector3 mousePos))
        {
            GlobalEventBus.OnMainActiveSkillCasted?.Invoke();
            SkillEffectProcessor.Instance.UseSkillEffect(skill, this.gameObject, payload, mousePos);
        }
    }

    /* 현재 마우스의 평면상의 위치*/
    private bool TryGetMouseWorldPosition(float _range, out Vector3 worldPosition)
    {
        // 가상의 평면 생성
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        // 카메라에서 마우스 커서 위치를 향해 쏘는 광선(Ray) 생성
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // 광선이 평면과 부딪혔는지 검사
        if (groundPlane.Raycast(ray, out float enter))
        {
            // 거리를 바탕으로 실제 충돌한 3D 좌표를 계산
            Vector3 hitPoint = ray.GetPoint(enter);

            // 플레이어와 마우스 포인터 사이의 거리를 계산
            float distance = Vector3.Distance(transform.position, hitPoint);

            // 마우스 위치가 지정된 사거리를 벗어났다면
            if (distance > _range)
            {
                // 플레이어 위치에서 마우스 방향으로 최고 사거리 지점을 계산
                Vector3 direction = (hitPoint - transform.position).normalized;
                worldPosition = transform.position + direction * _range;
            }
            else
            {
                // 사거리 내라면 그대로 반환
                worldPosition = hitPoint;
            }
            return true;
        }
        // 평면과 만나지 않았을 경우
        worldPosition = Vector3.zero;
        return false;
    }
}
