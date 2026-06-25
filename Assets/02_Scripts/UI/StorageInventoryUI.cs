using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StorageInventoryUI : MonoBehaviour
{
    private enum AreaType
    {
        Storage,
        Inventory,
        QuickSlot,
        StoragePanel,
        InventoryPanel
    }

    [System.Serializable]
    private class ItemMeta
    {
        public int TID;
        public string itemName;
        [TextArea] public string description;
        public Sprite icon;
        public int maxStack = 5;
    }

    [Header("Slot Roots")]
    [SerializeField] private Transform storageSlotRoot;
    [SerializeField] private Transform inventorySlotRoot;
    [SerializeField] private Transform quickSlotRoot;

    [Header("Capacity Text")]
    [SerializeField] private TMP_Text storageCapacityText;
    [SerializeField] private TMP_Text inventoryCapacityText;

    [Header("Description")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;

    [Header("Panel Drop Zones")]
    [SerializeField] private Graphic storagePanelRaycastTarget;
    [SerializeField] private Graphic inventoryPanelRaycastTarget;

    [Header("Existing UI")]
    [SerializeField] private QuickSlotGroupUI quickSlotGroupUI;

    [Header("Item Meta")]
    [SerializeField] private List<ItemMeta> itemMetaList = new();

    [Header("Debug Test")]
    [SerializeField] private bool useDebugData = true;
    [SerializeField] private int debugPotionTID = 2;
    [SerializeField] private int debugStorageFillCount = 19;
    [SerializeField] private int debugInventoryFillCount = 19;
    [SerializeField] private int debugStackAmount = 5;

    private readonly List<InventorySlotUI> storageSlotUIs = new();
    private readonly List<InventorySlotUI> inventorySlotUIs = new();
    private readonly List<QuickSlotUI> quickSlotUIs = new();

    private readonly List<InventorySlotData> storageData = new();
    private readonly List<InventorySlotData> inventoryData = new();
    private readonly List<int> quickSlotTIDs = new();

    private AreaType draggingArea;
    private int draggingIndex = -1;

    private void Awake()
    {
        // {기존 슬롯 UI 컴포넌트를 수집하고 입력 이벤트를 새 창고 UI에 연결}
        BindSlots();

        // {테스트용 초기 데이터를 구성}
        BuildDebugData();

        // {전체 UI 표시 갱신}
        RefreshAll();
    }

    public int GetStoredItemCount(int tid)
    {
        // {특정 TID 아이템의 창고 총수량을 반환}
        return storageData.Where(slot => slot.TID == tid).Sum(slot => slot.amount);
    }

    public int GetEquippedQuickSlotTID(int index)
    {
        // {출격 준비/인게임에서 사용할 퀵슬롯 TID 반환}
        if (index < 0 || index >= quickSlotTIDs.Count)
        {
            return 0;
        }

        return quickSlotTIDs[index];
    }

    public void LoadStorageData(List<InventorySlotData> newStorageData, int quickSlot1TID, int quickSlot2TID)
    {
        // {저장 데이터에서 창고 데이터와 퀵슬롯 장착 정보를 불러옴}
        ClearData(storageData, storageSlotUIs.Count);
        ClearData(inventoryData, inventorySlotUIs.Count);

        CopyData(newStorageData, storageData, storageSlotUIs.Count);

        quickSlotTIDs.Clear();
        quickSlotTIDs.Add(quickSlot1TID);
        quickSlotTIDs.Add(quickSlot2TID);

        RefreshAll();
    }

    private void BindSlots()
    {
        // {창고 슬롯 수집}
        storageSlotUIs.Clear();
        storageSlotUIs.AddRange(storageSlotRoot.GetComponentsInChildren<InventorySlotUI>(true));

        // {인벤토리 슬롯 수집}
        inventorySlotUIs.Clear();
        inventorySlotUIs.AddRange(inventorySlotRoot.GetComponentsInChildren<InventorySlotUI>(true));

        // {퀵슬롯 수집}
        quickSlotUIs.Clear();
        quickSlotUIs.AddRange(quickSlotRoot.GetComponentsInChildren<QuickSlotUI>(true));

        if (quickSlotGroupUI == null)
        {
            quickSlotGroupUI = quickSlotRoot.GetComponentInParent<QuickSlotGroupUI>();
        }

        BindInventorySlotEvents(storageSlotUIs, AreaType.Storage);
        BindInventorySlotEvents(inventorySlotUIs, AreaType.Inventory);
        BindQuickSlotEvents();

        AddPanelDropReceiver(storagePanelRaycastTarget, AreaType.StoragePanel);
        AddPanelDropReceiver(inventoryPanelRaycastTarget, AreaType.InventoryPanel);
    }

    private void BindInventorySlotEvents(List<InventorySlotUI> slotUIs, AreaType area)
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            // {기존 InventorySlotUI의 slotIndex를 현재 목록 순서와 맞춤}
            slotUIs[i].slotIndex = i;

            // {기존 드래그 이벤트 중복을 막고, UpdateSlot 함수만 재사용}
            slotUIs[i].enabled = false;

            AddInputReceiver(slotUIs[i].gameObject, area, i);
        }
    }

    private void BindQuickSlotEvents()
    {
        for (int i = 0; i < quickSlotUIs.Count; i++)
        {
            // {기존 QuickSlotUI의 slotIndex를 현재 목록 순서와 맞춤}
            quickSlotUIs[i].slotIndex = i;

            // {기존 드래그 이벤트 중복을 막고, UpdateSlot 함수만 재사용}
            quickSlotUIs[i].enabled = false;

            AddInputReceiver(quickSlotUIs[i].gameObject, AreaType.QuickSlot, i);
        }
    }

    private void AddInputReceiver(GameObject target, AreaType area, int index)
    {
        if (target == null)
        {
            return;
        }

        // {슬롯 클릭/더블클릭/드롭 이벤트를 StorageInventoryUI로 전달하는 컴포넌트 추가}
        StorageInventoryInputReceiver receiver = target.GetComponent<StorageInventoryInputReceiver>();
        if (receiver == null)
        {
            receiver = target.AddComponent<StorageInventoryInputReceiver>();
        }

        receiver.Initialize(this, (int)area, index);

        Graphic graphic = target.GetComponent<Graphic>();
        if (graphic != null)
        {
            // {슬롯이 마우스 입력을 받을 수 있도록 설정}
            graphic.raycastTarget = true;
        }
    }

    private void AddPanelDropReceiver(Graphic panelGraphic, AreaType area)
    {
        if (panelGraphic == null)
        {
            return;
        }

        // {패널 빈 공간에 드롭할 수 있도록 설정}
        panelGraphic.raycastTarget = true;
        AddInputReceiver(panelGraphic.gameObject, area, -1);
    }

    private void BuildDebugData()
    {
        // {슬롯 데이터 리스트 크기를 UI 슬롯 개수와 맞춤}
        ClearData(storageData, storageSlotUIs.Count);
        ClearData(inventoryData, inventorySlotUIs.Count);

        quickSlotTIDs.Clear();
        for (int i = 0; i < quickSlotUIs.Count; i++)
        {
            quickSlotTIDs.Add(0);
        }

        if (!useDebugData)
        {
            return;
        }

        // {시안 테스트용으로 창고/인벤토리를 19칸 채움}
        FillDebugSlots(storageData, debugPotionTID, debugStorageFillCount);
        FillDebugSlots(inventoryData, debugPotionTID, debugInventoryFillCount);
    }

    private void FillDebugSlots(List<InventorySlotData> targetData, int tid, int fillCount)
    {
        ItemMeta meta = GetMeta(tid);
        int count = Mathf.Min(fillCount, targetData.Count);

        for (int i = 0; i < count; i++)
        {
            // {테스트 아이템 데이터 배치}
            targetData[i] = new InventorySlotData(tid, i, debugStackAmount, meta != null ? meta.icon : null);
        }
    }

    private void ClearData(List<InventorySlotData> targetData, int size)
    {
        targetData.Clear();

        for (int i = 0; i < size; i++)
        {
            // {빈 슬롯 데이터 생성}
            targetData.Add(new InventorySlotData(0, i, 0, null));
        }
    }

    private void CopyData(List<InventorySlotData> source, List<InventorySlotData> target, int maxSize)
    {
        for (int i = 0; i < maxSize; i++)
        {
            if (source != null && i < source.Count)
            {
                InventorySlotData slot = source[i];
                target[i] = new InventorySlotData(slot.TID, i, slot.amount, GetIcon(slot.TID));
            }
            else
            {
                target[i] = new InventorySlotData(0, i, 0, null);
            }
        }
    }

    public void OnSlotClick(int areaValue, int index, PointerEventData eventData)
    {
        AreaType area = (AreaType)areaValue;

        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        // {클릭한 아이템 설명 표시}
        ShowDescription(area, index);

        if (eventData.clickCount < 2)
        {
            return;
        }

        // {더블클릭 이동 처리}
        if (area == AreaType.Storage)
        {
            MoveToArea(storageData, index, inventoryData);
        }
        else if (area == AreaType.Inventory)
        {
            MoveToArea(inventoryData, index, storageData);
        }
        else if (area == AreaType.QuickSlot)
        {
            quickSlotTIDs[index] = 0;
        }

        RefreshAll();
    }

    public void OnBeginDrag(int areaValue, int index)
    {
        AreaType area = (AreaType)areaValue;

        if (!HasItem(area, index))
        {
            return;
        }

        // {드래그 출발 슬롯 기록}
        draggingArea = area;
        draggingIndex = index;
    }

    public void OnDrop(int areaValue, int index)
    {
        if (draggingIndex < 0)
        {
            return;
        }

        AreaType targetArea = (AreaType)areaValue;

        // {드래그 도착 위치에 따라 이동 처리}
        if (targetArea == AreaType.Storage)
        {
            DropToSlot(storageData, index);
        }
        else if (targetArea == AreaType.Inventory)
        {
            DropToSlot(inventoryData, index);
        }
        else if (targetArea == AreaType.StoragePanel)
        {
            DropToArea(storageData);
        }
        else if (targetArea == AreaType.InventoryPanel)
        {
            DropToArea(inventoryData);
        }
        else if (targetArea == AreaType.QuickSlot)
        {
            EquipQuickSlot(index);
        }

        draggingIndex = -1;
        RefreshAll();
    }

    private void MoveToArea(List<InventorySlotData> sourceList, int sourceIndex, List<InventorySlotData> targetList)
    {
        if (!IsValid(sourceList, sourceIndex) || sourceList[sourceIndex].TID == 0)
        {
            return;
        }

        InventorySlotData source = sourceList[sourceIndex];
        int remain = source.amount;
        int maxStack = GetMaxStack(source.TID);

        // {같은 아이템 슬롯에 먼저 중첩}
        for (int i = 0; i < targetList.Count; i++)
        {
            if (remain <= 0)
            {
                break;
            }

            if (targetList[i].TID != source.TID)
            {
                continue;
            }

            int moveAmount = Mathf.Min(maxStack - targetList[i].amount, remain);
            targetList[i].amount += moveAmount;
            remain -= moveAmount;
        }

        // {남은 수량은 빈 슬롯으로 이동}
        for (int i = 0; i < targetList.Count; i++)
        {
            if (remain <= 0)
            {
                break;
            }

            if (targetList[i].TID != 0)
            {
                continue;
            }

            int moveAmount = Mathf.Min(maxStack, remain);
            targetList[i] = new InventorySlotData(source.TID, i, moveAmount, GetIcon(source.TID));
            remain -= moveAmount;
        }

        // {원본 슬롯 수량 갱신}
        sourceList[sourceIndex].amount = remain;

        if (sourceList[sourceIndex].amount <= 0)
        {
            sourceList[sourceIndex] = new InventorySlotData(0, sourceIndex, 0, null);
        }

        ValidateQuickSlots();
    }

    private void DropToArea(List<InventorySlotData> targetList)
    {
        if (draggingArea == AreaType.Storage && targetList == storageData)
        {
            return;
        }

        if (draggingArea == AreaType.Inventory && targetList == inventoryData)
        {
            return;
        }

        if (!TryGetDraggingSource(out List<InventorySlotData> sourceList))
        {
            return;
        }

        MoveToArea(sourceList, draggingIndex, targetList);
    }

    private void DropToSlot(List<InventorySlotData> targetList, int targetIndex)
    {
        if (!TryGetDraggingSource(out List<InventorySlotData> sourceList))
        {
            return;
        }

        if (!IsValid(sourceList, draggingIndex) || !IsValid(targetList, targetIndex))
        {
            return;
        }

        if (sourceList == targetList && draggingIndex == targetIndex)
        {
            return;
        }

        InventorySlotData source = sourceList[draggingIndex];
        InventorySlotData target = targetList[targetIndex];

        if (source.TID == 0)
        {
            return;
        }

        if (target.TID == 0)
        {
            // {빈 슬롯에 드롭하면 이동}
            targetList[targetIndex] = new InventorySlotData(source.TID, targetIndex, source.amount, GetIcon(source.TID));
            sourceList[draggingIndex] = new InventorySlotData(0, draggingIndex, 0, null);
        }
        else if (target.TID == source.TID)
        {
            // {같은 아이템이면 중첩}
            int maxStack = GetMaxStack(source.TID);
            int moveAmount = Mathf.Min(maxStack - target.amount, source.amount);

            targetList[targetIndex].amount += moveAmount;
            sourceList[draggingIndex].amount -= moveAmount;

            if (sourceList[draggingIndex].amount <= 0)
            {
                sourceList[draggingIndex] = new InventorySlotData(0, draggingIndex, 0, null);
            }
        }
        else
        {
            // {다른 아이템이면 교환}
            targetList[targetIndex] = new InventorySlotData(source.TID, targetIndex, source.amount, GetIcon(source.TID));
            sourceList[draggingIndex] = new InventorySlotData(target.TID, draggingIndex, target.amount, GetIcon(target.TID));
        }

        ValidateQuickSlots();
    }

    private void EquipQuickSlot(int quickSlotIndex)
    {
        if (quickSlotIndex < 0 || quickSlotIndex >= quickSlotTIDs.Count)
        {
            return;
        }

        int tid = GetDraggingTID();

        if (tid == 0)
        {
            return;
        }

        // {퀵슬롯은 아이템을 이동시키지 않고 장착 TID만 저장}
        quickSlotTIDs[quickSlotIndex] = tid;
    }

    private bool TryGetDraggingSource(out List<InventorySlotData> sourceList)
    {
        sourceList = null;

        if (draggingArea == AreaType.Storage)
        {
            sourceList = storageData;
        }
        else if (draggingArea == AreaType.Inventory)
        {
            sourceList = inventoryData;
        }

        return sourceList != null;
    }

    private int GetDraggingTID()
    {
        if (draggingArea == AreaType.Storage && IsValid(storageData, draggingIndex))
        {
            return storageData[draggingIndex].TID;
        }

        if (draggingArea == AreaType.Inventory && IsValid(inventoryData, draggingIndex))
        {
            return inventoryData[draggingIndex].TID;
        }

        return 0;
    }

    private void ValidateQuickSlots()
    {
        for (int i = 0; i < quickSlotTIDs.Count; i++)
        {
            if (quickSlotTIDs[i] == 0)
            {
                continue;
            }

            if (CountTotalItem(quickSlotTIDs[i]) <= 0)
            {
                // {창고/인벤토리에 없는 아이템은 퀵슬롯에서 자동 해제}
                quickSlotTIDs[i] = 0;
            }
        }
    }

    private void RefreshAll()
    {
        // {창고 슬롯 UI 갱신}
        for (int i = 0; i < storageSlotUIs.Count; i++)
        {
            RefreshInventorySlot(storageSlotUIs[i], storageData[i]);
        }

        // {인벤토리 슬롯 UI 갱신}
        for (int i = 0; i < inventorySlotUIs.Count; i++)
        {
            RefreshInventorySlot(inventorySlotUIs[i], inventoryData[i]);
        }

        // {퀵슬롯 UI 갱신}
        for (int i = 0; i < quickSlotTIDs.Count; i++)
        {
            RefreshQuickSlot(i, quickSlotTIDs[i]);
        }

        RefreshCapacity();
    }

    private void RefreshInventorySlot(InventorySlotUI slotUI, InventorySlotData slotData)
    {
        // {기존 InventorySlotUI의 표시 함수 재사용}
        slotUI.UpdateSlot(slotData.amount, GetIcon(slotData.TID));
    }

    private void RefreshQuickSlot(int index, int tid)
    {
        Sprite icon = GetIcon(tid);
        int count = tid == 0 ? 0 : 1;

        if (quickSlotGroupUI != null)
        {
            // {기존 QuickSlotGroupUI의 표시 함수 재사용}
            quickSlotGroupUI.UpdateSlot(index, icon, count);
        }
        else if (index < quickSlotUIs.Count)
        {
            // {QuickSlotGroupUI가 없을 경우 QuickSlotUI 직접 갱신}
            quickSlotUIs[index].UpdateSlot(count, icon);
        }
    }

    private void RefreshCapacity()
    {
        if (storageCapacityText != null)
        {
            // {창고 소지칸 수 갱신}
            storageCapacityText.text = $"{CountOccupied(storageData)} / {storageData.Count}";
        }

        if (inventoryCapacityText != null)
        {
            // {인벤토리 소지칸 수 갱신}
            inventoryCapacityText.text = $"{CountOccupied(inventoryData)} / {inventoryData.Count}";
        }
    }

    private void ShowDescription(AreaType area, int index)
    {
        int tid = 0;

        if (area == AreaType.Storage && IsValid(storageData, index))
        {
            tid = storageData[index].TID;
        }
        else if (area == AreaType.Inventory && IsValid(inventoryData, index))
        {
            tid = inventoryData[index].TID;
        }
        else if (area == AreaType.QuickSlot && index >= 0 && index < quickSlotTIDs.Count)
        {
            tid = quickSlotTIDs[index];
        }

        ItemMeta meta = GetMeta(tid);

        if (itemNameText != null)
        {
            // {아이템 이름 표시}
            itemNameText.text = meta != null ? meta.itemName : "";
        }

        if (itemDescriptionText != null)
        {
            // {아이템 설명 표시}
            itemDescriptionText.text = meta != null ? meta.description : "선택한 아이템이 없습니다.";
        }
    }

    private bool HasItem(AreaType area, int index)
    {
        if (area == AreaType.Storage && IsValid(storageData, index))
        {
            return storageData[index].TID != 0;
        }

        if (area == AreaType.Inventory && IsValid(inventoryData, index))
        {
            return inventoryData[index].TID != 0;
        }

        if (area == AreaType.QuickSlot && index >= 0 && index < quickSlotTIDs.Count)
        {
            return quickSlotTIDs[index] != 0;
        }

        return false;
    }

    private int CountOccupied(List<InventorySlotData> list)
    {
        // {아이템이 들어있는 슬롯 개수 계산}
        return list.Count(slot => slot.TID != 0 && slot.amount > 0);
    }

    private int CountTotalItem(int tid)
    {
        // {창고와 인벤토리의 특정 아이템 총량 계산}
        return storageData.Where(slot => slot.TID == tid).Sum(slot => slot.amount)
             + inventoryData.Where(slot => slot.TID == tid).Sum(slot => slot.amount);
    }

    private Sprite GetIcon(int tid)
    {
        ItemMeta meta = GetMeta(tid);
        return meta != null ? meta.icon : null;
    }

    private int GetMaxStack(int tid)
    {
        ItemMeta meta = GetMeta(tid);
        return meta != null ? Mathf.Max(1, meta.maxStack) : 1;
    }

    private ItemMeta GetMeta(int tid)
    {
        if (tid == 0)
        {
            return null;
        }

        return itemMetaList.FirstOrDefault(meta => meta.TID == tid);
    }

    private bool IsValid(List<InventorySlotData> list, int index)
    {
        // {리스트 인덱스 유효성 검사}
        return list != null && index >= 0 && index < list.Count;
    }
}

public class StorageInventoryInputReceiver : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private StorageInventoryUI owner;
    private int areaValue;
    private int index;

    public void Initialize(StorageInventoryUI newOwner, int newAreaValue, int newIndex)
    {
        // {StorageInventoryUI에 입력 이벤트를 전달하기 위한 초기화}
        owner = newOwner;
        areaValue = newAreaValue;
        index = newIndex;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // {클릭/더블클릭 이벤트 전달}
        owner.OnSlotClick(areaValue, index, eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // {드래그 시작 이벤트 전달}
        owner.OnBeginDrag(areaValue, index);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // {기존 InventorySlotUI를 비활성화했으므로 별도 드래그 아이콘은 사용하지 않음}
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // {드래그 종료 후 별도 처리 없음}
    }

    public void OnDrop(PointerEventData eventData)
    {
        // {드롭 이벤트 전달}
        owner.OnDrop(areaValue, index);
    }
}