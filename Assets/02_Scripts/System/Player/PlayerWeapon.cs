using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using AnyPortrait;

public class PlayerWeapon : MonoBehaviour
{
    [Header("Equip Weapon")]
    [SerializeField] private WeaponItemData weaponData;                 // 무기 데이터
    [SerializeField] private Transform firePoint;                       // 발사 지점
    [SerializeField] private LayerMask hitMask;                         // 피격 대상 레이어 마스크

    [Header("Shot Trace Visual")]
    [SerializeField] private bool showShotTrace = true;                 // 궤적 보이기 여부
    [SerializeField] private LineRenderer shotTraceRenderer;            // 궤적 렌더러
    [SerializeField] private float shotTraceDuration = 0.3f;           // 궤적이 보이는 시간
    [SerializeField] private Color hitTraceColor = Color.white;         // 적중 했을 시 궤적 색상
    [SerializeField] private Color missTraceColor = Color.red;          // 적중하지 않을 시 궤적 색상

    public bool isEquipped => weaponData != null;                       // 무기 장착 여부
    public float nowUseMana => weaponData.useMana;                      // 현재 무기의 마나 사용량
    public float nowAttackPower => weaponData.AtkValue;                 // 무기의 공격력

    private GameObject currentWeaponInstance;                           // 현재 무기 인스턴스
    private Coroutine shotTraceCoroutine;                               // 궤적 출력 코루틴
    private WaitForSeconds shotTraceWait;                               // 궤적 출력 코루틴 WS

    [Header("Aim")]
    [SerializeField] private float aimOriginHeight = 1.0f;        // 1차 조준 레이를 쏠 높이
    [SerializeField] private float muzzleBackstepDistance = 0.3f; // 총구가 벽 안에 들어갔을 때 시작점을 뒤로 물릴 거리

    [SerializeField] public apPortrait apPort;

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

    public void initialize(WeaponItemData _weapon)
    {
        weaponData = _weapon;
    }

    public void PlayerAttack()
    {
        if (weaponData == null || firePoint == null)
            return;

        apPort.SetControlParamFloat("Yuan_Recoil", 1.0f);
        apPort.SetControlParamFloat("Yuan_B_Recoil", 1.0f);
        StartCoroutine(PlayRecoilAnimation(0.5f, "Yuan_Recoil"));
        StartCoroutine(PlayRecoilAnimation(0.5f, "Yuan_B_Recoil"));

        Vector3 muzzleOrigin = firePoint.position;
        Vector3 aimOrigin = transform.position + Vector3.up * aimOriginHeight;
        Vector3 aimDirection = firePoint.forward;

        // 1차: 먼저 "어디를 겨누고 있는지"를 구한다.
        Vector3 targetPoint = aimOrigin + aimDirection * weaponData.fireRange;

        if (Physics.Raycast(
            aimOrigin,
            aimDirection,
            out RaycastHit aimHit,
            weaponData.fireRange,
            hitMask,
            QueryTriggerInteraction.Ignore))
        {
            targetPoint = aimHit.point;
        }

        // 2차: 총구에서 targetPoint까지 실제 발사 판정을 한다.
        Vector3 shotVector = targetPoint - muzzleOrigin;
        float shotDistance = shotVector.magnitude;

        if (shotDistance <= 0.001f)
            return;

        Vector3 shotDirection = shotVector / shotDistance;

        // 총구가 벽 안에 들어간 상황을 완화하려고 시작점을 약간 뒤로 민다.
        Vector3 safeShotOrigin = muzzleOrigin - shotDirection * muzzleBackstepDistance;
        float safeShotDistance = shotDistance + muzzleBackstepDistance;

        Vector3 endPoint = targetPoint;
        Color traceColor = missTraceColor;

        if (Physics.Raycast(
            safeShotOrigin,
            shotDirection,
            out RaycastHit shotHit,
            safeShotDistance,
            hitMask,
            QueryTriggerInteraction.Ignore))
        {
            endPoint = shotHit.point;

            IEffectReceiver target = shotHit.collider.GetComponentInParent<IEffectReceiver>();

            // 실제로 데미지를 줄 수 있는 적을 맞았을 때만 흰색으로 바꾸고 피해를 준다.
            if (target != null && target.EntityFaction != Faction.player)
            {
                traceColor = hitTraceColor;
                target.TakeDamage(weaponData.AtkValue);
            }
        }

        // 궤적은 여전히 총구에서 시작해 보이게 한다.
        ShowShotTrace(muzzleOrigin, endPoint, traceColor);

        // 실제 오디오 재생과 별개로, AI는 이 총소리 이벤트를 통해 위치를 조사합니다.
        NoiseSystem.Emit(NoiseType.Gunshot, muzzleOrigin, gameObject);
    }

    // 리코일 애니메이션 출력
    public IEnumerator PlayRecoilAnimation(float recoilDuration, string controlParamName)
    {
        float elapsedTime = 0f;
        // 시간에 비례하여 리코일 애니메이션을 천천히 복구
        while (elapsedTime < recoilDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / recoilDuration);
            apPort.SetControlParamFloat(controlParamName, 1.0f - normalizedTime);
            yield return null;
        }
        apPort.SetControlParamFloat(controlParamName, 0f);
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
        //if(!weaponData.itemPrefabRef.RuntimeKeyIsValid()) return;
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
