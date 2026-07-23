/// <summary>
/// 로컬 입력을 읽어서 글로벌 이벤트 버스에 전달하는 역할을 하는 클래스
/// 'Player input'에 등록되는 모든 메서드들이 정리되어 있습니다
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 동시에 입력을 차단하는 시스템들이 서로의 잠금을 해제하지 않도록 차단 소유자를 구분합니다.
/// </summary>
public enum GameplayInputBlockSource
{
    Legacy,
    Tutorial,
    GameMenu
}

public class LocalInputReader : MonoBehaviour
{
    // 참조 컴포넌트
    PlayerInput playerInput;
    public event Action OnInventoryOpenRequested;
    public event Action OnInventoryCloseRequested;
    private bool isInventoryOpen;
    private readonly HashSet<GameplayInputBlockSource> gameplayInputBlockSources = new();
    public bool isSprint;
    public bool IsInventoryOpen => isInventoryOpen;
    public bool IsGameplayInputBlocked => gameplayInputBlockSources.Count > 0;

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

        TrySwitchActionMap("UI");
    }

    /* 액션맵을 플레이어 모드로 전환 */
    public void SwitchToPlayerMap()
    {
        // 다른 시스템이 입력 차단을 유지 중이면 Player 맵을 다시 활성화하지 않습니다.
        if (IsGameplayInputBlocked)
            return;

        TrySwitchActionMap("Player");
    }

    /// <summary>
    /// 현재 입력을 관리하는 InputManager를 우선 사용하고, 연결되지 않은 경우에만 로컬 PlayerInput으로 전환합니다.
    /// </summary>
    private void TrySwitchActionMap(string mapName)
    {
        if (GlobalEventBus.OnSwitchInputMap != null)
        {
            GlobalEventBus.OnSwitchInputMap.Invoke(mapName);
            return;
        }

        // 플레이어 프리팹의 PlayerInput은 중앙 InputManager 구조에서 비활성화될 수 있으므로 안전하게 확인합니다.
        if (playerInput == null ||
            !playerInput.enabled ||
            !playerInput.gameObject.activeInHierarchy ||
            playerInput.actions == null)
        {
            Debug.LogWarning($"LocalInputReader: 입력 맵을 전환할 수 없습니다. ({mapName})", this);
            return;
        }

        InputActionMap targetMap = playerInput.actions.FindActionMap(mapName, false);
        if (targetMap == null)
        {
            Debug.LogWarning($"LocalInputReader: 입력 맵을 찾을 수 없습니다. ({mapName})", this);
            return;
        }

        if (playerInput.currentActionMap == targetMap)
            return;

        playerInput.SwitchCurrentActionMap(mapName);
    }

    /// <summary>
    /// 기존 호출부 호환용 입력 차단 API입니다.
    /// 신규 시스템은 차단 소유자를 명시하는 오버로드를 사용합니다.
    /// </summary>
    public void SetGameplayInputBlocked(bool isBlocked)
    {
        SetGameplayInputBlocked(isBlocked, GameplayInputBlockSource.Legacy);
    }

    /// <summary>
    /// 지정한 시스템의 입력 차단 상태만 추가하거나 해제합니다.
    /// </summary>
    public void SetGameplayInputBlocked(bool isBlocked, GameplayInputBlockSource source)
    {
        if (isBlocked)
            gameplayInputBlockSources.Add(source);
        else
            gameplayInputBlockSources.Remove(source);

        // 차단을 거는 순간 남아 있던 이동/달리기 입력이 유지되지 않도록 즉시 정지시킵니다.
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
