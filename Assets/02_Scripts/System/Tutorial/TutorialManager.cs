using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 튜토리얼 안내 데이터의 열림 조건과 클리어 조건을 해석하여 팝업 출력 흐름을 관리합니다.
/// 우선 Resources/JSON/TutorialGuide.json 데이터를 사용하고, 데이터가 없을 경우 기존 ScriptableObject 카탈로그 호출도 유지합니다.
/// </summary>
public sealed class TutorialManager : MonoBehaviour
{
    private const string CatalogResourcePath = "Tutorial/TutorialMessageCatalog";

    public static TutorialManager Instance { get; private set; }

    [SerializeField] private TutorialMessageCatalog catalog;
    [SerializeField] private TutorialPopup popup;

    private readonly Dictionary<int, TutorialGuideData> guideById = new();
    private readonly HashSet<int> openedGuideIds = new();
    private readonly Queue<TutorialGuideData> pendingGuides = new();

    private Action currentCompletion;
    private LocalInputReader inputReader;
    private TutorialGuideData currentGuide;
    private Coroutine durationRoutine;
    private float previousTimeScale = 1f;
    private bool isShowing;
    private bool pausedByTutorial;
    private bool tutorialCompletionSaved;

    public bool IsShowing => isShowing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        catalog ??= Resources.Load<TutorialMessageCatalog>(CatalogResourcePath);
        popup ??= GetComponentInChildren<TutorialPopup>(true);
        popup?.HideImmediate();

