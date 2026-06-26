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

    // 로컬 이벤트
    public event Action<int> OnSlotChanged;             // 특정 칸의 정보 업데이트
    
    // Addressable Assets 불러오기
    private AsyncOperationHandle<Sprite> loadHandle;    // 메모리 관리를 위해 로드 상태를 저장할 핸들

    void Awake()
    {
        slotNum = 20;
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
        InventorySlotData slot = slots[_slotIndex];
        InventorySlotData qSlot = quickSlots[_quickIndex];

        // 소모품이 아닐 경우에는 퀵슬롯에 등록하지 않음
        if(slot.TID<=300 || slot.TID>=400) return;

        // 이미 퀵슬롯에 있다면 기존의 퀵슬롯 내용을 삭제
        for (int i = 0; i < quickSlotNum; i++)
        {
            if(quickSlots[i].TID==slot.TID)
            {
                GlobalEventBus.OnQuickSlotChanged?.Invoke(i, null, 0);
            }
        }

        qSlot.TID = slot.TID;
        qSlot.amount = slot.amount;
        qSlot.icon = slot.icon;

        OnSlotChanged?.Invoke(_slotIndex);
        GlobalEventBus.OnQuickSlotChanged?.Invoke(_quickIndex, qSlot.icon, qSlot.amount);
    }

    /* 퀵슬롯 아이템 사용 */
    public void UseQuickSlotItem(int _index)
    {
        if(quickSlots[_index]==null || _index>quickSlotNum) return;

        InventorySlotData slot = quickSlots[_index];
        // 아이템 사용으로 갯수 감소
        slot.amount--;
        // 사용에 따른 퀵슬롯 변화
        GlobalEventBus.OnQuickSlotChanged?.Invoke(_index, slot.icon, slot.amount);
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
}
