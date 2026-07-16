using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 데이터에 따라 가이드, 관제사 무전, 유안 대사, 목표 안내 패널 중 하나를 표시합니다.
/// </summary>
public sealed class TutorialPopup : MonoBehaviour
{
    [Serializable]
    private sealed class TutorialPanelView
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Text")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text confirmText;
        [SerializeField] private Button confirmButton;

        [Header("Background")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Color fallbackBackgroundColor = new(0.055f, 0.035f, 0.12f, 0.94f);
        [SerializeField] private Color spriteTintColor = Color.white;

        [Header("Text Color")]
        [SerializeField] private Color titleColor = Color.white;
        [SerializeField] private Color messageColor = Color.white;

        [Header("Portrait")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private Sprite portraitSprite;
        [SerializeField] private Color portraitTintColor = Color.white;

        public GameObject Root => root;
        public TMP_Text TitleText => titleText;
        public TMP_Text MessageText => messageText;
        public TMP_Text ConfirmText => confirmText;
        public Button ConfirmButton => confirmButton;
        public Image BackgroundImage => backgroundImage;
        public Sprite BackgroundSprite => backgroundSprite;
        public Color FallbackBackgroundColor => fallbackBackgroundColor;
        public Color SpriteTintColor => spriteTintColor;
        public Color TitleColor => titleColor;
        public Color MessageColor => messageColor;
        public Image PortraitImage => portraitImage;
        public Sprite PortraitSprite => portraitSprite;
        public Color PortraitTintColor => portraitTintColor;

        public bool HasAnyReference =>
            root != null ||
            titleText != null ||
            messageText != null ||
            confirmText != null ||
            confirmButton != null ||
            backgroundImage != null ||
            portraitImage != null;

        public void SetRootVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
        }
    }

