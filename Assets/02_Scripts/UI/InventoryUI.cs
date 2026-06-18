/// <summary>
/// 플레이어 UI 실행에 관련된 모든 작업을 수행합니다
/// 정보를 저장하지 않고 UI 출력에 대한 업데이트만 수행합니다
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;

public class InventoryUI : MonoBehaviour
{
    [Header("Inventory UI")]
    [SerializeField] Transform slotContainer;               // 인벤토리 슬롯 목록 그리드
    [SerializeField] List<GameObject> slotsObj;              // 인벤토리 슬롯 오브젝트들
    [SerializeField] GameObject slotPrefab;                 // 슬롯 프리팹

    // 인벤토리 그리드 영역에 _count개의 슬롯을 생성
    public void CreatSlots(int _count)
    {
        if(slotsObj.Count==_count) return;

        // 리스트 내의 모든 슬롯 오브젝트 클리어
        slotsObj.Clear();
        // 지정된 갯수만큼 생성
        for (int i = 0; i < _count; i++)
        {
            // 새로운 슬롯 생성
            GameObject newSlot = Instantiate(slotPrefab, slotContainer.transform);
            newSlot.GetComponent<InventorySlotUI>().Initialize();
            slotsObj.Add(newSlot);
        }
    }

    public void UpdateSlot(int slotNum, InventorySlotData slotData)
    {
        var slotUI = slotsObj[slotNum].GetComponent<InventorySlotUI>();
        slotUI.UpdateSlot(slotData.amount, slotData.icon);
    }
}
