using UnityEngine;

/// <summary>
/// 현재 장착 중인 아티팩트 효과를 합산하여 플레이어 관련 시스템에 전달합니다.
/// 원본 스탯을 직접 덮어쓰지 않고, 각 시스템의 아티팩트 보정값만 갱신합니다.
/// </summary>
public class PlayerArtifactEffectController : MonoBehaviour
{
    private PlayerArtifactEquipment artifactEquipment;   // 현재 장착 중인 아티팩트 목록
    private PlayerStatus playerStatus;                   // HP/MP 회복 보정 적용 대상
    private PlayerMovement playerMovement;               // 이동 속도 보정 적용 대상
    private PlayerCombatPresenter combatPresenter;       // 공격 속도/공격 MP 보정 적용 대상
    private TimeLimitController timeLimitController;     // 제한 시간 감소 속도 보정 적용 대상

    private void Awake()
    {
        artifactEquipment = GetComponent<PlayerArtifactEquipment>();
        playerStatus = GetComponent<PlayerStatus>();
        playerMovement = GetComponent<PlayerMovement>();
        combatPresenter = GetComponent<PlayerCombatPresenter>();
    }

    private void OnEnable()
    {
        if (artifactEquipment != null)
            artifactEquipment.OnArtifactSlotChanged += HandleArtifactSlotChanged;
    }

    private void Start()
    {
        // 세이브 로드 직후 이미 복원된 장착 상태가 있을 수 있으므로 시작 시 한 번 전체 계산합니다.
        RecalculateAndApplyEffects();
    }

    private void OnDisable()
    {
        if (artifactEquipment != null)
            artifactEquipment.OnArtifactSlotChanged -= HandleArtifactSlotChanged;
    }

    private void HandleArtifactSlotChanged(int slotIndex, ArtifactItemData artifact)
    {
        RecalculateAndApplyEffects();
    }

    /// <summary>
    /// 장착 중인 모든 아티팩트 효과를 다시 합산합니다.
    /// 누적 오차를 막기 위해 이전 보정값에 더하지 않고, 매번 0부터 전체를 재계산합니다.
    /// </summary>
    private void RecalculateAndApplyEffects()
    {
        float hpRegenBonus = 0.0f;
        float mpRegenBonus = 0.0f;
        float moveSpeedRate = 0.0f;
        float fireSpeedRate = 0.0f;
        float fireMpDownRate = 0.0f;
        float timeLimitSpeedMultiplier = 1.0f;

        if (artifactEquipment != null)
        {
            for (int i = 0; i < artifactEquipment.SlotCount; i++)
            {
                ArtifactItemData artifact = artifactEquipment.GetEquippedArtifact(i);
                if (artifact == null || artifact.equipEffects == null)
                    continue;

                for (int j = 0; j < artifact.equipEffects.Count; j++)
                {
                    ArtifactEquipEffect effect = artifact.equipEffects[j];
                    if (effect == null)
                        continue;

                    ApplyEffectValue(
                        effect,
                        ref hpRegenBonus,
                        ref mpRegenBonus,
                        ref moveSpeedRate,
                        ref fireSpeedRate,
                        ref fireMpDownRate,
                        ref timeLimitSpeedMultiplier);
                }
            }
        }

        playerStatus?.ApplyArtifactStatusBonus(hpRegenBonus, mpRegenBonus);
        playerMovement?.ApplyArtifactMoveSpeedBonus(moveSpeedRate);
        combatPresenter?.ApplyArtifactCombatBonus(fireSpeedRate, fireMpDownRate);

        // TimeLimitController는 플레이어 프리팹이 아니라 씬 시스템 오브젝트에 있으므로 필요할 때 찾아서 적용합니다.
        if (timeLimitController == null)
            timeLimitController = FindObjectOfType<TimeLimitController>();

        timeLimitController?.ApplyArtifactTimeLimitSpeedMultiplier(timeLimitSpeedMultiplier);
    }

    private void ApplyEffectValue(
        ArtifactEquipEffect effect,
        ref float hpRegenBonus,
        ref float mpRegenBonus,
        ref float moveSpeedRate,
        ref float fireSpeedRate,
        ref float fireMpDownRate,
        ref float timeLimitSpeedMultiplier)
    {
        switch (effect.effectType)
        {
            case ArtifactEffectType.hpRegen:
                hpRegenBonus += Mathf.Max(0.0f, effect.effectValue);
                break;

            case ArtifactEffectType.mpRegen:
                mpRegenBonus += Mathf.Max(0.0f, effect.effectValue);
                break;

            case ArtifactEffectType.moveSpeedUp:
                moveSpeedRate += Mathf.Max(0.0f, effect.effectValue);
                break;

            case ArtifactEffectType.fireSpeedUp:
                fireSpeedRate += Mathf.Max(0.0f, effect.effectValue);
                break;

            case ArtifactEffectType.fireMPDown:
                fireMpDownRate += Mathf.Max(0.0f, effect.effectValue);
                break;

            case ArtifactEffectType.timeLimitSpeed:
                // timeLimitSpeed는 0.6처럼 "최종 시간 감소 배율"에 가까운 값입니다.
                // 여러 개가 장착되면 각 배율을 곱해 중첩 적용합니다.
                if (effect.effectValue > 0.0f)
                    timeLimitSpeedMultiplier *= effect.effectValue;
                break;
        }
    }
}
