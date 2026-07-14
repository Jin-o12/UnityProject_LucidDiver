using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class OcclusionDetector : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("SpawnManager가 플레이어를 생성하면 자동으로 연결됩니다.")]
    [SerializeField] private Transform playerTarget;

    [Header("벽 감지")]
    [SerializeField] private LayerMask wallLayerMask;
    [SerializeField, Min(0.01f)] private float sphereRadius = 0.35f;
    [SerializeField] private float playerHeightOffset = 1f;
    [SerializeField, Min(4)] private int maxHitCount = 32;

    [Header("디버그")]
    [SerializeField] private bool showDebugLog = true;

    private RaycastHit[] hitBuffer;

    private HashSet<OcclusionWall> activeWalls = new();
    private HashSet<OcclusionWall> detectedWalls = new();

    private void Awake()
    {
        if (targetCamera == null)
        {
            // 같은 오브젝트에 있는 Camera를 자동으로 연결한다
            targetCamera = GetComponent<Camera>();
        }

        if (targetCamera == null)
        {
            enabled = false;
            Debug.LogError("OcclusionDetector: Camera가 연결되지 않았습니다.");
            return;
        }

        // 플레이어는 런타임에 생성되므로 playerTarget이 비어 있어도 컴포넌트를 끄지 않는다

        // 매 프레임 배열이 생성되지 않도록 감지 결과 버퍼를 미리 만든다
        hitBuffer = new RaycastHit[Mathf.Max(4, maxHitCount)];
    }

    private void OnEnable()
    {
        // SpawnManager가 플레이어 생성을 완료했을 때 호출하는 이벤트를 구독한다
        GlobalEventBus.OnPlayerSpawned += HandlePlayerSpawned;

        // 플레이어가 제거될 때 참조를 정리하기 위한 이벤트를 구독한다
        GlobalEventBus.OnPlayerDespawned += HandlePlayerDespawned;
    }

    private void Start()
    {
        // 카메라보다 플레이어가 먼저 생성된 경우를 대비해 기존 플레이어를 검색한다
        TryBindExistingPlayer();
    }

    private void OnDisable()
    {
        // 이벤트 중복 구독을 방지한다
        GlobalEventBus.OnPlayerSpawned -= HandlePlayerSpawned;
        GlobalEventBus.OnPlayerDespawned -= HandlePlayerDespawned;

        // 컴포넌트가 꺼질 때 모든 벽을 복원한다
        RestoreAllWalls();
    }

    private void HandlePlayerSpawned(GameObject spawnedPlayer)
    {
        if (spawnedPlayer == null)
        {
            return;
        }

        // SpawnManager가 생성한 플레이어를 감지 대상으로 연결한다
        SetPlayerTarget(spawnedPlayer.transform);
    }

    private void HandlePlayerDespawned(Transform despawnedPlayer)
    {
        if (playerTarget == null)
        {
            return;
        }

        bool isCurrentPlayer =
            despawnedPlayer == playerTarget ||
            playerTarget.IsChildOf(despawnedPlayer);

        if (!isCurrentPlayer)
        {
            return;
        }

        // 현재 감지 대상 플레이어가 제거되면 벽과 참조를 초기화한다
        RestoreAllWalls();
        playerTarget = null;
    }

    private void TryBindExistingPlayer()
    {
        if (playerTarget != null)
        {
            return;
        }

        // OnPlayerSpawned 이벤트보다 카메라가 늦게 활성화된 경우 기존 플레이어를 찾는다
        PlayerMovement existingPlayer = FindObjectOfType<PlayerMovement>();

        if (existingPlayer != null)
        {
            SetPlayerTarget(existingPlayer.transform);
        }
    }

    public void SetPlayerTarget(Transform target)
    {
        // 이전 플레이어를 기준으로 투명화된 벽을 먼저 복원한다
        RestoreAllWalls();

        // 새로운 플레이어를 가림 감지 대상으로 등록한다
        playerTarget = target;

        if (showDebugLog && playerTarget != null)
        {
            Debug.Log(
                $"OcclusionDetector: 플레이어 연결 완료 - {playerTarget.name}");
        }
    }

    private void LateUpdate()
    {
        if (playerTarget == null)
        {
            return;
        }

        DetectOccludingWalls();
    }

    private void DetectOccludingWalls()
    {
        detectedWalls.Clear();

        Vector3 cameraPosition = targetCamera.transform.position;

        // 플레이어 피벗이 발에 있을 가능성을 고려해 몸통 높이로 보정한다
        Vector3 targetPosition =
            playerTarget.position + Vector3.up * playerHeightOffset;

        Vector3 cameraToPlayer = targetPosition - cameraPosition;
        float distance = cameraToPlayer.magnitude;

        if (distance <= 0.001f)
        {
            RestoreAllWalls();
            return;
        }

        Vector3 direction = cameraToPlayer / distance;

        // 카메라에서 플레이어 방향으로 벽 감지용 SphereCast를 실행한다
        int hitCount = Physics.SphereCastNonAlloc(
            cameraPosition,
            sphereRadius,
            direction,
            hitBuffer,
            distance,
            wallLayerMask,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = hitBuffer[i].collider;

            if (hitCollider == null)
            {
                continue;
            }

            // OcclusionVolume의 부모에서 OcclusionWall을 찾는다
            OcclusionWall wall =
                hitCollider.GetComponentInParent<OcclusionWall>();

            if (wall != null)
            {
                detectedWalls.Add(wall);
            }
        }

        foreach (OcclusionWall wall in activeWalls)
        {
            if (wall != null && !detectedWalls.Contains(wall))
            {
                // 더 이상 플레이어를 가리지 않는 벽을 복원한다
                wall.SetOccluded(false);
            }
        }

        foreach (OcclusionWall wall in detectedWalls)
        {
            if (wall != null && !activeWalls.Contains(wall))
            {
                // 새롭게 플레이어를 가린 벽을 투명 처리한다
                wall.SetOccluded(true);
            }
        }

        // 다음 프레임 비교를 위해 감지 목록을 교환한다
        HashSet<OcclusionWall> temporary = activeWalls;
        activeWalls = detectedWalls;
        detectedWalls = temporary;
    }

    private void RestoreAllWalls()
    {
        foreach (OcclusionWall wall in activeWalls)
        {
            if (wall != null)
            {
                // 현재 투명 처리된 벽을 원래 상태로 복원한다
                wall.SetOccluded(false);
            }
        }

        activeWalls.Clear();
        detectedWalls.Clear();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (targetCamera == null || playerTarget == null)
        {
            return;
        }

        Vector3 startPosition = targetCamera.transform.position;
        Vector3 endPosition =
            playerTarget.position + Vector3.up * playerHeightOffset;

        // Scene 창에 실제 감지 방향을 표시한다
        Gizmos.DrawLine(startPosition, endPosition);
        Gizmos.DrawWireSphere(endPosition, sphereRadius);
    }
#endif
}
