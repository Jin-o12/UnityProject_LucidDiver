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

        targetPlayer = null;
        sightLength = 10;
        sightLengthSqr = sightLength * sightLength;
        
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

    private IEnumerator CheckRoutine()
    {
        while(myStatus.nowState != EnemyStatus.EnemyState.dead)
        {
            UpdateTarget();

            if(targetPlayer != null)
            {
                animator.SetBool("isWalk", true);
                ChaseTarget();
            }
            else
            {
                animator.SetBool("isWalk", false);
            }

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
        foreach(Transform player in GlobalRuntimeData.ActivePlayers)
        {
            if(player==null) continue;

            // 나(적)과 상대(플레이어) 사이의 거리 제곱을 계산하여 시야반경 내에 있는지 확인
            float sqrDist = (transform.position - player.position).sqrMagnitude;
            if(sqrDist > sightLengthSqr) continue;

            if(sqrDist<moreCloser)
            {
                moreCloser = sqrDist;
                bestTarget = player;
            }
        }
        // 가장 가까운 사람을 타겟으로 지정합니다
        targetPlayer = bestTarget;
    }

    private void ChaseTarget()
    {
        navAgent.SetDestination(targetPlayer.position);

        float sqrDistToTarget = (transform.position - targetPlayer.position).sqrMagnitude;
        if (sqrDistToTarget > sightLengthSqr)
        {
            targetPlayer = null;
            navAgent.ResetPath(); 
        }
    }

    public void Die()
    {
        animator.SetTrigger("isDead");
        Destroy(gameObject, 3.0f);
    }
}
