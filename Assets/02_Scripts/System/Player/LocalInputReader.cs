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
    private bool isGameplayInputBlocked;
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


    /* 액션맵을 UI 모드로 전환 */
    public void SwitchToUIMap()
    {
        // 튜토리얼/인벤토리 UI가 열릴 때 기존 이동 입력이 남아 캐릭터가 계속 움직이지 않도록 즉시 정지 값을 전달합니다.
        isSprint = false;
        GlobalEventBus.OnSprintInput?.Invoke(false);
        GlobalEventBus.OnPlayerMove?.Invoke(Vector2.zero);

        playerInput.SwitchCurrentActionMap("UI");
    }

    /* 액션맵을 플레이어 모드로 전환 */
    public void SwitchToPlayerMap()
    {
        playerInput.SwitchCurrentActionMap("Player");
    }

    public void SetGameplayInputBlocked(bool isBlocked)
    {
        isGameplayInputBlocked = isBlocked;

        // 튜토리얼 UI가 열리는 순간 남아 있던 이동/달리기 입력이 유지되지 않도록 즉시 정지시킵니다.
        if (!isBlocked)
            return;

        isSprint = false;
        GlobalEventBus.OnSprintInput?.Invoke(false);
        GlobalEventBus.OnPlayerMove?.Invoke(Vector2.zero);
    }

    public void SetInventoryOpenState(bool isOpen)
    {
        isInventoryOpen = isOpen;
    }
}
