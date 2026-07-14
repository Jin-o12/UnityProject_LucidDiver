using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 튜토리얼 메시지 조회, 팝업 중복 방지, 입력 전환과 일시정지를 중앙에서 관리합니다.
/// </summary>
public sealed class TutorialManager : MonoBehaviour
{
    private const string CatalogResourcePath = "Tutorial/TutorialMessageCatalog";

    public static TutorialManager Instance { get; private set; }

    [SerializeField] private TutorialMessageCatalog catalog;
    [SerializeField] private TutorialPopup popup;

    private Action currentCompletion;
    private LocalInputReader inputReader;
    private float previousTimeScale = 1f;
    private bool isShowing;
    private bool pausedByTutorial;

    public bool IsShowing => isShowing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        catalog ??= Resources.Load<TutorialMessageCatalog>(CatalogResourcePath);
        popup ??= GetComponentInChildren<TutorialPopup>(true);
        popup?.HideImmediate();
    }

    private void Update()
    {
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
    /// ID에 해당하는 팝업을 표시합니다. 이미 팝업이 열려 있으면 새 요청을 받지 않습니다.
    /// </summary>
    public bool Show(string tutorialId, Action completed = null)
    {
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

        popup.Show(entry, CompleteCurrent);
        return true;
    }

    private void CompleteCurrent()
    {
        if (!isShowing)
            return;

        isShowing = false;
        RestoreGameplay();

        Action completion = currentCompletion;
        currentCompletion = null;
        completion?.Invoke();
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

    private void OnDisable()
    {
        if (!isShowing)
            return;

        popup?.HideImmediate();
        isShowing = false;
        currentCompletion = null;
        RestoreGameplay();
    }

    private void OnDestroy()
    {
        RestoreGameplay();
        if (Instance == this)
            Instance = null;
    }
}
