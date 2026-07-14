using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하나의 공통 패널에 튜토리얼 문구를 교체하여 표시합니다.
/// </summary>
public sealed class TutorialPopup : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private Button confirmButton;

    private Action onConfirmed;

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(Confirm);
    }

    public void Show(TutorialMessageEntry entry, Action confirmed)
    {
        if (entry == null)
            return;

        onConfirmed = confirmed;

        if (titleText != null)
            titleText.text = entry.Title;
        if (messageText != null)
            messageText.text = entry.Message;
        if (confirmText != null)
            confirmText.text = entry.ConfirmText;

        gameObject.SetActive(true);
        SetCanvasState(true);
    }

    public void Confirm()
    {
        if (!gameObject.activeSelf)
            return;

        Action callback = onConfirmed;
        HideImmediate();
        callback?.Invoke();
    }

    public void HideImmediate()
    {
        onConfirmed = null;
        SetCanvasState(false);
        gameObject.SetActive(false);
    }

    private void SetCanvasState(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(Confirm);
    }
}
