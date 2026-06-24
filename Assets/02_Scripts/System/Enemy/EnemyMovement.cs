/// <summary>
/// 적의 움직임과 상태에 따른 애니메이션을 관리하는 스크립트
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [Header("Enemy Movement Controll")]
    [SerializeField] private float moveSpeed;                       // 이동 속도
    private float sightLength;                                      // 시야 거리
    private float sightAngle;                                       // 시야 각도

    /// 플레이어 위치 추적 계산 및 이를 최적화 하기 위한 변수 입니다 ///
    private Transform targetPlayer;                                 // 추적하는 플레이어
    private float sightLengthSqr;                                   // 시야 거리의 제곱 값
    private WaitForSeconds checkingTime = new WaitForSeconds(0.2f); // 상태 확인 주기 시간

    [Header("Enemy Attack")]
    private float attackLength;                                     // 공격 사거리
    private float attackLengthSqr;                                  // 공격 사거리의 제곱 값
    private float attackCooldown;                                   // 공격 쿨타임

    [Header("Enemy Animation Controll")]
    private Animator animator;

    [Header("Componemt")]
    private EnemyStatus myStatus;
    private NavMeshAgent navAgent;

    void Awake()
    {
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        myStatus = GetComponent<EnemyStatus>();

        // 필수 컴포넌트가 존재하지 않을 시 스크립트 비활성화
        if(animator==null || myStatus==null)
        {
            this.enabled = false;
            Debug.LogError("PlayerMovement: 필요한 컴포넌트가 없습니다.");
            return;
        }

        // 플레이어 추적 및 거리계산 관련 값 지정
        targetPlayer = null;
        sightLength = 7;
        sightLengthSqr = sightLength * sightLength;
        attackLength = 3;
        attackLengthSqr = attackLength * attackLength;
        attackCooldown = 2.0f;
        
        moveSpeed = 3;
    }

    private void OnEnable()
    {
        /// 이벤트 구독 ///
        myStatus.OnLocalDeath += Die;
        StartCoroutine(CheckRoutine());
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        myStatus.OnLocalDeath -= Die;
    }

    /* 현재 상태 확인 */
    private IEnumerator CheckRoutine()
    {
        while(myStatus.nowState != EnemyStatus.EnemyState.Dead)
        {
            UpdateTarget();
            ChaseTarget();
            yield return checkingTime;
        }
    }

    /* 현재 플레이어들의 위치와 자신의 위치를 계산하고 인식 범위 내에 있는지 확인합니다 */
    private void UpdateTarget()
    {
        // 가장 가까운 타겟의 (제곱된)위치를 저장하여 누가 더 가까운지 판정합니다
        float moreCloser = sightLengthSqr;
        Transform bestTarget = null;

        // 현재 존재하는 모든 플레이어들 중 적에게 가장 가까운 대상을 추적합니다
        foreach(GameObject player in GlobalRuntimeData.ActivePlayers)
        {
            if(player==null) continue;

            // 나(적)과 상대(플레이어) 사이의 거리 제곱을 계산하여 시야반경 내에 있는지 확인
            float sqrDist = (transform.position - player.transform.position).sqrMagnitude;
            if(sqrDist > sightLengthSqr) continue;

            if(sqrDist<moreCloser)
            {
                moreCloser = sqrDist;
                bestTarget = player.transform;
            }
        }
        // 가장 가까운 사람을 타겟으로 지정합니다
        targetPlayer = bestTarget;
    }

    /* 거리 계산을 통한 추적 및 공격 상태 지정 */
    private void ChaseTarget()
    {
        if(targetPlayer==null)
        {
            myStatus.SetNowState(EnemyStatus.EnemyState.Idle);
            animator.SetBool("isWalk", false);
            targetPlayer = null;
            navAgent.ResetPath();
            return;
        }

        // 적과 플레이어 사이 거리 계산
        float sqrDistToTarget = (transform.position - targetPlayer.position).sqrMagnitude;
        // 공격 사거리 안에 있을 경우 공격
        if(attackLengthSqr >= sqrDistToTarget)
        {
            StartCoroutine(Attack());
        }
        // 공격 사거리 밖이지만 시야 안에 있을 경우 추적
        else if(sqrDistToTarget <= sightLengthSqr)
        {
            navAgent.SetDestination(targetPlayer.position);
            myStatus.SetNowState(EnemyStatus.EnemyState.Chase);
            animator.SetBool("isWalk", true);
        }
    }

    public void Die()
    {
        StopAllCoroutines();
        animator.SetTrigger("isDead");
        Destroy(gameObject, 3.0f);
    }

    private IEnumerator Attack()
    {
        // 공격 상태 돌입 및 이동 정지
        myStatus.SetNowState(EnemyStatus.EnemyState.Attack);
        myStatus.SetIsAttacking(true);

        navAgent.isStopped = true;          // 적 이동 정지
        navAgent.velocity = Vector3.zero;   // 관성에 의한 이동 방지

        // 플레이어 방향으로 몸체 회전
        transform.LookAt(new Vector3(targetPlayer.position.x, transform.position.y, targetPlayer.position.z));
        
        // 애니메이션 실행
        animator.SetTrigger("isAttack");
        
        // 공격 수행 및 공격 쿨타임만큼 대기
        yield return new WaitForSeconds(attackCooldown);

        // 공격 수행 후 다음 행동 실행
        myStatus.SetIsAttacking(false);
        navAgent.isStopped = false;
    }

    public void isPlayerTakeDamage()
    {
        float sqrDistToTarget = (transform.position - targetPlayer.position).sqrMagnitude;
        
        // 공격 성공 여부 판정 시 타겟이 범위 내에 있을 경우
        if(attackLengthSqr >= sqrDistToTarget)
        {
            targetPlayer.GetComponentInParent<IDamageable>().TakeDamage(myStatus.atkValue);
        }
    }
}
