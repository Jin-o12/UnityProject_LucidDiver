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
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        GlobalEventBus.OnSwapInventorySlot -= SwapSlotData;
        GlobalEventBus.OnDropItemQuickSlot -= AddItemToQuickslot;
    }

    public void AddItem(ItemData _itemData, int _count)
    {
        if(_itemData==null) return;

        for (int i = 0; i < slotNum; i++)
        {
            // 해당 인벤토리 칸이 비어있다면
            if(slots[i].TID==0)
            {
                slots[i].TID = _itemData.TID;
                slots[i].amount = _count;
                LoadSprite(_itemData.icon, i);
                return;
            }
        }
    }

    private void LoadSprite(AssetReferenceSprite iconRef, int slotIndex)
    {
        loadHandle = Addressables.LoadAssetAsync<Sprite>(iconRef);

        loadHandle.Completed += (handle) =>
        {
            // 성공적으로 가져왔는지 확인
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // handle.Result에 실제 Sprite 데이터가 들어있음
                //Debug.Log("스프라이트 로드 성공");
                slots[slotIndex].icon = handle.Result;
            }
            else
            {
                //Debug.LogError("스프라이트를 불러오는 데 실패했습니다.");
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

    /* 슬롯 데이터를 교환 */
    public void SwapSlotData(int _index1, int _index2)
    {
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

        // 절대적 갯수를 확인하여 총 갯수를 넘겨줌
        int sumCount = 0;
        for (int i = 0; i < slotNum; i++)
        {
            // 현재 찾는 아이템과 인벤토리에 있는 아이템이 동일한 경우
            if(slots[i].TID==slot.TID)
            {
                sumCount += slots[i].amount;
            }
        }

        // 이미 퀵슬롯에 있다면 기존의 퀵슬롯 내용을 삭제
        for (int i = 0; i < quickSlotNum; i++)
        {
            if(quickSlots[i].TID==slot.TID)
            {
                GlobalEventBus.OnQuickSlotChanged?.Invoke(i, null, 0);
            }
        }

        qSlot.TID = slot.TID;
        qSlot.amount = sumCount;
        qSlot.icon = slot.icon;

        OnSlotChanged?.Invoke(_slotIndex);
        GlobalEventBus.OnQuickSlotChanged?.Invoke(_quickIndex, qSlot.icon, qSlot.amount);
    }
}
