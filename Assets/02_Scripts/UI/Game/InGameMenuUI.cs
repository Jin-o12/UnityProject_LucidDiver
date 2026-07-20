using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameMenuUI : MonoBehaviour
{
    [Header("UI 버튼")]
    [SerializeField] Button closeButton;
    [SerializeField] Button settingButton;
    [SerializeField] Button lobbyButton;
    [SerializeField] Button exitButton;

    void OnEnable()
    {
        if(closeButton != null) closeButton.onClick.AddListener(OpenSettingUI);
        if(settingButton != null) settingButton.onClick.AddListener(OpenSettingUI);
        if(lobbyButton != null) lobbyButton.onClick.AddListener(OnLobbyButton);
        if(exitButton != null) exitButton.onClick.AddListener(ExitGame);
    } 

    void OnDisable()
    {
        if(closeButton != null) closeButton.onClick.RemoveListener(OpenSettingUI);
        if(settingButton != null) settingButton.onClick.RemoveListener(OpenSettingUI);
        if(lobbyButton != null) lobbyButton.onClick.RemoveListener(OnLobbyButton);
        if(exitButton != null) exitButton.onClick.RemoveListener(ExitGame);
    }

    /* 설정 UI 버튼 누를 시 */
    private void OpenSettingUI()
    {
        GlobalEventBus.OnOpenSettingUI?.Invoke();
    }

    /* 로비로 돌아가기 버튼 누를 시 */
    private void OnLobbyButton()
    {
        GlobalEventBus.OnOpenNoticeLobbyUI?.Invoke();
    }

    /* 게임 종료 버튼 누를 시 */
    private void ExitGame()
    {
// 유니티 에디터
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
// 실제 빌드된 게임
#else
        Application.Quit();
#endif
    }
}
