using System;
using UnityEngine;

/// <summary>
/// 플레이어의 아티팩트 장착 상태를 관리합니다.
/// 인벤토리 데이터와 분리하여 장착 슬롯 3칸의 현재 아티팩트만 보관합니다.
/// </summary>
public class PlayerArtifactEquipment : MonoBehaviour
{
    [SerializeField] private int artifactSlotCount = 3; // 플레이어가 사용할 아티팩트 장착 슬롯 수

    public ArtifactItemData[] equippedArtifacts;       // 현재 장착 중인 아티팩트 목록
    [SerializeField] private int equipAudioID = 10706; // 아티팩트 장착 시 SFX ID

    public event Action<int, ArtifactItemData> OnArtifactSlotChanged; // 장착 슬롯 변경 알림

    private void Awake()
    {
        artifactSlotCount = Mathf.Max(1, artifactSlotCount);
        equippedArtifacts = new ArtifactItemData[artifactSlotCount];
    }

    public int SlotCount => equippedArtifacts != null ? equippedArtifacts.Length : artifactSlotCount;

    /// <summary>
    /// 지정한 장착 슬롯에 들어 있는 아티팩트 데이터를 반환합니다.
    /// </summary>
    public ArtifactItemData GetEquippedArtifact(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
            return null;

        return equippedArtifacts[slotIndex];
    }

    /// <summary>
    /// 세이브 데이터에 저장된 아티팩트 장착 슬롯을 현재 런타임 장착 상태로 복원합니다.
    /// 아이템 원본 데이터는 JSON 아이템 저장소에서 TID 기준으로 다시 찾습니다.
    /// </summary>
    public void RestoreFromSave(PlayerSaveData saveData, IItemDataRepository itemRepository)
    {
        if (equippedArtifacts == null)
            equippedArtifacts = new ArtifactItemData[Mathf.Max(1, artifactSlotCount)];

        for (int i = 0; i < equippedArtifacts.Length; i++)
        {
            equippedArtifacts[i] = null;
            OnArtifactSlotChanged?.Invoke(i, null);
        }

        if (saveData == null || saveData.artifactSlots == null || itemRepository == null)
            return;

        foreach (SaveSlotData slotData in saveData.artifactSlots)
        {
            if (slotData == null || !IsValidSlotIndex(slotData.index) || slotData.TID == 0)
                continue;

            ArtifactItemData artifact = itemRepository.GetItemDataByID(slotData.TID) as ArtifactItemData;
            if (artifact == null)
                continue;

            equippedArtifacts[slotData.index] = artifact;
            OnArtifactSlotChanged?.Invoke(slotData.index, artifact);
        }
    }

    /// <summary>
    /// 현재 장착 중인 아티팩트를 세이브 데이터의 artifactSlots에 기록합니다.
    /// 인벤토리 슬롯에서 빠진 아티팩트가 탈출 정산 중 사라지지 않게 하기 위한 저장 동기화입니다.
    /// </summary>
    public void WriteToSave(PlayerSaveData saveData)
    {
        if (saveData == null)
            return;

        if (saveData.artifactSlots == null)
            saveData.artifactSlots = new System.Collections.Generic.List<SaveSlotData>();

        saveData.artifactSlots.Clear();

        if (equippedArtifacts == null)
            return;

        for (int i = 0; i < equippedArtifacts.Length; i++)
        {
            ArtifactItemData artifact = equippedArtifacts[i];
            if (artifact == null)
                continue;

            saveData.artifactSlots.Add(new SaveSlotData
            {
                index = i,
                TID = artifact.TID,
                amount = 1
            });
        }
    }

    /// <summary>
    /// 지정한 슬롯에 새 아티팩트를 장착하고, 기존 장착 아티팩트가 있으면 previousArtifact로 돌려줍니다.
    /// 실제 인벤토리 이동은 InventoryPresenter가 담당합니다.
    /// </summary>
    public bool EquipArtifact(int slotIndex, ArtifactItemData newArtifact, out ArtifactItemData previousArtifact)
    {
        previousArtifact = null;

        if (!IsValidSlotIndex(slotIndex) || newArtifact == null)
            return false;

        previousArtifact = equippedArtifacts[slotIndex];
        equippedArtifacts[slotIndex] = newArtifact;

        OnArtifactSlotChanged?.Invoke(slotIndex, newArtifact);
        VFXService.Instance?.Play(GameplayVFXIds.ArtifactEquip, transform.position, transform.rotation);

        // 장착 판정이 발생한 시점에 AudioManager에 사운드 출력 이벤트를 발송합니다.
        GlobalEventBus.OnPlay2DSoundRequested?.Invoke(equipAudioID);

        return true;
    }

    /// <summary>
    /// 지정한 슬롯의 아티팩트를 해제하고, 해제된 데이터를 removedArtifact로 반환합니다.
    /// </summary>
    public bool UnequipArtifact(int slotIndex, out ArtifactItemData removedArtifact)
    {
        removedArtifact = null;

        if (!IsValidSlotIndex(slotIndex))
            return false;

        removedArtifact = equippedArtifacts[slotIndex];

        if (removedArtifact == null)
            return false;

        equippedArtifacts[slotIndex] = null;
        OnArtifactSlotChanged?.Invoke(slotIndex, null);
        VFXService.Instance?.Play(GameplayVFXIds.ArtifactUnequip, transform.position, transform.rotation);
        return true;
    }

    /// <summary>
    /// 외부에서 잘못된 슬롯 번호로 접근하는 것을 막기 위한 공통 유효성 검사입니다.
    /// </summary>
    private bool IsValidSlotIndex(int slotIndex)
    {
        return equippedArtifacts != null && slotIndex >= 0 && slotIndex < equippedArtifacts.Length;
    }
}
