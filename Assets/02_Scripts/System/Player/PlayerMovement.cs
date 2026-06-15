/// <summary>
/// 플레이어의 이동을 처리하는 스크립트
/// </summary>
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // 컴포넌트 참조
    private Rigidbody rb;
    private Animator animator;

    // 플레이어 이동 변수
    private Vector2 movementInput;
    private readonly float isometricYAngle = -45.0f;    // 카메라가 보는 각도에 맞춰 플레이어 이동 방향 보정 (45도)
    public float moveSpeed = 5f;

    private void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // 필수 컴포넌트가 존재하지 않을 시 스크립트 비활성화
        if(!rb || !animator)
        {
            this.enabled = false;
            Debug.LogError("PlayerMovement: 필요한 컴포넌트가 없습니다.");
            return;
        }
        
        /// 이벤트 구독 ///
        GlobalEventBus.OnPlayerMove += PlayerMove;
        GlobalEventBus.OnAttackInput += PlayerAttack;
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        GlobalEventBus.OnPlayerMove -= PlayerMove;
        GlobalEventBus.OnAttackInput -= PlayerAttack;
    }

    /* 플레이어 이동 처리 */
    private void PlayerMove(Vector2 input)
    {
        movementInput = input;
    }

    /* 플레이어 공격 처리 */
    private void PlayerAttack()
    {
        // 공격 입력이 들어왔을 때의 처리 (예: 총알 발사)
        Debug.Log("Player Attack!");
    }
    
    private void FixedUpdate()
    {
        /// 플레이어 이동 처리 ///
        // input system의 입력 값으로 평면상의 기본 벡터 생성
        Vector3 inputDir = new Vector3(movementInput.x, 0f, movementInput.y);

        // Y축을 기준으로 일정 각도 회전하는 수학적 행렬(Quaternion) 생성
        Quaternion isoRotation = Quaternion.Euler(0f, isometricYAngle, 0f);

        // 기본 입력 벡터를 일정 각도 회전시켜 쿼터뷰 화면에 맞게 보정
        // 주의: 반드시 Quaternion이 앞에 와야 합니다.
        Vector3 movement = isoRotation * inputDir;

        // 벡터 길이 정규화
        movement = movement.normalized;

        // 물리 엔진을 이용해 최종 이동
        Vector3 targetPosition = rb.position + movement * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetPosition);
    }
}