        LoadJsonGuides();
    }

    private void Start()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        NotifySceneLoaded(activeScene.name);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        GlobalEventBus.OnItemBoxOpened += HandleItemBoxOpened;
        GlobalEventBus.OnEnemyDead += HandleEnemyDead;
        GlobalEventBus.OnEscapeRequest += HandleEscapeRequest;
        GlobalEventBus.OnMainActiveSkillCasted += HandleMainActiveSkillCasted;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        GlobalEventBus.OnItemBoxOpened -= HandleItemBoxOpened;
        GlobalEventBus.OnEnemyDead -= HandleEnemyDead;
        GlobalEventBus.OnEscapeRequest -= HandleEscapeRequest;
        GlobalEventBus.OnMainActiveSkillCasted -= HandleMainActiveSkillCasted;

        if (!isShowing)
            return;

        popup?.HideImmediate();
        isShowing = false;
        currentCompletion = null;
        currentGuide = null;
        StopDurationRoutine();
        RestoreGameplay();
    }

    private void Update()
    {
        TryShowPendingGuide();

        if (!isShowing || popup == null)
            return;

        bool keyboardConfirmed = Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.spaceKey.wasPressedThisFrame ||
             Keyboard.current.escapeKey.wasPressedThisFrame);
        bool gamepadConfirmed = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        if (keyboardConfirmed || gamepadConfirmed)
            popup.Confirm();
    }

    /// <summary>
    /// 기존 TutorialTrigger나 외부 코드에서 특정 ID 팝업을 직접 요청할 때 사용하는 호환용 API입니다.
    /// JSON 데이터가 로드된 경우에는 JSON 흐름만 사용하고, JSON 데이터가 없을 때만 기존 카탈로그를 백업으로 사용합니다.
    /// </summary>
    public bool Show(string tutorialId, Action completed = null)
    {
        if (int.TryParse(tutorialId, out int tid) && guideById.TryGetValue(tid, out TutorialGuideData guide))
            return ShowGuide(guide, completed);

        if (guideById.Count > 0)
        {
            Debug.LogWarning($"[TutorialManager] JSON 튜토리얼 데이터가 로드되어 Catalog fallback을 사용하지 않습니다. 요청 ID={tutorialId}", this);
            return false;
        }

        if (isShowing || popup == null || catalog == null)
            return false;

        if (!catalog.TryGetEntry(tutorialId, out TutorialMessageEntry entry))
        {
            Debug.LogWarning($"[TutorialManager] 등록되지 않은 튜토리얼 ID입니다: {tutorialId}", this);
            return false;
        }

        isShowing = true;
        currentCompletion = completed;

        if (entry.PauseGame)
            PauseGameplay();

        popup.Show(entry, CompleteLegacyPopup);
        return true;
    }

    /// <summary>
    /// 콜라이더 트리거 진입 조건을 튜토리얼 데이터 시스템에 전달합니다.
    /// </summary>
    public bool NotifyTriggerEnter(string triggerValue)
    {
        bool clearedCurrent = TryClearCurrent(TutorialConditionTypes.TriggerEnter, triggerValue);
        bool openedNew = TryOpenByCondition(TutorialConditionTypes.TriggerEnter, triggerValue);
        return clearedCurrent || openedNew;
    }

    /// <summary>
    /// 임의 이벤트 조건을 튜토리얼 데이터 시스템에 전달합니다.
    /// </summary>
    public bool NotifyEvent(string eventName)
    {
        bool clearedCurrent = TryClearCurrent(TutorialConditionTypes.Event, eventName);
        bool openedNew = TryOpenByCondition(TutorialConditionTypes.Event, eventName);
        return clearedCurrent || openedNew;
    }

    private void LoadJsonGuides()
    {
        guideById.Clear();
        List<TutorialGuideData> guides = new LocalJsonTutorialGuideRepository().LoadAll();
        guides.Sort((a, b) =>
        {
            int stepCompare = a.TutorialStep.CompareTo(b.TutorialStep);
            return stepCompare != 0 ? stepCompare : a.TutorialGuideOrder.CompareTo(b.TutorialGuideOrder);
        });

        foreach (TutorialGuideData guide in guides)
        {
            if (guide == null || guide.TID <= 0 || guideById.ContainsKey(guide.TID))
                continue;

            guideById.Add(guide.TID, guide);
        }

        Debug.Log($"[TutorialManager] JSON 튜토리얼 데이터 {guideById.Count}개 로드 완료", this);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        NotifySceneLoaded(scene.name);
    }

    private void NotifySceneLoaded(string sceneName)
    {
        TryOpenByCondition(TutorialConditionTypes.SceneLoaded, sceneName);
    }

    private void HandleItemBoxOpened(IInteractable interactable, int playerId)
    {
        if (interactable is ItemBox)
            NotifyEvent(TutorialEventNames.ItemBoxOpened);
    }

    private void HandleEnemyDead(int enemyId)
    {
        NotifyEvent(TutorialEventNames.EnemyDead);
    }

    private void HandleEscapeRequest(bool success)
    {
        NotifyEvent(success ? TutorialEventNames.EscapeSucceeded : TutorialEventNames.EscapeFailed);

        if (success)
            MarkTutorialCompleted();
    }

    private void HandleMainActiveSkillCasted()
    {
        NotifyEvent(TutorialEventNames.MainActiveSkillCasted);
    }

    private bool TryOpenByCondition(string conditionType, string conditionValue)
    {
        foreach (TutorialGuideData guide in guideById.Values)
        {
            if (openedGuideIds.Contains(guide.TID))
                continue;

            if (!IsConditionMatch(guide.OpenConditionType, guide.OpenConditionValue, conditionType, conditionValue))
                continue;

            RequestGuide(guide);
            return true;
        }

        return false;
    }

    private void RequestGuide(TutorialGuideData guide)
    {
        if (guide == null)
            return;

        if (openedGuideIds.Contains(guide.TID))
            return;

        if (isShowing)
        {
            pendingGuides.Enqueue(guide);
            return;
        }

        ShowGuide(guide);
    }

    private void TryShowPendingGuide()
    {
        if (isShowing || pendingGuides.Count == 0)
            return;

        ShowGuide(pendingGuides.Dequeue());
    }

    private bool ShowGuide(TutorialGuideData guide, Action completed = null)
    {
        if (guide == null || isShowing || popup == null)
            return false;

        openedGuideIds.Add(guide.TID);
        currentGuide = guide;
        currentCompletion = completed;
        isShowing = true;

        if (guide.PauseGame)
            PauseGameplay();

        popup.Show(guide, HandlePopupConfirmed);
        StartDurationClearIfNeeded(guide);
        Debug.Log($"[TutorialManager] 안내 출력: {guide.TID} / UIHighlight={guide.UIHighlightPosition} / WorldHighlight={guide.HighlightEffectPosition}", this);
        return true;
    }

    private void HandlePopupConfirmed()
    {
        TryClearCurrent(TutorialConditionTypes.NextButton, string.Empty);
    }

    private bool TryClearCurrent(string conditionType, string conditionValue)
    {
        if (!isShowing || currentGuide == null)
            return false;

        if (!IsConditionMatch(currentGuide.ClearConditionType, currentGuide.ClearConditionValue, conditionType, conditionValue))
            return false;

        CompleteCurrentGuide();
        return true;
    }

    private void CompleteCurrentGuide()
    {
        if (!isShowing)
            return;

        TutorialGuideData completedGuide = currentGuide;
        isShowing = false;
        currentGuide = null;
        StopDurationRoutine();
        popup?.HideImmediate();
        RestoreGameplay();

        Action completion = currentCompletion;
        currentCompletion = null;
        completion?.Invoke();

        if (completedGuide == null)
            return;

        TryOpenByCondition(TutorialConditionTypes.PrevGuideClosed, completedGuide.TID.ToString());

        if (completedGuide.NextGuideID > 0 && guideById.TryGetValue(completedGuide.NextGuideID, out TutorialGuideData nextGuide))
            RequestGuide(nextGuide);
    }

    private void CompleteLegacyPopup()
    {
        if (!isShowing)
            return;

        isShowing = false;
        RestoreGameplay();
        popup?.HideImmediate();

        Action completion = currentCompletion;
        currentCompletion = null;
        completion?.Invoke();
    }

    private void StartDurationClearIfNeeded(TutorialGuideData guide)
    {
        StopDurationRoutine();

        if (!IsSameConditionType(guide.ClearConditionType, TutorialConditionTypes.Duration))
            return;

        float duration = guide.TutorialDuration > 0f ? guide.TutorialDuration : 3f;
        durationRoutine = StartCoroutine(DurationClearRoutine(duration));
    }

    private IEnumerator DurationClearRoutine(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        TryClearCurrent(TutorialConditionTypes.Duration, duration.ToString());
    }

    private void StopDurationRoutine()
    {
        if (durationRoutine == null)
            return;

        StopCoroutine(durationRoutine);
        durationRoutine = null;
    }

    private static bool IsConditionMatch(string expectedType, string expectedValue, string actualType, string actualValue)
    {
        if (!IsSameConditionType(expectedType, actualType))
            return false;

        if (string.IsNullOrWhiteSpace(expectedValue) || expectedValue == "-" || expectedValue == "*")
            return true;

        return string.Equals(expectedValue.Trim(), actualValue?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSameConditionType(string a, string b)
    {
        return string.Equals(NormalizeConditionType(a), NormalizeConditionType(b), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeConditionType(string conditionType)
    {
        if (string.IsNullOrWhiteSpace(conditionType))
            return TutorialConditionTypes.None;

        string normalized = conditionType.Trim();

        // 엑셀 원본에서 사용하는 약식 조건명도 내부 조건명과 동일하게 처리합니다.
        if (string.Equals(normalized, "prevGuideEnd", StringComparison.OrdinalIgnoreCase))
            return TutorialConditionTypes.PrevGuideClosed;
        if (string.Equals(normalized, "triggerEnter", StringComparison.OrdinalIgnoreCase))
            return TutorialConditionTypes.TriggerEnter;
        if (string.Equals(normalized, "sceneLoaded", StringComparison.OrdinalIgnoreCase))
            return TutorialConditionTypes.SceneLoaded;
        if (string.Equals(normalized, "nextButton", StringComparison.OrdinalIgnoreCase))
            return TutorialConditionTypes.NextButton;
        if (string.Equals(normalized, "event", StringComparison.OrdinalIgnoreCase))
            return TutorialConditionTypes.Event;
        if (string.Equals(normalized, "duration", StringComparison.OrdinalIgnoreCase))
            return TutorialConditionTypes.Duration;

        return normalized;
    }

    private void PauseGameplay()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        pausedByTutorial = true;

        inputReader = FindFirstObjectByType<LocalInputReader>();
        inputReader?.SwitchToUIMap();
    }

    private void RestoreGameplay()
    {
        if (!pausedByTutorial)
            return;

        Time.timeScale = previousTimeScale;
        pausedByTutorial = false;

        if (inputReader != null)
            inputReader.SwitchToPlayerMap();
        inputReader = null;
    }

    private void MarkTutorialCompleted()
    {
        if (tutorialCompletionSaved)
            return;

        tutorialCompletionSaved = true;

        PlayerSaveDataSO saveDataSO = PlayerSaveDataSO.Instance;
        PlayerSaveData saveData = saveDataSO != null
            ? saveDataSO.LoadSaveData()
            : new PlayerSaveData();

        saveData.isTutorialCompleted = true;
        saveDataSO?.SaveGameData(saveData);

        // 튜토리얼 완료 여부만 저장하고, 로비 이동은 인게임과 동일하게 ResultUI의 버튼 흐름에 맡깁니다.
    }

    private void OnDestroy()
    {
        RestoreGameplay();
        if (Instance == this)
            Instance = null;
    }
}
