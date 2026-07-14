using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SessionDataSO", menuName = "Runtime Data/Session Data")]

public class SessionDataSO : ScriptableObject
{
    // 싱글톤 선언
    private static SessionDataSO _instance;
    public static SessionDataSO Instance
    {
        get
        {
            if(_instance==null)
            {
                _instance = Resources.Load<SessionDataSO>("ScriptableObjects/PlayerData/SessionDataSO");

                if(_instance == null)
                {
                    Debug.LogError("SessionDataSO 에셋을 찾을 수 없습니다");
                }
            }
            return _instance;
        }
    }

    // 이번 세션에서 획득한 아이템들을 기록하는 딕셔너리
    private Dictionary<int, int> acquiredItems = new Dictionary<int, int>();
    public Dictionary<int, int> AcquiredItems => acquiredItems;

    // 게임 시작 시점의 아이템 스냅샷을 저장하는 딕셔너리
    private Dictionary<int, int> startingItems = new Dictionary<int, int>();
    public Dictionary<int, int> StartingItems => startingItems;

    // 세션 데이터 초기화 (게임 시작 시 호출 권장)
    public void ResetSessionData()
    {
        acquiredItems.Clear();
        startingItems.Clear();
    }

    // 세션 시작 시 현재 인벤토리 상태를 스냅샷으로 저장
    public void SnapshotStartingInventory(List<InventorySlotData> inventorySlots)
    {
        startingItems.Clear();
        if (inventorySlots == null) return;

        foreach (var slot in inventorySlots)
        {
            if (slot == null || slot.TID == 0 || slot.amount <= 0) continue;

            if (startingItems.ContainsKey(slot.TID))
            {
                startingItems[slot.TID] += slot.amount;
            }
            else
            {
                startingItems.Add(slot.TID, slot.amount);
            }
        }
    }

    // 아이템 획득 기록에 특정 아이템과 수량 추가
    public void AddAcquiredItem(int _itemTID, int _amount)
    {
        if(acquiredItems.ContainsKey(_itemTID))
        {
            acquiredItems[_itemTID] += _amount;
        }
        else
        {
            acquiredItems.Add(_itemTID, _amount);
        }
    }

    // 아이템 획득 기록에서 아이템 버림 처리
    public void RemoveAcquiredItem(int _itemTID, int _amount)
    {
        if (acquiredItems.ContainsKey(_itemTID))
        {
            acquiredItems[_itemTID] -= _amount;
            if (acquiredItems[_itemTID] <= 0)
            {
                acquiredItems.Remove(_itemTID);
            }
        }
    }
}
