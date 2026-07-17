using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayUI : MonoBehaviour
{
    [Header("하위 UI 판넬 컴포넌트")]
    [SerializeField] PlayerStatusUI statusUI;
    [SerializeField] QuickSlotGroupUI quickSlotGroupUI;
    [SerializeField] SkillPlayUI skillUI;

    private void Awake()
    {
        statusUI = GetComponent<PlayerStatusUI>();
        quickSlotGroupUI = GetComponent<QuickSlotGroupUI>();
        skillUI = GetComponent<SkillPlayUI>();

        if(statusUI==null || quickSlotGroupUI==null || skillUI == null)
        {
            this.enabled = false;
            Debug.LogError("GamePlayUI: 필요한 컴포넌트가 없습니다.");
            return;
        }

        // 퀵슬롯 초기화
        quickSlotGroupUI.InitializeSlots();
    }

    private void Start()
    {
        SyncQuickSlotsFromPlayerInventory();
    }

    private void SyncQuickSlotsFromPlayerInventory()
    {
        if (quickSlotGroupUI == null)
        {
            return;
        }

        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory == null)
        {
            Debug.LogWarning("GamePlayUI: 플레이어 인벤토리를 찾지 못해 퀵슬롯 초기 동기화를 건너뜁니다.", this);
            return;
        }

        // PlayerInventory.RestoreFromSave가 GamePlayUI 생성보다 먼저 실행되면
        // 퀵슬롯 갱신 이벤트를 놓칠 수 있으므로 UI 생성 후 현재 상태를 한 번 직접 반영한다.
        quickSlotGroupUI.SyncFromInventory(playerInventory);
    }
}