    [Header("Legacy Common Popup")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private Button confirmButton;

    [Header("Separated Panels")]
    [SerializeField] private TutorialPanelView guidePanel = new();
    [SerializeField] private TutorialPanelView operatorRadioPanel = new();
    [SerializeField] private TutorialPanelView yuanDialoguePanel = new();
    [SerializeField] private TutorialPanelView objectivePanel = new();

    [Header("Legacy Mode Background Images")]
    [SerializeField] private Image guideBackgroundImage;
    [SerializeField] private Image operatorRadioBackgroundImage;
    [SerializeField] private Image yuanDialogueBackgroundImage;
    [SerializeField] private Image objectiveBackgroundImage;

    [Header("Legacy Mode Background Sprites")]
    [SerializeField] private Sprite guideBackgroundSprite;
    [SerializeField] private Sprite operatorRadioBackgroundSprite;
    [SerializeField] private Sprite yuanDialogueBackgroundSprite;
    [SerializeField] private Sprite objectiveBackgroundSprite;

    [Header("Legacy Yuan Portrait")]
    [SerializeField] private Image yuanPortraitImage;
    [SerializeField] private Sprite yuanPortraitSprite;
    [SerializeField] private Color yuanPortraitTintColor = Color.white;

    [Header("Legacy Fallback Background Colors")]
    [SerializeField] private Color guideBackgroundColor = new(0.055f, 0.035f, 0.12f, 0.94f);
    [SerializeField] private Color yuanDialogueBackgroundColor = new(0.02f, 0.08f, 0.07f, 0.94f);
    [SerializeField] private Color operatorRadioBackgroundColor = new(0.02f, 0.16f, 0.11f, 0.94f);
    [SerializeField] private Color objectiveBackgroundColor = new(0.12f, 0.075f, 0.02f, 0.94f);

    [Header("Legacy Sprite Tint Colors")]
    [SerializeField] private Color guideSpriteTintColor = Color.white;
    [SerializeField] private Color yuanDialogueSpriteTintColor = Color.white;
    [SerializeField] private Color operatorRadioSpriteTintColor = Color.white;
    [SerializeField] private Color objectiveSpriteTintColor = Color.white;

    [Header("Legacy Title Colors")]
    [SerializeField] private Color guideTitleColor = new(0.72f, 0.58f, 1f, 1f);
    [SerializeField] private Color yuanDialogueTitleColor = new(0.75f, 1f, 0.85f, 1f);
    [SerializeField] private Color operatorRadioTitleColor = new(0.65f, 1f, 0.72f, 1f);
    [SerializeField] private Color objectiveTitleColor = new(1f, 0.72f, 0.32f, 1f);

    [Header("Legacy Message Colors")]
    [SerializeField] private Color guideMessageColor = Color.white;
    [SerializeField] private Color yuanDialogueMessageColor = Color.white;
    [SerializeField] private Color operatorRadioMessageColor = Color.white;
    [SerializeField] private Color objectiveMessageColor = Color.white;

    private enum PopupMode
    {
        Guide,
        OperatorRadio,
        YuanDialogue,
        Objective
    }

    private Action onConfirmed;
    private RectTransform popupRect;
    private RectTransform titleRect;
    private RectTransform messageRect;
    private RectTransform confirmButtonRect;
    private Image backgroundImage;
    private Image activeBackgroundImage;
    private Sprite rootBackgroundSprite;
    private TutorialPanelView activePanel;
    private Button operatorRadioRootButton;

    private void Awake()
    {
        CacheComponents();
        RegisterConfirmButton(confirmButton);
        RegisterConfirmButton(guidePanel.ConfirmButton);
        RegisterConfirmButton(operatorRadioPanel.ConfirmButton);
        RegisterConfirmButton(yuanDialoguePanel.ConfirmButton);
        RegisterConfirmButton(objectivePanel.ConfirmButton);
        RegisterOperatorRadioPanelButton();
    }

    public void Show(TutorialMessageEntry entry, Action confirmed)
    {
        if (entry == null)
            return;

        onConfirmed = confirmed;
        ApplyGuideStyle();
        ApplyText(entry.Title, entry.Message, entry.ConfirmText);

        gameObject.SetActive(true);
        SetCanvasState(true);
    }

    public void Show(TutorialGuideData guide, Action confirmed)
    {
        if (guide == null)
            return;

        onConfirmed = confirmed;
        ApplyGuideStyle(guide);
        ApplyText(guide.ResolvedTitle, guide.Message, IsOperatorDialogue(guide) ? string.Empty : guide.ResolvedConfirmText);

        gameObject.SetActive(true);
        SetCanvasState(true);
    }

    public void Confirm()
    {
        if (!gameObject.activeSelf)
            return;

        Action callback = onConfirmed;
        callback?.Invoke();
    }

    public void HideImmediate()
    {
        onConfirmed = null;
        HideSeparatedPanels();
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

    private void CacheComponents()
    {
        popupRect = transform as RectTransform;
        backgroundImage = GetComponent<Image>();
        rootBackgroundSprite = backgroundImage != null ? backgroundImage.sprite : null;
        titleRect = titleText != null ? titleText.transform as RectTransform : null;
        messageRect = messageText != null ? messageText.transform as RectTransform : null;
        confirmButtonRect = confirmButton != null ? confirmButton.transform as RectTransform : null;
    }

    private void ApplyGuideStyle(TutorialGuideData guide)
    {
        bool isDialogue = IsContentType(guide.ContentType, "Dialogue");
        bool isObjective = IsContentType(guide.ContentType, "Objective") ||
                           IsConditionType(guide.ClearConditionType, TutorialConditionTypes.Event);

        if (isObjective)
        {
            ApplyObjectiveStyle();
            return;
        }

        if (isDialogue)
        {
            if (IsSpeaker(guide.Speaker, "Operator"))
            {
                ApplyOperatorRadioStyle();
                return;
            }

            ApplyDialogueStyle();
            return;
        }

        ApplyGuideStyle();
    }

    private void ApplyGuideStyle()
    {
        SelectPanel(PopupMode.Guide);
        SetOperatorRadioPanelClickable(false);
        SetPortrait(false);
        SetPopupRect(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(920f, 230f), new Vector2(0.5f, 1f));
        SetTitleRect(new Vector2(0f, 0.68f), new Vector2(1f, 1f), new Vector2(0f, -11f), new Vector2(-56f, -2f), TextAlignmentOptions.Center, 36f, GetTitleColor(PopupMode.Guide));
        SetMessageRect(new Vector2(0f, 0.28f), new Vector2(1f, 0.68f), Vector2.zero, new Vector2(-84f, 0f), TextAlignmentOptions.Center, 27f, GetMessageColor(PopupMode.Guide));
        SetConfirmButton(true, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(180f, 52f), new Vector2(0.5f, 0f));
        SetBackground(PopupMode.Guide, false);
    }

    private void ApplyDialogueStyle()
    {
        SelectPanel(PopupMode.YuanDialogue);
        SetOperatorRadioPanelClickable(false);
        SetPortrait(true);
        SetPopupRect(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(1180f, 260f), new Vector2(0.5f, 0f));
        SetTitleRect(new Vector2(0f, 0.72f), new Vector2(0f, 1f), new Vector2(138f, -14f), new Vector2(240f, -16f), TextAlignmentOptions.Center, 34f, GetTitleColor(PopupMode.YuanDialogue));
        SetMessageRect(new Vector2(0f, 0.28f), new Vector2(1f, 0.74f), new Vector2(0f, -4f), new Vector2(-160f, -8f), TextAlignmentOptions.Left, 34f, GetMessageColor(PopupMode.YuanDialogue));
        SetConfirmButton(false, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-136f, 28f), new Vector2(200f, 54f), new Vector2(1f, 0f));
        SetBackground(PopupMode.YuanDialogue, true);
    }

    private void ApplyOperatorRadioStyle()
    {
        // 관제사 무전은 별도 버튼 대신 패널 전체를 눌러 진행합니다.
        SelectPanel(PopupMode.OperatorRadio);
        SetOperatorRadioPanelClickable(true);
        SetPortrait(false);
        SetPopupRect(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(980f, 170f), new Vector2(0.5f, 0.5f));
        SetTitleRect(new Vector2(0f, 0.64f), new Vector2(0f, 1f), new Vector2(124f, -8f), new Vector2(220f, -8f), TextAlignmentOptions.Center, 30f, GetTitleColor(PopupMode.OperatorRadio));
        SetMessageRect(new Vector2(0f, 0.16f), new Vector2(1f, 0.72f), new Vector2(0f, 0f), new Vector2(-120f, -6f), TextAlignmentOptions.Center, 34f, GetMessageColor(PopupMode.OperatorRadio));
        SetConfirmButton(false, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-80f, 18f), new Vector2(170f, 46f), new Vector2(1f, 0f));
        SetBackground(PopupMode.OperatorRadio, false);
    }

