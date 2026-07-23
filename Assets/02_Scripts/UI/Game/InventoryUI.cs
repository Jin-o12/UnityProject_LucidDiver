using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 인벤토리 UI 출력만 담당한다.
/// 실제 데이터 저장은 하지 않고 슬롯 생성/갱신과 드롭존 표시만 처리한다.
/// </summary>
public class InventoryUI : MonoBehaviour, ICloseAnimatable
{
    [Header("DOTween 연출")]
    [SerializeField] RectTransform inventoryPanel;
    [SerializeField] RectTransform dropPanel;
    CanvasGroup panelGroup;                 //연출 적용 캔버스 그룹
    public float initTime = 0.5f;           //연출 적용 시간
    public float initScale = 0.75f;         //연출용 사이즈
    public float initFade = 0.5f;           //연출용 투명도

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

    [Header("장착 대상 드롭 피드백")]
    [SerializeField] private InventoryDragFeedbackController dropFeedbackController;

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

        if (dropFeedbackController == null)
            dropFeedbackController = GetComponent<InventoryDragFeedbackController>();

        panelGroup = GetComponent<CanvasGroup>();

        HideDropZone();
        HideEquipmentDropFeedback(true);
        DOTween.Init();
    }

    private void OnEnable()
    {
        panelGroup.alpha = initFade;
        ScaleChange(inventoryPanel, initScale, 1f, initTime);
        DOTween.Sequence().Join(panelGroup.DOFade(1f, initTime));

        // Vignette 조절 이벤트를 호출
        GlobalEventBus.OnVignetteChange?.Invoke(0.5f, 0.2f, true);
    }

    private void OnDisable()
    {
        // Vignette 조절 이벤트를 호출
        GlobalEventBus.OnVignetteChange?.Invoke(0.25f, 0.2f, true);

        HideDropZone();
        HideEquipmentDropFeedback(true);
    }

    /* DOTween 시퀀스로 스케일 애니메이션 재생 */
    private void ScaleChange(RectTransform rect, float init, float end, float duration)
    {
        rect.localScale = Vector3.one * init;
        DOTween.Sequence().Append(rect.DOScale(end, duration));
    }

    /* DOTween 시퀀스로 UI 닫기 애니메이션 재생 */
    public void PlayCloseAnimation(Action onComplete)
    {
        if (inventoryPanel == null)
        {
            onComplete?.Invoke();
            return;
        }

        inventoryPanel.localScale = Vector3.one;
        DOTween.Sequence().SetAutoKill(false).
            Append(inventoryPanel.DOScale(initScale, initTime)).
            Join(panelGroup.DOFade(initFade, initTime)).
            OnComplete(() =>
            {
                onComplete?.Invoke();
            });
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
                slotUI.Initialize(j + invenCnt, false);

            safeSlotsObj.Add(newSlot);
        }
    }

    public void UpdateSlot(int slotNum, InventorySlotData slotData)
    {
        InventorySlotUI slotUI = slotsObj[slotNum].GetComponent<InventorySlotUI>();
        itemCategory category = slotData.itemData != null ? slotData.itemData.category : itemCategory.empty;
        ItemGrade grade = slotData.itemData != null ? slotData.itemData.itemGrade : ItemGrade.empty;
        slotUI.UpdateSlot(slotData.amount, slotData.icon, category, grade, SlotType.inventory, slotData.itemData, slotData.TID);
    }

    public void UpdateSafeSlot(int slotNum, InventorySlotData slotData)
    {
        InventorySlotUI slotUI = safeSlotsObj[slotNum].GetComponent<InventorySlotUI>();
        itemCategory category = slotData.itemData != null ? slotData.itemData.category : itemCategory.empty;
        ItemGrade grade = slotData.itemData != null ? slotData.itemData.itemGrade : ItemGrade.empty;
        slotUI.UpdateSlot(slotData.amount, slotData.icon, category, grade, SlotType.inventory, slotData.itemData, slotData.TID);
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

        ScaleChange(dropPanel, initScale, 1f, initScale);
        dropZoneCanvasGroup.alpha = 1f;
        dropZoneCanvasGroup.blocksRaycasts = true;
        dropZoneCanvasGroup.interactable = true;
    }

    public void HideDropZone()
    {
        if (dropZoneCanvasGroup == null)
            return;

        ScaleChange(dropPanel, 1f, initScale, initTime);

        dropPanel.localScale = Vector3.one;
        DOTween.Sequence().Append(dropPanel.DOScale(0f, initTime)).
            OnComplete(() =>
            {
                dropZoneCanvasGroup.alpha = 0f;
                dropZoneCanvasGroup.blocksRaycasts = false;
                dropZoneCanvasGroup.interactable = false;
            });
    }

    /// <summary>
    /// 드래그 중인 인벤토리 슬롯을 기준으로 장착 가능한 목적지 슬롯만 강조합니다.
    /// </summary>
    public void ShowEquipmentDropFeedback(InventorySlotUI sourceSlot)
    {
        if (dropFeedbackController != null)
            dropFeedbackController.ShowFor(sourceSlot);
    }

    /// <summary>
    /// 장착 가능 슬롯 강조를 숨깁니다. UI 종료나 강제 취소 시에는 즉시 숨길 수 있습니다.
    /// </summary>
    public void HideEquipmentDropFeedback(bool immediate = false)
    {
        if (dropFeedbackController != null)
            dropFeedbackController.HideAll(immediate);
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

        HideEquipmentDropFeedback(true);
    }
}
