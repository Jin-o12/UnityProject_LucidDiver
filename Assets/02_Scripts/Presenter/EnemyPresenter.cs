using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPresenter : MonoBehaviour
{
    [Header("필수 컴포넌트")]
    [SerializeField] private EnemyMovement movement;     // 움직임
    [SerializeField] private EnemyAnimator animator;     // 애니메이션
    [SerializeField] private EnemyStatus status;         // 상태 관리

    private void Awake()
    {
        // 필수 컴포넌트 초기화
        movement = GetComponent<EnemyMovement>();
        animator = GetComponentInChildren<EnemyAnimator>();
        status = GetComponent<EnemyStatus>();
        if (movement == null || animator == null || status == null)
        {
            enabled = false;
            Debug.LogError("EnemyPresenter: required components are missing.");
            return;
        }
    }

    private void OnEnable()
    {
        // 이벤트 구독
        movement.OnWalkEvent += animator.PlayWalk;
        movement.OnAttackEvent += animator.PlayAttack;
        movement.OnDeathEvent += animator.PlayDeath;
        movement.OnLookDirEvent += animator.PlayLookDir;

        animator.OnGetAttack += movement.CheckAndApplyDamage;
    }

    private void OnDisable()
    {
        // 구독 해제
        movement.OnWalkEvent -= animator.PlayWalk;
        movement.OnAttackEvent -= animator.PlayAttack;
        movement.OnDeathEvent -= animator.PlayDeath;
        movement.OnLookDirEvent -= animator.PlayLookDir;

        animator.OnGetAttack -= movement.CheckAndApplyDamage;
    }
}
