/// <summary>
/// 플레이어의 무기 장착 및 공격 실행에 대한 스크립트
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PlayerWeapon : MonoBehaviour
{
    [Header("Equip Weapon")]
    [SerializeField] private WeaponItemData weaponData;     // 장착된 무기의 데이터
    [SerializeField] private Transform handPos;             // 무기가 장착되는 손 위치
    [SerializeField] private Transform firePoint;           // 총알이 발사되는 위치
    public bool isEquipped => weaponData != null;           // 무기가 장착되어 있는지 여부
    public float nowUseMana => weaponData.useMana;          // 현재 무기가 소모하는 마나량
    private GameObject currentWeaponInstance;               // 현재 장착 중인 무기 오브젝트


    // 임시로 무기 데이터를 설정하기 위한 프리팹 참조
    public GameObject weaponPrefab;
    public GameObject bulletPrefab;
    public GameObject bulletPool;


    private void Awake()
    {
        weaponData = null;    // 무기가 장착되어 있지 않음
    }

    /* 플레이어 공격 처리 */
    public void PlayerAttack()
    {
        // 공격 신호 시 총알 발사
        // 이후 무기에 따른 공격 패턴의 분리, 발사체 및 이펙트 생성 시 오브젝트 풀링 적용
        GameObject currentBulletObject = Instantiate(bulletPrefab, handPos.transform.position, handPos.transform.rotation, bulletPool.transform);
        // 무기의 스텟에 따라 발사체의 스텟 전달 
        ProjectileSystem bulletSystem = currentBulletObject.GetComponent<ProjectileSystem>();
        if(bulletSystem!=null)
        {
            bulletSystem.Setup(weaponData.AtkValue, Faction.player, weaponData.fireRange, weaponData.fireRange);
        }
    }

    public void EquipWeapon(WeaponItemData weaponItemData)
    {
        // weaponItemData를 통해 무기 데이터를 불러옴
        weaponData = weaponItemData;

        // 무기 프리팹 주소가 비어 있을 시 실패
        if(!weaponData.itemPrefabRef.RuntimeKeyIsValid()) return;
        // Addressble을 통해 비동기로 무기를 소환, 손 위치에 부착함
        // 2D 캐릭터를 사용하기 때문에 3D 무기 장착 코드는 사용하지 않습니다
        // Addressables.InstantiateAsync(weaponData.itemPrefabRef, handPos).Completed += OnWeaponLoaded;
    }

    /* Addressables을 통한 로딩 완료 시 실행되는 콜백 함수 */
    private void OnWeaponLoaded(AsyncOperationHandle<GameObject> handle)
    {
        // 데이터 로딩에 성공 했다면 생성된 무기 오브젝트를 저장하고 장착 상태로 전환
        if(handle.Status == AsyncOperationStatus.Succeeded)
        {
            // 로딩에 성공했다면, 생성된 무기 오브젝트를 변수에 저장하고 장착 상태를 켭니다.
            currentWeaponInstance = handle.Result;

            // 프리팹의 로컬 위치와 회전을 0으로 맞춰서 정렬
            currentWeaponInstance.transform.localPosition = Vector3.zero;
            currentWeaponInstance.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogError("무기 장착에 실패했습니다.");
        }
    }
}
