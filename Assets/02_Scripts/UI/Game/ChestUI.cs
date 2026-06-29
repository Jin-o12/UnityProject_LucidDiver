using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 상자 UI를 관리하는 스크립트
/// 상자 슬롯은 고정 개수로 한 번만 생성하고,
/// 이후에는 슬롯 내용을 갱신하는 방식으로 동작한다.
/// </summary>
public class ChestUI : MonoBehaviour
{
    [Header("Chest UI")]
    [SerializeField] private Transform slotContainer;           // 체스트 슬롯이 생성될 부모
    [SerializeField] private GameObject slotPrefab;             // 체스트 슬롯 프리팹
    [SerializeField] private List<GameObject> slotsObj = new(); // 생성된 슬롯 오브젝트 목록
    [SerializeField] private Button closeButton;                // 닫기 버튼

    private ItemBox itemBox;                                    // 현재 열려 있는 상자 데이터
    private PlayerInventory playerInventory;                    // 플레이어 인벤토리 참조
    private Action onCloseRequested;                            // 닫기 요청 콜백

    public static ChestUI ActiveUI { get; private set; }        // 현재 열려 있는 체스트 UI

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseUI);
    }

    private void OnDisable()
    {
        if (ActiveUI == this)
            ActiveUI = null;
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseUI);
    }

    /// <summary>
    /// 체스트 UI와 상자 데이터를 연결한다.
    /// 상자 슬롯 개수만큼 슬롯을 만들고, 내용을 갱신한다.
    /// </summary>
    public void Bind(ItemBox box, PlayerInventory inventory, Action closeRequested = null)
    {
        itemBox = box;
        playerInventory = inventory;
        onCloseRequested = closeRequested;
        ActiveUI = this;

        if (itemBox == null)
            return;

        // 고정 슬롯 개수 기준으로 슬롯 생성
        CreateSlots(itemBox.SlotCount);

        // 현재 상자 내용으로 슬롯 갱신
        RefreshAll();
    }

    /// <summary>
    /// 슬롯을 count개 생성한다.
    /// 이미 같은 개수로 만들어져 있으면 다시 만들지 않는다.
    /// </summary>
    public void CreateSlots(int count)
    {
        if (slotsObj.Count == count)
            return;

        // 기존 슬롯 제거
        for (int i = 0; i < slotsObj.Count; i++)
        {
            if (slotsObj[i] != null)
                Destroy(slotsObj[i]);
        }

        slotsObj.Clear();

        // 새 슬롯 생성
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
    /// 모든 슬롯의 UI를 현재 상자 데이터 기준으로 갱신한다.
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
    /// 특정 슬롯 하나만 갱신한다.
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

        // 슬롯이 비어 있으면 빈 칸으로 갱신
        if (entry == null || entry.itemData == null || entry.amount <= 0)
        {
            slotUI.UpdateSlot(0, null);
            return;
        }

        // 아이콘이 필요한 슬롯이면 비동기로 불러와서 갱신
        LoadSlotIcon(slotUI, entry);
    }

    /// <summary>
    /// 체스트 슬롯의 아이템을 인벤토리로 옮긴다.
    /// 인벤토리에 다 못 들어간 수량은 상자에 남긴다.
    /// </summary>
    public void TryMoveToInventory(int slotIndex)
    {
        if (itemBox == null || playerInventory == null)
            return;

        BoxItemEntry entry = itemBox.GetItem(slotIndex);
        if (entry == null || entry.itemData == null || entry.amount <= 0)
            return;

        int remain = playerInventory.AddItem(entry.itemData, entry.amount);
        int movedAmount = entry.amount - remain;

        if (movedAmount <= 0)
            return;

        itemBox.RemoveAmount(slotIndex, movedAmount);
        RefreshAll();
    }

    /// <summary>
    /// 체스트 슬롯의 아이템을 인벤토리의 특정 칸으로 옮긴다.
    /// 비어 있는 칸이거나 같은 아이템 스택일 때만 이동된다.
    /// </summary>
    public void TryMoveToInventorySlot(int chestSlotIndex, int inventorySlotIndex)
    {
        if (itemBox == null || playerInventory == null)
            return;

        BoxItemEntry entry = itemBox.GetItem(chestSlotIndex);
        if (entry == null || entry.itemData == null || entry.amount <= 0)
            return;

        int remain = playerInventory.TryAddToSlot(inventorySlotIndex, entry.itemData, entry.amount);
        int movedAmount = entry.amount - remain;

        if (movedAmount <= 0)
            return;

        itemBox.RemoveAmount(chestSlotIndex, movedAmount);
        RefreshAll();
    }

    /// <summary>
    /// 인벤토리 슬롯의 아이템을 상자 안으로 옮긴다.
    /// 먼저 같은 아이템 스택을 채우고, 남는 수량은 빈 슬롯에 넣는다.
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
    /// 인벤토리 슬롯의 아이템을 상자의 특정 칸으로 옮긴다.
    /// 대상 칸이 비어 있거나 같은 아이템 스택일 때만 이동된다.
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
    /// 상자 UI를 닫는다.
    /// 실제 닫기 처리는 Presenter 쪽 콜백에 맡긴다.
    /// </summary>
    public void CloseUI()
    {
        onCloseRequested?.Invoke();
    }

    /// <summary>
    /// 슬롯 아이콘을 Addressables로 불러와 갱신한다.
    /// </summary>
    private void LoadSlotIcon(ChestSlotUI slotUI, BoxItemEntry entry)
    {
        if (slotUI == null || entry == null || entry.itemData == null)
            return;

        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(entry.itemData.icon);

        handle.Completed += operation =>
        {
            if (slotUI == null)
                return;

            if (operation.Status == AsyncOperationStatus.Succeeded)
                slotUI.UpdateSlot(entry.amount, operation.Result);
            else
                slotUI.UpdateSlot(entry.amount, null);

            Addressables.Release(operation);
        };
    }
}