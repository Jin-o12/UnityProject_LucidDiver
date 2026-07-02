/// <summary>
/// 로컬 입력을 읽어서 글로벌 이벤트 버스에 전달하는 역할을 하는 클래스
/// 'Player input'에 등록되는 모든 메서드들이 정리되어 있습니다
/// </summary>
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class LocalInputReader : MonoBehaviour
{
    // 참조 컴포넌트
    PlayerInput playerInput;
    public event Action OnInventoryOpenRequested;
    public event Action OnInventoryCloseRequested;
    private bool isInventoryOpen;
    public bool isSprint;

    private void Awake()
    {
        playerInput = GetComponentInParent<PlayerInput>();

        // 필수 컴포넌트가 존재하지 않을 시 스크립트 비활성화
        if(playerInput==null)
        {
            this.enabled = false;
            Debug.LogError("LocalInputReader: 필요한 컴포넌트가 없습니다.");
            return;
        }
    }

    // 플레이어의 입력을 받아 글로벌 이벤트 변수에 전달하는 함수들

    /* 플레이어 이동 입력 처리 */
    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();
        
        GlobalEventBus.OnPlayerMove?.Invoke(moveInput);
    }

    /* 플레이어 공격 입력 처리 */
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (isInventoryOpen)
            return;

        if (context.performed)
        {
            GlobalEventBus.OnAttackInput?.Invoke();
        }
    }

    /* 플레이어 스킬 입력 처리 */
    public void OnActiveSkill(InputAction.CallbackContext context)
    {
        //인벤토리 메뉴 오픈 중에는 스킬이 발동하지 않도록 잠금
        if (isInventoryOpen)
            return;

        if (context.performed)
        {
            GlobalEventBus.OnMainActiveSkillRequested?.Invoke();
        }
    }

    /* 플레이어 상호작용 입력 처리 */
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            GlobalEventBus.OnInteractionInput?.Invoke();
        }
    }

    public void OnAimPosition(InputAction.CallbackContext context)
    {
        // 마우스의 2D 픽셀 좌표를 읽어서 이벤트 버스로 전송
        Vector2 mousePos = context.ReadValue<Vector2>();
        GlobalEventBus.OnMousePositionInput?.Invoke(mousePos);
    }

    public void OpenInventoryUI(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (isInventoryOpen)
                OnInventoryCloseRequested?.Invoke();
            else
                OnInventoryOpenRequested?.Invoke();
        }
    }

    public void CloseInventoryUI(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnInventoryCloseRequested?.Invoke();
        }
    }

    public void OnUseQuickSlot(InputAction.CallbackContext context)
    {
        if (isInventoryOpen)
            return;

        if(context.performed)
        {
            // Scale값을 float로 읽어옴
            float rawValue = context.ReadValue<float>();
            // int로 형변환 하여 인덱스로 사용
            int slotIndex = (int)rawValue;
            // 퀵슬롯 사용 요청
            GlobalEventBus.OnQuickSlotUseRequested?.Invoke(slotIndex);
        }
    }

    /* 플레이어 달리기 입력 처리 */
    public void OnSprint(InputAction.CallbackContext context)
    {
        // Sprint 키 입력 시작 시 및 입력 유지 중 달리기 상태를 true로
        if (context.started || context.performed)
        {
            isSprint = true;
        }
        // Sprint 키 입력 종료 시 달리기 상태를 false로
        if (context.canceled)
        {
            isSprint = false;
        }
        //변경한 isSprint 값을 이벤트로 PlayerStatus에 전달
        GlobalEventBus.OnSprintInput?.Invoke(isSprint);
    }

    /* 플레이어 구르기 입력 처리 */
    public void OnEvade(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            GlobalEventBus.OnEvadeRequested?.Invoke();
        }
    }

    /* 액션맵을 UI 모드로 전환 */
    public void SwitchToUIMap()
    {
        playerInput.SwitchCurrentActionMap("UI");
    }

    /* 액션맵을 플레이어 모드로 전환 */
    public void SwitchToPlayerMap()
    {
        playerInput.SwitchCurrentActionMap("Player");
    }

    public void SetInventoryOpenState(bool isOpen)
    {
        isInventoryOpen = isOpen;
    }
}
