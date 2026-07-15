using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

/// <summary>
/// ���� UI�� �����ϴ� ��ũ��Ʈ
/// ���� ������ ���� ������ �� ���� �����ϰ�,
/// ���Ŀ��� ���� ������ �����ϴ� ������� �����Ѵ�.
/// </summary>
public class ChestUI : MonoBehaviour
{
    [Header("Chest UI")]
    [SerializeField] private Transform slotContainer;           // 상자 슬롯 컨테이너
    [SerializeField] private GameObject slotPrefab;             // 슬롯 프리팹
    [SerializeField] private List<GameObject> slotsObj = new(); // 슬롯 인스턴스 리스트
    [SerializeField] private Button closeButton;                // 닫기 버튼

    private ItemBox itemBox;                                    // 아이템 박스 컴포넌트
    private PlayerInventory playerInventory;                    // 플레이어 인벤토리 스크립트
    private Action onCloseRequested;                            // 닫기 요청 이벤트

    public int[] box_rummag_AudioIdPool;                        // 상자 UI 채널링 사운드 리스트

    // 저장 데이터 인터페이스
    private IItemDataRepository itemRepo;                       // 아이템 데이터 접근 인터페이스

    // 사운드 리스트
    private int RootSoundID_Legend = 10901;                     // 전설 아이템 획득 사운드
    private int RootSoundID_Rare = 10903;                       // 레어 아이템 획득 사운드
    private int RootSoundID_Normal = 10902;                     // 일반 아이템 획득 사운드

    public static ChestUI ActiveUI { get; private set; }        // 현재 상자 UI 캐시

