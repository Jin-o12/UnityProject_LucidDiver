using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 튜토리얼 데이터의 하이라이트 ID와 실제 UI 또는 월드 오브젝트를 연결합니다.
/// 서로 다른 프리팹에 같은 ID를 사용할 수 있으며, 활성화된 대상만 하이라이트 계산에 참여합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class TutorialHighlightTarget : MonoBehaviour
{
    public enum TargetKind
    {
        UI,
        World
    }

    [Header("대상 식별")]
    [SerializeField] private string targetId;
    [SerializeField] private TargetKind targetKind = TargetKind.UI;

    [Header("UI 대상")]
    [SerializeField] private RectTransform uiTarget;
    [SerializeField] private Vector2 uiPadding = new(18f, 18f);

    [Header("월드 대상")]
    [SerializeField] private Transform worldTarget;
    [SerializeField] private Vector3 worldOffset = Vector3.up;
    [SerializeField] private Vector2 worldScreenSize = new(220f, 220f);
    [SerializeField] private Camera worldCamera;

    private static readonly HashSet<TutorialHighlightTarget> ActiveTargets = new();

    public static event Action ActiveTargetsChanged;

    public string TargetId => targetId?.Trim() ?? string.Empty;
    public TargetKind Kind => targetKind;
    public RectTransform UITarget => uiTarget != null ? uiTarget : transform as RectTransform;
    public Vector2 UIPadding => uiPadding;
    public Transform WorldTarget => worldTarget != null ? worldTarget : transform;
    public Vector3 WorldOffset => worldOffset;
    public Vector2 WorldScreenSize => worldScreenSize;
    public Camera WorldCamera => worldCamera != null ? worldCamera : Camera.main;

    private void Reset()
    {
        uiTarget = transform as RectTransform;
        targetKind = uiTarget != null ? TargetKind.UI : TargetKind.World;
        worldTarget = transform;
    }

    private void OnEnable()
    {
        ResolveMissingReferences();

        if (ActiveTargets.Add(this))
            ActiveTargetsChanged?.Invoke();
    }

    private void OnDisable()
    {
        if (ActiveTargets.Remove(this))
            ActiveTargetsChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (ActiveTargets.Remove(this))
            ActiveTargetsChanged?.Invoke();
    }

    private void OnValidate()
    {
        uiPadding.x = Mathf.Max(0f, uiPadding.x);
        uiPadding.y = Mathf.Max(0f, uiPadding.y);
        worldScreenSize.x = Mathf.Max(1f, worldScreenSize.x);
        worldScreenSize.y = Mathf.Max(1f, worldScreenSize.y);
        ResolveMissingReferences();
    }

    /// <summary>
    /// 현재 활성화된 대상 중 요청된 ID와 일치하는 대상을 수집합니다.
    /// </summary>
    public static void CollectActiveTargets(IReadOnlyCollection<string> requestedIds, List<TutorialHighlightTarget> results)
    {
        results.Clear();

        if (requestedIds == null || requestedIds.Count == 0)
            return;

        foreach (TutorialHighlightTarget target in ActiveTargets)
        {
            if (target == null || !target.isActiveAndEnabled || string.IsNullOrWhiteSpace(target.TargetId))
                continue;

            foreach (string requestedId in requestedIds)
            {
                if (!string.Equals(target.TargetId, requestedId, StringComparison.OrdinalIgnoreCase))
                    continue;

                results.Add(target);
                break;
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry()
    {
        ActiveTargets.Clear();
        ActiveTargetsChanged = null;
    }

    private void ResolveMissingReferences()
    {
        if (uiTarget == null)
            uiTarget = transform as RectTransform;

        if (worldTarget == null)
            worldTarget = transform;
    }
}
