using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

public class InGameMenuUI : MonoBehaviour, ICloseAnimatable
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
    [SerializeField] Button lobbyButton;
    [SerializeField] Button exitButton;

    private void Awake()
    {
        panelGroup = GetComponent<CanvasGroup>();
        DOTween.Init();
    }

    void OnEnable()
    {
        if (closeButton != null) closeButton.onClick.AddListener(CloseMenuUI);
        if (settingButton != null) settingButton.onClick.AddListener(OpenSettingUI);
        if (lobbyButton != null) lobbyButton.onClick.AddListener(OnLobbyButton);
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);

        // Vignette 조절 이벤트를 호출
        GlobalEventBus.OnVignetteChange?.Invoke(0.5f, 0.2f, true);

        /* DOTween 시퀀스로 스케일 애니메이션 재생 */
        panel.localScale = Vector3.one * initScale;
        panelGroup.alpha = initFade;
        DOTween.Sequence()
            .Append(panel.DOScale(1f, initTime)).
            Join(panelGroup.DOFade(1f, initTime));
    }

    void OnDisable()
    {
        // Vignette 조절 이벤트를 호출
        GlobalEventBus.OnVignetteChange?.Invoke(0.25f, 0.2f, true);

        if (closeButton != null) closeButton.onClick.RemoveListener(CloseMenuUI);
        if(settingButton != null) settingButton.onClick.RemoveListener(OpenSettingUI);
        if(lobbyButton != null) lobbyButton.onClick.RemoveListener(OnLobbyButton);
        if(exitButton != null) exitButton.onClick.RemoveListener(ExitGame);
    }

    /* 닫기 버튼을 누르면 인게임 메뉴 닫기를 요청한다. */
    private void CloseMenuUI()
    {
        GlobalEventBus.OnCloseInGameMenuUI?.Invoke();
    }

    /// <summary>
    /// ICloseAnimatable 구현: 닫기 애니메이션을 실행하고 완료 시 onComplete를 호출합니다.
    /// UIManager가 이 메서드를 호출하면 onComplete에서 실제 비활성화/스택 제거가 수행됩니다.
    /// </summary>
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
