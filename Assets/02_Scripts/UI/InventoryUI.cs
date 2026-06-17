/// <summary>
/// 플레이어 UI 실행에 관련된 모든 작업을 수행합니다
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Inventory UI")]
    [SerializeField] Transform slotContainer;              // 인벤토리 슬롯 목록 그리드
    [SerializeField] GameObject slotPrefab;                     // 슬롯 프리팹

    // 인벤토리 그리드 영역에 _count개의 슬롯을 생성
    public void CreatSlots(int _count, System.Action<int, GameObject> onSlotCreated)
    {
        // 기존 슬롯 초기화
        foreach (Transform child in slotContainer) Destroy(child.gameObject);

        // 지정된 갯수만큼 생성
        for (int i = 0; i < _count; i++)
        {
            // 새로운 슬롯 생성
            GameObject newSlot = Instantiate(slotPrefab, slotContainer);
            // 슬롯 번호와 함께 이벤트로 전달
            onSlotCreated?.Invoke(i, newSlot);
        }
    }

}
