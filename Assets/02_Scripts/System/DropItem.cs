using UnityEngine;

/// <summary>
/// 바닥에 떨어져 있는 아이템 오브젝트.
/// 플레이어가 상호작용하면 실제 아이템 획득 이벤트를 보낸다.
/// 실제 삭제 여부는 InventoryPresenter에서 판단한다.
/// </summary>
public class DropItem : MonoBehaviour, IInteractable
{
    public ItemData itemData;     // 바닥 아이템이 들고 있는 실제 아이템 데이터
    public int stackCount = 1;    // 바닥에 남아 있는 수량

    public bool Interact(int playerID)
    {
        // 획득 시도를 Presenter 쪽에 전달한다.
        // 이 시점에서는 아직 삭제하지 않는다.
        GlobalEventBus.OnItemPickedUp?.Invoke(playerID, itemData.TID, stackCount, this);

        // 아직 바닥 오브젝트를 제거할지 확정되지 않았으므로 false 반환.
        // 그래야 인벤토리가 꽉 찼을 때 같은 자리에서 다시 상호작용 가능하다.
        return false;
    }
}