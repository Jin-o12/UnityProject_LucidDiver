using UnityEngine;

/// <summary>
/// 인벤토리 아이템 드래그 중 유효한 퀵슬롯과 아티팩트 슬롯의 피드백을 한곳에서 제어합니다.
/// 실제 색상과 테두리 이미지는 Canvas 프리팹에서 설정하고 이 컴포넌트는 표시 여부만 결정합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class InventoryDragFeedbackController : MonoBehaviour
{
    [Header("소모품 장착 대상")]
    [SerializeField] private InventoryDropTargetFeedbackUI[] quickSlotTargets;

    [Header("아티팩트 장착 대상")]
    [SerializeField] private InventoryDropTargetFeedbackUI[] artifactSlotTargets;

    private void Awake()
    {
        HideAll(true);
    }

    private void OnDisable()
    {
        HideAll(true);
    }

    /// <summary>
    /// 드래그를 시작한 인벤토리 슬롯의 아이템을 기준으로 유효한 목적지만 표시합니다.
    /// </summary>
    public void ShowFor(InventorySlotUI sourceSlot)
    {
        if (sourceSlot == null)
        {
            HideAll();
            return;
        }

        ItemData itemData = sourceSlot.CurrentItemData;
        bool canUseQuickSlot = sourceSlot.CanAssignToQuickSlot
            && InventoryItemPlacementPolicy.CanPlace(
                itemData,
                sourceSlot.CurrentItemTid,
                InventoryDropTargetType.QuickSlot);
        bool canUseArtifactSlot = InventoryItemPlacementPolicy.CanPlace(
            itemData,
            InventoryDropTargetType.ArtifactSlot);

        SetTargets(quickSlotTargets, canUseQuickSlot);
        SetTargets(artifactSlotTargets, canUseArtifactSlot);
    }

    /// <summary>
    /// 모든 장착 대상 피드백을 숨깁니다. 강제 초기화 시에는 페이드 없이 즉시 제거합니다.
    /// </summary>
    public void HideAll(bool immediate = false)
    {
        HideTargets(quickSlotTargets, immediate);
        HideTargets(artifactSlotTargets, immediate);
    }

    private static void SetTargets(InventoryDropTargetFeedbackUI[] targets, bool available)
    {
        if (targets == null)
            return;

        foreach (InventoryDropTargetFeedbackUI target in targets)
        {
            if (target != null)
                target.SetAvailable(available);
        }
    }

    private static void HideTargets(InventoryDropTargetFeedbackUI[] targets, bool immediate)
    {
        if (targets == null)
            return;

        foreach (InventoryDropTargetFeedbackUI target in targets)
        {
            if (target == null)
                continue;

            if (immediate)
                target.HideImmediate();
            else
                target.SetAvailable(false);
        }
    }
}
