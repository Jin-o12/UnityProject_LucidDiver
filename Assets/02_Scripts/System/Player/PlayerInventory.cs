/// <summary>
/// 플레이어의 인벤토리 데이터를 보관하고 아이템 수납 / 사용 / 교환을 관리한다.
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PlayerInventory : MonoBehaviour
{
    public List<InventorySlotData> slots = new();
    public List<InventorySlotData> quickSlots = new();
    public int slotNum { get; private set; }
    public int quickSlotNum { get; private set; }

    // 특정 슬롯 UI를 갱신할 때 사용하는 이벤트
    public event Action<int> OnSlotChanged;

    // Addressables 스프라이트 로드 핸들
    private AsyncOperationHandle<Sprite> loadHandle;

    private void Awake()
    {
        slotNum = 10;
        quickSlotNum = 3;

        // 인벤토리 슬롯 초기화
        for (int i = 0; i < slotNum; i++)
        {
            slots.Add(new InventorySlotData(0, i, 0, null, null));
        }

        // 퀵슬롯 초기화
        for (int i = 0; i < quickSlotNum; i++)
        {
            quickSlots.Add(new InventorySlotData(0, i, 0, null, null));
        }
    }

    private void OnEnable()
    {
        /// 이벤트 구독 ///
        GlobalEventBus.OnSwapInventorySlot += SwapSlotData;
        GlobalEventBus.OnDropItemQuickSlot += AddItemToQuickslot;
        GlobalEventBus.OnSwapItemQuickSlot += SwapItemQuickSlot;
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        GlobalEventBus.OnSwapInventorySlot -= SwapSlotData;
        GlobalEventBus.OnDropItemQuickSlot -= AddItemToQuickslot;
        GlobalEventBus.OnSwapItemQuickSlot -= SwapItemQuickSlot;
    }

    /// <summary>
    /// 인벤토리에 아이템을 넣고, 다 못 넣은 남은 수량을 반환한다.
    /// 1. 먼저 같은 아이템 스택을 채운다.
    /// 2. 남은 수량은 빈 슬롯에 넣는다.
    /// 3. 끝까지 못 넣은 수량만 반환한다.
    /// </summary>
    public int AddItem(ItemData _itemData, int _count)
    {
        if (_itemData == null || _count <= 0)
            return _count;

        int remain = _count;

        // 1. 먼저 같은 아이템이 있는 기존 스택을 최대치까지 채운다.
        for (int i = 0; i < slotNum; i++)
        {
            if (slots[i].TID != _itemData.TID)
                continue;

            if (slots[i].amount >= _itemData.itemMultiple)
                continue;

            remain = TryAddToSlot(i, _itemData, remain);

            if (remain <= 0)
                return 0;
        }

        // 2. 남은 수량을 빈 슬롯들에 순서대로 배치한다.
        for (int i = 0; i < slotNum; i++)
        {
            if (!IsSlotEmpty(i))
                continue;

            remain = TryAddToSlot(i, _itemData, remain);

            if (remain <= 0)
                return 0;
        }

        // 3. 끝까지 못 넣은 수량 반환
        if (remain > 0)
            Debug.Log("인벤토리가 가득차 일부 아이템을 넣지 못했습니다.");

        return remain;
    }

    /// <summary>
    /// 특정 슬롯 하나에 아이템을 넣고, 남은 수량을 반환한다.
    /// 같은 아이템 스택이거나 빈 슬롯일 때만 넣을 수 있다.
    /// </summary>
    public int TryAddToSlot(int _slotIndex, ItemData _itemData, int _count)
    {
        if (_slotIndex < 0 || _slotIndex >= slotNum)
            return _count;

        if (_itemData == null || _count <= 0)
            return _count;

        InventorySlotData slot = slots[_slotIndex];
        int maxStack = Mathf.Max(1, _itemData.itemMultiple);

        // 빈 슬롯이면 새로 아이템 배치
        if (IsSlotEmpty(_slotIndex))
        {
            int addAmount = Mathf.Min(_count, maxStack);

            slot.TID = _itemData.TID;
            slot.amount = addAmount;
            slot.icon = null;
            slot.itemData = _itemData;

            // 아이콘 로드
            LoadSprite(_itemData.icon, _slotIndex);

            // 텍스트 수량 반영을 위해 즉시 UI 갱신
            OnSlotChanged?.Invoke(_slotIndex);

            return _count - addAmount;
        }

        // 다른 아이템이 들어 있으면 넣을 수 없음
        if (slot.TID != _itemData.TID)
            return _count;

        // 같은 아이템 스택인데 원본 데이터가 비어 있으면 다시 연결해 둔다.
        if (slot.itemData == null)
            slot.itemData = _itemData;

        // 같은 아이템이면 남은 스택 공간만큼 추가
        int canAdd = maxStack - slot.amount;

        if (canAdd <= 0)
            return _count;

        int realAdd = Mathf.Min(_count, canAdd);
        slot.amount += realAdd;

        // 같은 아이템 스택 증가 반영
        OnSlotChanged?.Invoke(_slotIndex);

        return _count - realAdd;
    }

    /// <summary>
    /// 특정 슬롯 데이터를 반환한다.
    /// </summary>
    public InventorySlotData GetSlot(int _slotIndex)
    {
        if (_slotIndex < 0 || _slotIndex >= slotNum)
            return null;

        return slots[_slotIndex];
    }

    /// <summary>
    /// 특정 슬롯이 들고 있는 실제 ItemData를 반환한다.
    /// 상자와 인벤토리 사이 이동 처리에서 사용한다.
    /// </summary>
    public ItemData GetSlotItemData(int _slotIndex)
    {
        if (_slotIndex < 0 || _slotIndex >= slotNum)
            return null;

        return slots[_slotIndex].itemData;
    }

    /// <summary>
    /// 특정 슬롯이 비어 있는지 확인한다.
    /// </summary>
    public bool IsSlotEmpty(int _slotIndex)
    {
        if (_slotIndex < 0 || _slotIndex >= slotNum)
            return true;

        return slots[_slotIndex].TID == 0 || slots[_slotIndex].amount <= 0;
    }

    /// <summary>
    /// 특정 슬롯의 수량을 감소시킨다.
    /// 수량이 0 이하가 되면 슬롯을 비운다.
    /// </summary>
    public void RemoveAmount(int _slotIndex, int _count)
    {
        if (_slotIndex < 0 || _slotIndex >= slotNum)
            return;

        if (IsSlotEmpty(_slotIndex))
            return;

        slots[_slotIndex].amount -= _count;

        if (slots[_slotIndex].amount <= 0)
        {
            ClearSlot(_slotIndex);
            return;
        }

        OnSlotChanged?.Invoke(_slotIndex);
    }

    /// <summary>
    /// 특정 슬롯을 완전히 비운다.
    /// </summary>
    public void ClearSlot(int _slotIndex)
    {
        if (_slotIndex < 0 || _slotIndex >= slotNum)
            return;

        slots[_slotIndex].TID = 0;
        slots[_slotIndex].amount = 0;
        slots[_slotIndex].icon = null;
        slots[_slotIndex].itemData = null;

        OnSlotChanged?.Invoke(_slotIndex);
    }

    /// <summary>
    /// 아이템 아이콘 Addressable 주소를 해석해서 슬롯에 반영한다.
    /// </summary>
    private void LoadSprite(AssetReferenceSprite iconRef, int slotIndex)
    {
        if (iconRef == null || !iconRef.RuntimeKeyIsValid())
        {
            slots[slotIndex].icon = null;
            OnSlotChanged?.Invoke(slotIndex);
            return;
        }

        loadHandle = Addressables.LoadAssetAsync<Sprite>(iconRef);

        loadHandle.Completed += (handle) =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                slots[slotIndex].icon = handle.Result;
                OnSlotChanged?.Invoke(slotIndex);
            }
            else
            {
                slots[slotIndex].icon = null;
                OnSlotChanged?.Invoke(slotIndex);
            }
        };
    }

    /// <summary>
    /// 슬롯의 아이템을 1개 사용한다.
    /// </summary>
    public void UseItem(int slotIndex)
    {
        if (IsSlotEmpty(slotIndex))
            return;

        RemoveAmount(slotIndex, 1);
    }

    /// <summary>
    /// 인벤토리 슬롯끼리 데이터를 교환한다.
    /// </summary>
    public void SwapSlotData(int _index1, int _index2)
    {
        InventorySlotData slot1 = slots[_index1];
        InventorySlotData slot2 = slots[_index2];

        (slot1.TID, slot2.TID) = (slot2.TID, slot1.TID);
        (slot1.amount, slot2.amount) = (slot2.amount, slot1.amount);
        (slot1.icon, slot2.icon) = (slot2.icon, slot1.icon);
        (slot1.itemData, slot2.itemData) = (slot2.itemData, slot1.itemData);

        OnSlotChanged?.Invoke(_index1);
        OnSlotChanged?.Invoke(_index2);
    }

    /// <summary>
    /// 인벤토리 아이템을 퀵슬롯에 등록한다.
    /// </summary>
    public void AddItemToQuickslot(int _quickIndex, int _slotIndex)
    {
        InventorySlotData slot = slots[_slotIndex];
        InventorySlotData qSlot = quickSlots[_quickIndex];

        // 소모품만 퀵슬롯 등록 가능
        if (slot.TID <= 300 || slot.TID >= 400)
            return;

        // 이미 등록된 동일 아이템이 있으면 기존 퀵슬롯 비우기
        for (int i = 0; i < quickSlotNum; i++)
        {
            if (quickSlots[i].TID == slot.TID)
            {
                GlobalEventBus.OnQuickSlotChanged?.Invoke(i, null, 0);
            }
        }

        qSlot.TID = slot.TID;
        qSlot.amount = slot.amount;
        qSlot.icon = slot.icon;
        qSlot.itemData = slot.itemData;

        OnSlotChanged?.Invoke(_slotIndex);
        GlobalEventBus.OnQuickSlotChanged?.Invoke(_quickIndex, qSlot.icon, qSlot.amount);
    }

    /// <summary>
    /// 퀵슬롯 아이템을 1개 사용한다.
    /// </summary>
    public void UseQuickSlotItem(int _index)
    {
        if (_index < 0 || _index >= quickSlotNum)
            return;

        if (quickSlots[_index] == null)
            return;

        InventorySlotData slot = quickSlots[_index];
        slot.amount--;

        GlobalEventBus.OnQuickSlotChanged?.Invoke(_index, slot.icon, slot.amount);
    }

    /// <summary>
    /// 퀵슬롯끼리 데이터를 교환한다.
    /// </summary>
    private void SwapItemQuickSlot(int _index1, int _index2)
    {
        InventorySlotData slot1 = quickSlots[_index1];
        InventorySlotData slot2 = quickSlots[_index2];

        (slot1.TID, slot2.TID) = (slot2.TID, slot1.TID);
        (slot1.amount, slot2.amount) = (slot2.amount, slot1.amount);
        (slot1.icon, slot2.icon) = (slot2.icon, slot1.icon);
        (slot1.itemData, slot2.itemData) = (slot2.itemData, slot1.itemData);

        GlobalEventBus.OnQuickSlotChanged?.Invoke(_index1, slot1.icon, slot1.amount);
        GlobalEventBus.OnQuickSlotChanged?.Invoke(_index2, slot2.icon, slot2.amount);
    }
}
