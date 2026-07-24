using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameMenuUI : MonoBehaviour, ICloseAnimatable
{
    [Header("DOTween 연출")]
    [SerializeField] RectTransform panel;
    public CanvasGroup panelGroup;          //연출 적용 캔버스 그룹
    public float initTime = 0.5f;           //연출 적용 시간
    public float initScale = 0.75f;         //연출용 사이즈
    public float initFade = 0.5f;           //연출용 투명도

    [Header("UI 버튼")]
    [SerializeField] Button closeButton;
    [SerializeField] Button settingButton;
    [SerializeField] Button exitButton;

    private void Awake()
    {
        panelGroup = GetComponent<CanvasGroup>();
        DOTween.Init();
    }

    void OnEnable()
    {
        if(closeButton != null) closeButton.onClick.AddListener(CloseMenuUI);
        if(settingButton != null) settingButton.onClick.AddListener(OpenSettingUI);
        if(exitButton != null) exitButton.onClick.AddListener(ExitGame);

        /* DOTween 시퀀스로 스케일 애니메이션 재생 */
        panel.localScale = Vector3.one * initScale;
        panelGroup.alpha = initFade;
        DOTween.Sequence().Append(panel.DOScale(1f, initTime)).
            Join(panelGroup.DOFade(1f, initTime));
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
            });
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
