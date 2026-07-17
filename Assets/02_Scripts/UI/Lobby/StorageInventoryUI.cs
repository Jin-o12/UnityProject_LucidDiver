using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        SafeSlot,
        Artifact,
        QuickSlot,
        StoragePanel,
        InventoryPanel,
        SafeSlotPanel
    }

    private readonly Dictionary<int, Sprite> iconCache = new();          // {TID별 로드 완료 아이콘 캐시}
    private readonly HashSet<int> loadingIconTIDs = new();               // {아이콘 로드 중복 요청 방지}

    [Header("Slot Roots")]
    [SerializeField] private Transform storageSlotRoot;
    [SerializeField] private Transform inventorySlotRoot;
    [SerializeField] private Transform safeSlotRoot;
    [SerializeField] private Transform quickSlotRoot;

    [Header("Buttons")]
    [SerializeField] private Button buttonBackTop;      // 상단 뒤로가기 버튼 참조



    [Header("Capacity Text")]
    [SerializeField] private TMP_Text storageCapacityText;
    [SerializeField] private TMP_Text inventoryCapacityText;

    [Header("Description")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;

    [Header("Panel Drop Zones")]
    [SerializeField] private Graphic storagePanelRaycastTarget;
    [SerializeField] private Graphic inventoryPanelRaycastTarget;
    [SerializeField] private Graphic safeSlotPanelRaycastTarget;

    [Header("Existing UI")]
    [SerializeField] private QuickSlotGroupUI quickSlotGroupUI;

    [Header("Artifact Slots")]
    [SerializeField] private List<ArtifactEquipSlotUI> artifactSlotUIs = new(); // 프리팹 3개 슬롯 연결용
    private readonly List<InventorySlotData> artifactData = new();                // 아티팩트 내부 데이터 목록

    [Header("Drag Preview")]
    [SerializeField] private Vector2 dragPreviewSize = new Vector2(80f, 80f);

    private Canvas mainCanvas;
    private RectTransform dragPreviewRect;
    private Image dragPreviewImage;
    private CanvasGroup dragPreviewCanvasGroup;

    private readonly List<InventorySlotUI> storageSlotUIs = new();
    private readonly List<InventorySlotUI> inventorySlotUIs = new();
    private readonly List<InventorySlotUI> safeSlotUIs = new();
    private readonly List<QuickSlotUI> quickSlotUIs = new();

    private readonly List<InventorySlotData> storageData = new();
    private readonly List<InventorySlotData> inventoryData = new();
    private readonly List<InventorySlotData> safeSlotData = new();
    private readonly List<int> quickSlotTIDs = new();

    private IItemDataRepository itemRepo;

    private PlayerSaveData currentSaveData;

    private AreaType draggingArea;
    private int draggingIndex = -1;

    private void Awake()
    {
        // {로컬 세이브 저장소 연결 (DataManager가 로드한 인스턴스 참조)}
        itemRepo = new LocalJsonItemRepository();

        // {드래그 미리보기 아이콘을 띄울 최상위 Canvas를 찾음}
        mainCanvas = GetComponentInParent<Canvas>();

        // {기존 슬롯 UI 컴포넌트를 수집하고 입력 이벤트를 새 창고 UI에 연결}
        BindSlots();

        // {초기 상태는 빈 슬롯 데이터로 구성한다}
        InitializeEmptyData();
    }

    private void OnEnable()
    {
        // {뒤로가기 버튼 클릭 이벤트를 등록한다}
        if (buttonBackTop != null)
            buttonBackTop.onClick.AddListener(OnClickBack);

        GlobalEventBus.OnArtifactEquipRequested += HandleArtifactEquip;
        GlobalEventBus.OnArtifactUnequipRequested += HandleArtifactUnequip;
        GlobalEventBus.OnTooltipUIOpen += HandleOnTooltipUIOpen;

        LoadFromPlayerData();
    }


    private void OnDisable()
    {
        // {뒤로가기 버튼 클릭 이벤트를 해제하여 중복 등록을 방지한다}
        if (buttonBackTop != null)
            buttonBackTop.onClick.RemoveListener(OnClickBack);

        GlobalEventBus.OnArtifactEquipRequested -= HandleArtifactEquip;
        GlobalEventBus.OnArtifactUnequipRequested -= HandleArtifactUnequip;
        GlobalEventBus.OnTooltipUIOpen -= HandleOnTooltipUIOpen;
    }


    private void OnClickBack()
    {
        // {로비 메인 UI 열기 이벤트를 호출한다}
        GlobalEventBus.OnOpenLobbyUI?.Invoke();
    }



    private void InitializeEmptyData()
    {
        // {창고 데이터를 빈 슬롯으로 초기화한다}
        ClearData(storageData, storageSlotUIs.Count);

        // {인벤토리 데이터를 빈 슬롯으로 초기화한다}
        ClearData(inventoryData, inventorySlotUIs.Count);

        // {각성 보존 슬롯 데이터를 빈 슬롯으로 초기화한다}
        ClearData(safeSlotData, safeSlotUIs.Count);

        // {아티팩트 장착 데이터를 빈 슬롯으로 초기화한다}
        ClearData(artifactData, 3);


        // {퀵슬롯 데이터를 빈 상태로 초기화한다}
        quickSlotTIDs.Clear();

        for (int i = 0; i < quickSlotUIs.Count; i++)
        {
            // {빈 퀵슬롯 TID를 추가한다}
            quickSlotTIDs.Add(0);
        }

        // {초기화된 빈 데이터를 UI에 반영한다}
        RefreshAll();
    }


    private void LoadFromPlayerData()
    {
        // {플레이어 저장 데이터를 불러온다}
        PlayerSaveData saveData = PlayerSaveDataSO.Instance.LoadSaveData();

        if (saveData == null)
        {
            // {저장 데이터가 없으면 빈 슬롯 상태로 초기화한다}
            InitializeEmptyData();
            return;
        }

        // {현재 저장 데이터를 캐싱한다}
        currentSaveData = saveData;

        // {저장 데이터 리스트가 비어 있으면 새 리스트로 보정한다}
        if (saveData.storageSlots == null)
            saveData.storageSlots = new List<SaveSlotData>();

        if (saveData.inventorySlots == null)
            saveData.inventorySlots = new List<SaveSlotData>();

        if (saveData.safeSlots == null)
            saveData.safeSlots = new List<SaveSlotData>();

        if (saveData.artifactSlots == null)
            saveData.artifactSlots = new List<SaveSlotData>();


        if (saveData.quickSlots == null)
            saveData.quickSlots = new List<int>();

        // {창고/인벤토리 런타임 데이터를 빈 슬롯으로 초기화한다}
        ClearData(storageData, storageSlotUIs.Count);
        ClearData(inventoryData, inventorySlotUIs.Count);
        ClearData(safeSlotData, safeSlotUIs.Count);

        // {저장된 창고 슬롯 데이터를 런타임 창고 데이터로 복원한다}
        CopyFromSaveSlots(saveData.storageSlots, storageData, storageSlotUIs.Count);

        // {저장된 인벤토리 슬롯 데이터를 런타임 인벤토리 데이터로 복원한다}
        CopyFromSaveSlots(saveData.inventorySlots, inventoryData, inventorySlotUIs.Count);

        // {저장된 각성 보존 슬롯 데이터를 런타임 인벤토리 데이터로 복원한다}
        CopyFromSaveSlots(saveData.safeSlots, safeSlotData, safeSlotUIs.Count);

        // {저장된 아티팩트 슬롯 데이터를 런타임 아티팩트 데이터로 복원한다}
        ClearData(artifactData, 3);
        CopyFromSaveSlots(saveData.artifactSlots, artifactData, 3);


        // {저장된 퀵슬롯 데이터를 복원한다}
        quickSlotTIDs.Clear();

        for (int i = 0; i < quickSlotUIs.Count; i++)
        {
            // {저장된 퀵슬롯 TID가 있으면 사용하고, 없으면 빈 값으로 처리한다}
            int tid = i < saveData.quickSlots.Count ? saveData.quickSlots[i] : 0;
            quickSlotTIDs.Add(tid);
        }

        // {인벤토리에 없는 아이템이 퀵슬롯에 남아 있으면 제거한다}
        ValidateQuickSlots();

        // {복원된 데이터를 UI에 반영한다}
        RefreshAll();
    }

    private void CopyFromSaveSlots(List<SaveSlotData> source, List<InventorySlotData> target, int maxSize)
    {
        for (int i = 0; i < maxSize; i++)
        {
            target[i] = new InventorySlotData(0, i, 0, null);
        }

        if (source == null)
        {
            return;
        }

        foreach (SaveSlotData slot in source)
        {
            if (slot.index < 0 || slot.index >= maxSize)
            {
                continue;
            }

            target[slot.index] = new InventorySlotData(slot.TID, slot.index, slot.amount, GetIcon(slot.TID));
        }
    }

    private void SaveToPlayerData()
    {
        PlayerSaveData saveData = currentSaveData ?? PlayerSaveDataSO.Instance.LoadSaveData();
        if (saveData == null)
        {
            return;
        }

        currentSaveData = saveData;

        if (saveData.storageSlots == null) saveData.storageSlots = new List<SaveSlotData>();
        if (saveData.inventorySlots == null) saveData.inventorySlots = new List<SaveSlotData>();
        if (saveData.safeSlots == null) saveData.safeSlots = new List<SaveSlotData>();
        if (saveData.artifactSlots == null) saveData.artifactSlots = new List<SaveSlotData>();
        if (saveData.quickSlots == null) saveData.quickSlots = new List<int>();


        WriteSaveSlots(storageData, saveData.storageSlots);
        WriteSaveSlots(inventoryData, saveData.inventorySlots);
        WriteSaveSlots(safeSlotData, saveData.safeSlots);
        WriteSaveSlots(artifactData, saveData.artifactSlots);


        saveData.quickSlots.Clear();
        for (int i = 0; i < quickSlotUIs.Count; i++)
        {
            saveData.quickSlots.Add(i < quickSlotTIDs.Count ? quickSlotTIDs[i] : 0);
        }

        PlayerSaveDataSO.Instance.SaveGameData(saveData);
    }

    private void WriteSaveSlots(List<InventorySlotData> source, List<SaveSlotData> target)
    {
        target.Clear();

        for (int i = 0; i < source.Count; i++)
        {
            InventorySlotData slot = source[i];
            if (slot == null || slot.TID == 0 || slot.amount <= 0)
            {
                continue;
            }

            target.Add(new SaveSlotData
            {
                index = i,
                TID = slot.TID,
                amount = slot.amount
            });
        }
    }

    private int CountInventoryItem(int tid)
    {
        return inventoryData.Where(slot => slot.TID == tid).Sum(slot => slot.amount);
    }

    public int GetStoredItemCount(int tid)
    {
        // {저장 데이터를 먼저 불러와 창고 데이터를 최신 상태로 갱신한다}
        LoadFromPlayerData();

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

    private void BindSlots()
    {
        // {창고 슬롯 수집}
        storageSlotUIs.Clear();
        storageSlotUIs.AddRange(storageSlotRoot.GetComponentsInChildren<InventorySlotUI>(true));

        // {인벤토리 슬롯 수집}
        inventorySlotUIs.Clear();
        inventorySlotUIs.AddRange(inventorySlotRoot.GetComponentsInChildren<InventorySlotUI>(true));

        // {각성 보존 슬롯 수집}
        safeSlotUIs.Clear();
        safeSlotUIs.AddRange(safeSlotRoot.GetComponentsInChildren<InventorySlotUI>(true));

        // {퀵슬롯 수집}
        quickSlotUIs.Clear();
        quickSlotUIs.AddRange(quickSlotRoot.GetComponentsInChildren<QuickSlotUI>(true));

        if (quickSlotGroupUI == null)
        {
            quickSlotGroupUI = quickSlotRoot.GetComponentInParent<QuickSlotGroupUI>();
        }

        BindInventorySlotEvents(storageSlotUIs, AreaType.Storage);
        BindInventorySlotEvents(inventorySlotUIs, AreaType.Inventory);
        BindInventorySlotEvents(safeSlotUIs, AreaType.SafeSlot);
        BindArtifactSlotEvents();
        BindQuickSlotEvents();

        AddPanelDropReceiver(storagePanelRaycastTarget, AreaType.StoragePanel);
        AddPanelDropReceiver(inventoryPanelRaycastTarget, AreaType.InventoryPanel);
        AddPanelDropReceiver(safeSlotPanelRaycastTarget, AreaType.SafeSlotPanel);
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

    private void BindArtifactSlotEvents()
    {
        for (int i = 0; i < artifactSlotUIs.Count; i++)
        {
            if (artifactSlotUIs[i] == null)
            {
                continue;
            }

            // 창고 화면에서는 StorageInventoryUI가 드래그 규칙을 통합 관리합니다.
            // 기존 ArtifactEquipSlotUI의 프리젠터 이벤트와 중복되지 않도록 입력 처리는 전용 Receiver로 넘깁니다.
            artifactSlotUIs[i].enabled = false;
            AddInputReceiver(artifactSlotUIs[i].gameObject, AreaType.Artifact, i);
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

    private void ClearData(List<InventorySlotData> targetData, int size)
    {
        targetData.Clear();

        for (int i = 0; i < size; i++)
        {
            // {빈 슬롯 데이터 생성}
            targetData.Add(new InventorySlotData(0, i, 0, null));
        }
    }

    public void OnSlotClick(int areaValue, int index, PointerEventData eventData)
    {
        AreaType area = (AreaType)areaValue;

        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        // 클릭 시 사운드 재생
        GlobalEventBus.OnClickAudio?.Invoke(true);

        ShowDescription(area, index);

        if (eventData.clickCount < 2)
        {
            return;
        }

        if (area == AreaType.Storage)
        {
            MoveToArea(storageData, index, inventoryData);
        }
        else if (area == AreaType.Inventory)
        {
            MoveToArea(inventoryData, index, storageData);
        }
        else if (area == AreaType.SafeSlot)
        {
            MoveToArea(safeSlotData, index, inventoryData);
        }
        else if (area == AreaType.QuickSlot)
        {
            quickSlotTIDs[index] = 0;
        }
        else if (area == AreaType.Artifact)
        {
            MoveToArea(artifactData, index, inventoryData);
        }

        ValidateQuickSlots();
        SaveToPlayerData();
        RefreshAll();
    }

    public void OnBeginDrag(int areaValue, int index, PointerEventData eventData)
    {
        AreaType area = (AreaType)areaValue;

        if (!HasItem(area, index))
        {
            return;
        }

        // {드래그 출발 슬롯 기록}
        draggingArea = area;
        draggingIndex = index;

        // {드래그 중 마우스를 따라오는 아이콘 생성}
        ShowDragPreview(area, index, eventData.position);
    }

    public void OnDrop(int areaValue, int index)
    {
        if (draggingIndex < 0)
        {
            return;
        }

        AreaType targetArea = (AreaType)areaValue;

        if (targetArea == AreaType.Storage)
        {
            DropToSlot(storageData, index);
        }
        else if (targetArea == AreaType.Inventory)
        {
            DropToSlot(inventoryData, index);
        }
        else if (targetArea == AreaType.SafeSlot)
        {
            DropToSlot(safeSlotData, index);
        }
        else if (targetArea == AreaType.StoragePanel)
        {
            DropToArea(storageData);
        }
        else if (targetArea == AreaType.InventoryPanel)
        {
            DropToArea(inventoryData);
        }
        else if (targetArea == AreaType.SafeSlotPanel)
        {
            DropToArea(safeSlotData);
        }
        else if (targetArea == AreaType.QuickSlot)
        {
            DropToQuickSlot(index);
        }
        else if (targetArea == AreaType.Artifact)
        {
            DropToArtifactSlot(index);
        }

        ValidateQuickSlots();
        SaveToPlayerData();
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

        if (draggingArea == AreaType.SafeSlot && targetList == safeSlotData)
        {
            return;
        }

        if (draggingArea == AreaType.Artifact)
        {
            // 아티팩트 장착 슬롯에서 패널 빈 공간으로 드롭하는 경우는 허용하지 않습니다.
            // 해제는 인벤토리 슬롯 또는 더블클릭 흐름으로만 처리합니다.
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

    private void DropToQuickSlot(int quickSlotIndex)
    {
        if (quickSlotIndex < 0 || quickSlotIndex >= quickSlotTIDs.Count)
        {
            return;
        }

        if (draggingArea == AreaType.QuickSlot)
        {
            SwapQuickSlots(draggingIndex, quickSlotIndex);
            return;
        }

        if (draggingArea != AreaType.Inventory || !IsValid(inventoryData, draggingIndex))
        {
            return;
        }

        int tid = inventoryData[draggingIndex].TID;
        if (tid == 0)
        {
            return;
        }

        if (!CanRegisterQuickSlot(tid))
        {
            Debug.LogWarning("퀵슬롯에는 소비 아이템만 등록할 수 있습니다.");
            return;
        }

        for (int i = 0; i < quickSlotTIDs.Count; i++)
        {
            if (i == quickSlotIndex)
            {
                continue;
            }

            if (quickSlotTIDs[i] == tid)
            {
                quickSlotTIDs[i] = 0;
            }
        }

        quickSlotTIDs[quickSlotIndex] = tid;
    }

    private void SwapQuickSlots(int sourceIndex, int targetIndex)
    {
        if (sourceIndex < 0 || sourceIndex >= quickSlotTIDs.Count)
        {
            return;
        }

        if (targetIndex < 0 || targetIndex >= quickSlotTIDs.Count || sourceIndex == targetIndex)
        {
            return;
        }

        (quickSlotTIDs[sourceIndex], quickSlotTIDs[targetIndex]) = (quickSlotTIDs[targetIndex], quickSlotTIDs[sourceIndex]);
    }

    private void DropToArtifactSlot(int artifactSlotIndex)
    {
        if (artifactSlotIndex < 0 || artifactSlotIndex >= artifactData.Count)
        {
            return;
        }

        if (draggingArea == AreaType.Artifact)
        {
            SwapArtifactSlots(draggingIndex, artifactSlotIndex);
            return;
        }

        if (draggingArea != AreaType.Inventory || !IsValid(inventoryData, draggingIndex))
        {
            return;
        }

        InventorySlotData source = inventoryData[draggingIndex];
        if (source == null || source.TID == 0)
        {
            return;
        }

        if (GetItemData(source.TID) is not ArtifactItemData)
        {
            Debug.LogWarning("아티팩트 아이템만 장착 슬롯에 넣을 수 있습니다.");
            return;
        }

        InventorySlotData target = artifactData[artifactSlotIndex];
        int previousTID = target.TID;

        artifactData[artifactSlotIndex] = new InventorySlotData(source.TID, artifactSlotIndex, 1, GetIcon(source.TID));
        inventoryData[draggingIndex] = new InventorySlotData(previousTID, draggingIndex, previousTID == 0 ? 0 : 1, GetIcon(previousTID));
    }

    private void SwapArtifactSlots(int sourceIndex, int targetIndex)
    {
        if (!IsValid(artifactData, sourceIndex) || !IsValid(artifactData, targetIndex) || sourceIndex == targetIndex)
        {
            return;
        }

        (artifactData[sourceIndex], artifactData[targetIndex]) = (artifactData[targetIndex], artifactData[sourceIndex]);
        artifactData[sourceIndex].order = sourceIndex;
        artifactData[targetIndex].order = targetIndex;
    }

    private bool CanRegisterQuickSlot(int tid)
    {
        ItemData itemData = GetItemData(tid);
        return itemData != null && itemData.category == itemCategory.consume;
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
        else if (draggingArea == AreaType.SafeSlot)
        {
            sourceList = safeSlotData;
        }
        else if (draggingArea == AreaType.Artifact)
        {
            sourceList = artifactData;
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

        if (draggingArea == AreaType.SafeSlot && IsValid(safeSlotData, draggingIndex))
        {
            return safeSlotData[draggingIndex].TID;
        }

        if (draggingArea == AreaType.Artifact && IsValid(artifactData, draggingIndex))
        {
            return artifactData[draggingIndex].TID;
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

            if (CountInventoryItem(quickSlotTIDs[i]) <= 0)
            {
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

        // {각성 보존 슬롯 UI 갱신}
        for (int i = 0; i < safeSlotUIs.Count; i++)
        {
            RefreshInventorySlot(safeSlotUIs[i], safeSlotData[i]);
        }

        // {퀵슬롯 UI 갱신}
        for (int i = 0; i < quickSlotTIDs.Count; i++)
        {
            RefreshQuickSlot(i, quickSlotTIDs[i]);
        }

        // {아티팩트 슬롯 UI 갱신}
        for (int i = 0; i < artifactSlotUIs.Count; i++)
        {
            RefreshArtifactSlot(i);
        }

        RefreshCapacity();
    }


    private void RefreshInventorySlot(InventorySlotUI slotUI, InventorySlotData slotData)
    {
        if (slotUI == null || slotData == null)
        {
            return;
        }

        // 창고 UI도 인게임 인벤토리와 같은 슬롯 프리팹을 사용하므로,
        // TID로 아이템 등급을 조회해 레어도 프레임까지 함께 갱신합니다.
        ItemData itemData = GetItemData(slotData.TID);
        itemCategory category = itemData != null ? itemData.category : itemCategory.empty;
        ItemGrade grade = itemData != null ? itemData.itemGrade : ItemGrade.empty;
        slotUI.UpdateSlot(slotData.amount, GetIcon(slotData.TID), category, grade, SlotType.inventory);
    }

    private void RefreshQuickSlot(int index, int tid)
    {
        Sprite icon = GetIcon(tid);
        int count = tid == 0 ? 0 : CountInventoryItem(tid);
        ItemData itemData = GetItemData(tid);
        ItemGrade grade = itemData != null ? itemData.itemGrade : ItemGrade.empty;

        if (quickSlotGroupUI != null)
        {
            quickSlotGroupUI.UpdateSlot(index, icon, count, grade);
        }
        else if (index < quickSlotUIs.Count)
        {
            quickSlotUIs[index].UpdateSlot(count, icon, grade);
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

    private void RefreshArtifactSlot(int index)
    {
        if (index < 0 || index >= artifactSlotUIs.Count || index >= artifactData.Count) return;
        int tid = artifactData[index].TID;
        if (tid == 0)
        {
            artifactSlotUIs[index].UpdateSlot(null);
        }
        else
        {
            var itemData = GetItemData(tid) as ArtifactItemData;
            artifactSlotUIs[index].UpdateSlot(itemData);
        }
    }

    private void HandleArtifactEquip(int equipSlotIndex, int inventorySlotIndex)
    {
        if (inventorySlotIndex < 0 || inventorySlotIndex >= inventoryData.Count) return;
        if (equipSlotIndex < 0 || equipSlotIndex >= artifactData.Count) return;

        var invSlot = inventoryData[inventorySlotIndex];
        if (invSlot.TID == 0) return;

        var itemData = GetItemData(invSlot.TID);
        var artifactItem = itemData as ArtifactItemData;
        if (artifactItem == null)
        {
            Debug.LogWarning("아티팩트 아이템만 장착할 수 있습니다.");
            return;
        }

        // Swap slot items
        int prevTID = artifactData[equipSlotIndex].TID;
        artifactData[equipSlotIndex].TID = invSlot.TID;
        artifactData[equipSlotIndex].amount = 1;

        invSlot.TID = prevTID;
        invSlot.amount = prevTID == 0 ? 0 : 1;

        SaveToPlayerData();
        RefreshInventorySlot(inventorySlotUIs[inventorySlotIndex], invSlot);
        RefreshArtifactSlot(equipSlotIndex);
        RefreshCapacity();
    }

    private void HandleArtifactUnequip(int equipSlotIndex)
    {
        if (equipSlotIndex < 0 || equipSlotIndex >= artifactData.Count) return;

        int tid = artifactData[equipSlotIndex].TID;
        if (tid == 0) return;

        // Find empty slot in inventory
        int emptyIndex = -1;
        for (int i = 0; i < inventoryData.Count; i++)
        {
            if (inventoryData[i].TID == 0)
            {
                emptyIndex = i;
                break;
            }
        }

        if (emptyIndex == -1)
        {
            Debug.LogWarning("인벤토리가 가득 차서 아티팩트를 해제할 수 없습니다.");
            return;
        }

        inventoryData[emptyIndex].TID = tid;
        inventoryData[emptyIndex].amount = 1;

        artifactData[equipSlotIndex].TID = 0;
        artifactData[equipSlotIndex].amount = 0;

        SaveToPlayerData();
        RefreshInventorySlot(inventorySlotUIs[emptyIndex], inventoryData[emptyIndex]);
        RefreshArtifactSlot(equipSlotIndex);
        RefreshCapacity();
    }

    private void HandleOnTooltipUIOpen(SlotType slot, int index)
    {
        if (slot == SlotType.artifact && index >= 0 && index < artifactData.Count)
        {
            int tid = artifactData[index].TID;
            if (tid != 0)
            {
                ItemData itemData = GetItemData(tid);
                if (itemNameText != null) itemNameText.text = itemData != null ? itemData.itemName : "";
                if (itemDescriptionText != null) itemDescriptionText.text = itemData != null ? itemData.desc : "선택한 아이템이 없습니다.";
            }
        }
    }


    private void ShowDescription(AreaType area, int index)

    {
        int tid = 0;

        if (area == AreaType.Storage && IsValid(storageData, index))
        {
            // {창고 슬롯의 TID를 가져온다}
            tid = storageData[index].TID;
        }
        else if (area == AreaType.Inventory && IsValid(inventoryData, index))
        {
            // {인벤토리 슬롯의 TID를 가져온다}
            tid = inventoryData[index].TID;
        }
        else if (area == AreaType.SafeSlot && IsValid(safeSlotData, index))
        {
            // {각성 보존 슬롯의 TID를 가져온다}
            tid = safeSlotData[index].TID;
        }
        else if (area == AreaType.QuickSlot && index >= 0 && index < quickSlotTIDs.Count)
        {
            // {퀵슬롯의 TID를 가져온다}
            tid = quickSlotTIDs[index];
        }
        else if (area == AreaType.Artifact && IsValid(artifactData, index))
        {
            // {아티팩트 장착 슬롯의 TID를 가져온다}
            tid = artifactData[index].TID;
        }

        // {TID로 아이템 원본 데이터를 가져온다}
        ItemData itemData = GetItemData(tid);

        if (itemNameText != null)
        {
            // {아이템 이름을 표시한다}
            itemNameText.text = itemData != null ? itemData.itemName : "";
        }

        if (itemDescriptionText != null)
        {
            // {아이템 설명을 표시한다}
            itemDescriptionText.text = itemData != null ? itemData.desc : "선택한 아이템이 없습니다.";
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

        if (area == AreaType.SafeSlot && IsValid(safeSlotData, index))
        {
            return safeSlotData[index].TID != 0;
        }

        if (area == AreaType.QuickSlot && index >= 0 && index < quickSlotTIDs.Count)
        {
            return quickSlotTIDs[index] != 0;
        }

        if (area == AreaType.Artifact && IsValid(artifactData, index))
        {
            return artifactData[index].TID != 0;
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
        // {창고 + 인벤토리 + 각성 보존 슬롯의 특정 아이템 총량 계산}
        return storageData.Where(slot => slot.TID == tid).Sum(slot => slot.amount)
             + inventoryData.Where(slot => slot.TID == tid).Sum(slot => slot.amount)
             + safeSlotData.Where(slot => slot.TID == tid).Sum(slot => slot.amount);
    }

    private ItemData GetItemData(int tid)
    {
        // {빈 슬롯이면 아이템 데이터를 반환하지 않는다}
        if (tid == 0)
        {
            return null;
        }

        // {리포지토리에서 TID에 해당하는 아이템 데이터를 찾는다}
        ItemData itemData = itemRepo.GetItemDataByID(tid);
        if (itemData == null)
        {
            Debug.LogWarning($"StorageInventoryUI: ItemData TID {tid}를 찾을 수 없습니다.");
        }

        return itemData;
    }

    private Sprite GetIcon(int tid)
    {
        // {빈 슬롯이면 아이콘을 반환하지 않는다}
        if (tid == 0)
        {
            return null;
        }

        // {이미 로드된 아이콘이 있으면 재사용한다}
        if (iconCache.TryGetValue(tid, out Sprite cachedIcon))
        {
            return cachedIcon;
        }

        // {아이템 데이터를 가져온다}
        ItemData itemData = GetItemData(tid);

        // {아이템 데이터나 아이콘 참조가 없으면 아이콘을 반환하지 않는다}
        if (itemData == null || string.IsNullOrEmpty(itemData.iconAddress))
        {
            return null;
        }

        // {이미 로드 중인 아이콘이면 중복 로드하지 않는다}
        if (loadingIconTIDs.Contains(tid))
        {
            return null;
        }

        // {아이콘 로드 중 상태를 기록한다}
        loadingIconTIDs.Add(tid);

        // {AddressableLoader를 통해 비동기로 아이콘 로드 시작 (Fire-and-Forget)}
        _ = LoadIconAsync(tid, itemData.iconAddress);

        // {로드 완료 전에는 임시로 빈 아이콘을 반환한다}
        return null;
    }

    private async Task LoadIconAsync(int tid, string address)
    {
        Sprite loadedIcon = await AddressableLoader.LoadAssetAsync<Sprite>(address);

        // {아이콘 로드 중 상태를 해제한다}
        loadingIconTIDs.Remove(tid);

        // {아이콘 로드 성공 시 캐시에 저장한다}
        if (loadedIcon != null)
        {
            iconCache[tid] = loadedIcon;

            // {아이콘 로드 후 슬롯 UI를 다시 갱신한다}
            RefreshAll();
        }
        else
        {
            Debug.LogWarning($"StorageInventoryUI: 아이콘 로드 실패 TID {tid}");
        }
    }

    private int GetMaxStack(int tid)
    {
        // {아이템 데이터를 가져온다}
        ItemData itemData = GetItemData(tid);

        // {아이템 데이터가 없거나 중첩 수가 0 이하이면 기본값 1을 사용한다}
        if (itemData == null || itemData.itemMultiple <= 0)
        {
            return 1;
        }

        // {아이템 데이터의 최대 중첩 수를 반환한다}
        return itemData.itemMultiple;
    }

    private bool IsValid(List<InventorySlotData> list, int index)
    {
        // {리스트 인덱스 유효성 검사}
        return list != null && index >= 0 && index < list.Count;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // {드래그 미리보기 아이콘이 없으면 처리하지 않음}
        if (dragPreviewRect == null)
        {
            return;
        }

        // {마우스 위치로 드래그 미리보기 아이콘 이동}
        dragPreviewRect.position = eventData.position;
    }

    public void OnEndDrag()
    {
        // {드래그 종료 시 미리보기 아이콘 제거}
        HideDragPreview();

        // {드롭 대상이 없었던 경우를 대비해 드래그 상태 초기화}
        draggingIndex = -1;
    }

    private void ShowDragPreview(AreaType area, int index, Vector2 screenPosition)
    {
        // {드래그할 아이템 아이콘 가져오기}
        Sprite icon = GetDragPreviewIcon(area, index);

        if (icon == null || mainCanvas == null)
        {
            return;
        }

        // {이전 드래그 미리보기 아이콘이 남아 있으면 제거}
        HideDragPreview();

        // {드래그 미리보기 오브젝트 생성}
        GameObject previewObject = new GameObject("DragPreviewIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        previewObject.transform.SetParent(mainCanvas.transform, false);
        previewObject.transform.SetAsLastSibling();

        // {드래그 미리보기 RectTransform 설정}
        dragPreviewRect = previewObject.GetComponent<RectTransform>();
        dragPreviewRect.sizeDelta = dragPreviewSize;
        dragPreviewRect.position = screenPosition;

        // {드래그 미리보기 Image 설정}
        dragPreviewImage = previewObject.GetComponent<Image>();
        dragPreviewImage.sprite = icon;
        dragPreviewImage.raycastTarget = false;
        dragPreviewImage.preserveAspect = true;

        // {드래그 미리보기 아이콘이 드롭 판정을 막지 않도록 Raycast 차단}
        dragPreviewCanvasGroup = previewObject.GetComponent<CanvasGroup>();
        dragPreviewCanvasGroup.blocksRaycasts = false;
        dragPreviewCanvasGroup.interactable = false;
        dragPreviewCanvasGroup.alpha = 0.85f;
    }

    private void HideDragPreview()
    {
        // {드래그 미리보기 오브젝트 제거}
        if (dragPreviewRect != null)
        {
            Destroy(dragPreviewRect.gameObject);
        }

        // {드래그 미리보기 참조 초기화}
        dragPreviewRect = null;
        dragPreviewImage = null;
        dragPreviewCanvasGroup = null;
    }

    private Sprite GetDragPreviewIcon(AreaType area, int index)
    {
        if (area == AreaType.Storage)
        {
            // {창고 슬롯의 아이콘 반환}
            return GetSlotIcon(storageData, index);
        }

        if (area == AreaType.Inventory)
        {
            // {인벤토리 슬롯의 아이콘 반환}
            return GetSlotIcon(inventoryData, index);
        }

        if (area == AreaType.SafeSlot)
        {
            // {각성 보존 슬롯의 아이콘 반환}
            return GetSlotIcon(safeSlotData, index);
        }

        if (area == AreaType.QuickSlot)
        {
            // {퀵슬롯 인덱스 범위 검사}
            if (index < 0 || index >= quickSlotTIDs.Count)
            {
                return null;
            }

            // {퀵슬롯에 장착된 아이템 아이콘 반환}
            return GetIcon(quickSlotTIDs[index]);
        }

        if (area == AreaType.Artifact)
        {
            // {아티팩트 장착 슬롯의 아이콘 반환}
            return GetSlotIcon(artifactData, index);
        }

        return null;
    }

    private Sprite GetSlotIcon(List<InventorySlotData> sourceList, int index)
    {
        // {슬롯 인덱스 유효성 검사}
        if (!IsValid(sourceList, index))
        {
            return null;
        }

        InventorySlotData slot = sourceList[index];

        // {빈 슬롯이면 아이콘을 반환하지 않음}
        if (slot == null || slot.TID == 0 || slot.amount <= 0)
        {
            return null;
        }

        // {슬롯에 캐싱된 아이콘이 있으면 우선 사용}
        if (slot.icon != null)
        {
            return slot.icon;
        }

        // {캐싱된 아이콘이 없으면 TID로 아이콘 조회}
        return GetIcon(slot.TID);
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
        // {드래그 시작 이벤트와 마우스 위치 전달}
        owner.OnBeginDrag(areaValue, index, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // {드래그 중 마우스 위치 전달}
        owner.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // {드래그 종료 이벤트 전달}
        owner.OnEndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        // {드롭 이벤트 전달}
        owner.OnDrop(areaValue, index);
    }
}
