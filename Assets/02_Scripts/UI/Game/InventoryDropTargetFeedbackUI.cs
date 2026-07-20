using UnityEngine;

/// <summary>
/// 슬롯 하위 Canvas 오브젝트로 구성한 밝기/테두리 피드백을 표시합니다.
/// 색상, 이미지, 두께와 배치는 프리팹에서 설정하고 이 스크립트는 알파 전환만 담당합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class InventoryDropTargetFeedbackUI : MonoBehaviour
{
    [Header("드롭 대상 종류")]
    [SerializeField] private InventoryDropTargetType targetType;

    [Header("Canvas 시각 요소")]
    [SerializeField] private CanvasGroup visualGroup;

    [Header("표시 전환")]
    [SerializeField, Range(0f, 1f)] private float pulseMinimumAlpha = 0.55f;
    [SerializeField, Range(0f, 1f)] private float pulseMaximumAlpha = 1f;
    [SerializeField, Min(0f)] private float pulseSpeed = 1.25f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.12f;

    private bool isAvailable;

    public InventoryDropTargetType TargetType => targetType;

    private void Awake()
    {
        ResolveReferences();
        HideImmediate();
    }

    private void OnDisable()
    {
        isAvailable = false;
        HideImmediate();
    }

    private void Update()
    {
        if (visualGroup == null)
            return;

        float targetAlpha = 0f;
        if (isAvailable)
        {
            float wave = (Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            targetAlpha = Mathf.Lerp(pulseMinimumAlpha, pulseMaximumAlpha, wave);
        }

        if (fadeDuration <= 0f)
        {
            visualGroup.alpha = targetAlpha;
            return;
        }

        float fadeSpeed = 1f / fadeDuration;
        visualGroup.alpha = Mathf.MoveTowards(
            visualGroup.alpha,
            targetAlpha,
            Time.unscaledDeltaTime * fadeSpeed);
    }

    /// <summary>
    /// 현재 드래그한 아이템을 이 슬롯에 배치할 수 있는지에 따라 표시 상태를 변경합니다.
    /// </summary>
    public void SetAvailable(bool available)
    {
        ResolveReferences();
        isAvailable = available;
    }

    /// <summary>
    /// 인벤토리 UI가 닫히거나 드래그가 강제 취소될 때 잔상을 즉시 제거합니다.
    /// </summary>
    public void HideImmediate()
    {
        isAvailable = false;

        if (visualGroup != null)
            visualGroup.alpha = 0f;
    }

    private void ResolveReferences()
    {
        if (visualGroup == null)
            visualGroup = GetComponent<CanvasGroup>();

        if (visualGroup == null)
            return;

        visualGroup.interactable = false;
        visualGroup.blocksRaycasts = false;
    }

    private void OnValidate()
    {
        pulseMinimumAlpha = Mathf.Clamp01(pulseMinimumAlpha);
        pulseMaximumAlpha = Mathf.Clamp(pulseMaximumAlpha, pulseMinimumAlpha, 1f);
        pulseSpeed = Mathf.Max(0f, pulseSpeed);
        fadeDuration = Mathf.Max(0f, fadeDuration);
        ResolveReferences();
    }
}
