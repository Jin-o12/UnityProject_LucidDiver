using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }  //싱글톤 인스턴스 지정
    [SerializeField] private PlayerInput playerInput;

    // 메뉴를 닫을 때 열기 전 입력 상태로 안전하게 복구하기 위한 현재 액션 맵 이름입니다.
    public string CurrentActionMapName => CanUsePlayerInput() ? playerInput.currentActionMap?.name : null;

    private void Awake()
    {
        // 싱글톤 인스턴스 중복 방지 설정
        if (Instance != null)
        {
            // 같은 Manager 오브젝트에 다른 매니저 컴포넌트가 함께 붙어 있으므로 오브젝트 전체가 아니라 ResultManager 컴포넌트만 제거합니다.
            Destroy(this);
            return;
        }
        else
            Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        GlobalEventBus.OnSwitchInputMap += SwitchInputMap;
        GlobalEventBus.OnGetInputAction += GetAction;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnSwitchInputMap -= SwitchInputMap;
        GlobalEventBus.OnGetInputAction -= GetAction;
    }

    public void SwitchInputMap(string mapName)
    {
        if (string.IsNullOrWhiteSpace(mapName))
        {
            Debug.LogWarning("[InputManager] 전환할 입력 맵 이름이 비어 있습니다.", this);
            return;
        }

        if (!CanUsePlayerInput())
        {
            Debug.LogWarning($"[InputManager] PlayerInput이 준비되지 않아 입력 맵을 전환할 수 없습니다. ({mapName})", this);
            return;
        }

        InputActionMap targetMap = playerInput.actions.FindActionMap(mapName, false);
        if (targetMap == null)
        {
            Debug.LogWarning($"[InputManager] 입력 맵을 찾을 수 없습니다. ({mapName})", this);
            return;
        }

        if (playerInput.currentActionMap == targetMap)
            return;

        playerInput.SwitchCurrentActionMap(mapName);
        Debug.Log($"[InputManager] 조작 모드가 변경되었습니다: {mapName}");
    }

    // 다른 씬에서 특정 키(액션)를 구독할 수 있게 반환해주는 함수
    public InputAction GetAction(string mapName, string actionName)
    {
        if (!CanUsePlayerInput())
        {
            Debug.LogWarning("[InputManager] PlayerInput이 준비되지 않아 액션을 찾을 수 없습니다.", this);
            return null;
        }

        InputActionMap actionMap = playerInput.actions.FindActionMap(mapName, false);
        if (actionMap == null)
        {
            Debug.LogWarning($"[InputManager] 입력 맵을 찾을 수 없습니다. ({mapName})", this);
            return null;
        }

        InputAction action = actionMap.FindAction(actionName, false);
        if (action == null)
            Debug.LogWarning($"[InputManager] 입력 액션을 찾을 수 없습니다. ({mapName}/{actionName})", this);

        return action;
    }

    /// <summary>
    /// 현재 중앙 입력기가 액션 조회와 맵 전환에 사용 가능한 상태인지 확인합니다.
    /// </summary>
    private bool CanUsePlayerInput()
    {
        return playerInput != null &&
               playerInput.enabled &&
               playerInput.gameObject.activeInHierarchy &&
               playerInput.actions != null;
    }
}
