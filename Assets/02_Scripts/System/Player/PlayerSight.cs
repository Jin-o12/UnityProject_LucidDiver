using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnyPortrait;

/// <summary>
/// 플레이어의 손전등 시야 기능을 담당하는 모듈입니다.
/// </summary>
public class PlayerSight : MonoBehaviour
{
    [SerializeField] private Transform tr;                  // 플레이어 Transform 컴포넌트
    [SerializeField] private Camera mainCamera;             // 마우스 방향 계산을 위한 카메라

    public float sightRange;                                // 시야 부채꼴 반지름
    public float minCircleSight;                            // 모든 방향이 보이는 시야 최소값
    public float sightAngle;                                // 시야 부채꼴 사이각

    private float sqrCircleSight;                           // 시야 최소 제곱 캐시
    private float halfSightAngle;                           // 반 사이각 캐시
    private Vector2 currentMousePos;                        // 현재 마우스 화면 좌표
    
    private void Awake()
    {
        tr = GetComponent<Transform>();
        if (mainCamera == null) mainCamera = Camera.main;
        UpdateParameters();  // 런타임이 시작될 때 캐시 초기화
    }

    private void OnEnable()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        GlobalEventBus.OnMousePositionInput += UpdateMousePos;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnMousePositionInput -= UpdateMousePos;
    }

    private void FixedUpdate()
    {
        // 메인 카메라 연결이 끊겼다면 갱신
        if (mainCamera == null && Camera.main != null) mainCamera = Camera.main;
    }

    // 인스펙터에서 값이 바뀔 때 에디터에서 호출됨
    public void OnValidate()
    {
        UpdateParameters();
    }

    // 캐시 갱신 로직 메소드
    private void UpdateParameters()
    {
        sightRange = Mathf.Max(0.0f, sightRange);
        sightAngle = Mathf.Clamp(sightAngle, 0.0f, 360.0f);
        halfSightAngle = sightAngle * 0.5f;
        sqrCircleSight = minCircleSight * minCircleSight;
    }

    // 마우스 화면 좌표 갱신
    private void UpdateMousePos(Vector2 pos) => currentMousePos = pos;

    /* 마우스 커서가 가리키는 월드 위치 계산 */
    private bool TryGetMouseWorldPoint(Transform viewer, out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;
        if (mainCamera == null) return false;

        Plane plane = new Plane(Vector3.up, viewer.position);
        Ray ray = mainCamera.ScreenPointToRay(currentMousePos);

        if (plane.Raycast(ray, out float enter))
        {
            worldPoint = ray.GetPoint(enter);
            return true;
        }

        return false;
    }

    // target의 transform 위치가 시야 영역에 들어왔는지 판정
    public bool IsTargetInSight(Transform viewer, Transform target)
    {
        // 플레이어와 대상 오브젝트의 거리 벡터 읽기
        Vector3 Dist = target.position - viewer.position;

        // 거리 벡터를 y=0 평면에 투영
        Vector2 flatDist = new(Dist.x, Dist.z);

        // 최소 거리 이내에 있으면 보임(true 판정)
        if (flatDist.sqrMagnitude <= sqrCircleSight) return true;

        // 시야 거리 바깥에 있으면 안 보임 (false 판정)
        if (flatDist.magnitude > sightRange) return false;

        // 마우스 월드 포인트로부터 정면 벡터 계산 (viewer 기준 평면에 투영)
        Vector3 mouseWorld;
        Vector2 flatForward;
        if (TryGetMouseWorldPoint(viewer, out mouseWorld))
        {
            Vector3 forwardWorld = mouseWorld - viewer.position;
            // 거리 벡터를 y=0 평면에 투영
            flatForward = new Vector2(forwardWorld.x, forwardWorld.z);
        }
        else
        {
            // 폴백: viewer.forward를 평면에 투영
            Vector3 fwdWorld = Vector3.ProjectOnPlane(viewer.forward, Vector3.up);
            flatForward = new Vector2(fwdWorld.x, fwdWorld.z);
        }

        // 영거리 보정(true 판정)
        if (flatDist.sqrMagnitude <= 0.001f || flatForward.sqrMagnitude <= 0.001f) return true;

        // 시야 각 바깥에 있으면 안 보임(false 판정)
        float angleToTarget = Vector2.Angle(flatForward, flatDist);
        if (angleToTarget > halfSightAngle)  return false;

        // 시선 Raycast가 장애물에 가로막히는지 판정
        return HasClearLineOfSight(viewer, target);
    }

    // 플레이어와 대상 사이에 시선을 가로막는 장애물이 있는지 판정
    public bool HasClearLineOfSight(Transform viewer, Transform target)
    {
        if (viewer == null) return false;

        // 플레이어와 대상의 위치 및 거리를 y=0 평면에 투영해서 읽기
        Vector3 eyePosition = viewer.position;
        eyePosition.y = 0;
        Vector3 targetPosition = target.position;
        targetPosition.y = 0;
        Vector3 directionToTarget = targetPosition - eyePosition;
        directionToTarget.y = 0;
        float targetDistance = directionToTarget.magnitude;

        // 최소 거리 이내에 있으면 보임(true 판정)
        if (targetDistance <= minCircleSight) return true;

        // Ray가 시야 거리 내에서 히트하지 않았으면 그 방향은 끝까지 보임 (true 판정)
        if (!Physics.Raycast(
                eyePosition,
                directionToTarget.normalized,
                out RaycastHit hit,
                sightRange,
                ~0,
                QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        // Ray가 대상 및 그 하위 Collider에 히트했는지 판정
        return hit.transform == target || hit.transform.IsChildOf(target);
    }
}