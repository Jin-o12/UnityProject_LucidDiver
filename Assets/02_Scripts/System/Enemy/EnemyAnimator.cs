using System;
using System.Collections;
using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    // 필수 컴포넌트
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject spriteObject;

    // 애니메이션 파라미터 해시
    private readonly int isWalkHash = Animator.StringToHash("isWalking");
    private readonly int isAttackHash = Animator.StringToHash("isAttack");
    private readonly int isDeadHash = Animator.StringToHash("isDead");
    private readonly int lookDirHash = Animator.StringToHash("LookDir");

    // 지역 이벤트
    public event Action<int> OnAttackSwing;

    private Coroutine hitStopRoutine;              // 공격을 취소하지 않고 애니메이션만 잠시 멈추는 코루틴
    private float animatorSpeedBeforeHitStop = 1f; // 히트 스톱 종료 후 복원할 기존 재생 속도

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            enabled = false;
            Debug.LogError("EnemyAnimator: 필수 컴포넌트가 존재하지 않습니다");
            return;
        }
    }

    /// <summary>
    /// 피격 순간 애니메이터만 잠시 멈춘 뒤 기존 속도로 복구합니다.
    /// 공격 트리거와 콤보 코루틴은 건드리지 않아 공격 자체는 취소되지 않습니다.
    /// </summary>
    public void PlayHitStop(float duration)
    {
        if (animator == null || duration <= 0.0f)
        {
            return;
        }

        if (hitStopRoutine != null)
        {
            StopCoroutine(hitStopRoutine);
            animator.speed = animatorSpeedBeforeHitStop;
        }

        hitStopRoutine = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        animatorSpeedBeforeHitStop = animator.speed;
        animator.speed = 0.0f;

        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        animator.speed = animatorSpeedBeforeHitStop;
        hitStopRoutine = null;
    }

    private void OnDisable()
    {
        if (hitStopRoutine == null)
        {
            return;
        }

        StopCoroutine(hitStopRoutine);
        hitStopRoutine = null;

        if (animator != null)
        {
            animator.speed = animatorSpeedBeforeHitStop;
        }
    }

    /* 이동 애니메이션 */
    public void PlayWalk(bool isWalking)
    {
        animator.SetBool(isWalkHash, isWalking);
    }

    /* 공격 애니메이션 */
    public void PlayAttack()
    {
        animator.SetTrigger(isAttackHash);
    }

    /* 사망 애니메이션 */
    public void PlayDeath()
    {
        animator.SetTrigger(isDeadHash);
    }

    /* 적의 움직임에 따른 바라보는 방향 애니메이션 */
    public void PlayLookDir(int lookDir, int lookRight)
    {
        animator.SetInteger(lookDirHash, lookDir);
        spriteObject.transform.localScale = new Vector3(lookRight, 1, 1);
    }

    /* 첫 번째 스윙 타이밍에 호출되는 애니메이션 이벤트 */
    public void GetAttack1()
    {
        OnAttackSwing?.Invoke(0);
    }

    /* 두 번째 스윙 타이밍에 호출되는 애니메이션 이벤트 */
    public void GetAttack2()
    {
        OnAttackSwing?.Invoke(1);
    }

    /* 기존 단일 이벤트와의 충돌을 막기 위해 남겨두는 빈 메서드 */
    public void GetAttack()
    {
    }
}
