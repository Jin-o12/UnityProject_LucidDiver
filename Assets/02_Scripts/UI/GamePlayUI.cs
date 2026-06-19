using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayUI : MonoBehaviour
{
    [Header("하위 UI 판넬 컴포넌트")]
    [SerializeField] PlayerStatusUI statusUI;
    [SerializeField] QuickSlotGroupUI quickSlotGroupUI;

    private void Awake()
    {
        statusUI = GetComponent<PlayerStatusUI>();
        quickSlotGroupUI = GetComponent<QuickSlotGroupUI>();

        if(statusUI==null || quickSlotGroupUI==null)
        {
            this.enabled = false;
            Debug.LogError("GamePlayUI: 필요한 컴포넌트가 없습니다.");
            return;
        }

        // 퀵슬롯 초기화
        quickSlotGroupUI.InitializeSlots();
    }


}
