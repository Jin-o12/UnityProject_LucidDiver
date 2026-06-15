using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class InventoryPresenter : MonoBehaviour
{
    [Header("플레이어 컴포넌트 (이후 삭제 예정)")]
    [SerializeField] private EquipWaepon EquipWaepon;

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
    private void HandleItemPickUp(int pickedItemTID)
    {
        // DataManager를 통해 ID로 아이템 원본 데이터를 찾음
        ItemData data = DataManager.Instance.GetItemData(pickedItemTID);
        if(data==null) return;

        // 주운 아이템을 인벤토리에 먼저 추가 후 하위 작업 진행(이후 구현)

        // 주운 아이템이 무기 카테고리라면
        if(data is WeaponItemData weaponData)
        {
            if(!EquipWaepon.isEquipped)
            {
                EquipWaepon.EquipWeapon(weaponData);
            }
        }
    }
}
