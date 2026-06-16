using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private void OnEnable()
    {
        /// 이벤트 구독 ///
        GlobalEventBus.OnItemPickedUp += GetInteraction;
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        GlobalEventBus.OnItemPickedUp -= GetInteraction;
    }

    private void GetInteraction(int itemTID)
    {
        // 아이템 TID에 따른 처리
        Debug.Log("Item picked up with TID: " + itemTID);

        // TID 100대는 무기
        if(100 < itemTID && itemTID < 200)
        {
            Debug.Log("Weapon item added to inventory.");
            //GlobalEventBus.OnWeaponEquipped?.Invoke(itemTID);   // 무기 장착 이벤트 발생
        }
        // TID 200대는 총알
        else if(200 < itemTID && itemTID < 300)
        {
            Debug.Log("Ammo item added to inventory.");
        }
        // TID 300대는 파밍 아이템
        else if(300 < itemTID && itemTID < 400)
        {
            Debug.Log("General item added to inventory.");
        }
        // 그 외 아이템은 알 수 없는 아이템
        else
        {
            Debug.LogWarning("Unknown item TID: " + itemTID);
        }
    }
}
