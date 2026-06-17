/// <summary>
/// 플레이어의 인벤토리 데이터와 내부의 슬롯, 아이템을 관리하는 클래스
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public List<InventorySlotData> slots = new List<InventorySlotData>();

    public void AddItem(int itemTID, int count)
    {
        slots.Add(new InventorySlotData(itemTID, count));
        GlobalEventBus.OnInventoryUpdate?.Invoke();
    }
}
