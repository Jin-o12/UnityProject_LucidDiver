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

    /* 인벤토리에 아이템 추가 또는 더해짐 */
    public int AddItem(ItemData _itemData, int _count)
    {
        if(_itemData==null) return 0;

        // 동일 아이템이 이미 인벤토리에 존재한다면 합산 (합산 가능한 스텍일 시)
        for (int i = 0; i < slotNum; i++)
        {
            // 해당 아이템이 넣으려는 아이템과 동일하고 최대 스택 미만이라면 합산하여 저장
            if(slots[i].TID == _itemData.TID && slots[i].amount<_itemData.itemMultiple)
            {
                int throwItem = 0;
                // 아이템을 합산 했을 때, 스텍을 초과하면 나머지는 먹지 않고, 그렇지 않다면 모두 합산
                slots[i].amount = slots[i].amount+_count>=_itemData.itemMultiple ? _itemData.itemMultiple : _count;
                throwItem = (slots[i].amount+_count)-_itemData.itemMultiple;
                return throwItem;
            }
        }

        for (int i = 0; i < slotNum; i++)
        {
            // 해당 인벤토리 칸이 비어있다면
            if(slots[i].TID==0)
            {
                slots[i].TID = _itemData.TID;
                slots[i].amount = _count;
                LoadSprite(_itemData.icon, i);
                return 0;
            }
        }

        return 0;
    }

    /* 아이템 아이콘 Addressable 주소 해석 및 스프라이트 이미지 가져오기 */
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
}
