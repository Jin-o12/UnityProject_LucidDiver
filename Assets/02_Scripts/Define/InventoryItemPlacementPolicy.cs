/// <summary>
/// 인벤토리 아이템을 배치할 수 있는 장착 대상 종류입니다.
/// UI 표시와 실제 데이터 처리에서 같은 값을 사용합니다.
/// </summary>
public enum InventoryDropTargetType
{
    QuickSlot = 0,
    ArtifactSlot = 1,
    InventorySlot = 2,
    ChestSlot = 3
}

/// <summary>
/// 아이템 데이터와 장착 대상의 조합이 유효한지 판단하는 공통 규칙입니다.
/// 드롭 하이라이트와 실제 장착 로직이 서로 다른 결과를 내지 않도록 한곳에서 관리합니다.
/// </summary>
public static class InventoryItemPlacementPolicy
{
    public static bool CanPlace(ItemData itemData, InventoryDropTargetType targetType)
    {
        if (itemData == null)
            return false;

        return targetType switch
        {
            InventoryDropTargetType.QuickSlot =>
                itemData is ConsumeItemData && itemData.category == itemCategory.consume,
            InventoryDropTargetType.ArtifactSlot =>
                itemData is ArtifactItemData && itemData.category == itemCategory.artifact,
            _ => false
        };
    }

    /// <summary>
    /// 이전 저장 데이터처럼 ItemData 참조가 비어 있는 퀵슬롯 후보는 기존 TID 규칙으로만 보완합니다.
    /// 아티팩트는 실제 장착에 ArtifactItemData가 필요하므로 TID만으로 허용하지 않습니다.
    /// </summary>
    public static bool CanPlace(ItemData itemData, int tid, InventoryDropTargetType targetType)
    {
        if (itemData != null)
            return CanPlace(itemData, targetType);

        return targetType == InventoryDropTargetType.QuickSlot && tid > 300 && tid < 400;
    }
}
