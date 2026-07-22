using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameMenuUI : MonoBehaviour
{
    [Header("UI 버튼")]
    [SerializeField] Button closeButton;
    [SerializeField] Button settingButton;
    [SerializeField] Button exitButton;

    void OnEnable()
    {
        if(closeButton != null) closeButton.onClick.AddListener(CloseMenuUI);
        if(settingButton != null) settingButton.onClick.AddListener(OpenSettingUI);
        if(exitButton != null) exitButton.onClick.AddListener(ExitGame);
    } 

    void OnDisable()
    {
        if(closeButton != null) closeButton.onClick.RemoveListener(CloseMenuUI);
        if(settingButton != null) settingButton.onClick.RemoveListener(OpenSettingUI);
        if(exitButton != null) exitButton.onClick.RemoveListener(ExitGame);
    }

    /* 닫기 버튼을 누르면 현재 로비 게임 메뉴만 닫는다. */
    private void CloseMenuUI()
    {
        GlobalEventBus.OnCloseTopUI?.Invoke();
    }

    private void OpenSettingUI()
    {
        GlobalEventBus.OnOpenSettingUI?.Invoke();
    }

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
