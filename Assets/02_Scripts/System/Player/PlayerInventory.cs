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

    void Awake()
    {
        slotNum = 10;
        quickSlotNum = 3;

        // 모든 슬롯 데이터 초기화
        for (int i = 0; i < slotNum; i++)
        {
            slots.Add(new InventorySlotData(0, i, 0, null));
        }
        for (int i = 0; i < quickSlotNum; i++)
        {
            quickSlots.Add(new InventorySlotData(0, i, 0, null));
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

        // 핸들이 유효하다면 (이미 로드된 상태라면)
        if (loadHandle.IsValid())
        {
            // 메모리에서 해당 스프라이트를 안전하게 해제
            Addressables.Release(loadHandle);
        }
    }

    /* 인벤토리에 아이템 추가 또는 더해짐 */
    /* 인벤토리에 아이템 추가 및 남는 수량 반환 */
    public int AddItem(ItemData _itemData, int _count)
    {
        if (_itemData == null) return 0;

        // 동일 아이템이 이미 있으면 먼저 해당 슬롯에 누적 시도
        for (int i = 0; i < slotNum; i++)
        {
            if (slots[i].TID == _itemData.TID && slots[i].amount < _itemData.itemMultiple)
            {
                // 현재 수량 + 추가 수량을 합친 뒤 최대 스택 수를 넘는지 계산
                int totalAmount = slots[i].amount + _count;

                // 슬롯에는 최대 스택 수까지만 저장
                slots[i].amount = totalAmount >= _itemData.itemMultiple ? _itemData.itemMultiple : totalAmount;

                // UI 갱신 이벤트 호출
                OnSlotChanged?.Invoke(i);

                // 초과 수량 반환
                int remain = totalAmount - _itemData.itemMultiple;
                return remain > 0 ? remain : 0;
            }
        }

        // 빈 슬롯이 있으면 새 슬롯에 아이템 추가
        for (int i = 0; i < slotNum; i++)
        {
            if (slots[i].TID == 0)
            {
                slots[i].TID = _itemData.TID;

                // 아이콘 로드
                LoadSprite(_itemData.icon, i);

                // 새 슬롯에도 최대 스택 수까지만 저장
                slots[i].amount = _count >= _itemData.itemMultiple ? _itemData.itemMultiple : _count;

                // UI 갱신 이벤트 호출
                OnSlotChanged?.Invoke(i);

                // 초과 수량 반환
                int remain = _count - _itemData.itemMultiple;
                return remain > 0 ? remain : 0;
            }
        }

        // 인벤토리가 가득 차서 넣지 못한 경우 남은 수량 그대로 반환
        Debug.Log("인벤토리가 가득차서 아이템을 주울 수 없습니다.");
        return _count;
    }

    /* 아이템의 아이콘 Addressable 주소 해석 및 스프라이트 이미지 가져오기 */
    private void LoadSprite(AssetReferenceSprite iconRef, int slotIndex)
    {
        loadHandle = Addressables.LoadAssetAsync<Sprite>(iconRef);

        loadHandle.Completed += (handle) =>
        {
            // 성공적으로 가져왔는지 확인
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // 로드된 아이콘을 슬롯 데이터에 반영
                slots[slotIndex].icon = handle.Result;

                // 아이콘이 준비된 시점에 UI를 한 번 더 갱신
                OnSlotChanged?.Invoke(slotIndex);
            }
            else
            {
                // 필요 시 에러 로그 추가 가능
                // Debug.LogError("스프라이트를 불러오는데 실패했습니다.");
            }
        };
    }

    public void UseItem(int slotIndex)
    {
        // 유효성 검사 (빈 슬롯이 아닌지)
        if (slots[slotIndex].TID == 0) return;

        // 갯수 차감
        slots[slotIndex].amount--;

        // 갯수가 0 이하라면 슬롯 초기화 (빈 슬롯으로 만들기)
        if (slots[slotIndex].amount <= 0)
        {
            slots[slotIndex].TID = 0;
            slots[slotIndex].amount = 0;
            slots[slotIndex].icon = null; // 필요시 명시적 null 처리
        }
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
            GlobalEventBus.OnQuickSlotChanged?.Invoke(i, null, 0);
        }

        InventorySlotData qSlot = quickSlots[_quickIndex];
        qSlot.TID = slot.TID;
        qSlot.amount = GetInventoryItemCount(slot.TID);
        qSlot.icon = slot.icon;

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
                slots[i].TID = 0;
                slots[i].amount = 0;
                slots[i].icon = null;
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
                GlobalEventBus.OnQuickSlotChanged?.Invoke(i, null, 0);
                continue;
            }

            quickSlots[i].TID = tid;
            quickSlots[i].amount = GetSavedItemCount(saveData, tid);

            ItemData itemData = GetItemDataByTID(tid);
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
        loadHandle = Addressables.LoadAssetAsync<Sprite>(iconRef);

        loadHandle.Completed += handle =>
        {
            if (handle.Status != AsyncOperationStatus.Succeeded) return;

            quickSlots[quickIndex].icon = handle.Result;
            GlobalEventBus.OnQuickSlotChanged?.Invoke(quickIndex, handle.Result, quickSlots[quickIndex].amount);
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
            OnSlotChanged?.Invoke(i);
        }

        for (int i = 0; i < quickSlotNum; i++)
        {
            quickSlots[i].TID = 0;
            quickSlots[i].amount = 0;
            quickSlots[i].icon = null;
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
