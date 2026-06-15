using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipWaepon : MonoBehaviour
{
    WeaponItemData weaponData;      // 장착된 무기의 데이터
    public Transform handPos;       // 무기가 장착되는 손 위치
    public Transform firePoint;     // 총알이 발사되는 위치

    public bool isEquipped => weaponData != null;   // 무기가 장착되어 있는지 여부

    // 임시로 무기 데이터를 설정하기 위한 프리팹 참조
    public GameObject weaponPrefab;
    public GameObject bulletPrefab;

    private void OnEnable()
    {
        weaponData = null;    // 무기가 장착되어 있지 않음
        
        /// 이벤트 구독 ///
        //GlobalEventBus.OnWeaponEquipped += EquipWeapon;
        GlobalEventBus.OnAttackInput += PlayerAttack;
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        //GlobalEventBus.OnWeaponEquipped -= EquipWeapon;
        GlobalEventBus.OnAttackInput -= PlayerAttack;
    }

    /* 플레이어 공격 처리 */
    private void PlayerAttack()
    {
        // 무기 소지 시 공격 처리
        if(weaponData != null)
        {
            // 공격 신호 시 총알 발사
            // 이후 무기에 따른 공격 패턴의 분리, 총알 및 이펙트 생성 시 오브젝트 풀링 적용
            GameObject currentBulletObject = Instantiate(bulletPrefab, handPos);
        }
    }

    public void EquipWeapon(WeaponItemData weaponItemData)
    {
        // weaponItemData에 따른 무기 장착 처리
        Debug.Log("Equipping weapon with TID: " + weaponItemData.TID);
        // 실제로는 weaponItemData를 통해 무기 데이터를 불러와야 함
        weaponData = weaponItemData;

        // 무기 모델을 손 위치에 장착하는 로직
        GameObject currentWeaponObject = Instantiate(weaponPrefab, handPos);

        // 프리팹의 로컬 위치와 회전을 0으로 맞춰서 정렬
        currentWeaponObject.transform.localPosition = Vector3.zero;
        currentWeaponObject.transform.localRotation = Quaternion.identity;
    }
}
