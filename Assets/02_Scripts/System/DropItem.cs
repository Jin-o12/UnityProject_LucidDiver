using UnityEngine;

/// <summary>
/// 바닥에 떨어져 있는 아이템 오브젝트.
/// 플레이어가 상호작용하면 실제 아이템 획득 이벤트를 보낸다.
/// </summary>
public class DropItem : MonoBehaviour, IInteractable
{
    public ItemData itemData;   // 드랍 아이템이 가지고 있는 실제 아이템 데이터
    public int stackCount;      // 떨어져 있는 수량

    public bool Interact(int playerID)
    {
        // 이벤트 버스로 아이템 획득을 전달한다.
        GlobalEventBus.OnItemPickedUp?.Invoke(playerID, itemData.TID, stackCount);
        Destroy(gameObject);

        return true;    // 상호작용 성공, 상호작용 리스트에서 제거 요청
    }
}