    private void Awake()
    {
        itemRepo = new LocalJsonItemRepository();

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseUI);
    }

    private void OnEnable()
    {
        // 사운드 재생 이벤트를 AudioManager에 전달하여 2D 오디오 중지
        int boxRummagAudioID = box_rummag_AudioIdPool[UnityEngine.Random.Range(0, box_rummag_AudioIdPool.Length)];
        GlobalEventBus.OnPlay2DSoundRequested?.Invoke(boxRummagAudioID);
    }

    private void OnDisable()
    {
        // 사운드 재생 이벤트를 AudioManager에 전달하여 2D 오디오 중지
        foreach (var _id in box_rummag_AudioIdPool)
        {
            GlobalEventBus.OnStop2DSoundRequested?.Invoke(_id);
        }

        if (ActiveUI == this)
            ActiveUI = null;
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseUI);
    }

    /// <summary>
    /// ü��Ʈ UI�� ���� �����͸� �����Ѵ�.
    /// ���� ���� ������ŭ ������ �����, ������ �����Ѵ�.
    /// </summary>
    public void Bind(ItemBox box, PlayerInventory inventory, Action closeRequested = null)
    {
        itemBox = box;
        playerInventory = inventory;
        onCloseRequested = closeRequested;
        ActiveUI = this;

        if (itemBox == null)
            return;

        // ���� ���� ���� �������� ���� ����
        CreateSlots(itemBox.SlotCount);

        // ���� ���� �������� ���� ����
        RefreshAll();
    }

    /// <summary>
    /// ������ count�� �����Ѵ�.
    /// �̹� ���� ������ ������� ������ �ٽ� ������ �ʴ´�.
    /// </summary>
    public void CreateSlots(int count)
    {
        if (slotsObj.Count == count)
            return;

        // ���� ���� ����
        for (int i = 0; i < slotsObj.Count; i++)
        {
            if (slotsObj[i] != null)
                Destroy(slotsObj[i]);
        }

        slotsObj.Clear();

        // �� ���� ����
        for (int i = 0; i < count; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, slotContainer);
            ChestSlotUI slotUI = newSlot.GetComponent<ChestSlotUI>();

            if (slotUI != null)
                slotUI.Initialize(this, i);

            slotsObj.Add(newSlot);
        }
    }

    /// <summary>
    /// ��� ������ UI�� ���� ���� ������ �������� �����Ѵ�.
    /// </summary>
    public void RefreshAll()
    {
        if (itemBox == null)
            return;

        for (int i = 0; i < slotsObj.Count; i++)
        {
            RefreshSlot(i);
        }
    }

    /// <summary>
    /// Ư�� ���� �ϳ��� �����Ѵ�.
    /// </summary>
    public void RefreshSlot(int slotIndex)
    {
        if (itemBox == null)
            return;

        if (slotIndex < 0 || slotIndex >= slotsObj.Count)
            return;

        ChestSlotUI slotUI = slotsObj[slotIndex].GetComponent<ChestSlotUI>();
        if (slotUI == null)
            return;

        BoxItemEntry entry = itemBox.GetItem(slotIndex);

        // ������ ��� ������ �� ĭ���� ����
        if (entry == null || entry.itemData == null || entry.amount <= 0)
        {
            slotUI.UpdateSlot(0, null);
            return;
        }

        ItemData jsonItemData = itemRepo.GetItemDataByID(entry.itemData.TID);
        if (jsonItemData == null)
        {
            slotUI.UpdateSlot(0, null);
            return;
        }

        // AddressableLoader를 사용하여 비동기로 아이콘을 로드하고 슬롯을 업데이트합니다 (Fire-and-Forget)
        _ = LoadSlotIconAsync(slotUI, jsonItemData.iconAddress, entry.amount);
    }

    /// <summary>
    /// ü��Ʈ ������ �������� �κ��丮�� �ű��.
    /// �κ��丮�� �� �� �� ������ ���ڿ� �����.
    /// </summary>
    public void TryMoveToInventory(int slotIndex)
    {
        if (itemBox == null || playerInventory == null)
            return;

        BoxItemEntry entry = itemBox.GetItem(slotIndex);
        if (entry == null || entry.itemData == null || entry.amount <= 0)
            return;

        int remain = playerInventory.AddItem(itemRepo.GetItemDataByID(entry.itemData.TID), entry.amount);
        int movedAmount = entry.amount - remain;

        if (movedAmount <= 0)
            return;

        // entry 아이템 등급에 따라 사운드를 재생
        int soundID = entry.itemData.itemGrade switch
        {
            ItemGrade.normal    => RootSoundID_Normal,
            ItemGrade.uncommon  => RootSoundID_Normal,
            ItemGrade.rare      => RootSoundID_Rare,
            ItemGrade.epic      => RootSoundID_Rare,
            ItemGrade.legend    => RootSoundID_Legend,
            _                   => RootSoundID_Normal
        };
        GlobalEventBus.OnPlay2DSoundRequested?.Invoke(soundID);

        itemBox.RemoveAmount(slotIndex, movedAmount);
        RefreshAll();
    }

    /// <summary>
    /// ü��Ʈ ������ �������� �κ��丮�� Ư�� ĭ���� �ű��.
    /// ��� �ִ� ĭ�̰ų� ���� ������ ������ ���� �̵��ȴ�.
    /// </summary>
    public void TryMoveToInventorySlot(int chestSlotIndex, int inventorySlotIndex)
    {
        if (itemBox == null || playerInventory == null)
            return;

        BoxItemEntry entry = itemBox.GetItem(chestSlotIndex);
        if (entry == null || entry.itemData == null || entry.amount <= 0)
            return;

        int remain = playerInventory.TryAddToSlot(inventorySlotIndex, itemRepo.GetItemDataByID(entry.itemData.TID), entry.amount);
        int movedAmount = entry.amount - remain;

        if (movedAmount <= 0)
            return;

        itemBox.RemoveAmount(chestSlotIndex, movedAmount);
        RefreshAll();
    }

    /// <summary>
    /// �κ��丮 ������ �������� ���� ������ �ű��.
    /// ���� ���� ������ ������ ä���, ���� ������ �� ���Կ� �ִ´�.
    /// </summary>
    public void TryMoveFromInventory(int inventorySlotIndex)
    {
        if (itemBox == null || playerInventory == null)
            return;

        InventorySlotData inventorySlot = playerInventory.GetSlot(inventorySlotIndex);
        if (inventorySlot == null || playerInventory.IsSlotEmpty(inventorySlotIndex))
            return;

        ItemData itemData = playerInventory.GetSlotItemData(inventorySlotIndex);
        if (itemData == null)
            return;

        int remain = itemBox.TryAddItem(itemData, inventorySlot.amount);
        int movedAmount = inventorySlot.amount - remain;

        if (movedAmount <= 0)
            return;

        playerInventory.RemoveAmount(inventorySlotIndex, movedAmount);
        RefreshAll();
    }

    /// <summary>
    /// �κ��丮 ������ �������� ������ Ư�� ĭ���� �ű��.
    /// ��� ĭ�� ��� �ְų� ���� ������ ������ ���� �̵��ȴ�.
    /// </summary>
    public void TryMoveFromInventorySlot(int inventorySlotIndex, int chestSlotIndex)
    {
        if (itemBox == null || playerInventory == null)
            return;

        InventorySlotData inventorySlot = playerInventory.GetSlot(inventorySlotIndex);
        if (inventorySlot == null || playerInventory.IsSlotEmpty(inventorySlotIndex))
            return;

        ItemData itemData = playerInventory.GetSlotItemData(inventorySlotIndex);
        if (itemData == null)
            return;

        int remain = itemBox.TryAddToSlot(chestSlotIndex, itemData, inventorySlot.amount);
        int movedAmount = inventorySlot.amount - remain;

        if (movedAmount <= 0)
            return;

        playerInventory.RemoveAmount(inventorySlotIndex, movedAmount);
        RefreshAll();
    }

    /// <summary>
    /// ���� UI�� �ݴ´�.
    /// ���� �ݱ� ó���� Presenter �� �ݹ鿡 �ñ��.
    /// </summary>
    public void CloseUI()
    {
        onCloseRequested?.Invoke();
    }

    /// <summary>
    ///   AddressableLoader를 사용하여 아이콘을 로드합니다.
    /// </summary>
    private async Task LoadSlotIconAsync(ChestSlotUI slotUI, string iconAddress, int amount)
    {
        if (slotUI == null)
            return;

        if (string.IsNullOrEmpty(iconAddress))
        {
            Debug.LogWarning($"아이템의 아이콘 주소가 비어있습니다!");
            slotUI.UpdateSlot(amount, null);
            return;
        }

        Sprite loadedIcon = await AddressableLoader.LoadAssetAsync<Sprite>(iconAddress);

        if (slotUI != null)
        {
            slotUI.UpdateSlot(amount, loadedIcon);
        }
    }

    /// <summary>
    ///   slotIndex번 칸에 등록된 아이템의 데이터를 추출합니다.
    /// </summary>
    public ItemData GetItemDataAt(int slotIndex)
    {
        if (itemBox == null)
            return null;

        BoxItemEntry entry = itemBox.GetItem(slotIndex);
        if (entry == null || entry.itemData == null || entry.amount <= 0)
            return null;

        return itemRepo.GetItemDataByID(entry.itemData.TID);
    }
}