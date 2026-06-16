using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    
    private Dictionary<int, ItemData> itemDataDictionary;  // 아이템 데이터 사전

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            itemDataDictionary = new Dictionary<int, ItemData>();
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);

        LoadGameData();
    }

    /* 모든 게임 데이터 로드 */
    private void LoadGameData()
    {
        ItemData[] itemDatas = Resources.LoadAll<ItemData>("ScriptableObjects/Item");
        foreach(ItemData data in itemDatas)
        {
            itemDataDictionary[data.TID] = data;
        }
    }

    /* 아이템 데이터 가져오기 */
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
