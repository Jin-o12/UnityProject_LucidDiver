using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnyPortrait;

public class EnemyVisible : MonoBehaviour
{
    [Tooltip("apPortrait가 포함된 시각 오브젝트 (혹은 자기 자신)")]
    [SerializeField] private GameObject spriteObject;

    private PlayerSight playerSight;            // 플레이어 시야 스크립트
    private Rigidbody rb;                       // 적 몸통 오브젝트
    private apPortrait portrait;                // 스프라이트 포트레이트
    private Renderer[] cachedRenderers;         // 스프라이트 렌더러
    private float visibleAnimationTime = 0.5f;  // 투명화 애니메이션 시간
    private float currentAlpha = 0f;            // 현재 투명도 값
    [SerializeField] private Canvas uiCanvas;   // 체력 바 캔버스

    private void Awake()
    {
        if (spriteObject == null) spriteObject = gameObject;

        // 플레이어 시야 스크립트를 찾는다
        playerSight = FindObjectOfType<PlayerSight>();

        rb = GetComponentInParent<Rigidbody>();
        portrait = GetComponentInChildren<apPortrait>();

        // 렌더러 캐시
        cachedRenderers = spriteObject.GetComponentsInChildren<Renderer>(includeInactive: true);
    }

    // 플레이어 시야 스크립트에 따라 렌더러 출력 상태 업데이트
    private void FixedUpdate()
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (playerSight == null || rb == null) return;
        bool visible = playerSight.IsTargetInSight(playerSight.transform, rb.transform);

        // apPortrait의 전체 투명도 값을 시야 노출 여부에 따라 점진적으로 변경
        float targetAlpha = visible ? 1f : 0f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime / visibleAnimationTime);
        if (portrait != null) 
        { 
            portrait.SetMeshAlphaAll(currentAlpha);
        }

        uiCanvas.enabled = visible;
    }
}
