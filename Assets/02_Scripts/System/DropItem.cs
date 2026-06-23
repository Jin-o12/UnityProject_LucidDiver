using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropItem : MonoBehaviour, IInteractable
{
    public ItemData itemData;   // 드롭 아이템이 가지고 있는 아이템 데이터
    public int stackCount;      // 쌓여있는 갯수

    public bool Interact(int playerID)
    {
        // 이벤트 버스에 아이템 TID 전송
        GlobalEventBus.OnItemPickedUp?.Invoke(playerID, itemData.TID, stackCount);
        Destroy(gameObject);

        return true;    // 상호작용 성공, 상호작용 리스트에서 삭제 요청
    }
}
