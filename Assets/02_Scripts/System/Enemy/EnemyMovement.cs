using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [Header("Enemy Movement Controll")]
    [SerializeField] private float moveSpeed;               // 이동 속도
    private float sightLength;                              // 시야 거리
    private float sightAngle;                               // 시야 각도
    

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

        moveSpeed = 3;
    }

    private void OnEnable()
    {
        /// 이벤트 구독 ///
        myStatus.OnLocalDeath += Die;
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        myStatus.OnLocalDeath -= Die;
    }

    private void Update()
    {

    }

    public void Die()
    {
        animator.SetTrigger("isDead");
        Destroy(gameObject, 3.0f);
    }
}
