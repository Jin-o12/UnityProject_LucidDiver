using System;
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
