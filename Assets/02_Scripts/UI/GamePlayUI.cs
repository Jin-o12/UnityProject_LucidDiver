using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayUI : MonoBehaviour
{
    [Header("하위 UI 판넬 컴포넌트")]
    [SerializeField] PlayerStatusUI statusUI;
    [SerializeField] QuickSlotUI quickSlotUI;

    private void Awake()
    {
        statusUI = GetComponent<PlayerStatusUI>();
        quickSlotUI = GetComponent<QuickSlotUI>();

        if(statusUI==null || quickSlotUI==null)
        {
            this.enabled = false;
            Debug.LogError("InventoryPresenter: 필요한 컴포넌트가 없습니다.");
            return;
        }

        // 퀵슬롯 초기화
        quickSlotUI.initialize();
    }


}
