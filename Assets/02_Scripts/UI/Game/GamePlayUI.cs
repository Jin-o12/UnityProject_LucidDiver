using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayUI : MonoBehaviour
{
    [Header("하위 UI 판넬 컴포넌트")]
    [SerializeField] PlayerStatusUI statusUI;
    [SerializeField] QuickSlotGroupUI quickSlotGroupUI;
    [SerializeField] private CanvasGroup hudCanvasGroup;

    private const float DefaultHUDAlpha = 1.0f;
    [SerializeField] SkillPlayUI skillUI;

    private void Awake()
    {
        statusUI = GetComponent<PlayerStatusUI>();
        quickSlotGroupUI = GetComponent<QuickSlotGroupUI>();
        hudCanvasGroup = GetComponent<CanvasGroup>();

        // 튜토리얼 대화/안내 UI가 열릴 때 인게임 HUD를 자연스럽게 뒤로 물리기 위한 알파 조절용입니다.
        if (hudCanvasGroup == null)
            hudCanvasGroup = gameObject.AddComponent<CanvasGroup>();
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

    private void OnEnable()
    {
        GlobalEventBus.OnGameplayHUDAlphaRequested += SetHUDAlpha;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnGameplayHUDAlphaRequested -= SetHUDAlpha;
        SetHUDAlpha(DefaultHUDAlpha);
    }

    private void Start()
    {
        SyncQuickSlotsFromPlayerInventory();
    }

    private void SetHUDAlpha(float alpha)
    {
        if (hudCanvasGroup == null)
            return;

        hudCanvasGroup.alpha = Mathf.Clamp01(alpha);
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
