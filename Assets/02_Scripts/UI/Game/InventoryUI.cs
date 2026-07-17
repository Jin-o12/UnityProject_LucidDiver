using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 인벤토리 UI 출력만 담당한다.
/// 실제 데이터 저장은 하지 않고 슬롯 생성/갱신과 드롭존 표시만 처리한다.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Inventory UI")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private Transform safeSlotContainer;
    [SerializeField] private List<GameObject> slotsObj = new();
    [SerializeField] private List<GameObject> safeSlotsObj = new();
    [SerializeField] private GameObject slotPrefab;

    [Header("Drop Zone UI")]
    [SerializeField] private InventoryDropZoneUI dropZone;

    [Header("Artifact Equip UI")]
    [SerializeField] private ArtifactEquipSlotUI[] artifactEquipSlots;  // 인벤토리 상단의 아티팩트 장착 슬롯 UI 목록

    [Header("Quickslot UI")]
    public QuickSlotGroupUI quickSlot;                // 인벤토리 상단의 소비품 장착 퀵슬롯 UI

    private CanvasGroup dropZoneCanvasGroup;
    private bool canUseDropZone = true;

    private void Awake()
    {
        if (dropZone == null)
            dropZone = GetComponentInChildren<InventoryDropZoneUI>(true);

        if (dropZone != null)
            dropZoneCanvasGroup = dropZone.GetComponent<CanvasGroup>();

        HideDropZone();
    }

    public void CreatSlots(int count)
    {
        if (slotsObj.Count == count)
            return;

        for (int i = 0; i < slotsObj.Count; i++)
        {
            if (slotsObj[i] != null)
                Destroy(slotsObj[i]);
        }

        slotsObj.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, slotContainer);
            InventorySlotUI slotUI = newSlot.GetComponent<InventorySlotUI>();

            if (slotUI != null)
                slotUI.Initialize(i);

            slotsObj.Add(newSlot);
        }
    }

    // 각성 보존 슬롯 생성
    public void CreateSafeSlots(int count, int slotIndexOffset)
    {
        if (safeSlotsObj.Count == count)
            return;

        for (int j = 0; j < safeSlotsObj.Count; j++)
        {
            if (safeSlotsObj[j] != null)
                Destroy(safeSlotsObj[j]);
        }

        safeSlotsObj.Clear();

        int invenCnt = slotsObj.Count;

        for (int j = 0; j < count; j++)
        {
            GameObject newSlot = Instantiate(slotPrefab, safeSlotContainer);
            InventorySlotUI slotUI = newSlot.GetComponent<InventorySlotUI>();

            if (slotUI != null)
                slotUI.Initialize(j + invenCnt);

            safeSlotsObj.Add(newSlot);
        }
    }

    public void UpdateSlot(int slotNum, InventorySlotData slotData)
    {
        InventorySlotUI slotUI = slotsObj[slotNum].GetComponent<InventorySlotUI>();
        itemCategory category = slotData.itemData != null ? slotData.itemData.category : itemCategory.empty;
        ItemGrade grade = slotData.itemData != null ? slotData.itemData.itemGrade : ItemGrade.empty;
        slotUI.UpdateSlot(slotData.amount, slotData.icon, category, grade, SlotType.inventory);
    }

    public void UpdateSafeSlot(int slotNum, InventorySlotData slotData)
    {
        InventorySlotUI slotUI = safeSlotsObj[slotNum].GetComponent<InventorySlotUI>();
        itemCategory category = slotData.itemData != null ? slotData.itemData.category : itemCategory.empty;
        ItemGrade grade = slotData.itemData != null ? slotData.itemData.itemGrade : ItemGrade.empty;
        slotUI.UpdateSlot(slotData.amount, slotData.icon, category, grade, SlotType.inventory);
    }

    public void SetDropZoneAvailable(bool available)
    {
        canUseDropZone = available;

        if (!canUseDropZone)
            HideDropZone();
    }

    public void ShowDropZone()
    {
        if (!canUseDropZone || dropZoneCanvasGroup == null)
            return;

        dropZoneCanvasGroup.alpha = 1f;
        dropZoneCanvasGroup.blocksRaycasts = true;
        dropZoneCanvasGroup.interactable = true;
    }

    public void HideDropZone()
    {
        if (dropZoneCanvasGroup == null)
            return;

        dropZoneCanvasGroup.alpha = 0f;
        dropZoneCanvasGroup.blocksRaycasts = false;
        dropZoneCanvasGroup.interactable = false;
    }

    public void UpdateArtifactSlot(int slotIndex, ArtifactItemData artifact)
    {
        if (artifactEquipSlots == null || slotIndex < 0 || slotIndex >= artifactEquipSlots.Length)
            return;

        if (artifactEquipSlots[slotIndex] == null)
            return;

        artifactEquipSlots[slotIndex].UpdateSlot(artifact);
    }

    /// <summary>
    /// 인벤토리를 열 때 현재 플레이어가 장착 중인 아티팩트 상태를 장착 슬롯 UI에 반영합니다.
    /// </summary>
    public void UpdateArtifactSlots(PlayerArtifactEquipment equipment)
    {
        if (equipment == null || artifactEquipSlots == null)
            return;

        for (int i = 0; i < artifactEquipSlots.Length; i++)
        {
            UpdateArtifactSlot(i, equipment.GetEquippedArtifact(i));
        }
    }

    /* 인벤토리 비활성화 될 때, 드래그 중이던 슬롯을 안전하게 복구 */
    public void ResetAllSlotsDragState()
    {
        if (slotsObj != null)
        {
            foreach (var slotObj in slotsObj)
            {
                if (slotObj != null && slotObj.TryGetComponent<InventorySlotUI>(out var slotUI))
                    slotUI.ResetDragState();
            }
        }
        
        if (safeSlotsObj != null)
        {
            foreach (var slotObj in safeSlotsObj)
            {
                if (slotObj != null && slotObj.TryGetComponent<InventorySlotUI>(out var slotUI))
                    slotUI.ResetDragState();
            }
        }
    }
}