    private void ApplyObjectiveStyle()
    {
        SelectPanel(PopupMode.Objective);
        SetOperatorRadioPanelClickable(false);
        SetPortrait(false);
        SetPopupRect(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(960f, 178f), new Vector2(0.5f, 1f));
        SetTitleRect(new Vector2(0f, 0.58f), new Vector2(1f, 1f), new Vector2(0f, -6f), new Vector2(-56f, -2f), TextAlignmentOptions.Center, 34f, GetTitleColor(PopupMode.Objective));
        SetMessageRect(new Vector2(0f, 0.08f), new Vector2(1f, 0.62f), new Vector2(0f, 0f), new Vector2(-96f, -6f), TextAlignmentOptions.Center, 27f, GetMessageColor(PopupMode.Objective));
        SetConfirmButton(false, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(180f, 52f), new Vector2(0.5f, 0f));
        SetBackground(PopupMode.Objective, false);
    }

    private void SelectPanel(PopupMode mode)
    {
        SetOperatorRadioPanelClickable(false);
        HideSeparatedPanels();
        activePanel = GetPanel(mode);

        if (activePanel != null)
            activePanel.SetRootVisible(true);
    }

    private void HideSeparatedPanels()
    {
        guidePanel.SetRootVisible(false);
        operatorRadioPanel.SetRootVisible(false);
        yuanDialoguePanel.SetRootVisible(false);
        objectivePanel.SetRootVisible(false);
    }

    private void ApplyText(string title, string message, string confirm)
    {
        TMP_Text targetTitle = GetActiveTitleText();
        TMP_Text targetMessage = GetActiveMessageText();
        TMP_Text targetConfirm = GetActiveConfirmText();

        if (targetTitle != null)
            targetTitle.text = title;
        if (targetMessage != null)
            targetMessage.text = message;
        if (targetConfirm != null)
            targetConfirm.text = confirm;
    }

