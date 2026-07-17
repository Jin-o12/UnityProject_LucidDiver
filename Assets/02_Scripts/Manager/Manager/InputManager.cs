using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }  //싱글톤 인스턴스 지정
    [SerializeField] private PlayerInput playerInput;

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
    }

    private void OnDisable()
    {
        GlobalEventBus.OnSwitchInputMap -= SwitchInputMap;
    }

    public void SwitchInputMap(string mapName)
    {
        if (playerInput.currentActionMap != null && playerInput.currentActionMap.name == mapName) return;

        playerInput.SwitchCurrentActionMap(mapName);
        Debug.Log($"[InputManager] 조작 모드가 변경되었습니다: {mapName}");
    }

    // 다른 씬에서 특정 키(액션)를 구독할 수 있게 반환해주는 함수
    public InputAction GetAction(string mapName, string actionName)
    {
        return playerInput.actions.FindActionMap(mapName).FindAction(actionName);
    }
}
