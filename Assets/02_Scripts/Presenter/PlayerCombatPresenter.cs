using UnityEngine;

/// <summary>
/// 플레이어의 전투 입력을 실제 전투 시스템에 연결하는 Presenter입니다.
/// 기본 공격, 구르기, 액티브 스킬 사용 가능 여부를 검사하고 실행을 요청합니다.
/// </summary>
public class PlayerCombatPresenter : MonoBehaviour
{
    private PlayerWeapon playerWeapon;          // 플레이어가 장착한 무기 정보
    private PlayerStatus playerStatus;          // 플레이어 HP/MP/상태 정보

    private ISkillRepository skillRepo;         // 스킬 데이터 저장소
    private ICharDataRepository charRepo;       // 캐릭터 데이터 저장소

    private float artifactFireSpeedRate;        // 아티팩트 공격 속도 증가율
    private float artifactFireMpDownRate;       // 아티팩트 공격 MP 소모 감소율
    private float nextAttackAvailableTime;      // 다음 기본 공격 가능 시각

    private void Awake()
    {
        playerWeapon = GetComponent<PlayerWeapon>();
        playerStatus = GetComponent<PlayerStatus>();

        if (playerWeapon == null || playerStatus == null)
        {
            enabled = false;
            Debug.LogError("PlayerCombatPresenter: 필요한 컴포넌트가 없습니다.");
            return;
        }

        skillRepo = new LocalJsonSkillRepository();
        charRepo = new SOCharacterRepository();
    }

    private void OnEnable()
    {
        GlobalEventBus.OnAttackInput += TryAttack;
        GlobalEventBus.OnEvadeRequested += TryEvade;
        GlobalEventBus.OnMainActiveSkillRequested += TrySkill;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnAttackInput -= TryAttack;
        GlobalEventBus.OnEvadeRequested -= TryEvade;
        GlobalEventBus.OnMainActiveSkillRequested -= TrySkill;
    }

    private void TryAttack()
    {
        if (playerStatus.nowState != PlayerStatus.livingState.idle)
            return;

        if (Time.time < nextAttackAvailableTime)
            return;

        float useMana = GetModifiedAttackManaCost();

        if (!playerWeapon.isEquipped || playerStatus.mpCurrent < useMana)
            return;

        playerWeapon.PlayerAttack();
        playerStatus.UseAttackMana(useMana);
        nextAttackAvailableTime = Time.time + GetModifiedFireInterval();
    }

    /// <summary>
    /// 아티팩트 장착 효과로 추가되는 공격 속도/공격 MP 소모 보정값을 갱신합니다.
    /// 무기 원본 fireRate/useMana는 보존하고, 공격 시점에 최종값만 계산해 사용합니다.
    /// </summary>
    public void ApplyArtifactCombatBonus(float fireSpeedRate, float fireMpDownRate)
    {
        artifactFireSpeedRate = Mathf.Max(0.0f, fireSpeedRate);
        artifactFireMpDownRate = Mathf.Clamp01(fireMpDownRate);
    }

    private float GetModifiedAttackManaCost()
    {
        if (!playerWeapon.isEquipped)
            return 0.0f;

        return playerWeapon.nowUseMana * (1.0f - artifactFireMpDownRate);
    }

    private float GetModifiedFireInterval()
    {
        if (!playerWeapon.isEquipped)
            return 0.0f;

        float baseFireRate = Mathf.Max(0.0f, playerWeapon.nowFireRate);
        float speedMultiplier = 1.0f + artifactFireSpeedRate;
        return speedMultiplier > 0.0f ? baseFireRate / speedMultiplier : baseFireRate;
    }

    private void TryEvade()
    {
        if (playerStatus.nowState != PlayerStatus.livingState.idle)
            return;

        if (playerStatus.mpCurrent < playerStatus.evadeMP)
            return;

        if (Time.time < playerStatus.lastEvadeTime + playerStatus.evadeCooltime)
            return;

        playerStatus.UseEvadeMana(playerStatus.evadeMP);
    }

    private void TrySkill()
    {
        if (playerStatus.nowState != PlayerStatus.livingState.idle)
            return;

        CharacterData charData = charRepo.GetCharacterData(DataManager.Instance.playerData.SelectCharID);
        SkillData skill = skillRepo.GetSkillData(charData.skillNum);

        bool success = GlobalEventBus.OnRequestManaConsume(skill.mpCost);
        if (!success)
            return;

        CasterStatPayload payload = new CasterStatPayload
        {
            attackPower = playerWeapon.nowAttackPower
        };

        if (TryGetMouseWorldPosition(out Vector3 mousePos))
        {
            SkillEffectProcessor.Instance.UseSkillEffect(skill, gameObject, payload, mousePos);
        }
    }

    private bool TryGetMouseWorldPosition(out Vector3 worldPosition)
    {
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (groundPlane.Raycast(ray, out float enter))
        {
            worldPosition = ray.GetPoint(enter);
            return true;
        }

        worldPosition = Vector3.zero;
        return false;
    }
}
