using UnityEngine;

/// <summary>
/// 바닥에 떨어져 있는 아이템 오브젝트.
/// 플레이어가 상호작용하면 실제로 주울 수 있는 수량만 인벤토리에 반영한다.
/// </summary>
public class DropItem : MonoBehaviour, IInteractable
{
    public ItemData itemData;   // 드랍 아이템이 가지고 있는 실제 아이템 데이터
    public int stackCount;      // 바닥에 남아 있는 수량

    public bool Interact(int playerID)
    {
        if (itemData == null || stackCount <= 0)
        {
            Debug.LogWarning("DropItem: 유효하지 않은 드랍 아이템 데이터입니다.");
            return false;
        }

        if (!TryFindPicker(playerID, out MonoBehaviour pickupHandler))
        {
            Debug.LogWarning($"DropItem: playerID={playerID} 에 해당하는 픽업 처리 대상을 찾지 못했습니다.");
            return false;
        }

        int remain = InvokePickupHandler(pickupHandler);
        if (remain >= stackCount)
        {
            Debug.Log("인벤토리가 가득 차서 아이템을 주울 수 없습니다.");
            return false;
        }

        stackCount = remain;

        // 전부 주웠으면 월드 아이템을 제거한다.
        if (stackCount <= 0)
        {
            Destroy(gameObject);
            return true;
        }

        // 일부만 주운 경우에는 바닥에 남겨 둔다.
        return false;
    }

    /// <summary>
    /// 상호작용한 플레이어 ID와 일치하는 픽업 처리 대상을 찾는다.
    /// </summary>
    private bool TryFindPicker(int playerID, out MonoBehaviour pickupHandler)
    {
        EntityIdentity[] identities = FindObjectsOfType<EntityIdentity>();
        pickupHandler = null;

        for (int i = 0; i < identities.Length; i++)
        {
            if (identities[i] == null || identities[i].entityID != playerID)
                continue;

            pickupHandler = identities[i].GetComponent("InventoryPresenter") as MonoBehaviour;
            return pickupHandler != null;
        }

        return false;
    }

    /// <summary>
    /// PresenterAD를 직접 참조하지 않고 TryPickUpItem 메서드를 호출한다.
    /// </summary>
    private int InvokePickupHandler(MonoBehaviour pickupHandler)
    {
        if (pickupHandler == null)
            return stackCount;

        var pickupMethod = pickupHandler.GetType().GetMethod("TryPickUpItem");
        if (pickupMethod == null)
        {
            Debug.LogWarning("DropItem: TryPickUpItem 메서드를 찾지 못했습니다.");
            return stackCount;
        }

        object result = pickupMethod.Invoke(pickupHandler, new object[] { itemData, stackCount });
        if (result is int remain)
            return remain;

        Debug.LogWarning("DropItem: TryPickUpItem 반환값을 처리하지 못했습니다.");
        return stackCount;
    }
}
