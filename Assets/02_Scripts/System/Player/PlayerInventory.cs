/// <summary>
/// 플레이어의 인벤토리 데이터와 내부의 슬롯, 아이템을 관리하는 클래스
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public List<InventorySlotData> slots = new List<InventorySlotData>();
    public int slotNum { get; private set; }

    void Awake()
    {
        slotNum = 10;

        for (int i = 0; i < slotNum; i++)
        {
            slots.Add(new InventorySlotData(0, i, 0));
        }
    }

    public void AddItem(int _itemTID, int _count)
    {
        for (int i = 0; i < slotNum; i++)
        {
            // 해당 인벤토리 칸이 비어있다면
            if(slots[i].TID==0)
            {
                slots[i].TID = _itemTID;
                slots[i].amount = _count;
                return;
            }
        }
    }
}
