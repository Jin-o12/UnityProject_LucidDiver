/// <summary>
/// 로컬 입력을 읽어서 글로벌 이벤트 버스에 전달하는 역할을 하는 클래스
/// PlayerInput에 등록되는 입력 메서드들을 모아 둔다
/// </summary>
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class LocalInputReader : MonoBehaviour
{
    // 참조 컴포넌트
    private PlayerInput playerInput;

    public event Action OnInventoryOpenRequested;
    public event Action OnInventoryCloseRequested;

    private bool isInventoryOpen;   // 인벤토리나 체스트 UI가 열려 있는지 여부

    private void Awake()
    {
        playerInput = GetComponentInParent<PlayerInput>();

        // 필수 컴포넌트가 존재하지 않을 시 스크립트 비활성화
        if (playerInput == null)
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
        // 인벤토리가 열려 있을 때는 공격 입력을 막는다
        if (isInventoryOpen)
            return;

        if (context.performed)
        {
            GlobalEventBus.OnAttackInput?.Invoke();
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
            // 일반 인벤토리는 Player 맵을 유지한 채 열고
            // 이미 열려 있다면 같은 E 키로 다시 닫을 수 있게 토글 처리한다
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
        // 인벤토리가 열려 있을 때는 퀵슬롯 사용을 막는다
        if (isInventoryOpen)
            return;

        if (context.performed)
        {
            // Scale값을 float로 읽어옴
            float rawValue = context.ReadValue<float>();

            // int로 형변환 하여 인덱스로 사용
            int slotIndex = (int)rawValue;

            // 퀵슬롯 사용 요청
            GlobalEventBus.OnQuickSlotUseRequested?.Invoke(slotIndex);
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

    /// <summary>
    /// 인벤토리 열림 상태를 기록한다
    /// 일반 인벤토리는 Player 맵을 유지하므로 입력 분기용으로 사용한다
    /// </summary>
    public void SetInventoryOpenState(bool isOpen)
    {
        isInventoryOpen = isOpen;
    }
}
