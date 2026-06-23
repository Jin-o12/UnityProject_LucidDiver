/// <summary>
/// 퀵슬롯에 대한 프레젠터입니다
/// 퀵슬롯 UI, 데이터 변환 등에 대한 조율자 역할을 수행합니다
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuickSlotPresenter : MonoBehaviour
{
    [Header("참조 컴포넌트")]
    public PlayerInventory inventory;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();

        if(inventory==null)
        {
            this.enabled = false;
            Debug.LogError("InventoryPresenter: 필요한 컴포넌트가 없습니다.");
            return;
        }
    }

    public void OnEnable()
    {
        GlobalEventBus.OnQuickSlotUseRequested += UseQuickSLotItem;
    }

    public void OnDisable()
    {
        GlobalEventBus.OnQuickSlotUseRequested -= UseQuickSLotItem;
    }

    /* 퀵슬롯에서 아이템을 사용 */
    private void UseQuickSLotItem(int slotIndex)
    {
        // 퀵슬롯 범위 검사 및 데이터 존재 여부 확인
        if(slotIndex<0 || slotIndex>=inventory.quickSlotNum) return;
        // 슬롯 데이터 저장
        InventorySlotData slot = inventory.quickSlots[slotIndex];
 
        // 슬롯이 비어있거나, 아이템을 보유하고 있지 않은 경우 처리하지 않음
        if(slot == null || slot.amount <= 0) return;

        // 아이템 고유 번호로 아이템 데이터를 가져옴
        ItemData itemData = DataManager.Instance.GetItemData(slot.TID);

        // 플리이어 인벤토리에서 아이템 사용 처리
        inventory.UseQuickSlotItem(slotIndex);

        // 만약 아이템이 소비 아이템일 경우
        if(itemData is ConsumeItemData consumeable)
        {
            // 사용 시의 모든 효과를 발동
            for(int i=0; i<consumeable.useEffect.Count; i++)
            {
                ItemEffect currentEffect = consumeable.useEffect[i];
                GameObject targetObj = null;

                // 타겟 지정
                switch(currentEffect.effectTarget)
                {
                    case EffectTarget.self:
                        //Debug.Log($"자신에게 아이템 사용: {itemData.itemName}");
                        targetObj = this.gameObject;
                        break;
                    case EffectTarget.enemy:
                        Debug.Log($"적에게 아이템 사용: {itemData.itemName}");
                        break;
                }

                // 지정된 타겟에게 효과 실행
                if(targetObj != null)
                {
                    currentEffect.Execute(targetObj);
                }
            }
        }

        
    }
}
