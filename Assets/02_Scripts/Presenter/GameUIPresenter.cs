using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameUIPresenter : MonoBehaviour
{
    private UIManager uiManager;
    private InGameMenuUI inGameMenuUI;                  // 게임 메뉴 UI 캐시
    private SettingUI settingUI;                    // 설정 UI 캐시
    private NoticeLobbyUI noticeLobbyUI;            // 로비 이동 경고 UI
    private InputAction openSettingAction;          // 설정 창 열기 이벤트 (ESC)

    private void Start()
    {
        uiManager = UIManager.Instance;
    }

    private void OnEnable()
    {
        GlobalEventBus.OnOpenSettingUI += OpenSettingUI;
        GlobalEventBus.OnOpenNoticeLobbyUI += OpenNoticeLobbyUI;

        if (InputManager.Instance != null)
        {
            openSettingAction = InputManager.Instance.GetAction("Player", "GameMenu");
            if (openSettingAction != null)
            {
                openSettingAction.Enable();
                openSettingAction.performed += OpenPauseUI;
            }
        }
    }

    private void OnDisable()
    {
        GlobalEventBus.OnOpenSettingUI -= OpenSettingUI;
        GlobalEventBus.OnOpenNoticeLobbyUI -= OpenNoticeLobbyUI;

        if (openSettingAction != null)
        {
            openSettingAction.performed -= OpenPauseUI;
            openSettingAction.Disable();
        }
    }

    /* ESC 목록UI 열기 */
    public void OpenPauseUI(InputAction.CallbackContext context)
    {
        if (uiManager == null)
            uiManager = UIManager.Instance;
            
        if (uiManager == null) return;

        if (inGameMenuUI == null)
        {
            inGameMenuUI = uiManager.Open<InGameMenuUI>();
            return;
        }

        if (inGameMenuUI.gameObject.activeInHierarchy == false)
        {
            uiManager.Open<InGameMenuUI>();
        }
        else
        {
            if (uiManager.GetTopUI() != inGameMenuUI)
            {
                uiManager.CloseNowUI();
            }
            else
            {
                uiManager.Close<InGameMenuUI>();
            }
        }
    }

    /* 설정 UI 열기 */
    public void OpenSettingUI()
    {
        if (uiManager == null)
            uiManager = UIManager.Instance;
            
        if (uiManager == null) return;

        if (settingUI == null)
        {
            settingUI = uiManager.Open<SettingUI>();
            return;
        }

        if (settingUI.gameObject.activeInHierarchy == false)
        {
            uiManager.Open<SettingUI>();
        }
        else
        {
            uiManager.Close<SettingUI>();
        }
    }

    /* 로바 아동 시 경고 팝업 출력 */
    private void OpenNoticeLobbyUI()
    {
        if (uiManager == null)
            uiManager = UIManager.Instance;
            
        if (uiManager == null) return;

        if (noticeLobbyUI == null)
        {
            noticeLobbyUI = uiManager.Open<NoticeLobbyUI>();
            return;
        }

        if (noticeLobbyUI.gameObject.activeInHierarchy == false)
        {
            uiManager.Open<NoticeLobbyUI>();
        }
        else
        {
            uiManager.Close<NoticeLobbyUI>();
        }
    }
    
    /* 로비로 돌아가기 */
    private void GoToLobby()
    {
        
    }
}
