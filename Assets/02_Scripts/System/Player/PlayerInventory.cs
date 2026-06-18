/// <summary>
/// 플레이어의 인벤토리 데이터와 내부의 슬롯, 아이템을 관리하는 클래스
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PlayerInventory : MonoBehaviour
{
    public List<InventorySlotData> slots = new List<InventorySlotData>();
    public int slotNum { get; private set; }

    // 로컬 이벤트
    public event Action<int> OnSlotChanged;             // 특정 칸의 정보 업데이트
    
    // Addressable Assets 불러오기
    private AsyncOperationHandle<Sprite> loadHandle;    // 메모리 관리를 위해 로드 상태를 저장할 핸들

    void Awake()
    {
        slotNum = 10;

        for (int i = 0; i < slotNum; i++)
        {
            slots.Add(new InventorySlotData(0, i, 0, null));
        }
    }

    public void AddItem(ItemData _itemData, int _count)
    {
        if(_itemData==null) return;

        for (int i = 0; i < slotNum; i++)
        {
            // 해당 인벤토리 칸이 비어있다면
            if(slots[i].TID==0)
            {
                slots[i].TID = _itemData.TID;
                slots[i].amount = _count;
                LoadSprite(_itemData.icon, i);
                return;
            }
        }
    }

    private void LoadSprite(AssetReferenceSprite iconRef, int slotIndex)
    {
        loadHandle = Addressables.LoadAssetAsync<Sprite>(iconRef);

        loadHandle.Completed += (handle) =>
        {
            // 성공적으로 가져왔는지 확인
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // handle.Result에 실제 Sprite 데이터가 들어있음
                //Debug.Log("스프라이트 로드 성공");
                slots[slotIndex].icon = handle.Result;
            }
            else
            {
                //Debug.LogError("스프라이트를 불러오는 데 실패했습니다.");
            }
        };
    }
}
