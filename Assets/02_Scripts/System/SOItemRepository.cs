using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SOItemRepository : IItemDataRepository
{
    private Dictionary<int, ItemData> itemDataDictionary = new();           // 아이템 데이터 사전

    public SOItemRepository()
    {
        LoadGameData();
    }

    /* 아이템 데이터 로드 */
    public void LoadGameData()
    {
        itemDataDictionary.Clear();
        // 아이템 데이터
        ItemData[] itemDatas = Resources.LoadAll<ItemData>("ScriptableObjects/Item");
        foreach(ItemData data in itemDatas)
        {
            itemDataDictionary[data.TID] = data;
        }
    }

    public ItemData GetItemData(int itemTID)
    {
        if(itemDataDictionary.TryGetValue(itemTID, out ItemData data))
        {
            return data;
        }
        else
        {
            Debug.LogWarning("Item TID " + itemTID + " not found!");
            return null;
        }
    }
}
