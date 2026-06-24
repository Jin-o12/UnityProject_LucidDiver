using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PlayerWeapon : MonoBehaviour
{
    [Header("Equip Weapon")]
    [SerializeField] private WeaponItemData weaponData;
    [SerializeField] private Transform handPos;
    [SerializeField] private Transform firePoint;
    [SerializeField] private LayerMask hitMask;

    [Header("Shot Trace Visual")]
    [SerializeField] private bool showShotTrace = true;
    [SerializeField] private LineRenderer shotTraceRenderer;
    [SerializeField] private float shotTraceDuration = 0.08f;
    [SerializeField] private Color hitTraceColor = Color.white;
    [SerializeField] private Color missTraceColor = Color.red;

    public bool isEquipped => weaponData != null;
    public float nowUseMana => weaponData.useMana;

    private GameObject currentWeaponInstance;
    private Coroutine shotTraceCoroutine;
    private WaitForSeconds shotTraceWait;

    private void Awake()
    {
        weaponData = null;
        shotTraceWait = new WaitForSeconds(shotTraceDuration);
        HideShotTrace();
    }

    private void OnDisable()
    {
        if (shotTraceCoroutine != null)
        {
            StopCoroutine(shotTraceCoroutine);
            shotTraceCoroutine = null;
        }

        HideShotTrace();
    }

    public void PlayerAttack()
    {
        if (weaponData == null || firePoint == null)
            return;

        Vector3 origin = firePoint.position;
        Vector3 direction = firePoint.forward;
        Vector3 endPoint = origin + direction * weaponData.fireRange;
        Color traceColor = missTraceColor;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, weaponData.fireRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            endPoint = hit.point;
            traceColor = hitTraceColor;

            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();

            if (target != null && target.EntityFaction != Faction.player)
            {
                target.TakeDamage(weaponData.AtkValue);
            }
        }

        ShowShotTrace(origin, endPoint, traceColor);
    }

    public void EquipWeapon(WeaponItemData weaponItemData)
    {
        if (weaponItemData == null)
            return;

        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance);
            currentWeaponInstance = null;
        }

        weaponData = weaponItemData;

        // 무기 프리팹 주소가 비어 있을 시 실패
        if(!weaponData.itemPrefabRef.RuntimeKeyIsValid()) return;
        // Addressble을 통해 비동기로 무기를 소환, 손 위치에 부착함
        // 2D 캐릭터를 사용하기 때문에 3D 무기 장착 코드는 사용하지 않습니다
        // Addressables.InstantiateAsync(weaponData.itemPrefabRef, handPos).Completed += OnWeaponLoaded;
    }

    private void OnWeaponLoaded(AsyncOperationHandle<GameObject> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            currentWeaponInstance = handle.Result;
            currentWeaponInstance.transform.localPosition = Vector3.zero;
            currentWeaponInstance.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogError("무기 장착에 실패했습니다.");
        }
    }

    private void ShowShotTrace(Vector3 start, Vector3 end, Color traceColor)
    {
        if (!showShotTrace || shotTraceRenderer == null)
            return;

        shotTraceRenderer.positionCount = 2;
        shotTraceRenderer.SetPosition(0, start);
        shotTraceRenderer.SetPosition(1, end);
        shotTraceRenderer.startColor = traceColor;
        shotTraceRenderer.endColor = traceColor;
        shotTraceRenderer.enabled = true;

        if (shotTraceCoroutine != null)
            StopCoroutine(shotTraceCoroutine);

        shotTraceCoroutine = StartCoroutine(HideShotTraceAfterDelay());
    }

    private IEnumerator HideShotTraceAfterDelay()
    {
        yield return shotTraceWait;
        HideShotTrace();
        shotTraceCoroutine = null;
    }

    private void HideShotTrace()
    {
        if (shotTraceRenderer != null)
            shotTraceRenderer.enabled = false;
    }
}
