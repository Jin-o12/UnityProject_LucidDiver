using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnyPortrait;

public class EnemyVisible : MonoBehaviour
{
    [Tooltip("apPortrait가 포함된 시각 오브젝트 (혹은 자기 자신)")]
    [SerializeField] private GameObject spriteObject;
    private PlayerSight playerSight;    // 플레이어 시야 스크립트
    private Rigidbody rb;               // 적 몸통 오브젝트
    private apPortrait portrait;
    private Renderer[] cachedRenderers;
    private bool isSelfSpriteObject;
    private void Awake()
    {
        if (spriteObject == null) spriteObject = gameObject;

        // 플레이어 시야 스크립트를 찾는다
        playerSight = FindObjectOfType<PlayerSight>();

        rb = GetComponentInParent<Rigidbody>();
        portrait = GetComponentInChildren<apPortrait>();
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
        // apPortrait가 있으면 컴포넌트 레벨로 제어 (안전)
        if (portrait != null)
        {
            if (portrait.enabled != visible)
                portrait.enabled = visible;
        }

        // 자식 렌더러들을 토글해서 시각적으로 숨김 처리
        foreach (var r in cachedRenderers)
        {
            if (r != null && r.enabled != visible)
                r.enabled = visible;
        }
    }
}
