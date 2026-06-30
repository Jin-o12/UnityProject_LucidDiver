/// <summary>
/// 플레이어의 인벤토리 데이터와 내부의 슬롯, 아이템을 관리하는 클래스
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

    // 어드레시블로 불러온 스프라이트 주소 가져오기
    private AsyncOperationHandle<Sprite> loadHandle;


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

        // 핸들이 유효하다면 (이미 로드된 상태라면)
        if (loadHandle.IsValid())
        {
            // 메모리에서 해당 스프라이트를 안전하게 해제
            Addressables.Release(loadHandle);
        }
    }

    /* 인벤토리 UI 초기화 */
    public void Initialize(int _slotNum, int _quickSlotNum)
    {
        // 모든 슬롯 데이터 초기화
        for (int i = 0; i < _slotNum; i++)
        {
            slots.Add(new InventorySlotData(0, i, 0, null));
        }

        for (int i = 0; i < _quickSlotNum; i++)
        {
            quickSlots.Add(new InventorySlotData(0, i, 0, null));
        }
    }

    /* 인벤토리에 아이템 추가 및 남는 수량 반환 */
    public int AddItem(ItemData _itemData, int _count)
    {
        if (_itemData == null || _count <= 0) return _count;

        int remain = _count;

        // 1. 이미 인벤토리에 해당 아이템이 있는지 먼저 확인
        // (순서 관계 없이 모든 슬롯을 탐색)
        foreach (InventorySlotData slot in slots)
        {
            // 가득 차지 않은 슬롯에 각각 채움
            if (slot.TID == _itemData.TID)
            {
                // 동일 아이템이 있는 슬롯을 찾아 채움
                int beforeAmount = slot.amount;
                remain = TryAddToSlot(slot.order, _itemData, remain);

                // 획득 후 수량을 퀵슬롯에 반영
                if (beforeAmount != slot.amount)
                {
                    QuickSlotRenew(_itemData);
                }
            }
        }

        // 2. 인벤토리에 해당 아이템이 아예 없는 경우 빈 슬롯에 새롭게 추가
        // (모든 슬롯을 가득 채운 후 남은 아이템에 대해서도 동일한 처리를 실행)
        while (remain > 0)
        {
            //빈 슬롯을 찾았는지 확인
            bool foundEmptySlot = false;
            for (int i = 0; i < slotNum; i++)
            {
                // 이미 아이템이 있는 슬롯은 넘김
                if (!IsSlotEmpty(i)) continue;

                // 새 슬롯에 아이템을 추가
                int beforeAmount = slots[i].amount;
                remain = TryAddToSlot(i, _itemData, remain);
                foundEmptySlot = true;

                // 추가 후 총 보유 수량을 퀵슬롯에 반영
                if (beforeAmount != slots[i].amount)
                {
                    QuickSlotRenew(_itemData);
                }
            }

            // 빈 슬롯이 없으면 루프 탈출
            if (!foundEmptySlot) break;
        }

        // 3. 빈 슬롯을 찾을 수 없는 경우 처리 종료
        if (remain > 0)
        {
            Debug.Log("인벤토리가 가득차서 아이템을 주울 수 없습니다.");
        }

        return remain;
    }

    /* 특정 슬롯 하나에 아이템을 넣고 남은 수량 반환 */
    public int TryAddToSlot(int _slotIndex, ItemData _itemData, int _count)
    {
        if (_slotIndex < 0 || _slotIndex >= slotNum) return _count;
        if (_itemData == null || _count <= 0) return _count;

        InventorySlotData slot = slots[_slotIndex];
        int maxStack = Mathf.Max(1, _itemData.itemMultiple);

        // 빈 슬롯이면 새로 배치
        if (IsSlotEmpty(_slotIndex))
        {
            int addAmount = Mathf.Min(_count, maxStack);

            slot.TID = _itemData.TID;
            slot.amount = addAmount;
            slot.icon = null;
            slot.itemData = _itemData;

            // 아이콘 로드
            LoadSprite(_itemData.icon, _slotIndex);

            // 수량부터 먼저 반영
            OnSlotChanged?.Invoke(_slotIndex);

            return _count - addAmount;
        }

        // 다른 아이템이 들어 있으면 추가 불가
        if (slot.TID != _itemData.TID) return _count;

        // 같은 아이템인데 원본 데이터가 비어 있으면 다시 연결
        if (slot.itemData == null)
            slot.itemData = _itemData;

        int canAdd = maxStack - slot.amount;
        if (canAdd <= 0) return _count;

        int realAdd = Mathf.Min(_count, canAdd);
        slot.amount += realAdd;

        OnSlotChanged?.Invoke(_slotIndex);
        return _count - realAdd;
    }

    /* 특정 슬롯 데이터 반환 */
    public InventorySlotData GetSlot(int _slotIndex)
    {
        if (_slotIndex < 0 || _slotIndex >= slotNum) return null;
        return slots[_slotIndex];
    }

    /* 특정 슬롯의 원본 ItemData 반환 */
    public ItemData GetSlotItemData(int _slotIndex)
    {
        if (_slotIndex < 0 || _slotIndex >= slotNum) return null;
        return slots[_slotIndex].itemData;
    }

    /* 특정 슬롯이 비어 있는지 확인 */
    public bool IsSlotEmpty(int _slotIndex)
    {
        if (_slotIndex < 0 || _slotIndex >= slotNum) return true;
        return slots[_slotIndex].TID == 0 || slots[_slotIndex].amount <= 0;
    }

    /* 특정 슬롯 수량 차감 */
    public void RemoveAmount(int _slotIndex, int _count)
    {
        if (_slotIndex < 0 || _slotIndex >= slotNum) return;
        if (IsSlotEmpty(_slotIndex)) return;

        slots[_slotIndex].amount -= _count;

        if (slots[_slotIndex].amount <= 0)
        {
            ClearSlot(_slotIndex);
            return;
        }

        OnSlotChanged?.Invoke(_slotIndex);
    }

    /* 특정 슬롯 완전 초기화 */
    public void ClearSlot(int _slotIndex)
    {
        if (_slotIndex < 0 || _slotIndex >= slotNum) return;

        slots[_slotIndex].TID = 0;
        slots[_slotIndex].amount = 0;
        slots[_slotIndex].icon = null;
        slots[_slotIndex].itemData = null;

        OnSlotChanged?.Invoke(_slotIndex);
    }

    /* 특정 슬롯 아이템을 월드에 드롭하고, 성공 시 후처리 콜백을 실행 */
    public void TryDropSlotToWorld(int _slotIndex, Vector3 dropPosition, Action onDropped = null)
    {
        InventorySlotData slot = GetSlot(_slotIndex);
        if (slot == null || IsSlotEmpty(_slotIndex))
            return;

        ItemData itemData = GetSlotItemData(_slotIndex);
        if (itemData == null)
            return;

        if (itemData.itemPrefabRef == null || !itemData.itemPrefabRef.RuntimeKeyIsValid())
        {
            Debug.LogWarning($"드롭 프리팹 주소가 비어 있습니다. TID: {itemData.TID}");
            return;
        }

        int dropCount = slot.amount;

        itemData.itemPrefabRef.InstantiateAsync(dropPosition, Quaternion.identity).Completed += handle =>
        {
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError($"드롭 아이템 생성 실패: {itemData.itemName}");
                return;
            }

            GameObject dropObject = handle.Result;
            DropItem dropItem = dropObject.GetComponent<DropItem>();

            if (dropItem == null)
            {
                Debug.LogError($"생성된 프리팹에 DropItem 컴포넌트가 없습니다: {dropObject.name}");
                Addressables.ReleaseInstance(dropObject);
                return;
            }

            dropItem.itemData = itemData;
            dropItem.stackCount = dropCount;

            RemoveAmount(_slotIndex, dropCount);
            onDropped?.Invoke();
        };
    }

    /* 획득한 아이템을 퀵슬롯에서 찾아 개수 변동 반영 */
    private void QuickSlotRenew(ItemData _itemData)
    {
        foreach (InventorySlotData qslot in quickSlots)
        {
            if (qslot.TID == _itemData.TID)
            {
                GlobalEventBus.OnQuickSlotChanged?.Invoke(qslot.order, qslot.icon, GetInventoryItemCount(qslot.TID));
            }
        }
    }

    /* 아이템의 아이콘 Addressable 주소 해석 및 스프라이트 이미지 가져오기 */
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
            // 성공적으로 가져왔는지 확인
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // 로드된 아이콘을 슬롯 데이터에 반영
                slots[slotIndex].icon = handle.Result;
            }
            else
            {
                slots[slotIndex].icon = null;
            }

            // 아이콘이 준비된 시점에 UI를 한 번 더 갱신
            OnSlotChanged?.Invoke(slotIndex);
        };
    }

    public void UseItem(int slotIndex)
    {
        // 유효성 검사 (빈 슬롯이 아닌지)
        if (IsSlotEmpty(slotIndex)) return;

        // 갯수 차감
        RemoveAmount(slotIndex, 1);
    }

    /* 인벤토리 슬롯 데이터를 교환 */
    public void SwapSlotData(int _index1, int _index2)
    {
        /// ※추가: 해당 아이템이 동일한 아이템이라면 존재한다면 합산 가능한지 판정 후 합산 ///

        InventorySlotData slot1 = slots[_index1];
        InventorySlotData slot2 = slots[_index2];

        // 두 데이터 교환
        (slot1.TID, slot2.TID) = (slot2.TID, slot1.TID);
        (slot1.amount, slot2.amount) = (slot2.amount, slot1.amount);
        (slot1.icon, slot2.icon) = (slot2.icon, slot1.icon);
        (slot1.itemData, slot2.itemData) = (slot2.itemData, slot1.itemData);

        // 변동사항 알림
        OnSlotChanged?.Invoke(_index1);
        OnSlotChanged?.Invoke(_index2);
    }

    /* 퀵슬롯에 아이템 추가 */
    public void AddItemToQuickslot(int _quickIndex, int _slotIndex)
    {
        // 퀵슬롯/인벤토리 인덱스 유효성 검사
        if (_quickIndex < 0 || _quickIndex >= quickSlotNum) return;
        if (_slotIndex < 0 || _slotIndex >= slotNum) return;

        InventorySlotData slot = slots[_slotIndex];

        // 소모품이 아닐 경우에는 퀵슬롯에 등록하지 않음
        if (slot.TID <= 300 || slot.TID >= 400) return;

        // 이미 다른 퀵슬롯에 있다면 기존 슬롯을 비움
        for (int i = 0; i < quickSlotNum; i++)
        {
            if (i == _quickIndex) continue;
            if (quickSlots[i].TID != slot.TID) continue;

            quickSlots[i].TID = 0;
            quickSlots[i].amount = 0;
            quickSlots[i].icon = null;
            quickSlots[i].itemData = null;
            GlobalEventBus.OnQuickSlotChanged?.Invoke(i, null, 0);
        }

        InventorySlotData qSlot = quickSlots[_quickIndex];
        qSlot.TID = slot.TID;
        qSlot.amount = GetInventoryItemCount(slot.TID);
        qSlot.icon = slot.icon;
        qSlot.itemData = slot.itemData;

        // 변동사항 알림
        OnSlotChanged?.Invoke(_slotIndex);
        GlobalEventBus.OnQuickSlotChanged?.Invoke(_quickIndex, qSlot.icon, qSlot.amount);
    }

    /* 퀵슬롯 아이템 사용 */
    public void UseQuickSlotItem(int _index)
    {
        // 퀵슬롯 범위 및 데이터 유효성 검사
        if (_index < 0 || _index >= quickSlotNum) return;

        InventorySlotData slot = quickSlots[_index];
        if (slot == null || slot.TID == 0 || slot.amount <= 0) return;

        int tid = slot.TID;

        // 실제 인벤토리 수량을 먼저 차감
        ConsumeInventoryItemByTID(tid);

        // 차감 후 남은 수량을 퀵슬롯에 다시 반영
        int remain = GetInventoryItemCount(tid);
        slot.amount = remain;

        if (remain <= 0)
        {
            slot.TID = 0;
            slot.amount = 0;
            slot.icon = null;
            slot.itemData = null;
            GlobalEventBus.OnQuickSlotChanged?.Invoke(_index, null, 0);
            return;
        }

        GlobalEventBus.OnQuickSlotChanged?.Invoke(_index, slot.icon, slot.amount);
    }

    /* 인벤토리에서 특정 TID 아이템의 총 수량 계산 */
    private int GetInventoryItemCount(int tid)
    {
        int count = 0;

        for (int i = 0; i < slotNum; i++)
        {
            if (slots[i].TID == tid)
            {
                count += slots[i].amount;
            }
        }

        return count;
    }

    /* 인벤토리에서 특정 TID 아이템 1개를 차감 */
    private void ConsumeInventoryItemByTID(int tid)
    {
        for (int i = 0; i < slotNum; i++)
        {
            if (slots[i].TID != tid || slots[i].amount <= 0)
            {
                continue;
            }

            slots[i].amount--;

            if (slots[i].amount <= 0)
            {
                ClearSlot(i);
                return;
            }

            // 수량 변화가 생긴 인벤토리 슬롯 UI를 갱신
            OnSlotChanged?.Invoke(i);
            return;
        }
    }

    /* 퀵슬롯간에 아이템 교환 */
    private void SwapItemQuickSlot(int _index1, int _index2)
    {
        InventorySlotData slot1 = quickSlots[_index1];
        InventorySlotData slot2 = quickSlots[_index2];

        // 두 데이터 교환
        (slot1.TID, slot2.TID) = (slot2.TID, slot1.TID);
        (slot1.amount, slot2.amount) = (slot2.amount, slot1.amount);
        (slot1.icon, slot2.icon) = (slot2.icon, slot1.icon);
        (slot1.itemData, slot2.itemData) = (slot2.itemData, slot1.itemData);

        // 변동사항 알림
        GlobalEventBus.OnQuickSlotChanged?.Invoke(_index1, slot1.icon, slot1.amount);
        GlobalEventBus.OnQuickSlotChanged?.Invoke(_index2, slot2.icon, slot2.amount);
    }

    /* 저장된 인벤토리/퀵슬롯 데이터를 런타임 슬롯에 복원 */
    public void RestoreFromSave(PlayerSaveData saveData)
    {
        if (saveData == null) return;

        // 현재 런타임 슬롯 데이터를 모두 초기화
        ClearRuntimeSlots();

        // 저장된 인벤토리 슬롯 데이터를 순서대로 복원
        foreach (var savedSlot in saveData.inventorySlots)
        {
            if (savedSlot.index < 0 || savedSlot.index >= slotNum) continue;

            slots[savedSlot.index].TID = savedSlot.TID;
            slots[savedSlot.index].amount = savedSlot.amount;

            ItemData itemData = GetItemDataByTID(savedSlot.TID);
            slots[savedSlot.index].itemData = itemData;

            if (itemData != null) LoadSprite(itemData.icon, savedSlot.index);

            OnSlotChanged?.Invoke(savedSlot.index);
        }

        // 저장된 퀵슬롯 TID를 기준으로 퀵슬롯 데이터를 복원
        for (int i = 0; i < quickSlotNum; i++)
        {
            int tid = i < saveData.quickSlots.Count ? saveData.quickSlots[i] : 0;

            if (tid == 0)
            {
                quickSlots[i].TID = 0;
                quickSlots[i].amount = 0;
                quickSlots[i].icon = null;
                quickSlots[i].itemData = null;
                GlobalEventBus.OnQuickSlotChanged?.Invoke(i, null, 0);
                continue;
            }

            quickSlots[i].TID = tid;
            quickSlots[i].amount = GetSavedItemCount(saveData, tid);

            ItemData itemData = GetItemDataByTID(tid);
            quickSlots[i].itemData = itemData;

            if (itemData != null)
            {
                LoadQuickSlotSprite(itemData.icon, i);
            }
        }
    }

    /* 아이템 TID로 ScriptableObject 원본 데이터 찾기 */
    private ItemData GetItemDataByTID(int tid)
    {
        if (tid == 0) return null;

        ItemData[] itemDatas = Resources.LoadAll<ItemData>("ScriptableObjects/Item");
        foreach (ItemData itemData in itemDatas)
        {
            if (itemData != null && itemData.TID == tid)
            {
                return itemData;
            }
        }

        return null;
    }

    /* 퀵슬롯 아이콘 Addressable 주소 해석 및 스프라이트 이미지 가져오기 */
    private void LoadQuickSlotSprite(AssetReferenceSprite iconRef, int quickIndex)
    {
        if (iconRef == null || !iconRef.RuntimeKeyIsValid())
        {
            quickSlots[quickIndex].icon = null;
            GlobalEventBus.OnQuickSlotChanged?.Invoke(quickIndex, null, quickSlots[quickIndex].amount);
            return;
        }

        loadHandle = Addressables.LoadAssetAsync<Sprite>(iconRef);

        loadHandle.Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                quickSlots[quickIndex].icon = handle.Result;
                GlobalEventBus.OnQuickSlotChanged?.Invoke(quickIndex, handle.Result, quickSlots[quickIndex].amount);
            }
            else
            {
                quickSlots[quickIndex].icon = null;
                GlobalEventBus.OnQuickSlotChanged?.Invoke(quickIndex, null, quickSlots[quickIndex].amount);
            }
        };
    }

    /* 런타임 인벤토리/퀵슬롯 데이터를 모두 초기화 */
    private void ClearRuntimeSlots()
    {
        for (int i = 0; i < slotNum; i++)
        {
            slots[i].TID = 0;
            slots[i].amount = 0;
            slots[i].icon = null;
            slots[i].itemData = null;
            OnSlotChanged?.Invoke(i);
        }

        for (int i = 0; i < quickSlotNum; i++)
        {
            quickSlots[i].TID = 0;
            quickSlots[i].amount = 0;
            quickSlots[i].icon = null;
            quickSlots[i].itemData = null;
            GlobalEventBus.OnQuickSlotChanged?.Invoke(i, null, 0);
        }
    }

    /* 저장 데이터 기준 특정 TID 아이템 총 수량 계산 */
    private int GetSavedItemCount(PlayerSaveData saveData, int tid)
    {
        int count = 0;
        foreach (var slot in saveData.inventorySlots)
        {
            if (slot.TID == tid) count += slot.amount;
        }
        return count;
    }
}