    private void SetPopupRect(Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
    {
        if (popupRect == null)
            return;

        popupRect.anchorMin = anchorMin;
        popupRect.anchorMax = anchorMax;
        popupRect.anchoredPosition = anchoredPosition;
        popupRect.sizeDelta = sizeDelta;
        popupRect.pivot = pivot;
    }

    private void SetTitleRect(Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, TextAlignmentOptions alignment, float fontSize, Color color)
    {
        TMP_Text targetTitle = GetActiveTitleText();
        RectTransform targetTitleRect = targetTitle != null ? targetTitle.transform as RectTransform : titleRect;

        if (targetTitleRect != null)
        {
            targetTitleRect.anchorMin = anchorMin;
            targetTitleRect.anchorMax = anchorMax;
            targetTitleRect.anchoredPosition = anchoredPosition;
            targetTitleRect.sizeDelta = sizeDelta;
        }

        if (targetTitle == null)
            return;

        targetTitle.alignment = alignment;
        targetTitle.fontSize = fontSize;
        targetTitle.color = color;
    }

    private void SetMessageRect(Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, TextAlignmentOptions alignment, float fontSize, Color color)
    {
        TMP_Text targetMessage = GetActiveMessageText();
        RectTransform targetMessageRect = targetMessage != null ? targetMessage.transform as RectTransform : messageRect;

        if (targetMessageRect != null)
        {
            targetMessageRect.anchorMin = anchorMin;
            targetMessageRect.anchorMax = anchorMax;
            targetMessageRect.anchoredPosition = anchoredPosition;
            targetMessageRect.sizeDelta = sizeDelta;
        }

        if (targetMessage == null)
            return;

        targetMessage.alignment = alignment;
        targetMessage.fontSize = fontSize;
        targetMessage.color = color;
    }

    private void SetConfirmButton(bool visible, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
    {
        Button targetButton = GetActiveConfirmButton();

        if (targetButton != null)
        {
            targetButton.gameObject.SetActive(visible);
            targetButton.interactable = visible;
        }

        if (!visible)
            return;

        RectTransform targetButtonRect = targetButton != null ? targetButton.transform as RectTransform : confirmButtonRect;

        if (targetButtonRect == null)
            return;

        targetButtonRect.anchorMin = anchorMin;
        targetButtonRect.anchorMax = anchorMax;
        targetButtonRect.anchoredPosition = anchoredPosition;
        targetButtonRect.sizeDelta = sizeDelta;
        targetButtonRect.pivot = pivot;
    }

    private void SetBackground(PopupMode mode, bool allowRootSpriteFallback)
    {
        HideBackgroundImages();

        Image modeBackgroundImage = GetBackgroundImage(mode);
        Sprite modeBackgroundSprite = GetBackgroundSprite(mode);
        Color fallbackColor = GetBackgroundColor(mode);
        Color spriteTintColor = GetSpriteTintColor(mode);

        activeBackgroundImage = modeBackgroundImage != null ? modeBackgroundImage : backgroundImage;

        if (activeBackgroundImage == null)
            return;

        activeBackgroundImage.enabled = true;

        if (modeBackgroundSprite != null)
        {
            activeBackgroundImage.sprite = modeBackgroundSprite;
        }
        else if (activeBackgroundImage == backgroundImage)
        {
            // 루트 Image에 직접 넣은 텍스트 박스 스프라이트는 유안 대사 모드에서만 재사용합니다.
            // 툴팁/무전/목표 모드는 별도 이미지가 없으면 색상 배경으로만 표시합니다.
            activeBackgroundImage.sprite = allowRootSpriteFallback ? rootBackgroundSprite : null;
        }

        // UI 프레임 스프라이트가 들어간 경우에는 모드별 tint 색으로 표시합니다.
        // 기본값은 흰색이라 원본 이미지 색을 그대로 유지합니다.
        if (activeBackgroundImage.sprite != null)
        {
            activeBackgroundImage.color = spriteTintColor;
            activeBackgroundImage.type = Image.Type.Sliced;
            return;
        }

        activeBackgroundImage.color = fallbackColor;
    }

    private void SetPortrait(bool visible)
    {
        Image targetPortrait = GetActivePortraitImage();

        if (targetPortrait == null)
            return;

        targetPortrait.enabled = visible;

        if (!visible)
            return;

        Sprite portraitSprite = GetActivePortraitSprite();
        if (portraitSprite != null)
            targetPortrait.sprite = portraitSprite;

        targetPortrait.color = GetActivePortraitTintColor();
        targetPortrait.preserveAspect = true;
    }

    private void HideBackgroundImages()
    {
        SetBackgroundImageVisible(guideBackgroundImage, false);
        SetBackgroundImageVisible(operatorRadioBackgroundImage, false);
        SetBackgroundImageVisible(yuanDialogueBackgroundImage, false);
        SetBackgroundImageVisible(objectiveBackgroundImage, false);
        SetBackgroundImageVisible(guidePanel.BackgroundImage, false);
        SetBackgroundImageVisible(operatorRadioPanel.BackgroundImage, false);
        SetBackgroundImageVisible(yuanDialoguePanel.BackgroundImage, false);
        SetBackgroundImageVisible(objectivePanel.BackgroundImage, false);
        SetBackgroundImageVisible(backgroundImage, false);
    }

    private void SetBackgroundImageVisible(Image targetImage, bool visible)
    {
        if (targetImage == null)
            return;

        targetImage.enabled = visible;
    }

    private TutorialPanelView GetPanel(PopupMode mode)
    {
        return mode switch
        {
            PopupMode.Guide => guidePanel,
            PopupMode.OperatorRadio => operatorRadioPanel,
            PopupMode.YuanDialogue => yuanDialoguePanel,
            PopupMode.Objective => objectivePanel,
            _ => guidePanel
        };
    }

    private TMP_Text GetActiveTitleText()
    {
        return activePanel != null && activePanel.TitleText != null ? activePanel.TitleText : titleText;
    }

    private TMP_Text GetActiveMessageText()
    {
        return activePanel != null && activePanel.MessageText != null ? activePanel.MessageText : messageText;
    }

    private TMP_Text GetActiveConfirmText()
    {
        return activePanel != null && activePanel.ConfirmText != null ? activePanel.ConfirmText : confirmText;
    }

    private Button GetActiveConfirmButton()
    {
        return activePanel != null && activePanel.ConfirmButton != null ? activePanel.ConfirmButton : confirmButton;
    }

    private Image GetActivePortraitImage()
    {
        return activePanel != null && activePanel.PortraitImage != null ? activePanel.PortraitImage : yuanPortraitImage;
    }

    private Sprite GetActivePortraitSprite()
    {
        return activePanel != null && activePanel.PortraitSprite != null ? activePanel.PortraitSprite : yuanPortraitSprite;
    }

    private Color GetActivePortraitTintColor()
    {
        return activePanel != null && activePanel.PortraitImage != null ? activePanel.PortraitTintColor : yuanPortraitTintColor;
    }

    private Image GetBackgroundImage(PopupMode mode)
    {
        TutorialPanelView panel = GetPanel(mode);

        if (panel != null && panel.BackgroundImage != null)
            return panel.BackgroundImage;

        return mode switch
        {
            PopupMode.Guide => guideBackgroundImage,
            PopupMode.OperatorRadio => operatorRadioBackgroundImage,
            PopupMode.YuanDialogue => yuanDialogueBackgroundImage,
            PopupMode.Objective => objectiveBackgroundImage,
            _ => guideBackgroundImage
        };
    }

    private Sprite GetBackgroundSprite(PopupMode mode)
    {
        TutorialPanelView panel = GetPanel(mode);

        if (panel != null && panel.BackgroundSprite != null)
            return panel.BackgroundSprite;

        return mode switch
        {
            PopupMode.Guide => guideBackgroundSprite,
            PopupMode.OperatorRadio => operatorRadioBackgroundSprite,
            PopupMode.YuanDialogue => yuanDialogueBackgroundSprite,
            PopupMode.Objective => objectiveBackgroundSprite,
            _ => guideBackgroundSprite
        };
    }

    private Color GetBackgroundColor(PopupMode mode)
    {
        TutorialPanelView panel = GetPanel(mode);

        if (panel != null && panel.HasAnyReference)
            return panel.FallbackBackgroundColor;

        return mode switch
        {
            PopupMode.Guide => guideBackgroundColor,
            PopupMode.OperatorRadio => operatorRadioBackgroundColor,
            PopupMode.YuanDialogue => yuanDialogueBackgroundColor,
            PopupMode.Objective => objectiveBackgroundColor,
            _ => guideBackgroundColor
        };
    }

    private Color GetSpriteTintColor(PopupMode mode)
    {
        TutorialPanelView panel = GetPanel(mode);

        if (panel != null && panel.HasAnyReference)
            return panel.SpriteTintColor;

        return mode switch
        {
            PopupMode.Guide => guideSpriteTintColor,
            PopupMode.OperatorRadio => operatorRadioSpriteTintColor,
            PopupMode.YuanDialogue => yuanDialogueSpriteTintColor,
            PopupMode.Objective => objectiveSpriteTintColor,
            _ => guideSpriteTintColor
        };
    }

    private Color GetTitleColor(PopupMode mode)
    {
        TutorialPanelView panel = GetPanel(mode);

        if (panel != null && panel.HasAnyReference)
            return panel.TitleColor;

        return mode switch
        {
            PopupMode.Guide => guideTitleColor,
            PopupMode.OperatorRadio => operatorRadioTitleColor,
            PopupMode.YuanDialogue => yuanDialogueTitleColor,
            PopupMode.Objective => objectiveTitleColor,
            _ => guideTitleColor
        };
    }

    private Color GetMessageColor(PopupMode mode)
    {
        TutorialPanelView panel = GetPanel(mode);

        if (panel != null && panel.HasAnyReference)
            return panel.MessageColor;

        return mode switch
        {
            PopupMode.Guide => guideMessageColor,
            PopupMode.OperatorRadio => operatorRadioMessageColor,
            PopupMode.YuanDialogue => yuanDialogueMessageColor,
            PopupMode.Objective => objectiveMessageColor,
            _ => guideMessageColor
        };
    }

    private void RegisterConfirmButton(Button targetButton)
    {
        if (targetButton != null)
            targetButton.onClick.AddListener(Confirm);
    }

    private void RegisterOperatorRadioPanelButton()
    {
        operatorRadioRootButton = EnsurePanelButton(operatorRadioPanel.Root);
        RegisterConfirmButton(operatorRadioRootButton);
        SetOperatorRadioPanelClickable(false);
    }

    private Button EnsurePanelButton(GameObject targetRoot)
    {
        if (targetRoot == null)
            return null;

        Button targetButton = targetRoot.GetComponent<Button>();
        if (targetButton == null)
            targetButton = targetRoot.AddComponent<Button>();

        targetButton.transition = Selectable.Transition.None;
        targetButton.targetGraphic = targetRoot.GetComponent<Graphic>();
        return targetButton;
    }

    private void SetOperatorRadioPanelClickable(bool clickable)
    {
        if (operatorRadioRootButton == null)
            return;

        operatorRadioRootButton.interactable = clickable;

        Graphic targetGraphic = operatorRadioRootButton.targetGraphic;
        if (targetGraphic != null)
            targetGraphic.raycastTarget = clickable;
    }

    private void UnregisterConfirmButton(Button targetButton)
    {
        if (targetButton != null)
            targetButton.onClick.RemoveListener(Confirm);
    }

    private static bool IsContentType(string current, string expected)
    {
        return string.Equals(current?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsConditionType(string current, string expected)
    {
        return string.Equals(current?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSpeaker(string current, string expected)
    {
        return string.Equals(current?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOperatorDialogue(TutorialGuideData guide)
    {
        return guide != null &&
               IsContentType(guide.ContentType, "Dialogue") &&
               IsSpeaker(guide.Speaker, "Operator");
    }

    private void OnDestroy()
    {
        UnregisterConfirmButton(confirmButton);
        UnregisterConfirmButton(guidePanel.ConfirmButton);
        UnregisterConfirmButton(operatorRadioPanel.ConfirmButton);
        UnregisterConfirmButton(yuanDialoguePanel.ConfirmButton);
        UnregisterConfirmButton(objectivePanel.ConfirmButton);
        UnregisterConfirmButton(operatorRadioRootButton);
    }
}
