/// <summary>
/// 아이템과 인벤토리에 관한 모든 상호작용의 중재자 역할을 수행합니다.
/// 아이템 습득 시 상황에 따라 즉시 장착하거나 인벤토리에 수납합니다.
/// [26.06.15_강다영] 인벤토리 UI 및 기능 구현 이후 인벤토리에 아이템이 들어가는 기능도 추가할 것
/// </summary>
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class InventoryPresenter : MonoBehaviour
{
    // 인벤토리 기능 동작 시 사용 할 플레이어 데이터
    private PlayerWeapon playerWeapon;          // 플레이어 무기
    private PlayerInventory playerInventory;    // 플레이어 인벤토리
    private int playerID;                       // 플레이어 식별자

    private void Awake()
    {
        playerWeapon = GetComponent<PlayerWeapon>();
        playerInventory = GetComponent<PlayerInventory>();
        
        if(playerWeapon==null || playerInventory==null)
        {
            this.enabled = false;
            Debug.LogError("InventoryPresenter: 필요한 컴포넌트가 없습니다.");
            return;
        }
    }

    private void OnEnable()
    {
        // 바닥에서 아이템이 주어졌을 때 터지는 전역 이벤트를 구독합니다.
        GlobalEventBus.OnItemPickedUp += HandleItemPickUp;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위한 구독 해제
        GlobalEventBus.OnItemPickedUp -= HandleItemPickUp;
    }

    /// <summary>
    /// 인벤토리 UI에서 유저가 직접 장착 버튼을 누르는 경우 (인벤토리 구현 후 개발)
    /// </summary>
    // public void OnRequestEquipWeapon(string weaponTID)
    // {}

    /// <summary>
    /// 아이템 줍기 이벤트 신호를 받았을 때 자동 장착 혹은 인벤토리 수납 수행
    /// </summary>
    private void HandleItemPickUp(int pickerID, int pickedItemTID, int count)
    {
        //Debug.Log("Item picked up with TID: " + pickedItemTID);

        // DataManager를 통해 ID로 아이템 원본 데이터를 찾음
        ItemData data = DataManager.Instance.GetItemData(pickedItemTID);
        if(data==null) return;

        // 주운 아이템을 인벤토리에 먼저 추가 후 하위 작업 진행(이후 구현)
        // TID 100대는 무기
        if(100 < pickedItemTID && pickedItemTID < 200)
        {
            Debug.Log("Weapon item added to inventory.");
            // 만약 장착하고 있는 무기가 없다면 무기를 장착하고, 그렇지 않다면 인벤토리에 수납
            if(!playerWeapon.isEquipped)
            {
                WeaponItemData weaponData = data as WeaponItemData;
                if(weaponData!=null)
                    playerWeapon.EquipWeapon(weaponData);
                else
                    Debug.LogError("무기가 아닌 아이템이 잘못된 TID를 가지고 있습니다.");
            }
            else
            {
                playerInventory.AddItem(pickedItemTID, count);
            }
        }
        // TID 300대는 소모품
        else if(300 < pickedItemTID && pickedItemTID < 400)
        {
            Debug.Log("Consume item added to inventory.");
            playerInventory.AddItem(pickedItemTID, count);
        }
        // TID 400대는 파밍 아이템
        else if(400 < pickedItemTID && pickedItemTID < 500)
        {
            Debug.Log("General item added to inventory.");
            playerInventory.AddItem(pickedItemTID, count);
        }
        // 그 외 아이템은 알 수 없는 아이템
        else
        {
            Debug.LogWarning("Unknown item TID: " + pickedItemTID);
        }
    }
}
