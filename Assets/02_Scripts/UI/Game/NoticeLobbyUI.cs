using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoticeLobbyUI : MonoBehaviour
{
    [Header("UI 버튼")]
    [SerializeField] Button closeButton;
    [SerializeField] Button lobbyButton;

    void OnEnable()
    {
        if(closeButton != null) closeButton.onClick.AddListener(CloseNoticeUI);
        if(lobbyButton != null) lobbyButton.onClick.AddListener(GoToLobby);
    } 

    void OnDisable()
    {
        if(closeButton != null) closeButton.onClick.RemoveListener(CloseNoticeUI);
        if(lobbyButton != null) lobbyButton.onClick.RemoveListener(GoToLobby);
    }

    private void CloseNoticeUI()
    {
        GlobalEventBus.OnCloseTopUI?.Invoke();
    }

    private void GoToLobby()
    {
        GlobalEventBus.OnCloseTopUI?.Invoke();
        GlobalEventBus.OnGoToLobbyScene?.Invoke();
    }
}
