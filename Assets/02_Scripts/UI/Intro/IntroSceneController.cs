using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using TMPro;

public class IntroSceneController : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoRenderImage;
    [SerializeField] private VideoClip initialPVClip; // Lucid_Diver_PV.mp4
    [SerializeField] private VideoClip loopTitleClip;  // Intro_PV.mp4
    [SerializeField] private bool forcePlayPV = true; // 테스트용 항시 PV 재생 모드

    [Header("UI Panels")]
    [SerializeField] private CanvasGroup skipGuidePanel; // 스킵 가이드 패널 (중앙 배치)
    [SerializeField] private CanvasGroup titleUIPanel;   // 타이틀 메인 UI 패널
    [SerializeField] private CanvasGroup loadingOverlay; // 로딩 검은화면 + 중앙 로고 오버레이 패널
    [SerializeField] private Image loadingLogoImage;     // 로딩 화면 중앙 로고 이미지 (페이드인 제어용)

    [Header("Title UI Elements")]
    [SerializeField] private TMP_Text tapToStartText;
    [SerializeField] private Button settingButton;          // SETTING 단일 버튼
    [SerializeField] private GameObject settingPopupPanel;  // 설정 더미 팝업 패널
    [SerializeField] private Button settingCloseButton;     // 설정 팝업 닫기 버튼

    private enum IntroState
    {
        PlayingPV,
        TitleScreen
    }

    private IntroState currentState = IntroState.PlayingPV;
    private bool skipPromptActive = false;
    private float skipPromptTimer = 0f;
    private bool isTransitioning = false;
    private RenderTexture dynamicRT;

    private void Awake()
    {
        // 씬 내 필수 UI 컴포넌트 자동 바인딩 및 예외 처리
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null) videoPlayer = gameObject.AddComponent<VideoPlayer>();
        
        // 1920x1080 규격의 dynamic RenderTexture 생성 및 매핑
        dynamicRT = new RenderTexture(1920, 1080, 24);
        videoPlayer.targetTexture = dynamicRT;
        if (videoRenderImage != null)
        {
            videoRenderImage.texture = dynamicRT;
        }

        // 초기 알파 설정
        if (skipGuidePanel != null) skipGuidePanel.alpha = 0f;
        if (titleUIPanel != null)
        {
            titleUIPanel.alpha = 0f;
            titleUIPanel.interactable = false;
            titleUIPanel.blocksRaycasts = false;
        }
        
        // 시작 시 로딩 오버레이가 완전히 덮도록 설정 보장
        if (loadingOverlay != null)
        {
            loadingOverlay.gameObject.SetActive(true);
            loadingOverlay.alpha = 1f;
        }

        // 로고 페이드인을 위해 초기 상태 알파를 0으로 조율
        if (loadingLogoImage != null)
        {
            Color c = loadingLogoImage.color;
            c.a = 0f;
            loadingLogoImage.color = c;
        }
    }

    private void Start()
    {
        // 설정 버튼 리스너 세팅
        if (settingButton != null)
        {
            settingButton.onClick.AddListener(() => OpenSettingPopup());
        }
        if (settingCloseButton != null)
        {
            settingCloseButton.onClick.AddListener(() => CloseSettingPopup());
        }

        // 설정 팝업 초기화
        if (settingPopupPanel != null)
        {
            settingPopupPanel.SetActive(false);
        }

        // 대기 및 비디오 로딩 가동 코루틴 실행
        StartCoroutine(IntroSequenceCoroutine());
    }

    private void Update()
    {
        // 마우스 클릭 및 모바일 터치 감지
        if (Input.GetMouseButtonDown(0))
        {
            HandleScreenTap();
        }

        // 스킵 프롬프트 자동 소멸 타이머
        if (skipPromptActive)
        {
            skipPromptTimer -= Time.deltaTime;
            if (skipPromptTimer <= 0f)
            {
                HideSkipPrompt();
            }
        }

        // 스크립트 효과 연출 (Blinking, Fading)
        UpdateVisualEffects();
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnInitialPVFinished;
        }
        
        if (dynamicRT != null)
        {
            dynamicRT.Release();
            Destroy(dynamicRT);
        }
    }

    // 영상 완전 로딩 대기 코루틴 (로고 페이드인 1초 + 강제 3초 대기 + 비디오 로딩 대기)
    private IEnumerator IntroSequenceCoroutine()
    {
        bool isFirstPlay = forcePlayPV || CheckIsFirstPlay();
        if (isFirstPlay && initialPVClip != null)
        {
            currentState = IntroState.PlayingPV;
            videoPlayer.clip = initialPVClip;
            videoPlayer.isLooping = false;
            videoPlayer.loopPointReached += OnInitialPVFinished;
        }
        else
        {
            currentState = IntroState.TitleScreen;
            if (loopTitleClip != null)
            {
                videoPlayer.clip = loopTitleClip;
                videoPlayer.isLooping = true;
            }
        }

        // 비디오 클립 비동기 준비 시작
        videoPlayer.Prepare();

        // 1단계: 로고 페이드인 연출 (1.0초간 서서히 노출)
        float logoFadeElapsed = 0f;
        float logoFadeDuration = 1.0f;
        while (logoFadeElapsed < logoFadeDuration)
        {
            logoFadeElapsed += Time.deltaTime;
            if (loadingLogoImage != null)
            {
                Color c = loadingLogoImage.color;
                c.a = Mathf.Lerp(0f, 1f, logoFadeElapsed / logoFadeDuration);
                loadingLogoImage.color = c;
            }
            yield return null;
        }

        // 로고 알파 1.0 고정
        if (loadingLogoImage != null)
        {
            Color c = loadingLogoImage.color;
            c.a = 1f;
            loadingLogoImage.color = c;
        }

        // 2단계: 총 3.0초 대기시간 충족 및 비디오 준비 완료 대기 (페이드인에 쓴 1.0초 제외 2.0초 추가 대기)
        float elapsed = logoFadeElapsed;
        while (elapsed < 3.0f || !videoPlayer.isPrepared)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 3단계: 로고만 먼저 페이드 아웃 (0.5초 간) - 배경 검은화면은 100% 알파로 유지하여 스카이박스 노출 방지
        if (loadingLogoImage != null)
        {
            float logoFadeOutDuration = 0.5f;
            float logoFadeOutElapsed = 0f;
            while (logoFadeOutElapsed < logoFadeOutDuration)
            {
                logoFadeOutElapsed += Time.deltaTime;
                Color c = loadingLogoImage.color;
                c.a = Mathf.Lerp(1f, 0f, logoFadeOutElapsed / logoFadeOutDuration);
                loadingLogoImage.color = c;
                yield return null;
            }
            Color cFinal = loadingLogoImage.color;
            cFinal.a = 0f;
            loadingLogoImage.color = cFinal;
        }

        // 4단계: 비디오 재생 시작 (검은 화면 뒤에서 영상 재생 시작)
        videoPlayer.Play();

        // 비디오 첫 프레임 렌더 버퍼 대기
        yield return null;

        // 5단계: 로딩 오버레이 패널 전체(검은 배경)를 부드럽게 페이드 아웃 (0.5초 간)
        if (loadingOverlay != null)
        {
            float fadeDuration = 0.5f;
            float fadeElapsed = 0f;
            while (fadeElapsed < fadeDuration)
            {
                fadeElapsed += Time.deltaTime;
                loadingOverlay.alpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeDuration);
                yield return null;
            }
            loadingOverlay.gameObject.SetActive(false);
        }

        // 다회 실행 상태로 즉시 타이틀에 진입했을 경우 UI 상호작용 개방
        if (currentState == IntroState.TitleScreen)
        {
            if (titleUIPanel != null)
            {
                titleUIPanel.interactable = true;
                titleUIPanel.blocksRaycasts = true;
            }
        }
    }

    // 화면 터치 제어 핸들러
    private void HandleScreenTap()
    {
        // 로딩 오버레이 작동 중에는 화면 입력 무시
        if (loadingOverlay != null && loadingOverlay.gameObject.activeSelf && loadingOverlay.alpha > 0.1f) return;

        // 설정 창이 열려 있는 경우 탭 입력 및 씬 이동 무시
        if (settingPopupPanel != null && settingPopupPanel.activeSelf) return;

        // 버튼 영역 클릭 시 화면 터치 처리 무시
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (currentState == IntroState.PlayingPV)
        {
            if (!skipPromptActive)
            {
                ShowSkipPrompt();
            }
            else
            {
                // 두 번째 터치 시 스킵
                SkipInitialPV();
            }
        }
        else if (currentState == IntroState.TitleScreen)
        {
            if (!isTransitioning)
            {
                StartLobbySceneTransition();
            }
        }
    }

    // 최초 PV 재생 완료 콜백
    private void OnInitialPVFinished(VideoPlayer source)
    {
        EnterTitleScreen();
    }

    // 스킵 유도 프롬프트 활성화
    private void ShowSkipPrompt()
    {
        skipPromptActive = true;
        skipPromptTimer = 3.0f; // 3초간 노출
    }

    // 스킵 유도 프롬프트 비활성화
    private void HideSkipPrompt()
    {
        skipPromptActive = false;
    }

    // PV 강제 스킵
    private void SkipInitialPV()
    {
        HideSkipPrompt();
        videoPlayer.Stop();
        EnterTitleScreen();
    }

    // 타이틀 메인 화면 진입
    private void EnterTitleScreen()
    {
        UpdateFirstPlayFlag();
        currentState = IntroState.TitleScreen;

        // 스킵으로 중간에 튀었을 경우 로딩 Overlay가 켜져 있다면 강제 오프
        if (loadingOverlay != null && loadingOverlay.gameObject.activeSelf)
        {
            loadingOverlay.gameObject.SetActive(false);
        }

        // 배경 비디오 루프 클립 변경 및 가동
        if (loopTitleClip != null)
        {
            videoPlayer.loopPointReached -= OnInitialPVFinished;
            videoPlayer.clip = loopTitleClip;
            videoPlayer.isLooping = true;
            videoPlayer.Play();
        }
        
        // 타이틀 UI 상호작용 활성화
        if (titleUIPanel != null)
        {
            titleUIPanel.interactable = true;
            titleUIPanel.blocksRaycasts = true;
        }
    }

    // 설정 팝업 열기
    private void OpenSettingPopup()
    {
        if (settingPopupPanel != null)
        {
            settingPopupPanel.SetActive(true);
        }
    }

    // 설정 팝업 닫기
    private void CloseSettingPopup()
    {
        if (settingPopupPanel != null)
        {
            settingPopupPanel.SetActive(false);
        }
    }

    // 로비 씬 전환 시작
    private void StartLobbySceneTransition()
    {
        isTransitioning = true;
        StartCoroutine(LobbyLoadCoroutine());
    }

    private IEnumerator LobbyLoadCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 씬 로더 전환 적용 (리플렉션을 통한 어셈블리 경계 우회)
        bool loadedByController = false;
        System.Type sceneControllerType = System.Type.GetType("SceneController, ManagerAD");
        if (sceneControllerType != null)
        {
            var instanceProp = sceneControllerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (instanceProp != null)
            {
                var instance = instanceProp.GetValue(null);
                if (instance != null)
                {
                    var method = sceneControllerType.GetMethod("GoToLobbyScene", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (method != null)
                    {
                        method.Invoke(instance, null);
                        loadedByController = true;
                    }
                }
            }
        }

        if (!loadedByController)
        {
            // 에디터 독립 구동 및 대비 Fallback
            SceneManager.LoadScene("LobbyScene");
        }
    }

    // 점멸 및 페이드 효과 처리
    private void UpdateVisualEffects()
    {
        // 스킵 패널 페이드
        if (skipGuidePanel != null)
        {
            float targetAlpha = skipPromptActive ? 1.0f : 0.0f;
            skipGuidePanel.alpha = Mathf.MoveTowards(skipGuidePanel.alpha, targetAlpha, Time.deltaTime * 4f);
        }

        // 타이틀 UI 페이드인
        if (currentState == IntroState.TitleScreen && titleUIPanel != null)
        {
            titleUIPanel.alpha = Mathf.MoveTowards(titleUIPanel.alpha, 1.0f, Time.deltaTime * 2f);
        }

        // TAP TO START 텍스트 서서히 점멸 (Blue Archive 스타일)
        if (currentState == IntroState.TitleScreen && tapToStartText != null)
        {
            float alpha = 0.3f + Mathf.PingPong(Time.time * 1.5f, 0.7f); // 0.3 ~ 1.0 사이 점멸
            Color color = tapToStartText.color;
            color.a = alpha;
            tapToStartText.color = color;
        }
    }

    // persistentDataPath/SaveFile.json 데이터를 파싱하여 최초 시작 유무 리턴
    private bool CheckIsFirstPlay()
    {
        string path = Path.Combine(Application.persistentDataPath, "SaveFile.json");
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
                if (data != null)
                {
                    return data.isFirstPlay;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Intro] 세이브 데이터 복구 실패: {e.Message}");
            }
        }
        return true; // 세이브 파일 자체가 존재하지 않는 경우 최초 진입으로 판단
    }

    // 최초 실행 여부를 false로 업데이트 및 물리 세이브 저장
    private void UpdateFirstPlayFlag()
    {
        string path = Path.Combine(Application.persistentDataPath, "SaveFile.json");
        PlayerSaveData data = null;

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                data = JsonUtility.FromJson<PlayerSaveData>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Intro] 세이브 파일 불러오기 에러: {e.Message}");
            }
        }

        if (data == null)
        {
            data = new PlayerSaveData();
        }

        if (data.isFirstPlay)
        {
            data.isFirstPlay = false;
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(path, json);
                
                // 런타임 캐싱 SO 업데이트가 활성 상태일 시 연동
                if (PlayerSaveDataSO.Instance != null && PlayerSaveDataSO.Instance.currentData != null)
                {
                    PlayerSaveDataSO.Instance.currentData.isFirstPlay = false;
                }
                Debug.Log("[Intro] 세이브의 isFirstPlay를 false로 정상 변경 저장 완료.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Intro] 세이브 파일 플래그 업데이트 실패: {e.Message}");
            }
        }
    }
}
