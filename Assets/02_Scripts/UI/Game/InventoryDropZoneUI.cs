using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 인벤토리 아이템을 바닥에 버릴 때 사용하는 드롭존 UI이다.
/// 인벤토리 슬롯이 이 영역에 드롭되면 Presenter 쪽으로 버리기 요청을 전달한다.
/// </summary>
public class InventoryDropZoneUI : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null)
            return;

        if (!droppedObj.TryGetComponent<InventorySlotUI>(out var inventorySlot))
            return;

        GlobalEventBus.OnInventoryDropRequested?.Invoke(inventorySlot.slotIndex);
    }
}