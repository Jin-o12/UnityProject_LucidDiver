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
        IResultService resultService = ResultServiceLocator.Instance;
        if (resultService == null)
        {
            Debug.LogError("NoticeLobbyUI: 게임 포기 결과를 처리할 ResultService가 준비되지 않았습니다.", this);
            return;
        }

        GlobalEventBus.OnCloseTopUI?.Invoke();
        // 씬을 즉시 이동하지 않고 기존 실패 정산과 결과 화면을 거친 뒤 로비 버튼에서 이동합니다.
        resultService.HandleGameAbandon();
    }
}
