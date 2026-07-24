using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoticeLobbyUI : MonoBehaviour, ICloseAnimatable
{
    [Header("DOTween 연출")]
    [SerializeField] RectTransform panel;
    CanvasGroup panelGroup;                 //연출 적용 캔버스 그룹
    public float initTime = 0.5f;           //연출 적용 시간
    public float initScale = 0.75f;         //연출용 사이즈
    public float initFade = 0.5f;           //연출용 투명도

    [Header("UI 버튼")]
    [SerializeField] Button closeButton;
    [SerializeField] Button lobbyButton;

    private void Awake()
    {
        panelGroup = GetComponent<CanvasGroup>();
        DOTween.Init();
    }

    void OnEnable()
    {
        if(closeButton != null) closeButton.onClick.AddListener(CloseNoticeUI);
        if(lobbyButton != null) lobbyButton.onClick.AddListener(GoToLobby);

        // 팝업을 열 때 게임 메뉴 UI를 숨김
        GlobalEventBus.OnHideMenuUI.Invoke(true);

        /* DOTween 시퀀스로 스케일 애니메이션 재생 */
        panel.localScale = Vector3.one * initScale;
        panelGroup.alpha = initFade;
        DOTween.Sequence().Append(panel.DOScale(1f, initTime)).
            Join(panelGroup.DOFade(1f, initTime));
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

    /* DOTween 시퀀스로 UI 닫기 애니메이션 재생 */
    public void PlayCloseAnimation(Action onComplete)
    {
        if (panel == null)
        {
            onComplete?.Invoke();
            return;
        }

        panel.localScale = Vector3.one;
        DOTween.Sequence().
            Append(panel.DOScale(initScale, initTime)).
            Join(panelGroup.DOFade(initFade, initTime)).
            OnComplete(() =>
            {
                onComplete?.Invoke();
                GlobalEventBus.OnHideMenuUI?.Invoke(false);  // 팝업을 닫을 때 게임 메뉴 UI를 숨김 해제
            });
    }
}
